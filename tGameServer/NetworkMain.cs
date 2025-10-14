using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using tGameServer.NetworkDefine;


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
        public void SubThreadStart()
        {
            TestJoin();
            TestLogin();

            _sendGameThread.Start();
            _receiveGameThread.Start();
            _sendDBMSThread.Start();
            _receiveDBMSThread.Start();

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
                try
                {
                    //버퍼로 받아서 packet으로 변환하여 receive에 전달.
                    byte[] buffer = new byte[1024];
                    int receiveLength = _socketDB.Receive(buffer);
                    if (receiveLength > 0)
                    {
                        Packet receive = (Packet)ConverterPack.ByteArrayToStructure(buffer, typeof(Packet), receiveLength);
                        _receiveDBMSQueue.Enqueue(receive);
                    }
                    else
                    {

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("오류 : {0}", ex.ToString());
                }
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
                {


                }
            }
        }
        void SendDBMSLoop()
        {
            while (!_isQuit)
            {
                if (_sendDBMSQueue.Count > 0)
                {
                    Packet pack = _sendDBMSQueue.Dequeue();

                    switch ((SProtocol.Send)pack._protocol)
                    {
                        case SProtocol.Send.Join_User:
                            byte[] bytes = ConverterPack.StructureToByteArray(pack);
                            _socketDB.Send(bytes);
                            break;

                        case SProtocol.Send.Login_User:
                            bytes = ConverterPack.StructureToByteArray(pack);
                            _socketDB.Send(bytes);
                            break;

                    }

                }
            }
        }
        void ReceiveDBMSLoop()
        {
            while (!_isQuit)
            {
                if (_receiveDBMSQueue.Count > 0)
                {
                    Packet pack = _receiveDBMSQueue.Dequeue();

                    switch ((SProtocol.Receive)pack._protocol)
                    {
                        case SProtocol.Receive.DBConnect_Success:
                            Console.WriteLine("접속 성공");
                            break;

                        case SProtocol.Receive.Join_Success:
                            Console.WriteLine("join 성공");
                            break;

                        case SProtocol.Receive.Join_Failed:
                            Packet_Std_Failed failedData = (Packet_Std_Failed)ConverterPack.ByteArrayToStructure(pack._data, typeof(Packet_Std_Failed), (int)pack._totalSize);
                            Console.WriteLine("join 실패,{0}", failedData._errorCord);
                            break;

                        case SProtocol.Receive.Login_Success:
                            Console.WriteLine("login 성공");
                            break;

                        case SProtocol.Receive.Login_Failed:
                            Console.WriteLine("login 실패");
                            break;
                    }

                }
            }
        }
        #endregion [Thread]


        //임시
        void TestJoin()
        {
            Packet_Join packetJoin;
            packetJoin._id = "asdf";
            packetJoin._pw = "zxcv";
            packetJoin._clearStage = 0;
            packetJoin._gold = 1000;
            packetJoin._name = "qwer";

            byte[] bytes = ConverterPack.StructureToByteArray(packetJoin);
            Packet pack = ConverterPack.CreatePack((uint)SProtocol.Send.Join_User, (uint)bytes.Length, bytes);
            _sendDBMSQueue.Enqueue(pack);
        }

        void TestLogin()
        {
            Packet_Login packetLogin;
            packetLogin._id = "asdf";
            packetLogin._pw = "zxcv";

            byte[] bytes = ConverterPack.StructureToByteArray(packetLogin);

            Packet pack = ConverterPack.CreatePack((uint)SProtocol.Send.Login_User, (uint)bytes.Length, bytes);
            _sendDBMSQueue.Enqueue(pack);
        }

        //==
    }
}
