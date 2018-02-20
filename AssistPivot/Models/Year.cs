using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AssistPivot.Models
{
    public class Year
    {
        [Key]
        public int YearId { get; set; }
        public string Name { get; set; }
        public int FirstYearExpanded { get; set; }
    }
}