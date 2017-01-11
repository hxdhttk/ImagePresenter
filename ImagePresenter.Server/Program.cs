using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImagePresenter.Server
{
    class Program
    {
        internal static readonly string ServerSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), "ServerSettings.json");

        internal static ServerSettings ServerSettings
        {
            get
            {
                ServerSettings settings;
                using (var sr = new StreamReader(ServerSettingsFile))
                {
                    var str = sr.ReadToEnd();
                    settings = JsonConvert.DeserializeObject<ServerSettings>(str);
                }

                if (settings == null)
                {
                    throw new InvalidOperationException("Bad setting file!");
                }

                return settings;
            }
        }

        static void Main(string[] args)
        {
            var httpServer = new SimpleHttpServer(ServerSettings.Port);
            var thread = new Thread(httpServer.Listen);
            thread.Start();
        }      
    }

    public class ServerSettings
    {
        public int Port { get; set; }

        public string ImagePath { get; set; }
    }
}
