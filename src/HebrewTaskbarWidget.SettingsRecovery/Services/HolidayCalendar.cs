using System;
using System.Collections.Generic;
using System.Linq;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>איפה מוצג טקסט המועדים - בוידג'ט אפשר להסתפק במועד העיקרי.</summary>
    public enum HolidayDisplayContext
    {
        Widget,
        Overlay,
        Popup,
    }

    /// <summary>הדלקת נרות בערב של יום נתון.</summary>
    public enum CandleLightingKind
    {
        None,

        /// <summary>לפני השקיעה - ערב שבת או ערב חג רגיל.</summary>
        BeforeSunset,

        /// <summary>אחרי צאת הכוכבים, מאש קיימת - במוצאי שבת שלפני חג ובליל יום טוב שני.</summary>
        AfterTzeit,
    }

    /// <summary>אפשרויות המועדים אחרי פענוח מצבי "אוטומטי" לפי המיקום.</summary>
    public sealed record HolidayOptions(bool Israel, bool WalledCity, HolidaySettings Display)
    {
        public static HolidayOptions FromSettings(AppSettings settings)
        {
            HolidaySettings display = settings.Holidays ?? new HolidaySettings();

            bool israel = display.Region switch
            {
                HolidayRegionMode.Israel => true,
                HolidayRegionMode.Diaspora => false,
                _ => IsIsraelTimeZone(settings.TimeZoneId),
            };

            bool walledCity = display.Purim switch
            {
                PurimObservanceMode.Walled => true,
                PurimObservanceMode.Regular => false,
                _ => IsJerusalem(settings.LocationName),
            };

            return new HolidayOptions(israel, walledCity, display);
        }

        public static bool IsIsraelTimeZone(string? timeZoneId) =>
            timeZoneId is "Israel Standard Time" or "Asia/Jerusalem";

        public static bool IsJerusalem(string? locationName) =>
            locationName?.Trim().StartsWith("ירושלים", StringComparison.Ordinal) == true;
    }

    /// <summary>
    /// לוח החגים והמועדים: ימים טובים וחול המועד, צומות (כולל דחייה והקדמה), ראש חודש, חנוכה,
    /// פורים (כולל פורים המשולש), שבתות מיוחדות, ספירת העומר ועוד - בארץ ישראל ובחו"ל.
    /// ימים לאומיים של מדינת ישראל אינם חלק מהלוח בכוונה.
    /// כל שנה עברית מחושבת פעם אחת ונשמרת בזיכרון.
    /// </summary>
    public static class HolidayCalendar
    {
        private static readonly Dictionary<(int Year, bool Israel, bool WalledCity), Dictionary<DateTime, List<HolidayEvent>>> Cache = new();
        private static readonly object CacheLock = new();

        public static HolidayOptions CurrentOptions() => HolidayOptions.FromSettings(SettingsService.Current);

        /// <summary>כל המועדים של היום, בלי סינון לפי ההגדרות, ממוינים לפי חשיבות.</summary>
        public static IReadOnlyList<HolidayEvent> GetAllEvents(DateTime date, bool israel, bool walledCity)
        {
            date = date.Date;
            int year = HebrewCalendarMath.FromGregorian(date).Year;
            Dictionary<DateTime, List<HolidayEvent>>? yearEvents;
            lock (CacheLock)
            {
                if (!Cache.TryGetValue((year, israel, walledCity), out yearEvents))
                {
                    yearEvents = ComputeYear(year, israel, walledCity);
                    Cache[(year, israel, walledCity)] = yearEvents;
                }
            }

            return yearEvents.TryGetValue(date, out List<HolidayEvent>? events) ? events : Array.Empty<HolidayEvent>();
        }

        /// <summary>המועדים של היום שמוצגים לפי ההגדרות, ממוינים לפי חשיבות.</summary>
        public static IReadOnlyList<HolidayEvent> GetEvents(DateTime date, HolidayOptions options) =>
            GetAllEvents(date, options.Israel, options.WalledCity)
                .Where(options.Display.IsVisible)
                .ToList();

        /// <summary>הטקסט להצגה, או null אם אין היום מועד. כמה מועדים מופרדים ב-" · ".</summary>
        public static string? GetDisplayText(DateTime date, HolidayDisplayContext context, HolidayOptions? options = null)
        {
            options ??= CurrentOptions();
            IReadOnlyList<HolidayEvent> events = GetEvents(date, options);
            if (events.Count == 0)
            {
                return null;
            }

            bool withDayNumber = options.Display.ShowDayNumbers;
            if (context == HolidayDisplayContext.Widget && options.Display.WidgetPrimaryOnly)
            {
                return events[0].DisplayName(withDayNumber);
            }

            return string.Join(" · ", events.Select(e => e.DisplayName(withDayNumber)));
        }

        /// <summary>המועדים הקרובים החל מהתאריך הנתון (כולל), בלי ימי ספירת העומר.</summary>
        public static IReadOnlyList<DatedHolidayEvent> GetUpcoming(DateTime from, int count, HolidayOptions options)
        {
            var result = new List<DatedHolidayEvent>();
            for (DateTime day = from.Date; result.Count < count && day < from.Date.AddDays(400); day = day.AddDays(1))
            {
                foreach (HolidayEvent holiday in GetEvents(day, options))
                {
                    if (holiday.Category != HolidayCategory.Omer)
                    {
                        result.Add(new DatedHolidayEvent(day, holiday));
                    }
                }
            }

            return result;
        }

        public static bool IsYomTov(DateTime date, bool israel) =>
            GetAllEvents(date, israel, walledCity: false).Any(e => e.IsYomTov);

        public static bool IsShabbatOrYomTov(DateTime date, bool israel) =>
            date.DayOfWeek == DayOfWeek.Saturday || IsYomTov(date, israel);

        public static bool IsShabbatOrYomTov(DateTime date) => IsShabbatOrYomTov(date, CurrentOptions().Israel);

        /// <summary>האם ומתי מדליקים נרות בערב של היום הנתון (לקראת שבת או יום טוב שמתחילים בלילה).</summary>
        public static CandleLightingKind GetCandleLighting(DateTime date, bool israel)
        {
            DateTime tomorrow = date.Date.AddDays(1);
            if (!IsShabbatOrYomTov(tomorrow, israel))
            {
                return CandleLightingKind.None;
            }

            // אסור להדליק לפני צאת השבת או החג של היום עצמו - חוץ מיום טוב שחל ביום שישי.
            bool todayIsHoly = date.DayOfWeek == DayOfWeek.Saturday
                || (IsYomTov(date, israel) && tomorrow.DayOfWeek != DayOfWeek.Saturday);

            return todayIsHoly ? CandleLightingKind.AfterTzeit : CandleLightingKind.BeforeSunset;
        }

        public static CandleLightingKind GetCandleLighting(DateTime date) => GetCandleLighting(date, CurrentOptions().Israel);

        private static Dictionary<DateTime, List<HolidayEvent>> ComputeYear(int year, bool israel, bool walledCity)
        {
            var events = new Dictionary<DateTime, List<HolidayEvent>>();
            DateTime Date(HebrewMonth month, int day) => HebrewCalendarMath.ToGregorian(year, month, day);

            void Add(DateTime date, HolidayEvent holiday)
            {
                if (!events.TryGetValue(date, out List<HolidayEvent>? list))
                {
                    list = new List<HolidayEvent>();
                    events[date] = list;
                }

                list.Add(holiday);
            }

            AddTishrei(year, israel, Date, Add);
            AddRoshChodesh(year, Date, Add);
            AddWinter(year, israel, Date, Add);
            AddPurim(year, walledCity, Date, Add);
            AddPesachAndOmer(year, israel, Date, Add);
            AddShavuotAndSummer(year, israel, Date, Add);
            AddMonthlyObservances(year, Date, Add);
            AddBehab(Date, Add);
            AddBirkatHachamah(year, Date, Add);

            foreach (List<HolidayEvent> list in events.Values)
            {
                // מיון יציב: קודם לפי חשיבות הקטגוריה, ובתוכה לפי סדר ההוספה.
                List<HolidayEvent> sorted = list.OrderBy(e => (int)e.Category).ToList();
                list.Clear();
                list.AddRange(sorted);
            }

            return events;
        }

        private static void AddTishrei(int year, bool israel, Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            add(date(HebrewMonth.Tishrei, 1), new("Rosh Hashana 1", "ראש השנה", HolidayCategory.YomTov, true, "ראש השנה (יום א')"));
            add(date(HebrewMonth.Tishrei, 2), new("Rosh Hashana 2", "ראש השנה", HolidayCategory.YomTov, true, "ראש השנה (יום ב')"));

            DateTime gedaliah = date(HebrewMonth.Tishrei, 3);
            if (gedaliah.DayOfWeek == DayOfWeek.Saturday)
            {
                add(gedaliah.AddDays(1), new("Tzom Gedaliah", "צום גדליה (נדחה)", HolidayCategory.MinorFast));
            }
            else
            {
                add(gedaliah, new("Tzom Gedaliah", "צום גדליה", HolidayCategory.MinorFast));
            }

            add(HebrewCalendarMath.SaturdayOnOrAfter(date(HebrewMonth.Tishrei, 3)), new("Shabbat Shuva", "שבת שובה", HolidayCategory.SpecialShabbat));
            add(date(HebrewMonth.Tishrei, 9), new("Erev Yom Kippur", "ערב יום כיפור", HolidayCategory.ErevChag));
            add(date(HebrewMonth.Tishrei, 10), new("Yom Kippur", "יום כיפור", HolidayCategory.YomTov, true));
            add(date(HebrewMonth.Tishrei, 14), new("Erev Sukkot", "ערב סוכות", HolidayCategory.ErevChag));

            AddFestivalWeek(date(HebrewMonth.Tishrei, 15), israel, "Sukkot", "סוכות", "חול המועד סוכות", add);
            add(date(HebrewMonth.Tishrei, 21), new("Sukkot 7", "הושענא רבה", HolidayCategory.CholHamoed));

            if (israel)
            {
                add(date(HebrewMonth.Tishrei, 22), new("Shmini Atzeret", "שמיני עצרת ושמחת תורה", HolidayCategory.YomTov, true));
                add(date(HebrewMonth.Tishrei, 23), new("Isru Chag", "אסרו חג", HolidayCategory.IsruChag));
            }
            else
            {
                add(date(HebrewMonth.Tishrei, 22), new("Shmini Atzeret", "שמיני עצרת", HolidayCategory.YomTov, true));
                add(date(HebrewMonth.Tishrei, 23), new("Simchat Torah", "שמחת תורה", HolidayCategory.YomTov, true));
                add(date(HebrewMonth.Tishrei, 24), new("Isru Chag", "אסרו חג", HolidayCategory.IsruChag));
            }
        }

        /// <summary>
        /// יום טוב ראשון (ובחו"ל גם שני) וימי חול המועד של סוכות או פסח. Key הוא מספר היום בחג
        /// (Sukkot 1..6, Pesach 1..6), כמו בלוח של Hebcal.
        /// </summary>
        private static void AddFestivalWeek(DateTime firstDay, bool israel, string key, string name, string cholHamoedName, Action<DateTime, HolidayEvent> add)
        {
            int yomTovDays = israel ? 1 : 2;
            for (int day = 1; day <= 6; day++)
            {
                DateTime date = firstDay.AddDays(day - 1);
                if (day <= yomTovDays)
                {
                    string yomTovName = israel ? name : $"{name} {HebrewGematria.Punctuate(HebrewGematria.ToLetters(day))}";
                    add(date, new($"{key} {day}", yomTovName, HolidayCategory.YomTov, true));
                }
                else
                {
                    int cholHamoedDay = day - yomTovDays;
                    add(date, new($"{key} {day}", cholHamoedName, HolidayCategory.CholHamoed, false,
                        $"{cholHamoedName} (יום {HebrewGematria.FormatDay(cholHamoedDay)})"));
                }
            }
        }

        private static void AddRoshChodesh(int year, Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            HebrewMonth previous = HebrewMonth.Tishrei;
            for (HebrewMonth? current = HebrewCalendarMath.NextMonthInYear(year, previous); current is HebrewMonth month; current = HebrewCalendarMath.NextMonthInYear(year, month))
            {
                string name = $"ראש חודש {HebrewCalendarMath.MonthName(year, month)}";
                if (HebrewCalendarMath.DaysInMonth(year, previous) == 30)
                {
                    add(date(previous, 30), new("Rosh Chodesh", name, HolidayCategory.RoshChodesh, false, $"{name} (יום א')"));
                    add(date(month, 1), new("Rosh Chodesh", name, HolidayCategory.RoshChodesh, false, $"{name} (יום ב')"));
                }
                else
                {
                    add(date(month, 1), new("Rosh Chodesh", name, HolidayCategory.RoshChodesh));
                }

                previous = month;
            }
        }

        private static void AddWinter(int year, bool israel, Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            DateTime chanukah = date(HebrewMonth.Kislev, 25);
            add(chanukah.AddDays(-1), new("Erev Chanukah", "ערב חנוכה", HolidayCategory.ErevChag));
            for (int day = 1; day <= 8; day++)
            {
                string numbered = day == 8 ? "זאת חנוכה" : $"חנוכה (יום {HebrewGematria.FormatDay(day)})";
                add(chanukah.AddDays(day - 1), new($"Chanukah {day}", "חנוכה", HolidayCategory.Chanukah, false, numbered));
            }

            // עשרה בטבת יכול לחול ביום שישי ואף פעם לא נדחה.
            add(date(HebrewMonth.Tevet, 10), new("Asara BTevet", "עשרה בטבת", HolidayCategory.MinorFast));
            add(date(HebrewMonth.Shevat, 15), new("Tu BiShvat", "ט\"ו בשבט", HolidayCategory.MinorHoliday));

            for (DateTime shabbat = HebrewCalendarMath.SaturdayOnOrAfter(date(HebrewMonth.Tevet, 1)); shabbat < date(HebrewMonth.Adar, 1); shabbat = shabbat.AddDays(7))
            {
                if (ParashaService.IsShabbatShira(shabbat, israel))
                {
                    add(shabbat, new("Shabbat Shirah", "שבת שירה", HolidayCategory.SpecialShabbat));
                    break;
                }
            }

            if (HebrewCalendarMath.IsLeapYear(year))
            {
                add(date(HebrewMonth.Adar, 14), new("Purim Katan", "פורים קטן", HolidayCategory.MinorHoliday));
                add(date(HebrewMonth.Adar, 15), new("Shushan Purim Katan", "שושן פורים קטן", HolidayCategory.MinorHoliday));
            }
        }

        private static void AddPurim(int year, bool walledCity, Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            HebrewMonth adar = HebrewCalendarMath.PurimAdar(year);

            add(HebrewCalendarMath.SaturdayOnOrBefore(date(adar, 1)), new("Shabbat Shekalim", "שבת שקלים", HolidayCategory.SpecialShabbat));
            add(HebrewCalendarMath.SaturdayOnOrBefore(date(adar, 13)), new("Shabbat Zachor", "שבת זכור", HolidayCategory.SpecialShabbat));

            // תענית אסתר שחלה בשבת מוקדמת ליום חמישי, כי אי אפשר לדחות אותה לפורים עצמו.
            DateTime esther = date(adar, 13);
            if (esther.DayOfWeek == DayOfWeek.Saturday)
            {
                add(date(adar, 11), new("Taanit Esther", "תענית אסתר (מוקדמת)", HolidayCategory.MinorFast));
            }
            else
            {
                add(esther, new("Taanit Esther", "תענית אסתר", HolidayCategory.MinorFast));
            }

            add(esther, new("Erev Purim", "ערב פורים", HolidayCategory.ErevChag));

            DateTime shushanPurim = date(adar, 15);
            if (walledCity && shushanPurim.DayOfWeek == DayOfWeek.Saturday)
            {
                // פורים המשולש: המגילה ומתנות לאביונים ביום שישי, על הניסים בשבת, והסעודה ומשלוח המנות ביום ראשון.
                add(date(adar, 14), new("Purim", "פורים המשולש: מגילה ומתנות לאביונים", HolidayCategory.Purim));
                add(shushanPurim, new("Shushan Purim", "שושן פורים (פורים המשולש)", HolidayCategory.Purim));
                add(date(adar, 16), new("Purim Meshulash", "פורים המשולש: סעודה ומשלוח מנות", HolidayCategory.Purim));
            }
            else
            {
                add(date(adar, 14), new("Purim", "פורים", HolidayCategory.Purim));
                add(shushanPurim, new("Shushan Purim", "שושן פורים", HolidayCategory.Purim));
            }

            DateTime hachodesh = HebrewCalendarMath.SaturdayOnOrBefore(date(HebrewMonth.Nisan, 1));
            add(hachodesh.AddDays(-7), new("Shabbat Parah", "שבת פרה", HolidayCategory.SpecialShabbat));
            add(hachodesh, new("Shabbat HaChodesh", "שבת החודש", HolidayCategory.SpecialShabbat));
        }

        private static void AddPesachAndOmer(int year, bool israel, Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            DateTime erevPesach = date(HebrewMonth.Nisan, 14);
            add(HebrewCalendarMath.SaturdayOnOrBefore(erevPesach), new("Shabbat HaGadol", "שבת הגדול", HolidayCategory.SpecialShabbat));

            // תענית בכורות שחלה בשבת מוקדמת ליום חמישי.
            if (erevPesach.DayOfWeek == DayOfWeek.Saturday)
            {
                add(erevPesach.AddDays(-2), new("Taanit Bechorot", "תענית בכורות (מוקדמת)", HolidayCategory.MinorFast));
            }
            else
            {
                add(erevPesach, new("Taanit Bechorot", "תענית בכורות", HolidayCategory.MinorFast));
            }

            add(erevPesach, new("Erev Pesach", "ערב פסח", HolidayCategory.ErevChag));

            DateTime pesach = date(HebrewMonth.Nisan, 15);
            AddFestivalWeek(pesach, israel, "Pesach", "פסח", "חול המועד פסח", add);
            add(pesach.AddDays(6), new("Pesach 7", "שביעי של פסח", HolidayCategory.YomTov, true));
            if (!israel)
            {
                add(pesach.AddDays(7), new("Pesach 8", "אחרון של פסח", HolidayCategory.YomTov, true));
            }

            // המימונה נחגגת במוצאי החג, ולכן מסומנת ביום אסרו חג.
            DateTime isruChag = pesach.AddDays(israel ? 7 : 8);
            add(isruChag, new("Isru Chag", "אסרו חג", HolidayCategory.IsruChag));
            add(isruChag, new("Mimouna", "מימונה", HolidayCategory.CommunityCustom, Minhag: CommunityMinhag.Sephardi));

            // הספירה נאמרת בלילה, ולכן ביום ט"ז בניסן מוצג "יום א' לעומר".
            for (int day = 1; day <= 49; day++)
            {
                add(pesach.AddDays(day), new($"Omer {day}", $"יום {HebrewGematria.FormatDay(day)} לעומר", HolidayCategory.Omer));
            }

            add(date(HebrewMonth.Iyar, 14), new("Pesach Sheni", "פסח שני", HolidayCategory.MinorHoliday));
            add(date(HebrewMonth.Iyar, 18), new("Lag BaOmer", "ל\"ג בעומר", HolidayCategory.MinorHoliday));
        }

        private static void AddShavuotAndSummer(int year, bool israel, Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            add(date(HebrewMonth.Sivan, 5), new("Erev Shavuot", "ערב שבועות", HolidayCategory.ErevChag));
            DateTime shavuot = date(HebrewMonth.Sivan, 6);
            if (israel)
            {
                add(shavuot, new("Shavuot 1", "שבועות", HolidayCategory.YomTov, true));
                add(shavuot.AddDays(1), new("Isru Chag", "אסרו חג", HolidayCategory.IsruChag));
            }
            else
            {
                add(shavuot, new("Shavuot 1", "שבועות א'", HolidayCategory.YomTov, true));
                add(shavuot.AddDays(1), new("Shavuot 2", "שבועות ב'", HolidayCategory.YomTov, true));
                add(shavuot.AddDays(2), new("Isru Chag", "אסרו חג", HolidayCategory.IsruChag));
            }

            DateTime tammuz17 = date(HebrewMonth.Tammuz, 17);
            if (tammuz17.DayOfWeek == DayOfWeek.Saturday)
            {
                add(tammuz17.AddDays(1), new("Tzom Tammuz", "צום י\"ז בתמוז (נדחה)", HolidayCategory.MinorFast));
            }
            else
            {
                add(tammuz17, new("Tzom Tammuz", "צום י\"ז בתמוז", HolidayCategory.MinorFast));
            }

            DateTime av9 = date(HebrewMonth.Av, 9);
            add(HebrewCalendarMath.SaturdayOnOrBefore(av9), new("Shabbat Chazon", "שבת חזון", HolidayCategory.SpecialShabbat));
            DateTime tishaBav = av9.DayOfWeek == DayOfWeek.Saturday ? av9.AddDays(1) : av9;
            add(tishaBav.AddDays(-1), new("Erev Tisha BAv", "ערב תשעה באב", HolidayCategory.ErevChag));
            add(tishaBav, new("Tisha BAv", tishaBav == av9 ? "תשעה באב" : "תשעה באב (נדחה)", HolidayCategory.MajorFast));
            add(HebrewCalendarMath.SaturdayOnOrAfter(av9.AddDays(1)), new("Shabbat Nachamu", "שבת נחמו", HolidayCategory.SpecialShabbat));
            add(date(HebrewMonth.Av, 15), new("Tu BAv", "ט\"ו באב", HolidayCategory.MinorHoliday));

            // ליל סליחות (מנהג אשכנז): מוצאי השבת שלפני ראש השנה, ולפחות ארבעה ימי סליחות לפניו.
            DateTime nextRoshHashana = HebrewCalendarMath.ToGregorian(year + 1, HebrewMonth.Tishrei, 1);
            DateTime selichot = HebrewCalendarMath.SaturdayOnOrBefore(nextRoshHashana.AddDays(-4));

            add(selichot, new("Leil Selichot", "ליל סליחות", HolidayCategory.LeilSelichot, Minhag: CommunityMinhag.Ashkenaz));

            // בספרד ועדות המזרח אומרים סליחות כל חודש אלול, החל מהלילה שאחרי ראש חודש.
            add(date(HebrewMonth.Elul, 2), new("Selichot Sephardi", "תחילת אמירת הסליחות", HolidayCategory.LeilSelichot, Minhag: CommunityMinhag.Sephardi));
            add(date(HebrewMonth.Elul, 29), new("Erev Rosh Hashana", "ערב ראש השנה", HolidayCategory.ErevChag));
        }

        /// <summary>שבת מברכים לפני כל ראש חודש (חוץ מתשרי), ויום כיפור קטן בערב ראש חודש.</summary>
        private static void AddMonthlyObservances(int year, Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            HebrewMonth previous = HebrewMonth.Tishrei;
            for (HebrewMonth? current = HebrewCalendarMath.NextMonthInYear(year, previous); current is HebrewMonth month; current = HebrewCalendarMath.NextMonthInYear(year, month))
            {
                DateTime lastDayBefore = date(previous, 29);
                add(HebrewCalendarMath.SaturdayOnOrBefore(lastDayBefore),
                    new("Shabbat Mevarchim", $"שבת מברכים חודש {HebrewCalendarMath.MonthName(year, month)}", HolidayCategory.ShabbatMevarchim));

                // לא נוהגים יום כיפור קטן לפני חשון (אחרי החגים), טבת (חנוכה) ואייר (ניסן). שישי ושבת מוקדמים לחמישי.
                if (month is not (HebrewMonth.Cheshvan or HebrewMonth.Tevet or HebrewMonth.Iyar))
                {
                    DateTime yomKippurKatan = lastDayBefore.DayOfWeek switch
                    {
                        DayOfWeek.Friday => lastDayBefore.AddDays(-1),
                        DayOfWeek.Saturday => lastDayBefore.AddDays(-2),
                        _ => lastDayBefore,
                    };
                    add(yomKippurKatan, new("Yom Kippur Katan", "יום כיפור קטן", HolidayCategory.YomKippurKatan));
                }

                previous = month;
            }
        }

        /// <summary>
        /// תעניות בה"ב (מנהג אשכנז): שני, חמישי ושני שאחרי השבת הראשונה שאחרי ראש חודש חשון ואייר.
        /// השני האחרון באייר לא יכול לחול בפסח שני, ואז הוא נדחה לי"ז באייר.
        /// </summary>
        private static void AddBehab(Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            foreach (HebrewMonth month in new[] { HebrewMonth.Cheshvan, HebrewMonth.Iyar })
            {
                DateTime roshChodesh = date(month, 1);
                DateTime shabbat = HebrewCalendarMath.SaturdayOnOrAfter(roshChodesh);
                if (shabbat == roshChodesh)
                {
                    shabbat = shabbat.AddDays(7);
                }

                string[] names = { "תענית בה\"ב (שני קמא)", "תענית בה\"ב (חמישי)", "תענית בה\"ב (שני בתרא)" };
                int[] offsets = { 2, 5, 9 };
                for (int i = 0; i < 3; i++)
                {
                    DateTime fast = shabbat.AddDays(offsets[i]);
                    if (month == HebrewMonth.Iyar && fast == date(HebrewMonth.Iyar, 14))
                    {
                        fast = date(HebrewMonth.Iyar, 17);
                    }

                    add(fast, new("Behab", names[i], HolidayCategory.Behab, Minhag: CommunityMinhag.Ashkenaz));
                }
            }
        }

        /// <summary>ברכת החמה - פעם ב-28 שנה, ביום שבו תקופת ניסן לפי שמואל (26 במרץ היוליאני) חלה.</summary>
        private static void AddBirkatHachamah(int year, Func<HebrewMonth, int, DateTime> date, Action<DateTime, HolidayEvent> add)
        {
            DateTime yearStart = date(HebrewMonth.Tishrei, 1);
            DateTime nextYearStart = HebrewCalendarMath.ToGregorian(year + 1, HebrewMonth.Tishrei, 1);
            for (int gregorianYear = yearStart.Year; gregorianYear <= nextYearStart.Year; gregorianYear++)
            {
                if ((gregorianYear - 1981) % 28 != 0)
                {
                    continue;
                }

                int julianOffset = gregorianYear / 100 - gregorianYear / 400 - 2;
                DateTime blessing = new DateTime(gregorianYear, 3, 26).AddDays(julianOffset);
                if (blessing >= yearStart && blessing < nextYearStart)
                {
                    add(blessing, new("Birkat Hachamah", "ברכת החמה", HolidayCategory.MinorHoliday));
                }
            }
        }
    }
}
