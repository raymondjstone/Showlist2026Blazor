using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("TVDirectories")]
    public class TVDirectories
    {
        [Key] public int Id { get; set; }

        public string? Name{ get; set; }
        public int  DaysToScan { get; set; }
        public string? Filter { get; set; }
        public int MinFileSize { get; set; }

    }
}
