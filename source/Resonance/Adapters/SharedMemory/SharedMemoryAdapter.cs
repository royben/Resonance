using System;
using System.Collections.Generic;
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
        /// Gets the shared memory channel name.
        /// </summary>
        public String Address { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedMemoryAdapter"/> class.
        /// </summary>
        /// <param name="address">A unique address name (must match with the other-side adapter).</param>
        public SharedMemoryAdapter(String address)
        {
            Address = address;
        }

        protected override Task OnConnect()
        {
            bool created = false;
            Mutex syncMutex = new Mutex(true, Address + "-SYNC-MUTEX", out created);
            _mmf = MemoryMappedFile.CreateOrOpen(Address, 1000);
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

            _accessor.Write(0, data.Length);
            _accessor.WriteArray<byte>(4, data, 0, data.Length);

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
                _accessor.ReadArray<byte>(4, data, 0, data.Length);

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
