using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Friend")]
    public class Friend
    {
        [Key] public int Id { get; set; }

        [MaxLength(200)] public string? Name { get; set; }
        [MaxLength(300)] public string? Email { get; set; }
        [MaxLength(500)] public string? FolderPath { get; set; }

        public List<FriendShow> InterestedShows { get; set; } = new();
    }

    [Table("FriendShow")]
    public class FriendShow
    {
        [Key] public int Id { get; set; }

        public int FriendId { get; set; }
        public Friend? Friend { get; set; }

        public int ShowId { get; set; }
        public Show? Show { get; set; }
    }

    [Table("FriendCopy")]
    public class FriendCopy
    {
        [Key] public int Id { get; set; }

        public int FriendId { get; set; }
        public Friend? Friend { get; set; }

        /// <summary>
        /// The filename that was copied (matches TouchFile.Name).
        /// </summary>
        [MaxLength(500)] public string? FileName { get; set; }

        public System.DateTime CopiedAt { get; set; }
    }
}
