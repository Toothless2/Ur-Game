using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace UrGame.Net
{
    public class UPnP
    {
        public static void OpenFirewallPort(int port)
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            //for each nic in the pc
            foreach (var nic in nics)
            {
                try
                {
                    if ((nic.GetIPProperties().UnicastAddresses.Count <= 0) || (nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel) || (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                        continue;

                    string machineIP = nic.GetIPProperties().UnicastAddresses[0].Address.ToString();

                    //sends msg to geach gateway on this nic so open the port
                    foreach (var gwinfo in nic.GetIPProperties().GatewayAddresses)
                    {
                        try
                        {
                            OpenFirewallPort(machineIP, gwinfo.Address.ToString(), port);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Error opening port: {e}");
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error opening port: {e}");
                }
            }
        }

        public static void OpenFirewallPort(string machineIp, string gwAdress, int port)
        {
            string svc = GetServicesFromDevice(gwAdress);
            OpenPortFromService(svc, "urn:schemas-upnp-org:WANIPConnection:1", machineIp, gwAdress, 80, port);
            OpenPortFromService(svc, "urn:schemas-upnp-org:WANPPPConnection:1", machineIp, gwAdress, 80, port);
        }

        private static string GetServicesFromDevice(string firewallIP)
        {
            //To send a broadcast and get responses from all, send to 239.255.255.250
            string queryResponse = "";

            try
            {
                string query = "M-SEARCH * HTTP/1.1\r\n" +
                "Host:" + firewallIP + ":1900\r\n" +
                "ST:upnp:rootdevice\r\n" +
                "Man:\"ssdp:discover\"\r\n" +
                "MX:3\r\n" +
                "\r\n" +
                "\r\n";

                //use sockets instead of UdpClient so we can set a timeout easier
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Tcp);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(firewallIP), 1900);

                //1.5 second timeout because firewall should be on same segment(fast)
                client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1500);

                byte[] q = Encoding.ASCII.GetBytes(query);
                client.SendTo(q, q.Length, SocketFlags.None, endPoint);
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint senderEP = (EndPoint)sender;

                byte[] data = new byte[1024];
                int recv = client.ReceiveFrom(data, ref senderEP);
                queryResponse = Encoding.ASCII.GetString(data);
            }
            catch { }

            if (queryResponse.Length == 0)
                return "";


            /* QueryResult is somthing like this:
            *
            HTTP/1.1 200 OK
            Cache-Control:max-age=60
            Location:http://10.10.10.1:80/upnp/service/des_ppp.xml
            Server:NT/5.0 UPnP/1.0
            ST:upnp:rootdevice
            EXT:

            USN:uuid:upnp-InternetGatewayDevice-1_0-00095bd945a2::upnp:rootdevice
            */

            string location = "";
            string[] parts = queryResponse.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (part.ToLower().StartsWith("location"))
                {
                    location = part.Substring(part.IndexOf(':') + 1);
                    break;
                }
            }
            if (location.Length == 0)
                return "";

            //then using the location url, we get more information:

            WebClient webClient = new WebClient();
            try
            {
                string ret = webClient.DownloadString(location);
                return ret;//return services
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                webClient.Dispose();
            }
        }

        private static void OpenPortFromService(string services, string serviceType, string machineIP, string firewallIP, int gatewayPort, int portToForward)
        {
            if (services.Length == 0)
                return;
            int svcIndex = services.IndexOf(serviceType);
            if (svcIndex == -1)
                return;
            string controlUrl = services.Substring(svcIndex);
            string tag1 = "<controlURL>";
            string tag2 = "</controlURL>";
            controlUrl = controlUrl.Substring(controlUrl.IndexOf(tag1)
            + tag1.Length);
            controlUrl = controlUrl.Substring(0, controlUrl.IndexOf(tag2)); string soapBody = "<s:Envelope " + "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ \" " + "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/ \">" + "<s:Body>" + "<u:AddPortMapping xmlns:u=\"" + serviceType + "\">" + "<NewRemoteHost></NewRemoteHost>" + "<NewExternalPort>" + portToForward.ToString() + "</NewExternalPort>" + "<NewProtocol>TCP</NewProtocol>" + "<NewInternalPort>" + portToForward.ToString() + "</NewInternalPort>" + "<NewInternalClient>" + machineIP + "</NewInternalClient>" + "<NewEnabled>1</NewEnabled>" + "<NewPortMappingDescription>Woodchop Client</ NewPortMappingDescription > " + "<NewLeaseDuration>0</NewLeaseDuration>" + "</u:AddPortMapping>" + "</s:Body>" + "</s:Envelope>";

            byte[] body = System.Text.UTF8Encoding.ASCII.GetBytes(soapBody);

            string url = "http://" + firewallIP + ":" + gatewayPort.ToString() + controlUrl;
            System.Net.WebRequest wr = System.Net.WebRequest.Create(url);//+ controlUrl);
            wr.Method = "POST";
            wr.Headers.Add("SOAPAction" + serviceType + "#AddPortMapping\"");
            wr.ContentType = "text/xml;charset=\"utf-8\"";
            wr.ContentLength = body.Length;

            System.IO.Stream stream = wr.GetRequestStream();
            stream.Write(body, 0, body.Length);
            stream.Flush();
            stream.Close();

            WebResponse wres = wr.GetResponse();
            System.IO.StreamReader sr = new
            System.IO.StreamReader(wres.GetResponseStream());
            string ret = sr.ReadToEnd();
            sr.Close();
        }
    }
}
