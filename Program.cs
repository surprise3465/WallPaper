using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using D2D = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using Newtonsoft.Json.Linq;
using SharpDX.Direct2D1;
using SharpDX.IO;
using WIC = SharpDX.WIC;
using SharpDX;
using Microsoft.Win32;
using System.Linq;
using System.Numerics;
using SharpDX.Mathematics.Interop;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace WallPaper
{
    class Program
    {
        public static HttpClient http = new HttpClient();


        private static readonly Translate Translator = new Translate();
        public static string GenerateWallpaper(string pictureFileName, string english, string chinese, string weather )
        {
            var wic = new WIC.ImagingFactory2();
            var d2d = new D2D.Factory();
            float dpi = d2d.DesktopDpi.Width;

            Size2 size = new Size2(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            WIC.FormatConverter image = CreateWicImage(wic, pictureFileName);
            using (var wicBitmap = new WIC.Bitmap(wic, size.Width, size.Height, WIC.PixelFormat.Format32bppPBGRA, WIC.BitmapCreateCacheOption.CacheOnDemand))
            using (var target = new D2D.WicRenderTarget(d2d, wicBitmap, new D2D.RenderTargetProperties()))
            using (var dc = target.QueryInterface<D2D.DeviceContext>())
            using (var bmpPicture = Bitmap.FromWicBitmap(target, image))
            using (var dwriteFactory = new SharpDX.DirectWrite.Factory())
            using (var brush = new SolidColorBrush(target, new RawColor4(255, 255, 0, 1)))
            using (var bmpLayer = new D2D.Bitmap1(dc, target.PixelSize,
                new D2D.BitmapProperties1(new D2D.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied), dpi, dpi, BitmapOptions.Target)))
            {
                var oldTarget = dc.Target;
                dc.Target = bmpLayer;
                target.BeginDraw();
                {
                    var textFormat = new DWrite.TextFormat(dwriteFactory, "Tahoma", size.Height / 27);

                    // draw English
                    {
                        var textLayout = new DWrite.TextLayout(dwriteFactory, english, textFormat, target.Size.Width * 0.75f, float.MaxValue);
                        var center = new Vector2((target.Size.Width - textLayout.Metrics.Width) / 2, (target.Size.Height - textLayout.Metrics.Height) / 2);
                        target.DrawTextLayout(new RawVector2(center.X, center.Y), textLayout, brush);
                    }
                    {

                        var textLayout = new DWrite.TextLayout(dwriteFactory, chinese, textFormat, target.Size.Width * 0.75f, float.MaxValue);
                        var center = new Vector2((target.Size.Width - textLayout.Metrics.Width) / 2, target.Size.Height - textLayout.Metrics.Height - size.Height / 18);
                        target.DrawTextLayout(new RawVector2(center.X, center.Y), textLayout, brush);
                    }
                    {

                        var textLayout = new DWrite.TextLayout(dwriteFactory, weather, textFormat, target.Size.Width * 0.75f, float.MaxValue);
                        var center = new Vector2((target.Size.Width - textLayout.Metrics.Width) / 2, size.Height / 18 + textLayout.Metrics.Height);
                        target.DrawTextLayout(new RawVector2(center.X, center.Y), textLayout, brush);
                    }

                }
                target.EndDraw();

                // shadow
                var shadow = new D2D.Effects.Shadow(dc);
                shadow.SetInput(0, bmpLayer, new RawBool(false));

                dc.Target = oldTarget;
                target.BeginDraw();
                {
                    target.DrawBitmap(bmpPicture, new SharpDX.Mathematics.Interop.RawRectangleF(0, 0, target.Size.Width, target.Size.Height), 1.0f, BitmapInterpolationMode.Linear);
                    dc.DrawImage(shadow, new RawVector2(size.Height / 150.0f, size.Height / 150.0f));
                    dc.UnitMode = UnitMode.Pixels;
                    target.DrawBitmap(bmpLayer, 1.0f, BitmapInterpolationMode.Linear);
                }
                target.EndDraw();

                string wallpaperFileName = Path.GetTempPath() + "wallpaper.png";
                using (var wallpaperStream = File.OpenWrite(wallpaperFileName))
                {
                    SaveD2DBitmap(wic, wicBitmap, wallpaperStream);
                    wallpaperStream.Close();
                    return wallpaperFileName;
                }
            }
        }

        public static async Task<string> GetQuote()
        {
            var url = @"https://favqs.com/api/qotd";
            var content = await http.GetStringAsync(url);
            var json = JToken.Parse(content);
            return json["quote"]["body"] + "——" + json["quote"]["author"];
        }
        public static async Task<string> GetWeather()
        {
            string weather;
            var url = @"http://t.weather.sojson.com/api/weather/city/101070201";
            var content = await http.GetStringAsync(url);
            var WeatherMessage = JsonConvert.DeserializeObject<JsonInfo>(content);
            weather = WeatherMessage.cityinfo.city + " 当前温度 " + WeatherMessage.data.wendu + "℃ ";
            weather += WeatherMessage.data.forecast[0].low + " " + WeatherMessage.data.forecast[0].high + " ";
            weather += WeatherMessage.data.forecast[0].type + "\r\n";
            weather += WeatherMessage.time.ToString();
            return weather;
        }

        public static async Task<string> GetBingPicture()
        {
            var url = @"https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=5&mkt=zh-cn";
            var content = await http.GetStringAsync(url);
            var json = JToken.Parse(content);
            var images = json["images"]
                .Select(x => x["url"].ToString())
                .Select(x => "https://cn.bing.com" + x);
            return images.FirstOrDefault();
        }

        public static async Task<string> DownloadUrlAsFileName(string url)
        {
            var fileName = Path.GetTempFileName();
            File.WriteAllBytes(fileName, await http.GetByteArrayAsync(url));
            return fileName;
        }

        public static WIC.FormatConverter CreateWicImage(WIC.ImagingFactory wicFactory, string filename)
        {
            using (var decoder = new WIC.JpegBitmapDecoder(wicFactory))
            using (var decodeStream = new WIC.WICStream(wicFactory, filename, NativeFileAccess.Read))
            {
                decoder.Initialize(decodeStream, WIC.DecodeOptions.CacheOnLoad);
                using (var decodeFrame = decoder.GetFrame(0))
                {
                    var converter = new WIC.FormatConverter(wicFactory);
                    converter.Initialize(decodeFrame, WIC.PixelFormat.Format32bppPBGRA);
                    return converter;
                }
            }
        }

        public static void SaveD2DBitmap(WIC.ImagingFactory wicFactory, WIC.Bitmap wicBitmap, Stream outputStream)
        {
            using (var encoder = new WIC.BitmapEncoder(wicFactory, WIC.ContainerFormatGuids.Png))
            {
                encoder.Initialize(outputStream);
                using (var frame = new WIC.BitmapFrameEncode(encoder))
                {
                    frame.Initialize();
                    frame.SetSize(wicBitmap.Size.Width, wicBitmap.Size.Height);

                    var pixelFormat = wicBitmap.PixelFormat;
                    frame.SetPixelFormat(ref pixelFormat);
                    frame.WriteSource(wicBitmap);

                    frame.Commit();
                    encoder.Commit();
                }
            }
        }

        public sealed class Wallpaper
        {
            const int SPI_SETDESKWALLPAPER = 20;
            const int SPIF_UPDATEINIFILE = 0x01;
            const int SPIF_SENDWININICHANGE = 0x02;

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

            public enum Style : int
            {
                Tiled,
                Centered,
                Stretched
            }

            public static void Set(string pictureFileName, Style style)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                if (style == Style.Stretched)
                {
                    key.SetValue(@"WallpaperStyle", 2.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }

                if (style == Style.Centered)
                {
                    key.SetValue(@"WallpaperStyle", 1.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }

                if (style == Style.Tiled)
                {
                    key.SetValue(@"WallpaperStyle", 1.ToString());
                    key.SetValue(@"TileWallpaper", 1.ToString());
                }

                SystemParametersInfo(SPI_SETDESKWALLPAPER,
                    0,
                    pictureFileName,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
        }

        static async Task Main(string[] args)
        {
            string url = await GetBingPicture();
            string english = await GetQuote();
            string chinese = Translator.GoogleTranslate(english, "en", "zh-CN");
            string weather = await GetWeather();
            string file = await DownloadUrlAsFileName(url);
            string wallpaperFileName = GenerateWallpaper(file, english, chinese, weather);
            Wallpaper.Set(wallpaperFileName, Wallpaper.Style.Centered);
        }
        
    }
}
