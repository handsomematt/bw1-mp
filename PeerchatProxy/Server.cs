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
                        var tcpClient = await listener.AcceptTcpClientAsync().WithCancellation(cancellationToken);
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
            TcpClient ircClient = null;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(listenerCancellationToken);
            var remoteEndpoint = "Unknown";
            try
            {
                remoteEndpoint = tcpClient.Client.RemoteEndPoint.ToString();
                Console.WriteLine("New client connected: {0}", remoteEndpoint);
                tcpClient.NoDelay = true;

                using (var clientWriter = new StreamWriter(tcpClient.GetStream()))
                {
                    clientWriter.AutoFlush = true;

                    using (var clientReader = new StreamReader(tcpClient.GetStream()))
                    {
                        var ircCmd = await clientReader.ReadLineAsync();
                        if (ircCmd != "USRIP")
                            return; // TODO: reply/drop nicely
                        await clientWriter.WriteLineAsync(":s 302  :=+@0.0.0.0");

                        /* Proxy to our IRC server */
                        ircClient = new TcpClient();
                        await ircClient.ConnectAsync("irc.bwgame.xyz", 6667);

                        using (var ircWriter = new StreamWriter(ircClient.GetStream()))
                        {
                            ircWriter.AutoFlush = true;

                            using (var ircReader = new StreamReader(ircClient.GetStream()))
                            {
                                var tasks = new[]
                                {
                                    HandleClientToIRCData(clientReader, ircWriter, cts.Token),
                                    HandleIRCToClientData(ircReader, clientWriter, cts.Token)
                                };

                                await Task.WhenAny(tasks); // if either fails abort
                                cts.Cancel();
                                await Task.WhenAll(tasks); // but wait for the other one first
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection to {0} aborted due to error: {1}", remoteEndpoint, ex.Message);
            }
            finally
            {
                if (ircClient != null)
                    ircClient.Close();
                tcpClient.Close();
                Console.WriteLine("Client disconnected: {0}", remoteEndpoint);
            }
        }

        private async Task HandleClientToIRCData(StreamReader clientReader, StreamWriter ircWriter, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var ircCmd = await clientReader.ReadLineAsync();
                await ircWriter.WriteLineAsync(ircCmd);
                WriteConsole(ConsoleColor.Green, ircCmd);
            }
        }

        private async Task HandleIRCToClientData(StreamReader ircReader, StreamWriter clientWriter, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var ircCmd = await ircReader.ReadLineAsync();
                await clientWriter.WriteLineAsync(ircCmd);
                WriteConsole(ConsoleColor.Red, ircCmd);
            }
        }

        private void WriteConsole(ConsoleColor bgColor, string format, params object[] parameters)
        {
            var oldbg = Console.BackgroundColor;
            var oldfg = Console.ForegroundColor;

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("[{0}]", DateTime.Now.ToShortTimeString());

            Console.BackgroundColor = bgColor;
            Console.WriteLine(format, parameters);

            Console.BackgroundColor = oldbg;
            Console.ForegroundColor = oldfg;
        }
    }
}
