using AssistPivot.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace AssistPivot.Models
{
    public class CourseSet
    {
        [Key]
        public int CourseSetId { get; set; }
        public virtual College College {get; set;}
        public virtual Year Year { get; set; }
        public string Data { get; set; }
        public string CommaDelimitedCourseNames { get; set; }
        public DateTimeOffset? UpToDateAsOf { get; set; }

        //[Obsolete("EF is buggy with private parameterless constructors so you get an Obsolete tag instead", true)]
        public CourseSet() { }


        public CourseSet(College college, Year year, string part)
        {
            this.College = college;
            this.Year = year;
            this.Data = part;
            this.CommaDelimitedCourseNames = GetCourseNames(part);
            this.UpToDateAsOf = DateTimeOffset.Now;
        }

        private string GetCourseNames(string part)
        {
            var regex = RegexManager.CourseRegex();
            var matches = regex.Matches(part);
            var ret = new List<string>();
            foreach (Match match in matches)
            {
                foreach (Capture capture in match.Captures)
                {
                    ret.Add(capture.Value.TrimEnd());
                }
            }

            ret = ret.Distinct().OrderBy(r => r).ToList();

            return String.Join(",", ret);
        }

        public override bool Equals(object otherObject)
        {
            var otherCourseSet = otherObject as CourseSet;
            if (otherCourseSet == null) return false;

            var ret = College.Equals(otherCourseSet.College)
                && Year.Equals((object)otherCourseSet.Year)
                && Data == otherCourseSet.Data
                && CommaDelimitedCourseNames == otherCourseSet.CommaDelimitedCourseNames;
            return ret;
        }

        public override int GetHashCode()
        {
            var ret = College.GetHashCode() * 71 
                + Year.GetHashCode() * 73 
                + Data.GetHashCode() * 79 
                + CommaDelimitedCourseNames.GetHashCode() * 83;
            return ret;
        }
        //public bool IsEmpty()
        //{
        //    return CourseSetId == 0
        //        && College == null
        //        && Year == null
        //        && Name == null
        //        && Description == null
        //        && Credits == null
        //        && UpToDateAsOf == null;
        //}

        //public bool Equals(CourseSet otherCourse)
        //{
        //    return College.CollegeId == otherCourse.College.CollegeId
        //        && Year.YearId == otherCourse.Year.YearId
        //        && Name == otherCourse.Name
        //        && Description == otherCourse.Description;
        //        //credits/UpToDateAsOf may change, I dont care
        //}

        ////For comparisons when you haven't mapped on the complex objects yet
        //public bool Equals(CourseSet otherCourse)
        //{
        //    return Name == otherCourse.Name
        //        && Description == otherCourse.Description;
        //}

        //public void PatchFromTemplate(CourseSet template)
        //{
        //    if (template.College != null) College = template.College;
        //    if (template.Year != null) Year = template.Year;
        //    if (template.Name != null) Name = template.Name;
        //    if (template.Description != null) Description = template.Description;
        //    if (template.Credits != null) Credits = template.Credits;
        //    if (template.UpToDateAsOf != null) UpToDateAsOf = template.UpToDateAsOf;
        //}
    }
}