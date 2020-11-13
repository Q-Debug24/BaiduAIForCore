using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HY.FaceRecognitionForWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FaceDAL.Service;
using FaceModels;
using Microsoft.Extensions.Configuration;

namespace HY.FaceRecognitionForWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IFaceEFService _faceEFService;
        private IConfiguration _configuration { get; set; }
        private static string imgData64 { get; set; }

        public HomeController(ILogger<HomeController> logger, IFaceEFService faceEFService, IConfiguration configuration)
        {
            _faceEFService = faceEFService;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy(FaceReconitionModel model)
        {
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //人脸注册前的检验方法，检验当前人脸系统中是否存在，不存在则进行注册，存在则提示信息，防止数据重复
        public IActionResult CheckIfUserHave(string imgData64FromAjax)
        {
            // 静态字段接受前端返回的base64格式的图片字节
            imgData64 = imgData64FromAjax;
            //设置百度云人脸识别应用密匙
            string ApiKey = _configuration["BaiduAiConfig:BaiDuFaceReco:ApiKey_Face"];
            string SecretKey = _configuration["BaiduAiConfig:BaiDuFaceReco:SecretKey_Face"];
            //nuget一下 baidu Ai
            var client = new Baidu.Aip.Face.Face(ApiKey, SecretKey);
            client.Timeout = 10000;
            var imageType = "BASE64";  //BASE64   URL
            string requestImgData64 = imgData64FromAjax;
            requestImgData64 = requestImgData64.Substring(imgData64.IndexOf(",") + 1);
            //设置用户组
            string groupId = "FaceGroupForNetCore";
            //返回一个JObject对象
            var findUserJsonResult = client.Search(requestImgData64, imageType, groupId);

            if (findUserJsonResult["error_code"].ToString() == "0"
                 && findUserJsonResult["error_msg"].ToString() == "SUCCESS")
            {
                var userList = JArray.Parse(findUserJsonResult["result"]["user_list"].ToString());
                foreach (var user in userList)
                {
                    //取出人脸分数，我这里直接取整了，例如89分
                    var score = Convert.ToInt32(user["score"].ToString().Substring(0, 2));
                    if (score > 80)
                    {
                        //取出百度云人脸库的id
                        Guid userId = Guid.Parse(user["user_id"].ToString());
                        //查询数据库，看看数据库中是否保存了该用户信息
                        var userModel = _faceEFService.GetByGuiIdAsync(userId).Result;
                        if (userModel != null)
                        {
                            var returnModel = new FaceReconitionModel
                            {
                                UserName = userModel.UserName
                            };

                            return Json(returnModel.UserName);
                        }
                    }
                }
            }
            //照片中没有人脸，会返回 222202 状态码
            if (findUserJsonResult["error_code"].ToString() == "222202")
            {
                return Json("无法识别到人脸");
            }
            //还有一种情况是会出现222207，就是该人脸未在百度云中录入此时可以进行注册操作
            return View();
        }

        //人脸注册
        [HttpPost]
        public IActionResult FaceRegister(
            [FromForm] FaceReconitionModel faceReconitionModel)
        {
            string ApiKey = _configuration["BaiduAiConfig:BaiDuFaceReco:ApiKey_Face"];
            string SecretKey = _configuration["BaiduAiConfig:BaiDuFaceReco:SecretKey_Face"];
            var client = new Baidu.Aip.Face.Face(ApiKey, SecretKey);
            client.Timeout = 10000;

            var imageType = "BASE64";  //BASE64   URL
            imgData64 = imgData64.Substring(imgData64.IndexOf(",") + 1);
            string groupId = "FaceGroupForNetCore";
            //这里要注意文档中user_id的格式，不能是带有下划线的，所以使用Guid.NewGuid().ToString("N");
            string userId = Guid.NewGuid().ToString("N");
            var userInfo = Guid.NewGuid();
            var options = new Dictionary<string, object>{
                        {"user_info", userInfo.ToString()},
                       {"quality_control", "NORMAL"},
                        {"liveness_control", "LOW"},
                      {"action_type", "REPLACE"}
                };
            client.UserAdd(imgData64, imageType, groupId, userId, options);
            var model = new FaceMdel
            {
                UserName = faceReconitionModel.UserName,
                Month = faceReconitionModel.Month.ToString(),
                Sex = faceReconitionModel.Sex,
                Works = faceReconitionModel.Works,
                Position = faceReconitionModel.Position,
                GuidId = Guid.Parse(userId)
            };
            _faceEFService.AddAsync(model);
            return RedirectToAction("index");
        }

        //这个字段用于人脸识别时保存人脸字节码
        private static string FaceImgData64 { get; set; }

        public IActionResult FaceDistinguish(string faceImgData64)
        {
            FaceImgData64 = faceImgData64;
            string ApiKey = _configuration["BaiduAiConfig:BaiDuFaceReco:ApiKey_Face"];
            string SecretKey = _configuration["BaiduAiConfig:BaiDuFaceReco:SecretKey_Face"];
            var client = new Baidu.Aip.Face.Face(ApiKey, SecretKey);
            client.Timeout = 10000;
            faceImgData64 = faceImgData64.Substring(faceImgData64.IndexOf(",") + 1);
            var faces = new JArray
                        {
                            new JObject
                            {
                                {"image", faceImgData64},
                                {"image_type", "BASE64"}
                            }
                        };

            //人脸识别前的活体检测方法，防止作弊
            var checkLiving = client.Faceverify(faces);
            if (checkLiving["error_code"].ToString() == "0" &&
                checkLiving["error_msg"].ToString() == "SUCCESS")
            {
                var livingList = checkLiving["result"]["thresholds"];
                double faceLiveness = Convert.ToDouble(checkLiving["result"]["face_liveness"].ToString());
                double frr_1e4 = Convert.ToDouble(livingList["frr_1e-4"].ToString());
                //这一块的分值设定直接看文档吧
                if (faceLiveness < frr_1e4)
                {
                    return Json("不是活体");
                }
            }
            if (checkLiving["error_code"].ToString() == "222202")
            {
                return Json("无法识别到人脸");
            }
            return Json("活体检验通过");
        }

        //这个方法用于返回人脸识别结果，其实可以在FaceDistinguish（）中返回json直接处理返回，我前端不太熟。。还是返回一个视图吧
        public IActionResult FaceResult()
        {
            string ApiKey = _configuration["BaiduAiConfig:BaiDuFaceReco:ApiKey_Face"];
            string SecretKey = _configuration["BaiduAiConfig:BaiDuFaceReco:SecretKey_Face"];
            var client = new Baidu.Aip.Face.Face(ApiKey, SecretKey);
            client.Timeout = 10000;
            var imageType = "BASE64";  //BASE64   URL
            FaceImgData64 = FaceImgData64.Substring(FaceImgData64.IndexOf(",") + 1);
            string groupId = "FaceGroupForNetCore";
            var faceDistinguishJson = client.Search(FaceImgData64, imageType, groupId);

            if (faceDistinguishJson["error_code"].ToString() == "0"
            && faceDistinguishJson["error_msg"].ToString() == "SUCCESS")
            {
                var userList = JArray.Parse(faceDistinguishJson["result"]["user_list"].ToString());
                foreach (var user in userList)
                {
                    var score = Convert.ToInt32(user["score"].ToString().Substring(0, 2));
                    if (score > 80)
                    {
                        Guid userId = Guid.Parse(user["user_id"].ToString());

                        var userModel = _faceEFService.GetByGuiIdAsync(userId).Result;
                        if (userModel != null)
                        {
                            var returnModel = new FaceReconitionModel
                            {
                                UserName = userModel.UserName,
                                Month = DateTime.Parse(userModel.Month),
                                Position = userModel.Position,
                                Works = userModel.Works,
                                Sex = userModel.Sex
                            };

                            return View(returnModel);
                        }
                    }
                }
            }
            return View();
        }

        public IActionResult FaceScore(string imgData64FromAjax)
        {
            var ApiKey = _configuration["BaiduAiConfig:BaiDuFaceReco:ApiKey_Face"];           //你的 Api Key
            var SecretKey = _configuration["BaiduAiConfig:BaiDuFaceReco:SecretKey_Face"];      //你的 Secret Key
            var client = new Baidu.Aip.Face.Face(ApiKey, SecretKey);
            imgData64FromAjax = imgData64FromAjax.Substring(imgData64FromAjax.IndexOf(",") + 1);
            client.Timeout = 60000;  // 修改超时时间
            var imageType = "BASE64";
            var options = new Dictionary<string, object>{
                        {"user_info", "user's info"},
                        {"quality_control", "NORMAL"},
                        {"liveness_control", "LOW" },
                       { "face_field", "beauty,age,gender"}
            };
            var result = client.Detect(imgData64FromAjax, imageType, options);
            JObject jToken = JsonConvert.DeserializeObject<JObject>(result.ToString());
            if (jToken["error_code"].ToString() == "0"
           && jToken["error_msg"].ToString() == "SUCCESS")
            {
                string faceScore = jToken["result"]["face_list"][0]["beauty"].ToString();
                //string age = jToken["result"]["face_list"][0]["age"].ToString();
                // gender = jToken["result"]["face_list"][0]["gender"].ToString();
                // return Json(new { faceScore, age, gender });
                return Json(double.Parse(faceScore));
            }
            return Json(null);
        }
    }
}