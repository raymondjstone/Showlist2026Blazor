using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("UserShowSelection")]
    public class UserShowSelection
    {
        [Key] public int Id { get; set; }


        public bool include { get; set; }
        public int Priority { get; set; }

        public Show show { get; set; }
    }
}
