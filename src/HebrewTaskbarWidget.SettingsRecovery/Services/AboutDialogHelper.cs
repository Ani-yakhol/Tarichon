using System.Reflection;
using System.Windows;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// חלוניות משותפות ("אודות", "מה חדש") - זהות בדיוק בין תפריט ההקשר של
    /// הוידג'ט בשורת המשימות (MainWindow), תפריט ההקשר של תצוגת שולחן
    /// העבודה (DesktopOverlayWindow), ולשונית "כללי" בפאנל ההגדרות, כדי
    /// שלא לשכפל את הטקסט/הפורמט בכמה מקומות.
    /// </summary>
    public static class AboutDialogHelper
    {
        public static void Show(Window owner)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.8.4";

            AppMessageBoxWindow.Show(
                $"תאריכון - וידג'ט תאריך עברי לשורת המשימות\nגרסה {version}\n" +
                "פותח ע\"י ישראל אמיתי\n\n" +
                "מציג את התאריך העברי, היום בשבוע ופרשת השבוע, צמוד לשעון המערכת.\n\n" +
                "נתוני פרשת השבוע: Hebcal.com (רישיון CC BY 4.0)\n" +
                "חישוב זמנים (KosherJava): Yitzchok/Zmanim, מאת אליהו הרשפלד (רישיון LGPL 2.1)\n\n" +
                $"מקור התוכנה: {UpdateService.RepositoryUrl}",
                "אודות",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                owner);
        }

        /// <summary>
        /// מציגה את חלונית "מה חדש בגרסה זו" - זהה בין כפתור "מה חדש" בלשונית
        /// "כללי" לבין כפתור "מה חדש" (אופציונלי) בהודעת "עדכון תוכנה זמין"
        /// (ראו MainWindow.PromptForUpdateIfAvailableAsync). לא עושה כלום אם
        /// אין כרגע עדכון זמין ידוע, או אם לעדכון הזה אין הערות שחרור בכלל.
        /// </summary>
        public static void ShowWhatsNew(Window owner)
        {
            UpdateInfo? available = UpdateService.AvailableUpdate;
            if (available is null || string.IsNullOrWhiteSpace(available.ReleaseNotes))
            {
                return;
            }

            AppMessageBoxWindow.Show(
                available.ReleaseNotes,
                $"מה חדש בגרסה {available.Version}",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                owner,
                largeScrollable: true);
        }
    }
}
