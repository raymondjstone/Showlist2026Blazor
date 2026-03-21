using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("WebNetwork")]
    public class WebNetwork
    {
        [Key] public int Id { get; set; }

        public long webid { get; set; }
        public string? name{ get; set; }
        //public long countryid { get; set; }
        public string? timezone { get; set; }

        public Country? country { get; set; }

        public bool? Wanted { get; set; }
        public Timezone? tz { get; set; }

    }
}
