using System;
using System.Collections.Generic;
using System.Linq;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מחשב את פרשת השבוע לכל שבת, לפי לוח ארץ ישראל או חו"ל.
    /// הכללים לפי שו"ע או"ח תכח: צו לפני פסח בשנה פשוטה (מצורע, או אחרי מות כשיש שבת עודפת,
    /// בשנה מעוברת), במדבר לפני שבועות כשאפשר, דברים בשבת חזון ונצבים לפני ראש השנה.
    /// הפרשות מצורפות רק כשחסרות שבתות. התוצאה נבדקת מול Hebcal לשנים ה'תש"ס-ה'תתר"ס.
    /// </summary>
    public static class ParashaService
    {
        private static readonly string[] Names =
        {
            "בראשית", "נח", "לך לך", "וירא", "חיי שרה", "תולדות", "ויצא", "וישלח", "וישב", "מקץ",
            "ויגש", "ויחי", "שמות", "וארא", "בא", "בשלח", "יתרו", "משפטים", "תרומה", "תצוה",
            "כי תשא", "ויקהל", "פקודי", "ויקרא", "צו", "שמיני", "תזריע", "מצורע", "אחרי מות", "קדושים",
            "אמור", "בהר", "בחוקותי", "במדבר", "נשא", "בהעלותך", "שלח לך", "קורח", "חוקת", "בלק",
            "פינחס", "מטות", "מסעי", "דברים", "ואתחנן", "עקב", "ראה", "שופטים", "כי תצא", "כי תבוא",
            "נצבים", "וילך", "האזינו", "וזאת הברכה",
        };

        // זוג פרשות שמצורפות מזוהה לפי האינדקס של הראשונה בו.
        private const int VayakhelPekudei = 21;
        private const int TazriaMetzora = 26;
        private const int AchreiKedoshim = 28;
        private const int BeharBechukotai = 31;
        private const int ChukatBalak = 38;
        private const int MatotMasei = 41;
        private const int NitzavimVayeilech = 50;

        private const int Beshalach = 15;
        private const int Tzav = 24;
        private const int Metzora = 27;
        private const int Bamidbar = 33;
        private const int Masei = 42;
        private const int Nitzavim = 50;
        private const int Vayeilech = 51;
        private const int Haazinu = 52;

        private static readonly Dictionary<(int Year, bool Israel), Dictionary<DateTime, int[]>> Cache = new();
        private static readonly object CacheLock = new();

        /// <summary>פרשת השבת של השבוע (ראשון עד שבת) לפי ההגדרות הנוכחיות, או null אם באותה שבת אין קריאה רגילה.</summary>
        public static string? GetParashaName(DateTime date) =>
            GetParashaName(date, HolidayCalendar.CurrentOptions().Israel);

        /// <summary>פרשת השבת של השבוע (ראשון עד שבת), או null אם באותה שבת קוראים קריאת חג.</summary>
        public static string? GetParashaName(DateTime date, bool israel)
        {
            int[]? reading = GetReading(HebrewCalendarMath.SaturdayOnOrAfter(date), israel);
            return reading is null ? null : string.Join("־", reading.Select(index => Names[index]));
        }

        /// <summary>האם בשבת הזו קוראים את פרשת בשלח (שבת שירה).</summary>
        public static bool IsShabbatShira(DateTime saturday, bool israel) =>
            GetReading(saturday, israel)?.Contains(Beshalach) == true;

        /// <summary>מספרי הפרשות (0 = בראשית) שנקראות בשבת הנתונה, או null אם אין בה קריאה רגילה.</summary>
        internal static int[]? GetReading(DateTime saturday, bool israel)
        {
            int year = HebrewCalendarMath.FromGregorian(saturday).Year;
            Dictionary<DateTime, int[]>? readings;
            lock (CacheLock)
            {
                if (!Cache.TryGetValue((year, israel), out readings))
                {
                    readings = ComputeYear(year, israel);
                    Cache[(year, israel)] = readings;
                }
            }

            return readings.TryGetValue(saturday.Date, out int[]? reading) ? reading : null;
        }

        private static Dictionary<DateTime, int[]> ComputeYear(int year, bool israel)
        {
            var readings = new Dictionary<DateTime, int[]>();
            DateTime Date(HebrewMonth month, int day) => HebrewCalendarMath.ToGregorian(year, month, day);

            // בין ראש השנה לסוכות: וילך (אם לא צורפה לנצבים בשנה שעברה) והאזינו.
            DateTime yomKippur = Date(HebrewMonth.Tishrei, 10);
            var tishreiShabbatot = new List<DateTime>();
            for (DateTime d = HebrewCalendarMath.SaturdayOnOrAfter(Date(HebrewMonth.Tishrei, 3)); d <= Date(HebrewMonth.Tishrei, 14); d = d.AddDays(7))
            {
                if (d != yomKippur)
                {
                    tishreiShabbatot.Add(d);
                }
            }

            if (tishreiShabbatot.Count == 2)
            {
                readings[tishreiShabbatot[0]] = new[] { Vayeilech };
                readings[tishreiShabbatot[1]] = new[] { Haazinu };
            }
            else if (tishreiShabbatot.Count == 1)
            {
                readings[tishreiShabbatot[0]] = new[] { Haazinu };
            }

            DateTime pesach = Date(HebrewMonth.Nisan, 15);
            DateTime pesachEnd = pesach.AddDays(israel ? 6 : 7);
            DateTime shavuot = Date(HebrewMonth.Sivan, 6);
            DateTime shavuotEnd = israel ? shavuot : shavuot.AddDays(1);
            DateTime shabbatChazon = HebrewCalendarMath.SaturdayOnOrBefore(Date(HebrewMonth.Av, 9));
            DateTime nextRoshHashana = HebrewCalendarMath.ToGregorian(year + 1, HebrewMonth.Tishrei, 1);

            // ארבעה קטעים, כל אחד מסתיים בפרשה קבועה: לפני פסח, לפני שבועות, לפני שבת חזון, ועד ראש השנה.
            var beforePesach = new List<DateTime>();
            var beforeShavuot = new List<DateTime>();
            var beforeChazon = new List<DateTime>();
            var untilRoshHashana = new List<DateTime>();

            for (DateTime d = HebrewCalendarMath.SaturdayOnOrAfter(Date(HebrewMonth.Tishrei, 23)); d < nextRoshHashana; d = d.AddDays(7))
            {
                if ((d >= pesach && d <= pesachEnd) || (d >= shavuot && d <= shavuotEnd))
                {
                    continue;
                }

                if (d < pesach)
                {
                    beforePesach.Add(d);
                }
                else if (d < shavuot)
                {
                    beforeShavuot.Add(d);
                }
                else if (d < shabbatChazon)
                {
                    beforeChazon.Add(d);
                }
                else
                {
                    untilRoshHashana.Add(d);
                }
            }

            int next = 0;
            next = Assign(readings, beforePesach, next, HebrewCalendarMath.IsLeapYear(year) ? Metzora : Tzav, VayakhelPekudei);
            next = Assign(readings, beforeShavuot, next, Bamidbar, TazriaMetzora, AchreiKedoshim, BeharBechukotai);
            next = Assign(readings, beforeChazon, next, Masei, MatotMasei, ChukatBalak);

            // כשראש השנה הבא חל בשני או בשלישי, וילך נקראת בשבת שובה ולכן לא מצורפת לנצבים.
            bool vayeilechNextYear = nextRoshHashana.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday;
            Assign(readings, untilRoshHashana, next, vayeilechNextYear ? Nitzavim : Vayeilech, NitzavimVayeilech);

            return readings;
        }

        /// <summary>
        /// משבץ פרשות ברצף לשבתות של קטע אחד, כך שהקטע יסתיים בפרשת lastParasha.
        /// אם חסרות שבתות מצרפים זוגות לפי סדר העדיפות; אם יש שבת עודפת, הרצף פשוט ממשיך הלאה.
        /// </summary>
        private static int Assign(Dictionary<DateTime, int[]> readings, List<DateTime> shabbatot, int next, int lastParasha, params int[] pairsByPriority)
        {
            int pairsNeeded = lastParasha - next + 1 - shabbatot.Count;
            var combinedPairs = new HashSet<int>();
            foreach (int first in pairsByPriority)
            {
                if (pairsNeeded <= 0)
                {
                    break;
                }

                if (first >= next && first + 1 <= lastParasha)
                {
                    combinedPairs.Add(first);
                    pairsNeeded--;
                }
            }

            foreach (DateTime shabbat in shabbatot)
            {
                if (combinedPairs.Contains(next))
                {
                    readings[shabbat] = new[] { next, next + 1 };
                    next += 2;
                }
                else
                {
                    readings[shabbat] = new[] { next };
                    next++;
                }
            }

            return next;
        }
    }
}
