namespace HebrewTaskbarWidget.Models
{
    /// <summary>
    /// מיקום גיאוגרפי לחישוב זמני היום ההלכתיים: קווי רוחב/אורך, גובה מעל פני הים
    /// (משפיע על הנץ/שקיעה), ואזור הזמן. בגרסה 0.2 יש ברירת מחדל אחת (ירושלים);
    /// אפשרות לבחור/להזין מיקום אחר מתוכננת לפאנל ההגדרות המלא (חלק 3).
    /// </summary>
    public sealed class GeoLocation
    {
        public required string Name { get; init; }
        public required double LatitudeDegrees { get; init; }
        public required double LongitudeDegrees { get; init; }
        public double ElevationMeters { get; init; }
        public required string TimeZoneId { get; init; }

        /// <summary>ברירת המחדל של גרסה 0.2 - ירושלים.</summary>
        public static GeoLocation JerusalemDefault => new()
        {
            Name = "ירושלים",
            LatitudeDegrees = 31.7683,
            LongitudeDegrees = 35.2137,
            ElevationMeters = 754,
            TimeZoneId = "Israel Standard Time", // מזוהה גם ב-Windows וגם (fallback) כ-Asia/Jerusalem ב-.NET 8
        };
    }
}
