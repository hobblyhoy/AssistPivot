using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AssistPivot.Models
{
    public enum CourseRelationshipType { Unset, None, And, Or }

    public class CourseRelationship
    {
        [Key]
        public int CourseRelationshipId { get; set; }
        public virtual List<Course> ToCourses { get; set; }
        public virtual List<Course> FromCourses { get; set; }
        public DateTimeOffset? UpToDateAsOf { get; set; }
        public CourseRelationshipType ToRelationshipType { get; set; }
        public CourseRelationshipType FromRelationshipType { get; set; }

        public bool Equals(CourseRelationship otherCourseRelationship)
        {
            if (ToCourses.Count != otherCourseRelationship.ToCourses.Count)
            {
                return false;
            }
            if (FromCourses.Count != otherCourseRelationship.FromCourses.Count)
            {
                return false;
            }
            for (int i=0; i < ToCourses.Count; i++)
            {
                if (!otherCourseRelationship.ToCourses.Exists(course => course.Equals(ToCourses[i]))) return false;
            }
            for (int i = 0; i < FromCourses.Count; i++)
            {
                if (!otherCourseRelationship.FromCourses.Exists(course => course.Equals(FromCourses[i]))) return false;
            }
            //dont care about relationship types and updatedates

            return true;
        }
    }
}