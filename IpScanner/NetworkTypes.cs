using System.Net.NetworkInformation;
using System.Net;

namespace IpScanner
{
    public enum NetworkClass
    {
        ClassA = 3,
        ClassB = 2,
        ClassC = 1,
    }

    public struct DeviceData
    {
        public string Adapter;
        public string Address;
        public string Mac;
        public string Name;
    }
}
