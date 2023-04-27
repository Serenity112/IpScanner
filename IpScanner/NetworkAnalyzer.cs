using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace IpScanner
{
    public class NetworkAnalyzer
    {
        private List<string> _targetAddresses;

        public NetworkAnalyzer()
        {
        }

        public List<DeviceData> ScanNetwork(List<string> adapters)
        {
            List<DeviceData> data = new List<DeviceData>();
            List<Thread> threads = new List<Thread>();

            foreach (var adapterip in adapters)
            {
                string[] networkIp = adapterip.Split('.');
                networkIp[3] = "1";

                foreach (string ipaddress in GetPossibleIp(networkIp))
                {
                    if(ipaddress == adapterip)
                    {
                        continue;
                    }

                    Thread thread = new Thread(() =>
                    {
                        DeviceData? d = SendArpRequest(IPAddress.Parse(adapterip), IPAddress.Parse(ipaddress));

                        if (d != null)
                        {
                            data.Add((DeviceData)d);
                        }
                    });
                    threads.Add(thread);
                    thread.Start();
                }
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            return data;
        }

        private static DeviceData? SendArpRequest(IPAddress SrcIp, IPAddress DestIp)
        {
            byte[] macAddr = new byte[6];
            int macAddrLen = macAddr.Length;

            int intSrcIp = BitConverter.ToInt32(SrcIp.GetAddressBytes(), 0);
            int intDestIp = BitConverter.ToInt32(DestIp.GetAddressBytes(), 0);

            if (PacketSender.SendARP(intDestIp, intSrcIp, macAddr, ref macAddrLen) == 0)
            {
                DeviceData data = new DeviceData();
                data.Adapter = SrcIp.ToString();
                data.Address = DestIp.ToString();
                data.Mac = new PhysicalAddress(macAddr).ToString();
                data.Name = Dns.GetHostEntry(DestIp.ToString()).HostName;
                return data;
            }

            return null;
        }

        public List<string> GetActiveAdapters()
        {
            List<string> adapters = new List<string>();

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in interfaces)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();

                foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address.Address))
                    {
                        continue;
                    }

                    string subnetMask = address.IPv4Mask.ToString();
                    if (GetNetworkClass(subnetMask) != NetworkClass.ClassC)
                    {
                        continue;
                    }

                    string ipAddress = address.Address.ToString();

                    adapters.Add(ipAddress);
                }
            }

            return adapters;
        }

        private NetworkClass GetNetworkClass(string subnetMask)
        {
            string[] maskParts = subnetMask.Split('.');

            int counter = 4;
            foreach (string maskByte in maskParts)
            {
                if (maskByte == "255")
                {
                    counter--;
                }
                else
                {
                    break;
                }
            }

            if (counter < 1 || counter > 3)
            {
                throw new Exception("Wrong subnet mask format!");
            }

            return (NetworkClass)counter;
        }

        private IEnumerable<string> GetPossibleIp(string[] ipParts)
        {
            for (int i = 1; i <= 254; i++)
            {
                ipParts[3] = i.ToString();
                yield return string.Join(".", ipParts);
            }
        }
    }

    internal static class PacketSender
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        internal static extern int SendARP(int destinationIp, int sourceIp, byte[] macAddress, ref int physicalAddrLength);
    }
}
