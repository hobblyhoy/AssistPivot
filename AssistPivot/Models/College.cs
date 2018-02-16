using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace AssistPivot.Models
{
    //[Table(name:"Colleges")] //redundant but useful tag if your table doesn't plurarlize well (e.g. "Library")
    public class College
    {
        public int CollegeId { get; set; }
        public string Name { get; set; }
        public string Shorthand { get; set; }
        public DateTimeOffset UpToDateAsOf { get; set; }
    }

}