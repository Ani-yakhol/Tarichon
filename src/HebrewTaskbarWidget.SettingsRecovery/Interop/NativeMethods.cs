using System;
using System.Runtime.InteropServices;

namespace HebrewTaskbarWidget.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }

    /// <summary>נציג את פונקציית ה-Callback הנדרשת ל-EnumChildWindows.</summary>
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    /// <summary>
    /// עטיפות P/Invoke עבור פונקציות Win32 הדרושות כדי לאתר את חלון שעון המערכת
    /// בשורת המשימות, לקבל את מיקומו ומידותיו המדוייקים, ולהגדיר את הוידג'ט
    /// כחלון-כלי (Tool Window) שאינו מופיע ב-Alt+Tab ואינו גונב פוקוס.
    /// </summary>
    public static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        // --- זיהוי "מסך-על" של ה-Shell פתוח כרגע (תפריט התחל/חיפוש/וכו') ---
        // ראו הערה מפורטת ב-MainWindow.IsShellOverlayLikelyOpen.
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // WM_SETREDRAW: משמש למניעת "קפיצה" חזותית כשמשנים גם את גודל החלון
        // וגם את מיקומו יחד (למשל בפופ-אפ לוח הזמנים, שמתארך/מתקצר כלפי
        // מעלה) - משהים ציור עד שכל השינויים הושלמו, ואז מציירים הכול בבת
        // אחת עם RedrawWindow, כדי שלא יוצג אף פריים ביניים עם גודל/מיקום
        // לא-מתואמים.
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        public const uint RDW_INVALIDATE = 0x0001;
        public const uint RDW_ERASE = 0x0004;
        public const uint RDW_ALLCHILDREN = 0x0080;
        public const uint RDW_UPDATENOW = 0x0100;

        // משמשות להסתרה/הצגה ישירה של חלון שעון המערכת (TrayClockWClass) -
        // עובד חוצה-תהליכים (בין אם החלון שייך לתהליך אחר, כמו explorer.exe),
        // ואינו תלוי בערך מדיניות Registry לא-רשמי ולא מצריך הפעלה מחדש של
        // Explorer - ולכן אמין משמעותית יותר, כולל ב-Windows 11.
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        // --- הזרקת קליק "אמיתי" (SendInput) על חלון שעון המערכת ---
        //
        // הערה: PostMessage (הודעות Win32 גולמיות) עבד באופן אמין להעברת
        // קליקים ל-HWND-ים קלאסיים, אך לא בהכרח לרכיבים מבוססי-XAML/Composition
        // (כפי שהשעון עשוי להיות ב-Windows 11) - רכיבים כאלה מצפים לקלט אמיתי
        // מתור הקלט של המערכת (Hit Testing אמיתי לפי מיקום הסמן), ולא בהכרח
        // מגיבים להודעות שנשלחות ישירות לחלון. SendInput מדמה קלט חומרה אמיתי
        // (הזזת הסמן בפועל + לחיצה), ולכן אמין משמעותית יותר גם מול רכיבים כאלה.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        public const uint INPUT_MOUSE = 0;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        // אלה מאפשרות חיפוש רקורסיבי בכל עץ החלונות-הבנים (לא רק ברמה אחת,
        // כמו FindWindowEx) - הכרחי כדי לאתר את שעון המערכת גם כאשר הוא מקונן
        // עמוק יותר מאחורי מארחי XAML Islands (למשל ב-Windows 11).
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        // שמשמשות לאיתור גבולות המסך הפיזי שעליו נמצא שעון המערכת, כדי
        // לוודא שהוידג'ט לא ייצא מחוץ לתחומי המסך (למשל בצד ימין הקיצוני).
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        // משמשת לאילוץ חוזר ונשנה של הוידג'ט להישאר "עליון" (Topmost), כדי
        // למנוע ממנו להיעלם מאחורי שורת המשימות בכל פעם ששורת המשימות עצמה
        // מאלצת את עצמה בחזרה לקדמת סדר השכבות (קורה בכל לחיצה עליה).
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public static readonly IntPtr HWND_TOPMOST = new(-1);
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_NOZORDER = 0x0004;

        public const uint MONITOR_DEFAULTTONEAREST = 2;

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        // גורם למסגרת החלון המקורית של Windows (כותרת, סמל, כפתורי מזעור/סגירה)
        // "להתהפך" לפריסת ימין-לשמאל, כך שסמל האפליקציה יופיע ליד שם התוכנה
        // בצד ימין, בהתאמה לכיווניות RTL של שאר ממשק האפליקציה.
        public const int WS_EX_LAYOUTRTL = 0x00400000;

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        // --- הודעות עכבר גולמיות (Raw Win32) ---
        //
        // הערה חשובה: החלון הראשי משתמש ב-AllowsTransparency="True" (חלון
        // "שכבתי", WS_EX_LAYERED) יחד עם מודעות DPI מסוג PerMonitorV2. השילוב
        // הזה גורם לבאג ידוע ב-WPF (dotnet/wpf) שבו חישוב ה-Hit Testing הפנימי
        // של WPF לאירועי עכבר על אלמנטים (MouseLeftButtonDown/Up וכו') מתבצע
        // לפי קנה-מידה שגוי כאשר יש שינוי DPI - התוצאה בפועל היא בדיוק התופעה
        // שדווחה: איזור לחיצה שמוגבל לפינה/רצועה קטנה בלבד מתוך כל הוידג'ט.
        //
        // הפתרון: מטפלים בלחיצות/גרירה ברמת הודעת ה-Win32 הגולמית (WM_LBUTTONDOWN
        // וכו', בתוך ה-WndProc hook) במקום להסתמך על אירועי העכבר של WPF -
        // הודעות אלה מגיעות עבור כל שטח החלון (תא הלקוח) כפי שנקבע ע"י
        // Windows עצמו, ללא תלות בבאג ה-Hit Testing הפנימי של WPF.
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static readonly IntPtr HWND_BROADCAST = new(0xffff);

        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int VK_CONTROL = 0x11;
    }
}
