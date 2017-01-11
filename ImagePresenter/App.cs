using System;
using Bridge;
using Bridge.Html5;

namespace ImagePresenter
{
    public class App
    {
        public static void Main()
        {
            var img = new HTMLImageElement
            {
                Id = "bg",
                Width = Window.InnerWidth,
                Height = Window.InnerHeight,
                Src = "http://127.0.0.1:13017/"
            };

            Document.Body.AppendChild(img);
        }
    }
}