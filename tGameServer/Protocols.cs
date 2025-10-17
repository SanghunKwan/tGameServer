using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace tGameServer.NetworkDefine
{
    #region [패킷구조체]
    [StructLayout(LayoutKind.Sequential)]
    public struct Packet
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint _protocol;
        [MarshalAs(UnmanagedType.U4)]
        public uint _totalSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1008)]
        public byte[] _data;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Packet_Std_Failed
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint _errorCord;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Packet_Join
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string _id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string _pw;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string _name;
        [MarshalAs(UnmanagedType.U4)]
        public uint _clearStage;
        [MarshalAs(UnmanagedType.U8)]
        public ulong _gold;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Packet_Login
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string _id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string _pw;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Packet_LoginData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string _name;
        [MarshalAs(UnmanagedType.U4)]
        public uint _clearStage;
        [MarshalAs(UnmanagedType.U8)]
        public ulong _gold;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Packet_UserData
    {
        [MarshalAs(UnmanagedType.U8)]
        public ulong _uuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string _id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string _pw;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string _name;
        [MarshalAs(UnmanagedType.U4)]
        public uint _clearStage;
        [MarshalAs(UnmanagedType.U8)]
        public ulong _gold;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Packet_DuplicationId
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string _id;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Packet_uuid
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint _protocol;
        [MarshalAs(UnmanagedType.U4)]
        public uint _totalSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1008)]
        public byte[] _data;
        [MarshalAs(UnmanagedType.U8)]
        public ulong _uuid;
    }
    #endregion [패킷구조체]

    #region [DB프로토콜]
    public class DBProtocol
    {
        public enum Send
        {
            DBConnect_Success               = 0,

            Join_Success,
            Join_Failed,

            Login_Success,
            Login_Failed,

            CheckId_Success,
            CheckId_Failed,

            End
        }
        public enum Receive
        {
            Join_User,

            Login_User,

            CheckId_User
        }
    }
    #endregion [DB프로토콜]

    #region [Server프로토콜]
    internal class SProtocol
    {
        public enum Send
        {
            Join_User                       = 0,
            Login_User,

            CheckId_User,


            Client_Connect_Success          = 200,
            
            Client_Join_Success,
            Client_Join_Failed,
            
            Client_Login_Success,
            Client_Login_Failed,

            Client_Depulication_True,
            Client_Depulication_False,

            End
        }
        public enum Receive
        {
            DBConnect_Success               = 0,

            Join_Success,
            Join_Failed,

            Login_Success,
            Login_Failed,

            CheckId_Success,
            CheckId_Failed,


            Client_Join                     = 200,
            Client_Login,

            Client_CheckIdDuplication,

            End
        }
    }
    #endregion [Server프로토콜]
}
