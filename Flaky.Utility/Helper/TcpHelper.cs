using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GtaTick.Crypto
{
    public static class TcpHelper
    {
        public static TcpState GetState(this TcpClient tcpClient)
        {
            var foo =
                IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpConnections()
                    .SingleOrDefault(x =>
                        x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                        && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)
                    );
            return foo?.State ?? TcpState.Unknown;
        }
    }
}