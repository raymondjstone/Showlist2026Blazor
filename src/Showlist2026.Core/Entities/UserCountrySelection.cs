using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("UserCountrySelection")]
    public class UserCountrySelection
    {
        [Key] public int Id { get; set; }

        //public long userid { get; set; }
        //public long countryid { get; set; }

        public bool include { get; set; }

        public virtual Country country { get; set; }


    }
}
