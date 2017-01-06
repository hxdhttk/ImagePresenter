using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImagePresenter.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleHttpServer httpServer;
            if (args.GetLength(0) > 0)
            {
                httpServer = new SimpleHttpServer(Convert.ToInt16(args[0]));
            }
            else
            {
                httpServer = new SimpleHttpServer(13017);
            }
            var thread = new Thread(new ThreadStart(httpServer.Listen));
            thread.Start();
        }
    }
}
