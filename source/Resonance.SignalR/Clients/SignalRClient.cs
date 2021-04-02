using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Clients
{
    internal class SignalRClient : ISignalRClient
    {
        private HubConnection _connection;
        private IHubProxy _proxy;

        public String Url { get; private set; }

        public SignalRClient(String url)
        {
            Url = url;
        }

        public Task Start()
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }

        public Task Invoke(string methodName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public Task<T> Invoke<T>(string methodName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public IDisposable On(string methodName, Action action)
        {
            throw new NotImplementedException();
        }

        public IDisposable On<T>(string methodName, Action<T> action)
        {
            throw new NotImplementedException();
        }

        public IDisposable On<T1, T2>(string methodName, Action<T1, T2> action)
        {
            throw new NotImplementedException();
        }

        public IDisposable On<T1, T2, T3>(string methodName, Action<T1, T2, T3> action)
        {
            throw new NotImplementedException();
        }
    }
}
