using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Touchfile")]
    public class TouchFile
    {
        [Key] public int Id { get; set; }

        public string? Name{ get; set; }

        public bool WasRealFile { get; set; } = false;
        public DateTime FileDate { get; set; }
        public Episode? Episode { get; set; }
    }
    [Table("Touchfolder")]
    public class TouchFolder
    {
        [Key] public int Id { get; set; }

        public string? Name { get; set; }
        public DateTime FileDate { get; set; }
    }
}
