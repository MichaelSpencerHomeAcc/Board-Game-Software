using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Board_Game_Software.Models;

public partial class BoardGameDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // DateOnly? -> date
        var dateOnlyNullableConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            d => d.HasValue ? DateOnly.FromDateTime(d.Value) : null);

        // DateOnly -> date
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d));

        // BoardGame.ReleaseDate (nullable)
        modelBuilder.Entity<BoardGame>()
            .Property(e => e.ReleaseDate)
            .HasConversion(dateOnlyNullableConverter)
            .HasColumnType("date");

        // BoardGameNight.GameNightDate (non-nullable)
        modelBuilder.Entity<BoardGameNight>()
            .Property(e => e.GameNightDate)
            .HasConversion(dateOnlyConverter)
            .HasColumnType("date");

        // Add any other DateOnly properties here the same way...
    }
}
