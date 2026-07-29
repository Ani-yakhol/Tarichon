using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using HebrewTaskbarWidget.Interop;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// חלון "מכסה" מינימלי, ללא שום אינטראקציה (לא ניתן להתמקד בו, לא
    /// מופיע ב-Alt+Tab), שמוצג בדיוק מעל מלבן תצוגת התאריך/שעה המקורית
    /// של Windows - ראו הסבר מלא ב-Services/TaskbarClockCoverService.cs
    /// על מתי ולמה משתמשים בו (בעיקר: Windows 11, שם הסתרה "אמיתית" של
    /// חלון השעון לא תמיד עובדת). "מיטב מאמץ" בלבד - הצבע הוא קירוב לרקע
    /// שורת המשימות (בהיר/כהה לפי ערכת הנושא), לא בהכרח זהה בדיוק אם
    /// למשתמש יש ערכת נושא/שקיפות מותאמת אישית.
    /// </summary>
    internal sealed class ClockCoverWindow : Window
    {
        public ClockCoverWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            Focusable = false;
            ShowActivated = false;
            Background = Brushes.Black;
            Left = -10000;
            Top = -10000;
            Width = 1;
            Height = 1;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            if (helper.Handle == IntPtr.Zero)
            {
                return;
            }

            int exStyle = NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE, exStyle);
        }
    }
}
