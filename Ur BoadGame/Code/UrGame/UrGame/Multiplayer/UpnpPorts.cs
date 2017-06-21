using System.Linq;
using System.Net;
using System.Net.Sockets;
using Open.Nat;
using static UnityEngine.MonoBehaviour;
using System.Threading;
using System.Threading.Tasks;

namespace UrGame.Multiplayer
{
    public class UpnpPorts
    {
        public static NatDevice openPortDevice;

        public static IPAddress GetIp()
        {
            var discoverer = new NatDiscoverer();
            var device = discoverer.DiscoverDeviceAsync(PortMapper.Upnp, new CancellationTokenSource()).Result;
            var ip = device.GetExternalIPAsync().Result;

            return ip;
        }

        public static string GetLocalIp()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if(ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    print(ip.ToString());
                    return ip.ToString();
                }
            }

            return "127.0.0.1";
        }

        public static void OpenPort(int port = 9657)
        {
            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource();
            openPortDevice = discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts).Result;
            Task t = Task.Factory.StartNew(() => {
                                        openPortDevice.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port, "Ur Game Port"));
                                    });
            t.Wait();
        }

        public static void RemovePortMapping(int port = 9657)
        {
            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource();
            var device = discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts).Result;

            device.DeletePortMapAsync(new Mapping(Protocol.Tcp, port, port, "Ur Game Port"));
        }

        public static void CheckPort()
        {
            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource();
            var device = discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts).Result;
            int count = device.GetAllMappingsAsync().Result.ToArray().Length;
            var things = device.GetAllMappingsAsync().Result.ToArray();

            for (int i = 0; i < count; i++)
            {
                print(things[i]);
            }
        }
    }
}
