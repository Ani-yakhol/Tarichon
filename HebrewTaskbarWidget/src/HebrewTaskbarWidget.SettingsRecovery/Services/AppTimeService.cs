using System;
using System.Globalization;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מקור אמת יחיד ל"עכשיו" בכל האפליקציה, וכן לפורמט תצוגת השעה (12/24 שעות,
    /// עם/בלי שניות) - כדי שכל מקומות התצוגה (הוידג'ט, תצוגת שולחן העבודה, לוח
    /// הזמנים, ההתראות) יהיו עקביים, ויכבדו יחד את הגדרות "תאריך ושעה" בלשונית
    /// "מיקום וזמנים".
    ///
    /// כאשר UseManualDateTime מופעל, "עכשיו" אינו מוקפא בתאריך/שעה שנבחרו -
    /// הזמן ממשיך לזרום קדימה כרגיל החל מהרגע שבו העוגן נקבע (ManualDateTimeBaseTicks
    /// יחד עם ManualDateTimeSetAtUtcTicks), בדיוק כמו שעון רגיל שכוונן ידנית.
    /// </summary>
    public static class AppTimeService
    {
        /// <summary>
        /// שיבוט של CultureInfo.CurrentCulture עם הלוח (Calendar) נעול במפורש
        /// ל-GregorianCalendar - לשימוש בכל מקום שאמור להציג תאריך "לועזי"
        /// (הוידג'ט, תצוגת שולחן העבודה) באמצעות DateTime.ToString(...).
        ///
        /// למה זה נחוץ: אם המשתמש הגדיר ב-Windows "לוח שנה" (Calendar) עברי
        /// בהגדרות אזור (Region) - הגדרה נפרדת לגמרי משפת התצוגה - אז
        /// CultureInfo.CurrentCulture.DateTimeFormat.Calendar עצמו הופך
        /// להיות HebrewCalendar. במצב כזה, DateTime.ToString(..., CultureInfo.CurrentCulture)
        /// "לועזי" בשמו בלבד היה בפועל מציג את התאריך *העברי* (גם עם פורמט
        /// מותאם-אישית כמו "dd/MM/yyyy", לא רק "d" הסטנדרטי - שדות היום/חודש/
        /// שנה תמיד נפתרים דרך ה-Calendar של ה-DateTimeFormatInfo האפקטיבי) -
        /// בדיוק בחלק שאמור תמיד להציג את הלועזי. שימוש בזה במקום ב-
        /// CultureInfo.CurrentCulture ישירות משמר את פורמט האזור (סדר יום/
        /// חודש/שנה, מפרידים וכו') אך מבטיח שהמספרים המוצגים תמיד יהיו לפי
        /// הלוח הלועזי, ללא תלות בהגדרת "לוח שנה" הנוכחית ב-Windows.
        ///
        /// מחושב מחדש בכל קריאה (לא נשמר סטטית) כדי לשקף נכון גם שינוי
        /// הגדרות אזוריות תוך כדי ריצת התוכנה (נדיר, אך זול - שיבוט CultureInfo בודד).
        /// </summary>
        public static CultureInfo GregorianDisplayCulture
        {
            get
            {
                var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                culture.DateTimeFormat.Calendar = new GregorianCalendar();
                return culture;
            }
        }

        /// <summary>"עכשיו" האפקטיבי - לפי שעון המחשב, או לפי התאריך/שעה הידניים אם הוגדרו.</summary>
        public static DateTime Now()
        {
            AppSettings settings = SettingsService.Current;

            if (settings.UseManualDateTime && settings.ManualDateTimeBaseTicks > 0 && settings.ManualDateTimeSetAtUtcTicks > 0)
            {
                var basis = new DateTime(settings.ManualDateTimeBaseTicks, DateTimeKind.Unspecified);
                var setAtUtc = new DateTime(settings.ManualDateTimeSetAtUtcTicks, DateTimeKind.Utc);
                TimeSpan elapsedSinceSet = DateTime.UtcNow - setAtUtc;
                return basis + elapsedSinceSet;
            }

            return DateTime.Now;
        }

        /// <summary>"היום" האפקטיבי (ללא רכיב שעה) - לפי אותו מקור כמו <see cref="Now"/>.</summary>
        public static DateTime Today() => Now().Date;

        /// <summary>מפרמט שעה לתצוגה בוידג'ט/שולחן העבודה - מכבד את הגדרות 12/24 שעות והצגת שניות.</summary>
        public static string FormatClockTime(DateTime time)
        {
            return FormatTime(time, includeSeconds: SettingsService.Current.ShowSecondsInTime);
        }

        /// <summary>מפרמט שעה לתצוגה בלוח הזמנים/בהתראות - מכבד 12/24 שעות, אך לעולם ללא שניות (לא רלוונטי לזמנים הלכתיים).</summary>
        public static string FormatZmanTime(DateTime time)
        {
            return FormatTime(time, includeSeconds: false);
        }

        private static string FormatTime(DateTime time, bool includeSeconds)
        {
            bool use12Hour = SettingsService.Current.Use12HourFormat;

            string pattern = use12Hour
                ? (includeSeconds ? "h:mm:ss tt" : "h:mm tt")
                : (includeSeconds ? "HH:mm:ss" : "HH:mm");

            return time.ToString(pattern, CultureInfo.InvariantCulture);
        }
    }
}
