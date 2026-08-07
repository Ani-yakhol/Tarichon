using System;
using System.Collections.Generic;
using System.Linq;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// זמן הלכתי בודד להצגה ברשימה.
    /// </summary>
    public sealed class ZmanEntry
    {
        /// <summary>
        /// מפתח יציב לזיהוי הזמן הזה (לצורך התאמת כללי התראה/נראות ברחבי
        /// האפליקציה) - השם הקנוני הרגיל (כמו ZmanimCalendar.NameAlotHaShachar)
        /// לזמן "רגיל", או Id ייחודי (ZmanDuplicateRow.Id) לשורת "זמן כפול".
        /// **לא** משתנה גם אם למשתמש יש שם מותאם אישית לזמן הזה - כך ששינוי
        /// שם לא "שובר" כללי התראה/נראות קיימים. לא מוצג למשתמש ישירות.
        /// </summary>
        public required string Key { get; init; }

        /// <summary>השם המוצג בפועל למשתמש (בלוח הזמנים, בהתראות) - השם הקנוני, או שם מותאם אישית אם הוגדר (ראו AppSettings.ZmanCustomizations/ZmanDuplicateRows).</summary>
        public required string DisplayName { get; init; }

        /// <summary>
        /// השם הקנוני "האמיתי" לצורך הכרזה קולית - תמיד שם הזמן הבסיסי
        /// (למשל ZmanimCalendar.NameAlotHaShachar), **גם** לשורת "זמן כפול"
        /// (ששני העותקים - הראשי והכפול - חולקים את אותו VoiceKey). הסיבה:
        /// קובצי ההקראה הקוליים מוקלטים מראש לפי השם הקנוני בלבד - אין
        /// הקלטה לשם מותאם אישית שהמשתמש הזין, אז אין דרך להשמיע אותו
        /// בפועל; הפתרון הסביר היחיד הוא להמשיך ולהקריא את שם הזמן המקורי,
        /// גם כששמו המוצג (DisplayName) שונה.
        /// </summary>
        public required string VoiceKey { get; init; }

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
    ///
    /// נוספה שיטת חישוב שנייה (ZmanCalculationMethod.Mga72Zmaniyos,
    /// מבוססת KosherJava/Yitzchok-Zmanim) לצד השיטה המקורית (Gra) - ראו
    /// AppSettings.DefaultZmanCalculationMethod ותיעוד ZmanCalculationMethod.
    /// </summary>
    public static class ZmanimCalendar
    {
        private const double AlotHaShachatDegrees = 16.1;
        private const double TzeitHakochavimDegrees = 8.5;

        // שיטת ה-MGA "72 דקות זמניות": יחס השעות הזמניות (1.2 = 72/60) לפני
        // הנץ/אחרי השקיעה ברמת פני הים - ראו ZmanCalculationMethod.Mga72Zmaniyos.
        private const double Mga72ZmaniyosHours = 1.2;

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
        /// שם הזמן להצגה - גרסה "פשוטה" המקבלת רק את השם הקנוני (למשל
        /// משורת כלל התראה, ZmanRuleRow.ZmanName) - משמשת בעיקר בתצוגות
        /// "נסיון"/תצוגה מקדימה בפאנל ההגדרות, שאין להן גישה לישות ZmanEntry
        /// המלאה (ולכן לא יכולות לשקף שם מותאם אישית, אם יש). לתצוגה אמיתית
        /// (לוח הזמנים, התראה בפועל) יש להשתמש בעומס-היתר המקבל ZmanEntry.
        /// </summary>
        public static string GetPopupDisplayName(string canonicalName)
        {
            if (canonicalName == NameAlotHaShachar)
            {
                return "עלות השחר";
            }

            return canonicalName;
        }

        /// <summary>
        /// שם הזמן להצגה בלוח הזמנים (הפופ-אפ)/בהתראות בפועל - ה-DisplayName
        /// של הערך, חוץ מ"עלות השחר" (כשלא הוגדר לו שם מותאם אישית) - שם
        /// ה-"(16.1°)" הטכני מוצג רק בהגדרות "מיקום וזמנים" (כדי לדייק
        /// בבחירה), אך מושמט בתצוגה היומיומית. אם למשתמש כבר יש שם מותאם
        /// אישית לעלות השחר, הוא מוצג כפי שהוא (בלי שום עיבוד נוסף).
        /// </summary>
        public static string GetPopupDisplayName(ZmanEntry entry)
        {
            if (entry.Key == NameAlotHaShachar && entry.DisplayName == NameAlotHaShachar)
            {
                return "עלות השחר";
            }

            return entry.DisplayName;
        }

        public static IReadOnlyList<ZmanEntry> Calculate(
            DateTime date, GeoLocation location,
            int candleLightingMinutesBeforeSunset = 40,
            int? tzeitHakochavimMinutesAfterSunset = null,
            ZmanCalculationMethod method = ZmanCalculationMethod.Gra,
            IReadOnlyList<ZmanCustomization>? customizations = null,
            IReadOnlyList<ZmanDuplicateRow>? duplicateRows = null,
            bool forceIncludeCandleLighting = false)
        {
            TimeZoneInfo timeZone = ResolveTimeZone(location.TimeZoneId);

            // מחשבים את כל הזמנים התלויי-שיטה פעמיים (Gra ו-Mga72Zmaniyos) -
            // זול יחסית (כמה קריאות טריגונומטריות נוספות), ומאפשר לכל זמן
            // להשתמש בשיטה שלו (הכללית, או דריסה פרטית - ראו ZmanCustomization)
            // בלי לחשב הכל מחדש שוב בכל פעם. זמנים שלא תלויים בשיטה כלל
            // (הנץ/שקיעה, רבנו תם וכו') יוצאים זהים בשני המילונים ממילא.
            Dictionary<string, DateTime?> graTimes = ComputeBaseTimes(date, location, timeZone, ZmanCalculationMethod.Gra);
            Dictionary<string, DateTime?> mgaTimes = ComputeBaseTimes(date, location, timeZone, ZmanCalculationMethod.Mga72Zmaniyos);

            DateTime? shkia = graTimes[NameShkia];

            Dictionary<string, ZmanCustomization> customizationByBase =
                (customizations ?? Array.Empty<ZmanCustomization>()).ToDictionary(c => c.BaseZmanName);

            var duplicateByBase = new Dictionary<string, ZmanDuplicateRow>();
            foreach (ZmanDuplicateRow dup in duplicateRows ?? Array.Empty<ZmanDuplicateRow>())
            {
                duplicateByBase[dup.BaseZmanName] = dup;
            }

            var entries = new List<ZmanEntry>();

            foreach (string baseName in AllZmanNames)
            {
                if (baseName == NameCandleLighting)
                {
                    // forceIncludeCandleLighting: לשימוש בפאנל ההגדרות בלבד
                    // ("אילו זמנים להציג", לשונית "התראות") - שם רוצים תמיד
                    // לראות/להגדיר את השורה הזו, לא רק בימים שהיא רלוונטית
                    // בפועל (בניגוד לתצוגת "היום" האמיתית - הפופ-אפ/התראות,
                    // ששם עדיין רוצים לדלג עליה בימים לא-רלוונטיים).
                    if ((forceIncludeCandleLighting || HolidayService.IsErevCandleLighting(date)) && shkia is not null)
                    {
                        string clDisplayName = ResolveDisplayName(baseName, customizationByBase);
                        entries.Add(new ZmanEntry
                        {
                            Key = baseName,
                            DisplayName = clDisplayName,
                            VoiceKey = baseName,
                            Time = shkia.Value.AddMinutes(-Math.Max(0, candleLightingMinutesBeforeSunset)),
                        });
                    }

                    continue;
                }

                ZmanCalculationMethod effectiveMethod = method;
                customizationByBase.TryGetValue(baseName, out ZmanCustomization? customization);
                if (customization?.MethodOverride is ZmanCalculationMethod overrideMethod)
                {
                    effectiveMethod = overrideMethod;
                }

                string displayName = ResolveDisplayName(baseName, customizationByBase);
                DateTime? primaryTime = ResolveZmanTime(baseName, effectiveMethod, graTimes, mgaTimes, shkia, tzeitHakochavimMinutesAfterSunset);
                var primaryEntry = new ZmanEntry { Key = baseName, DisplayName = displayName, VoiceKey = baseName, Time = primaryTime };

                if (duplicateByBase.TryGetValue(baseName, out ZmanDuplicateRow? dup))
                {
                    // "צאת הכוכבים" הכפול לא מקבל את הדריסה הידנית (דקות-אחרי-
                    // שקיעה) - זו רלוונטית רק לשורה ה"ראשית", כדי שהכפילה תישאר
                    // תמיד מבוססת-שיטה טהורה (אחרת שתי השורות היו יכולות לצאת
                    // זהות, בניגוד לכל המטרה של הכפלה).
                    DateTime? duplicateTime = baseName == NameTzeitHakochavim
                        ? (dup.Method == ZmanCalculationMethod.Gra ? graTimes[baseName] : mgaTimes[baseName])
                        : ResolveZmanTime(baseName, dup.Method, graTimes, mgaTimes, shkia, tzeitHakochavimMinutesAfterSunset: null);

                    // VoiceKey = baseName גם כאן (לא dup.Id!) - אין הקלטה קולית
                    // לשם מותאם אישית; שתי השורות (ראשית וכפולה) מקריאות את
                    // אותו שם זמן בסיסי בפועל, ראו תיעוד ZmanEntry.VoiceKey.
                    var duplicateEntry = new ZmanEntry { Key = dup.Id, DisplayName = dup.CustomName, VoiceKey = baseName, Time = duplicateTime };

                    // הראשון ברשימה - הזמן המוקדם יותר (אם שניהם ידועים).
                    bool duplicateFirst = duplicateTime.HasValue && (!primaryTime.HasValue || duplicateTime.Value < primaryTime.Value);
                    entries.Add(duplicateFirst ? duplicateEntry : primaryEntry);
                    entries.Add(duplicateFirst ? primaryEntry : duplicateEntry);
                }
                else
                {
                    entries.Add(primaryEntry);
                }
            }

            return entries;
        }

        private static string ResolveDisplayName(string baseName, Dictionary<string, ZmanCustomization> customizationByBase)
        {
            if (customizationByBase.TryGetValue(baseName, out ZmanCustomization? customization) &&
                !string.IsNullOrWhiteSpace(customization.CustomName))
            {
                return customization.CustomName!;
            }

            return baseName;
        }

        private static DateTime? ResolveZmanTime(
            string baseName, ZmanCalculationMethod method,
            Dictionary<string, DateTime?> graTimes, Dictionary<string, DateTime?> mgaTimes,
            DateTime? shkia, int? tzeitHakochavimMinutesAfterSunset)
        {
            // "צאת הכוכבים": אם המשתמש הגדיר "דקות אחרי השקיעה" מפורש (ראו
            // AppSettings.TzeitHakochavimMinutesAfterSunset) - זה מה שמוצג,
            // במקום החישוב מבוסס-השיטה. משפיע רק על הזמן *המוצג* - לא על
            // חישובים פנימיים אחרים (כמו סוף זמן ק"ש/תפילה מג"א), שממשיכים
            // תמיד להתבסס על חישוב "צאת הכוכבים" הטהור לפי השיטה שנבחרה.
            if (baseName == NameTzeitHakochavim && tzeitHakochavimMinutesAfterSunset.HasValue)
            {
                return shkia?.AddMinutes(Math.Max(0, tzeitHakochavimMinutesAfterSunset.Value));
            }

            Dictionary<string, DateTime?> source = method == ZmanCalculationMethod.Gra ? graTimes : mgaTimes;
            return source.TryGetValue(baseName, out DateTime? time) ? time : null;
        }

        /// <summary>
        /// מחשבת את כל הזמנים (חוץ מ"הדלקת נרות", המטופל בנפרד ב-Calculate כי
        /// אינו תלוי-שיטה ומוצג רק בערבי שבת/חג) לפי שיטת חישוב נתונה - ראו
        /// ZmanCalculationMethod. זמנים שאינם תלויים בשיטה כלל (הנץ/שקיעה
        /// "מותאמי-גובה", זמני גר"א, רבנו תם) יוצאים זהים בין שתי השיטות.
        /// </summary>
        private static Dictionary<string, DateTime?> ComputeBaseTimes(DateTime date, GeoLocation location, TimeZoneInfo timeZone, ZmanCalculationMethod method)
        {
            var result = new Dictionary<string, DateTime?>();

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

            DateTime? alotHaShachar;
            DateTime? tzeitHakochavim;

            if (method == ZmanCalculationMethod.Mga72Zmaniyos && seaLevelSunrise is not null && seaLevelSunset is not null)
            {
                // ראו תיעוד ZmanCalculationMethod.Mga72Zmaniyos - נוסחה מדוייקת
                // מ-KosherJava (ComplexZmanimCalendar.GetAlos72/GetTzais72Zmanis
                // וכיו"ב): שעה זמנית "גר"א" מחושבת מרמת פני הים, ו-1.2 שעות
                // זמניות כאלה (=72 "דקות זמניות") לפני/אחרי הנץ/שקיעה.
                double shaahZmanisGraMinutes = (seaLevelSunset.Value - seaLevelSunrise.Value).TotalMinutes / 12.0;
                alotHaShachar = seaLevelSunrise.Value.AddMinutes(-shaahZmanisGraMinutes * Mga72ZmaniyosHours);
                tzeitHakochavim = seaLevelSunset.Value.AddMinutes(shaahZmanisGraMinutes * Mga72ZmaniyosHours);
            }
            else
            {
                alotHaShachar = AstronomicalCalculator.CalculateSunEvent(
                    date, location.LatitudeDegrees, location.LongitudeDegrees,
                    AstronomicalCalculator.GeometricZenith + AlotHaShachatDegrees, isSunrise: true, timeZone);
                tzeitHakochavim = AstronomicalCalculator.CalculateSunEvent(
                    date, location.LatitudeDegrees, location.LongitudeDegrees,
                    AstronomicalCalculator.GeometricZenith + TzeitHakochavimDegrees, isSunrise: false, timeZone);
            }

            result[NameAlotHaShachar] = alotHaShachar;
            result[NameNetz] = netz;
            result[NameShkia] = shkia;
            result[NameTzeitHakochavim] = tzeitHakochavim;
            result[NameRabbeinuTam] = shkia?.AddMinutes(72);

            if (netz is not null && shkia is not null)
            {
                double shaaZmanitGraMinutes = (shkia.Value - netz.Value).TotalMinutes / 12.0;

                result[NameSofZmanKriatShmaMga] = AddMinutesFromMga(alotHaShachar, tzeitHakochavim, 3.0);
                result[NameSofZmanKriatShmaGra] = netz.Value.AddMinutes(shaaZmanitGraMinutes * 3.0);
                result[NameSofZmanTefilaMga] = AddMinutesFromMga(alotHaShachar, tzeitHakochavim, 4.0);
                result[NameSofZmanTefilaGra] = netz.Value.AddMinutes(shaaZmanitGraMinutes * 4.0);
                result[NameChatzot] = netz.Value.AddMinutes((shkia.Value - netz.Value).TotalMinutes / 2.0);
                result[NameMinchaGedola] = netz.Value.AddMinutes(shaaZmanitGraMinutes * 6.5);
                result[NameMinchaKetana] = netz.Value.AddMinutes(shaaZmanitGraMinutes * 9.5);
                result[NamePelagHaMincha] = netz.Value.AddMinutes(shaaZmanitGraMinutes * 10.75);
            }
            else
            {
                // מגן מפני מצב קיצון תיאורטי (לא צפוי בקווי רוחב של ישראל) שבו
                // לא ניתן לחשב הנץ/שקיעה גיאומטריים ביום הנתון.
                result[NameSofZmanKriatShmaMga] = null;
                result[NameSofZmanKriatShmaGra] = null;
                result[NameSofZmanTefilaMga] = null;
                result[NameSofZmanTefilaGra] = null;
                result[NameChatzot] = null;
                result[NameMinchaGedola] = null;
                result[NameMinchaKetana] = null;
                result[NamePelagHaMincha] = null;
            }

            return result;
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
