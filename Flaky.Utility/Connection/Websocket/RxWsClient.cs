using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flaky.Utility.Connection.Websocket
{
    public class RxWsClient: IDisposable
#if NET5_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private readonly ILogger _log;
        private ClientWebSocket _wsClient;
        private CancellationTokenSource _cts;

        private readonly BehaviorSubject<WebSocketState> _websocketStateObservable;
        private readonly Subject<Exception> _exceptionSubject;
        private readonly Subject<string> _requestSubject;
        private readonly Subject<string> _responseSubject;

        private IDisposable _requestSub;
        private bool disposedValue;

        public IObservable<WebSocketState> WebsocketStateObservable => _websocketStateObservable;
        public IObservable<Exception> ExceptionObservable => _exceptionSubject;
        public IObserver<string> RequestObserver => _requestSubject;
        public IObservable<string> ResponseObservable => _responseSubject;
        public WebSocketState WebSocketState => _wsClient.State;
        public string RemoteUrl { get; set; }
        public int BufferSize { get; }


        public RxWsClient(string remote, int bufferSize = 1024, ILoggerFactory loggerFactory = null) : this(remote, bufferSize)
        {
            _log = loggerFactory != null
                ? loggerFactory.CreateLogger("RxWsClient")
                : NullLogger.Instance;
        }


        //public RxWsClient(string remote, int bufferSize = 1024, ILogger<RxWsClient> log = null) : this(remote, bufferSize)
        //{
        //    if (log == null)
        //    {
        //        _log = NullLogger.Instance;
        //    }
        //    _log = log;
        //}

        public RxWsClient(string remote, int bufferSize = 1024,ILogger log = null):this(remote, bufferSize)
        {
            _log = log ?? NullLogger.Instance;
        }


        public RxWsClient(string remote, int bufferSize = 1024)
        {
            RemoteUrl = remote;
            BufferSize = bufferSize;

            _requestSubject = new Subject<string>();
            _responseSubject = new Subject<string>();
            _websocketStateObservable = new BehaviorSubject<WebSocketState>(WebSocketState.None);
            _exceptionSubject = new Subject<Exception>();
        }

        public void Connect() => ConnectAsync().Wait();

        public async Task ConnectAsync()
        {
            
            _wsClient = new ClientWebSocket();
            var serverUri = new Uri(RemoteUrl);
            _cts = new CancellationTokenSource();
            await _wsClient
                .ConnectAsync(serverUri, _cts.Token)
                

                .ContinueWith(async t =>
                {
#if NET5_0_OR_GREATER
                    if (t.IsCompletedSuccessfully)
                    {
                        _log.LogInformation("Connecting...done");
                        _websocketStateObservable.OnNext(WebSocketState);
                        if (WebSocketState == WebSocketState.Open)
                        {
                            await Echo();
                            return;
                        }
                    }
                    else if (t.IsFaulted || t.IsCanceled || t.IsCompleted)
                    {
                        if (t.Exception != null)
                        {
                            _log.LogError("Exception: {e}", t.Exception);
                            _exceptionSubject.OnNext(t.Exception);
                            return;
                        }
                        _websocketStateObservable.OnNext(WebSocketState);
                        return;
                    }

#elif NETSTANDARD2_0
                    if (t.IsCompleted)
	                {
                        if (WebSocketState == WebSocketState.Open)
                            {
                                await Echo();
                                return;
                            }
	                }
                    else if (t.IsFaulted || t.IsCanceled)
                    {
                        if (t.Exception != null)
                        {
                            _log.LogError("Exception: {e}", t.Exception);
                            _exceptionSubject.OnNext(t.Exception);
                            return;
                        }
                    }
                    _websocketStateObservable.OnNext(WebSocketState);
                    return;
#else
#error This code block does not match csproj TargetFrameworks list
#endif
                });
        }

        public void Close() => CloseAsync().Wait();

        public async Task CloseAsync()
        {
            try
            {
                if (WebSocketState != WebSocketState.Open) return;
                _requestSub?.Dispose();
                await _wsClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token);
                Task.Delay(1000).Wait();
                //_cts.Cancel();
                //await _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }
            catch (Exception e)
            {
                _log.LogError("Exception: {e}", e);
                _exceptionSubject?.OnNext(e);
                return;
            }
        }

        private async Task Send(string request)
        {
            try
            {
                var encoded = Encoding.UTF8.GetBytes(request);
                var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
                await _wsClient.SendAsync(buffer, WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception e)
            {
                _log.LogError("Exception: {e}", e);
                _exceptionSubject.OnNext(e);
                return;
            }
        }

        private async Task Echo()
        {
            _requestSub?.Dispose();
            _requestSub = _requestSubject
                .Where(m => WebSocketState == WebSocketState.Open)
                .Select(m => Observable.FromAsync(() => Send(m)))
                .Concat()
                .Subscribe();
            
            try
            {
                var buffer = new byte[BufferSize];
                var offset = 0;
                var free = buffer.Length;

                while (WebSocketState == WebSocketState.Open || WebSocketState == WebSocketState.CloseSent)
                {
                    var bytesReceived = new ArraySegment<byte>(buffer, offset, free);
                    var result = await _wsClient.ReceiveAsync(bytesReceived, _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        using (_log.BeginScope(new Dictionary<string, object>
                        {
                            ["method"] = nameof(Echo),
                        }))
                        {
                            _log.LogInformation("A receive has completed because a close message was received.");
                        }

                        break;
                    }

                    offset += result.Count;
                    free -= result.Count;

                    if (result.EndOfMessage)
                    {
                        var str = Encoding.UTF8.GetString(buffer, 0, offset);
                        _responseSubject.OnNext(str);
                        buffer = new byte[BufferSize];
                        offset = 0;
                        free = buffer.Length;
                    }

                    if (free != 0) continue;
                    var newSize = buffer.Length + BufferSize;
                    var newBuffer = new byte[newSize];
                    Array.Copy(buffer, 0, newBuffer, 0, offset);
                    buffer = newBuffer;
                    free = buffer.Length - offset;
                }
                _websocketStateObservable.OnNext(WebSocketState);
            }
            catch (Exception e)
            {
                _log.LogError("Exception: {e}", e);
                _log.LogDebug("State: {state}", WebSocketState);
                _websocketStateObservable.OnNext(WebSocketState);
                _exceptionSubject.OnNext(e);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Close();
                    _responseSubject?.OnCompleted();
                    _responseSubject?.Dispose();
                    _websocketStateObservable?.OnCompleted();
                    _websocketStateObservable?.Dispose();
                    _exceptionSubject?.OnCompleted();
                    _exceptionSubject?.Dispose();
                    _requestSubject.OnCompleted();
                    _requestSubject?.Dispose();
                    _cts?.Cancel();
                    _wsClient?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RxWsClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            //GC.SuppressFinalize(this);
            _log.LogDebug("RxWsClient Dispose complete");
        }


#if NET5_0_OR_GREATER
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncInternal(disposing: true);
            //GC.SuppressFinalize(this);
            _log.LogDebug("RxWsClient DisposeAsync complete");
        }

        protected virtual async ValueTask DisposeAsyncInternal(bool disposing)
        {
            // Async cleanup mock
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    await CloseAsync();
                    _responseSubject?.OnCompleted();
                    _responseSubject?.Dispose();
                    _websocketStateObservable?.OnCompleted();
                    _websocketStateObservable?.Dispose();
                    _exceptionSubject?.OnCompleted();
                    _exceptionSubject?.Dispose();
                    _requestSubject.OnCompleted();
                    _requestSubject?.Dispose();
                    _cts?.Cancel();
                    _wsClient.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
#endif
    }
}
