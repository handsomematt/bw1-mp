using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*
1:49 PM ‑ Rohan: also ur class should create the CancellationTokenSource
1:49 PM ‑ Rohan: private
1:49 PM ‑ Rohan: and make it disposable or something
1:49 PM ‑ Rohan: dispose should cancel + close socket
1:49 PM ‑ Rohan: THAT will stop everything
*/

namespace GamespyMaster
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
                        var tcpClient = await listener.AcceptTcpClientAsync();
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

                var stream = tcpClient.GetStream();

                var reply = Encoding.ASCII.GetBytes("\\basic\\\\secure\\TXKOAT");
                await stream.WriteAsync(reply, 0, reply.Length, cts.Token);

                byte[] packet = new byte[4096];
                var readBytes = await stream.ReadAsync(packet, 0, 4096);
                Console.WriteLine("Read: {0}", Encoding.ASCII.GetString(packet, 0, readBytes));

                var serverlist = new MemoryStream();
                var writer = new BinaryWriter(serverlist);

                writer.Write((byte)127);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)1);
                writer.Write((short)6500); // UDP UDP UDP UDP UDP UDP
                writer.Write(Encoding.ASCII.GetBytes("\\final\\"));

                var reply2 = serverlist.ToArray();
                Console.WriteLine("Reply: {0}", Encoding.ASCII.GetString(reply2));

                await stream.WriteAsync(reply2, 0, reply2.Length, cts.Token);

                // client sends 2:
                // CLIENT: \gamename\bandw\gamever\1.6\location\0\validate\ILIiWIhp\final\\queryid\1.1\
                // CLIENT: \list\cmp\gamename\bandw\where\groupid is null\final\

                // for each server:
                // put: b b b b address
                // put: s port & 0xFFFF

                // SERVER: \final\
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
        
    }
}
