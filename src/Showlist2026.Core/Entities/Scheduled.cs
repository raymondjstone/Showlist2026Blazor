using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Scheduled")]
    public class Scheduled
    {
        [Key] public int Id { get; set; }

        public long scheduledid { get; set; }
        public long showid { get; set; }
        public string? name{ get; set; }
        public string? season { get; set; }
        public string? number { get; set; }
        public string? airdate { get; set; }
        public string? airtime { get; set; }
        public string? airstamp { get; set; }
        public string? runtime { get; set; }
        public string? image { get; set; }

        [Column(TypeName = "varchar(MAX)")]
        public string? summary { get; set; }
        public string? links { get; set; }
    }
}
