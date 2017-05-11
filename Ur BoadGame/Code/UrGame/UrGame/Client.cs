using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace UrGame
{
    public class Client : MonoBehaviour
    {
        public string clientName;

        private bool socketReady;
        private TcpClient socket;
        private NetworkStream stream;
        private StreamWriter writer;
        private StreamReader reader;

        public bool isHost = false;

        private List<GameClient> connectedClients = new List<GameClient>();

        public bool ConnectToServer(string host, int port)
        {
            if (socketReady)
                return false;

            try
            {
                socket = new TcpClient();
                socket.Connect(IPAddress.Parse(host), port);
                stream = socket.GetStream();
                writer = new StreamWriter(stream);
                reader = new StreamReader(stream);

                socketReady = true;
            }
            catch(Exception e)
            {
                CloseSocket();
                FindObjectOfType<MainMenuManager>().BackButton();
                Debug.LogWarning($"Connecting Socket Error {e}");
            }

            return false;
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (socketReady)
                if (stream.DataAvailable)
                        OnIncommingData(reader.ReadLine() ?? "");
        }

        //* read msgs from server
        private void OnIncommingData(string data)
        {
            string[] splitData = data.Split('|');

            switch (splitData[0])
            {
                case "SWHO":
                    Send($"CWHO|{clientName}|{(isHost ? 1 : 0)}");

                    for (int i = 1; i < splitData.Length; i++)
                    {
                        UserConnected(splitData[i], false);
                    }
                    
                    break;
                case "CJC":
                    if(splitData[1] != clientName)
                        UserConnected(splitData[1], false);
                    break;
                case "TUR":
                    GameManager.managerInstance.MoveingPiece(float.Parse(splitData[1]), float.Parse(splitData[2]), float.Parse(splitData[3]), float.Parse(splitData[4]));
                    GameManager.managerInstance.StartTurn(bool.Parse(splitData[5]), int.Parse(splitData[6]), int.Parse(splitData[7]));
                    break;
                case "RMV":
                    GameManager.managerInstance.chips[int.Parse(splitData[1])].ReturnToStart();
                    GameManager.managerInstance.MoveingPiece(float.Parse(splitData[2]), float.Parse(splitData[3]), float.Parse(splitData[4]), float.Parse(splitData[5]));
                    GameManager.managerInstance.StartTurn(bool.Parse(splitData[6]), int.Parse(splitData[7]), int.Parse(splitData[8]));
                    break;
            }
        }
        
        //* send msgs to the server
        public bool Send(string data)
        {
            if (!socketReady)
                return false;

            print(data);

            writer.WriteLine(data);
            writer.Flush();
            return true;
        }

        private void UserConnected(string clientName, bool host)
        {
            GameClient c = new GameClient()
            {
                name = clientName ?? "temp"
            };

            connectedClients.Add(c);

            if (connectedClients.Count == 2)
                MainMenuManager.instnace.StartGame();
        }

        private void OnApplicationQuit()
        {
            CloseSocket();
        }
        private void OnDisable()
        {
            CloseSocket();
        }
        private void OnDestroy()
        {
            CloseSocket();
        }
        public void CloseSocket()
        {
            if (!socketReady)
                return;

            reader.Close();
            writer.Close();
            stream.Close();
            socket.Close();
        }
    }

    public class GameClient
    {
        public string name;
        public bool isHost;
    }
}
