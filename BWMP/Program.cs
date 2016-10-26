using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using System.Runtime.InteropServices;

/*

      USER:
      #'XflsaqOa9X|165580976' is encodedIp|GSProfileId aka persona, '127.0.0.1', 'peerchat.gamespy.com', 'a69b3a7a0837fdcd763fdeb0456e77cb' is cdkey
      user, ip, host, self.cdKeyHash = params

    NICK BNW_536871013
    USER X14saFv19|536871013 127.0.0.1 peerchat.bwgame.xyz :dick

*/

namespace BWMP
{
    public struct UserAccount
    {
        public uint UID;
        public string UserName;
        public string UserPass;

        public UserAccount(uint id, string name, string pass)
        {
            UID = id;
            UserName = name;
            UserPass = pass;
        }
    }

    class Program
    {
        public static List<UserAccount> UserAccounts = new List<UserAccount>()
        {
            new UserAccount(100, "cunt", "cunt"),
            new UserAccount(101, "dick", "fuck")
        };

        static void Main(string[] args)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            HttpListener listener = new HttpListener();

            // listener.Prefixes.Add("http://login.bwgame.xyz:9280/");
            // listener.Prefixes.Add("http://db.bwgame.xyz:9280/");
            listener.Prefixes.Add("http://*:80/");
            listener.Prefixes.Add("http://*:9280/");
            listener.Start();

            Console.WriteLine("BWGame Server Listening...");

            try
            {
                while (listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        var ctx = c as HttpListenerContext;
                        try
                        {
                            HandleRequest(ctx.Request, ctx.Response);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception in queue: {0}", ex.Message);
                        }
                        finally
                        {
                            ctx.Response.OutputStream.Close();
                        }
                    }, listener.GetContext());
                }
            }
            catch (Exception ex) {
                Console.WriteLine("Exception whilst listening: {0}", ex.Message);
            }
        }

        static void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (request.UserHostName)
            {
                case "login.bwgame.xyz":
                    HandleLoginServerRequest(request, response);
                    break;
                case "db.bwgame.xyz":
                    HandleDBServerRequest(request, response);
                    break;
                case "storage.bwgame.xyz":
                    HandleStorageServerRequest(request, response);
                    break;
                default:
                    Console.WriteLine("Request made to unknown host: {0}", request.UserHostName);
                    break;
            }
        }

        static void HandleLoginServerRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.Url.AbsolutePath == "/login/")
            {
                var username = request.QueryString["username"];
                var password = request.QueryString["userpassword"];

                Console.WriteLine("Login request {1}:{2} from {0}", request.RemoteEndPoint.Address.ToString(), request.QueryString["username"], request.QueryString["userpassword"]);

                foreach (UserAccount user in UserAccounts)
                {
                    if (username == user.UserName && password == user.UserPass)
                    {
                        var uid = user.UID;

                        string rstr = string.Format("bnwuserid:{0} 1 1 hello", uid);

                        byte[] buf = Encoding.UTF8.GetBytes(rstr);
                        response.ContentLength64 = buf.Length;
                        response.OutputStream.Write(buf, 0, buf.Length);

                        return;
                    }
                }
                
                byte[] invalidBuf = Encoding.UTF8.GetBytes("Invalid User");
                response.ContentLength64 = invalidBuf.Length;
                response.OutputStream.Write(invalidBuf, 0, invalidBuf.Length);

                return;
            }

            Console.WriteLine("Unknown request made to login server {0}", request.RawUrl);
        }

        static void HandleStorageServerRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            Console.WriteLine("Requested " + request.RawUrl + " from the storage server");

            var filePath = request.Url.AbsolutePath.TrimStart('/');

            if (!File.Exists(filePath))
            {
                response.StatusCode = 404;
                response.StatusDescription = "404 Not Found";

                Console.WriteLine("404: Could not find: {0}", filePath);
                return;
            }

            using (FileStream fs = File.OpenRead(filePath))
            {
                response.ContentLength64 = fs.Length;
                response.SendChunked = false;

                byte[] buffer = new byte[64 * 1024];
                int read;

                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    response.OutputStream.Write(buffer, 0, read);
                    response.OutputStream.Flush();
                }

            }
        }

        static void HandleDBServerRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.Url.AbsolutePath == "/query/")
            {
                byte[] data = new byte[request.ContentLength64];
                request.InputStream.Read(data, 0, (int)request.ContentLength64);

                string querystring = Encoding.UTF8.GetString(data);
                NameValueCollection qscoll = HttpUtility.ParseQueryString(querystring);

                // domode, dbflags, bwversion, bwlanguage, uid, uname, upass, query
                // dbflags = strlen(queryNamePlainText);

                Lionhead.ResetMagicSecret();
                var queryPlain = Lionhead.DeobsufucateAsString(Lionhead.LHWebDecode(qscoll["query"]), Convert.ToInt32(qscoll["dbflags"]));

                Console.WriteLine("Query {3} from: {0} {1} {2}", qscoll["uid"], qscoll["uname"], qscoll["upass"], queryPlain);

                string returnString = "";

                switch(queryPlain)
                {
                    case "BWMAPS_GETLIST":
                        var mapList = new string[4, 6]
                            {
                                /* ID, Name,                                    Map         Players    ?       ?   */
                                { "1", "Bombardment - 2 players",               "mpm_2p_1", "2",    "text", "texty" }, /*  Default - hardcoded ID */
                                { "2", "King of the hill - 3 players",          "mpm_3p_1", "3",    "text", "texty" }, /*  Default - hardcoded ID */
                                { "3", "The four corners of Eden - 4 players",  "mpm_4p_1", "4",    "text", "texty" }, /*  Default - hardcoded ID */
                                { "4", "Shit Map",                              "mpm_shit", "2",    "text", "texty" }
                            };

                        returnString = Lionhead.ConstructDBTableString(mapList);
                        Console.WriteLine(returnString);

                        break;
                    case "BWGETPEERCHAT":
                        string peerchatServer = "peerchat.bwgame.xyz:6667";
                        returnString = Lionhead.ConstructDBTableString(new string[1, 1] { { peerchatServer } });
                        Console.WriteLine("BWGETPEERCHAT: Sent {0}", peerchatServer);

                        break;
                    default:
                        Console.WriteLine("Unknown Query: {0}", queryPlain);
                        break;
                }
                
                byte[] buf = Encoding.ASCII.GetBytes(returnString);
                response.ContentEncoding = Encoding.ASCII; 
                response.ContentLength64 = buf.Length;
                response.OutputStream.Write(buf, 0, buf.Length);

                return;
            }

            Console.WriteLine("Unknown request made to db server {0}", request.RawUrl);
        }
    }
}
