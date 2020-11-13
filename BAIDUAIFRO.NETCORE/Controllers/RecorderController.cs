using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HY.FaceRecognitionForWeb.Controllers
{
    public class RecorderController : Controller
    {
        public RecorderController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IConfiguration _configuration { get; set; }

        [HttpPost]
        public IActionResult RecorderVoice([FromForm] IFormFile audio)
        {
            string appId = _configuration["BaiduAiConfig:BaiDuLanguage:AppId_Language"];
            string apiKey = _configuration["BaiduAiConfig:BaiDuLanguage:ApiKey_Language"];
            string secertKey = _configuration["BaiduAiConfig:BaiDuLanguage:SecertKey_Language"];
            var client = new Baidu.Aip.Speech.Asr(appId, apiKey, secertKey);
            client.Timeout = 60000;  // 修改超时时间

            string filename = Path.Combine("wwwroot/files", Guid.NewGuid().ToString().Substring(0, 6) + ".wav");
            using
                (FileStream fs = System.IO.File.Create(filename))
            {
                audio.CopyTo(fs);
                fs.Flush();
            }

            FileStream filestream = new FileStream(filename, FileMode.Open);
            byte[] arr = new byte[filestream.Length];
            filestream.Read(arr, 0, (int)filestream.Length);
            filestream.Close();
            // 可选参数
            var options = new Dictionary<string, object>
            {
               {"dev_pid", 1537}
              //  {"dev_pid",1737 }
             };
            client.Timeout = 120000; // 若语音较长，建议设置更大的超时时间. ms
            var result = client.Recognize(arr, "wav", 16000, options);

            if (int.Parse(result["err_no"].ToString()) == 0 && result["err_msg"].ToString() == "success.")
            {
                return Json(result["result"][0].ToString());
            }
            return Json("Erro");
        }
    }
}