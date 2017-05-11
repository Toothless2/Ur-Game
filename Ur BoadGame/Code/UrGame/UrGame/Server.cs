using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace UrGame
{
    public class Server  : MonoBehaviour
    {
        public int port = 9657;

        public List<ServerClient> connectedClients;
        public List<ServerClient> disconnectedClients;

        private TcpListener server;

        public GameManager manager;

        private bool serverStarted;

        public void Init()
        {
            DontDestroyOnLoad(gameObject);
            connectedClients = new List<ServerClient>();
            disconnectedClients = new List<ServerClient>();

            try
            {
                UrGame.Net.UPnP.OpenFirewallPort(port);

                server = new TcpListener(new IPEndPoint(IPAddress.Parse(GetIPV4()), 9657));
                server.Start();
                serverStarted = true;
                StartListening();
            }
            catch(Exception e)
            {
                Debug.LogWarning("Failed to Start Server: " + e);
            }

        }

        private static string ip;
        public static string GetIPV4()
        {
            if (ip != "" && ip != null)
                return ip;

            foreach (var item in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                    return (ip = item.ToString());
            }

            return "127.0.0.1";
        }

        private void Update()
        {
            //* every frame server checks for messages
            if (!serverStarted)
                return;

            CheckConnectedClients();
        }

        private void CheckConnectedClients()
        {
            foreach (var item in connectedClients)
            {
                //* is the client connected?
                if (!IsConnected(item.client))
                {
                    item.client.Close();
                    disconnectedClients.Add(item);
                }
                else
                {
                    NetworkStream ns = item.client.GetStream();

                    if (ns.DataAvailable)
                    {
                        StreamReader sr = new StreamReader(ns, true);

                        string data = sr.ReadLine();

                        if (data != null)
                            OnIncommingData(item, data);
                    }
                }
            }


            foreach (var item in disconnectedClients)
            {
                connectedClients.Remove(item);
            }

            disconnectedClients = new List<ServerClient>();
        }

        private void StartListening()
        {
            server.BeginAcceptTcpClient(AcceptTCPClient, server);
        }
        private void AcceptTCPClient(IAsyncResult result)
        {
            TcpListener listener = (TcpListener)result.AsyncState;

            string allUsers = "";
            foreach (var item in connectedClients)
            {
                allUsers += item.clientName + '|';
            }
            
            ServerClient client = new ServerClient(listener.EndAcceptTcpClient(result));

            connectedClients.Add(client);

            if(connectedClients.Count < 2)
                StartListening();

            //* asks who just joined the server and sends all usernames to the connecting client
            Broadcast($"SWHO|{allUsers}", connectedClients[connectedClients.Count - 1]);
        }

        private bool IsConnected(TcpClient c)
        {
            try
            {
                if(c != null && c.Client != null && c.Client.Connected)
                {
                    if(c.Client.Poll(0, SelectMode.SelectRead))
                    {
                        return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                    }

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        //* send from server
        private void Broadcast(string data, List<ServerClient> cl, ServerClient from)
        {
            foreach (var item in cl)
            {
                if (from.clientName == item.clientName)
                    continue;

                try
                {
                    StreamWriter w = new StreamWriter(item.client.GetStream());
                    w.WriteLine(data);
                    w.Flush();
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"Failed to send massage from server to client {item}, Exception: {e}");
                }
            }
        }
        private void Broadcast(string data, ServerClient cl)
        {
            try
            {
                StreamWriter w = new StreamWriter(cl.client.GetStream());
                w.WriteLine(data);
                w.Flush();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to send message from server to client {cl}, Exception: {e}");
            }
        }

        //* server read
        private void OnIncommingData(ServerClient c, string data)
        {
            //print($"Recived: {data}");

            string[] splitData = data.Split('|');

            //* CWHO: client connects
            //* CJC: Client just Connected
            //* TUR: Next Turn
            //* RMV: Remove chip

            switch (splitData[0])
            {
                case "CWHO":
                    c.clientName = splitData[1];
                    c.isHost = splitData[2] == "1" ? true : false;
                    Broadcast($"CJC|{c.clientName}", connectedClients, c);
                    break;
                case "TUR":
                    Broadcast(data, connectedClients, c);
                    break;
                case "RMV":
                    Broadcast(data, connectedClients, c);
                    break;
            }
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }
        private void OnDestroy()
        {
            StopServer();
        }
        private void OnDisable()
        {
            StopServer();
        }
        public void StopServer()
        {
            if (server == null)
                return;
            
            server.Stop();
            connectedClients = new List<ServerClient>();
            disconnectedClients = new List<ServerClient>();
        }
    }

    [Serializable]
    public class ServerClient
    {
        public string clientName;
        public TcpClient client;
        public bool isHost;

        public ServerClient(TcpClient tcp)
        {
            client = tcp;
        }

        ~ServerClient()
        {
            if(client != null)
                client.Close();
        }
    }
}
