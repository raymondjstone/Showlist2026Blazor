using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Genre")]
    public class Genre
    {
        [Key] public int Id { get; set; }

        public Show? show { get; set; }

        public GenreText? genretext{ get; set; }


    }
}
