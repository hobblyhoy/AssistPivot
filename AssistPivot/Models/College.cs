using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;
using AssistPivot.Managers;

namespace AssistPivot.Models
{
    //[Table(name:"Colleges")] //redundant but useful tag if your table doesn't plurarlize well (e.g. "Library")
    public class College
    {
        [Key]
        public int CollegeId { get; set; }
        public string Name { get; set; }
        public string Shorthand { get; set; }
        public DateTimeOffset UpToDateAsOf { get; set; }

        public void Patch(College templateCollege)
        {
            this.Name = templateCollege.Name;
            this.Shorthand = templateCollege.Shorthand;
            this.UpToDateAsOf = templateCollege.UpToDateAsOf;
        }

        public override bool Equals(object otherObject)
        {
            var otherCollege = otherObject as College;
            if (otherObject == null) return false;

            return Name == otherCollege.Name
                && Shorthand == otherCollege.Shorthand;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() * 71
                + Shorthand.GetHashCode() * 73;
        }
    }

}