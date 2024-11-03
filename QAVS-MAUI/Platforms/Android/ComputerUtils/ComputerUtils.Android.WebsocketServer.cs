using System;
using Fleck;
using Java.Net;

namespace ComputerUtils.Webserver
{
    public class WebsocketServer
    {
        public WebSocketServer? server = null;
        public Action<IWebSocketConnection>? OnOpen = null;
        public Action<IWebSocketConnection>? OnClose = null;
        public Action<IWebSocketConnection, string>? OnMessage = null;
        public void StartServer(int port)
        {
            server = new WebSocketServer($"ws://0.0.0.0:{port}", true);
            server.ListenerSocket.NoDelay = true;
            server.RestartAfterListenError = true;
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    OnOpen.Invoke(socket);
                };
                socket.OnClose = () =>
                {
                    OnClose.Invoke(socket);
                };
                socket.OnMessage = msg =>
                {
                    OnMessage.Invoke(socket, msg);
                };
            });
        }
    }
}