using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Type")]
    public class Type
    {
        [Key] public int Id { get; set; }

        //public long typeid { get; set; }
        public string? type{ get; set; }

        public bool? Wanted { get; set; }

    }
}
