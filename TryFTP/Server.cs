using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace TryFTP
{
    class Server
    {
        private TcpListener TCPServer;

        static void Main(string[] args)
        {
            Server server = new Server(21);
        }

        public Server(int port)
        {
            try
            {
                TCPServer = new TcpListener(IPAddress.Any, port);
                TCPServer.Start();

                while (true)
                {
                    Client client = new Client(TCPServer.AcceptTcpClient());
                    Thread clientThread = new Thread(new ThreadStart(client.Process));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (TCPServer != null)
                TCPServer.Stop();
            Environment.Exit(0);
        }
    }
}
