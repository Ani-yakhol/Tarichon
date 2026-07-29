using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HebrewTaskbarWidget.Interop;
using Microsoft.Win32;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מנגנון חלופי ל"הסתרת" תצוגת התאריך/שעה המקורית של Windows, המיועד
    /// ל-Windows 11 בלבד (ראו TaskbarClockLocator.IsWindows11) - שם ההסתרה
    /// "האמיתית" (ShowWindow על חלון השעון עצמו, וגם ההגדרה המדיניות-ברישום
    /// + הפעלה מחדש של Explorer עבור "צמצום הרווח") לא תמיד עובדות, כי
    /// חלק מגרסאות/builds של Windows 11 יישמו את תצוגת השעון מחדש כרכיב
    /// XAML/Composition שלא בהכרח מכבד את הקריאות/הערכים האלה יותר.
    ///
    /// הפתרון כאן שונה באופן יסודי: **לא מנסים לגעת בשעון האמיתי בכלל** -
    /// פשוט "מכסים" אותו חזותית עם חלון צף, טופ-מוסט, בצבע שמקרב (מיטב
    /// מאמץ - לא בהכרח מדוייק, למשל אם יש ערכת נושא/שקיפות מותאמת אישית)
    /// את רקע שורת המשימות. גישה זו אינה תלויה כלל בהתנהגות הפנימית
    /// הלא-מתועדת של explorer.exe, ולכן צפויה לעבוד בעקביות בכל build של
    /// Windows 11 - אך חשוב להבהיר את המגבלה שלה: היא **מסתירה** את השעון
    /// חזותית, אך אינה יכולה "לצמצם את הרווח" באמת (לגרום ל-Explorer
    /// לפנות בפועל את השטח לשימוש סמלים אחרים) - זה נשאר בלתי-אפשרי בלי
    /// שיתוף פעולה של Explorer עצמו. תיבת הכיסוי מכסה מעט שטח נוסף
    /// (ראו ExtraCoverPixels) כדי לפחות ליצור רושם חזותי דומה.
    /// </summary>
    public static class TaskbarClockCoverService
    {
        private const int TrackingIntervalMs = 500;

        /// <summary>שטח נוסף (בפיקסלים פיזיים) שהכיסוי "גולש" אל מעבר לגבולות השעון עצמו בלבד - קירוב חזותי בלבד ל"צמצום הרווח", לא צמצום אמיתי.</summary>
        private const int ExtraCoverPixels = 4;

        private static ClockCoverWindow? _coverWindow;
        private static DispatcherTimer? _trackingTimer;
        private static bool _isActive;

        /// <summary>מפעיל/מכבה את מנגנון הכיסוי החזותי. קריאה חוזרת עם אותו ערך היא no-op זול.</summary>
        public static void SetActive(bool active)
        {
            if (active == _isActive)
            {
                return;
            }

            _isActive = active;

            if (active)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        private static void Start()
        {
            if (_coverWindow is null)
            {
                _coverWindow = new ClockCoverWindow();
                _coverWindow.Show();
            }

            UpdateCoverPositionAndColor();

            if (_trackingTimer is null)
            {
                _trackingTimer = new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromMilliseconds(TrackingIntervalMs),
                };
                _trackingTimer.Tick += (_, _) => UpdateCoverPositionAndColor();
                _trackingTimer.Start();
            }
        }

        private static void Stop()
        {
            _trackingTimer?.Stop();
            _trackingTimer = null;

            _coverWindow?.Close();
            _coverWindow = null;
        }

        private static void UpdateCoverPositionAndColor()
        {
            if (_coverWindow is null)
            {
                return;
            }

            if (!TaskbarClockLocator.TryLocateClock(out RECT clockRect))
            {
                // לא נמצא כרגע (למשל בזמן הפעלה מחדש של Explorer) - מרחיקים
                // את חלון הכיסוי מהתצוגה עד שהמיקום ייקבע שוב.
                _coverWindow.Left = -10000;
                _coverWindow.Top = -10000;
                return;
            }

            double dpiScale = TaskbarClockLocator.GetTaskbarDpiScale();
            if (dpiScale <= 0)
            {
                dpiScale = 1.0;
            }

            bool isRtl = TaskbarClockLocator.IsTaskbarRightToLeft();

            // מוסיפים מעט שטח כיסוי נוסף מהצד ה"פנימי" (לכיוון שאר סמלי
            // המגש) - קירוב חזותי בלבד ל"צמצום הרווח", ראו הערה בראש הקובץ.
            int extraPhysical = (int)Math.Round(ExtraCoverPixels * dpiScale);
            int left = isRtl ? clockRect.Left : clockRect.Left - extraPhysical;
            int width = clockRect.Width + extraPhysical;

            _coverWindow.Left = left / dpiScale;
            _coverWindow.Top = clockRect.Top / dpiScale;
            _coverWindow.Width = Math.Max(1, width / dpiScale);
            _coverWindow.Height = Math.Max(1, clockRect.Height / dpiScale);
            _coverWindow.Background = new SolidColorBrush(GetApproximateTaskbarColor());

            if (!_coverWindow.IsVisible)
            {
                _coverWindow.Show();
            }

            // דוחפים אותו לקדמת סדר-השכבות שוב ושוב - כמו הוידג'ט הראשי -
            // כדי לנצח את שורת המשימות במאבקי ה-Topmost.
            if (PresentationSource.FromVisual(_coverWindow) is HwndSource source)
            {
                NativeMethods.SetWindowPos(
                    source.Handle,
                    NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }
        }

        /// <summary>קירוב (מיטב-מאמץ) לצבע רקע שורת המשימות הנוכחי - שחור/אפור כהה אם ערכת הנושא כהה (ברירת המחדל הרווחת ב-Windows 11), או בהיר-אפרפר אם ערכת הנושא בהירה.</summary>
        private static Color GetApproximateTaskbarColor()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: false);

                bool isLight = key?.GetValue("SystemUsesLightTheme") is int value && value == 1;
                return isLight ? Color.FromRgb(0xF3, 0xF3, 0xF3) : Color.FromRgb(0x20, 0x20, 0x20);
            }
            catch
            {
                return Color.FromRgb(0x20, 0x20, 0x20);
            }
        }
    }
}
