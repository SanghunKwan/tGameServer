using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tGameServer
{
    internal class ServerMainManager
    {
        string _dbTableName = "userinfodata";
        long _stdUUID = 10000000000000000;


        NetworkMain _netMain;

        public void InitServer()
        {
            //_stdUUID를 파일 또는 DB에서 받아와야 함.

            _netMain = new NetworkMain(666, _stdUUID);
            while (!_netMain.ConnectDBMS("127.0.0.1", 789))
            {
                Console.WriteLine("DB에 접속하지 못했습니다.");
                Thread.Sleep(1000);
            }
            ReadyServer();
        }

        
        public void RunServer()
        {
            while (_netMain.ProcessLoop())
            {
                //서버 명령 사용...
            }

            _netMain.OnApplicationQuit();
        }
        void ReadyServer()
        {
            _netMain.InitNetwork();
            _netMain.SubThreadStart();
        }
    }
}
