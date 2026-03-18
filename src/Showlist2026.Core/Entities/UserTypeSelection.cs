using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("UserTypeSelection")]
    public class UserTypeSelection
    {
        [Key] public int Id { get; set; }

        public bool include { get; set; }

        public virtual Type type { get; set; }

    }
}
