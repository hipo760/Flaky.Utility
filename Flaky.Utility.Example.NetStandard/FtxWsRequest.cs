using System;
using System.Security.Cryptography;
using System.Text;

namespace Ftx.Connector
{
    public static class FtxWsRequest
    {

        public static string GetSubscribeRequest(string channel) 
            => $"{{\"op\": \"subscribe\", \"channel\": \"{channel}\"}}";

        public static string GetUnsubscribeRequest(string channel) 
            => $"{{\"op\": \"unsubscribe\", \"channel\": \"{channel}\"}}";

        public static string GetSubscribeRequest(string channel, string instrument) 
            => $"{{\"op\": \"subscribe\", \"channel\": \"{channel}\", \"market\": \"{instrument}\"}}";

        public static string GetUnsubscribeRequest(string channel, string instrument) 
            => $"{{\"op\": \"unsubscribe\", \"channel\": \"{channel}\", \"market\": \"{instrument}\"}}";
    }
}