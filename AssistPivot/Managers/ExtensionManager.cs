using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AssistPivot.Managers
{
    public static class ExtensionManager
    {
        public static string Between(this String str, string start, string end)
        {
            int startLoc = str.IndexOf(start) + start.Length;
            int endLoc = str.IndexOf(end) - startLoc;
            return str.Substring(startLoc, endLoc);
        }

        public static string[] Split(this String str, string splitStr)
        {
            var splitStrArr = new string[] { splitStr };
            return str.Split(splitStrArr, StringSplitOptions.None);
        }
    }
}