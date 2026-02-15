using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Board_Game_Software.Models;

public partial class BoardGameDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        var dateOnlyNullableConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            d => d.HasValue ? DateOnly.FromDateTime(d.Value) : null);

        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d));

        modelBuilder.Entity<BoardGame>()
            .Property(e => e.ReleaseDate)
            .HasConversion(dateOnlyNullableConverter)
            .HasColumnType("date");

        modelBuilder.Entity<BoardGameNight>()
            .Property(e => e.GameNightDate)
            .HasConversion(dateOnlyConverter)
            .HasColumnType("date");
    }
}
