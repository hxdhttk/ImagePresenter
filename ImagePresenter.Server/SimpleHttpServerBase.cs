using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImagePresenter.Server
{
    //Loopback only
    public abstract class SimpleHttpServerBase
    {
        protected int Port { get; set; }
        private TcpListener Listener { get; set; }
        private bool IsActive { get; set; }

        public SimpleHttpServerBase(int port)
        {
            Port = port;
            IsActive = true;
        }

        public void Listen()
        {
            Listener = new TcpListener(IPAddress.Loopback, Port);
            Listener.Start();
            while (IsActive)
            {
                var s = Listener.AcceptTcpClient();
                var processor = new HttpProcessor(s, this);
                var thread = new Thread(new ThreadStart(processor.Process));
                thread.Start();

                Thread.Sleep(1);
            }
        }

        public abstract void HandleGetRequest(HttpProcessor p);
    }
}
