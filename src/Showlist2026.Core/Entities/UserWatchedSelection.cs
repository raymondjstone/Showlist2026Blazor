using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("UserWatchedSelection")]
    public class UserWatchedSelection
    {
        [Key] public int Id { get; set; }

        public virtual Episode episode { get; set; }

    }
}
