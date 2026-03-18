using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("WatchedHistory")]
    public class WatchedHistory
    {
        [Key] public int Id { get; set; }
        public virtual Episode episode { get; set; }
        public DateTimeOffset WatchedDate { get; set; }
    }
}
