using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HebrewTaskbarWidget.Interop;
using HebrewTaskbarWidget.Models;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    public partial class MainWindow : Window
    {
        // תדירות בדיקת מיקום השעון מחדש (שורת המשימות עשויה לזוז/להשתנות בגודלה)
        private static readonly TimeSpan PositionPollInterval = TimeSpan.FromMilliseconds(500);

        // תדירות אילוץ חוזר של "עליון" (Topmost) - מהירה יותר מבדיקת המיקום,
        // כדי שהוידג'ט לא "ייעלם" (יוסתר מאחורי שורת המשימות) גם לרגע קצר
        // בכל פעם ששורת המשימות עצמה נאבקת בחזרה לקדמת סדר השכבות (קורה בכל
        // אינטראקציה איתה - לחיצה על סמל, פתיחת תפריט התחל וכו').
        //
        // הערה (0.5.6): גרסאות 0.5.4 ו-0.5.5 ניסו כאן שני "טלאים" שונים
        // (Hook מבוסס-אירועים, ואז טיימר מהיר במיוחד) - אף אחד מהם לא היה
        // פתרון אמיתי לבעיה, ושניהם הוסרו. הערך כאן הוחזר בכוונה בדיוק
        // לזה שהיה בגרסה 0.5.3, עד לפתרון יסודי אמיתי (בהשראת תוכנות כמו
        // BatteryBar, שבהן התופעה הזו לא קיימת כלל) שיטופל בסבב נפרד.
        private static readonly TimeSpan TopmostReassertInterval = TimeSpan.FromMilliseconds(150);

        // תדירות בדיקת שינוי בתוכן (יום/תאריך/פרשה/שעה לועזית) - פעם בשנייה
        private static readonly TimeSpan ContentPollInterval = TimeSpan.FromSeconds(1);

        // מרחק מינימלי (בפיקסלים פיזיים) בין לחיצת עכבר לשחרורה כדי שהתנועה
        // תיחשב "גרירה" ולא "קליק" - מונע פתיחה בטעות של חלונית הזמנים בעת
        // רעידת יד קלה בזמן לחיצה רגילה.
        private const int DragThresholdPhysicalPixels = 4;

        // תדירות אילוץ חוזר של הסתרת שעון Windows (כאשר מופעל בהגדרות) - ראו
        // הערה מפורטת ב-WindowsClockVisibilityService: Explorer עלול "להחזיר"
        // את השעון לגלוי מיוזמתו (קריסה/הפעלה מחדש עצמית, שינוי DPI וכו'),
        // ולכן צריך לאכוף את ההסתרה מעת לעת - בדיוק כמו אכיפת ה-Topmost.
        private static readonly TimeSpan ClockVisibilityReassertInterval = TimeSpan.FromSeconds(1.5);

        private readonly DispatcherTimer _positionTimer;
        private readonly DispatcherTimer _topmostTimer;
        private readonly DispatcherTimer _contentTimer;
        private readonly DispatcherTimer _clockVisibilityTimer;

        private uint _taskbarCreatedMessage;
        private string _lastTopLine = string.Empty;
        private string _lastBottomLine = string.Empty;
        private string _lastGregorianTime = string.Empty;
        private string _lastGregorianDate = string.Empty;
        private bool? _lastIsLightTheme;
        private string? _lastCustomColorHex;
        private bool? _lastUseCustomBackground;
        private string? _lastBackgroundColorHex;
        private double? _lastBackgroundOpacity;
        private bool? _lastUseWidgetBorder;
        private string? _lastWidgetBorderColorHex;
        private double? _lastWidgetBorderThickness;
        private bool? _lastShowGregorianClock;
        private WidgetAttachSide? _lastGregorianClockSide;
        private bool? _lastShowHolidayPanel;
        private HolidayPanelPosition? _lastHolidayPanelSide;
        private string? _lastHolidayName;

        // --- הגנה עיקרית: אם "מסך-על" של ה-Shell פתוח, לא סומכים על מדידה חדשה ---
        //
        // גילינו (ותודה על התיאור המדוייק שהוביל לכך) שהבעיה האמיתית היא
        // שהוידג'ט "קופץ" למקום גבוה יותר על המסך בדיוק כשתפריט ההתחל
        // פתוח - לא בעיית Topmost/סדר-שכבות. הניסיון הראשון לתקן זאת
        // (אימות מלבן הכפתור מול מלבן שורת המשימות - ראו TaskbarClockLocator)
        // לא עזר בפועל: גם המדידה ה"אמינה" (GetWindowRect על חלון Win32
        // אמיתי, לא רק UI Automation) עלולה עצמה לדווח על מיקום שונה
        // באופן שנשאר עקבי מול עצמו לאורך כל הזמן שהתפריט פתוח - לא רק
        // פריים חולף בודד. גם ניסיון שני (החלת קפיצה רק אם היא חוזרת פעמיים
        // ברציפות, ראו למטה) לא הספיק: תפריט ההתחל נשאר פתוח הרבה יותר
        // מסבב הבדיקה האחד (חצי שנייה) שנדרש כדי ש"קפיצה" תאושר, כך שהיא
        // בכל זאת מתקבלת אחרי שנייה אחת ונשארת עד שהתפריט נסגר.
        //
        // הפתרון האמיתי: לזהות **ישירות** מתי תפריט ההתחל (או מסך-על דומה
        // של ה-Shell - חיפוש, Widgets וכו') פתוח, ובזמן הזה פשוט **לא**
        // לעדכן מיקום בכלל - להישאר במקום היציב האחרון עד שהוא נסגר, בלי
        // תלות בגודל/חזרתיות של הקפיצה. הזיהוי: מסכי-העל האלה תמיד "גונבים"
        // את החלון הקדמי (Foreground) של המערכת בזמן שהם פתוחים, ורצים
        // בתהליכים ידועים ומתועדים (StartMenuExperienceHost.exe וכו') -
        // ראו IsShellOverlayLikelyOpen.
        private static readonly HashSet<string> ShellOverlayProcessNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "StartMenuExperienceHost", // תפריט ההתחל (Windows 10 1809+ ו-Windows 11)
            "SearchHost",              // תיבת/חלונית החיפוש
            "ShellExperienceHost",     // כמה ממסכי-העל הישנים יותר של ה-Shell
            "Widgets",                 // לוח ה-Widgets (Windows 11)
        };

        /// <summary>
        /// true אם החלון הקדמי (Foreground) הנוכחי במערכת שייך לאחד מתהליכי
        /// מסך-העל הידועים של ה-Shell (ראו ShellOverlayProcessNames) - כלומר
        /// תפריט ההתחל/חיפוש/Widgets וכו' פתוח כרגע. ראו הערה מפורטת למעלה.
        /// עטופה ב-try/catch: Process.GetProcessById עלולה לזרוק אם התהליך
        /// כבר נסגר בדיוק בין השאילתות (מקרה קצה נדיר, לא קריטי - פשוט
        /// מתייחסים לזה כ"לא פתוח").
        /// </summary>
        private static bool IsShellOverlayLikelyOpen()
        {
            IntPtr foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            NativeMethods.GetWindowThreadProcessId(foreground, out uint processId);
            if (processId == 0)
            {
                return false;
            }

            try
            {
                using Process process = Process.GetProcessById((int)processId);
                return ShellOverlayProcessNames.Contains(process.ProcessName);
            }
            catch
            {
                return false;
            }
        }

        // --- הגנה משנית: "קפיצת מיקום" גדולה ופתאומית שלא נובעת ממסך-על ידוע ---
        //
        // ההגנה למעלה (IsShellOverlayLikelyOpen) לא מכסה הכל - למשל, חלונית
        // הווליום או תצוגה-מוקטנת (thumbnail) של חלון בריחוף עכבר לא בהכרח
        // "גונבות" חלון קדמי (הן עלולות להיות חלונות שלא-מפעילים, בדיוק כמו
        // הוידג'ט שלנו). כרשת ביטחון נוספת: קפיצה גדולה ופתאומית (מעבר לסף)
        // ביחס למיקום היציב האחרון שהוחל בפועל - לא מוחלת מיד, אלא רק אם
        // אותה קפיצה בדיוק חוזרת שוב בבדיקה הבאה (חצי שנייה אח"כ), כדי לסנן
        // פריימים חולפים בודדים בלי לעכב שינויי מיקום אמיתיים ביותר מסבב
        // בדיקה אחד.
        private double? _lastStableLeft;
        private double? _lastStableTop;
        private double? _pendingSuspiciousLeft;
        private double? _pendingSuspiciousTop;
        private const double SuspiciousPositionJumpThresholdDip = 80.0;

        // --- מצב הגרירה החופשית (Ctrl + גרירת עכבר), ברמת Win32 גולמית ---
        //
        // הערה חשובה: החלון הזה משתמש ב-AllowsTransparency="True" (חלון
        // שכבתי, WS_EX_LAYERED) יחד עם מודעות DPI מסוג PerMonitorV2
        // (ב-app.manifest). לשילוב הזה יש באג ידוע ב-WPF שבו ה-Hit Testing
        // הפנימי לאירועי עכבר על אלמנטים מחושב בקנה-מידה שגוי - זו הסיבה
        // המדוייקת לכך שלחיצות והגרירה עבדו רק באיזור קטן/כלל לא עבדו.
        // הפתרון: מטפלים בהודעות העכבר הגולמיות (WM_LBUTTONDOWN/UP,
        // WM_MOUSEMOVE, WM_RBUTTONDOWN) ישירות ב-WndProc, בפיקסלים פיזיים -
        // אלה מגיעות תמיד עבור כל שטח חלון הלקוח, ללא תלות בבאג הנ"ל.
        private bool _isCtrlDragging;
        private bool _dragMoved;
        private bool _leftButtonDownPending;
        private NativeMethods.POINT _dragStartScreenPhysical;
        private int _dragStartWindowLeftPhysical;
        private int _dragStartWindowTopPhysical;
        private IntPtr _hwnd;

        // חלונית זמני היום (חלק 2) - נוצרת לפי דרישה בלחיצה, ומתבטלת כשנסגרת
        private ZmanimPopup? _zmanimPopup;
        private SettingsWindow? _settingsWindow;

        public MainWindow()
        {
            InitializeComponent();

            ApplyPlatformAppropriateFont();

            _positionTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = PositionPollInterval,
            };
            _positionTimer.Tick += (_, _) => UpdatePosition();

            _topmostTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TopmostReassertInterval,
            };
            _topmostTimer.Tick += (_, _) => ReassertTopmost();

            // 5 דקות אחרי עליית התוכנה (בין אם נפתחה ידנית ובין אם עלתה
            // אוטומטית עם Windows) - בודקים (שוב, בשקט) אם יש עדכון זמין,
            // ואם כן שואלים את המשתמש במפורש אם לעדכן (בדיוק כמו ההודעה
            // של אישור הפעלה מחדש של Explorer) - גם אם לא נכנסו בכלל
            // ללשונית "כללי". פועם רק פעם אחת (לא טיימר חוזר).
            var updatePromptTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            updatePromptTimer.Tick += async (_, _) =>
            {
                updatePromptTimer.Stop();
                await PromptForUpdateIfAvailableAsync();
            };
            updatePromptTimer.Start();

            // כשתפריט ההקשר פתוח, משהים את האכיפה החוזרת של Topmost על
            // הוידג'ט עצמו - זו הייתה הסיבה לכך שהתפריט (שאינו Topmost
            // בעצמו) יכול "להיבלע" מתחת לוידג'ט בין אכיפה לאכיפה.
            if (WidgetBackground.ContextMenu is { } contextMenu)
            {
                contextMenu.Opened += (_, _) => _topmostTimer.Stop();
                contextMenu.Closed += (_, _) => _topmostTimer.Start();
            }

            _contentTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = ContentPollInterval,
            };
            _contentTimer.Tick += (_, _) => UpdateContent();

            _clockVisibilityTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = ClockVisibilityReassertInterval,
            };
            _clockVisibilityTimer.Tick += (_, _) =>
            {
                if (SettingsService.Current.HideWindowsClock)
                {
                    WindowsClockVisibilityService.ApplyLiveVisibility(true);
                }
            };

            SourceInitialized += MainWindow_SourceInitialized;
            ContentRendered += MainWindow_ContentRendered;

            // כל שינוי בגודל בפועל של הוידג'ט (למשל אחרי שינוי גופן/טקסט) מפעיל
            // מיד חישוב מיקום מחדש - כדי שגבולות החלון תמיד יתאימו בדיוק
            // לתוכן המוצג בפועל.
            SizeChanged += (_, _) => UpdatePosition();

            // כשההגדרות משתנות (למשל בפאנל ההגדרות) - נעדכן מיד גופן, צבע,
            // שורות מוצגות, שעון לועזי ומיקום, בלי צורך בהפעלה מחדש.
            SettingsService.SettingsChanged += (_, _) => Dispatcher.Invoke(() =>
            {
                ApplyPlatformAppropriateFont();
                ApplyLineVisibilityAndOrder();
                _lastIsLightTheme = null;
                _lastUseCustomBackground = null;
                _lastShowGregorianClock = null;
                _lastShowHolidayPanel = null;
                UpdateContent();
                ApplyBackground();
                UpdatePosition();
                WindowsClockVisibilityService.ApplyLiveVisibility(SettingsService.Current.HideWindowsClock);
            });

            // מאזינים לאיתותים בין-תהליכיים מתהליכים אחרים (בפרט: כלי הגישה
            // העצמאי לפאנל ההגדרות, HebrewTaskbarWidgetSettings.exe - קובץ
            // הרצה נפרד לגמרי, גם אם מקומפל מאותה תיקיית קוד מקור) - כדי
            // שהגדרות שנשמרו משם, או בקשת יציאה משם, ייכנסו לתוקף מיד גם
            // בוידג'ט הראשי הרץ ברקע, בלי צורך בהפעלה מחדש שלו. ה-callbacks
            // עצמם רצים על תהליכון הרקע של המאזין, ולכן עוברים ל-
            // Dispatcher.Invoke כדי לגעת בבטחה ברכיבי ה-UI/במצב החלון.
            CrossProcessSignal.StartListening(
                onSettingsChanged: () => Dispatcher.Invoke(SettingsService.ReloadFromDisk),
                onExitRequested: () => Dispatcher.Invoke(ShutDownCompletely));
        }

        /// <summary>
        /// נקרא פעם אחת, 5 דקות אחרי עליית התוכנה - בודק (בשקט) אם קיימת
        /// גרסה חדשה, ואם כן שואל את המשתמש במפורש (אישור/ביטול) אם לעדכן
        /// עכשיו - גם אם מעולם לא נכנס ללשונית "כללי". במקרה של אישור,
        /// ההורדה עצמה רצה ללא פס התקדמות גלוי (אין חלון הגדרות פתוח בהכרח
        /// כרגע כדי להציג אותו) - ההתקנה בפועל (יציאה + החלפת קבצים +
        /// הפעלה מחדש) מתבצעת בדיוק כמו בכפתור "עדכן" בפאנל ההגדרות.
        /// </summary>
        private async Task PromptForUpdateIfAvailableAsync()
        {
            if (!SettingsService.Current.CheckForUpdates)
            {
                return;
            }

            UpdateInfo? info = await UpdateService.CheckForUpdateAsync();
            if (info is null)
            {
                return;
            }

            MessageBoxResult result = AppMessageBoxWindow.Show(
                $"נמצאה גרסה חדשה של תאריכון: {info.Version}. האם לעדכן עכשיו?",
                "עדכון תוכנה זמין",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information,
                this);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string? downloadedPath = await UpdateService.DownloadUpdateAsync(info.DownloadUrl, new Progress<double>(), CancellationToken.None);
            if (downloadedPath is not null)
            {
                UpdateService.ApplyUpdateAndRestart(downloadedPath);
            }
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;

            // הפיכת החלון לחלון-כלי: לא מופיע ב-Alt+Tab, לא בשורת המשימות, ולא
            // "גונב" פוקוס מהיישום הפעיל כאשר לוחצים עליו.
            int exStyle = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            exStyle &= ~NativeMethods.WS_EX_APPWINDOW;
            NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE, exStyle);

            // נרשמים להודעת המערכת שנשלחת מחדש בכל פעם ש-explorer.exe מתאתחל
            // (קריסה/הפעלה מחדש), כדי למקם את הוידג'ט מחדש כשזה קורה.
            _taskbarCreatedMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");

            HwndSource? source = HwndSource.FromHwnd(_hwnd);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_taskbarCreatedMessage != 0 && msg == _taskbarCreatedMessage)
            {
                UpdatePosition();
                return IntPtr.Zero;
            }

            switch (msg)
            {
                case NativeMethods.WM_LBUTTONDOWN:
                    OnRawLeftButtonDown(hwnd, lParam);
                    handled = true;
                    break;

                case NativeMethods.WM_MOUSEMOVE:
                    OnRawMouseMove(hwnd, lParam);
                    break;

                case NativeMethods.WM_LBUTTONUP:
                    OnRawLeftButtonUp();
                    handled = true;
                    break;

                case NativeMethods.WM_RBUTTONDOWN:
                    OnRawRightButtonDown(hwnd, lParam);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private static NativeMethods.POINT GetScreenPointFromLParam(IntPtr hwnd, IntPtr lParam)
        {
            long raw = lParam.ToInt64();
            int x = unchecked((short)(raw & 0xFFFF));
            int y = unchecked((short)((raw >> 16) & 0xFFFF));

            var point = new NativeMethods.POINT { X = x, Y = y };
            NativeMethods.ClientToScreen(hwnd, ref point);
            return point;
        }

        private void OnRawLeftButtonDown(IntPtr hwnd, IntPtr lParam)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && !SettingsService.Current.LockWidgetPosition)
            {
                _isCtrlDragging = true;
                _dragMoved = false;
                _leftButtonDownPending = false;

                _dragStartScreenPhysical = GetScreenPointFromLParam(hwnd, lParam);

                if (NativeMethods.GetWindowRect(hwnd, out RECT currentRect))
                {
                    _dragStartWindowLeftPhysical = currentRect.Left;
                    _dragStartWindowTopPhysical = currentRect.Top;
                }

                NativeMethods.SetCapture(hwnd);
            }
            else
            {
                _leftButtonDownPending = true;
            }
        }

        private void OnRawMouseMove(IntPtr hwnd, IntPtr lParam)
        {
            if (!_isCtrlDragging)
            {
                return;
            }

            NativeMethods.POINT current = GetScreenPointFromLParam(hwnd, lParam);
            int deltaX = current.X - _dragStartScreenPhysical.X;
            int deltaY = current.Y - _dragStartScreenPhysical.Y;

            if (!_dragMoved && (Math.Abs(deltaX) > DragThresholdPhysicalPixels || Math.Abs(deltaY) > DragThresholdPhysicalPixels))
            {
                _dragMoved = true;
            }

            if (_dragMoved)
            {
                int newLeft = _dragStartWindowLeftPhysical + deltaX;
                int newTop = _dragStartWindowTopPhysical + deltaY;

                NativeMethods.SetWindowPos(
                    hwnd,
                    IntPtr.Zero,
                    newLeft,
                    newTop,
                    0,
                    0,
                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
            }
        }

        private void OnRawLeftButtonUp()
        {
            if (_isCtrlDragging)
            {
                _isCtrlDragging = false;
                NativeMethods.ReleaseCapture();

                if (_dragMoved)
                {
                    // שומרים את המיקום החדש כ"מיקום חופשי" - מכאן והלאה הוידג'ט
                    // לא יחושב יותר אוטומטית ביחס לשעון, אלא יישאר במקום שנבחר
                    // (ורק יהודק לגבולות המסך אם המסך/הרזולוציה משתנים).
                    AppSettings settings = SettingsService.Current;
                    settings.PositionMode = WidgetPositionMode.FreeDrag;
                    settings.FreeDragLeft = Left;
                    settings.FreeDragTop = Top;
                    SettingsService.Save(settings);
                }

                _dragMoved = false;
                return;
            }

            if (_leftButtonDownPending)
            {
                _leftButtonDownPending = false;
                ToggleZmanimPopup();
            }
        }

        /// <summary>
        /// לחיצה ימנית על הוידג'ט: שני חלקי התאריך (העברי והלועזי) הם בפועל
        /// וידג'ט אחד ארוך, אך מגיבים אחרת ללחיצה ימנית - לחיצה ימנית על
        /// החלק העברי (או בכל מקום אחר בוידג'ט, אם השעון הלועזי כבוי) פותחת
        /// את תפריט ההקשר הרגיל של האפליקציה (כמו קודם); לחיצה ימנית על חלק
        /// השעון/תאריך הלועזי המשולב (אם מוצג) מועברת ישירות לחלון שעון
        /// המערכת האמיתי של Windows, כך שהתפריט/החלונית שהוא עצמו פותח
        /// בלחיצה ימנית (הזהה למה שהיה נפתח בלחיצה ימנית על השעון האמיתי
        /// בשורת המשימות) ייפתחו - ולא תפריט ההקשר של האפליקציה שלנו.
        /// </summary>
        private void OnRawRightButtonDown(IntPtr hwnd, IntPtr lParam)
        {
            _isCtrlDragging = false;
            _leftButtonDownPending = false;

            if (TryGetGregorianClockForwardTarget(hwnd, lParam, out IntPtr clockWnd, out RECT clockScreenRect))
            {
                ForwardRightClickToWindowsClock(clockWnd, clockScreenRect);
                return;
            }

            if (WidgetBackground.ContextMenu is { } menu)
            {
                menu.IsOpen = true;
            }
        }

        /// <summary>
        /// בודקת אם נקודת הלחיצה נמצאת בתוך חלק השעון/תאריך הלועזי המשולב
        /// (GregorianClockPanel) - ואם כן, מאתרת את חלון שעון המערכת האמיתי
        /// של Windows ומחזירה את מלבנו (בפיקסלים פיזיים, קואורדינטות מסך).
        ///
        /// משתמשת ב-VisualTreeHelper.HitTest (מנוע הבדיקה המובנה של WPF)
        /// במקום חישוב ידני של טרנספורמציות/גבולות - כדי שהזיהוי יהיה מדוייק
        /// תמיד, ללא קשר למיקום המדוייק של GregorianClockPanel בתוך הוידג'ט
        /// (למשל אחרי הוספת חלק "חג ומועד" שיכול לשבת בכל צד).
        /// </summary>
        private bool TryGetGregorianClockForwardTarget(IntPtr hwnd, IntPtr lParam, out IntPtr clockWnd, out RECT clockScreenRect)
        {
            clockWnd = IntPtr.Zero;
            clockScreenRect = default;

            if (GregorianClockPanel.Visibility != Visibility.Visible)
            {
                return false;
            }

            // בדיקת בטיחות מפורשת: לחיצה על החלק העברי (WidgetLinesPanel) לעולם
            // לא תעביר ללוח השנה של Windows, ללא קשר לתוצאה של הבדיקה הבאה -
            // מבטיח שהשניים לא "יתחלפו" גם אם יש חפיפה כלשהי בין תחומי
            // הבדיקה של שני האלמנטים (התנהגות שדווחה בעבר).
            if (IsPointOverElement(hwnd, lParam, WidgetLinesPanel))
            {
                return false;
            }

            if (!IsPointOverElement(hwnd, lParam, GregorianClockPanel))
            {
                return false;
            }

            if (!TaskbarClockLocator.TryLocateClockWindow(out IntPtr foundClockWnd) || foundClockWnd == IntPtr.Zero)
            {
                return false;
            }

            if (!NativeMethods.GetWindowRect(foundClockWnd, out RECT rect))
            {
                return false;
            }

            // הגנה: אם ההסתרה + צמצום הרווח פעילים, ל-Explorer כבר אין בכלל
            // שטח שמור לשעון (המלבן שלו הפך מנוון - כמעט/בדיוק 0 על 0, או
            // ממוקם ב-(0,0)) - אם ננסה בכל זאת ללחוץ שם, נלחץ בפועל על מה
            // שתופס עכשיו את אותו מיקום פיזי במגש (למשל מחליף שפה) - תקלה
            // שדווחה בפועל. לכן, במקרה כזה, מתייחסים לזיהוי כאילו נכשל -
            // הקוראים (לחיצה ימנית/שמאלית) כבר יודעים ליפול חזרה בבטחה
            // (לחיצה ימנית: תפריט ההקשר שלנו; שמאלית: לוח הזמנים שלנו).
            if (rect.Width <= 1 || rect.Height <= 1)
            {
                return false;
            }

            clockWnd = foundClockWnd;
            clockScreenRect = rect;
            return true;
        }

        /// <summary>
        /// בודקת (בעזרת VisualTreeHelper.HitTest של WPF עצמו - לא חישוב ידני)
        /// אם נקודת לחיצה נתונה (lParam, בפיקסלים פיזיים יחסית לחלון) נופלת
        /// בתוך אלמנט מסויים או אחד מצאצאיו בעץ הויזואלי - זו הדרך המדוייקת
        /// ביותר לבדוק "על מה בדיוק לחצו", ללא תלות בחישובי טרנספורמציה/
        /// גבולות ידניים שעלולים לא לקחת בחשבון שוליים/ריפוד/מיקום בפועל.
        /// </summary>
        /// <summary>
        /// קובעת אם נקודת לחיצה נתונה (lParam, קואורדינטות client גולמיות של
        /// Win32) נמצאת בתוך התחום הפיזי בפועל של אלמנט מסויים על המסך.
        ///
        /// ממירה את שתי הנקודות (הלחיצה, וגבולות האלמנט) לקואורדינטות מסך
        /// פיזיות אמיתיות ומשווה ביניהן ישירות - הלחיצה דרך ClientToScreen
        /// (Win32, חד-משמעי), וגבולות האלמנט דרך UIElement.PointToScreen
        /// (שמטפל נכון, מבפנים, בכל הטרנספורמציות של WPF - כולל את המיראור
        /// שה-Window מבצע בגלל FlowDirection="RightToLeft"). בכך נמנעים
        /// לגמרי מהצורך לנחש/לתקן ידנית את כיוון המיראור של קואורדינטות
        /// Hit-Testing - זו הייתה הסיבה לזיהוי הפוך (עברי/לועזי) בעבר.
        /// </summary>
        private bool IsPointOverElement(IntPtr hwnd, IntPtr lParam, FrameworkElement targetElement)
        {
            if (targetElement.ActualWidth <= 0 || targetElement.ActualHeight <= 0)
            {
                return false;
            }

            NativeMethods.POINT clickScreenPoint = GetScreenPointFromLParam(hwnd, lParam);

            Point corner1;
            Point corner2;
            try
            {
                corner1 = targetElement.PointToScreen(new Point(0, 0));
                corner2 = targetElement.PointToScreen(new Point(targetElement.ActualWidth, targetElement.ActualHeight));
            }
            catch (InvalidOperationException)
            {
                return false; // עדיין לא בעץ הויזואלי (למשל בשלב אתחול מוקדם מדי)
            }

            double left = Math.Min(corner1.X, corner2.X);
            double right = Math.Max(corner1.X, corner2.X);
            double top = Math.Min(corner1.Y, corner2.Y);
            double bottom = Math.Max(corner1.Y, corner2.Y);

            return clickScreenPoint.X >= left && clickScreenPoint.X <= right &&
                   clickScreenPoint.Y >= top && clickScreenPoint.Y <= bottom;
        }

        /// <summary>
        /// מדמה לחיצה ימנית **אמיתית** (SendInput, לא הודעת Win32 גולמית -
        /// ראו הערה ב-NativeMethods) במרכז מלבן השעון האמיתי של Windows,
        /// כך שנפתחים בדיוק התפריט/החלונית שהיו נפתחים בלחיצה ימנית בפועל
        /// על השעון המקורי בשורת המשימות. אם השעון מוסתר כרגע (הגדרת
        /// "הסתר את תצוגת התאריך/שעה המקורית" מופעלת) הוא מוצג זמנית לצורך
        /// זה בלבד - טיימר ההסתרה החוזרת של MainWindow יסתיר אותו שוב מעצמו
        /// תוך כשנייה וחצי, אחרי שהתפריט/החלונית כבר נפתחו.
        /// </summary>
        private static void ForwardRightClickToWindowsClock(IntPtr clockWnd, RECT clockScreenRect)
        {
            if (!NativeMethods.IsWindowVisible(clockWnd))
            {
                NativeMethods.ShowWindow(clockWnd, NativeMethods.SW_SHOW);
            }

            NativeMethods.GetCursorPos(out NativeMethods.POINT originalCursor);

            int targetX = clockScreenRect.Left + clockScreenRect.Width / 2;
            int targetY = clockScreenRect.Top + clockScreenRect.Height / 2;
            NativeMethods.SetCursorPos(targetX, targetY);

            var inputs = new NativeMethods.INPUT[2];
            inputs[0] = new NativeMethods.INPUT { type = NativeMethods.INPUT_MOUSE, mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_RIGHTDOWN } };
            inputs[1] = new NativeMethods.INPUT { type = NativeMethods.INPUT_MOUSE, mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_RIGHTUP } };
            NativeMethods.SendInput(2, inputs, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());

            // מחזירים את הסמן למקומו המקורי - מבחינת המשתמש הלחיצה הייתה על
            // הוידג'ט שלנו, לא על השעון האמיתי (שנמצא במיקום אחר על המסך).
            NativeMethods.SetCursorPos(originalCursor.X, originalCursor.Y);
        }

        /// <summary>
        /// דוחפת את הוידג'ט בחזרה לקדמת סדר-השכבות (Z-Order) בלי לשנות מיקוד
        /// (SWP_NOACTIVATE) ובלי לזוז/להשתנות בגודל. פועלת בתדירות גבוהה כדי
        /// "לנצח" את שורת המשימות במאבקי ה-Topmost שמתרחשים בכל אינטראקציה
        /// איתה, ובכך למנוע מהוידג'ט "להיעלם" מאחוריה.
        /// </summary>
        private void ReassertTopmost()
        {
            if (_hwnd == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }

        /// <summary>
        /// בוחר גופן וגודל שמדמים במדוייק את תצוגת השעון/תאריך המקורית של Windows:
        /// "Segoe UI Variable" ב-Windows 11, ו-"Segoe UI" ב-Windows 10.
        /// </summary>
        private void ApplyPlatformAppropriateFont()
        {
            AppSettings settings = SettingsService.Current;

            FontFamily fontFamily;
            double fontSize;

            if (settings.UseCustomFont && !string.IsNullOrWhiteSpace(settings.FontFamilyName))
            {
                fontFamily = new FontFamily(settings.FontFamilyName);
                fontSize = settings.FontSize;
            }
            else
            {
                bool isWindows11OrNewer = Environment.OSVersion.Version.Build >= 22000;
                fontFamily = isWindows11OrNewer
                    ? new FontFamily("Segoe UI Variable Text, Segoe UI")
                    : new FontFamily("Segoe UI");
                fontSize = 12.0;
            }

            TopLineText.FontFamily = fontFamily;
            TopLineText.FontSize = fontSize;
            BottomLineText.FontFamily = fontFamily;
            BottomLineText.FontSize = fontSize;
            GregorianTimeText.FontFamily = fontFamily;
            GregorianTimeText.FontSize = fontSize;
            GregorianDateText.FontFamily = fontFamily;
            GregorianDateText.FontSize = fontSize;
        }

        /// <summary>
        /// מיישם את הגדרות הצגה/הסתרה וסדר של שתי השורות העבריות, וכן את
        /// הצגת/מיקום השעון הלועזי המשולב (לצד ימין/שמאל של השורות העבריות).
        /// </summary>
        private void ApplyLineVisibilityAndOrder()
        {
            AppSettings settings = SettingsService.Current;

            TopLineText.Visibility = settings.ShowTopLine ? Visibility.Visible : Visibility.Collapsed;
            BottomLineText.Visibility = settings.ShowBottomLine ? Visibility.Visible : Visibility.Collapsed;

            if (WidgetLinesPanel.Children.Contains(TopLineText) && WidgetLinesPanel.Children.Contains(BottomLineText))
            {
                int topIndex = WidgetLinesPanel.Children.IndexOf(TopLineText);
                int bottomIndex = WidgetLinesPanel.Children.IndexOf(BottomLineText);

                bool currentlySwapped = topIndex > bottomIndex;
                if (currentlySwapped != settings.SwapLineOrder)
                {
                    WidgetLinesPanel.Children.Remove(TopLineText);
                    WidgetLinesPanel.Children.Remove(BottomLineText);

                    if (settings.SwapLineOrder)
                    {
                        WidgetLinesPanel.Children.Add(BottomLineText);
                        WidgetLinesPanel.Children.Add(TopLineText);
                    }
                    else
                    {
                        WidgetLinesPanel.Children.Add(TopLineText);
                        WidgetLinesPanel.Children.Add(BottomLineText);
                    }
                }
            }

            bool showGregorian = settings.ShowGregorianClock;
            GregorianClockPanel.Visibility = showGregorian ? Visibility.Visible : Visibility.Collapsed;

            // כשהשעון הלועזי מוצג, המפריד תמיד תופס מקום (Visibility.Visible) -
            // כדי שהרווח בין שתי התצוגות יישאר קבוע גם כשמבטלים אותו; במקרה
            // הזה רק הצבע הופך לשקוף (הקו לא נראה, אבל הרווח שהוא תופס נשאר).
            ClockSeparator.Visibility = showGregorian ? Visibility.Visible : Visibility.Collapsed;
            ClockSeparator.Background = settings.ShowGregorianSeparator
                ? new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF))
                : Brushes.Transparent;

            // "המשבצת" של חלק "חג ומועד" נכללת בסדר הילדים בכל פעם שהוא
            // מופעל בהגדרות - ללא קשר לכך שיש חג היום בפועל (זה נבדק בנפרד,
            // בכל שנייה, ב-UpdateContent, שקובע רק את ה-Visibility/הטקסט).
            bool showHolidaySlot = settings.ShowHolidayPanel;

            if (_lastShowGregorianClock != showGregorian || _lastGregorianClockSide != settings.GregorianClockSide ||
                _lastShowHolidayPanel != showHolidaySlot || _lastHolidayPanelSide != settings.HolidayPanelSide)
            {
                // RootLayoutPanel הוא Horizontal בתוך חלון RTL: הילד הראשון
                // מוצג ויזואלית מימין. GregorianClockSide=Left פירושו שהשעון
                // הלועזי יופיע *משמאל* לשורות העבריות - כלומר, הוא צריך להיות
                // הילד האחרון (Add אחרי WidgetLinesPanel), ולהיפך עבור Right.
                RootLayoutPanel.Children.Clear();

                var order = new List<UIElement>();

                if (showHolidaySlot && settings.HolidayPanelSide == HolidayPanelPosition.BetweenHebrewAndGregorian)
                {
                    // רצף מפורש עם בדיוק 2 מפרידים (אחד לכל זוג שכנים) - לא
                    // "מוסיפים" את חלק החג לתוך רצף הליבה הקיים, כי זה עלול
                    // ליצור כפל מפרידים בצד אחד וכלום בצד השני (התנהגות באגית
                    // קודמת). כאן כל אחד מה-2 אלמנטי המפריד (ClockSeparator,
                    // HolidaySeparator) משמש בדיוק לגבול אחד, בעקביות.
                    if (settings.GregorianClockSide == WidgetAttachSide.Right)
                    {
                        order.Add(GregorianClockPanel);
                        order.Add(ClockSeparator);
                        order.Add(HolidayPanel);
                        order.Add(HolidaySeparator);
                        order.Add(WidgetLinesPanel);
                    }
                    else
                    {
                        order.Add(WidgetLinesPanel);
                        order.Add(HolidaySeparator);
                        order.Add(HolidayPanel);
                        order.Add(ClockSeparator);
                        order.Add(GregorianClockPanel);
                    }
                }
                else
                {
                    // "ימין"/"שמאל" ממקמים את חלק "חג ומועד" בקצה ה**כולל**
                    // של הוידג'ט (לפני/אחרי כל השאר, לא בין העברי ללועזי).
                    if (showHolidaySlot && settings.HolidayPanelSide == HolidayPanelPosition.FarRight)
                    {
                        order.Add(HolidayPanel);
                        order.Add(HolidaySeparator);
                    }

                    if (settings.GregorianClockSide == WidgetAttachSide.Right)
                    {
                        order.Add(GregorianClockPanel);
                        order.Add(ClockSeparator);
                        order.Add(WidgetLinesPanel);
                    }
                    else
                    {
                        order.Add(WidgetLinesPanel);
                        order.Add(ClockSeparator);
                        order.Add(GregorianClockPanel);
                    }

                    if (showHolidaySlot && settings.HolidayPanelSide == HolidayPanelPosition.FarLeft)
                    {
                        order.Add(HolidaySeparator);
                        order.Add(HolidayPanel);
                    }
                }

                foreach (UIElement child in order)
                {
                    RootLayoutPanel.Children.Add(child);
                }

                _lastShowGregorianClock = showGregorian;
                _lastGregorianClockSide = settings.GregorianClockSide;
                _lastShowHolidayPanel = showHolidaySlot;
                _lastHolidayPanelSide = settings.HolidayPanelSide;
            }
        }

        /// <summary>
        /// מיישם את צבע רקע קוביית הוידג'ט: ברירת המחדל היא שקיפות מלאה (רואים
        /// רק את הטקסט), אך ניתן להגדיר בפאנל ההגדרות צבע רקע קבוע (כולל
        /// שקיפות משלו) לקוביה כולה.
        /// </summary>
        private void ApplyBackground()
        {
            AppSettings settings = SettingsService.Current;

            if (_lastUseCustomBackground == settings.UseCustomBackgroundColor &&
                _lastBackgroundColorHex == settings.WidgetBackgroundColorHex &&
                _lastBackgroundOpacity == settings.WidgetBackgroundOpacity &&
                _lastUseWidgetBorder == settings.UseWidgetBorder &&
                _lastWidgetBorderColorHex == settings.WidgetBorderColorHex &&
                _lastWidgetBorderThickness == settings.WidgetBorderThickness)
            {
                return;
            }

            if (settings.UseWidgetBorder && TryParseColor(settings.WidgetBorderColorHex, out Color borderColor))
            {
                WidgetBackground.BorderBrush = new SolidColorBrush(borderColor);
                WidgetBackground.BorderThickness = new Thickness(Math.Max(0, settings.WidgetBorderThickness));
            }
            else
            {
                WidgetBackground.BorderBrush = null;
                WidgetBackground.BorderThickness = new Thickness(0);
            }

            _lastUseWidgetBorder = settings.UseWidgetBorder;
            _lastWidgetBorderColorHex = settings.WidgetBorderColorHex;
            _lastWidgetBorderThickness = settings.WidgetBorderThickness;

            if (settings.UseCustomBackgroundColor && TryParseColor(settings.WidgetBackgroundColorHex, out Color color))
            {
                byte alpha = (byte)Math.Clamp(settings.WidgetBackgroundOpacity * 255.0, 0, 255);
                WidgetBackground.Background = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            }
            else
            {
                // "שקוף" ויזואלית, אך לא ברמת Alpha=0 האמיתית: לחלון שכבתי
                // (WS_EX_LAYERED עם AllowsTransparency="True") Windows מנתב
                // קליקים "דרך" כל פיקסל שה-Alpha שלו הוא בדיוק 0 אל מה
                // שנמצא מתחתיו (שולחן העבודה/שורת המשימות) - זו הייתה הסיבה
                // המדוייקת לכך שבמצב שקיפות מלאה אפשר היה ללחוץ רק במקום
                // זעיר (פיקסלי הטקסט עצמו, שאינם שקופים). Alpha=1 (מתוך 255)
                // בלתי-נראה לעין לגמרי, אך מבטיח שכל שטח הוידג'ט ימשיך לקבל
                // קליקים כרגיל.
                WidgetBackground.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            }

            _lastUseCustomBackground = settings.UseCustomBackgroundColor;
            _lastBackgroundColorHex = settings.WidgetBackgroundColorHex;
            _lastBackgroundOpacity = settings.WidgetBackgroundOpacity;
        }

        /// <summary>
        /// מעדכן את שורות הטקסט (עברי + לועזי) ואת צבען, רק כאשר יש שינוי
        /// בפועל - כדי להימנע מעדכוני פריסה מיותרים.
        /// </summary>
        private void UpdateContent()
        {
            AppSettings settings = SettingsService.Current;
            DateTime now = AppTimeService.Now();
            DateTime hebrewDisplayDate = HebrewDayRolloverService.GetEffectiveHebrewDate(now, settings, SettingsService.BuildLocation());
            HebrewDateDisplay display = HebrewDateFormatter.Format(hebrewDisplayDate);

            if (display.TopLine != _lastTopLine)
            {
                TopLineText.Text = display.TopLine;
                _lastTopLine = display.TopLine;
            }

            if (display.BottomLine != _lastBottomLine)
            {
                BottomLineText.Text = display.BottomLine;
                _lastBottomLine = display.BottomLine;
            }

            // חלק "חג ומועד" - מוצג רק אם הופעל בהגדרות **וגם** יש חג/מועד
            // ביום המוצג כרגע (לפי אותו תאריך עברי "אפקטיבי" כמו שאר הוידג'ט,
            // כולל מעבר יום בשקיעה אם הוגדר כך). נבדק בכל רענון (כל שנייה),
            // כדי שהתוכן יתעדכן ממילא עם חילופי יום - סדר/משבצת הילדים
            // עצמם (ApplyLineVisibilityAndOrder) מתעדכנים רק בשינוי הגדרות.
            string? holidayName = HolidayService.GetHolidayName(hebrewDisplayDate);
            bool showHolidayContent = settings.ShowHolidayPanel && !string.IsNullOrEmpty(holidayName);

            if (holidayName != _lastHolidayName)
            {
                HolidayPanelText.Text = holidayName ?? string.Empty;
                _lastHolidayName = holidayName;
            }

            HolidayPanel.Visibility = showHolidayContent ? Visibility.Visible : Visibility.Collapsed;
            HolidaySeparator.Visibility = showHolidayContent ? Visibility.Visible : Visibility.Collapsed;
            HolidaySeparator.Background = settings.ShowHolidaySeparator
                ? new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF))
                : Brushes.Transparent;

            // שעון/תאריך לועזי - הפורמט נבחר ע"י המשתמש (GregorianDateFormatComboBox
            // בהגדרות, ברירת מחדל "dd/MM/yyyy" = "05/08/2026") - לא עוד לפי
            // הפורמט הקצר ("d") של האזור (Region) הנוכחי, כי זה יכול להיראות
            // שונה מאוד בין אזורים (כולל בלי אפסים מובילים). עדיין משתמשים
            // ב-AppTimeService.GregorianDisplayCulture בשביל לוח השנה (לועזי,
            // תמיד) - ראו הערה מפורטת שם.
            string gregorianTime = AppTimeService.FormatClockTime(now);
            string gregorianDate = now.ToString(settings.GregorianDateFormat, AppTimeService.GregorianDisplayCulture);

            if (gregorianTime != _lastGregorianTime)
            {
                GregorianTimeText.Text = gregorianTime;
                _lastGregorianTime = gregorianTime;
            }

            if (gregorianDate != _lastGregorianDate)
            {
                GregorianDateText.Text = gregorianDate;
                _lastGregorianDate = gregorianDate;
            }

            if (settings.UseCustomTextColor)
            {
                if (_lastCustomColorHex != settings.CustomTextColorHex)
                {
                    Brush customBrush = TryParseColor(settings.CustomTextColorHex, out Color customColor)
                        ? new SolidColorBrush(customColor)
                        : Brushes.White;

                    SetAllTextColors(customBrush);
                    _lastCustomColorHex = settings.CustomTextColorHex;
                    _lastIsLightTheme = null;
                }

                return;
            }

            _lastCustomColorHex = null;

            bool isLightTheme = TaskbarThemeService.IsTaskbarLightTheme();
            if (_lastIsLightTheme != isLightTheme)
            {
                SetAllTextColors(isLightTheme ? Brushes.Black : Brushes.White);
                _lastIsLightTheme = isLightTheme;
            }
        }

        private void SetAllTextColors(Brush brush)
        {
            TopLineText.Foreground = brush;
            BottomLineText.Foreground = brush;
            GregorianTimeText.Foreground = brush;
            GregorianDateText.Foreground = brush;
            HolidayPanelText.Foreground = brush;
        }

        private static bool TryParseColor(string hex, out Color color)
        {
            try
            {
                color = (Color)ColorConverter.ConvertFromString(hex);
                return true;
            }
            catch
            {
                color = Colors.White;
                return false;
            }
        }

        private void MainWindow_ContentRendered(object? sender, EventArgs e)
        {
            ApplyLineVisibilityAndOrder();
            UpdateContent();
            ApplyBackground();
            UpdatePosition();

            _positionTimer.Start();
            _contentTimer.Start();
            _topmostTimer.Start();
            _clockVisibilityTimer.Start();

            // מיישמים מיד את מצב ההסתרה השמור (למשל אם המשתמש הפעיל את זה
            // בהפעלה קודמת) - בלי להמתין לטיק הראשון של הטיימר.
            WindowsClockVisibilityService.ApplyLiveVisibility(SettingsService.Current.HideWindowsClock);

            // אם גם ההסתרה החיה וגם "צמצום הרווח הריק" מופעלים - יש לבצע גם
            // הפעלה מחדש של Explorer כבר בעליית התוכנה, אחרת ההסתרה החיה
            // בלבד (למעלה) משאירה את הרווח הריק ריק (Explorer כבר הקצה לו
            // שטח לפני שהתוכנה בכלל עלתה). לפי ההגדרה: מפעילים אוטומטית בלי
            // לשאול, או שואלים אישור קודם (ברירת המחדל).
            //
            // יוצא מן הכלל 1: אם התוכנה עולה כחלק מעליית Windows עצמה (הפעלה
            // אוטומטית) - Explorer עצמו רק עכשיו עולה בפעם הראשונה, ועדיין
            // לא הקצה שטח לתצוגת השעון המקורית כלל (כי ערך המדיניות כבר
            // מוגדר מפעם קודמת - ראו SetPolicyValue) - כך שאין צורך, וגם לא
            // הגיוני, להפעיל אותו מחדש שוב בסמוך לעלייתו הראשונה.
            //
            // יוצא מן הכלל 2 (זיהוי חכם): אם explorer.exe כבר עלה מחדש
            // מסיבה כלשהי (הפעלה מחדש של המחשב, קריסה שהתאוששה, הפעלה
            // מחדש ידנית) מאז הפעם האחרונה שהתוכנה בדקה/ביצעה הפעלה מחדש -
            // הוא כבר קרא את ערך המדיניות בעצמו, ואין צורך (ולא הגיוני)
            // להפעיל אותו מחדש שוב רק כדי להשיג את אותה תוצאה בדיוק.
            if (SettingsService.Current.HideWindowsClock && SettingsService.Current.HideWindowsClockReduceGap &&
                !StartupService.IsAutoStartLaunch() && WindowsClockVisibilityService.NeedsExplorerRestart())
            {
                switch (SettingsService.Current.ExplorerAutoLaunchMode)
                {
                    case ExplorerAutoLaunchMode.Automatic:
                        WindowsClockVisibilityService.ApplyFullEffectWithRestart(true);
                        break;

                    case ExplorerAutoLaunchMode.Never:
                        // המשתמש ביקש לעולם לא להפעיל אוטומטית ולא לשאול -
                        // לא ביציאה ולא בפתיחה (ראו גם ShutDownCompletely).
                        // אם יידרש, אפשר תמיד להפעיל ידנית דרך כפתור "הפעל"
                        // הצמוד בפאנל ההגדרות.
                        break;

                    case ExplorerAutoLaunchMode.AskEachTime:
                    default:
                        MessageBoxResult confirm = AppMessageBoxWindow.Show(
                            "כדי לצמצם גם את הרווח הריק שנשאר אחרי הסתרת התאריך/שעה המקורית, יש להפעיל מחדש את Explorer (שולחן העבודה ושורת המשימות ייעלמו וייטענו מחדש לרגע). להמשיך?",
                            "אישור הפעלה מחדש של Explorer",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question,
                            this);

                        if (confirm == MessageBoxResult.Yes)
                        {
                            WindowsClockVisibilityService.ApplyFullEffectWithRestart(true);
                        }
                        break;
                }
            }

            ShowOnboardingTourIfNeeded();
        }

        /// <summary>
        /// מציגה את חלונית סיור ההיכרות (OnboardingTourWindow) אוטומטית -
        /// רק אם עוד לא דולג עליה במפורש (ראו AppSettings.OnboardingTourSkipped)
        /// וגם רק אם יש בפועל לפחות תמונת סיור אחת מוטמעת בתוכנה (כדי לא
        /// להציג חלונית ריקה למי שמריץ בנייה בלי שהתמונות הוטמעו עדיין).
        /// נקראת פעם אחת בעליית התוכנה - לא מטיימר חוזר.
        /// </summary>
        private void ShowOnboardingTourIfNeeded()
        {
            if (SettingsService.Current.OnboardingTourSkipped)
            {
                return;
            }

            if (!OnboardingTourWindow.HasAnyImages())
            {
                return;
            }

            var tour = new OnboardingTourWindow();
            tour.ShowDialog();
        }

        /// <summary>
        /// ממקם את הוידג'ט לפי מצב המיקום שנבחר בהגדרות:
        ///
        /// - AboveTaskbar (ברירת מחדל, בטוח): מעל השעון, מיושר לצד שנבחר.
        /// - BesideClock (מצב קודם): בתוך גובה שורת המשימות, לצד השעון.
        /// - CustomEdgeOffset: **בתוך** גובה שורת המשימות (מיושר אנכית בדיוק
        ///   כמו BesideClock), אך במרחק אופקי קבוע בפיקסלים מהקצה הימני/שמאלי
        ///   של שורת המשימות עצמה - ולא ביחס למיקום השעון. יש להגדיר מרחק
        ///   שמניח אותו באיזור פנוי בפועל, אחרת הוא עלול לחפוף לסמלים אחרים.
        /// - FreeDrag: מיקום מוחלט שנקבע ע"י גרירה חופשית עם Ctrl - לא מחושב
        ///   מחדש ביחס לשעון/לשורת המשימות, רק מהודק לגבולות המסך.
        /// </summary>
        private void UpdatePosition()
        {
            AppSettings settings = SettingsService.Current;

            // ראו הערה מפורטת ליד IsShellOverlayLikelyOpen למטה - אם "מסך-על"
            // של ה-Shell (בעיקר תפריט ההתחל) פתוח כרגע, לא סומכים כלל על
            // מדידות שורת המשימות החדשות, ופשוט נשארים במקום היציב האחרון.
            if (settings.PositionMode != WidgetPositionMode.FreeDrag &&
                _lastStableLeft.HasValue && _lastStableTop.HasValue &&
                IsShellOverlayLikelyOpen())
            {
                PositionDiagnosticsLogger.Log("מסך-על Shell זוהה כפתוח - מדלגים על עדכון מיקום, נשארים ב-" +
                    $"({_lastStableLeft.Value:0.#}, {_lastStableTop.Value:0.#})");
                return;
            }

            double dpiScale = TaskbarClockLocator.GetTaskbarDpiScale();
            if (dpiScale <= 0)
            {
                dpiScale = 1.0;
            }

            double widgetWidthDip = ActualWidth > 0 ? ActualWidth : Width;
            double widgetHeightDip = ActualHeight > 0 ? ActualHeight : Height;

            double newLeft;
            double newTop;

            if (settings.PositionMode == WidgetPositionMode.FreeDrag && settings.FreeDragLeft.HasValue && settings.FreeDragTop.HasValue)
            {
                newLeft = settings.FreeDragLeft.Value;
                newTop = settings.FreeDragTop.Value;
            }
            else if (settings.PositionMode == WidgetPositionMode.CustomEdgeOffset)
            {
                if (!TaskbarClockLocator.TryGetTaskbarRect(out RECT taskbarRect))
                {
                    return; // שורת המשימות לא נמצאה כרגע
                }

                double taskbarLeftDip = taskbarRect.Left / dpiScale;
                double taskbarRightDip = taskbarRect.Right / dpiScale;
                double taskbarTopDip = taskbarRect.Top / dpiScale;
                double taskbarHeightDip = taskbarRect.Height / dpiScale;

                newLeft = settings.CustomOffsetSide == WidgetAttachSide.Left
                    ? taskbarLeftDip + settings.CustomOffsetPixels
                    : taskbarRightDip - settings.CustomOffsetPixels - widgetWidthDip;

                // בתוך גובה שורת המשימות עצמה (ממורכז אנכית), כמו BesideClock -
                // לא מעל, לפי הדרישה: מרחק מהקצה אמור להחליף מיקום בתוך שורת
                // המשימות, לא ליצור עוד תצוגה מרחפת מעליה.
                newTop = taskbarTopDip + (taskbarHeightDip - widgetHeightDip) / 2.0;
            }
            else
            {
                // ChevronAttached (ברירת המחדל): צמוד לכפתור "^" (הצג סמלים
                // מוסתרים) במגש המערכת, ממש כמו BatteryBar. צד ההצמדה תלוי
                // בכיווניות שורת המשימות: ב-Windows בעברית (RTL) מצמידים
                // לצד הימני של הכפתור; ב-Windows באנגלית (LTR) לצד השמאלי -
                // כדי לחקות בדיוק את המיקום שבו סמל מגש חדש/גלוי היה מופיע
                // לצד הכפתור. כשמראים/מסתירים סמלים מוסתרים נוספים (והכפתור
                // עצמו זז בעקבות זאת), הוידג'ט זז יחד איתו - כי המיקום נבדק
                // מחדש כל חצי שנייה.
                bool isRtl = TaskbarClockLocator.IsTaskbarRightToLeft();

                if (TaskbarClockLocator.TryLocateChevronButton(out RECT chevronRect))
                {
                    double chevronLeftDip = chevronRect.Left / dpiScale;
                    double chevronRightDip = chevronRect.Right / dpiScale;
                    double chevronTopDip = chevronRect.Top / dpiScale;
                    double chevronHeightDip = chevronRect.Height / dpiScale;

                    newLeft = isRtl ? chevronRightDip : chevronLeftDip - widgetWidthDip;
                    newTop = chevronTopDip + (chevronHeightDip - widgetHeightDip) / 2.0;
                }
                else if (TaskbarClockLocator.TryLocateClock(out RECT clockRect))
                {
                    // גיבוי: אם משום מה כפתור החץ לא נמצא (למשל כל הסמלים
                    // גלויים ואין בכלל כפתור חץ במגש) - נצמדים לצד השעון
                    // במקום (מהצד הפנימי, לא חופף לו), כדי שהוידג'ט לא ייעלם.
                    double clockLeftDip = clockRect.Left / dpiScale;
                    double clockRightDip = clockRect.Right / dpiScale;
                    double clockTopDip = clockRect.Top / dpiScale;
                    double clockHeightDip = clockRect.Height / dpiScale;

                    newLeft = isRtl ? clockRightDip : clockLeftDip - widgetWidthDip;
                    newTop = clockTopDip + (clockHeightDip - widgetHeightDip) / 2.0;
                }
                else
                {
                    return; // שורת המשימות לא נמצאה כרגע (למשל בזמן הפעלה מחדש של explorer)
                }
            }

            // הידוק לגבולות המסך **המלאים** (לא רק אזור העבודה, שמחריג את
            // שורת המשימות) - כדי שגם מצב "מיקום חופשי" (גרירה) וגם "מרחק
            // מותאם אישית מקצה" יוכלו למקם את הוידג'ט בכל מקום, כולל בתוך
            // גובה שורת המשימות עצמה, ולא רק מעליה/מחוצה לה.
            if (TaskbarClockLocator.TryGetTaskbarMonitorFullRect(out RECT monitorRect))
            {
                double monitorLeftDip = monitorRect.Left / dpiScale;
                double monitorRightDip = monitorRect.Right / dpiScale;
                double monitorTopDip = monitorRect.Top / dpiScale;
                double monitorBottomDip = monitorRect.Bottom / dpiScale;

                if (newLeft + widgetWidthDip > monitorRightDip)
                {
                    newLeft = monitorRightDip - widgetWidthDip;
                }

                if (newLeft < monitorLeftDip)
                {
                    newLeft = monitorLeftDip;
                }

                if (newTop + widgetHeightDip > monitorBottomDip)
                {
                    newTop = monitorBottomDip - widgetHeightDip;
                }

                if (newTop < monitorTopDip)
                {
                    newTop = monitorTopDip;
                }
            }

            // ראו הערה מפורטת ליד הצהרת השדות _lastStableLeft/_pendingSuspiciousLeft
            // למעלה - קפיצה גדולה ופתאומית לא מוחלת מיד, רק אם היא חוזרת
            // שוב בבדיקה הבאה (מסנן פריימים חולפים של אנימציות שורת המשימות,
            // בלי לעכב שינויי מיקום אמיתיים ביותר מסבב בדיקה אחד).
            if (_lastStableLeft.HasValue && _lastStableTop.HasValue)
            {
                double jump = Math.Max(Math.Abs(newLeft - _lastStableLeft.Value), Math.Abs(newTop - _lastStableTop.Value));

                if (jump > SuspiciousPositionJumpThresholdDip)
                {
                    bool matchesPreviousPending =
                        _pendingSuspiciousLeft.HasValue && _pendingSuspiciousTop.HasValue &&
                        Math.Abs(newLeft - _pendingSuspiciousLeft.Value) < 2.0 &&
                        Math.Abs(newTop - _pendingSuspiciousTop.Value) < 2.0;

                    if (!matchesPreviousPending)
                    {
                        // קפיצה חשודה חדשה - עדיין לא ראינו אותה פעמיים ברציפות.
                        // לא מיישמים הפעם; הוידג'ט נשאר במקומו האחרון היציב.
                        PositionDiagnosticsLogger.Log(
                            $"קפיצה חשודה ({jump:0.#} DIP): מ-({_lastStableLeft.Value:0.#},{_lastStableTop.Value:0.#}) " +
                            $"ל-({newLeft:0.#},{newTop:0.#}), מצב={settings.PositionMode} - ממתין לאישור בבדיקה הבאה");
                        _pendingSuspiciousLeft = newLeft;
                        _pendingSuspiciousTop = newTop;
                        return;
                    }

                    // אותה קפיצה בדיוק נראתה גם בבדיקה הקודמת - כנראה שינוי
                    // אמיתי (לא רק פריים חולף), ממשיכים ליישם אותה כרגיל.
                    PositionDiagnosticsLogger.Log(
                        $"קפיצה חשודה ({jump:0.#} DIP) אושרה (נראתה פעמיים ברציפות) - מוחלת: ({newLeft:0.#},{newTop:0.#})");
                }

                _pendingSuspiciousLeft = null;
                _pendingSuspiciousTop = null;
            }

            _lastStableLeft = newLeft;
            _lastStableTop = newTop;

            bool leftChanged = Math.Abs(Left - newLeft) > 0.5 || double.IsNaN(Left);
            bool topChanged = Math.Abs(Top - newTop) > 0.5 || double.IsNaN(Top);

            if (leftChanged || topChanged)
            {
                PositionDiagnosticsLogger.Log($"מיקום הוחל: ({newLeft:0.#},{newTop:0.#}), מצב={settings.PositionMode}");
            }

            if (leftChanged)
            {
                Left = newLeft;
            }

            if (topChanged)
            {
                Top = newTop;
            }
        }

        private void ZmanimMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleZmanimPopup();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings(SettingsWindow.WidgetTabIndex);
        }

        private void NotificationsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings(SettingsWindow.NotificationsTabIndex);
        }

        /// <summary>
        /// פותח את פאנל ההגדרות (חלק 3), או מביא אותו לקדמת המסך אם כבר פתוח.
        /// forceTabIndex מאפשר לקפוץ ישירות ללשונית מסויימת (למשל התראות) -
        /// **גם אם** הפאנל כבר פתוח על לשונית אחרת (בניגוד לפתיחה רגילה בלי
        /// לשונית מבוקשת, ששומרת על הלשונית הנוכחית כדי לא "לגנוב" מהמשתמש
        /// עריכה שהוא כבר באמצעה). למשל: לחיצה על "הגדרות..." בתפריט אף פעם
        /// לא מקפיצה ללשונית הראשונה בעל-כורחו; לחיצה על "התראות..." תמיד
        /// תקפיץ ללשונית ההתראות, גם אם הפאנל כבר היה פתוח על לשונית אחרת.
        /// </summary>
        public void OpenSettings(int? forceTabIndex = null)
        {
            if (_settingsWindow is not null)
            {
                if (forceTabIndex.HasValue)
                {
                    _settingsWindow.SelectTab(forceTabIndex.Value);
                }

                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(forceTabIndex ?? 0);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        /// <summary>
        /// פותח את חלונית זמני היום אם היא סגורה, או סוגר אותה אם היא כבר
        /// פתוחה. זמין הן בלחיצה על הוידג'ט (כולל השעון הלועזי, אם מוצג) והן
        /// דרך תפריט ההקשר.
        /// </summary>
        private void ToggleZmanimPopup()
        {
            if (_zmanimPopup is not null)
            {
                _zmanimPopup.Close();
                return;
            }

            _zmanimPopup = new ZmanimPopup();
            _zmanimPopup.Closed += (_, _) => _zmanimPopup = null;

            _zmanimPopup.Show();
            _zmanimPopup.PositionAboveWidget(Left, Top, ActualWidth);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.6.2";

            AppMessageBoxWindow.Show(
                $"תאריכון - וידג'ט תאריך עברי לשורת המשימות\nגרסה {version} (גרסת בטא)\n\n" +
                "מציג את התאריך העברי, היום בשבוע ופרשת השבוע, צמוד לשעון המערכת.\n\n" +
                "נתוני פרשת השבוע: Hebcal.com (רישיון CC BY 4.0)\n\n" +
                $"מקור התוכנה: {UpdateService.RepositoryUrl}",
                "אודות",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                this);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShutDownCompletely();
        }

        /// <summary>
        /// כיבוי מסודר ומלא של כל התוכנה (לא רק סגירת חלון ההגדרות) - עוצר את
        /// כל הטיימרים ויוצא מהתהליך. נקרא הן מ"יציאה" בתפריט ההקשר של
        /// הוידג'ט, והן מבקשת יציאה שמגיעה מתהליך אחר (כפתור "כיבוי ויציאה
        /// מוחלטת" בפאנל ההגדרות - זמין גם דרך כלי הגישה העצמאי).
        /// </summary>
        private void ShutDownCompletely()
        {
            _positionTimer.Stop();
            _topmostTimer.Stop();
            _contentTimer.Stop();
            _clockVisibilityTimer.Stop();

            // משחזרים את הגלוי של שעון Windows לפני היציאה - אחרת הוא יישאר
            // מוסתר גם אחרי סגירת התוכנה, בלי שום דרך להחזירו מלבד הפעלה מחדש.
            WindowsClockVisibilityService.ApplyLiveVisibility(false);

            // אם גם "צמצום הרווח הריק" היה מופעל - ההסתרה החיה למעלה בלבד
            // לא מספיקה כדי להחזיר את המצב לגמרי: הרווח שהתוכנה צמצמה (ע"י
            // מדיניות Registry + הפעלה מחדש של Explorer) נשאר מצומצם גם אחרי
            // סגירת התוכנה, עד הפעלה מחדש נוספת של Explorer. ההתנהגות כאן
            // תואמת בדיוק להגדרת ExplorerAutoLaunchMode (זהה לעלייה - ראו
            // MainWindow_ContentRendered): "לא מפעיל" - לא מפעילים ולא
            // שואלים בכלל, גם לא ביציאה; "הפעלה אוטומטית" - מפעילים מחדש
            // מיד בלי לשאול; "שאל בכל פעם" (ברירת המחדל) - שואלים אישור
            // (ולא אוטומטית, כי זה גורם להבהוב קצר של שולחן העבודה).
            if (SettingsService.Current.HideWindowsClock && SettingsService.Current.HideWindowsClockReduceGap)
            {
                switch (SettingsService.Current.ExplorerAutoLaunchMode)
                {
                    case ExplorerAutoLaunchMode.Automatic:
                        WindowsClockVisibilityService.ApplyFullEffectWithRestart(false);
                        break;

                    case ExplorerAutoLaunchMode.Never:
                        // לא מפעילים ולא שואלים - בדיוק כמו בעלייה.
                        break;

                    case ExplorerAutoLaunchMode.AskEachTime:
                    default:
                        MessageBoxResult restoreConfirm = AppMessageBoxWindow.Show(
                            "התוכנה מוגדרת גם לצמצם את הרווח הריק שנשאר במקום התאריך/שעה המקורית - כדי להחזיר את זה לגמרי לקדמותו צריך להפעיל מחדש את Explorer (שולחן העבודה ושורת המשימות ייעלמו וייטענו מחדש לרגע). להחזיר עכשיו?",
                            "החזרת התצוגה המקורית",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question,
                            this);

                        if (restoreConfirm == MessageBoxResult.Yes)
                        {
                            WindowsClockVisibilityService.ApplyFullEffectWithRestart(false);
                        }
                        break;
                }
            }

            Application.Current.Shutdown();
        }
    }
}
