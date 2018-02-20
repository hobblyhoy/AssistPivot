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
        public College College { get; set; }
        public Year Year { get; set; }
        public DateTimeOffset UpToDateAsOf { get; set; }
        public UpdateStatusTypes UpdateStatus { get; set; }
    }
}