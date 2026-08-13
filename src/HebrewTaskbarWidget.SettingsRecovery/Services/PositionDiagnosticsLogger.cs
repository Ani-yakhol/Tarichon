using System;
using System.IO;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// יומן אבחון תמידי (פעיל תמיד, אין הגדרה להפעלה/כיבוי) למעקב אחרי
    /// החלטות מיקום הוידג'ט בזמן אמת - נועד לאסוף נתונים אמיתיים על מקרי
    /// הבהוב/היעלמות עתידיים (אם עדיין יקרו), במקום להמשיך לנחש את הסיבה
    /// מרחוק בלי שום דרך לבדוק בפועל.
    ///
    /// אין הגדרה/מתג ייעודיים ללשונית "כללי" בכוונה - היומן קליל מאוד
    /// (כמה שורות טקסט קצרות בודדות בכל דקה לכל היותר, רק באירועים
    /// חריגים - לא בכל טיק) ומוגבל בגודל (ראו למטה), כך שאין סיבה אמיתית
    /// לכבות אותו. מיקום קובץ היומן מתועד ב-docs/מדריך-אבחון-מיקום.md.
    /// </summary>
    public static class PositionDiagnosticsLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HebrewTaskbarWidget",
            "position-diagnostics.log");

        private static readonly object WriteLock = new();

        public static void Log(string message)
        {
            try
            {
                lock (WriteLock)
                {
                    string? directory = Path.GetDirectoryName(LogFilePath);
                    if (directory is not null)
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // מגבילים את גודל הקובץ - אם הוא כבר גדול מדי, פשוט
                    // מתחילים אותו מחדש, כדי לא לצבור יומן שמתנפח לנצח.
                    if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > 2 * 1024 * 1024)
                    {
                        File.Delete(LogFilePath);
                    }

                    File.AppendAllText(LogFilePath, $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // אף פעם לא מפילים את התוכנה בגלל כתיבת יומן אבחון - זו
                // תכונת עזר לאיתור תקלות, לא תכונה קריטית.
            }
        }
    }
}
