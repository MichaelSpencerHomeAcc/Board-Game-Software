using System;

namespace Board_Game_Software.Models
{
    public partial class VwBoardGameNight
    {
        public long Id { get; set; }
        public Guid Gid { get; set; }
        public bool Inactive { get; set; }
        public byte[]? VersionStamp { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime TimeCreated { get; set; }
        public string ModifiedBy { get; set; } = null!;
        public DateTime TimeModified { get; set; }
        public DateOnly GameNightDate { get; set; }
        public bool Finished { get; set; }

        // New columns from the updated SQL view
        public int PlayerCount { get; set; }
        public int MatchCount { get; set; }
    }
}
