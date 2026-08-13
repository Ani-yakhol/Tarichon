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
    /// היום, או עם צאת הכוכבים (לפי ברירת המחדל המבוססת-מעלות, או לפי
    /// הערך שהוגדר בהגדרות - ראו AppSettings.TzeitHakochavimMinutesAfterSunset).
    /// שימו לב: זה משפיע רק על *התצוגה* של התאריך העברי הנוכחי (ושם החג,
    /// אם יש) - לא על חישוב זמני היום עצמם, שנשארים תמיד לפי היום הלועזי/
    /// האסטרונומי בפועל.
    /// </summary>
    public static class HebrewDayRolloverService
    {
        /// <summary>
        /// מחזירה את התאריך הלועזי ה"אפקטיבי" לחישוב התאריך העברי המוצג כרגע.
        /// כברירת מחדל (מעבר בחצות) זהה תמיד לתאריך הלועזי הנוכחי. במצבי
        /// "מעבר בשקיעה"/"מעבר בצאת הכוכבים", אם כבר עבר הזמן הרלוונטי של
        /// היום - מחזירה את המחר, כדי שהתאריך העברי המוצג יתקדם כבר אז,
        /// ולא רק מחצות הלילה.
        /// </summary>
        public static DateTime GetEffectiveHebrewDate(DateTime now, AppSettings settings, GeoLocation location)
        {
            DateTime today = now.Date;

            if (settings.HebrewDayChangeMode == HebrewDayChangeMode.Midnight)
            {
                return today;
            }

            IReadOnlyList<ZmanEntry> entries = ZmanimCalendar.Calculate(
                today, location,
                SettingsService.Current.CandleLightingMinutesBeforeSunset,
                SettingsService.Current.TzeitHakochavimMinutesAfterSunset,
                SettingsService.Current.DefaultZmanCalculationMethod,
                SettingsService.Current.ZmanCustomizations,
                SettingsService.Current.ZmanDuplicateRows);

            string relevantZmanName = settings.HebrewDayChangeMode == HebrewDayChangeMode.AtTzeitHakochavim
                ? ZmanimCalendar.NameTzeitHakochavim
                : ZmanimCalendar.NameShkia;

            ZmanEntry? relevantZman = entries.FirstOrDefault(z => z.Key == relevantZmanName);

            if (relevantZman?.Time is DateTime relevantTime && now >= relevantTime)
            {
                return today.AddDays(1);
            }

            return today;
        }
    }
}
