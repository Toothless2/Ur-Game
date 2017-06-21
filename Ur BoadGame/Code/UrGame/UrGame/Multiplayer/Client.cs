using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace UrGame.Multiplayer
{
    public class Client
    {
        public string clientName;

        private bool socketReady;
        private TcpClient socket;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;

        public void ConnectToServer(string ip)
        {
            socket = new TcpClient(ip, 9657);
            //socket.Connect(UpnpPorts.GetIp(), 9657);

            stream = socket.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
        }
    }
}
