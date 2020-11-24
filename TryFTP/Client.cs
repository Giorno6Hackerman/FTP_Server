using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TryFTP
{
    class Client
    {
        string MainDir = "D:/prog/4 sem/KSIS/Labs/Lab_4/files";
        string currentDir = "D:/prog/4 sem/KSIS/Labs/Lab_4/files";
        string transferMode = "A";
        int dataPort = 6666;
        string address = "127.0.0.1";
        string currentFile;
        string createFileName;
        TcpClient client;
        string userName = "mefistofel666";
        string userPassword = "666mefistofel";
        TcpListener dataServer;

        public Client(TcpClient tcpClient)
        {
            client = tcpClient;
        }

        public void Process()
        {
            bool start = true;
            try
            {
                // Запрос клиента
                string Request = "";
                // Буфер для хранения принятых от клиента данных
                byte[] Buffer = new byte[1024];
                // Переменная для хранения количества байт, принятых от клиента
                int Count;

                while (true)
                {
                    Request = "";
                    if (start)
                    {
                        start = false;
                        SendResponse("220 FTP server ready\r\n");
                    }
                    else
                    {
                        Thread.Sleep(200);
                        while (client.GetStream().DataAvailable)
                        {
                            Count = client.GetStream().Read(Buffer, 0, Buffer.Length);
                            Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                            /*if (Request.IndexOf("\r\n\r\n") >= 0)
                            {
                                break;
                            }*/
                        }
                        Console.WriteLine(Request);
                        DefineCommand(Request);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (client != null)
                    client.Close();
            }
        }

        private void DefineCommand(string Request)
        {
            string command;
            if (Request.Length <= 0)
            {
                SendResponse("220\r\n");
                return;
            }
            if (Request.IndexOf(" ") != -1)
            {
                command = Request.Substring(0, Request.IndexOf(" "));
            }
            else
            {
                command = Request.Substring(0, Request.IndexOf("\r"));
            }

            switch (command)
            {
                case "USER":
                    string name = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r\n") - Request.IndexOf(" ") - 1);
                    if (name == userName)
                    {
                        SendResponse("331 please specify the password\r\n");
                    }
                    else
                    {
                        SendResponse("530 wrong username\r\n");
                    }
                    break;
                case "PASS":
                    string password = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r\n") - Request.IndexOf(" ") - 1);
                    if (password == userPassword)
                    {
                        SendResponse("230 login successfull\r\n");
                    }
                    else
                    {
                        SendResponse("530 wrong password\r\n");
                    }
                    break;
                case "SYST":
                    SendResponse("215 WIN\r\n");
                    break;
                case "OPTS":
                    SendResponse("200 command ok\r\n");
                    break;
                case "PWD":
                    SendResponse("257 \"/" + currentDir + "\" is the current directory\r\n");
                    break;
                case "CWD":
                    string fullPath, path;
                    if (Request.LastIndexOf("/") == (Request.IndexOf("\r") - 1))
                    {
                        fullPath = Request.Substring(Request.IndexOf("/") + 1, Request.LastIndexOf("/") - Request.IndexOf("/") - 1);
                        path = fullPath.Substring(fullPath.LastIndexOf("/") + 1);
                        while (fullPath.IndexOf("/") == 0)
                        {
                            fullPath = fullPath.Substring(1);
                        }
                    }
                    else
                    {
                        if (Request.Contains("/"))
                        {
                            fullPath = Request.Substring(Request.IndexOf("/") + 1, Request.IndexOf("\r") - Request.IndexOf("/") - 1);
                            path = Request.Substring(Request.LastIndexOf("/") + 1, Request.IndexOf("\r") - Request.LastIndexOf("/") - 1);
                        }
                        else
                        {
                            fullPath = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1);
                            path = fullPath;
                        }
                    }

                    if (!fullPath.Contains("D:"))
                    {
                        fullPath = currentDir + "/" + path;
                    }
                    
                    if (Request.Contains(".."))
                    {
                        currentDir = Directory.GetParent(currentDir).FullName;
                        string dirName = currentDir.Substring(currentDir.LastIndexOf("/") + 1);
                        SendResponse("250 current directory is /" + dirName + "\r\n");
                    }
                    else
                    {
                        if (Directory.Exists(fullPath))
                        {
                            currentDir = fullPath;
                            SendResponse("250 current directory is /" + fullPath + "\r\n");
                        }
                        else
                        {
                            SendResponse("550 no such file or directory\r\n");
                        }
                    }
                    break;
                case "TYPE":
                    string mode = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1);
                    if ((mode == "A") || (mode == "E") || (mode == "I") || (mode == "L8"))
                    {
                        transferMode = mode;
                        switch (mode)
                        {
                            case "A":
                                mode = "ASCII";
                                break;
                            case "E":
                                mode = "EBCDIC";
                                break;
                            case "I":
                                mode = "Image";
                                break;
                            case "L":
                                mode = "Local";
                                break;
                        }

                        SendResponse("200 TYPE is now " + mode + "\r\n");
                    }
                    else
                    {
                        SendResponse("501 error mode\r\n");
                    }
                    break;
                case "PASV":
                    SendResponse("227 Entering Passive Mode (127,0,0,1,26,10)");
                    break;
                case "LIST":
                    string arg = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1);
                    SendResponse("150 File status okay; about to open data connection\r\n");
                    
                    if (!arg.Contains("/"))
                    {
                        SendList();
                    }
                    else
                    { 
                        
                    }
                    SendResponse("250 transfer complete\r\n");
                    break;
                case "RETR":
                    string fileName;
                    if (Request.Contains("/"))
                    {
                        fileName = currentDir + "/" + Request.Substring(Request.LastIndexOf("/") + 1, Request.IndexOf("\r") - Request.LastIndexOf("/") - 1);
                    }
                    else
                    {
                        fileName = currentDir + "/" + Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1);
                    }
                    Console.WriteLine("filename = {0}", fileName);
                    SendResponse("150 File status okay; about to open data connection\r\n");
                    FileInfo inf = new FileInfo(fileName);
                    if (inf.Exists)
                    {
                        SendFile(fileName);
                        SendResponse("226 closing data connection\r\n");
                    }
                    else
                    {
                        SendResponse("551 file does not exist\r\n");
                    }
                    break;
                case "MKD":
                    string folder = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1);
                    Directory.CreateDirectory(currentDir + "/" + folder);
                    SendResponse("257 \"/" + currentDir + "/" + folder + "\" directory successfully created\r\n");
                    break;
                case "RMD":
                    string dir;
                    if (Request.Contains("/"))
                    {
                        if (Request.LastIndexOf("/") == (Request.IndexOf("\r") - 1))
                        {
                            dir = Request.Substring(0, Request.LastIndexOf("/"));
                            dir = dir.Substring(dir.LastIndexOf("/") + 1);
                        }
                        else
                        {
                            dir = Request.Substring(Request.LastIndexOf("/") + 1, Request.IndexOf("\r") - Request.LastIndexOf("/") - 1);
                        }
                    }
                    else
                    {
                        dir = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1);
                    }
                    Directory.Delete(currentDir + "/" + dir);
                    SendResponse("250 \"/" + currentDir + "/" + dir + "\" directory successfully deleted\r\n");
                    break;
                case "DELE":
                    string file;
                    if (Request.Contains("/"))
                    {
                        if (Request.LastIndexOf("/") == (Request.IndexOf("\r") - 1))
                        {
                            file = Request.Substring(0, Request.LastIndexOf("/"));
                            file = file.Substring(file.LastIndexOf("/") + 1);
                        }
                        else
                        {
                            file = Request.Substring(Request.LastIndexOf("/") + 1, Request.IndexOf("\r") - Request.LastIndexOf("/") - 1);
                        }
                    }
                    else
                    {
                        file = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1);
                    }
                    Console.WriteLine("file path = {0}", currentDir + "/" + file);
                    FileInfo info = new FileInfo(currentDir + "/" + file);
                    info.Delete();
                    SendResponse("250 \"/" + currentDir + "/" + file + "\" file successfully deleted\r\n");
                    break;
                case "STOR":
                    createFileName = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1);
                    SendResponse("150 File status okay; about to open data connection\r\n");
                    CreateFile();
                    SendResponse("226 file successfully stored\r\n");
                    break;
                case "QUIT":
                    SendResponse("221 bye\r\n");
                    client.Close();
                    break;
                default:
                    SendResponse("502 error\r\n");
                    break;
            }
        }

        private void SendResponse(string Response)
        {
            byte[] buf = Encoding.ASCII.GetBytes(Response);
            client.GetStream().Write(buf, 0, buf.Length);
            Console.WriteLine(Response);
        }

        private void SendList()
        {
            try
            {
                dataServer = new TcpListener(IPAddress.Parse(address), dataPort);
                dataServer.Start();

                //while (true)
                //{
                    TcpClient dataClient = dataServer.AcceptTcpClient();
                    Thread dataThread = new Thread(new ParameterizedThreadStart(ListThread));
                    dataThread.Start(dataClient);

                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (dataServer != null)
                    dataServer.Stop();
            }
        }

        public void ListThread(object dataClient)
        {
            TcpClient dClient = (TcpClient)dataClient;
            try
            {
                string Response = "";

                string[] directories = Directory.GetDirectories(currentDir);
                string[] files = Directory.GetFiles(currentDir);
                foreach (string directory in directories)
                {
                    long size = GetDirectorySize(directory);
                    DateTime date = Directory.GetCreationTime(directory);
                    Response += "drwxrwxrwx 1 root " + size.ToString() + " Apr " + date.ToString("d HH:mm") + " " + directory.Substring(directory.LastIndexOf("\\") + 1) + "\r\n";
                }
                foreach (string file in files)
                {
                    FileInfo inf = new FileInfo(file);
                    Response += "-rwxrwxrwx 1 root " + inf.Length.ToString() + " Apr " + inf.CreationTime.ToString("d HH:mm") + " " + inf.Name + "\r\n";
                }

                Console.WriteLine(Response);
                byte[] res = Encoding.ASCII.GetBytes(Response);
                dClient.GetStream().Write(res, 0, res.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (dClient != null)
                    dClient.Close();
            }
        }

        private long GetDirectorySize(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            long size = 0;
            foreach (string file in files)
            {
                FileInfo inf = new FileInfo(file);
                size += inf.Length;
            }

            string[] directories = Directory.GetDirectories(directory);
            if (directories.Length != 0)
            {
                foreach (string dir in directories)
                {
                    size += GetDirectorySize(dir);
                }
            }
            return size;            
        }

        private void SendFile(string name)
        {
            try
            {
                dataServer = new TcpListener(IPAddress.Parse(address), dataPort);
                dataServer.Start();

                currentFile = name;
                //while (true)
                //{
                TcpClient dataClient = dataServer.AcceptTcpClient();
                Thread dataThread = new Thread(new ParameterizedThreadStart(FileThread));
                dataThread.Start(dataClient);

                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (dataServer != null)
                    dataServer.Stop();
            }
        }

        public void FileThread(object dataClient)
        {
            TcpClient dClient = (TcpClient)dataClient;
            try
            {
                StreamReader reader = new StreamReader(currentFile);
                string Response = reader.ReadToEnd() + "\r\n";
                reader.Close();

                if (transferMode == "A")
                {
                    byte[] res = Encoding.ASCII.GetBytes(Response);
                    dClient.GetStream().Write(res, 0, res.Length);
                }
                else if ((transferMode == "I") ||(transferMode == "L8"))
                {
                    BinaryReader file = new BinaryReader(File.Open(currentFile, FileMode.Open));
                    byte[] res = new byte[1024];
                    int count = 0;

                    do
                    {
                        count = file.Read(res, 0, 1024);
                        dClient.GetStream().Write(res, 0, count);
                    }
                    while (count != 0);
                    file.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (dClient != null)
                    dClient.Close();
            }
        }

        private void CreateFile()
        {
            try
            {
                dataServer = new TcpListener(IPAddress.Parse(address), dataPort);
                dataServer.Start();

                //while (true)
                //{
                TcpClient dataClient = dataServer.AcceptTcpClient();
                Thread dataThread = new Thread(new ParameterizedThreadStart(CreateFileThread));
                dataThread.Start(dataClient);

                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (dataServer != null)
                    dataServer.Stop();
            }
        }

        public void CreateFileThread(object dataClient)
        {
            TcpClient dClient = (TcpClient)dataClient;
            
            try
            {
                BinaryWriter writer = new BinaryWriter(File.Create(currentDir + "/" + createFileName));

                int count;
                byte[] buf = new byte[1024];

                while (dClient.GetStream().DataAvailable)
                {
                    count = client.GetStream().Read(buf, 0, buf.Length);
                    writer.Write(buf, 0, count);
                }
                writer.Close();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (dClient != null)
                    dClient.Close();
            }
        }
    }
}
