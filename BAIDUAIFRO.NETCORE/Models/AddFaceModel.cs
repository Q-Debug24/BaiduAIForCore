using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HY.FaceRecognitionForWeb.Models
{
    public class AddFaceModel
    {
        public string UserName { get; set; }  //姓名
        public string BirthDay { get; set; }  //出生年月
        public string Sex { get; set; }//性别
        public string Works { get; set; }  //工作/学习单位
        public string Position { get; set; }
        public string FaceToken { get; set; }  //人脸唯一标识
        public Guid GuidId { get; set; } //人脸和数据库表关联字段
    }
}