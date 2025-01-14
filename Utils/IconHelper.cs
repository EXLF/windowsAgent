using System;
using System.Drawing;
using System.Net.Http;
using System.IO;

namespace AIAssistant.Utils
{
    public static class IconHelper
    {
        public static Icon CreateIconFromPng(string pngUrl)
        {
            using (var client = new HttpClient())
            {
                var bytes = client.GetByteArrayAsync(pngUrl).Result;
                using (var ms = new MemoryStream(bytes))
                {
                    var bitmap = new Bitmap(ms);
                    return Icon.FromHandle(bitmap.GetHicon());
                }
            }
        }
    }
} 