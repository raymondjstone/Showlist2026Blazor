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

        /// <summary>
        /// Season offset for continuation shows: showSeason = fileSeason - SeasonOffset.
        /// 0 = plain folder alias (no continuation).
        /// </summary>
        public int SeasonOffset { get; set; }

        [ForeignKey("ShowId")]
        public Show? Show { get; set; }
    }
}
