using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UrGame.Multiplayer
{
    public class StartGame : MonoBehaviour
    {
        private void Start()
        {
            Server.StartServer();
            //UpnpPorts.CheckPort();

            new Client().ConnectToServer("127.0.0.1");
        }

        private void OnApplicationQuit()
        {
            Server.StopServer();
        }
    }
}
