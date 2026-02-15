using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Board_Game_Software.Models;

public partial class BoardGameDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d));

        var dateOnlyNullableConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            d => d.HasValue ? DateOnly.FromDateTime(d.Value) : null);

        var timeOnlyConverter = new ValueConverter<TimeOnly, TimeSpan>(
            t => t.ToTimeSpan(),
            t => TimeOnly.FromTimeSpan(t));

        var timeOnlyNullableConverter = new ValueConverter<TimeOnly?, TimeSpan?>(
            t => t.HasValue ? t.Value.ToTimeSpan() : null,
            t => t.HasValue ? TimeOnly.FromTimeSpan(t.Value) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType == null) continue;

            foreach (var prop in entityType.GetProperties())
            {
                if (prop.ClrType == typeof(DateOnly))
                {
                    modelBuilder.Entity(clrType)
                        .Property(prop.Name)
                        .HasConversion(dateOnlyConverter)
                        .HasColumnType("date");
                }
                else if (prop.ClrType == typeof(DateOnly?))
                {
                    modelBuilder.Entity(clrType)
                        .Property(prop.Name)
                        .HasConversion(dateOnlyNullableConverter)
                        .HasColumnType("date");
                }
                else if (prop.ClrType == typeof(TimeOnly))
                {
                    modelBuilder.Entity(clrType)
                        .Property(prop.Name)
                        .HasConversion(timeOnlyConverter)
                        .HasColumnType("time");
                }
                else if (prop.ClrType == typeof(TimeOnly?))
                {
                    modelBuilder.Entity(clrType)
                        .Property(prop.Name)
                        .HasConversion(timeOnlyNullableConverter)
                        .HasColumnType("time");
                }
            }
        }
    }
}
