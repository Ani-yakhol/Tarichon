using System;
using System.Collections.Generic;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// זמן הלכתי בודד להצגה ברשימה: שם עברי וזמן מקומי (אם ניתן היה לחשבו).
    /// </summary>
    public sealed class ZmanEntry
    {
        public required string Name { get; init; }
        public DateTime? Time { get; init; }
    }

    /// <summary>
    /// שכבת החישוב ההלכתי: בונה מתוך זריחה/שקיעה אסטרונומיות את רשימת זמני היום
    /// המקובלים, לפי אותה מתודולוגיה שעליה מבוססת ספריית KosherJava - זריחה/שקיעה
    /// "גיאומטריות" (ברמת פני הים) לצורך זמנים מבוססי-מעלות (עלות/צאת), וזריחה/
    /// שקיעה "מותאמות גובה" (הנץ/שקיעה הנראים בפועל ממיקום המשתמש) לצורך שאר
    /// הזמנים ולתצוגת הנץ/השקיעה עצמם.
    ///
    /// הערה: אלו הכרעות נפוצות ומקובלות רווחות (לדוגמה: עלות השחר ב-16.1 מעלות,
    /// צאת הכוכבים ב-8.5 מעלות/13.5 דקות), אך אינן פוסקות הלכה למעשה. "צאת
    /// הכוכבים" ניתן כעת להגדרה כדקות-אחרי-השקיעה בהגדרות ("מיקום וזמנים") -
    /// ראו tzeitHakochavimMinutesAfterSunset למטה; שאר הזמנים עדיין קבועים.
    /// </summary>
    public static class ZmanimCalendar
    {
        private const double AlotHaShachatDegrees = 16.1;
        private const double TzeitHakochavimDegrees = 8.5;

        // שמות קבועים לכל זמן, כדי שגם פאנל ההגדרות (רשימת הזמנים להתראה)
        // וגם שכבת החישוב עצמה ישתמשו באותן מחרוזות בדיוק (מונע חוסר-התאמה).
        public const string NameAlotHaShachar = "עלות השחר (16.1°)";
        public const string NameNetz = "הנץ החמה";
        public const string NameSofZmanKriatShmaMga = "סוף זמן ק\"ש (מג\"א)";
        public const string NameSofZmanKriatShmaGra = "סוף זמן ק\"ש (גר\"א)";
        public const string NameSofZmanTefilaMga = "סוף זמן תפילה (מג\"א)";
        public const string NameSofZmanTefilaGra = "סוף זמן תפילה (גר\"א)";
        public const string NameChatzot = "חצות היום והלילה";
        public const string NameMinchaGedola = "מנחה גדולה";
        public const string NameMinchaKetana = "מנחה קטנה";
        public const string NamePelagHaMincha = "פלג המנחה";
        public const string NameShkia = "שקיעת החמה";
        public const string NameTzeitHakochavim = "צאת הכוכבים";
        public const string NameRabbeinuTam = "רבנו תם (72 דקות)";
        public const string NameCandleLighting = "הדלקת נרות";

        /// <summary>כל שמות הזמנים לפי סדר הופעתם ברשימה - נוח לשימוש בפאנל ההגדרות.</summary>
        public static readonly IReadOnlyList<string> AllZmanNames = new[]
        {
            NameAlotHaShachar, NameNetz,
            NameSofZmanKriatShmaMga, NameSofZmanKriatShmaGra,
            NameSofZmanTefilaMga, NameSofZmanTefilaGra,
            NameChatzot, NameMinchaGedola, NameMinchaKetana, NamePelagHaMincha,
            NameCandleLighting, NameShkia, NameTzeitHakochavim, NameRabbeinuTam,
        };

        /// <summary>
        /// שם הזמן להצגה בלוח הזמנים (הפופ-אפ) - זהה לשם הפנימי/הקנוני (המשמש
        /// גם בהגדרות ובכללי ההתראה) עבור כל הזמנים, חוץ מ"עלות השחר" - שם
        /// ה-"(16.1°)" הטכני מוצג רק בהגדרות "מיקום וזמנים" (כדי לדייק
        /// בבחירה), אך מושמט בתצוגה היומיומית בפופ-אפ.
        /// </summary>
        public static string GetPopupDisplayName(string canonicalName)
        {
            if (canonicalName == NameAlotHaShachar)
            {
                return "עלות השחר";
            }

            return canonicalName;
        }

        public static IReadOnlyList<ZmanEntry> Calculate(
            DateTime date, GeoLocation location,
            int candleLightingMinutesBeforeSunset = 40,
            int? tzeitHakochavimMinutesAfterSunset = null)
        {
            TimeZoneInfo timeZone = ResolveTimeZone(location.TimeZoneId);

            DateTime? seaLevelSunrise = AstronomicalCalculator.CalculateSunEvent(
                date, location.LatitudeDegrees, location.LongitudeDegrees, AstronomicalCalculator.GeometricZenith, isSunrise: true, timeZone);
            DateTime? seaLevelSunset = AstronomicalCalculator.CalculateSunEvent(
                date, location.LatitudeDegrees, location.LongitudeDegrees, AstronomicalCalculator.GeometricZenith, isSunrise: false, timeZone);

            double elevationZenithAdjustmentDegrees = ElevationAdjustmentDegrees(location.ElevationMeters);

            DateTime? netz = AstronomicalCalculator.CalculateSunEvent(
                date, location.LatitudeDegrees, location.LongitudeDegrees,
                AstronomicalCalculator.GeometricZenith + elevationZenithAdjustmentDegrees, isSunrise: true, timeZone);
            DateTime? shkia = AstronomicalCalculator.CalculateSunEvent(
                date, location.LatitudeDegrees, location.LongitudeDegrees,
                AstronomicalCalculator.GeometricZenith + elevationZenithAdjustmentDegrees, isSunrise: false, timeZone);

            DateTime? alotHaShachar = AstronomicalCalculator.CalculateSunEvent(
                date, location.LatitudeDegrees, location.LongitudeDegrees,
                AstronomicalCalculator.GeometricZenith + AlotHaShachatDegrees, isSunrise: true, timeZone);
            DateTime? tzeitHakochavim = AstronomicalCalculator.CalculateSunEvent(
                date, location.LatitudeDegrees, location.LongitudeDegrees,
                AstronomicalCalculator.GeometricZenith + TzeitHakochavimDegrees, isSunrise: false, timeZone);

            var entries = new List<ZmanEntry>
            {
                new() { Name = NameAlotHaShachar, Time = alotHaShachar },
                new() { Name = NameNetz, Time = netz },
            };

            if (netz is not null && shkia is not null)
            {
                double shaaZmanitGraMinutes = (shkia.Value - netz.Value).TotalMinutes / 12.0;

                entries.Add(new ZmanEntry { Name = NameSofZmanKriatShmaMga, Time = AddMinutesFromMga(alotHaShachar, tzeitHakochavim, 3.0) });
                entries.Add(new ZmanEntry { Name = NameSofZmanKriatShmaGra, Time = netz.Value.AddMinutes(shaaZmanitGraMinutes * 3.0) });
                entries.Add(new ZmanEntry { Name = NameSofZmanTefilaMga, Time = AddMinutesFromMga(alotHaShachar, tzeitHakochavim, 4.0) });
                entries.Add(new ZmanEntry { Name = NameSofZmanTefilaGra, Time = netz.Value.AddMinutes(shaaZmanitGraMinutes * 4.0) });
                entries.Add(new ZmanEntry { Name = NameChatzot, Time = netz.Value.AddMinutes((shkia.Value - netz.Value).TotalMinutes / 2.0) });
                entries.Add(new ZmanEntry { Name = NameMinchaGedola, Time = netz.Value.AddMinutes(shaaZmanitGraMinutes * 6.5) });
                entries.Add(new ZmanEntry { Name = NameMinchaKetana, Time = netz.Value.AddMinutes(shaaZmanitGraMinutes * 9.5) });
                entries.Add(new ZmanEntry { Name = NamePelagHaMincha, Time = netz.Value.AddMinutes(shaaZmanitGraMinutes * 10.75) });
            }
            else
            {
                // מגן מפני מצב קיצון תיאורטי (לא צפוי בקווי רוחב של ישראל) שבו
                // לא ניתן לחשב הנץ/שקיעה גיאומטריים ביום הנתון.
                entries.Add(new ZmanEntry { Name = NameSofZmanKriatShmaMga, Time = null });
                entries.Add(new ZmanEntry { Name = NameSofZmanKriatShmaGra, Time = null });
                entries.Add(new ZmanEntry { Name = NameSofZmanTefilaMga, Time = null });
                entries.Add(new ZmanEntry { Name = NameSofZmanTefilaGra, Time = null });
                entries.Add(new ZmanEntry { Name = NameChatzot, Time = null });
                entries.Add(new ZmanEntry { Name = NameMinchaGedola, Time = null });
                entries.Add(new ZmanEntry { Name = NameMinchaKetana, Time = null });
                entries.Add(new ZmanEntry { Name = NamePelagHaMincha, Time = null });
            }

            if (HolidayService.IsErevCandleLighting(date) && shkia is not null)
            {
                entries.Add(new ZmanEntry { Name = NameCandleLighting, Time = shkia.Value.AddMinutes(-Math.Max(0, candleLightingMinutesBeforeSunset)) });
            }

            entries.Add(new ZmanEntry { Name = NameShkia, Time = shkia });

            // "צאת הכוכבים" המוצג: אם המשתמש הגדיר "דקות אחרי השקיעה" מפורש
            // (ראו AppSettings.TzeitHakochavimMinutesAfterSunset) - זה מה
            // שמוצג, במקום החישוב מבוסס-המעלות. שימו לב: tzeitHakochavim
            // (מבוסס-המעלות, למעלה) עדיין משמש כפי שהוא לחישובים הפנימיים
            // האחרים (סוף זמן ק"ש/תפילה מג"א) - שינוי הגדרת התצוגה כאן לא
            // "מדליף" לשאר הזמנים, בכוונה, כדי לא לשנות בהם משהו שלא ביקשו.
            DateTime? tzeitDisplay = tzeitHakochavimMinutesAfterSunset.HasValue
                ? shkia?.AddMinutes(Math.Max(0, tzeitHakochavimMinutesAfterSunset.Value))
                : tzeitHakochavim;
            entries.Add(new ZmanEntry { Name = NameTzeitHakochavim, Time = tzeitDisplay });

            entries.Add(new ZmanEntry { Name = NameRabbeinuTam, Time = shkia?.AddMinutes(72) });

            return entries;
        }

        /// <summary>
        /// עבור זמנים המחושבים בשיטת "היום המגן אברהם" (72 דקות לפני עלות ועד אחרי צאת),
        /// כאשר יש עלות/צאת בזווית תקפים משתמשים בהם; אחרת נופלים חזרה לגישת 72 הדקות
        /// הקבועות סביב הנץ/שקיעה הגיאומטריים.
        /// </summary>
        private static DateTime? AddMinutesFromMga(DateTime? alotHaShachar, DateTime? tzeitHakochavim, double shaosZmaniyot)
        {
            if (alotHaShachar is null || tzeitHakochavim is null)
            {
                return null;
            }

            double mgaDayMinutes = (tzeitHakochavim.Value - alotHaShachar.Value).TotalMinutes;
            double shaaZmanitMgaMinutes = mgaDayMinutes / 12.0;

            return alotHaShachar.Value.AddMinutes(shaaZmanitMgaMinutes * shaosZmaniyot);
        }

        /// <summary>
        /// תוספת המעלות לזווית הזנית הנובעת מגובה מעל פני הים - ככל שהמיקום גבוה
        /// יותר, האופק הנראה בפועל נמוך יותר, והנץ מוקדם יותר / השקיעה מאוחרת יותר.
        /// נוסחה סטנדרטית: dip (מעלות) ≈ 0.0347 * sqrt(גובה במטרים).
        /// </summary>
        private static double ElevationAdjustmentDegrees(double elevationMeters)
        {
            if (elevationMeters <= 0)
            {
                return 0;
            }

            return 0.0347 * Math.Sqrt(elevationMeters);
        }

        private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // fallback לשם ה-IANA, למקרה שהריצה על .NET עם מסד נתוני אזורי-זמן שונה
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");
                }
                catch
                {
                    return TimeZoneInfo.Local;
                }
            }
        }
    }
}
