using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR
{
    public static class ResonanceHubMethods
    {
        public const string Login = nameof(Login); // Service | Client
        public const string RegisterService = nameof(RegisterService); // Service
        public const string UnregisterService = nameof(UnregisterService); // Service
        public const string GetAvailableServices = nameof(GetAvailableServices); // Client

        public const string Connect = nameof(Connect); // Client
        public const string ConnectionRequest = nameof(ConnectionRequest); // Hub
        public const string AcceptConnection = nameof(AcceptConnection); // Client
        public const string DeclineConnection = nameof(DeclineConnection); // Client
        public const string Connected = nameof(Connected); // Hub
        public const string Declined = nameof(Declined); // Hub
        public const string ServiceDown = nameof(ServiceDown); // Hub
        public const string Disconnect = nameof(Disconnect); // Client
        public const string Disconnected = nameof(Disconnected); // Hub

        public const string Write = nameof(Write); // Client
        public const string DataAvailable = nameof(DataAvailable); // Hub
    }
}
