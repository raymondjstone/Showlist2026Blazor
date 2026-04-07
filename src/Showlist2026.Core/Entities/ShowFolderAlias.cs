using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("ShowFolderAliases")]
    public class ShowFolderAlias
    {
        [Key] public int Id { get; set; }

        public int ShowId { get; set; }

        [MaxLength(500)]
        public string AliasName { get; set; } = "";

        [ForeignKey("ShowId")]
        public Show? Show { get; set; }
    }
}
