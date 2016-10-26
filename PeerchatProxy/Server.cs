using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerchatProxy
{
    public class Server
    {
        private readonly int _listenerPort;

        public Server(int listenerPort)
        {
            _listenerPort = listenerPort;
        }

        public Task Start(CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting TCP listener on port {0}", _listenerPort);

            var listener = new TcpListener(IPAddress.Any, _listenerPort);
            listener.Start();

            return Task.Run(async () =>
            {
                var clients = new List<Task>();
                try
                {
                    /* Accept new clients until this task is cancelled */
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var tcpClient = await listener.AcceptTcpClientAsync().ContinueWith(t => t.Result, cancellationToken);
                        clients.RemoveAll(task => task.IsCompleted);
                        clients.Add(HandleSingleClient(tcpClient, cancellationToken));
                    }
                }
                finally
                {
                    /* Make sure all our client tasks end properly */
                    await Task.WhenAll(clients.ToArray());
                }
            }, cancellationToken);
        }

        private async Task HandleSingleClient(TcpClient tcpClient, CancellationToken listenerCancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(listenerCancellationToken);
            var remoteEndpoint = "Unknown";
            try
            {
                remoteEndpoint = tcpClient.Client.RemoteEndPoint.ToString();
                Console.WriteLine("New client connected: {0}", remoteEndpoint);
                tcpClient.NoDelay = true;

                using (var writer = new StreamWriter(tcpClient.GetStream()))
                {
                    writer.AutoFlush = true;

                    using (var reader = new StreamReader(tcpClient.GetStream()))
                    {
                        var ircCmd = reader.ReadLine();
                        Console.WriteLine("IRC Cmd: {0}", ircCmd);
                        // Client: USRIP
                        // Server: ":s 302  :=+@0.0.0.0\r\n"

                        // Client: USER 'XflsaqOa9X|165580976 127.0.0.1 peerchat.bwgame.xyz :matt
                        // Client: NICK bblahh

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection to {0} aborted due to error: {1}", remoteEndpoint, ex.Message);
            }
            finally
            {
                tcpClient.Close();
            }
        }
    }
}
