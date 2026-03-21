using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Network")]
    public class Network
    {
        [Key] public int Id { get; set; }

        public long networkid { get; set; }
        public string? name{ get; set; }
        public string? timezone { get; set; }

        public Country? country { get; set; }


        public bool? Wanted { get; set; }
        public Timezone? tz { get; set; }

    }
}
