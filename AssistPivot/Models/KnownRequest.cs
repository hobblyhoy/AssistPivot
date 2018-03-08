using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AssistPivot.Models
{
    // Mostly for debugging but does serve some functionality in allowing us to skip known bad requests.
    // This has a very particular use in checking strings so for this particular class it makes sense to leave all
    // our normal DB objects as strings.
    public enum RequestSource { Main } //For now only concerned with the Main process one but want the ability to expand to others later without trouble
    public class KnownRequest
    {

        [Key]
        public int KnownRequestId { get; set; }
        public RequestSource RequestTo { get; set; }
        public string Url { get; set; }
        public int? Length { get; set; }
        public DateTimeOffset? UpToDateAsOf { get; set; }

        public override bool Equals(object otherObject)
        {
            var otherRequest = otherObject as KnownRequest;
            if (otherRequest == null) return false;

            return LooseEquals(otherRequest)
                && this.Length == otherRequest.Length
                && this.UpToDateAsOf == otherRequest.UpToDateAsOf;
        }

        // For checking equality before we've actually made the request
        public bool LooseEquals(KnownRequest otherRequest)
        {
            return this.RequestTo == otherRequest.RequestTo
                && this.Url == otherRequest.Url;
        }

        public bool IsValid()
        {
            //686 is the length of a response with only the templated information and no actual course content
            return this.Length > 686;
        }

        public void Update(int Length)
        {
            this.Length = Length;
            UpToDateAsOf = DateTimeOffset.Now;
        }

    }

}