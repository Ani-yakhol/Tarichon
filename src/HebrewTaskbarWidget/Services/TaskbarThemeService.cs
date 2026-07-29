using System;
using Microsoft.Win32;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// קורא את הגדרת ערכת הנושא של שורת המשימות מהרישום, כדי להציג טקסט לבן על
    /// שורת משימות כהה, או טקסט שחור על שורת משימות בהירה - בדיוק כמו תצוגת
    /// השעה/תאריך המקורית של Windows.
    /// </summary>
    public static class TaskbarThemeService
    {
        private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>
        /// מחזיר true אם שורת המשימות בערכת נושא בהירה (ואז יש להשתמש בטקסט כהה),
        /// ו-false אם היא כהה (ברירת המחדל הנפוצה יותר ב-Windows 11 - טקסט לבן).
        /// </summary>
        public static bool IsTaskbarLightTheme()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
                if (key is null)
                {
                    return false; // ברירת מחדל: ערכת נושא כהה
                }

                // "SystemUsesLightTheme" קובע את צבע שורת המשימות/התחל (בניגוד ל-
                // "AppsUseLightTheme" שקובע את צבע חלונות היישומים).
                object? systemValue = key.GetValue("SystemUsesLightTheme");
                if (systemValue is int systemIntValue)
                {
                    return systemIntValue != 0;
                }

                // נפילה חינה: אם המפתח הספציפי לא קיים (גרסאות ישנות יותר של Windows 10)
                object? appsValue = key.GetValue("AppsUseLightTheme");
                if (appsValue is int appsIntValue)
                {
                    return appsIntValue != 0;
                }
            }
            catch
            {
                // אם קריאת הרישום נכשלה מכל סיבה שהיא - נמשיך עם ברירת המחדל
            }

            return false;
        }
    }
}
