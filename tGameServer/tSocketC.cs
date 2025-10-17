using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;

namespace tGameServer
{
    internal class tSocketC
    {
        public TcpClient _client;
        public ulong _uuid;

        public tSocketC(TcpClient socket, ulong id)
        {
            _client = socket;
            _uuid = id;
        }
    }
}
