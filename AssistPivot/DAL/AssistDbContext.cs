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
        }

        public DbSet<College> Colleges { get; set; }

    }
}