using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImagePresenter.Server
{
    public class SimpleHttpServer : SimpleHttpServerBase
    {
        public SimpleHttpServer(int port) : base(port)
        {
        }
        
        private List<FileInfo> ImageFileInfos
        {
            get
            {
                return Directory.EnumerateFiles(Program.ServerSettings.ImagePath)
                    .Select(f => new FileInfo(f))
                    .Where(fi => fi.Extension == ".jpg" ||
                                 fi.Extension == ".png" ||
                                 fi.Extension == ".bmp")
                    .ToList();
            }
        }

        public override void HandleGetRequest(HttpProcessor p)
        {
            var r = new Random();
            var fi = ImageFileInfos[r.Next(0, ImageFileInfos.Count)];
            using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine($"Request: {p.HttpUrl}");
                switch (fi.Extension)
                {
                    case ".jpg":
                        p.WriteSuccess("jpeg");
                        break;
                    case ".png":
                        p.WriteSuccess("png");
                        break;
                    case ".bmp":
                        p.WriteSuccess("bmp");
                        break;
                    default:
                        throw new Exception("Reached unreachable path!");
                }               
                byte[] imgBytes = new byte[fs.Length];
                using (var br = new BinaryReader(fs))
                {
                    imgBytes = br.ReadBytes(Convert.ToInt32(fs.Length));
                    p.OutputStream.Write(imgBytes, 0, imgBytes.Length);
                }

                Console.WriteLine($"Image:{fi.Name} Size:{imgBytes.Length}bytes sent.");
            }
        }
    }
}
