using HY.FaceRecognitionForWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HY.FaceRecognitionForWeb.Views.Home
{
    public class GestureController : Controller
    {
        private static string imgData64 { get; set; }
        private IConfiguration _configuration { get; set; }

        public GestureController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 调用getAccessToken()获取的 access_token建议根据expires_in 时间 设置缓存
        // 返回token示例
        public static string AccessToken = "";

        public string GetAccessToken()
        {
            string authHost = "https://aip.baidubce.com/oauth/2.0/token";
            HttpClient client = new HttpClient();
            List<KeyValuePair<String, String>> paraList = new List<KeyValuePair<string, string>>();
            paraList.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            paraList.Add(new KeyValuePair<string, string>("client_id", _configuration["BaiduAiConfig:BaiDuGestureRecon:ApiKey_Gesture"]));
            paraList.Add(new KeyValuePair<string, string>("client_secret",
                _configuration["BaiduAiConfig:BaiDuGestureRecon:SecretKey_Gesture"]));

            HttpResponseMessage response = client.PostAsync(authHost, new FormUrlEncodedContent(paraList)).Result;
            string result = response.Content.ReadAsStringAsync().Result;
            var resultJson = JsonConvert.DeserializeObject<JObject>(result);
            AccessToken = resultJson["access_token"].ToString();
            return AccessToken;
        }

        public IActionResult GestureFromWeb(string imgData64FromAjax)
        {
            GetAccessToken();
            string host = "https://aip.baidubce.com/rest/2.0/image-classify/v1/gesture?access_token=" + AccessToken;
            Encoding encoding = Encoding.Default;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(host);
            request.Method = "post";
            request.KeepAlive = true;
            // 图片的base64编码
            //  string base64 = GetFileBase64("[本地图片文件]");
            string requestImgData64 = imgData64FromAjax;
            requestImgData64 = requestImgData64.Substring(requestImgData64.IndexOf(",") + 1);
            String str = "image=" + HttpUtility.UrlEncode(requestImgData64);
            byte[] buffer = encoding.GetBytes(str);
            request.ContentLength = buffer.Length;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
            string result = reader.ReadToEnd();
            var resultJson = JsonConvert.DeserializeObject<JObject>(result);
            if (int.Parse(resultJson["result_num"].ToString()) != 0)
            {
                string gestureToken = resultJson["result"][0]["classname"].ToString();
                GestureResultDict resultDict = new GestureResultDict();
                try
                {
                    string resultStr = resultDict.resultDict.FirstOrDefault(x => x.Key == gestureToken).Value;
                    if (!string.IsNullOrWhiteSpace(resultStr))
                    {
                        return Json(resultStr);
                    }
                    return Json("无法识别手势");
                }
                catch
                {
                    return Json("无法识别手势");
                }
            }
            return RedirectToAction("index", "home");
        }

        public static string GetFileBase64(String fileName)
        {
            FileStream filestream = new FileStream(fileName, FileMode.Open);
            byte[] arr = new byte[filestream.Length];
            filestream.Read(arr, 0, (int)filestream.Length);
            string baser64 = Convert.ToBase64String(arr);
            filestream.Close();
            return baser64;
        }
    }
}