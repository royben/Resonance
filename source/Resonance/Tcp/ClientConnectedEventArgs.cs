using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Resonance.Tcp
{
    /// <summary>
    /// Represents a <see cref="ResonanceTcpServer.ClientConnected"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the new tcp client.
        /// </summary>
        public TcpClient TcpClient { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectedEventArgs"/> class.
        /// </summary>
        /// <param name="tcpClient">The tcp client.</param>
        public ClientConnectedEventArgs(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }
    }
}
