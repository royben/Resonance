using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents an async disposable component.
    /// </summary>
    public interface IResonanceAsyncDisposable
    {
        /// <summary>
        /// Disposes component resources asynchronously.
        /// </summary>
        Task DisposeAsync();
    }
}
