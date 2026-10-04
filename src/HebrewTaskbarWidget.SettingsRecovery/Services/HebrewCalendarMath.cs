using System;
using System.Globalization;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// חודש עברי לוגי. Adar הוא אדר בשנה פשוטה ואדר א' בשנה מעוברת; AdarII קיים רק בשנה מעוברת.
    /// </summary>
    public enum HebrewMonth
    {
        Tishrei,
        Cheshvan,
        Kislev,
        Tevet,
        Shevat,
        Adar,
        AdarII,
        Nisan,
        Iyar,
        Sivan,
        Tammuz,
        Av,
        Elul,
    }

    /// <summary>תאריך עברי עם חודש לוגי (לא לפי המספור של .NET, שמשתנה בשנה מעוברת).</summary>
    public readonly record struct HebrewDate(int Year, HebrewMonth Month, int Day);

    /// <summary>
    /// חישובי לוח עברי משותפים מעל System.Globalization.HebrewCalendar - מרכז במקום אחד
    /// את ההמרה בין החודש הלוגי למספור של .NET.
    /// </summary>
    public static class HebrewCalendarMath
    {
        private static HebrewCalendar Calendar => HebrewDateFormatter.Calendar;

        private static readonly string[] MonthNames =
        {
            "תשרי", "חשון", "כסלו", "טבת", "שבט", "אדר", "אדר ב'",
            "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול",
        };

        public static bool IsLeapYear(int year) => Calendar.IsLeapYear(year);

        public static HebrewDate FromGregorian(DateTime date)
        {
            date = date.Date;
            int year = Calendar.GetYear(date);
            return new HebrewDate(year, FromNetMonth(year, Calendar.GetMonth(date)), Calendar.GetDayOfMonth(date));
        }

        public static DateTime ToGregorian(int year, HebrewMonth month, int day) =>
            Calendar.ToDateTime(year, ToNetMonth(year, month), day, 0, 0, 0, 0);

        public static int DaysInMonth(int year, HebrewMonth month) =>
            Calendar.GetDaysInMonth(year, ToNetMonth(year, month));

        /// <summary>חודש אדר שבו חלים פורים ותענית אסתר: אדר בשנה פשוטה, אדר ב' בשנה מעוברת.</summary>
        public static HebrewMonth PurimAdar(int year) => IsLeapYear(year) ? HebrewMonth.AdarII : HebrewMonth.Adar;

        /// <summary>החודש הבא באותה שנה עברית, או null אחרי אלול.</summary>
        public static HebrewMonth? NextMonthInYear(int year, HebrewMonth month)
        {
            if (month == HebrewMonth.Elul)
            {
                return null;
            }

            if (month == HebrewMonth.Adar)
            {
                return IsLeapYear(year) ? HebrewMonth.AdarII : HebrewMonth.Nisan;
            }

            return month + 1;
        }

        /// <summary>שם החודש לתצוגה. בשנה מעוברת אדר מוצג כ"אדר א'".</summary>
        public static string MonthName(int year, HebrewMonth month) =>
            month == HebrewMonth.Adar && IsLeapYear(year) ? "אדר א'" : MonthNames[(int)month];

        public static DateTime SaturdayOnOrAfter(DateTime date) =>
            date.Date.AddDays(((int)DayOfWeek.Saturday - (int)date.DayOfWeek + 7) % 7);

        public static DateTime SaturdayOnOrBefore(DateTime date) =>
            date.Date.AddDays(-(((int)date.DayOfWeek + 1) % 7));

        private static int ToNetMonth(int year, HebrewMonth month)
        {
            bool leap = IsLeapYear(year);
            return month switch
            {
                <= HebrewMonth.Adar => (int)month + 1,
                HebrewMonth.AdarII when leap => 7,
                HebrewMonth.AdarII => throw new ArgumentOutOfRangeException(nameof(month), "אדר ב' קיים רק בשנה מעוברת."),
                _ => (int)month + (leap ? 1 : 0),
            };
        }

        private static HebrewMonth FromNetMonth(int year, int netMonth)
        {
            if (netMonth <= 6)
            {
                return (HebrewMonth)(netMonth - 1);
            }

            return IsLeapYear(year) ? (HebrewMonth)(netMonth - 1) : (HebrewMonth)netMonth;
        }
    }
}
