using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("UserNetworkSelection")]
    public class UserNetworkSelection
    {
        [Key] public int Id { get; set; }

        public bool include { get; set; }


        public virtual Network network { get; set; }

    }
}
