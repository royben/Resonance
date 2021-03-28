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
    public class ResonanceTcpServerClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the new TCP client.
        /// </summary>
        public TcpClient TcpClient { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceTcpServerClientConnectedEventArgs"/> class.
        /// </summary>
        /// <param name="tcpClient">The TCP client.</param>
        public ResonanceTcpServerClientConnectedEventArgs(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }
    }
}
