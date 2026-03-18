using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("UserLanguageSelection")]
    public class UserLanguageSelection
    {
        [Key] public int Id { get; set; }

        public bool include { get; set; }


        public virtual Language language { get; set; }

    }
}
