// See https://aka.ms/new-console-template for more information

using Flaky.Utility.Connection.Websocket;
using Ftx.Connector;

var id = Flaky.Utility.Tool.Uid.ShortId(6);
Console.WriteLine(id);


var wsClient = new RxWsClient("wss://ftx.com/ws/");

wsClient.ResponseObservable.Subscribe(s => Console.WriteLine(s));
wsClient.WebsocketStateObservable.Subscribe(s => Console.WriteLine(s));
wsClient.Connect();

var tick = FtxWsRequest.GetSubscribeRequest("ticker", "BTC-PERP");
wsClient.RequestObserver.OnNext(tick);



Console.ReadLine();
wsClient.Close();
Console.ReadLine();