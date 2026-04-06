using System.Globalization;
using System.Text.Json;

namespace PharmaGo.Infrastructure.Services;

public static class PharmacyDiscoverySupport
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static double? CalculateDistanceKm(
        double? userLatitude,
        double? userLongitude,
        decimal? pharmacyLatitude,
        decimal? pharmacyLongitude)
    {
        if (!userLatitude.HasValue ||
            !userLongitude.HasValue ||
            !pharmacyLatitude.HasValue ||
            !pharmacyLongitude.HasValue)
        {
            return null;
        }

        const double earthRadiusKm = 6371d;
        var dLat = DegreesToRadians((double)pharmacyLatitude.Value - userLatitude.Value);
        var dLon = DegreesToRadians((double)pharmacyLongitude.Value - userLongitude.Value);
        var lat1 = DegreesToRadians(userLatitude.Value);
        var lat2 = DegreesToRadians((double)pharmacyLatitude.Value);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(earthRadiusKm * c, 2, MidpointRounding.AwayFromZero);
    }

    public static bool IsOpenNow(bool isOpen24Hours, string? openingHoursJson, DateTime utcNow)
    {
        if (isOpen24Hours)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(openingHoursJson))
        {
            return false;
        }

        try
        {
            var schedule = JsonSerializer.Deserialize<OpeningHoursDocument>(openingHoursJson, JsonOptions);
            if (schedule?.Weekly is null || schedule.Weekly.Count == 0)
            {
                return false;
            }

            var timeZone = ResolveTimeZone(schedule.TimeZone);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            var localTime = TimeOnly.FromDateTime(localNow);
            var currentDay = NormalizeDay(localNow.DayOfWeek);
            var previousDay = NormalizeDay(localNow.AddDays(-1).DayOfWeek);

            foreach (var entry in schedule.Weekly)
            {
                if (!TryParseTime(entry.Open, out var openTime) ||
                    !TryParseTime(entry.Close, out var closeTime))
                {
                    continue;
                }

                var entryDay = NormalizeDay(entry.Day);
                if (entryDay == currentDay && IsWithinHours(localTime, openTime, closeTime))
                {
                    return true;
                }

                if (entryDay == previousDay &&
                    closeTime <= openTime &&
                    localTime < closeTime)
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }

        return false;
    }

    public static DateTime? GetPickupAvailableFromUtc(bool isOpen24Hours, string? openingHoursJson, DateTime utcNow)
    {
        if (isOpen24Hours)
        {
            return utcNow;
        }

        if (string.IsNullOrWhiteSpace(openingHoursJson))
        {
            return null;
        }

        try
        {
            var schedule = JsonSerializer.Deserialize<OpeningHoursDocument>(openingHoursJson, JsonOptions);
            if (schedule?.Weekly is null || schedule.Weekly.Count == 0)
            {
                return null;
            }

            if (IsOpenNow(isOpen24Hours, openingHoursJson, utcNow))
            {
                return utcNow;
            }

            var timeZone = ResolveTimeZone(schedule.TimeZone);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);

            var entries = schedule.Weekly
                .Select(entry => new
                {
                    Day = NormalizeDay(entry.Day),
                    Open = TryParseTime(entry.Open, out var openTime) ? openTime : (TimeOnly?)null
                })
                .Where(x => x.Open.HasValue && !string.IsNullOrWhiteSpace(x.Day))
                .ToList();

            for (var dayOffset = 0; dayOffset <= 7; dayOffset++)
            {
                var targetDate = localNow.Date.AddDays(dayOffset);
                var targetDay = NormalizeDay(targetDate.DayOfWeek);
                var nextEntry = entries
                    .Where(x => x.Day == targetDay)
                    .OrderBy(x => x.Open)
                    .FirstOrDefault(x => dayOffset > 0 || x.Open!.Value > TimeOnly.FromDateTime(localNow));

                if (nextEntry is null)
                {
                    continue;
                }

                var localPickup = targetDate + nextEntry.Open!.Value.ToTimeSpan();
                return TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(localPickup, DateTimeKind.Unspecified),
                    timeZone);
            }
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }

        return null;
    }

    private static bool TryParseTime(string? value, out TimeOnly time)
    {
        return TimeOnly.TryParseExact(
            value,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);
    }

    private static bool IsWithinHours(TimeOnly currentTime, TimeOnly openTime, TimeOnly closeTime)
    {
        if (closeTime > openTime)
        {
            return currentTime >= openTime && currentTime < closeTime;
        }

        return currentTime >= openTime || currentTime < closeTime;
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    private static string NormalizeDay(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "mon",
        DayOfWeek.Tuesday => "tue",
        DayOfWeek.Wednesday => "wed",
        DayOfWeek.Thursday => "thu",
        DayOfWeek.Friday => "fri",
        DayOfWeek.Saturday => "sat",
        DayOfWeek.Sunday => "sun",
        _ => string.Empty
    };

    private static string NormalizeDay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim()[..Math.Min(3, value.Trim().Length)].ToLowerInvariant();
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private sealed class OpeningHoursDocument
    {
        public string? TimeZone { get; init; }
        public IReadOnlyCollection<OpeningHoursEntry> Weekly { get; init; } = Array.Empty<OpeningHoursEntry>();
    }

    private sealed class OpeningHoursEntry
    {
        public string? Day { get; init; }
        public string? Open { get; init; }
        public string? Close { get; init; }
    }
}
