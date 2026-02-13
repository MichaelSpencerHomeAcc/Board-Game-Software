using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Board_Game_Software.Models;

[Table("PlayerBoardGame", Schema = "bgd")]
public partial class PlayerBoardGame
{
    [Key]
    public long Id { get; set; }

    public Guid Gid { get; set; }

    public bool Inactive { get; set; }

    [Timestamp]
    public byte[]? VersionStamp { get; set; }

    [StringLength(256)]
    public string CreatedBy { get; set; } = null!;

    public DateTime TimeCreated { get; set; }

    [StringLength(256)]
    public string ModifiedBy { get; set; } = null!;

    public DateTime TimeModified { get; set; }

    public long? FkBgdPlayer { get; set; }

    public long? FkBgdBoardGame { get; set; }

    public short Rank { get; set; }

    [ForeignKey("FkBgdBoardGame")]
    public virtual BoardGame? BoardGame { get; set; }

    [ForeignKey("FkBgdPlayer")]
    public virtual Player? Player { get; set; }
}