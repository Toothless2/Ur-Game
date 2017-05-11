using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UrGame
{
    public class MainMenuManager : MonoBehaviour
    {
        public static MainMenuManager instnace;

        public GameObject mainMenu;
        public GameObject serverMenu;
        public GameObject connectionMenu;

        public Text watingText;

        public InputField nameInput;

        public GameObject serverPrefab;
        public GameObject clientPrefab;

        private Server server;
        private Client client;

        private void Start()
        {
            instnace = this;
            BackButton();

            DontDestroyOnLoad(gameObject);
        }

        public void ConnectButton()
        {
            mainMenu.SetActive(false);
            serverMenu.SetActive(false);
            connectionMenu.SetActive(true);
        }

        public void HostButton()
        {
            mainMenu.SetActive(false);
            connectionMenu.SetActive(false);
            serverMenu.SetActive(true);
            try
            {
                server = Instantiate(serverPrefab).GetComponent<Server>();
                server.Init();

                client = Instantiate(clientPrefab).GetComponent<Client>();
                client.isHost = true;
                client.clientName = nameInput.text != "" ? nameInput.text : "Host";
                client.ConnectToServer(Server.GetIPV4(), 9657);

                connectionMenu.SetActive(false);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to start server: {e}");
            }
        }

        public void ConnectToServerbutton()
        {
            string hostAdress = GameObject.Find("HostInput")?.GetComponent<InputField>().text ?? "";

            if (hostAdress == "" || hostAdress == null)
                hostAdress = Server.GetIPV4();

            try
            {

                UrGame.Net.UPnP.OpenFirewallPort(9657);

                client = Instantiate(clientPrefab).GetComponent<Client>();
                client.clientName = nameInput.text != ""? nameInput.text :  "Client";
                client.ConnectToServer(hostAdress, 9657);

                connectionMenu.SetActive(false);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to connect to server {e}");
            }
        }

        public void BackButton()
        {
            if (server != null)
                Destroy(server.gameObject);

            if (client != null)
                Destroy(client.gameObject);

            server = null;
            connectionMenu.SetActive(false);
            serverMenu.SetActive(false);
            mainMenu.SetActive(true);
        }

        public void StartGame()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
