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
        ulong _nowUUID;
        bool _isQuit;


        List<tSocketC> _clientList;
        List<tSocketC> _disconnectList;
        Queue<Packet_uuid> _sendGameQueue;
        Queue<Packet_uuid> _receiveGameQueue;
        Queue<Packet_uuid> _sendDBMSQueue;
        Queue<Packet_uuid> _receiveDBMSQueue;

        Thread _sendGameThread;
        Thread _receiveGameThread;
        Thread _sendDBMSThread;
        Thread _receiveDBMSThread;






        public NetworkMain(short port, ulong startID)
        {
            _port = port;
            _nowUUID = startID;

            _clientList = new List<tSocketC>();
            _disconnectList = new List<tSocketC>();

            _sendGameQueue = new Queue<Packet_uuid>();
            _receiveGameQueue = new Queue<Packet_uuid>();
            _sendDBMSQueue = new Queue<Packet_uuid>();
            _receiveDBMSQueue = new Queue<Packet_uuid>();

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
            TestCheckId();

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
                        Packet_uuid receive = (Packet_uuid)ConverterPack.ByteArrayToStructure(buffer, typeof(Packet_uuid), receiveLength);
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
                        int receiveLength = stream.Read(buffer, 0, buffer.Length);
                        Console.WriteLine("클라 읽음");
                        if (receiveLength > 0)
                        {
                            Packet pack = (Packet)ConverterPack.ByteArrayToStructure(buffer, typeof(Packet), receiveLength);
                            Packet_uuid packUuid;
                            packUuid._uuid = client._uuid;
                            packUuid._protocol = pack._protocol;
                            packUuid._totalSize = pack._totalSize;
                            packUuid._data = new byte[1008];
                            Array.Copy(pack._data, packUuid._data, packUuid._data.Length);
                            Console.WriteLine("클라이언트에서 옴");
                            _receiveGameQueue.Enqueue(packUuid);
                        }

                        //IAsyncResult result = stream.BeginRead(buffer, 0, buffer.Length, ReceiveCallBack, client._client);

                        //변환...
                        //Packet pack = (Packet)ConverterPack.ByteArrayToStructure(buffer, typeof(Packet), 1024);

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
                {
                    Packet_uuid packUuid = _sendGameQueue.Dequeue();

                    for (int i = 0; i < _clientList.Count; i++)
                    {
                        if (_clientList[i]._uuid == packUuid._uuid)
                        {
                            Packet pack = ConverterPack.CreatePack(packUuid._protocol, packUuid._totalSize, packUuid._data);

                            byte[] bytes = ConverterPack.StructureToByteArray(pack);
                            _clientList[i]._client.Client.Send(bytes);
                            break;
                        }
                    }
                }
            }
        }
        void ReceiveGameLoop()
        {
            while (!_isQuit)
            {
                if (_receiveGameQueue.Count > 0)
                {
                    Packet_uuid packUuid = _receiveGameQueue.Dequeue();

                    switch ((SProtocol.Receive)packUuid._protocol)
                    {
                        case SProtocol.Receive.DBConnect_Success:
                            packUuid._protocol = (uint)SProtocol.Send.Client_Connect_Success;
                            packUuid._totalSize = 0;
                            Array.Clear(packUuid._data, 0, packUuid._data.Length);
                            _sendGameQueue.Enqueue(packUuid);
                            break;

                        case SProtocol.Receive.Client_Join:
                            Packet_UserData userData = (Packet_UserData)ConverterPack.ByteArrayToStructure(packUuid._data, typeof(Packet_UserData), (int)packUuid._totalSize);

                            Packet_Join packJoin;
                            packJoin._id = userData._id;
                            packJoin._pw = userData._pw;
                            packJoin._name = userData._name;
                            packJoin._gold = 1000;
                            packJoin._clearStage = 0;

                            byte[] data = ConverterPack.StructureToByteArray(packJoin);
                            packUuid._protocol = (uint)SProtocol.Send.Join_User;
                            packUuid._totalSize = (uint)data.Length;
                            Array.Copy(data, packUuid._data, data.Length);
                            int length = 1008 - data.Length;
                            if (length >= 0)
                                Array.Clear(packUuid._data, data.Length, length);

                            _sendDBMSQueue.Enqueue(packUuid);
                            break;

                        case SProtocol.Receive.Client_Login:
                            packUuid._protocol = (uint)SProtocol.Send.Login_User;
                            _sendDBMSQueue.Enqueue(packUuid);
                            break;

                        case SProtocol.Receive.Client_CheckIdDuplication:

                            Packet sendPack = ConverterPack.CreatePack((uint)SProtocol.Send.CheckId_User, packUuid._totalSize, packUuid._data);
                            packUuid._protocol = sendPack._protocol;
                            Console.WriteLine("클라이언트에서 아이디 존재 확인 요청");
                            _sendDBMSQueue.Enqueue(packUuid);
                            break;
                    }

                }
            }
        }
        void SendDBMSLoop()
        {
            while (!_isQuit)
            {
                if (_sendDBMSQueue.Count > 0)
                {
                    Packet_uuid pack = _sendDBMSQueue.Dequeue();

                    byte[] bytes = ConverterPack.StructureToByteArray(pack);
                    _socketDB.Send(bytes);

                }
            }
        }
        void ReceiveDBMSLoop()
        {
            while (!_isQuit)
            {
                if (_receiveDBMSQueue.Count > 0)
                {
                    Packet_uuid pack = _receiveDBMSQueue.Dequeue();

                    switch ((SProtocol.Receive)pack._protocol)
                    {
                        case SProtocol.Receive.DBConnect_Success:
                            Console.WriteLine("접속 성공");
                            break;

                        case SProtocol.Receive.Join_Success:
                            Console.WriteLine("join 성공");
                            pack._protocol = (uint)SProtocol.Send.Client_Join_Success;
                            Array.Clear(pack._data, 0, pack._data.Length);
                            pack._totalSize = 0;
                            _sendGameQueue.Enqueue(pack);
                            break;

                        case SProtocol.Receive.Join_Failed:
                            Packet_Std_Failed failedData = (Packet_Std_Failed)ConverterPack.ByteArrayToStructure(pack._data, typeof(Packet_Std_Failed), (int)pack._totalSize);
                            pack._protocol = (uint)SProtocol.Send.Client_Join_Failed;
                            byte[] data = ConverterPack.StructureToByteArray(failedData);
                            pack._totalSize = (uint)data.Length;
                            Array.Copy(data, pack._data, data.Length);
                            int length = pack._data.Length - data.Length;
                            if (length >= 0)
                                Array.Clear(pack._data, data.Length, length);
                            _sendGameQueue.Enqueue(pack);
                            Console.WriteLine("join 실패,{0}", failedData._errorCord);
                            break;

                        case SProtocol.Receive.Login_Success:
                            pack._protocol = (uint)SProtocol.Send.Client_Login_Success;
                            pack._totalSize = 0;
                            Array.Clear(pack._data, 0, pack._data.Length);
                            _sendGameQueue.Enqueue(pack);
                            Console.WriteLine("login 성공");
                            break;

                        case SProtocol.Receive.Login_Failed:
                            failedData = (Packet_Std_Failed)ConverterPack.ByteArrayToStructure(pack._data, typeof(Packet_Std_Failed), (int)pack._totalSize);
                            pack._protocol = (uint)SProtocol.Send.Client_Login_Failed;
                            data = ConverterPack.StructureToByteArray(failedData);
                            pack._totalSize = (uint)data.Length;
                            Array.Copy(data, pack._data, data.Length);
                            length = pack._data.Length - data.Length;
                            if (length >= 0)
                                Array.Clear(pack._data, data.Length, length);

                            _sendGameQueue.Enqueue(pack);
                            Console.WriteLine("login 실패");
                            break;

                        case SProtocol.Receive.CheckId_Success:
                            Packet depulicationTruePack = ConverterPack.CreatePack((uint)SProtocol.Send.Client_Depulication_True, 0, new byte[0]);
                            pack._protocol = depulicationTruePack._protocol;
                            _sendGameQueue.Enqueue(pack);
                            Console.WriteLine("아이디 존재 확인");
                            break;

                        case SProtocol.Receive.CheckId_Failed:
                            Packet depulicationFalsePack = ConverterPack.CreatePack((uint)SProtocol.Send.Client_Depulication_False, 0, new byte[0]);
                            pack._protocol = depulicationFalsePack._protocol;
                            _sendGameQueue.Enqueue(pack);
                            Console.WriteLine("존재하지 않는 아이디");
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
            Packet_uuid packUuid;
            packUuid._uuid = 10000000000000000;
            packUuid._protocol = (uint)SProtocol.Send.Join_User;
            packUuid._totalSize = (uint)bytes.Length;
            packUuid._data = new byte[1008];
            Array.Copy(bytes, packUuid._data, bytes.Length);
            _sendDBMSQueue.Enqueue(packUuid);
        }

        void TestLogin()
        {
            Packet_Login packetLogin;
            packetLogin._id = "asdf";
            packetLogin._pw = "zxcv";

            byte[] bytes = ConverterPack.StructureToByteArray(packetLogin);

            Packet_uuid packUuid;
            packUuid._uuid = 10000000000000000;
            packUuid._protocol = (uint)SProtocol.Send.Login_User;
            packUuid._totalSize = (uint)bytes.Length;
            packUuid._data = new byte[1008];
            Array.Copy(bytes, packUuid._data, bytes.Length);
            _sendDBMSQueue.Enqueue(packUuid);
        }
        void TestCheckId()
        {
            Packet_DuplicationId packetLogin;
            packetLogin._id = "zxcv";

            byte[] bytes = ConverterPack.StructureToByteArray(packetLogin);

            Packet_uuid packUuid;
            packUuid._uuid = 10000000000000000;
            packUuid._protocol = (uint)SProtocol.Send.CheckId_User;
            packUuid._totalSize = (uint)bytes.Length;
            packUuid._data = new byte[1008];
            Array.Copy(bytes, packUuid._data, bytes.Length);
            _sendDBMSQueue.Enqueue(packUuid);
        }

        //==
    }
}
