using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("GenreText")]
    public class GenreText
    {
        [Key] public int Id { get; set; }

        public string? genre { get; set; }

        public ICollection<Genre>? Genres { get; set; }

        public bool? Wanted { get; set; }

    }
}
