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
        public StreamWriter OutputStream { get; set; }

        public string HttpMethod { get; set; }
        public string HttpUrl { get; set; }
        public string HttpProtocolVersionString { get; set; }
        public Hashtable HttpHeaders;
        
        private const int MaxPostSize = 10 * 1024 * 1024; // 10MB
        private const int BufSize = 4096;

        public HttpProcessor(TcpClient s, SimpleHttpServerBase srv)
        {
            Socket = s;
            Server = srv;

            HttpHeaders = new Hashtable();
        }

        private string StreamReadLine(Stream inputStream)
        {
            var nextChar = 0;
            var data = string.Empty;
            while (true)
            {
                nextChar = inputStream.ReadByte();
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

        public void Process()
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            InputStream = new BufferedStream(Socket.GetStream());
            // we probably shouldn't be using a streamwriter for all output from handlers either
            OutputStream = new StreamWriter(new BufferedStream(Socket.GetStream()));

            try
            {
                ParseRequest();
                ReadHeaders();
                if (HttpMethod.Equals("GET"))
                {
                    HandleGetRequest();
                }
                else if (HttpMethod.Equals("POST"))
                {
                    HandlePostRequest();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                WriteFailure();
            }

            OutputStream.Flush();

            Socket.Close();
        }

        public void ParseRequest()
        {
            var request = StreamReadLine(InputStream);
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
            Console.WriteLine("ReadHeaders()");

            var line = string.Empty;
            while ((line = StreamReadLine(InputStream)) != null)
            {
                if (line.Equals(""))
                {
                    Console.WriteLine("Got headers");
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
                    pos++; // strip any spaces
                }

                var value = line.Substring(pos, line.Length - pos);
                Console.WriteLine($"Header: {name}:{value}");
                HttpHeaders[name] = value;
            }
        }

        public void HandleGetRequest()
        {
            Server.HandleGetRequest(this);
        }
        
        public void HandlePostRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 
            Console.WriteLine("Get post data start");
            var contentLength = 0;
            var ms = new MemoryStream();
            if (HttpHeaders.ContainsKey("Content-Length"))
            {
                contentLength = Convert.ToInt32(HttpHeaders["Content-Length"]);
                if (contentLength > MaxPostSize)
                {
                    throw new Exception(
                        $"POST Content-Length({contentLength}) too big for this simple server");
                }
                var buf = new byte[BufSize];
                var toRead = contentLength;
                while (toRead > 0)
                {
                    Console.WriteLine($"Starting Read, to_read={toRead}");

                    var numread = InputStream.Read(buf, 0, Math.Min(BufSize, toRead));
                    Console.WriteLine($"Read finished, numread={numread}");
                    if (numread == 0)
                    {
                        if (toRead == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("Client disconnected during post");
                        }
                    }
                    toRead -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            Console.WriteLine("Get post data end");
            Server.HandlePostRequest(this, new StreamReader(ms));
        }

        public void WriteSuccess()
        {
            OutputStream.WriteLine("HTTP/1.0 200 OK");
            OutputStream.WriteLine("Content-Type: text/html");
            OutputStream.WriteLine("Connection: close");
            OutputStream.WriteLine("");
        }

        public void WriteFailure()
        {
            OutputStream.WriteLine("HTTP/1.0 404 File not found");
            OutputStream.WriteLine("Connection: close");
            OutputStream.WriteLine("");
        }
    }
}
