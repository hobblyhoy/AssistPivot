using AssistPivot.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace AssistPivot.Models
{
    public enum UpdateStatusTypes { None, InFlight, Completed, Error }

    [Table(name: "CollegeYearStatuses")]
    public class CollegeYearStatus
    {
        [Key]
        public int CollegeYearStatusId { get; set; }
        public virtual College College { get; set; }
        public virtual Year Year { get; set; }
        public DateTimeOffset? UpToDateAsOf { get; set; }
        public UpdateStatusTypes UpdateStatus { get; set; }

        //private void BasicConstruct(College college, Year year)
        //{
        //    this.College = college;
        //    this.Year = year;
        //    this.UpToDateAsOf = null; // Object isn't "up to date" yet.
        //    this.UpdateStatus = UpdateStatusTypes.None;
        //}

        //private CollegeYearStatus() {
        //    this.College = null;
        //    this.Year = null;
        //    this.UpToDateAsOf = null; // Object isn't "up to date" yet.
        //    this.UpdateStatus = UpdateStatusTypes.None;
        //}

        //public CollegeYearStatus(int collegeId, int yearId, AssistDbContext db)
        //{
        //    var college = db.Colleges.FirstOrDefault(c => c.CollegeId == collegeId);
        //    var year = db.Years.FirstOrDefault(y => y.YearId == yearId);
        //    BasicConstruct(college, year);
        //}

        //public CollegeYearStatus(College college, Year year)
        //{
        //    BasicConstruct(college, year);
        //}
    }


}