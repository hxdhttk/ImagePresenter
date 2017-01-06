using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImagePresenter.Server
{
    public class SimpleHttpServer : SimpleHttpServerBase
    {
        public SimpleHttpServer(int port) : base(port)
        {
        }

        public override void HandleGetRequest(HttpProcessor p)
        {
            Console.WriteLine($"Request: {p.HttpUrl}");
            p.WriteSuccess();
            p.OutputStream.WriteLine("<html><body><h1>test server</h1>");
            p.OutputStream.WriteLine("Current Time: " + DateTime.Now);
            p.OutputStream.WriteLine($"url : {p.HttpUrl}");

            p.OutputStream.WriteLine("<form method=post action=/form>");
            p.OutputStream.WriteLine("<input type=text name=foo value=foovalue>");
            p.OutputStream.WriteLine("<input type=submit name=bar value=barvalue>");
            p.OutputStream.WriteLine("</form>");
        }

        public override void HandlePostRequest(HttpProcessor p, StreamReader inputData)
        {
            Console.WriteLine($"POST request: {p.HttpUrl}");
            var data = inputData.ReadToEnd();

            p.OutputStream.WriteLine("<html><body><h1>test server</h1>");
            p.OutputStream.WriteLine("<a href=/test>return</a><p>");
            p.OutputStream.WriteLine("postbody: <pre>{0}</pre>", data);
        }
    }
}
