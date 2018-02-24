using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AssistPivot.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace AssistPivot.DAL
{
    public class AssistDbContext : DbContext
    {
        public AssistDbContext() : base("AssistDbContext")
        {
            Database.Log = sql => System.Diagnostics.Debug.WriteLine(sql);
        }

        public DbSet<College> Colleges { get; set; }
        public DbSet<Year> Years { get; set; }
        public DbSet<CollegeYearStatus> CollegeYearStatuses { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseRelationship> CourseRelationships { get; set; }

    }
}