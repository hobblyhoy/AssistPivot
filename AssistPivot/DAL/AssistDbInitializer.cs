// We dont need this anymore since we enable automatic migrations
// just going to commit it to SC so I can go back to it if I need to

using AssistPivot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AssistPivot.DAL
{
    // todo "drop if model changes" is fine for now but at some point you need to figure out how to NOT lose all your data
    // every time you want to change the schema :S
    public class AssistDbInitializer : System.Data.Entity.DropCreateDatabaseIfModelChanges<AssistDbContext>
    {
        protected override void Seed(AssistDbContext context)
        {
            var colleges = new List<College>();
            colleges.Add(new College { Name = "Allan Hancock College", Shorthand = "ACH" });
            colleges.Add(new College { Name = "American River College", Shorthand = "ARC" });
            colleges.Add(new College { Name = "Antelope Valley College", Shorthand = "AVC" });
            colleges.Add(new College { Name = "Bakersfield College", Shorthand = "BAKERFLD" });

            colleges.ForEach(c => context.Colleges.Add(c));
            context.SaveChanges();
        }
    }
}