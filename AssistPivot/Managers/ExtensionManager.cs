using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace AssistPivot.Managers
{
    public static class ExtensionManager
    {
        //String
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

        public static string[] Split(this String str, string splitStr, int count)
        {
            var splitStrArr = new string[] { splitStr };
            return str.Split(splitStrArr, count, StringSplitOptions.None);
        }

        // List<T>
        public static string Stringify<T>(this List<T> list)
        {
            return Stringify<T>(list, "\r\n");
        }

        public static string Stringify<T>(this List<T> list, string customSeperator)
        {
            return String.Join(customSeperator, list);
        }

    }

}