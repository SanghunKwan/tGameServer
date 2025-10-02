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
    internal class NetworkMain
    {
        short _port;
        Socket _socketDB;
        TcpListener _socketListen;
        long _nowUUID;
        bool _isQuit;


        List<tSocketC> _clientList;
        List<tSocketC> _disconnectList;
        Queue<Packet> _sendGameQueue;
        Queue<Packet> _receiveGameQueue;
        Queue<Packet> _sendDBMSQueue;
        Queue<Packet> _receiveDBMSQueue;

        Thread _sendGameThread;
        Thread _receiveGameThread;
        Thread _sendDBMSThread;
        Thread _receiveDBMSThread;






        public NetworkMain(short port, long startID)
        {
            _port = port;
            _nowUUID = startID;

            _clientList = new List<tSocketC>();
            _disconnectList = new List<tSocketC>();

            _sendGameQueue = new Queue<Packet>();
            _receiveGameQueue = new Queue<Packet>();
            _sendDBMSQueue = new Queue<Packet>();
            _receiveDBMSQueue = new Queue<Packet>();

            _sendGameThread = new Thread(SendGameLoop);
            _receiveGameThread = new Thread(ReceiveGameLoop);
            _sendDBMSThread = new Thread(SendDBMSLoop);
            _receiveDBMSThread = new Thread(ReceiveDBMSLoop);
        }

        public void InitNetwork()
        {
            StartListening("127.0.0.1", _port);
        }
        #region [ServerNdbms]
        public bool ConnectDBMS(string ip, int port)
        {
            try
            {
                _socketDB = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socketDB.Connect(ip, port);
            }
            catch (Exception ex)
            {
                _socketDB = null;
                Console.WriteLine("오류 : {0}", ex.ToString());
                return false;
            }

            return true;
        }
        public void OnApplicationQuit()
        {
            if (_socketDB != null)
            {
                _socketDB.Shutdown(SocketShutdown.Both);
                _socketDB.Close();

                _socketDB = null;
            }
        }
        #endregion [ServerNdbms]
        #region [serverNClient]
        void StartListening(string ip, short port)
        {
            _socketListen = new TcpListener(IPAddress.Parse(ip), port);
            _socketListen.Start();
            _socketListen.BeginAcceptTcpClient(AcceptClient, _socketListen);
        }
        void AcceptClient(IAsyncResult iAr)
        {
            TcpListener listener = (TcpListener)iAr.AsyncState;
            tSocketC socket = new tSocketC(listener.EndAcceptTcpClient(iAr), _nowUUID++);
            _clientList.Add(socket);
            _socketListen.BeginAcceptTcpClient(AcceptClient, _socketListen);

            Console.WriteLine("Client가 접속하였습니다.");
        }
        bool IsConnected(TcpClient client)
        {
            try
            {
                if (client != null && client.Client != null && client.Client.Connected)
                {
                    if (client.Client.Poll(0, SelectMode.SelectRead))
                        return !(client.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
        void ReceiveCallBack(IAsyncResult iAr)
        {
            TcpClient client = (TcpClient)iAr.AsyncState;
            NetworkStream stream = client.GetStream();

            stream.EndRead(iAr);
        }
        #endregion [serverNClient]

        #region [Thread]
        public bool ProcessLoop()
        {
            if (_isQuit)
            {
                //네트워크 닫는 명령.
                return false;
            }

            for (int i = 0; i < _disconnectList.Count; i++)
            {
                _clientList.Remove(_disconnectList[i]);
                _disconnectList.RemoveAt(i--);
            }

            //DB 먼저
            if (_socketDB != null && _socketDB.Poll(0, SelectMode.SelectRead))
            {
                //버퍼로 받아서 packet으로 변환하여 receive에 전달.
            }
            //client 나중
            for (int i = 0; i < _clientList.Count; i++)
            {
                tSocketC client = _clientList[i];
                if (!IsConnected(client._client))
                {
                    client._client.Close();
                    _disconnectList.Add(client);
                    continue;
                }
                else
                {
                    //버퍼로 받아서 Packet으로 변환하여 ReceiveGameQueue로 전달
                    NetworkStream stream = client._client.GetStream();
                    if (stream.DataAvailable)
                    {
                        byte[] buffer = new byte[1024];
                        stream.BeginRead(buffer, 0, buffer.Length, ReceiveCallBack, stream);

                        //변환...
                    }
                }
            }

            return true;
        }

        void SendGameLoop()
        {
            while (!_isQuit)
            {
                if (_sendGameQueue.Count > 0)
                { }
            }
        }
        void ReceiveGameLoop()
        {
            while (!_isQuit)
            {
                if (_receiveGameQueue.Count > 0)
                { }
            }
        }
        void SendDBMSLoop()
        {
            while (!_isQuit)
            {
                if (_sendDBMSQueue.Count > 0)
                { }
            }
        }
        void ReceiveDBMSLoop()
        {
            while (!_isQuit)
            {
                if (_receiveDBMSQueue.Count > 0)
                { }
            }
        }
        #endregion [Thread]
    }
}
