using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace WallPaper
{
    public class Translate
    {
        /// <summary>
        /// 谷歌翻译
        /// </summary>
        /// <param name="text">待翻译文本</param>
        /// <param name="fromLanguage">自动检测：auto</param>
        /// <param name="toLanguage">中文：zh-CN，英文：en</param>
        /// <returns>翻译后文本</returns>
        public string GoogleTranslate(string text, string fromLanguage, string toLanguage)
        {
            CookieContainer cc = new CookieContainer();
            string ResultText = string.Empty;
            string tk = TokenGenerator.GetToken(text);
            string googleTransUrl = "https://translate.google.com.hk/translate_a/single?client=t&sl=" + fromLanguage + "&tl=" + toLanguage + "&hl=en&dt=at&dt=bd&dt=ex&dt=ld&dt=md&dt=qca&dt=rw&dt=rm&dt=ss&dt=t&ie=UTF-8&oe=UTF-8&otf=1&ssel=0&tsel=0&kc=1&tk=" + tk + "&q=" + HttpUtility.UrlEncode(text);

            var ResultHtml = GetResultHtml(googleTransUrl, cc, "https://translate.google.com.hk/");

            dynamic TempResult = Newtonsoft.Json.JsonConvert.DeserializeObject(ResultHtml);
            if (ResultHtml != "")
            {
                ResultText = Convert.ToString(TempResult[0][0][0]);
            }
            return ResultText;
        }

        public string GetResultHtml(string url, CookieContainer cookie, string referer)
        {
            try
            {
                var html = "";

                var webRequest = WebRequest.Create(url) as HttpWebRequest;

                webRequest.Method = "GET";

                webRequest.CookieContainer = cookie;

                webRequest.Referer = referer;

                webRequest.Timeout = 20000;

                webRequest.Headers.Add("X-Requested-With:XMLHttpRequest");

                webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";

                webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36";

                using (var webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    using (var reader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {

                        html = reader.ReadToEnd();
                        reader.Close();
                        webResponse.Close();
                    }
                }
                return html;
            }
            catch (Exception)
            {
                return "";
            }

        }

        /// <summary>
        /// 执行JS
        /// </summary>
        /// <param name="sExpression">参数体</param>
        /// <param name="sCode">JavaScript代码的字符串</param>
        /// <returns></returns>

        class TokenGenerator
        {
            private static A[] rl1 = new A[] { new A() { a = true, b = false, c = 10 }, new A() { a = false, b = true, c = 6 } };
            private static A[] rl2 = new A[] { new A() { a = true, b = false, c = 3 }, new A() { a = false, b = true, c = 11 }, new A() { a = true, b = false, c = 15 } };

            public static string GetToken(string a)
            {
                long b = 406398;

                for (int i = 0; i < a.Length; i++)
                {
                    int c = a[i];
                    if (c < 128)
                    {
                        B(ref b, c);
                    }
                    else
                    {
                        if (c < 2048)
                        {
                            B(ref b, c >> 6 | 192);
                        }
                        else
                        {
                            if ((c & 64512) == 55296 && i < a.Length - 1 && (a[i + 1] & 64512) == 56320)
                            {
                                c = 65536 + ((c & 1023) << 10) + (a[++i] & 1023);
                                B(ref b, c >> 18 | 240);
                                B(ref b, c >> 12 & 63 | 128);
                            }
                            else
                            {
                                B(ref b, c >> 12 | 224);
                            }
                            B(ref b, c >> 6 & 63 | 128);
                        }
                        B(ref b, c & 63 | 128);
                    }
                }

                RL(ref b, rl2);
                b ^= 2087938574;
                if (b < 0)
                    b = (b & 2147483647) + 2147483648;
                b %= 1000000;
                return $"{b}.{b ^ 406398}";
            }

            private static void B(ref long a, int b)
            {
                a += b;
                RL(ref a, rl1);
            }

            private static void RL(ref long a, A[] b)
            {
                foreach (A c in b)
                {
                    long d = c.b ? ((uint)a >> c.c) : a << c.c;
                    a = c.a ? ((a + d) & 4294967295) : a ^ d;
                }
            }

        }

        struct A
        {
            public bool a;
            public bool b;
            public int c;
        }
    }
}
