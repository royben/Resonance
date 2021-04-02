using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Clients
{
    internal interface ISignalRClient
    {
        String Url { get; }

        Task Start();

        Task Stop();

        Task Invoke(String methodName, params object[] args);

        Task<T> Invoke<T>(String methodName, params object[] args);

        IDisposable On(String methodName, Action action);

        IDisposable On<T>(String methodName, Action<T> action);

        IDisposable On<T1, T2>(String methodName, Action<T1, T2> action);

        IDisposable On<T1, T2, T3>(String methodName, Action<T1, T2, T3> action);
    }
}
