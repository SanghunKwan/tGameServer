using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tGameServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerMainManager manager = new ServerMainManager();

            manager.InitServer();
            manager.RunServer();

        }
    }
}
