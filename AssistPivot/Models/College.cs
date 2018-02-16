using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace AssistPivot.Models
{
    public class College
    {
        public int CollegeId { get; set; }
        public string Name { get; set; }
        public string Shorthand { get; set; }
    }

    //public class CollegeContext : DbContext
    //{
    //    public DbSet<College> Colleges { get; set; }
    //}
}