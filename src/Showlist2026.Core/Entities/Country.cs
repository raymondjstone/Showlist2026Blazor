using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Country")]
    public class Country
    {
        [Key] public int Id { get; set; }

        public string? code{ get; set; }
        public string? name { get; set; }

        public ICollection<UserCountrySelection>? UserCountrySelections { get; set; }

    }
}
