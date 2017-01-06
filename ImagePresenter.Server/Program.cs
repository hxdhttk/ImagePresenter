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
        private static readonly string ServerSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), "ServerSettings.json");

        static void Main(string[] args)
        {
            var settings = JsonConvert.DeserializeObject<ServerSettings>(ServerSettingsFile);
            if(settings == null) return;

            var httpServer = new SimpleHttpServer(settings.Port);
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
