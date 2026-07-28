using System;
using System.Collections.Generic;
using System.Linq;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// קובע מתי "היום" מבחינת התאריך העברי המוצג בפועל (בוידג'ט ובתצוגת
    /// שולחן העבודה) מתקדם ליום העברי הבא: כברירת מחדל בחצות הלילה (00:00,
    /// כמו התאריך הלועזי הרגיל) - או, אם כך הוגדר, כבר עם שקיעת החמה של
    /// היום. שימו לב: זה משפיע רק על *התצוגה* של התאריך העברי הנוכחי (ושם
    /// החג, אם יש) - לא על חישוב זמני היום עצמם, שנשארים תמיד לפי היום
    /// הלועזי/האסטרונומי בפועל.
    /// </summary>
    public static class HebrewDayRolloverService
    {
        /// <summary>
        /// מחזירה את התאריך הלועזי ה"אפקטיבי" לחישוב התאריך העברי המוצג כרגע.
        /// כברירת מחדל (מעבר בחצות) זהה תמיד לתאריך הלועזי הנוכחי. במצב
        /// "מעבר בשקיעה", אם כבר עברה שקיעת החמה של היום - מחזירה את המחר,
        /// כדי שהתאריך העברי המוצג יתקדם כבר משקיעה ולא רק מחצות הלילה.
        /// </summary>
        public static DateTime GetEffectiveHebrewDate(DateTime now, AppSettings settings, GeoLocation location)
        {
            DateTime today = now.Date;

            if (settings.HebrewDayChangeMode != HebrewDayChangeMode.AtSunset)
            {
                return today;
            }

            IReadOnlyList<ZmanEntry> entries = ZmanimCalendar.Calculate(today, location);
            ZmanEntry? shkia = entries.FirstOrDefault(z => z.Name == ZmanimCalendar.NameShkia);

            if (shkia?.Time is DateTime shkiaTime && now >= shkiaTime)
            {
                return today.AddDays(1);
            }

            return today;
        }
    }
}
