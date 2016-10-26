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

                using (var clientWriter = new StreamWriter(tcpClient.GetStream()))
                {
                    clientWriter.AutoFlush = true;

                    using (var clientReader = new StreamReader(tcpClient.GetStream()))
                    {
                        /* Do handshake */
                        var ircCmd = clientReader.ReadLine();
                        Console.WriteLine("IRC Cmd: {0}", ircCmd);


                        // Client: USRIP
                        // Server: ":s 302  :=+@0.0.0.0\r\n"

                        // Client: USER 'XflsaqOa9X|165580976 127.0.0.1 peerchat.bwgame.xyz :matt
                        // Client: NICK bblahh


                        /* After our handshake is complete proxy them to the IRC server */

                        // make another tcp connection to irc
                        var ircWriter = new StreamWriter(new MemoryStream()); /* fake */
                        var ircReader = new StreamReader(new MemoryStream()); /* fake */

                        var tasks = new[]
                        {
                            HandleClientToIRCData(clientReader, ircWriter, cts.Token),
                            HandleIRCToClientData(ircReader, clientWriter, cts.Token)
                        };

                        await Task.WhenAny(tasks); // if reading or writing to either fails abort
                        cts.Cancel();
                        await Task.WhenAll(tasks); // but wait for the other one first

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

        private async Task HandleClientToIRCData(StreamReader clientReader, StreamWriter ircWriter, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var ircCmd = await clientReader.ReadLineAsync(cancellationToken);
                Console.WriteLine("Client -> IRC: {0}", ircCmd);
                await ircWriter.WriteLineAsync(ircCmd, cancellationToken);
            }
        }

        private async Task HandleIRCToClientData(StreamReader ircReader, StreamWriter clientWriter, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var ircCmd = await ircReader.ReadLineAsync(cancellationToken);
                Console.WriteLine("IRC -> Client: {0}", ircCmd);
                await clientWriter.WriteLineAsync(ircCmd, cancellationToken);
            }
        }
    }
}
