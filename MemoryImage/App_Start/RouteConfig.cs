using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MemoryImage
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.Add(new Route("images/{*ImageName}",
                new CustomPNGRouteHandler()));

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }

    public class CustomPNGRouteHandler : IRouteHandler
    {
        public System.Web.IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new CustomPNGHandler(requestContext);
        }
    }

    public class CustomPNGHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }
        protected RequestContext RequestContext { get; set; }

        public CustomPNGHandler() : base() { }

        public CustomPNGHandler(RequestContext requestContext)
        {
            this.RequestContext = requestContext;
        }

        public void ProcessRequest(HttpContext context)
        {
            //using (Bitmap image = new Bitmap("c:\\" +RequestContext.RouteData.Values["ImageName"]))    
#region 1.Kısım       
            string filePath = string.Empty;

            //Gerçek resim ismi bulunur. Eğer başında Seo amaçtı girilen title var ise kaldırılır.
            //Örnek: http://localhost:11590/images/cdn/seo-amacli-deneme-resmi-bear.jpg?w=600&h=400
            string fullFileName = RequestContext.RouteData.Values["ImageName"].ToString();

            int startPoint = fullFileName.LastIndexOf('-') + 1;
            if (startPoint > 0)
            {
                int length = fullFileName.Length - startPoint;

                string RealFileName = fullFileName.Substring(startPoint, length);

                string imagePath = fullFileName.Substring(0, fullFileName.LastIndexOf('/') + 1);
#endregion
#region 2.Kısım
                if (File.Exists("c:\\" + imagePath + RealFileName))
                {
                    filePath = "c:\\" + imagePath + RealFileName;
                }
                else
                {
                    filePath = "c:\\default/default.jpg";
                }
            }
            else //If Not SEO Title Exist
            {
                if (File.Exists("c:\\" + RequestContext.RouteData.Values["ImageName"]))
                {
                    filePath = "c:\\" + RequestContext.RouteData.Values["ImageName"];
                }
                else
                {
                    filePath = "c:\\default/default.jpg";
                }
            }
#endregion
#region 3.Kısım
            using (var image = new Bitmap(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                context.Response.ContentType = string.Concat("image/", fileInfo.Extension.Replace(".", ""));

                var urlParams = context.Request.Url.Query;
                //1.Yol
                int[] numbers = (from Match m in Regex.Matches(urlParams, @"\d+") select int.Parse(m.Value)).ToArray();
                //2.Yol
                /*int width = int.Parse(new string(param.SkipWhile(x => !char.IsDigit(x))
                             .TakeWhile(char.IsDigit).ToArray()));*/
                if (numbers.Length > 1 && (numbers[0] > 0 || numbers[1] > 0))
                {
                    int width = numbers[0];
                    int height = numbers[1];
                    ResizeImage(image, new Size(width, height)).Save(context.Response.OutputStream, image.RawFormat);
                }
                else if (numbers.Length == 1 && numbers[0] > 0)
                {
                    int width = urlParams.Contains("w=") ? numbers[0] : 0;
                    int height = urlParams.Contains("h=") ? numbers[0] : 0;
                    ResizeImage(image, new Size(width, height)).Save(context.Response.OutputStream, image.RawFormat);
                }
                else
                {
                    image.Save(context.Response.OutputStream, ImageFormat.Png);
                }
            }
#endregion
        }

        public Bitmap ResizeImage(Bitmap imgToResize, Size size)
        {
            try
            {
                //Image'in Aspect Ratio Oranını Bul
                double dblRatio = (double)imgToResize.Width / (double)imgToResize.Height;

                //1-)Gelen Width ve Height Değerlerinden "0" Olan Var ise, buna karşılık gelen değer, Image'in Aspect Ratio Oranına Göre Bulunur.
                size.Width = size.Width == 0 ? (int)(size.Height * dblRatio) : size.Width;
                size.Height = size.Height == 0 ? (int)(size.Width / dblRatio) : size.Height;

                //2-) Eğer olması istenen boyutlardan biri, image'in gerçek boyutundan büyük ise, image'in orjinal boyutuna göre işleme devam edilir.
                size.Width = size.Width > imgToResize.Width ? imgToResize.Width : size.Width;
                size.Height = size.Height > imgToResize.Height ? imgToResize.Height : size.Height;

                //3-) Eğer belirtilen yeni boyutlar image'in yeni aspect ratio'suna uymuyor ise, büyük olan boyut sabit alınarak, diğeri aspect ratio buna göre bulunur.
                double dblResizeRatio = (double)size.Width / (double)size.Height;
                if (Math.Abs(dblResizeRatio - dblRatio) > 0.01)
                {
                    //Find bigger Size and Resize the Other One with keeping aspect ratio.
                    if (size.Width > size.Height)
                    {
                        size.Height = (int)(size.Width / dblRatio);
                    }
                    else
                    {
                        size.Width = (int)(size.Height * dblRatio);
                    }
                }

                Bitmap b = new Bitmap(size.Width, size.Height);
                using (Graphics g = Graphics.FromImage((Image)b))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
                }
                return b;
            }
            catch
            {
                Console.WriteLine("Bitmap could not be resized");
                return imgToResize;
            }
        }
    }
}


