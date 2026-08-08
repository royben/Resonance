using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Adapters.SharedMemory
{
    /// <summary>
    /// Represents a Resonance shared memory communication adapter.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    [SupportedOSPlatform("windows")]
    public class SharedMemoryAdapter : ResonanceAdapter
    {
        private EventWaitHandle _thisSemaphore;
        private EventWaitHandle _otherSemaphore;
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private Thread _readThread;
        private String _otherSemaphoreName;
        private String _thisSemaphoreName;
        private const int SEMAPHORE_NAME_LENGTH = 36 + 7;

        /// <summary>
        /// The number of bytes reserved at the start of the buffer for the message length.
        /// </summary>
        private const int LENGTH_PREFIX_SIZE = 4;

        /// <summary>
        /// The smallest buffer that can still carry the connection handshake.
        /// </summary>
        private const int MINIMUM_BUFFER_SIZE = SEMAPHORE_NAME_LENGTH * 2;

        /// <summary>
        /// The default shared memory buffer size in bytes (1 MB).
        /// </summary>
        public const int DefaultBufferSize = 1024 * 1024;

        /// <summary>
        /// Gets the shared memory channel name.
        /// </summary>
        public String Address { get; }

        /// <summary>
        /// Gets the size of the shared memory buffer in bytes.
        /// The largest message that can be sent is this value minus four bytes,
        /// which are used for the message length prefix.
        /// </summary>
        public int BufferSize { get; }

        /// <summary>
        /// Gets the largest encoded message size, in bytes, that this adapter can write.
        /// </summary>
        public int MaxMessageSize => BufferSize - LENGTH_PREFIX_SIZE;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedMemoryAdapter"/> class
        /// using the <see cref="DefaultBufferSize"/>.
        /// </summary>
        /// <param name="address">A unique address name (must match with the other-side adapter).</param>
        public SharedMemoryAdapter(String address) : this(address, DefaultBufferSize)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedMemoryAdapter"/> class.
        /// </summary>
        /// <param name="address">A unique address name (must match with the other-side adapter).</param>
        /// <param name="bufferSize">
        /// The shared memory buffer size in bytes. Both adapters sharing an address should
        /// specify the same size: the first one to connect creates the mapping and fixes its
        /// capacity, and the second one simply opens it.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the buffer is too small to carry the handshake.</exception>
        public SharedMemoryAdapter(String address, int bufferSize)
        {
            if (bufferSize < MINIMUM_BUFFER_SIZE)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bufferSize),
                    $"The shared memory buffer size must be at least {MINIMUM_BUFFER_SIZE} bytes.");
            }

            Address = address;
            BufferSize = bufferSize;
        }

        protected override Task OnConnect()
        {
            bool created = false;
            Mutex syncMutex = new Mutex(true, Address + "-SYNC-MUTEX", out created);
            _mmf = MemoryMappedFile.CreateOrOpen(Address, BufferSize);
            _accessor = _mmf.CreateViewAccessor();

            if (created)
            {
                _thisSemaphoreName = GetRandomSemaphoreName();
                _otherSemaphoreName = GetRandomSemaphoreName();

                byte[] thisMutexNameData = Encoding.ASCII.GetBytes(_thisSemaphoreName);
                byte[] otherMutexNameData = Encoding.ASCII.GetBytes(_otherSemaphoreName);

                _accessor.WriteArray(0, thisMutexNameData, 0, thisMutexNameData.Length);
                _accessor.WriteArray(thisMutexNameData.Length, otherMutexNameData, 0, otherMutexNameData.Length);

                syncMutex.ReleaseMutex();
            }
            else
            {
                syncMutex.WaitOne();

                byte[] otherMutexNameData = new byte[SEMAPHORE_NAME_LENGTH];
                byte[] thisMutexNameData = new byte[SEMAPHORE_NAME_LENGTH];

                _accessor.ReadArray(0, otherMutexNameData, 0, otherMutexNameData.Length);
                _accessor.ReadArray(otherMutexNameData.Length, thisMutexNameData, 0, thisMutexNameData.Length);

                _thisSemaphoreName = Encoding.ASCII.GetString(thisMutexNameData);
                _otherSemaphoreName = Encoding.ASCII.GetString(otherMutexNameData);
            }

            State = ResonanceComponentState.Connected;

            _readThread = new Thread(ReadThreadMethod);
            _readThread.IsBackground = true;
            _readThread.Start();

            return Task.FromResult(true);
        }

        protected override Task OnDisconnect()
        {
            _accessor?.Dispose();
            _mmf?.Dispose();

            State = ResonanceComponentState.Disconnected;

            if (_thisSemaphore != null)
            {
                _thisSemaphore.Set();
                _thisSemaphore.Dispose();
            }

            return Task.FromResult(true);
        }

        protected override void OnWrite(byte[] data)
        {
            if (_otherSemaphore == null)
            {
                _otherSemaphore = new EventWaitHandle(false, EventResetMode.AutoReset, _otherSemaphoreName);
            }

            // Report the limit explicitly. Letting MemoryMappedViewAccessor throw produces
            // "Not enough space available in the buffer", which says nothing about which
            // buffer, how large it is, or how to change it.
            if (data.Length > MaxMessageSize)
            {
                throw new InvalidOperationException(
                    $"The encoded message is {data.Length} bytes, which exceeds the {MaxMessageSize} bytes " +
                    $"available in the shared memory buffer of '{Address}'. Construct both adapters with a " +
                    $"larger bufferSize, for example new {nameof(SharedMemoryAdapter)}(address, {BufferSize * 2}).");
            }

            _accessor.Write(0, data.Length);
            _accessor.WriteArray<byte>(LENGTH_PREFIX_SIZE, data, 0, data.Length);

            _otherSemaphore.Set();
        }

        private void ReadThreadMethod()
        {
            while (State == ResonanceComponentState.Connected)
            {
                if (_thisSemaphore == null)
                {
                    if (!EventWaitHandle.TryOpenExisting(_thisSemaphoreName, out _thisSemaphore))
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                }

                _thisSemaphore.WaitOne();

                if (State != ResonanceComponentState.Connected)
                {
                    return;
                }

                int length = _accessor.ReadInt32(0);
                byte[] data = new byte[length];
                _accessor.ReadArray<byte>(LENGTH_PREFIX_SIZE, data, 0, data.Length);

                if (length > 0)
                {
                    OnDataAvailable(data);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        private String GetRandomSemaphoreName()
        {
            return $"Global\\{Guid.NewGuid()}";
        }
    }
}
