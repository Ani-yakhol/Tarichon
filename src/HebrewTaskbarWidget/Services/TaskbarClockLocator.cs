using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;
using HebrewTaskbarWidget.Interop;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מאתר את מיקום ומידות "כפתור" התאריך/שעה במגש המערכת (System Tray) שבשורת
    /// המשימות הראשית, כדי שנוכל להצמיד את הוידג'ט שלנו אליו.
    ///
    /// הערה: הגרסה הנוכחית תומכת בשורת המשימות הראשית (הצג הראשי) בלבד.
    /// תמיכה בשורות משימות על צגים משניים (Shell_SecondaryTrayWnd) מתוכננת
    /// לגרסה עתידית - ראו CHANGELOG.md.
    /// </summary>
    public static class TaskbarClockLocator
    {
        /// <summary>
        /// מנסה לאתר את מלבן התצוגה (בפיקסלים פיזיים, לא DIP) של שעון/תאריך המערכת.
        /// מחזיר true אם האיתור הצליח.
        /// </summary>
        public static bool TryLocateClock(out RECT clockRect)
        {
            clockRect = default;

            IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (trayWnd == IntPtr.Zero)
            {
                return false;
            }

            IntPtr notifyWnd = NativeMethods.FindWindowEx(trayWnd, IntPtr.Zero, "TrayNotifyWnd", null);

            // "TrayClockWClass" הוא חלון-הנגישות של השעון, קיים גם ב-Windows 11,
            // אך תחת המעטפת של Windows 11 (מבוססת XAML Islands) הוא לרוב מקונן
            // עמוק יותר בעץ החלונות (למשל מתחת ל-Windows.UI.Composition.DesktopWindowContentBridge
            // ו-DirectUIHWND), ולא כבן ישיר של TrayNotifyWnd. FindWindowEx בודק רק
            // בנים ישירים, ולכן חיפוש כזה נכשל ב-Windows 11 - וזו הייתה הסיבה
            // לכך שהוידג'ט "נפל" למלבן הרחב של כל אזור ההתראות (הכולל את כל
            // סמלי המגש) במקום למלבן הצר של השעון בלבד, וכתוצאה מכך הופיע
            // במקום לא נכון (לרוב סמוך לסמלי ההתראות, ולעיתים חלקית מחוץ למסך).
            //
            // הפתרון: חיפוש רקורסיבי (EnumChildWindows סורק את כל עץ הבנים,
            // לא רק רמה אחת) בתוך כל שורת המשימות.
            IntPtr clockWnd = FindDescendantByClassName(trayWnd, "TrayClockWClass");

            // סדר עדיפויות: שעון ספציפי -> אזור ההתראות כולו -> שורת המשימות כולה
            IntPtr target = clockWnd != IntPtr.Zero
                ? clockWnd
                : (notifyWnd != IntPtr.Zero ? notifyWnd : trayWnd);

            return NativeMethods.GetWindowRect(target, out clockRect);
        }

        /// <summary>
        /// מאתר ומחזיר את ה-HWND הגולמי של חלון שעון המערכת עצמו (TrayClockWClass)
        /// - בניגוד ל-<see cref="TryLocateClock"/> שמחזיר רק מלבן ועשוי ליפול חזרה
        /// לאזור ההתראות/שורת המשימות כולה אם השעון הספציפי לא נמצא. משמש
        /// להסתרה/הצגה ישירה (ShowWindow) של השעון עצמו, ולהעברת קליקים אליו.
        /// </summary>
        public static bool TryLocateClockWindow(out IntPtr clockWnd)
        {
            clockWnd = IntPtr.Zero;

            IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (trayWnd == IntPtr.Zero)
            {
                return false;
            }

            clockWnd = FindDescendantByClassName(trayWnd, "TrayClockWClass");
            return clockWnd != IntPtr.Zero;
        }

        /// <summary>
        /// מאתר את מלבן התצוגה (בפיקסלים פיזיים) של כפתור "הצג סמלים מוסתרים"
        /// (החץ "^") במגש המערכת - כדי שנוכל להצמיד את הוידג'ט אליו (כמו
        /// BatteryBar), כולל מעקב אחרי תזוזתו כשמראים/מסתירים סמלים.
        ///
        /// שתי אסטרטגיות, לפי סדר עדיפות: (1) UI Automation - אמינה יותר,
        /// כי היא עובדת ברמת עץ הנגישות ולא ברמת חלונות Win32 גולמיים,
        /// ולכן ממשיכה לעבוד גם ב-Windows 11 שבו חלק ניכר מאזור ההתראות
        /// מבוסס XAML Islands ואינו חושף HWND נפרד לכל כפתור/סמל (מה שגרם
        /// לשיטת Win32 הגולמית להיכשל לגמרי במציאת הכפתור, ובכך לוידג'ט
        /// "לרחף" במקום שגוי לגמרי - הבעיה שדווחה בפועל). (2) גיבוי: חיפוש
        /// Win32 גולמי (FindWindowEx/EnumChildWindows) - עדיין רלוונטי
        /// בגרסאות Windows 10 ישנות יותר, ואם UI Automation נכשלת מסיבה כלשהי.
        /// </summary>
        public static bool TryLocateChevronButton(out RECT chevronRect)
        {
            if (TryLocateChevronViaUIAutomation(out chevronRect))
            {
                return true;
            }

            return TryLocateChevronViaWin32(out chevronRect);
        }

        private static bool TryLocateChevronViaUIAutomation(out RECT chevronRect)
        {
            chevronRect = default;

            try
            {
                IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
                if (trayWnd == IntPtr.Zero)
                {
                    return false;
                }

                AutomationElement? trayElement = AutomationElement.FromHandle(trayWnd);
                if (trayElement is null)
                {
                    return false;
                }

                var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                AutomationElementCollection buttons = trayElement.FindAll(TreeScope.Descendants, condition);

                foreach (AutomationElement button in buttons)
                {
                    string name;
                    string automationId;

                    try
                    {
                        name = button.Current.Name ?? string.Empty;
                        automationId = button.Current.AutomationId ?? string.Empty;
                    }
                    catch
                    {
                        continue; // אלמנט שהתפרק/הוסר בדיוק ברגע הבדיקה - מדלגים
                    }

                    // שמות אפשריים לכפתור "הצג סמלים מוסתרים" - אנגלית, עברית
                    // (כמה ניסוחים אפשריים בתרגום), ו-AutomationId ידועים.
                    bool looksLikeChevron =
                        name.IndexOf("hidden icon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("מוסתר", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("נסתר", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        automationId.Equals("SystemTrayIcon", StringComparison.OrdinalIgnoreCase) ||
                        automationId.IndexOf("Overflow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        automationId.IndexOf("Chevron", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!looksLikeChevron)
                    {
                        continue;
                    }

                    System.Windows.Rect bounds;
                    try
                    {
                        bounds = button.Current.BoundingRectangle;
                    }
                    catch
                    {
                        continue;
                    }

                    if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
                    {
                        continue;
                    }

                    chevronRect = new RECT
                    {
                        Left = (int)Math.Round(bounds.Left),
                        Top = (int)Math.Round(bounds.Top),
                        Right = (int)Math.Round(bounds.Right),
                        Bottom = (int)Math.Round(bounds.Bottom),
                    };
                    return true;
                }
            }
            catch
            {
                // UI Automation עלולה להיכשל מכמה סיבות (תזמון, הרשאות, thread
                // apartment state) - לא קריטי, נופלים חזרה לשיטת ה-Win32 הגולמית.
            }

            return false;
        }

        private static bool TryLocateChevronViaWin32(out RECT chevronRect)
        {
            chevronRect = default;

            IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (trayWnd == IntPtr.Zero)
            {
                return false;
            }

            IntPtr notifyWnd = NativeMethods.FindWindowEx(trayWnd, IntPtr.Zero, "TrayNotifyWnd", null);
            if (notifyWnd == IntPtr.Zero)
            {
                return false;
            }

            // כפתור החץ הוא ה-Button היחיד (בד"כ) בעץ הבנים של TrayNotifyWnd -
            // חיפוש רקורסיבי (כמו עבור השעון) כדי לתמוך גם במבנה המקונן יותר
            // של Windows 11 (במידה וקיים HWND נפרד בכלל - ראו הערה למעלה).
            IntPtr chevronWnd = FindDescendantByClassName(notifyWnd, "Button");
            if (chevronWnd == IntPtr.Zero)
            {
                return false;
            }

            return NativeMethods.GetWindowRect(chevronWnd, out chevronRect);
        }

        /// <summary>
        /// האם המערכת היא Windows 11 (ולא 10)? שתיהן מדווחות "10.0.x" ב-
        /// Environment.OSVersion (Windows לא שינה את מספר הגרסה הראשי), אז
        /// הדרך התיכנותית הרשמית/מומלצת להבחין ביניהן היא לפי מספר ה-Build:
        /// Windows 11 מתחיל מ-build 22000 ומעלה.
        /// </summary>
        public static bool IsWindows11()
        {
            return Environment.OSVersion.Version.Build >= 22000;
        }

        /// <summary>
        /// האם שורת המשימות (ובעצם כל שכבת ה-Shell) מוגדרת בפריסת ימין-לשמאל
        /// (RTL) - כמו בוינדוס בעברית. קובע לאיזה צד של כפתור החץ "^" יש
        /// להצמיד את הוידג'ט: ב-RTL מצמידים לצד הימני של הכפתור, וב-LTR
        /// (למשל וינדוס באנגלית) לצד השמאלי שלו - כדי לחקות במדוייק את
        /// המיקום שבו סמל מגש חדש/גלוי היה מופיע לצד הכפתור.
        /// </summary>
        public static bool IsTaskbarRightToLeft()
        {
            IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (trayWnd == IntPtr.Zero)
            {
                return false;
            }

            int exStyle = NativeMethods.GetWindowLong(trayWnd, NativeMethods.GWL_EXSTYLE);
            return (exStyle & NativeMethods.WS_EX_LAYOUTRTL) != 0;
        }

        /// <summary>
        /// מחפש רקורסיבית (בכל עץ הבנים, לא רק ברמה אחת) חלון-צאצא לפי שם מחלקה מדוייק.
        /// </summary>
        private static IntPtr FindDescendantByClassName(IntPtr root, string targetClassName)
        {
            IntPtr found = IntPtr.Zero;
            var visited = new HashSet<IntPtr>();

            bool Callback(IntPtr hWnd, IntPtr _)
            {
                if (!visited.Add(hWnd))
                {
                    return true; // הגנה מפני לולאה אינסופית תיאורטית
                }

                var sb = new StringBuilder(256);
                if (NativeMethods.GetClassName(hWnd, sb, sb.Capacity) > 0 &&
                    string.Equals(sb.ToString(), targetClassName, StringComparison.Ordinal))
                {
                    found = hWnd;
                    return false; // עצור את החיפוש - מצאנו
                }

                return true; // המשך לחלון הבא
            }

            NativeMethods.EnumChildWindows(root, Callback, IntPtr.Zero);
            return found;
        }

        /// <summary>
        /// מחזיר את מלבן שורת המשימות הראשית כולה (בפיקסלים פיזיים) - משמש
        /// עבור מצב "מרחק מותאם אישית מקצה שורת המשימות", שאינו תלוי במיקום
        /// השעון אלא רק בקצה הימני/שמאלי של שורת המשימות עצמה.
        /// </summary>
        public static bool TryGetTaskbarRect(out RECT taskbarRect)
        {
            taskbarRect = default;

            IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (trayWnd == IntPtr.Zero)
            {
                return false;
            }

            return NativeMethods.GetWindowRect(trayWnd, out taskbarRect);
        }

        /// <summary>
        /// מחזיר את יחס ה-DPI (Scale Factor) של המסך שעליו נמצאת שורת המשימות,
        /// כדי להמיר בין פיקסלים פיזיים ליחידות WPF (DIP, בבסיס 96).
        /// </summary>
        public static double GetTaskbarDpiScale()
        {
            IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (trayWnd == IntPtr.Zero)
            {
                return 1.0;
            }

            uint dpi = NativeMethods.GetDpiForWindow(trayWnd);
            if (dpi == 0)
            {
                return 1.0;
            }

            return dpi / 96.0;
        }

        /// <summary>
        /// מחזיר את גבולות אזור העבודה (Work Area, בפיקסלים פיזיים) של המסך
        /// שעליו נמצאת שורת המשימות, כדי שנוכל למקם ולהצמיד את הוידג'ט בתוך
        /// גבולות המסך ולא לתת לו "לברוח" מעבר לקצה הימני/שמאלי.
        /// </summary>
        public static bool TryGetTaskbarMonitorWorkArea(out RECT workArea)
        {
            workArea = default;

            IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (trayWnd == IntPtr.Zero)
            {
                return false;
            }

            IntPtr monitor = NativeMethods.MonitorFromWindow(trayWnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
            {
                return false;
            }

            var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
            if (!NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                return false;
            }

            workArea = info.rcWork;
            return true;
        }

        /// <summary>
        /// מחזיר את גבולות המסך **המלאים** (rcMonitor, לא rcWork) שעליו נמצאת
        /// שורת המשימות - בניגוד ל-<see cref="TryGetTaskbarMonitorWorkArea"/>,
        /// זה כולל גם את שטח שורת המשימות עצמה. משמש למצב "מיקום חופשי"
        /// (גרירה), כדי שיהיה אפשר להניח את הוידג'ט בכל מקום על המסך - כולל
        /// בתוך גובה שורת המשימות - ולא רק באזור העבודה שמחוצה לה.
        /// </summary>
        public static bool TryGetTaskbarMonitorFullRect(out RECT monitorRect)
        {
            monitorRect = default;

            IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (trayWnd == IntPtr.Zero)
            {
                return false;
            }

            IntPtr monitor = NativeMethods.MonitorFromWindow(trayWnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
            {
                return false;
            }

            var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
            if (!NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                return false;
            }

            monitorRect = info.rcMonitor;
            return true;
        }
    }
}
