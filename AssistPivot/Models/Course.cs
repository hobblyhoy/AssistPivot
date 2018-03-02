using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AssistPivot.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }
        public virtual College College {get; set;}
        public virtual Year Year { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float? Credits { get; set; }
        public DateTimeOffset? UpToDateAsOf { get; set; }

        public bool IsEmpty()
        {
            return CourseId == 0
                && College == null
                && Year == null
                && Name == null
                && Description == null
                && Credits == null
                && UpToDateAsOf == null;
        }

        public bool Equals(Course otherCourse)
        {
            return College.CollegeId == otherCourse.College.CollegeId
                && Year.YearId == otherCourse.Year.YearId
                && Name == otherCourse.Name
                && Description == otherCourse.Description;
                //credits/UpToDateAsOf may change, I dont care
        }

        //For comparisons when you haven't mapped on the complex objects yet
        public bool LooseEquals(Course otherCourse)
        {
            return Name == otherCourse.Name
                && Description == otherCourse.Description;
        }

        public void PatchFromTemplate(Course template)
        {
            if (template.College != null) College = template.College;
            if (template.Year != null) Year = template.Year;
            if (template.Name != null) Name = template.Name;
            if (template.Description != null) Description = template.Description;
            if (template.Credits != null) Credits = template.Credits;
            if (template.UpToDateAsOf != null) UpToDateAsOf = template.UpToDateAsOf;
        }
    }
}