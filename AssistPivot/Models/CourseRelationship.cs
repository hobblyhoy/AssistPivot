using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using AssistPivot.Managers;

namespace AssistPivot.Models
{
    public enum CourseRelationshipType { Unset, None, And, Or }

    public class CourseRelationship : IEquatable<CourseRelationship>
    {
        [Key]
        public int CourseRelationshipId { get; set; }
        public virtual CourseSet ToCourseSet { get; set; }
        public virtual CourseSet FromCourseSet { get; set; }
        public DateTimeOffset? UpToDateAsOf { get; set; }

        public override bool Equals(object otherObject)
        {
            var otherCourseRelationship = otherObject as CourseRelationship;
            if (otherCourseRelationship == null) return false;

            return Equals(otherCourseRelationship);
        }

        public bool Equals(CourseRelationship otherCourseRelationship)
        {
            var ret = this.ToCourseSet.Equals(otherCourseRelationship.ToCourseSet)
                && this.FromCourseSet.Equals(otherCourseRelationship.FromCourseSet);
            return ret;
        }

        public override int GetHashCode()
        {
            var ret = ToCourseSet.GetHashCode() * 71
                + FromCourseSet.GetHashCode() * 73;
            return ret;
        }

        //public bool Equals(CourseRelationship otherCourseRelationship)
        //{
        //    //if (ToCourseSet.Count != otherCourseRelationship.ToCourseSet.Count)
        //    //{
        //    //    return false;
        //    //}
        //    //if (FromCourseSet.Count != otherCourseRelationship.FromCourseSet.Count)
        //    //{
        //    //    return false;
        //    //}
        //    //for (int i=0; i < ToCourseSet.Count; i++)
        //    //{
        //    //    if (!otherCourseRelationship.ToCourseSet.Exists(course => course.Equals(ToCourseSet[i]))) return false;
        //    //}
        //    //for (int i = 0; i < FromCourseSet.Count; i++)
        //    //{
        //    //    if (!otherCourseRelationship.FromCourseSet.Exists(course => course.Equals(FromCourseSet[i]))) return false;
        //    //}
        //    ////dont care about relationship types and updatedates

        //    return true;
        //}
    }
}