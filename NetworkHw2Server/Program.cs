using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NetworkHw2Server
{
    internal class Program
    {
        static TcpListener _listener = null!;
        static List<TcpClient> _tcpClients = [];
        static Dictionary<string, string> _clientsByNames = new();
        static void Main(string[] args)
        {
            Console.Title = "Server";
            IPAddress iPAddress = IPAddress.Parse("192.168.0.239");
            int port = 44000;
            var ep = new IPEndPoint(iPAddress, port);
            _listener = new TcpListener(ep);

            _listener.Start();
            Console.WriteLine($"Listening over: {_listener.LocalEndpoint}");

            Task.Run(AcceptClients);
            while (true)
            {
                string? msg = Console.ReadLine();
                if (msg == null) continue;

                if (msg == "_who")
                    PrintOnlineUsers();
                if (msg == "_list")
                    SendOnlineUsers();
                else
                    SendMsgToAll(msg);
            }
        }

        private static void SendOnlineUsers()
        {
            List<AppClient> appClients = [];
            var usersState = GetUsersState();
            foreach (var client in _tcpClients)
            {
                int state = 0;
                usersState.TryGetValue(client, out state);
                if (state == 1)
                {
                    appClients.Add(new AppClient
                    {
                        Name = _clientsByNames!.GetValueOrDefault(client.Client.RemoteEndPoint!.ToString()),
                        RemoteEndPoint = client.Client.RemoteEndPoint.ToString()
                    });
                }
                else
                    continue;
            }
            var clientsJson = JsonSerializer.Serialize(appClients);
            BinaryWriter bw = null!;
            var filteredTcpClients = new List<TcpClient>();
            foreach (var client in appClients)
            {
                filteredTcpClients.
                    Add(_tcpClients.First(c => c.Client.RemoteEndPoint!.ToString() == client.RemoteEndPoint));
            }
            foreach (var client in filteredTcpClients)
            {
                var stream = client.GetStream();
                bw = new BinaryWriter(stream);
                bw.Write(clientsJson);
            }
        }

        private static void AcceptClients()
        {
            while (true)
            {
                var client = _listener.AcceptTcpClient();
                _tcpClients.Add(client);
                Task.Run(() => ClientHandler(client));
            }
        }


        private static void ClientHandler(TcpClient tcpClient)
        {
            try
            {
                var stream = tcpClient.GetStream();
                var br = new BinaryReader(stream);
                string name = br.ReadString();
                Console.WriteLine($"Client {name ?? tcpClient.Client.RemoteEndPoint!.ToString()!} connected");
                _clientsByNames.Add(tcpClient.Client.RemoteEndPoint!.ToString()!, name!);
                AppClient myClient = new AppClient
                {
                    Name = name,
                    RemoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString()
                };
                while (true)
                {
                    string msg = br.ReadString();

                    Console.Write($"Client {name}: ", Console.ForegroundColor = ConsoleColor.Yellow);
                    Console.ResetColor();
                    Console.WriteLine(msg);
                }

            }
            catch (Exception)
            {

                string name = null!;
                _clientsByNames.TryGetValue(tcpClient.Client.RemoteEndPoint!.ToString()!, out name!);
                Console.WriteLine($"Client {name} is disconnected", Console.ForegroundColor = ConsoleColor.Red);
                Console.ResetColor();
            }

        }

        private static void SendMsgToAll(string msg)
        {
            foreach (var client in _tcpClients)
            {
                try
                {
                    var bw = new BinaryWriter(client.GetStream());
                    bw.Write(msg);
                }
                catch
                {
                }
            }
        }
        private static void PrintOnlineUsers()
        {
            var usersState = GetUsersState();
            if (usersState == null)
                return;
            foreach (var client in _tcpClients)
            {
                int state = 0;
                usersState.TryGetValue(client, out state);

                if (state == -1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    var nameOffline = _clientsByNames!.GetValueOrDefault((client.Client.RemoteEndPoint!.ToString()));
                    Console.WriteLine($"Client {nameOffline} is OFFLINE");
                    Console.ResetColor();
                }
                else if (state == 1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    var nameOnline = _clientsByNames!.GetValueOrDefault((client.Client.RemoteEndPoint!.ToString()));
                    Console.WriteLine($"Client {nameOnline} is online");
                    Console.ResetColor();
                }
            }
        }
        private static Dictionary<TcpClient, int> GetUsersState()
        {
            var usersState = new Dictionary<TcpClient, int>();
            BinaryWriter bw = null!;
            if (_tcpClients.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No user data found");
                Console.ResetColor();
                return null!;
            }
            foreach (var client in _tcpClients)
            {
                try
                {
                    var stream = client.GetStream();
                    bw = new BinaryWriter(stream);
                    bw.Write("_pingTest");
                    usersState.Add(client, 1);


                }
                catch (Exception)
                {
                    usersState.Add(client, -1);
                }

            }
            return usersState;
        }
    }
}
