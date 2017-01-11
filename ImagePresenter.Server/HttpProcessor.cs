using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImagePresenter.Server
{
    public class HttpProcessor
    {
        public TcpClient Socket { get; set; }
        public SimpleHttpServerBase Server { get; set; }

        private Stream InputStream { get; set; }
        public Stream OutputStream { get; set; }

        public string HttpMethod { get; set; }
        public string HttpUrl { get; set; }
        public string HttpProtocolVersionString { get; set; }
        public Hashtable HttpHeaders { get; set; }

        public HttpProcessor(TcpClient s, SimpleHttpServerBase srv)
        {
            Socket = s;
            Server = srv;
            HttpHeaders = new Hashtable();
        }
        
        public void Process()
        {
            // Initialize
            InputStream = new BufferedStream(Socket.GetStream());
            OutputStream = new BufferedStream(Socket.GetStream());

            try
            {
                Console.WriteLine("--- --- --- ---");
                Console.WriteLine("Start processing.");
                ParseRequest();
                ReadHeaders();
                HandleGetRequest();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                WriteFailure();
            }

            // Dispose
            OutputStream.Flush();
            Socket.Close();

            Console.WriteLine("Socket closed.");
            Console.WriteLine("--- --- --- ---");
            Console.Write(Environment.NewLine);
        }

        public void ParseRequest()
        {
            Console.WriteLine("--- ParseRequest() ---");

            var request = InputStream.StreamReadLine();
            var tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("Invalid http request line!");
            }
            HttpMethod = tokens[0].ToUpper();
            HttpUrl = tokens[1];
            HttpProtocolVersionString = tokens[2];

            Console.WriteLine($"Starting: {request}");
        }

        public void ReadHeaders()
        {
            Console.WriteLine("--- ReadHeaders() ---");

            var line = string.Empty;
            while ((line = InputStream.StreamReadLine()) != null)
            {
                if (line.Equals(""))
                {
                    Console.WriteLine("Got headers.");
                    return;
                }

                var separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception($"Invalid http header line: {line}");
                }

                var name = line.Substring(0, separator);
                var pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // Strip any spaces
                }

                var value = line.Substring(pos, line.Length - pos);
                Console.WriteLine($"Header: {name}:{value}");
                HttpHeaders[name] = value;
            }
        }

        public void HandleGetRequest()
        {
            Console.WriteLine("--- HandleGetRequest() ---");
            Server.HandleGetRequest(this);
        }

        public void WriteSuccess(string imageExension)
        {
            OutputStream.StreamWriteLine("HTTP/1.0 200 OK");
            OutputStream.StreamWriteLine($"Content-Type: image/{imageExension}");
            OutputStream.StreamWriteLine("Connection: close");
            OutputStream.StreamWriteLine("");
        }

        public void WriteFailure()
        {
            OutputStream.StreamWriteLine("HTTP/1.0 404 File not found");
            OutputStream.StreamWriteLine("Connection: close");
            OutputStream.StreamWriteLine("");
        }
    }

    public static class StreamExtensions
    {
        public static string StreamReadLine(this Stream inputStream)
        {
            var data = string.Empty;
            while (true)
            {
                var nextChar = inputStream.ReadByte();
                if (nextChar == '\n')
                {
                    break;
                }
                if (nextChar == '\r')
                {
                    continue;
                }
                if (nextChar == -1)
                {
                    Thread.Sleep(1);
                    continue;
                }
                data += Convert.ToChar(nextChar);
            }
            return data;
        }

        public static void StreamWriteLine(this Stream outputStream, string output)
        {
            output = output + "\n";
            var bytes = Encoding.UTF8.GetBytes(output);
            outputStream.Write(bytes, 0, bytes.Length);
        }
    }
}
