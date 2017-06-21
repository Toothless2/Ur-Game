using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UrGame.Multiplayer
{
    public class TestClient : MonoBehaviour
    {
        public bool connect;
        bool hasConnected;

        private void Update()
        {
            if (connect && !hasConnected)
            {
                hasConnected = true;
                Connected();
            }
        }

        private void Connected()
        {
            new Client().ConnectToServer(UpnpPorts.GetIp().ToString());
        }
    }
}
