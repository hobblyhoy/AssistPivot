using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace AssistPivot.Managers
{
    public class RegexManager
    {
        private static string uppercaseLetters = "[A-Z]";
        private static string anything = ".";
        private static string pipe = "[|]";
        private static string space = "[ ]";
        private static string numbers = "[0-9]";

        private static string One(string str) { return str; }
        private static string OneOrMore(string str) { return str + "+?"; }
        private static string ZeroOrMore(string str) { return str + "*?"; }

        private static string BuildCourseRegexStr()
        {
            return OneOrMore(uppercaseLetters) + One(space) + OneOrMore(numbers) + ZeroOrMore(uppercaseLetters) + One(space);
        }
        private static string BuildMultiCourseRegexStr()
        {
            var courseRegexStr = BuildCourseRegexStr();
            return courseRegexStr + ZeroOrMore(anything) + One(pipe) + ZeroOrMore(anything) + courseRegexStr;
        }


        public static Regex CourseRegex()
        {
            return new Regex(BuildCourseRegexStr(), RegexOptions.Singleline);
        }
        public static Regex MultiCourseRegex()
        {
            return new Regex(BuildMultiCourseRegexStr(), RegexOptions.Singleline);
        }



    }
}