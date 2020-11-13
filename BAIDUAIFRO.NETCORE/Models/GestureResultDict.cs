using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HY.FaceRecognitionForWeb.Models
{
    public class GestureResultDict
    {
        public GestureResultDict()
        {
            resultDict = results;
        }

        public Dictionary<string, string> resultDict { get; set; }

        private Dictionary<string, string> results = new Dictionary<string, string>()
        {
            {"Ok","Ok" },
            {"Six","数字6" },
            {"Rock","Rock" },
            {"Thumb_up","点赞" },
            {"One","数字1" },
            {"Five","数字5" },
            {"Fist","拳头" },
            {"Prayer","上天保佑" },
            {"Congratulation","恭喜恭喜" },
            {"Heart_single","笔芯" },
            {"Thumb_down","鄙视你" },
            {"ILY","黑凤梨" },
            { "Insult","竖中指"},
            { "Nine", "数字9" },
            { "Eight","数字8"},
            { "Seven","数字7"},
            { "Four","数字4"},
            { "Tow","数字2/Yeah"}
        };
    }
}