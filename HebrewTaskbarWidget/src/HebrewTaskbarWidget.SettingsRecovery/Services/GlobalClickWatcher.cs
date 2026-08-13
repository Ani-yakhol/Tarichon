using System;
using HebrewTaskbarWidget.Interop;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// עוקב אחר לחיצות עכבר בכל מקום על המסך - בכל תוכנה, לא רק בתוך
    /// התהליך של תאריכון עצמו - באמצעות Hook גלובלי ברמה נמוכה (WH_MOUSE_LL).
    ///
    /// למה זה נחוץ: כמה מהחלונות "הצפים" בתוכנה (תפריטי הקשר, לוח הזמנים)
    /// בנויים על חלונות עם AllowsTransparency/Topmost/NOACTIVATE - שילוב
    /// שידוע כפוגע בהתנהגות ה-Deactivated/Popup-dismiss הרגילה של WPF
    /// באופן לא עקבי. Hook גלובלי ברמה נמוכה עוקף את זה לגמרי - הוא רואה
    /// כל לחיצת עכבר בכל תוכנה על המסך, ולא תלוי בשום מנגנון מיקוד/הפעלה
    /// פנימי של WPF.
    ///
    /// בודק את החלון תחת נקודת הלחיצה (WindowFromPoint) מול רשימת "חלונות
    /// מוגנים" שסופקה - לחיצה בתוך אחד מהם (או צאצא שלו) לא נחשבת "לחיצה
    /// בחוץ" (כדי לא לסגור את החלון בגלל אינטראקציה לגיטימית איתו עצמו,
    /// כמו לחיצה על כפתור בתוך לוח הזמנים).
    /// </summary>
    public sealed class GlobalClickWatcher : IDisposable
    {
        private readonly NativeMethods.LowLevelMouseProc _proc;
        private readonly Action _onClickOutside;
        private readonly Func<IntPtr[]> _getProtectedRootHandles;
        private readonly Func<int, int, bool>? _onRightClickAt;
        private IntPtr _hookHandle;
        private bool _disposed;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;

        /// <summary>
        /// getProtectedRootHandles: פונקציה שמחזירה, בכל פעם שנקרא (לא פעם
        /// אחת מראש) - את רשימת ה-HWND-ים ה"מוגנים" הנוכחית (למשל, חלון
        /// לוח הזמנים עצמו) - כפונקציה ולא רשימה קבועה, כדי לתמוך במקרה
        /// שהחלון המוגן משתנה/עוד לא קיים בזמן היצירה.
        ///
        /// onRightClickAt (אופציונלי): נקרא עבור **כל** לחיצה ימנית בכל
        /// מקום על המסך, עם קואורדינטות המסך הפיזיות (x, y) - מחזיר true
        /// אם יש "לבלוע" את הלחיצה (למנוע ממנה להמשיך הלאה לחלון שמתחת,
        /// למשל שולחן העבודה) - ראו שימוש ב-DesktopOverlayWindow (לחיצה
        /// ימנית שפותחת תפריט הקשר גם דרך חלון שקוף-ללחיצות כברירת מחדל).
        /// כשלא מסופק, לחיצות ימניות מטופלות כמו כל לחיצה אחרת (בדיקת
        /// "בחוץ" רגילה מול onClickOutside, בלי בליעה).
        /// </summary>
        public GlobalClickWatcher(Action onClickOutside, Func<IntPtr[]> getProtectedRootHandles, Func<int, int, bool>? onRightClickAt = null)
        {
            _onClickOutside = onClickOutside;
            _getProtectedRootHandles = getProtectedRootHandles;
            _onRightClickAt = onRightClickAt;
            _proc = HookCallback;
            _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // עוטפים את כל הגוף ב-try/catch באופן גורף ומכוון: זהו Callback
            // גולמי שנקרא ישירות ע"י Windows דרך גבול native/managed - חריגה
            // לא-מטופלת שחוצה את הגבול הזה עלולה **להפיל את כל התהליך**
            // (לא רק לזרוק שגיאת WPF רגילה שניתן להתאושש ממנה) - זו ככל
            // הנראה הסיבה לדיווח על "וינדוס סוגר את התוכנה" בעקבות באג כלשהו
            // כאן. שום דבר בתוך ה-Hook הזה לא צריך אף פעם לגרום לתהליך
            // כולו לקרוס - גם אם, למשל, אחד הקריאות-חוזרות שסופקו (
            // onClickOutside/onRightClickAt) זורקת חריגה מסיבה כלשהי.
            try
            {
                return HookCallbackCore(nCode, wParam, lParam);
            }
            catch
            {
                return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }
        }

        private IntPtr HookCallbackCore(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam.ToInt32() == WM_LBUTTONDOWN || wParam.ToInt32() == WM_RBUTTONDOWN))
            {
                var hookStruct = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

                if (wParam.ToInt32() == WM_RBUTTONDOWN && _onRightClickAt is not null)
                {
                    bool shouldSwallow = _onRightClickAt(hookStruct.pt.X, hookStruct.pt.Y);
                    if (shouldSwallow)
                    {
                        // מחזירים ערך שונה מ-0 בלי לקרוא ל-CallNextHookEx -
                        // "בולע" את הלחיצה, מונע ממנה להמשיך לחלון שמתחת.
                        // (יכולת כללית - נסתה בעבר לתפריט הקשר בתצוגת שולחן
                        // העבודה, אך הוחלפה שם בגישה פשוטה יותר; עדיין
                        // זמינה כאן לשימוש עתידי במקום אחר אם יידרש).
                        return (IntPtr)1;
                    }
                }

                IntPtr clickedWindow = NativeMethods.WindowFromPoint(hookStruct.pt);
                IntPtr clickedRoot = NativeMethods.GetAncestor(clickedWindow, NativeMethods.GA_ROOT);

                bool isInsideProtected = false;
                foreach (IntPtr protectedHandle in _getProtectedRootHandles())
                {
                    if (protectedHandle != IntPtr.Zero && (clickedWindow == protectedHandle || clickedRoot == protectedHandle))
                    {
                        isInsideProtected = true;
                        break;
                    }
                }

                if (!isInsideProtected)
                {
                    _onClickOutside();
                }
            }

            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }
}
