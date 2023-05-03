using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace IpScanner
{
    public class NetworkAnalyzer
    {
        public NetworkAnalyzer()
        {
        }

        public List<DeviceData> ScanNetwork(List<string> adapters)
        {
            List<DeviceData> data = new List<DeviceData>();
            List<Thread> threads = new List<Thread>();

            foreach (var adapterStr in adapters)
            {
                string[] adapterData = adapterStr.Split(':');

                IPAddress networkIp = IPAddress.Parse(adapterData[0]);

                IPAddress maskIp = GetMaskByPrefix(Convert.ToInt32(adapterData[1]));

                foreach (IPAddress ipaddress in GetPossibleIp(networkIp, maskIp))
                {
                    Thread thread = new Thread(() =>
                    {
                        DeviceData? d = SendArpRequest(networkIp, ipaddress);

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

        private static DeviceData? SendArpRequest(IPAddress networkIp, IPAddress destIp)
        {
            byte[] macAddr = new byte[6];
            int macAddrLen = macAddr.Length;

            int intDestIp = BitConverter.ToInt32(destIp.GetAddressBytes(), 0);

            if (PacketSender.SendARP(intDestIp, 0, macAddr, ref macAddrLen) == 0)
            {
                DeviceData data = new DeviceData();
                data.Adapter = networkIp.ToString();
                data.Address = destIp.ToString();
                data.Mac = new PhysicalAddress(macAddr).ToString();
                try
                {
                    data.Name = Dns.GetHostEntry(destIp.ToString()).HostName;
                }
                catch
                {
                    data.Name = "";
                }

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

                    IPAddress subnetMask = address.IPv4Mask;

                    IPAddress networkAddress = GetNetworkAddress(address.Address, subnetMask);

                    if (GetNetworkClass(subnetMask) != NetworkClass.ClassC)
                    {
                        continue;
                    }

                    int maskPrefix = 24 + Convert.ToString(subnetMask.GetAddressBytes()[3], 2).Count(f => f == '1');
                    adapters.Add($"{networkAddress}:{maskPrefix}");
                }
            }

            return adapters;
        }

        private NetworkClass GetNetworkClass(IPAddress subnetMask)
        {
            string[] maskParts = subnetMask.ToString().Split('.');

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

        private IEnumerable<IPAddress> GetPossibleIp(IPAddress networkIp, IPAddress subnetMask)
        {
            IPAddress broadcastAddress = GetBroadcastAddress(networkIp, subnetMask);

            byte start = (byte)(networkIp.GetAddressBytes()[3] + 1);
            byte end = broadcastAddress.GetAddressBytes()[3];

            byte[] ip = networkIp.GetAddressBytes();

            for (byte i = start; i < end; i++)
            {
                ip[3] = i;
                yield return new IPAddress(ip);
            }
        }

        private IPAddress GetNetworkAddress(IPAddress ipAddress, IPAddress subnetMask)
        {
            byte[] ipAddressBytes = ipAddress.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            byte[] networkAddressBytes = new byte[ipAddressBytes.Length];
            for (int i = 0; i < ipAddressBytes.Length; i++)
            {
                networkAddressBytes[i] = (byte)(ipAddressBytes[i] & subnetMaskBytes[i]);
            }
            return new IPAddress(networkAddressBytes);
        }

        private IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask)
        {
            byte[] ipAddressBytes = ipAddress.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            byte[] broadcastAddressBytes = new byte[ipAddressBytes.Length];
            for (int i = 0; i < ipAddressBytes.Length; i++)
            {
                broadcastAddressBytes[i] = (byte)(ipAddressBytes[i] | ~subnetMaskBytes[i]);
            }
            return new IPAddress(broadcastAddressBytes);
        }

        private IPAddress GetMaskByPrefix(int prefix)
        {
            byte[] bytes = new byte[4];
            for (int i = 0; i < bytes.Length; i++)
            {
                if (prefix >= 8)
                {
                    bytes[i] = 255;
                    prefix -= 8;
                }
                else if (prefix > 0)
                {
                    bytes[i] = (byte)(255 << (8 - prefix));
                    prefix = 0;
                }
            }
            return new IPAddress(bytes);
        }
    }

    internal static class PacketSender
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        internal static extern int SendARP(int destinationIp, int sourceIp, byte[] macAddress, ref int physicalAddrLength);
    }
}
