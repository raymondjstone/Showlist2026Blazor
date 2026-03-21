using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Language")]
    public class Language
    {
        [Key] public int Id { get; set; }

        //public string languageid{ get; set; }
        public string? name { get; set; }


        public bool? Wanted { get; set; }

    }
}
