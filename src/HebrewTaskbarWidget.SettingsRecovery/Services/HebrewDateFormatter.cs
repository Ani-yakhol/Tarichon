using System;
using System.Globalization;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// תוצאת עיצוב התאריך העברי - שתי השורות שהוידג'ט מציג.
    /// </summary>
    public sealed class HebrewDateDisplay
    {
        /// <summary>השורה העליונה: יום בשבוע ופרשת השבוע (לדוגמה: "שני, דברים").</summary>
        public required string TopLine { get; init; }

        /// <summary>השורה התחתונה: התאריך העברי המלא (לדוגמה: "כ"ח תמוז ה'תשפ"ו").</summary>
        public required string BottomLine { get; init; }
    }

    /// <summary>
    /// ממיר תאריך לועזי לתצוגה עברית מלאה: יום בשבוע, פרשת השבוע, ותאריך עברי
    /// מפורש בגימטריה, תוך שימוש ב-System.Globalization.HebrewCalendar המובנה
    /// של .NET לחישוב הלוח העברי עצמו (חישוב זה מדוייק ואינו תלוי בטבלת הפרשות).
    /// </summary>
    public static class HebrewDateFormatter
    {
        /// <summary>מופע לוח עברי משותף - נחשף (internal use) כדי שרכיבים אחרים (למשל בורר התאריך העברי) יוכלו לבצע חישובי לוח עברי זהים בלי ליצור מופע נפרד.</summary>
        public static readonly HebrewCalendar Calendar = new();

        private static readonly string[] DayOfWeekNames =
        {
            "ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת",
        };

        // שמות החודשים לפי מספור .NET (Tishrei = 1). לוח מלא/חסר משתנה רק
        // באורך חשוון/כסלו ולא במספור החודשים, כך שאין צורך בטבלה נפרדת לכך.
        private static readonly string[] MonthNamesCommonYear =
        {
            "תשרי", "חשון", "כסלו", "טבת", "שבט", "אדר",
            "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול",
        };

        private static readonly string[] MonthNamesLeapYear =
        {
            "תשרי", "חשון", "כסלו", "טבת", "שבט", "אדר א'", "אדר ב'",
            "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול",
        };

        public static HebrewDateDisplay Format(DateTime gregorianDate)
        {
            int hebrewDay = Calendar.GetDayOfMonth(gregorianDate);
            int hebrewMonth = Calendar.GetMonth(gregorianDate);
            int hebrewYear = Calendar.GetYear(gregorianDate);
            bool isLeapYear = Calendar.IsLeapYear(hebrewYear);

            string monthName = GetMonthName(hebrewMonth, isLeapYear);

            string dayLetters = HebrewGematria.FormatDay(hebrewDay);
            string yearLetters = HebrewGematria.FormatYear(hebrewYear);

            string bottomLine = $"{dayLetters} {monthName} {yearLetters}";

            string dayOfWeekName = DayOfWeekNames[(int)gregorianDate.DayOfWeek];
            string? parashaName = ParashaService.GetParashaName(gregorianDate);

            string topLine = parashaName is null
                ? dayOfWeekName
                : $"{dayOfWeekName}, {parashaName}";

            return new HebrewDateDisplay
            {
                TopLine = topLine,
                BottomLine = bottomLine,
            };
        }

        /// <summary>שם החודש העברי (1-מבוסס, לפי מספור .NET HebrewCalendar: תשרי=1) - נחשף לשימוש חוזר (למשל בבורר התאריך העברי).</summary>
        public static string GetMonthName(int hebrewMonth, bool isLeapYear)
        {
            string[] monthNames = isLeapYear ? MonthNamesLeapYear : MonthNamesCommonYear;
            if (hebrewMonth < 1 || hebrewMonth > monthNames.Length)
            {
                return string.Empty;
            }

            return monthNames[hebrewMonth - 1];
        }
    }
}
