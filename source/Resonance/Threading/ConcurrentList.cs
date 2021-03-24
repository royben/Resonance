using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Threading
{
    /// <summary>
    /// Thread safe List.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Collections.Generic.IList{T}" />
    /// <seealso cref="System.IDisposable" />
    internal class ConcurrentList<T> : IList<T>, IDisposable
    {
        #region Fields
        internal readonly List<T> InnerList;
        private readonly ReaderWriterLockSlim _lock;
        #endregion

        #region Constructors
        public ConcurrentList()
        {
            this._lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            this.InnerList = new List<T>();
        }

        public ConcurrentList(int capacity)
        {
            this._lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            this.InnerList = new List<T>(capacity);
        }

        public ConcurrentList(IEnumerable<T> items)
        {
            this._lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            this.InnerList = new List<T>(items);
        }
        #endregion

        #region Methods
        public void Add(T item)
        {
            try
            {
                this._lock.EnterWriteLock();
                this.InnerList.Add(item);
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }

        public void Insert(int index, T item)
        {
            try
            {
                this._lock.EnterWriteLock();
                this.InnerList.Insert(index, item);
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }

        public bool Remove(T item)
        {
            try
            {
                this._lock.EnterWriteLock();
                return this.InnerList.Remove(item);
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }

        public void RemoveAt(int index)
        {
            try
            {
                this._lock.EnterWriteLock();
                this.InnerList.RemoveAt(index);
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }

        public int IndexOf(T item)
        {
            try
            {
                this._lock.EnterReadLock();
                return this.InnerList.IndexOf(item);
            }
            finally
            {
                this._lock.ExitReadLock();
            }
        }

        public void Clear()
        {
            try
            {
                this._lock.EnterWriteLock();
                this.InnerList.Clear();
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                this._lock.EnterReadLock();
                return this.InnerList.Contains(item);
            }
            finally
            {
                this._lock.ExitReadLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            try
            {
                this._lock.EnterReadLock();
                this.InnerList.CopyTo(array, arrayIndex);
            }
            finally
            {
                this._lock.ExitReadLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(this.InnerList, this._lock);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(this.InnerList, this._lock);
        }

        ~ConcurrentList()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            this._lock.Dispose();
        }
        #endregion

        #region Properties
        public T this[int index]
        {
            get
            {
                try
                {
                    this._lock.EnterReadLock();
                    return this.InnerList[index];
                }
                finally
                {
                    this._lock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    this._lock.EnterWriteLock();
                    this.InnerList[index] = value;
                }
                finally
                {
                    this._lock.ExitWriteLock();
                }
            }
        }

        public int Count
        {
            get
            {
                try
                {
                    this._lock.EnterReadLock();
                    return this.InnerList.Count;
                }
                finally
                {
                    this._lock.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
        #endregion
    }

    /// <summary>
    /// Thread safe enumerator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Collections.Generic.IEnumerator{T}" />
    public class ConcurrentEnumerator<T> : IEnumerator<T>
    {
        #region Fields
        private readonly IEnumerator<T> _inner;
        private readonly ReaderWriterLockSlim _lock;
        #endregion

        #region Constructor
        public ConcurrentEnumerator(IEnumerable<T> inner, ReaderWriterLockSlim @lock)
        {
            this._lock = @lock;
            this._lock.EnterReadLock();
            this._inner = inner.GetEnumerator();
        }
        #endregion

        #region Methods
        public bool MoveNext()
        {
            return _inner.MoveNext();
        }

        public void Reset()
        {
            _inner.Reset();
        }

        public void Dispose()
        {
            this._lock.ExitReadLock();
        }
        #endregion

        #region Properties
        public T Current
        {
            get { return _inner.Current; }
        }

        object IEnumerator.Current
        {
            get { return _inner.Current; }
        }
        #endregion
    }
}
