using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace UrGame.Multiplayer
{
    public static class Server
    {
        public static int port = 9657;

        public static List<ServerClient> connectedClients;

        private static TcpListener server;

        public static void StartServer()
        {
            UpnpPorts.OpenPort(port);

            server = new TcpListener(new IPEndPoint(IPAddress.Parse("0.0.0.0"), port));

            server.Start();
            StartListening();
        }

        public static void StartListening()
        {
            server.BeginAcceptTcpClient(AcceptTcpClient, server);
        }

        private static void AcceptTcpClient(IAsyncResult ar)
        {
            TcpListener listner = (TcpListener)ar.AsyncState;

            string allUsers = "";

            foreach (var user in connectedClients)
            {
                allUsers += $"{user.name}|";
            }

            ServerClient client = new ServerClient(listner.EndAcceptTcpClient(ar));

            connectedClients.Add(client);

            StartListening();
        }

        public static void StopServer()
        {
            UpnpPorts.RemovePortMapping(port);
            server.Stop();
        }
    }

    public class ServerClient
    {
        public string name;
        public TcpClient client;

        public ServerClient(TcpClient client)
        {
            this.client = client;
        }
    }
}
