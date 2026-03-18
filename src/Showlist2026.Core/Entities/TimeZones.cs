using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Timezone")]
    public class Timezone
    {
        [Key] public int Id { get; set; }

        public string? timezone { get; set; }
        public double UTCOffset { get; set; }
        public double UTCDSTOffset { get; set; }
        public string? countrycode { get; set; }
        public string? status  { get; set; }

    }
}
