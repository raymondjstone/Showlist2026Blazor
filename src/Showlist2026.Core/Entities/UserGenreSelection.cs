using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("UserGenreSelection")]
    public class UserGenreSelection
    {
        [Key] public int Id { get; set; }

        public bool include { get; set; }

        public GenreText genretext { get; set; }

    }
}
