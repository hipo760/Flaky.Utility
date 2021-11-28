using Flaky.Utility.Connection.Websocket;
using Flaky.Utility.Serilog;
using Ftx.Connector;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Flaky.Utility.Example.NetStandard
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var loggerFactory = MelSerilogInstance.FileLoggerFactory();
            ILogger log = loggerFactory.CreateLogger("Main");// ("Main");


            using (var client = new RxWsClient("wss://ftx.com/ws/", loggerFactory: loggerFactory))
            {
                client.Connect();
                var tick = FtxWsRequest.GetSubscribeRequest("ticker", "BTC-PERP");
                client.RequestObserver.OnNext(tick);

                long count = 0;

                using (var nums = Observable.Interval(TimeSpan.FromMilliseconds(100)).Subscribe(l =>
                {
                    count = l;
                    log.LogTrace("Count {@count}",count);
                }))
                {
                    using (var numTickSub = client.ResponseObservable.Subscribe(s =>
                    {
                        if (count >= 5)
                        {
                            nums.Dispose();
                        }

                    }))
                    {
                        Task.Delay(3000).Wait();
                    }
                }
            }
            log.LogTrace("client is dispose or not ?");
            Console.ReadLine();
        }
    }
}
