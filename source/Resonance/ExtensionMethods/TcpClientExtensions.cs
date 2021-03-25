using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Resonance.ExtensionMethods
{
    public static class TcpClientExtensions
    {
        /// <summary>
        /// Returns the TcpClient remote end point IP address.
        /// </summary>
        /// <param name="tcpClient">The tcp client.</param>
        /// <returns></returns>
        public static IPAddress GetIPAddress(this TcpClient tcpClient)
        {
            return (tcpClient.Client.RemoteEndPoint as IPEndPoint).Address;
        }

        /// <summary>
        /// Gets the TcpClient remote end point port number.
        /// </summary>
        /// <param name="tcpClient">The tcp client.</param>
        /// <returns></returns>
        public static int GetPort(this TcpClient tcpClient)
        {
            return ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port;
        }
    }
}
