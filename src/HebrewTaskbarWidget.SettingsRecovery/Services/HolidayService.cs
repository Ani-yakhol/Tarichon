using System;
using System.Globalization;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מחזיר את שם החג/המועד העברי (אם יש) עבור תאריך לועזי נתון - מיועד
    /// לתצוגה החופשית מעל שולחן העבודה (סעיף "הצג חגים ומועדים").
    ///
    /// לפי דרישה מפורשת: הרשימה **אינה** כוללת את יום הזיכרון (ד'/לעיתים
    /// ג'/ה' באייר) ואת יום העצמאות (ה'/לעיתים ד'/ו' באייר) - ימים אלה
    /// מוחרגים בכוונה, ולא רק נשכחו.
    ///
    /// החישוב מבוסס על System.Globalization.HebrewCalendar המובנה של .NET
    /// (אותו לוח משמש גם ב-HebrewDateFormatter) - ולכן מדוייק לכל שנה, ולא
    /// תלוי בטבלה חיצונית כמו ParashaService.
    ///
    /// הערה: תאריכי הצומות/חגים כאן הם לפי המקובל בישראל (יום טוב אחד; אין
    /// "יום טוב שני של גלויות"). דחיית צום שחל בשבת (י"ז בתמוז, צום גדליה,
    /// עשרה בטבת, תענית אסתר) ליום ראשון/חמישי מיושמת לפי הכללים המקובלים.
    /// </summary>
    public static class HolidayService
    {
        private static readonly HebrewCalendar Calendar = new();

        // מספור החודשים העברי לפי .NET: תשרי=1, חשון=2, כסלו=3, טבת=4, שבט=5,
        // אדר(-א' בשנה מעוברת)=6, אדר ב'=7 (בשנה מעוברת בלבד), ניסן=7/8, אייר=8/9,
        // סיון=9/10, תמוז=10/11, אב=11/12, אלול=12/13.
        public static string? GetHolidayName(DateTime gregorianDate)
        {
            DateTime date = gregorianDate.Date;

            int day = Calendar.GetDayOfMonth(date);
            int month = Calendar.GetMonth(date);
            int year = Calendar.GetYear(date);
            bool isLeap = Calendar.IsLeapYear(year);
            DayOfWeek dow = date.DayOfWeek;

            // בשנה מעוברת "אדר" הרגיל (המשמש לפורים) הוא אדר ב' = חודש 7.
            // בשנה פשוטה, אדר הוא חודש 6.
            int adarForPurim = isLeap ? 7 : 6;

            // תשרי (1)
            if (month == 1)
            {
                if (day is 1 or 2) return "ראש השנה";
                if (day == 3 && dow != DayOfWeek.Saturday) return "צום גדליה";
                if (day == 4 && dow == DayOfWeek.Sunday) return "צום גדליה"; // נדחה משבת
                if (day == 10) return "יום כיפור";
                if (day is >= 15 and <= 21) return day == 15 ? "סוכות" : "חול המועד סוכות";
                if (day == 22) return "שמיני עצרת";
                if (day == 23) return "שמחת תורה";
                return null;
            }

            // כסלו (3) / טבת (4) - חנוכה (כ"ה בכסלו עד ב'/ג' בטבת, תלוי אורך כסלו)
            if (month == 3 && day >= 25)
            {
                return "חנוכה";
            }

            if (month == 4)
            {
                int daysInKislev = Calendar.GetDaysInMonth(year, 3);
                int chanukahDaysInTevet = 8 - (daysInKislev - 24);
                if (day <= chanukahDaysInTevet)
                {
                    return "חנוכה";
                }

                if (day == 10 && dow != DayOfWeek.Saturday) return "עשרה בטבת";
                if (day == 11 && dow == DayOfWeek.Sunday) return "עשרה בטבת"; // נדחה משבת (נדיר)
                return null;
            }

            // שבט (5)
            if (month == 5)
            {
                if (day == 15) return "ט\"ו בשבט";
                return null;
            }

            // אדר א' (בשנה מעוברת בלבד, חודש 6 כאשר יש גם אדר ב')
            if (isLeap && month == 6)
            {
                if (day == 14) return "פורים קטן";
                if (day == 15) return "שושן פורים קטן";
                return null;
            }

            // אדר (הרגיל, המשמש לפורים בפועל)
            if (month == adarForPurim)
            {
                if (day == 13 && dow != DayOfWeek.Saturday) return "תענית אסתר";
                if (day == 11 && dow == DayOfWeek.Thursday) return "תענית אסתר"; // מוקדם ליום ה' כשי"ג חל בשבת
                if (day == 14) return "פורים";
                if (day == 15) return "שושן פורים";
                return null;
            }

            // ניסן - חודש 7 בשנה פשוטה, 8 בשנה מעוברת
            int nisan = isLeap ? 8 : 7;
            if (month == nisan)
            {
                if (day is >= 15 and <= 21) return day is 15 or 21 ? "פסח" : "חול המועד פסח";
                return null;
            }

            // אייר - חודש 8/9 (מוחרגים בכוונה: יום הזיכרון ויום העצמאות)
            int iyar = isLeap ? 9 : 8;
            if (month == iyar)
            {
                if (day == 18) return "ל\"ג בעומר";
                return null;
            }

            // סיון - חודש 9/10
            int sivan = isLeap ? 10 : 9;
            if (month == sivan)
            {
                if (day == 6) return "שבועות";
                return null;
            }

            // תמוז - חודש 10/11
            int tammuz = isLeap ? 11 : 10;
            if (month == tammuz)
            {
                if (day == 17 && dow != DayOfWeek.Saturday) return "י\"ז בתמוז";
                if (day == 18 && dow == DayOfWeek.Sunday) return "י\"ז בתמוז"; // נדחה משבת
                return null;
            }

            // אב - חודש 11/12
            int av = isLeap ? 12 : 11;
            if (month == av)
            {
                if (day == 9 && dow != DayOfWeek.Saturday) return "תשעה באב";
                if (day == 10 && dow == DayOfWeek.Sunday) return "תשעה באב"; // נדחה משבת
                if (day == 15) return "ט\"ו באב";
                return null;
            }

            return null;
        }

        /// <summary>
        /// האם התאריך הלועזי הנתון הוא ערב שבת, או ערב אחד מהחגים הבאים
        /// (שבהם נהוג להדליק נרות בערבו): ראש השנה (שני הימים), יום כיפור,
        /// סוכות (היום הראשון), שמיני עצרת, פסח (היום הראשון והאחרון),
        /// שבועות. נבדק בנפרד מ-GetHolidayName (שמיועד לתצוגה טקסטואלית
        /// בלבד) כדי לא להיות תלוי בעמימות שם התצוגה עצמו.
        /// </summary>
        public static bool IsErevCandleLighting(DateTime gregorianDate)
        {
            DateTime date = gregorianDate.Date;

            if (date.DayOfWeek == DayOfWeek.Friday)
            {
                return true;
            }

            DateTime tomorrow = date.AddDays(1);
            int day = Calendar.GetDayOfMonth(tomorrow);
            int month = Calendar.GetMonth(tomorrow);
            int year = Calendar.GetYear(tomorrow);
            bool isLeap = Calendar.IsLeapYear(year);

            // תשרי (1): ראש השנה (א'-ב'), יום כיפור (י'), סוכות א' (ט"ו), שמיני עצרת (כ"ב)
            if (month == 1 && day is 1 or 2 or 10 or 15 or 22)
            {
                return true;
            }

            // ניסן: פסח - יום ראשון (ט"ו) ויום אחרון (כ"א)
            int nisan = isLeap ? 8 : 7;
            if (month == nisan && day is 15 or 21)
            {
                return true;
            }

            // סיון: שבועות (ו')
            int sivan = isLeap ? 10 : 9;
            if (month == sivan && day == 6)
            {
                return true;
            }

            return false;
        }
    }
}
