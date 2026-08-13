using System;
using Microsoft.Win32;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מפעיל/מבטל הפעלה אוטומטית עם עליית Windows, באמצעות מפתח ה-Run
    /// הרגיל של המשתמש הנוכחי בלבד (HKCU) - אינו דורש הרשאות מנהל, ומשפיע
    /// רק על המשתמש המחובר כרגע (לא על כל המשתמשים במחשב).
    /// </summary>
    public static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "HebrewTaskbarWidget";

        /// <summary>
        /// דגל שורת-פקודה שנוסף להפעלה האוטומטית (ולה בלבד) - מאפשר לתוכנה
        /// לזהות בזמן ריצה שהיא עולה כחלק מעליית Windows עצמה, ולא הופעלה
        /// ידנית ע"י המשתמש (למשל לחיצה כפולה על קובץ ההרצה). ראו IsAutoStartLaunch.
        /// </summary>
        public const string AutoStartArgument = "--autostart";

        /// <summary>נתיב קובץ ההרצה הנוכחי (exe), משמש כערך שנשמר במפתח ה-Run.</summary>
        private static string ExecutablePath =>
            Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

        public static bool IsEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            object? value = key?.GetValue(ValueName);
            return value is string path && path.Contains(ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }

        public static void SetEnabled(bool enabled)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                                      ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            if (key is null)
            {
                return;
            }

            if (enabled)
            {
                // מוקף במרכאות כדי לתמוך בנתיבים עם רווחים, ועם דגל ה"הפעלה
                // אוטומטית" שמוסיפים רק כאן (הפעלה ידנית של קובץ ה-exe לעולם
                // לא כוללת אותו).
                key.SetValue(ValueName, $"\"{ExecutablePath}\" {AutoStartArgument}", RegistryValueKind.String);
            }
            else
            {
                if (key.GetValue(ValueName) is not null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }
            }
        }

        /// <summary>האם התהליך הנוכחי עלה עם דגל ה"הפעלה אוטומטית" (כלומר: כחלק מעליית Windows עצמה, לא הפעלה ידנית).</summary>
        public static bool IsAutoStartLaunch()
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, AutoStartArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
