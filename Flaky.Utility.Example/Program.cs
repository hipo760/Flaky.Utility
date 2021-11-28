// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Flaky.Utility.Connection.Websocket;
using Flaky.Utility.Serilog;
using Ftx.Connector;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

public class ShortIdBenchmarks
{
    [Benchmark]
    public string GetShortId() => Flaky.Utility.Tool.Uid.ShortId(6);

}

public class Md5VsSha256
{
    private const int N = 10000;
    private readonly byte[] data;

    private readonly SHA256 sha256 = SHA256.Create();
    private readonly MD5 md5 = MD5.Create();

    public Md5VsSha256()
    {
        data = new byte[N];
        new Random(42).NextBytes(data);
    }

    [Benchmark]
    public byte[] Sha256() => sha256.ComputeHash(data);

    [Benchmark]
    public byte[] Md5() => md5.ComputeHash(data);
}


namespace Flaky.Utility.Example
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var loggerFactory = MelSerilogInstance.ConsoleLoggerFactory();
            ILogger log = loggerFactory.CreateLogger("Main");

            await using (var client = new RxWsClient(remote: "wss://ftx.com/ws/", loggerFactory: loggerFactory))
            {
                client.Connect();
                var tick = FtxWsRequest.GetSubscribeRequest("ticker", "BTC-PERP");
                client.RequestObserver.OnNext(tick);

                long count = 0;

                using var nums = Observable.Interval(TimeSpan.FromMilliseconds(100)).Take(5).Subscribe(l =>
                {
                    count = l;
                    log.LogTrace("Count {count}", count);
                });

                using var ticks = client.ResponseObservable.Subscribe(s =>
                {
                    if (count >= 5)
                    {
                        nums.Dispose();
                    }
                });
                Task.Delay(3000).Wait();
            }
            log.LogTrace("client is dispose or not ?");
            Console.ReadLine();
        }
    }
}

