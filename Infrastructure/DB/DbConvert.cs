using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.DB
{
    public static class DbConvert
    {
        public static string TryToString(this object strRes)
        {
            if (strRes == null)
                return "";
            else
                return strRes.ToString();
        }
        public static int TryToInt(this object strRes)
        {
            if (strRes == null)
                return 0;
            else
            {
                int.TryParse(strRes.ToString(), out int rst);
                return rst;
            }
        }
        public static float TryToFloat(this object strRes)
        {
            if (strRes == null)
                return 0;
            else
            {
                float.TryParse(strRes.ToString(), out float rst);
                return rst;
            }
        }
        public static DateTime TryToDateTime(this object strRes)
        {
            if (strRes == null)
                return new DateTime(1970, 1, 1);
            else
            {
                DateTime.TryParse(strRes.ToString(), out DateTime rst);
                return rst;
            }
        }
    }
}
