using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("SSENData")]
    public class SSENData
    {
        [Key] public int Id { get; set; }

        public DateTime updateTime { get; set; }
        public string? rc{ get; set; }
        public string? json { get; set; }
    }
}
