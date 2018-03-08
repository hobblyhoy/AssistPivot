using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using AssistPivot.Managers;

namespace AssistPivot.Models
{
    public class Year
    {
        [Key]
        public int YearId { get; set; }
        public string Name { get; set; }
        public int FirstYearExpanded { get; set; }

        public override bool Equals(object otherObject)
        {
            var otherYear = otherObject as Year;
            if (otherYear == null) return false;

            return Name == otherYear.Name
                && FirstYearExpanded == otherYear.FirstYearExpanded;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() * 71
                + FirstYearExpanded.GetHashCode() * 73;
        }

    }
}