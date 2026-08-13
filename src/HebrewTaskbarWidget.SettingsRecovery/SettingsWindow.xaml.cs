using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Runtime.InteropServices;
using HebrewTaskbarWidget.Controls;
using HebrewTaskbarWidget.Interop;
using HebrewTaskbarWidget.Models;
using HebrewTaskbarWidget.Services;
using Windows.Devices.Geolocation;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// פאנל ההגדרות המלא (חלק 3 בפרוייקט) - נגיש דרך תפריט ההקשר של הוידג'ט
    /// או דרך סמל ההגדרות בחלונית זמני היום. כולל: הצגת הוידג'ט (הצמדה, שורות,
    /// גופן, צבע, רקע), מיקום גיאוגרפי לחישוב זמני היום, התראות על זמנים
    /// הלכתיים, ותצוגה חופשית מעל שולחן העבודה.
    ///
    /// השינויים נשמרים רק בלחיצה על "שמור" (עובדים על עותק מקומי של ההגדרות
    /// ולא על ה-Singleton ישירות, כדי ש"ביטול" באמת יבטל שינויים שלא נשמרו).
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// רשימת מיקומים מוגדרים מראש, לבחירה מהירה ללא הזנת קואורדינטות
        /// ידנית. הערכים (קו רוחב/אורך, גובה מעל פני הים, ומזהה אזור הזמן
        /// בפורמט של Windows) מבוססים על נתונים גיאוגרפיים ציבוריים מוכרים
        /// לכל עיר/יישוב (מרכז העיר בקירוב - מספיק לחישוב זמני היום, שאינו
        /// רגיש להפרשים של מאות מטרים בודדים בתוך אותה עיר).
        /// </summary>
        private static readonly (string Name, double Lat, double Lng, double Elevation, string TimeZoneId)[] LocationPresets =
        {
            // --- ישראל (ערים ויישובים פופולריים) ---
            ("ירושלים", 31.7683, 35.2137, 754, "Israel Standard Time"),
            ("תל אביב - יפו", 32.0853, 34.7818, 5, "Israel Standard Time"),
            ("חיפה", 32.7940, 34.9896, 100, "Israel Standard Time"),
            ("ראשון לציון", 31.9730, 34.7925, 30, "Israel Standard Time"),
            ("פתח תקווה", 32.0878, 34.8878, 54, "Israel Standard Time"),
            ("אשדוד", 31.8044, 34.6553, 30, "Israel Standard Time"),
            ("נתניה", 32.3215, 34.8532, 35, "Israel Standard Time"),
            ("באר שבע", 31.2530, 34.7915, 280, "Israel Standard Time"),
            ("בני ברק", 32.0807, 34.8338, 30, "Israel Standard Time"),
            ("חולון", 32.0117, 34.7735, 30, "Israel Standard Time"),
            ("רמת גן", 32.0684, 34.8248, 35, "Israel Standard Time"),
            ("אשקלון", 31.6688, 34.5715, 45, "Israel Standard Time"),
            ("רחובות", 31.8928, 34.8113, 65, "Israel Standard Time"),
            ("בת ים", 32.0171, 34.7502, 15, "Israel Standard Time"),
            ("כפר סבא", 32.1750, 34.9070, 65, "Israel Standard Time"),
            ("הרצליה", 32.1663, 34.8436, 30, "Israel Standard Time"),
            ("חדרה", 32.4340, 34.9196, 20, "Israel Standard Time"),
            ("מודיעין-מכבים-רעות", 31.8969, 35.0095, 230, "Israel Standard Time"),
            ("נצרת", 32.7021, 35.2978, 350, "Israel Standard Time"),
            ("לוד", 31.9515, 34.8898, 60, "Israel Standard Time"),
            ("רמלה", 31.9286, 34.8656, 90, "Israel Standard Time"),
            ("רעננה", 32.1847, 34.8706, 45, "Israel Standard Time"),
            ("גבעתיים", 32.0723, 34.8107, 40, "Israel Standard Time"),
            ("אילת", 29.5577, 34.9519, 10, "Israel Standard Time"),
            ("טבריה", 32.7940, 35.5305, -200, "Israel Standard Time"),
            ("צפת", 32.9646, 35.4960, 900, "Israel Standard Time"),
            ("בית שמש", 31.7452, 34.9887, 300, "Israel Standard Time"),
            ("קריית גת", 31.6100, 34.7642, 130, "Israel Standard Time"),
            ("נהריה", 33.0100, 35.0989, 5, "Israel Standard Time"),
            ("עכו", 32.9280, 35.0817, 5, "Israel Standard Time"),
            ("עפולה", 32.6076, 35.2897, 60, "Israel Standard Time"),
            ("כרמיאל", 32.9186, 35.2952, 220, "Israel Standard Time"),
            ("קריית שמונה", 33.2075, 35.5697, 150, "Israel Standard Time"),
            ("אריאל", 32.1058, 35.1749, 550, "Israel Standard Time"),
            ("ביתר עילית", 31.6969, 35.1122, 780, "Israel Standard Time"),
            ("אלעד", 32.0500, 34.9500, 100, "Israel Standard Time"),
            ("מודיעין עילית", 31.9319, 35.0472, 280, "Israel Standard Time"),
            ("מעלה אדומים", 31.7728, 35.2975, 490, "Israel Standard Time"),
            ("אום אל-פחם", 32.5169, 35.1531, 450, "Israel Standard Time"),
            ("דימונה", 31.0689, 35.0325, 585, "Israel Standard Time"),

            // --- חוץ לארץ (ערים פופולריות בקהילות יהודיות) ---
            ("ניו יורק, ארה״ב", 40.7128, -74.0060, 10, "Eastern Standard Time"),
            ("לוס אנג'לס, ארה״ב", 34.0522, -118.2437, 71, "Pacific Standard Time"),
            ("מיאמי, ארה״ב", 25.7617, -80.1918, 2, "Eastern Standard Time"),
            ("שיקגו, ארה״ב", 41.8781, -87.6298, 180, "Central Standard Time"),
            ("טורונטו, קנדה", 43.6532, -79.3832, 76, "Eastern Standard Time"),
            ("מונטריאול, קנדה", 45.5019, -73.5674, 36, "Eastern Standard Time"),
            ("לונדון, אנגליה", 51.5074, -0.1278, 11, "GMT Standard Time"),
            ("מנצ'סטר, אנגליה", 53.4808, -2.2426, 38, "GMT Standard Time"),
            ("פריז, צרפת", 48.8566, 2.3522, 35, "Romance Standard Time"),
            ("אנטוורפן, בלגיה", 51.2194, 4.4025, 8, "Romance Standard Time"),
            ("מוסקבה, רוסיה", 55.7558, 37.6173, 156, "Russian Standard Time"),
            ("יוהנסבורג, דרום אפריקה", -26.2041, 28.0473, 1753, "South Africa Standard Time"),
            ("בואנוס איירס, ארגנטינה", -34.6037, -58.3816, 25, "Argentina Standard Time"),
            ("מלבורן, אוסטרליה", -37.8136, 144.9631, 31, "AUS Eastern Standard Time"),
            ("סידני, אוסטרליה", -33.8688, 151.2093, 3, "AUS Eastern Standard Time"),
        };

        /// <summary>אינדקס לשונית "הוידג'ט" (הראשונה) - לשימוש בקפיצה ישירה אליה, למשל מ"הגדרות..." בתפריט הראשי. אם סדר הלשוניות ב-XAML משתנה, יש לעדכן כאן בהתאם.</summary>
        public const int WidgetTabIndex = 0;

        /// <summary>אינדקס לשונית "התראות" - לשימוש בקפיצה ישירה אליה, למשל מהתפריט הראשי. אם סדר הלשוניות ב-XAML משתנה, יש לעדכן כאן בהתאם.</summary>
        public const int NotificationsTabIndex = 2;

        /// <summary>אינדקס לשונית "שולחן עבודה" - לשימוש בקפיצה ישירה אליה, למשל מתפריט ההקשר של תצוגת שולחן העבודה עצמה. אם סדר הלשוניות ב-XAML משתנה, יש לעדכן כאן בהתאם.</summary>
        public const int DesktopTabIndex = 3;

        /// <summary>צבע רקע קבוע למצב כהה בפאנל ההגדרות (ובחלוניות ההודעה) - לא ניתן יותר לבחירה אישית ע"י המשתמש.</summary>
        private const string DefaultDarkBackgroundHex = "#1B1C1F";

        private AppSettings _working;
        private bool _isLoading;

        /// <summary>
        /// true אם המשתמש סימן את "צמצם גם את הרווח הריק" בסשן העריכה הנוכחי
        /// (ראו ReduceGapCheckBox_CheckedChanged) - ההפעלה מחדש בפועל של
        /// Explorer לא מתבצעת מיד יותר, אלא רק בעת שמירת ההגדרות (SaveButton_Click),
        /// ורק אם התיבה אכן נשארה מסומנת עד אז.
        /// </summary>
        private bool _reduceGapRestartPendingOnSave;

        /// <summary>
        /// true אם בטעינת ההגדרות המיקום בפועל היה "חופשי" (מוגדר ע"י גרירה)
        /// - כל עוד זה עדיין true, שמירה ללא נגיעה בהגדרות המרחק המותאם
        /// אישית תשמור את המיקום החופשי כפי שהוא; ברגע שהמשתמש נוגע במפורש
        /// באחת מהגדרות המרחק (או בוחר מפורשות באופציה הראשונה), הדגל מתאפס.
        /// </summary>
        private bool _freeDragPreserved;
        private readonly List<ZmanRuleRow> _zmanRuleRows = new();

        /// <summary>עותק עבודה של רשימת ההתראות המתקדמות - נערך ע"י עורך ההתראה ונשמר בפועל רק בלחיצה על "שמור" הראשית של פאנל ההגדרות.</summary>
        private List<AdvancedNotificationRule> _workingAdvancedRules = new();

        /// <summary>מזהה ההתראה המתקדמת שנמצאת כרגע בעריכה בעורך המשותף (null = מוסיפים התראה חדשה).</summary>
        private string? _editingAdvancedRuleId;

        /// <summary>שורה בודדת ברשימת ההתראות הראשית (לשונית התראות) - עוטפת את הפקדים בפועל של אותה שורה, כדי לקרוא/לכתוב מהם בקלות.</summary>
        private sealed class ZmanRuleRow
        {
            public required string ZmanName { get; init; }
            public required CheckBox EnabledCheckBox { get; init; }
            public required TextBox MinutesTextBox { get; init; }
            public required ComboBox SoundComboBox { get; init; }
            public required ToggleButton TestToggle { get; init; }
        }

        /// <summary>עותק עבודה של סדר הפריטים בתצוגה החופשית מעל שולחן העבודה - נערך ע"י כפתורי החצים ונשמר בפועל רק בלחיצה על "שמור".</summary>
        private List<string> _workingOverlayOrder = new();

        /// <summary>עותק עבודה של מצב הסימון ("האם להציג") לכל פריט ברשימת "מה להציג" - נערך ע"י תיבות הסימון שנבנות דינמית ב-RefreshOverlayOrderPanel, ונשמר בפועל רק בלחיצה על "שמור".</summary>
        private Dictionary<string, bool> _overlayShowState = new();

        /// <summary>תוויות תצוגה בעברית עבור כל מפתח פריט אפשרי ב-OverlayItemOrder.</summary>
        private static readonly Dictionary<string, string> OverlayItemLabels = new()
        {
            ["Time"] = "שעה",
            ["DayParasha"] = "יום ופרשת שבוע",
            ["HebrewDate"] = "תאריך עברי מלא",
            ["GregorianDate"] = "תאריך לועזי",
            ["Holiday"] = "מועדים",
        };

        /// <summary>פותח את פאנל ההגדרות. initialTabIndex מאפשר לקפוץ ישירות ללשונית מסויימת - ברירת המחדל 0 = הלשונית הראשונה ("הוידג'ט").</summary>
        public SettingsWindow(int initialTabIndex = 0)
        {
            InitializeComponent();

            SourceInitialized += SettingsWindow_SourceInitialized;

            // מאזין לאירוע סטטי (חוצה-מופעים) - חובה להסיר את המנוי בסגירת
            // החלון, אחרת המופע הזה של SettingsWindow "ידלוף" לצמיתות (יישאר
            // מוחזק ע"י המאזין הסטטי גם אחרי סגירת החלון).
            UpdateService.AvailableUpdateChanged += OnAvailableUpdateChanged;
            Closed += (_, _) => UpdateService.AvailableUpdateChanged -= OnAvailableUpdateChanged;

            BuildLocationPresetItems();

            // "מיקום על המסך" (לשונית "שולחן עבודה") - פריטיו קבועים ב-XAML
            // (לא נבנים דינמית כמו רשימת הערים), אבל עדיין רוצים למדוד את
            // רוחב הפריט הארוך ביניהם בפועל (ולא לנחש/לקבע פיקסלים ביד) -
            // באמצעות אותה פונקציית עזר משותפת.
            FitComboBoxWidthToContent(
                OverlayPositionComboBox,
                OverlayPositionComboBox.Items.OfType<ComboBoxItem>().Select(item => item.Content?.ToString() ?? string.Empty));

            // "היכן תצוף ההודעה" (לשונית "התראות") - אותה טכניקה בדיוק, כדי
            // שהתפריט לא ייפרש סתם לכל רוחב הפאנל למרות שכל האפשרויות
            // קצרות יחסית.
            FitComboBoxWidthToContent(
                NotificationToastPositionComboBox,
                NotificationToastPositionComboBox.Items.OfType<ComboBoxItem>().Select(item => item.Content?.ToString() ?? string.Empty));

            // עובדים על עותק, כדי שלחיצה על "ביטול" לא תשאיר שינויים חלקיים.
            _working = CloneSettings(SettingsService.Current);

            // אין צורך לבנות את שתי הרשימות (BuildZmanRulesPanel/BuildZmanVisibilityPanel)
            // כאן בנפרד - LoadFromSettings (למטה) כבר בונה את שתיהן מחדש עם
            // הנתונים האמיתיים שנטענים ממנה; קריאה נפרדת כאן הייתה רק
            // מיותרת (מוחלפת מיד), ואף עלולה להסתמך על ZmanCalculationMethodComboBox
            // לפני שהוא אותחל.
            LoadFromSettings(_working);

            TimeMaskTextBoxBehavior.Attach(ManualTimeTextBox);
            TimeMaskTextBoxBehavior.Attach(AdvancedRuleMinutesTextBox);

            ConfigureAccordionGroups();
            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;

            if (initialTabIndex >= 0 && initialTabIndex < MainTabControl.Items.Count)
            {
                MainTabControl.SelectedIndex = initialTabIndex;
            }

            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.8.4";
            AboutVersionText.Text = $"תאריכון - גרסה {version}";
        }

        /// <summary>מקפיצה ללשונית מסויימת - משמש כשהפאנל כבר פתוח ורוצים לקפוץ ללשונית ספציפית (למשל "התראות" מהתפריט הראשי) בלי לפתוח מופע נוסף.</summary>
        public void SelectTab(int tabIndex)
        {
            if (tabIndex >= 0 && tabIndex < MainTabControl.Items.Count)
            {
                MainTabControl.SelectedIndex = tabIndex;
            }
        }

        /// <summary>
        /// ממקד את מסגרת החלון הטבעית (כותרת, סמל, כפתורי מזעור/סגירה) לפריסת
        /// ימין-לשמאל, כדי שסמל האפליקציה יופיע ליד שם התוכנה בצד ימין - תואם
        /// לכיווניות RTL של שאר ממשק ההגדרות.
        /// </summary>
        private void SettingsWindow_SourceInitialized(object? sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int exStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_LAYOUTRTL;
            NativeMethods.SetWindowLong(handle, NativeMethods.GWL_EXSTYLE, exStyle);

            HwndSource.FromHwnd(handle)?.AddHook(WndProc);
            RegisterRawInputForScroll(handle);

            Closed += (_, _) => UnregisterRawInputForScroll();
        }

        /// <summary>
        /// נרשמים ל"קלט גולמי" (Raw Input, WM_INPUT) עבור התקני עכבר - ראו
        /// הערה מפורטת ליד ה-constants ב-NativeMethods.cs. תקציר: אבחון
        /// מפורש של המשתמש גילה שהגלילה לא מגיעה לחלון הזה בכלל - "עוברת
        /// דרך" לחלון אחר על שולחן העבודה - מה שמתאים בדיוק להתנהגות
        /// המתועדת של הגדרת Windows "Scroll inactive windows when I hover
        /// over them" (רישום MouseWheelRouting), שמבצעת בדיקת-פגיעה עצמאית
        /// לפי מיקום הסמן כדי להחליט לאיזה חלון לשלוח את הגלילה - ולא
        /// מזהה, מסיבה כלשהי, את החלון הזה במיקום הסמן. שום טיפול בהודעות
        /// WM_MOUSEWHEEL/WM_POINTERWHEEL הרגילות לא יכול לעזור למצב הזה,
        /// כי הן פשוט אף פעם לא מגיעות. קלט גולמי הוא ערוץ נפרד ונמוך-רמה
        /// יותר שמבוסס על מיקוד (Focus) של החלון בלבד - לא על שום בדיקת-
        /// פגיעה חיצונית - ולכן עוקף את הבעיה לגמרי, לכל התקן עכבר/לוח מגע
        /// סטנדרטי (לא רק תיקון ממוקד למכשיר ספציפי).
        /// </summary>
        private static void RegisterRawInputForScroll(IntPtr handle)
        {
            var device = new NativeMethods.RAWINPUTDEVICE
            {
                usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                usUsage = NativeMethods.HID_USAGE_GENERIC_MOUSE,
                dwFlags = NativeMethods.RIDEV_INPUTSINK,
                hwndTarget = handle,
            };

            NativeMethods.RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>());
        }

        /// <summary>מבטלת את ההרשמה לקלט גולמי בסגירת החלון - כדי לא להשאיר רישום "תקוע" מול HWND שכבר נהרס.</summary>
        private static void UnregisterRawInputForScroll()
        {
            var device = new NativeMethods.RAWINPUTDEVICE
            {
                usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                usUsage = NativeMethods.HID_USAGE_GENERIC_MOUSE,
                dwFlags = NativeMethods.RIDEV_REMOVE,
                hwndTarget = IntPtr.Zero,
            };

            NativeMethods.RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>());
        }

        // ראו הערה מפורטת ליד הטיפול ב-WM_MOUSEWHEEL/WM_POINTERWHEEL ב-WndProc:
        // חלון-זמן קצר (מילישניות) שמונע גלילה כפולה כשגם ה-Raw Input וגם
        // ההודעה הרגילה מגיעות עבור אותה תזוזת-גלגלת ממש.
        private int _lastRawWheelScrollTickCount = int.MinValue / 2;
        private const int RawWheelDedupeMilliseconds = 150;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_INPUT)
            {
                HandleRawInputWheel(lParam);
                // לא מסמנים handled=true - מאפשרים לעיבוד הרגיל (כולל
                // WM_MOUSEWHEEL הנלווה שעדיין עשוי להגיע, ומטופל למטה)
                // להמשיך כרגיל; ה-Raw Input הוא ערוץ מידע *נוסף*, לא תחליף
                // לעיבוד ברירת המחדל של ההודעה הזו עצמה.
            }
            else if (msg == NativeMethods.WM_MOUSEWHEEL || msg == NativeMethods.WM_POINTERWHEEL)
            {
                // בעבר: פשוט מסמנים Handled=true כאן, בהנחה שה-WM_INPUT
                // המקביל (למעלה) תמיד יגיע ויבצע בפועל את הגלילה. בפועל
                // התברר שזו הנחה שגויה - בחלק מהמצבים/מכשירי הקלט
                // (למשל מכונות וירטואליות, חיבור מרוחק, או דרייברים
                // מסויימים של עכבר/משטח מגע) הודעת ה-WM_INPUT הגולמית
                // כלל לא נשלחת, אף שההודעה "הרגילה" הזו כן מגיעה - וכך
                // הגלילה בגלגלת נעלמת לגמרי בפאנל ההגדרות (הבאג המדווח).
                //
                // התיקון: משתמשים בהודעה הרגילה הזו כרשת ביטחון - אם לא
                // עבר זמן-מה מאז שה-WM_INPUT האחרון כבר גלל בפועל (כלומר
                // זו כנראה אותה תזוזת-גלגלת ממש, רק "כפולה"), גוללים כאן
                // ישירות לפי הדלתא שבתוך wParam. כך הגלילה תמיד עובדת,
                // בלי קשר לזמינות ה-Raw Input בסביבת ההרצה, ובלי לגלול
                // פעמיים על אותה תזוזה כשגם הערוץ הרגיל וגם הגולמי מגיעים
                // (המקרה הנפוץ).
                if (Environment.TickCount - _lastRawWheelScrollTickCount > RawWheelDedupeMilliseconds)
                {
                    short wheelDelta = unchecked((short)((wParam.ToInt64() >> 16) & 0xFFFF));
                    if (wheelDelta != 0)
                    {
                        ScrollActiveTab(wheelDelta);
                    }
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        /// <summary>מפענחת הודעת WM_INPUT - אם זו תזוזת גלגלת-עכבר (כולל גלילה מדומה מלוח מגע), גוללת את הלשונית הפעילה.</summary>
        private void HandleRawInputWheel(IntPtr lParam)
        {
            uint size = 0;
            uint headerSize = (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>();
            NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, IntPtr.Zero, ref size, headerSize);
            if (size == 0)
            {
                return;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, buffer, ref size, headerSize) != size)
                {
                    return;
                }

                var raw = Marshal.PtrToStructure<NativeMethods.RAWINPUT>(buffer);
                if (raw.header.dwType != NativeMethods.RIM_TYPEMOUSE)
                {
                    return;
                }

                if ((raw.mouse.usButtonFlags & NativeMethods.RI_MOUSE_WHEEL) != 0)
                {
                    short rawDelta = unchecked((short)raw.mouse.usButtonData);
                    ScrollActiveTab(rawDelta);
                    _lastRawWheelScrollTickCount = Environment.TickCount;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>גוללת את ה-ScrollViewer של הלשונית הנבחרת כרגע (רק אחת מוצגת בפועל בכל רגע נתון).</summary>
        private void ScrollActiveTab(double delta)
        {
            ScrollViewer? active = MainTabControl.SelectedIndex switch
            {
                0 => WidgetTabScrollViewer,
                1 => ZmanimTabScrollViewer,
                2 => NotificationsTabScrollViewer,
                3 => DesktopTabScrollViewer,
                4 => GeneralTabScrollViewer,
                _ => null,
            };

            active?.ScrollToVerticalOffset(active.VerticalOffset - delta);
        }

        private static AppSettings CloneSettings(AppSettings source)
        {
            return new AppSettings
            {
                SettingsPanelDarkMode = source.SettingsPanelDarkMode,
                SettingsPanelDarkColorHex = source.SettingsPanelDarkColorHex,
                ShowWidget = source.ShowWidget,
                PositionMode = source.PositionMode,
                CustomOffsetSide = source.CustomOffsetSide,
                CustomOffsetPixels = source.CustomOffsetPixels,
                LockWidgetPosition = source.LockWidgetPosition,
                LockOverlayPosition = source.LockOverlayPosition,
                FreeDragLeft = source.FreeDragLeft,
                FreeDragTop = source.FreeDragTop,
                ShowTopLine = source.ShowTopLine,
                ShowBottomLine = source.ShowBottomLine,
                SwapLineOrder = source.SwapLineOrder,
                UseCustomFont = source.UseCustomFont,
                FontFamilyName = source.FontFamilyName,
                FontSize = source.FontSize,
                UseCustomTextColor = source.UseCustomTextColor,
                CustomTextColorHex = source.CustomTextColorHex,
                ZmanimPopupAlignment = source.ZmanimPopupAlignment,
                StartWithWindows = source.StartWithWindows,
                CheckForUpdates = source.CheckForUpdates,
                HideWindowsClock = source.HideWindowsClock,
                HideWindowsClockReduceGap = source.HideWindowsClockReduceGap,
                ExplorerAutoLaunchMode = source.ExplorerAutoLaunchMode,
                ShowGregorianClock = source.ShowGregorianClock,
                GregorianClockSide = source.GregorianClockSide,
                ShowGregorianSeparator = source.ShowGregorianSeparator,
                GregorianDateFormat = source.GregorianDateFormat,
                ShowHolidayPanel = source.ShowHolidayPanel,
                HolidayPanelSide = source.HolidayPanelSide,
                ShowHolidaySeparator = source.ShowHolidaySeparator,
                UseCustomBackgroundColor = source.UseCustomBackgroundColor,
                WidgetBackgroundColorHex = source.WidgetBackgroundColorHex,
                WidgetBackgroundOpacity = source.WidgetBackgroundOpacity,
                UseWidgetBorder = source.UseWidgetBorder,
                WidgetBorderColorHex = source.WidgetBorderColorHex,
                WidgetBorderThickness = source.WidgetBorderThickness,
                LocationName = source.LocationName,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                ElevationMeters = source.ElevationMeters,
                TimeZoneId = source.TimeZoneId,
                HebrewDayChangeMode = source.HebrewDayChangeMode,
                CandleLightingMinutesBeforeSunset = source.CandleLightingMinutesBeforeSunset,
                TzeitHakochavimMinutesAfterSunset = source.TzeitHakochavimMinutesAfterSunset,
                DefaultZmanCalculationMethod = source.DefaultZmanCalculationMethod,
                ZmanCustomizations = source.ZmanCustomizations.Select(c => new ZmanCustomization
                {
                    BaseZmanName = c.BaseZmanName,
                    CustomName = c.CustomName,
                    MethodOverride = c.MethodOverride,
                }).ToList(),
                ZmanDuplicateRows = source.ZmanDuplicateRows.Select(d => new ZmanDuplicateRow
                {
                    Id = d.Id,
                    BaseZmanName = d.BaseZmanName,
                    CustomName = d.CustomName,
                    Method = d.Method,
                }).ToList(),
                VisibleZmanNames = source.VisibleZmanNames is null ? null : new List<string>(source.VisibleZmanNames),
                UseManualDateTime = source.UseManualDateTime,
                ManualDateTimeBaseTicks = source.ManualDateTimeBaseTicks,
                ManualDateTimeSetAtUtcTicks = source.ManualDateTimeSetAtUtcTicks,
                Use12HourFormat = source.Use12HourFormat,
                ShowSecondsInTime = source.ShowSecondsInTime,
                NotificationsEnabled = source.NotificationsEnabled,
                DisableNotificationsOnShabbatAndChagim = source.DisableNotificationsOnShabbatAndChagim,
                NotificationShowPopup = source.NotificationShowPopup,
                NotificationToastDurationSeconds = source.NotificationToastDurationSeconds,
                SnoozeDurationMinutes = source.SnoozeDurationMinutes,
                NotificationToastDarkBackground = source.NotificationToastDarkBackground,
                NotificationToastPositionMode = source.NotificationToastPositionMode,
                NotificationToastCustomX = source.NotificationToastCustomX,
                NotificationToastCustomY = source.NotificationToastCustomY,
                NotificationPlaySound = source.NotificationPlaySound,
                NotificationSoundSource = source.NotificationSoundSource,
                NotificationFixedSoundName = source.NotificationFixedSoundName,
                NotificationCustomSoundPath = source.NotificationCustomSoundPath,
                NotificationVoiceKitFolderName = source.NotificationVoiceKitFolderName,
                ZmanNotificationRules = source.ZmanNotificationRules.Select(CloneZmanRule).ToList(),
                AdvancedNotificationRules = source.AdvancedNotificationRules.Select(CloneAdvancedRule).ToList(),
                ZmanimPopupDarkMode = source.ZmanimPopupDarkMode,
                OverlayEnabled = source.OverlayEnabled,
                OverlayShowTime = source.OverlayShowTime,
                OverlayShowGregorianDate = source.OverlayShowGregorianDate,
                OverlayShowHebrewDate = source.OverlayShowHebrewDate,
                OverlayShowDayAndParasha = source.OverlayShowDayAndParasha,
                OverlayShowHoliday = source.OverlayShowHoliday,
                OverlayPositionMode = source.OverlayPositionMode,
                OverlayCustomX = source.OverlayCustomX,
                OverlayCustomY = source.OverlayCustomY,
                OverlayFontFamilyName = source.OverlayFontFamilyName,
                OverlayFontSize = source.OverlayFontSize,
                OverlayTextColorHex = source.OverlayTextColorHex,
                OverlayAlwaysOnTop = source.OverlayAlwaysOnTop,
                OverlayTimeStyle = CloneOverlayItemStyle(source.OverlayTimeStyle),
                OverlayGregorianDateStyle = CloneOverlayItemStyle(source.OverlayGregorianDateStyle),
                OverlayHebrewDateStyle = CloneOverlayItemStyle(source.OverlayHebrewDateStyle),
                OverlayDayParashaStyle = CloneOverlayItemStyle(source.OverlayDayParashaStyle),
                OverlayHolidayStyle = CloneOverlayItemStyle(source.OverlayHolidayStyle),
                OverlayItemOrder = new List<string>(source.OverlayItemOrder),
            };
        }

        private static ZmanNotificationRule CloneZmanRule(ZmanNotificationRule source)
        {
            return new ZmanNotificationRule
            {
                ZmanName = source.ZmanName,
                Enabled = source.Enabled,
                MinutesBefore = source.MinutesBefore,
                SoundOverridePath = source.SoundOverridePath,
                SoundOverrideFixedName = source.SoundOverrideFixedName,
                SoundOverrideUseVoice = source.SoundOverrideUseVoice,
            };
        }

        private static AdvancedNotificationRule CloneAdvancedRule(AdvancedNotificationRule source)
        {
            return new AdvancedNotificationRule
            {
                Id = source.Id,
                ZmanName = source.ZmanName,
                MinutesBefore = source.MinutesBefore,
                Enabled = source.Enabled,
                ShowPopup = source.ShowPopup,
                ToastDurationSeconds = source.ToastDurationSeconds,
                ToastDarkBackground = source.ToastDarkBackground,
                PlaySound = source.PlaySound,
                SoundSource = source.SoundSource,
                SoundPath = source.SoundPath,
                FixedSoundName = source.FixedSoundName,
                VoiceKitFolderName = source.VoiceKitFolderName,
            };
        }

        /// <summary>שיבוט עמוק - כדי שעריכת ה"עותק לעבודה" בפאנל ההגדרות לא תשנה בטעות (דרך רפרנס משותף) את אובייקט ה-OverlayItemStyle של ה-Singleton המקורי לפני לחיצה על "שמור".</summary>
        private static OverlayItemStyle CloneOverlayItemStyle(OverlayItemStyle source)
        {
            return new OverlayItemStyle
            {
                UseCustomStyle = source.UseCustomStyle,
                FontFamilyName = source.FontFamilyName,
                FontSize = source.FontSize,
                ColorHex = source.ColorHex,
            };
        }

        /// <summary>
        /// ממלא את תיבת בחירת המיקום מתוך מערך LocationPresets (ישראל, ואז
        /// חו"ל, לפי סדר ההגדרה שלו), ומוסיף בסוף פריט "מותאם אישית..." -
        /// כך שכל שאר הקוד (שמזהה "מותאם אישית" לפי אינדקס מחוץ לתחום המערך)
        /// ממשיך לעבוד בלי שינוי.
        /// </summary>
        private void BuildLocationPresetItems()
        {
            LocationPresetComboBox.Items.Clear();

            foreach ((string name, _, _, _, _) in LocationPresets)
            {
                LocationPresetComboBox.Items.Add(new ComboBoxItem { Content = name });
            }

            LocationPresetComboBox.Items.Add(new ComboBoxItem { Content = "מותאם אישית..." });

            FitComboBoxWidthToContent(LocationPresetComboBox, LocationPresets.Select(p => p.Name).Append("מותאם אישית..."));
        }

        /// <summary>
        /// קובעת MinWidth לתיבת בחירה (ComboBox) לפי רוחב הטקסט הרחב ביותר
        /// מבין הפריטים הנתונים (נמדד בפועל בגופן/גודל הגופן הנוכחיים של
        /// התיבה עצמה) - כדי שהתיבה תתאים בדיוק לתוכן הארוך ביותר שהיא
        /// עשויה להציג, במקום להימתח סתם לכל הרוחב הפנוי בפאנל (Stretch,
        /// ברירת המחדל). מוסיפים לתוצאת המדידה מרווח קבוע לריפוד הפנימי
        /// (ראו Padding בסגנון ComboBox הגלובלי) ולעמודת החץ שלצידו.
        /// </summary>
        private static void FitComboBoxWidthToContent(ComboBox combo, IEnumerable<string> itemTexts)
        {
            var typeface = new Typeface(combo.FontFamily, combo.FontStyle, combo.FontWeight, combo.FontStretch);
            double maxTextWidth = 0;

            foreach (string text in itemTexts)
            {
                var formatted = new FormattedText(
                    text ?? string.Empty,
                    CultureInfo.CurrentCulture,
                    FlowDirection.RightToLeft,
                    typeface,
                    combo.FontSize,
                    Brushes.Black,
                    1.0);

                maxTextWidth = Math.Max(maxTextWidth, formatted.WidthIncludingTrailingWhitespace);
            }

            combo.MinWidth = Math.Ceiling(maxTextWidth) + 46;
        }

        /// <summary>מזהי תגית קבועים לפריטי תיבת בחירת הצליל (בשורות הזמן ובעורך ההתראה המתקדמת) - ראו SetSoundComboSelection/ReadSoundComboSelection.</summary>
        private const string SoundComboDefaultTag = "__default__";
        private const string SoundComboVoiceTag = "__voice__";
        private const string SoundComboBrowseTag = "__browse__";
        private const string SoundComboFilePrefix = "file:";

        /// <summary>בונה את שורות רשימת ההתראות הראשית (זמן + תיבת סימון + דקות-לפני + צליל מיוחד + נסוי) - שורה לכל זמן גלוי כרגע (כולל שורות "זמן כפול", בשמן המוצג בפועל) לפי רשימת "אילו זמנים להציג" הנוכחית (החיה, לא בהכרח השמורה) - מיושרות זו מתחת לזו באמצעות SharedSizeGroup על עמודת שם הזמן (ראו Grid.IsSharedSizeScope ב-XAML).</summary>
        private void BuildZmanRulesPanel()
        {
            _zmanRuleRows.Clear();
            ZmanimRulesPanel.Children.Clear();

            IEnumerable<Services.ZmanEntry> visibleEntries = ComputeZmanEntriesForSettingsUi()
                .Where(entry => !_zmanVisibilityCheckBoxes.TryGetValue(entry.Key, out CheckBox? visibilityBox) || visibilityBox.IsChecked == true);

            foreach (Services.ZmanEntry entry in visibleEntries)
            {
                var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "ZmanNameCol" });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var checkBox = new CheckBox { Content = entry.DisplayName, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(checkBox, 0);

                var minutesBox = new TextBox
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    ToolTip = "זמן לפני, בתבנית שעות:דקות (למשל 0:10 או 1:15)",
                };
                TimeMaskTextBoxBehavior.Attach(minutesBox);
                Grid.SetColumn(minutesBox, 2);

                var soundCombo = new ComboBox { VerticalAlignment = VerticalAlignment.Center };
                BuildSoundComboBoxItems(soundCombo);
                Grid.SetColumn(soundCombo, 4);

                // מתג "נסוי" מעוצב כמו שאר מתגי-הפעולה בתוכנה (ToggleActionButtonStyle -
                // "כדור" מעוגל שמודגש בצבע ההדגשה כשלחוץ), במקום הכרום הלא-מעוצב
                // המובנה של WPF. הגובה (28) מותאם בכוונה לגובה תפריט הבחירה
                // (soundCombo) שיושב לצידו באותה שורה - כדי שהשורה כולה תיראה
                // מיושרת ואחידה.
                var testToggle = new ToggleButton
                {
                    Content = "נסוי",
                    VerticalAlignment = VerticalAlignment.Center,
                    Height = 28,
                    MinWidth = 58,
                    Style = (Style)FindResource("ToggleActionButtonStyle"),
                };
                Grid.SetColumn(testToggle, 6);

                grid.Children.Add(checkBox);
                grid.Children.Add(minutesBox);
                grid.Children.Add(soundCombo);
                grid.Children.Add(testToggle);

                var row = new ZmanRuleRow
                {
                    ZmanName = entry.Key,
                    EnabledCheckBox = checkBox,
                    MinutesTextBox = minutesBox,
                    SoundComboBox = soundCombo,
                    TestToggle = testToggle,
                };

                checkBox.Checked += (_, _) => UpdateZmanRuleRowEnabledState(row);
                checkBox.Unchecked += (_, _) => UpdateZmanRuleRowEnabledState(row);

                soundCombo.SelectionChanged += (_, _) => ZmanRuleSoundComboBox_SelectionChanged(soundCombo);

                testToggle.Checked += (_, _) =>
                {
                    SimulateZmanRuleTest(row);
                    testToggle.IsChecked = false;
                };

                _zmanRuleRows.Add(row);
                ZmanimRulesPanel.Children.Add(grid);
            }
        }

        /// <summary>
        /// בונה מחדש את שתי הרשימות התלויות בהגדרות הזמנים ("אילו זמנים
        /// להציג" ורשימת ההתראות הראשית) יחד, תוך שמירה ושחזור של המצב
        /// שהיה לכל שורה קיימת בכל אחת מהן (נראות; הפעלה/דקות-לפני/צליל
        /// של התראה) - כדי שעריכת זמן אחד (שם/שיטה/כפילות) לא תאבד בטעות
        /// הגדרות שהוגדרו לזמנים אחרים. יש לקרוא לזה בכל פעם שמשהו שמשפיע
        /// על רשימת/שמות הזמנים משתנה (ראו OpenZmanEditDialog).
        /// </summary>
        private void RefreshZmanPanelsPreservingState()
        {
            var previousVisibility = _zmanVisibilityCheckBoxes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IsChecked == true);
            var previousRules = _zmanRuleRows.ToDictionary(
                r => r.ZmanName,
                r =>
                {
                    (string? path, string? fixedName, bool useVoice) = ReadSoundComboSelection(r.SoundComboBox);
                    return (Enabled: r.EnabledCheckBox.IsChecked == true, Minutes: r.MinutesTextBox.Text, SoundPath: path, SoundFixed: fixedName, SoundUseVoice: useVoice);
                });

            BuildZmanVisibilityPanel();
            foreach ((string zmanKey, CheckBox refreshedCheckBox) in _zmanVisibilityCheckBoxes)
            {
                refreshedCheckBox.IsChecked = previousVisibility.TryGetValue(zmanKey, out bool wasVisible) ? wasVisible : true;
            }

            BuildZmanRulesPanel();
            foreach (ZmanRuleRow row in _zmanRuleRows)
            {
                if (previousRules.TryGetValue(row.ZmanName, out var prev))
                {
                    row.EnabledCheckBox.IsChecked = prev.Enabled;
                    row.MinutesTextBox.Text = prev.Minutes;
                    row.MinutesTextBox.IsEnabled = prev.Enabled;
                    row.TestToggle.IsEnabled = prev.Enabled;
                    SetSoundComboSelection(row.SoundComboBox, prev.SoundPath, prev.SoundFixed, prev.SoundUseVoice);
                }
                else
                {
                    row.EnabledCheckBox.IsChecked = false;
                    row.MinutesTextBox.Text = FormatMinutesAsHhMm(10);
                    row.MinutesTextBox.IsEnabled = false;
                    row.TestToggle.IsEnabled = false;
                }
            }
        }

        /// <summary>מיפוי שם זמן -> תיבת הסימון שלו ברשימת "אילו זמנים להציג" (מיקום וזמנים) - נבנה מחדש בכל BuildZmanVisibilityPanel (כולל אחרי כל שינוי בחלונית עריכת זמן), נקרא בזמן טעינה/שמירה.</summary>
        private readonly Dictionary<string, CheckBox> _zmanVisibilityCheckBoxes = new();

        /// <summary>"כמה דקות לפני השקיעה" עבור "הדלקת נרות" - נערך דרך ZmanEditDialog (לא עוד תיבת הזנה ישירות בשורה) - ראו AppSettings.CandleLightingMinutesBeforeSunset.</summary>
        private int _candleLightingMinutesBeforeSunset = 40;

        /// <summary>"כמה דקות אחרי השקיעה" עבור "צאת הכוכבים" - null (ברירת המחדל) = לפי שיטת החישוב. נערך דרך ZmanEditDialog - ראו AppSettings.TzeitHakochavimMinutesAfterSunset.</summary>
        private int? _tzeitMinutesAfterSunsetOverride;

        /// <summary>התאמות אישיות (שם/שיטת חישוב) לזמנים בודדים, לפי השם הקנוני - נערכות דרך ZmanEditDialog. רק זמנים שיש להם התאמה בפועל מופיעים כאן.</summary>
        private readonly Dictionary<string, ZmanCustomization> _zmanCustomizations = new();

        /// <summary>שורות "זמן כפול" הקיימות כרגע - ראו AppSettings.ZmanDuplicateRow.</summary>
        private readonly List<ZmanDuplicateRow> _zmanDuplicateRows = new();

        /// <summary>
        /// בונה את רשימת "אילו זמנים להציג" בלשונית "מיקום וזמנים" - שורה
        /// לכל זמן (כולל כל "שורת זמן כפולה" קיימת, ממוקמת בסמוך לזמן
        /// הבסיסי שלה, לפי סדר כרונולוגי - המוקדם קודם) בדיוק כפי שהיא
        /// תופיע בפועל בלוח הזמנים/בהתראות: תיבת סימון (נראות - פועלת
        /// בדיוק אותו דבר לזמן רגיל ולשורה כפולה, לכל אחת התיבה שלה) וקישור
        /// בשם הזמן שהקשה עליו פותחת את חלונית העריכה (ZmanEditDialog) -
        /// לחיצה על שורה כפולה פותחת את אותה חלונית בדיוק כמו הבסיס שלה,
        /// כי מדובר באותה הגדרה. גם מציגה את הזמן המחושב כרגע לצד כל שורה,
        /// לשם התרשמות מיידית על ההבדל בין שיטות החישוב.
        /// נקראת מחדש אחרי כל שינוי בחלונית העריכה, כדי לשקף שמות/כפילות/
        /// זמנים מעודכנים מיד.
        /// </summary>
        private void BuildZmanVisibilityPanel()
        {
            _zmanVisibilityCheckBoxes.Clear();
            ZmanVisibilityPanel.Children.Clear();

            IReadOnlyList<Services.ZmanEntry> entries = ComputeZmanEntriesForSettingsUi();
            var baseNamesSet = new HashSet<string>(Services.ZmanimCalendar.AllZmanNames);

            foreach (Services.ZmanEntry entry in entries)
            {
                bool isDuplicate = !baseNamesSet.Contains(entry.Key);
                string dialogTargetKey = isDuplicate
                    ? _zmanDuplicateRows.FirstOrDefault(d => d.Id == entry.Key)?.BaseZmanName ?? entry.Key
                    : entry.Key;

                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };

                var checkBox = new CheckBox { VerticalAlignment = VerticalAlignment.Center, IsChecked = true };
                row.Children.Add(checkBox);
                _zmanVisibilityCheckBoxes[entry.Key] = checkBox;

                var nameText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
                var nameLink = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(entry.DisplayName)) { Tag = dialogTargetKey };
                nameLink.Click += ZmanNameLink_Click;
                nameText.Inlines.Add(nameLink);
                row.Children.Add(nameText);

                var timeText = new TextBlock
                {
                    Text = entry.Time.HasValue ? AppTimeService.FormatZmanTime(entry.Time.Value) : "—",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0),
                    FontSize = 11,
                    Foreground = (Brush)FindResource("SecondaryForegroundBrush"),
                };
                row.Children.Add(timeText);

                ZmanVisibilityPanel.Children.Add(row);
            }
        }

        /// <summary>
        /// מחשבת את רשימת הזמנים לתצוגה בפאנל ההגדרות (לשונית "מיקום וזמנים"
        /// ולשונית "התראות") - עם forceIncludeCandleLighting כדי ש"הדלקת
        /// נרות" תמיד תופיע ברשימות ההגדרות עצמן, לא רק בימים רלוונטיים
        /// בפועל (בניגוד לתצוגת "היום" האמיתית - הפופ-אפ/התראות בפועל).
        /// עטופה ב-try/catch: אם המיקום/ההגדרות עדיין לא תקינים (למשל
        /// באמצע טעינה) פשוט מחזירה רשימה ריקה, במקום לזרוק שגיאה שתקריס
        /// את פתיחת פאנל ההגדרות כולו.
        /// </summary>
        private IReadOnlyList<Services.ZmanEntry> ComputeZmanEntriesForSettingsUi()
        {
            try
            {
                var method = (ZmanCalculationMethod)ZmanCalculationMethodComboBox.SelectedIndex;
                return Services.ZmanimCalendar.Calculate(
                    AppTimeService.Today(), SettingsService.BuildLocation(),
                    _candleLightingMinutesBeforeSunset, _tzeitMinutesAfterSunsetOverride,
                    method, _zmanCustomizations.Values.ToList(), _zmanDuplicateRows,
                    forceIncludeCandleLighting: true);
            }
            catch
            {
                return Array.Empty<Services.ZmanEntry>();
            }
        }

        /// <summary>לחיצה על שם זמן (לא על תיבת הסימון!) פותחת את חלונית העריכה - ראו הערה מפורטת ב-BuildZmanVisibilityPanel.</summary>
        private void ZmanNameLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Documents.Hyperlink { Tag: string baseZmanName })
            {
                OpenZmanEditDialog(baseZmanName);
            }
        }

        /// <summary>
        /// מחשבת מה יהיה הזמן של baseZmanName אם היה נדרס לשיטה method
        /// ספציפית - לשימוש בתצוגת "זמן נוכחי" החיה בחלונית העריכה (ראו
        /// ZmanEditDialog.ComputeTimeForMethod). מחשבת מחדש בכל קריאה (לא
        /// יקר במיוחד - כמה קריאות טריגונומטריות - ורק בתגובה לאינטראקציה
        /// של המשתמש, לא בלולאה חמה).
        /// </summary>
        private DateTime? ComputeZmanTimeForMethod(string baseZmanName, ZmanCalculationMethod method)
        {
            try
            {
                var testCustomizations = _zmanCustomizations.Values
                    .Where(c => c.BaseZmanName != baseZmanName)
                    .Append(new ZmanCustomization { BaseZmanName = baseZmanName, MethodOverride = method })
                    .ToList();

                IReadOnlyList<Services.ZmanEntry> testEntries = Services.ZmanimCalendar.Calculate(
                    AppTimeService.Today(), SettingsService.BuildLocation(),
                    _candleLightingMinutesBeforeSunset, _tzeitMinutesAfterSunsetOverride,
                    (ZmanCalculationMethod)ZmanCalculationMethodComboBox.SelectedIndex,
                    testCustomizations, Array.Empty<ZmanDuplicateRow>(),
                    forceIncludeCandleLighting: true);

                return testEntries.FirstOrDefault(e => e.Key == baseZmanName)?.Time;
            }
            catch
            {
                return null;
            }
        }

        private void OpenZmanEditDialog(string baseZmanName)
        {
            _zmanCustomizations.TryGetValue(baseZmanName, out ZmanCustomization? existingCustomization);
            ZmanDuplicateRow? existingDuplicate = _zmanDuplicateRows.FirstOrDefault(d => d.BaseZmanName == baseZmanName);
            var globalMethod = (ZmanCalculationMethod)ZmanCalculationMethodComboBox.SelectedIndex;

            var dialog = new ZmanEditDialog(
                baseZmanName,
                existingCustomization?.CustomName,
                existingCustomization?.MethodOverride,
                _candleLightingMinutesBeforeSunset,
                _tzeitMinutesAfterSunsetOverride,
                existingDuplicate,
                globalMethod,
                m => ComputeZmanTimeForMethod(baseZmanName, m))
            {
                Owner = this,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (baseZmanName == Services.ZmanimCalendar.NameCandleLighting)
            {
                _candleLightingMinutesBeforeSunset = dialog.ResultCandleLightingMinutes;
            }
            else if (baseZmanName == Services.ZmanimCalendar.NameTzeitHakochavim)
            {
                _tzeitMinutesAfterSunsetOverride = dialog.ResultTzeitMinutesOverride;
            }

            if (baseZmanName != Services.ZmanimCalendar.NameCandleLighting &&
                (dialog.ResultCustomName is not null || dialog.ResultMethodOverride is not null))
            {
                _zmanCustomizations[baseZmanName] = new ZmanCustomization
                {
                    BaseZmanName = baseZmanName,
                    CustomName = dialog.ResultCustomName,
                    MethodOverride = dialog.ResultMethodOverride,
                };
            }
            else if (baseZmanName == Services.ZmanimCalendar.NameCandleLighting && dialog.ResultCustomName is not null)
            {
                _zmanCustomizations[baseZmanName] = new ZmanCustomization { BaseZmanName = baseZmanName, CustomName = dialog.ResultCustomName };
            }
            else
            {
                _zmanCustomizations.Remove(baseZmanName);
            }

            _zmanDuplicateRows.RemoveAll(d => d.BaseZmanName == baseZmanName);
            if (dialog.ResultDuplicateRow is not null)
            {
                _zmanDuplicateRows.Add(dialog.ResultDuplicateRow);
            }

            // BuildZmanVisibilityPanel יוצר תיבות-סימון חדשות (מ-Dictionary
            // חדש) לכל השורות - שומרים את מצב הנראות שהיה לכולן לפני
            // הפתיחה ומחזירים אותו לכולן, כדי שעריכת שם/שיטה לזמן אחד לא
            // "תאפס" בטעות את הסימון של שאר הזמנים ברשימה.
            RefreshZmanPanelsPreservingState();
        }

        /// <summary>ממלא תיבת בחירת צליל (בין אם בשורת זמן ובין אם בעורך ההתראה המתקדמת) באותה רשימת אפשרויות קבועה: ברירת מחדל/הקראה קולית/צליל קבוע/בחירת קובץ.</summary>
        private void BuildSoundComboBoxItems(ComboBox combo)
        {
            combo.Items.Add(new ComboBoxItem { Content = "ברירת מחדל", Tag = SoundComboDefaultTag });
            combo.Items.Add(new ComboBoxItem { Content = "הקראה קולית", Tag = SoundComboVoiceTag });

            for (int i = 0; i < FixedSoundKeys.Length; i++)
            {
                combo.Items.Add(new ComboBoxItem { Content = $"צליל {i + 1}", Tag = FixedSoundKeys[i] });
            }

            combo.Items.Add(new ComboBoxItem { Content = "בחירת קובץ מהמחשב...", Tag = SoundComboBrowseTag });
            combo.SelectedIndex = 0;
        }

        /// <summary>קובע את הבחירה הנוכחית בתיבת בחירת צליל (מוסיף/מחליף פריט "קובץ" מיוחד אם צריך), לפי הגדרה שמורה - קובץ מיוחד גובר על צליל קבוע מיוחד, שגובר על הכרזה קולית מיוחדת, וברירת המחדל היא כשאין אף אחד מהם.</summary>
        private static void SetSoundComboSelection(ComboBox combo, string? overridePath, string? overrideFixedName, bool overrideUseVoice)
        {
            for (int i = combo.Items.Count - 1; i >= 0; i--)
            {
                if (combo.Items[i] is ComboBoxItem existing && existing.Tag is string tag && tag.StartsWith(SoundComboFilePrefix, StringComparison.Ordinal))
                {
                    combo.Items.RemoveAt(i);
                }
            }

            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                var fileItem = new ComboBoxItem
                {
                    Content = System.IO.Path.GetFileName(overridePath),
                    Tag = SoundComboFilePrefix + overridePath,
                };
                combo.Items.Insert(combo.Items.Count - 1, fileItem);
                combo.SelectedItem = fileItem;
                return;
            }

            if (!string.IsNullOrWhiteSpace(overrideFixedName))
            {
                foreach (object obj in combo.Items)
                {
                    if (obj is ComboBoxItem item && (string?)item.Tag == overrideFixedName)
                    {
                        combo.SelectedItem = item;
                        return;
                    }
                }
            }

            if (overrideUseVoice)
            {
                foreach (object obj in combo.Items)
                {
                    if (obj is ComboBoxItem item && (string?)item.Tag == SoundComboVoiceTag)
                    {
                        combo.SelectedItem = item;
                        return;
                    }
                }
            }

            combo.SelectedIndex = 0;
        }

        /// <summary>קורא את הבחירה הנוכחית בתיבת בחירת צליל - מחזיר (נתיב קובץ, שם צליל קבוע, האם נבחרה הכרזה קולית מיוחדת); כולם null/false אם נבחרה ברירת המחדל.</summary>
        private static (string? Path, string? FixedName, bool UseVoice) ReadSoundComboSelection(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (tag.StartsWith(SoundComboFilePrefix, StringComparison.Ordinal))
                {
                    return (tag[SoundComboFilePrefix.Length..], null, false);
                }

                if (tag == SoundComboVoiceTag)
                {
                    return (null, null, true);
                }

                if (tag != SoundComboDefaultTag && tag != SoundComboBrowseTag)
                {
                    return (null, tag, false);
                }
            }

            return (null, null, false);
        }

        private void ZmanRuleSoundComboBox_SelectionChanged(ComboBox combo)
        {
            if (_isLoading || combo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
            {
                return;
            }

            if (tag == SoundComboBrowseTag)
            {
                string? path = BrowseForSoundFile();
                if (path is not null)
                {
                    SetSoundComboSelection(combo, path, null, false);
                }
                else
                {
                    combo.SelectedIndex = 0;
                }
            }
        }

        private void UpdateZmanRuleRowEnabledState(ZmanRuleRow row)
        {
            bool enabled = row.EnabledCheckBox.IsChecked == true;
            row.MinutesTextBox.IsEnabled = enabled;
            row.TestToggle.IsEnabled = enabled;
        }

        /// <summary>פותח תיבת דו-שיח לבחירת קובץ שמע מהמחשב (WAV/MP3/WMA או כל קובץ אחר).</summary>
        private static string? BrowseForSoundFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "בחירת קובץ שמע",
                Filter = "קבצי שמע (*.wav;*.mp3;*.wma)|*.wav;*.mp3;*.wma|כל הקבצים (*.*)|*.*",
                CheckFileExists = true,
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <summary>מדמה הצגת התראה בזמן אמת עבור שורת זמן ברשימה הראשית - הודעה קופצת (תמיד) וצליל (אם ההגדרה הכללית "צליל" מסומנת), בדיוק לפי מה שמוגדר בפועל לשורה הזו.</summary>
        /// <summary>קוראת את סוג ההתראה הקולית הנבחר בתפריט הבחירה - לפי הסדר הקבוע ברשימה (הקראה קולית / צליל קבוע / קובץ מהמחשב).</summary>
        private NotificationSoundSourceMode GetSelectedNotificationSoundSource()
        {
            return NotificationSoundSourceComboBox.SelectedIndex switch
            {
                0 => NotificationSoundSourceMode.Voice,
                2 => NotificationSoundSourceMode.CustomFile,
                _ => NotificationSoundSourceMode.Fixed,
            };
        }

        private void SimulateZmanRuleTest(ZmanRuleRow row)
        {
            int minutesBefore = ParseHhMmToMinutes(row.MinutesTextBox.Text, 10);
            Services.ZmanEntry? entry = ResolveTestZmanEntry(row.ZmanName);
            string voiceKey = entry?.VoiceKey ?? row.ZmanName;
            string displayName = entry is not null ? Services.ZmanimCalendar.GetPopupDisplayName(entry) : row.ZmanName;

            Action? replaySound = null;
            if (NotificationPlaySoundCheckBox.IsChecked == true)
            {
                (string? path, string? fixedName, bool useVoice) = ReadSoundComboSelection(row.SoundComboBox);
                var previewSettings = new AppSettings
                {
                    NotificationPlaySound = true,
                    NotificationSoundSource = GetSelectedNotificationSoundSource(),
                    NotificationCustomSoundPath = NotificationCustomSoundPathTextBox.Text,
                    NotificationFixedSoundName = FixedSoundKeys[Math.Clamp(NotificationFixedSoundComboBox.SelectedIndex, 0, FixedSoundKeys.Length - 1)],
                    NotificationVoiceKitFolderName = (NotificationVoiceKitComboBox.SelectedItem as ComboBoxItem)?.Tag as string,
                };
                replaySound = () => NotificationSoundService.PlayForZman(previewSettings, path, fixedName, useVoice, voiceKey, minutesBefore, entry?.Time);
            }

            ToastNotificationWindow.Show(displayName, minutesBefore, entry?.Time is DateTime t0 ? AppTimeService.FormatZmanTime(t0) : "--:--", isTest: true, zmanTime: entry?.Time, onSnoozeReplaySound: replaySound);

            replaySound?.Invoke();
        }

        /// <summary>מאתרת את הזמן המחושב היום עבור מפתח זמן נתון (לצורך "נסוי") - null אם לא ניתן היה לחשב אותו היום. משתמשת בנתוני העבודה החיים של החלונית (כולל התאמות אישיות/כפילות שעוד לא נשמרו), לא בהגדרות השמורות - כך שה"נסוי" תמיד משקף את המצב הנוכחי בפועל בפאנל.</summary>
        private Services.ZmanEntry? ResolveTestZmanEntry(string zmanKey)
        {
            return ComputeZmanEntriesForSettingsUi().FirstOrDefault(z => z.Key == zmanKey);
        }

        /// <summary>מחשבת את שעת הזמן להיום לצורך תצוגת "נסוי" - "--:--" אם לא ניתן היה לחשב זמן זה היום (למשל זמן שלא רלוונטי היום).</summary>
        private string ResolveTestTimeText(string zmanName)
        {
            Services.ZmanEntry? entry = ResolveTestZmanEntry(zmanName);
            return entry?.Time is DateTime time ? AppTimeService.FormatZmanTime(time) : "--:--";
        }

        private void LoadFromSettings(AppSettings s)
        {
            _isLoading = true;

            // --- מראה פאנל ההגדרות עצמו ---
            SettingsPanelAppearanceComboBox.SelectedIndex = s.SettingsPanelDarkMode ? 1 : 0;
            ApplyPanelTheme(s.SettingsPanelDarkMode, DefaultDarkBackgroundHex);

            // --- הוידג'ט: מיקום ---
            // "המיקום החופשי" (FreeDrag, נקבע ע"י גרירה עם Ctrl) אין לו
            // רדיו-בוטון נבחר משלו - מבחינת הרשימה למעלה הוא מוצג כאילו
            // האופציה השנייה ("מרחק מותאם אישית מקצה") נבחרה, עם הודעה
            // קטנה שמבהירה שזה בעצם מיקום גרירה. ברגע שהמשתמש בפועל יבחר/
            // ישנה משהו בהגדרות המרחק המותאם אישית, המיקום החופשי יוחלף.
            _freeDragPreserved = s.PositionMode == WidgetPositionMode.FreeDrag && s.FreeDragLeft.HasValue && s.FreeDragTop.HasValue;

            ShowWidgetCheckBox.IsChecked = s.ShowWidget;
            PositionChevronRadio.IsChecked = s.PositionMode == WidgetPositionMode.ChevronAttached;
            PositionCustomEdgeRadio.IsChecked = s.PositionMode == WidgetPositionMode.CustomEdgeOffset || _freeDragPreserved;

            CustomOffsetSideComboBox.SelectedIndex = s.CustomOffsetSide == WidgetAttachSide.Left ? 1 : 0;
            CustomOffsetPixelsTextBox.Text = s.CustomOffsetPixels.ToString(CultureInfo.InvariantCulture);
            LockWidgetPositionCheckBox.IsChecked = s.LockWidgetPosition;
            LockOverlayPositionCheckBox.IsChecked = s.LockOverlayPosition;
            CustomEdgeOffsetPanel.Visibility = PositionCustomEdgeRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            // --- הוידג'ט: גופן וצבע ---
            UseCustomFontCheckBox.IsChecked = s.UseCustomFont;
            FontFamilyTextBox.Text = s.FontFamilyName ?? "Segoe UI";
            FontSizeTextBox.Text = s.FontSize.ToString(CultureInfo.InvariantCulture);
            FontFamilyTextBox.IsEnabled = s.UseCustomFont;
            FontSizeTextBox.IsEnabled = s.UseCustomFont;

            UseCustomColorCheckBox.IsChecked = s.UseCustomTextColor;
            TextColorPicker.LoadSilently(s.CustomTextColorHex);
            TextColorPicker.IsEnabled = s.UseCustomTextColor;

            // --- הפעלה אוטומטית / הסתרת תצוגה מקורית / שעון לועזי משולב ---
            // הערה: קריאת המצב האמיתי של הפעלה אוטומטית נעשית מה-Registry
            // עצמו (StartupService.IsEnabled) ולא מה-JSON, כדי לשקף נכון גם
            // אם מישהו שינה זאת מחוץ לאפליקציה.
            ZmanimPopupAlignmentComboBox.SelectedIndex = (int)s.ZmanimPopupAlignment;
            StartWithWindowsCheckBox.IsChecked = StartupService.IsEnabled();
            CheckForUpdatesCheckBox.IsChecked = s.CheckForUpdates;
            RefreshUpdateUi();
            HideWindowsClockCheckBox.IsChecked = s.HideWindowsClock;
            ReduceGapCheckBox.IsChecked = s.HideWindowsClockReduceGap;
            ExplorerAutoLaunchModeComboBox.SelectedIndex = (int)s.ExplorerAutoLaunchMode;
            ReduceGapPanel.IsEnabled = s.HideWindowsClock;
            ShowGregorianClockCheckBox.IsChecked = s.ShowGregorianClock;
            GregorianClockSidePanel.IsEnabled = s.ShowGregorianClock;
            SelectComboItemByTag(GregorianDateFormatComboBox, s.GregorianDateFormat, fallbackIndex: 0);
            GregorianSideRightRadio.IsChecked = s.GregorianClockSide == WidgetAttachSide.Right;
            GregorianSideLeftRadio.IsChecked = s.GregorianClockSide == WidgetAttachSide.Left;
            ShowGregorianSeparatorCheckBox.IsChecked = s.ShowGregorianSeparator;

            ShowHolidayPanelCheckBox.IsChecked = s.ShowHolidayPanel;
            HolidayPanelSidePanel.IsEnabled = s.ShowHolidayPanel;
            HolidaySideRightRadio.IsChecked = s.HolidayPanelSide == HolidayPanelPosition.FarRight;
            HolidaySideLeftRadio.IsChecked = s.HolidayPanelSide == HolidayPanelPosition.FarLeft;
            HolidaySideBetweenRadio.IsChecked = s.HolidayPanelSide == HolidayPanelPosition.BetweenHebrewAndGregorian;
            ShowHolidaySeparatorCheckBox.IsChecked = s.ShowHolidaySeparator;

            // --- הוידג'ט: רקע ---
            UseCustomBackgroundCheckBox.IsChecked = s.UseCustomBackgroundColor;
            BackgroundColorPicker.ShowOpacitySlider = true;
            BackgroundColorPicker.LoadSilently(s.WidgetBackgroundColorHex, s.WidgetBackgroundOpacity);
            BackgroundColorPicker.IsEnabled = s.UseCustomBackgroundColor;

            // --- הוידג'ט: קו מתאר ---
            UseWidgetBorderCheckBox.IsChecked = s.UseWidgetBorder;
            WidgetBorderColorPicker.LoadSilently(s.WidgetBorderColorHex);
            WidgetBorderColorPicker.IsEnabled = s.UseWidgetBorder;
            WidgetBorderThicknessTextBox.Text = s.WidgetBorderThickness.ToString(CultureInfo.InvariantCulture);
            WidgetBorderThicknessTextBox.IsEnabled = s.UseWidgetBorder;

            // --- תאריך ושעה ---
            UseManualDateTimeCheckBox.IsChecked = s.UseManualDateTime;
            ManualDateTimePanel.IsEnabled = s.UseManualDateTime;
            DateTime manualBasis = AppTimeService.Now();
            ManualDatePicker.SelectedDate = manualBasis.Date;
            ManualTimeTextBox.Text = manualBasis.ToString("HH:mm", CultureInfo.InvariantCulture);
            TimeFormat24Radio.IsChecked = !s.Use12HourFormat;
            TimeFormat12Radio.IsChecked = s.Use12HourFormat;
            ShowSecondsCheckBox.IsChecked = s.ShowSecondsInTime;

            // --- מיקום וזמנים ---
            int presetIndex = Array.FindIndex(LocationPresets, p => p.Name == s.LocationName);
            LocationPresetComboBox.SelectedIndex = presetIndex >= 0 ? presetIndex : LocationPresets.Length; // "מותאם אישית"
            LatitudeTextBox.Text = s.Latitude.ToString(CultureInfo.InvariantCulture);
            LongitudeTextBox.Text = s.Longitude.ToString(CultureInfo.InvariantCulture);
            ElevationTextBox.Text = s.ElevationMeters.ToString(CultureInfo.InvariantCulture);
            TimeZoneTextBox.Text = s.TimeZoneId;
            bool isCustomLocation = presetIndex < 0;
            LatitudeTextBox.IsEnabled = isCustomLocation;
            LongitudeTextBox.IsEnabled = isCustomLocation;
            ElevationTextBox.IsEnabled = isCustomLocation;
            TimeZoneTextBox.IsEnabled = isCustomLocation;

            HebrewDayChangeMidnightRadio.IsChecked = s.HebrewDayChangeMode == HebrewDayChangeMode.Midnight;
            HebrewDayChangeSunsetRadio.IsChecked = s.HebrewDayChangeMode == HebrewDayChangeMode.AtSunset;
            HebrewDayChangeTzeitRadio.IsChecked = s.HebrewDayChangeMode == HebrewDayChangeMode.AtTzeitHakochavim;

            ZmanCalculationMethodComboBox.SelectedIndex = (int)s.DefaultZmanCalculationMethod;

            // --- זמנים: התאמות אישיות/כפילות - נטענים *לפני* בניית הרשימה
            // (BuildZmanVisibilityPanel), כדי שהיא תציג מיד שמות מותאמים
            // אישית ושורות כפולות קיימות, לא רק את ברירת המחדל. --- 
            _candleLightingMinutesBeforeSunset = s.CandleLightingMinutesBeforeSunset;
            _tzeitMinutesAfterSunsetOverride = s.TzeitHakochavimMinutesAfterSunset;

            _zmanCustomizations.Clear();
            foreach (ZmanCustomization customization in s.ZmanCustomizations)
            {
                _zmanCustomizations[customization.BaseZmanName] = new ZmanCustomization
                {
                    BaseZmanName = customization.BaseZmanName,
                    CustomName = customization.CustomName,
                    MethodOverride = customization.MethodOverride,
                };
            }

            _zmanDuplicateRows.Clear();
            foreach (ZmanDuplicateRow duplicate in s.ZmanDuplicateRows)
            {
                _zmanDuplicateRows.Add(new ZmanDuplicateRow
                {
                    Id = duplicate.Id,
                    BaseZmanName = duplicate.BaseZmanName,
                    CustomName = duplicate.CustomName,
                    Method = duplicate.Method,
                });
            }

            BuildZmanVisibilityPanel();

            foreach ((string name, CheckBox checkBox) in _zmanVisibilityCheckBoxes)
            {
                checkBox.IsChecked = s.IsZmanVisible(name);
            }

            // חובה לבנות מחדש כאן (לא להסתפק בבנייה החד-פעמית שכבר קרתה
            // בבנאי) - שם עדיין לא היו טעונים ההתאמות האישיות/הכפילות/
            // מצב הנראות בפועל שזה עתה נטענו למעלה, כך שהרשימה הייתה
            // משקפת מצב ריק/ברירת-מחדל בלבד.
            BuildZmanRulesPanel();

            // --- התראות ---
            NotificationsEnabledCheckBox.IsChecked = s.NotificationsEnabled;
            DisableNotificationsOnShabbatCheckBox.IsChecked = s.DisableNotificationsOnShabbatAndChagim;
            NotificationsDetailsPanel.IsEnabled = s.NotificationsEnabled;
            NotificationShowPopupCheckBox.IsChecked = s.NotificationShowPopup;
            ToastSettingsAccordionHeader.Visibility = s.NotificationShowPopup ? Visibility.Visible : Visibility.Collapsed;
            NotificationToastPositionComboBox.SelectedIndex = (int)s.NotificationToastPositionMode;
            NotificationToastCustomPositionPanel.Visibility = s.NotificationToastPositionMode == ToastPositionMode.Custom ? Visibility.Visible : Visibility.Collapsed;
            NotificationToastCustomPositionExplanationText.Visibility = s.NotificationToastPositionMode == ToastPositionMode.Custom ? Visibility.Visible : Visibility.Collapsed;
            NotificationToastCustomXTextBox.Text = s.NotificationToastCustomX.ToString(CultureInfo.InvariantCulture);
            NotificationToastCustomYTextBox.Text = s.NotificationToastCustomY.ToString(CultureInfo.InvariantCulture);
            NotificationToastDurationTextBox.Text = s.NotificationToastDurationSeconds.ToString(CultureInfo.InvariantCulture);
            SnoozeDurationMinutesTextBox.Text = s.SnoozeDurationMinutes.ToString(CultureInfo.InvariantCulture);
            NotificationToastBackgroundComboBox.SelectedIndex = s.NotificationToastDarkBackground ? 0 : 1;
            NotificationPlaySoundCheckBox.IsChecked = s.NotificationPlaySound;
            NotificationSoundDetailsPanel.IsEnabled = s.NotificationPlaySound;
            NotificationSoundSourceComboBox.SelectedIndex = s.NotificationSoundSource switch
            {
                NotificationSoundSourceMode.Voice => 0,
                NotificationSoundSourceMode.CustomFile => 2,
                _ => 1,
            };
            int fixedSoundIndex = Array.IndexOf(FixedSoundKeys, s.NotificationFixedSoundName);
            NotificationFixedSoundComboBox.SelectedIndex = fixedSoundIndex >= 0 ? fixedSoundIndex : 0;
            NotificationCustomSoundPathTextBox.Text = s.NotificationCustomSoundPath ?? string.Empty;
            UpdateNotificationSoundSourcePanels();

            RefreshVoiceKitComboBox(NotificationVoiceKitComboBox);
            SelectVoiceKitInComboBox(NotificationVoiceKitComboBox, s.NotificationVoiceKitFolderName);

            foreach (ZmanRuleRow row in _zmanRuleRows)
            {
                ZmanNotificationRule rule = s.ZmanNotificationRules.FirstOrDefault(r => r.ZmanName == row.ZmanName)
                    ?? new ZmanNotificationRule { ZmanName = row.ZmanName, Enabled = false, MinutesBefore = 10 };

                row.EnabledCheckBox.IsChecked = rule.Enabled;
                row.MinutesTextBox.Text = FormatMinutesAsHhMm(rule.MinutesBefore);
                row.MinutesTextBox.IsEnabled = rule.Enabled;
                row.TestToggle.IsEnabled = rule.Enabled;
                SetSoundComboSelection(row.SoundComboBox, rule.SoundOverridePath, rule.SoundOverrideFixedName, rule.SoundOverrideUseVoice);
            }

            _workingAdvancedRules = s.AdvancedNotificationRules.Select(CloneAdvancedRule).ToList();
            AdvancedRuleEditorBorder.Visibility = Visibility.Collapsed;
            _editingAdvancedRuleId = null;
            RefreshAdvancedRulesList();

            // --- שולחן עבודה ---
            OverlayEnabledCheckBox.IsChecked = s.OverlayEnabled;
            OverlayDetailsPanel.IsEnabled = s.OverlayEnabled;

            // --- שולחן עבודה: "מה להציג" - סימון תצוגה + סדר הצגה, כעת
            // ברשימה אחת משולבת (ראו RefreshOverlayOrderPanel). חובה
            // לאתחל את _workingOverlayOrder ואת _overlayShowState *לפני*
            // הקריאה ל-RefreshOverlayOrderPanel, כי היא בונה את הרשימה לפיהם.
            _workingOverlayOrder = NormalizeOverlayOrder(s.OverlayItemOrder);
            _overlayShowState = new Dictionary<string, bool>
            {
                ["Time"] = s.OverlayShowTime,
                ["DayParasha"] = s.OverlayShowDayAndParasha,
                ["HebrewDate"] = s.OverlayShowHebrewDate,
                ["GregorianDate"] = s.OverlayShowGregorianDate,
                ["Holiday"] = s.OverlayShowHoliday,
            };
            RefreshOverlayOrderPanel();

            OverlayPositionComboBox.SelectedIndex = (int)s.OverlayPositionMode;
            OverlayCustomPositionGrid.Visibility = s.OverlayPositionMode == OverlayPosition.Custom ? Visibility.Visible : Visibility.Collapsed;
            OverlayCustomPositionExplanationText.Visibility = s.OverlayPositionMode == OverlayPosition.Custom ? Visibility.Visible : Visibility.Collapsed;
            OverlayCustomXTextBox.Text = s.OverlayCustomX.ToString(CultureInfo.InvariantCulture);
            OverlayCustomYTextBox.Text = s.OverlayCustomY.ToString(CultureInfo.InvariantCulture);
            OverlayFontFamilyTextBox.Text = s.OverlayFontFamilyName;
            OverlayFontSizeTextBox.Text = s.OverlayFontSize.ToString(CultureInfo.InvariantCulture);
            OverlayColorPicker.LoadSilently(s.OverlayTextColorHex);
            OverlayAlwaysOnTopCheckBox.IsChecked = s.OverlayAlwaysOnTop;

            // --- שולחן עבודה: הגדרות מתקדמות (התאמה אישית לכל פריט) ---
            LoadOverlayItemStyle(s.OverlayTimeStyle, OverlayTimeCustomStyleCheckBox, OverlayTimeStylePanel, OverlayTimeFontFamilyTextBox, OverlayTimeFontSizeTextBox, OverlayTimeColorPicker);
            LoadOverlayItemStyle(s.OverlayGregorianDateStyle, OverlayGregorianCustomStyleCheckBox, OverlayGregorianStylePanel, OverlayGregorianFontFamilyTextBox, OverlayGregorianFontSizeTextBox, OverlayGregorianColorPicker);
            LoadOverlayItemStyle(s.OverlayHebrewDateStyle, OverlayHebrewCustomStyleCheckBox, OverlayHebrewStylePanel, OverlayHebrewFontFamilyTextBox, OverlayHebrewFontSizeTextBox, OverlayHebrewColorPicker);
            LoadOverlayItemStyle(s.OverlayDayParashaStyle, OverlayDayParashaCustomStyleCheckBox, OverlayDayParashaStylePanel, OverlayDayParashaFontFamilyTextBox, OverlayDayParashaFontSizeTextBox, OverlayDayParashaColorPicker);
            LoadOverlayItemStyle(s.OverlayHolidayStyle, OverlayHolidayCustomStyleCheckBox, OverlayHolidayStylePanel, OverlayHolidayFontFamilyTextBox, OverlayHolidayFontSizeTextBox, OverlayHolidayColorPicker);

            _isLoading = false;
        }

        /// <summary>מוודא שכל 5 המפתחות התקינים קיימים ברשימה בדיוק פעם אחת (מוסיף בסוף מפתחות חסרים - למשל בהגדרות שנשמרו לפני התוספת - ומתעלם ממפתחות לא-מוכרים).</summary>
        private static List<string> NormalizeOverlayOrder(List<string> source)
        {
            var result = new List<string>();
            foreach (string key in source)
            {
                if (OverlayItemLabels.ContainsKey(key) && !result.Contains(key))
                {
                    result.Add(key);
                }
            }

            foreach (string key in OverlayItemLabels.Keys)
            {
                if (!result.Contains(key))
                {
                    result.Add(key);
                }
            }

            return result;
        }

        /// <summary>
        /// בונה מחדש את רשימת "מה להציג" המשולבת (תיבת סימון + חצי מעלה/
        /// מטה לכל פריט) לפי _workingOverlayOrder/_overlayShowState
        /// הנוכחיים - משלבת את מה שהיה בעבר שתי תכונות נפרדות (תיבות
        /// הסימון הקבועות למעלה בלשונית, וחצי הסדר בתוך "הגדרות מתקדמות").
        /// </summary>
        private void RefreshOverlayOrderPanel()
        {
            OverlayShowOrderPanel.Children.Clear();

            for (int i = 0; i < _workingOverlayOrder.Count; i++)
            {
                string key = _workingOverlayOrder[i];
                string label = OverlayItemLabels.TryGetValue(key, out string? l) ? l : key;
                bool isChecked = _overlayShowState.TryGetValue(key, out bool v) && v;

                var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var checkBox = new CheckBox { Content = label, IsChecked = isChecked, VerticalAlignment = VerticalAlignment.Center, Tag = key };
                checkBox.Checked += OverlayShowCheckBox_CheckedChanged;
                checkBox.Unchecked += OverlayShowCheckBox_CheckedChanged;
                Grid.SetColumn(checkBox, 0);

                var upButton = new Button { Content = "▲", Style = (Style)FindResource("IconActionButtonStyle"), Width = 28, Height = 28, Margin = new Thickness(4, 0, 0, 0), Tag = i, IsEnabled = i > 0 };
                upButton.Click += OverlayOrderUpButton_Click;
                Grid.SetColumn(upButton, 1);

                var downButton = new Button { Content = "▼", Style = (Style)FindResource("IconActionButtonStyle"), Width = 28, Height = 28, Margin = new Thickness(4, 0, 0, 0), Tag = i, IsEnabled = i < _workingOverlayOrder.Count - 1 };
                downButton.Click += OverlayOrderDownButton_Click;
                Grid.SetColumn(downButton, 2);

                row.Children.Add(checkBox);
                row.Children.Add(upButton);
                row.Children.Add(downButton);

                OverlayShowOrderPanel.Children.Add(row);
            }
        }

        private void OverlayShowCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: string key } checkBox)
            {
                _overlayShowState[key] = checkBox.IsChecked == true;
            }
        }

        private void OverlayOrderUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: int index } || index <= 0)
            {
                return;
            }

            (_workingOverlayOrder[index - 1], _workingOverlayOrder[index]) = (_workingOverlayOrder[index], _workingOverlayOrder[index - 1]);
            RefreshOverlayOrderPanel();
        }

        private void OverlayOrderDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: int index } || index >= _workingOverlayOrder.Count - 1)
            {
                return;
            }

            (_workingOverlayOrder[index + 1], _workingOverlayOrder[index]) = (_workingOverlayOrder[index], _workingOverlayOrder[index + 1]);
            RefreshOverlayOrderPanel();
        }

        /// <summary>מזהי הצלילים הקבועים, לפי אותו סדר כמו הפריטים ב-XAML של NotificationFixedSoundComboBox ("צליל 1".."צליל 5").</summary>
        private static readonly string[] FixedSoundKeys = { "Asterisk", "Beep", "Exclamation", "Hand", "Question", "Chime", "Alert", "Bell", "Notify", "Ding" };

        private static void LoadOverlayItemStyle(OverlayItemStyle style, CheckBox useCustomCheckBox, StackPanel panel, TextBox fontFamilyTextBox, TextBox fontSizeTextBox, ColorPickerControl colorPicker)
        {
            useCustomCheckBox.IsChecked = style.UseCustomStyle;
            panel.IsEnabled = style.UseCustomStyle;
            fontFamilyTextBox.Text = style.FontFamilyName;
            fontSizeTextBox.Text = style.FontSize.ToString(CultureInfo.InvariantCulture);
            colorPicker.LoadSilently(style.ColorHex);
        }

        private static OverlayItemStyle SaveOverlayItemStyle(CheckBox useCustomCheckBox, TextBox fontFamilyTextBox, TextBox fontSizeTextBox, ColorPickerControl colorPicker)
        {
            return new OverlayItemStyle
            {
                UseCustomStyle = useCustomCheckBox.IsChecked == true,
                FontFamilyName = string.IsNullOrWhiteSpace(fontFamilyTextBox.Text) ? "Segoe UI" : fontFamilyTextBox.Text.Trim(),
                FontSize = ParseDoubleOrDefault(fontSizeTextBox.Text, 26.0),
                ColorHex = colorPicker.SelectedColorHex,
            };
        }

        private void PositionMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            CustomEdgeOffsetPanel.Visibility = PositionCustomEdgeRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            // ברגע שהמשתמש בוחר במפורש את האופציה הראשונה (צמוד לחץ), המיקום
            // החופשי הקודם (אם היה) יוחלף במצב הזה בעת השמירה - ההודעה כבר
            // לא רלוונטית. בחירה מפורשת של האופציה השנייה לא מבטלת בהכרח את
            // המיקום החופשי בפני עצמה (ראו CustomOffsetSettings_Changed) -
            // רק שינוי בפועל של הגדרות המרחק עצמן עושה זאת.
            if (PositionChevronRadio.IsChecked == true)
            {
                _freeDragPreserved = false;
            }
        }

        /// <summary>נקרא כשהמשתמש נוגע בפועל בהגדרות המרחק המותאם אישית (צד/מרחק בפיקסלים) - מבטל את שימור המיקום החופשי הקודם, כי כעת יש כוונה מפורשת למרחק מדוייק.</summary>
        private void CustomOffsetSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            _freeDragPreserved = false;
        }

        private void CustomOffsetPixelsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CustomOffsetSettings_Changed(sender, e);
        }

        private void CustomOffsetSideComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CustomOffsetSettings_Changed(sender, e);
        }

        private void LocationPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            int index = LocationPresetComboBox.SelectedIndex;
            bool isCustom = index < 0 || index >= LocationPresets.Length;

            LatitudeTextBox.IsEnabled = isCustom;
            LongitudeTextBox.IsEnabled = isCustom;
            ElevationTextBox.IsEnabled = isCustom;
            TimeZoneTextBox.IsEnabled = isCustom;

            if (!isCustom)
            {
                (string name, double lat, double lng, double elevation, string timeZoneId) = LocationPresets[index];
                LatitudeTextBox.Text = lat.ToString(CultureInfo.InvariantCulture);
                LongitudeTextBox.Text = lng.ToString(CultureInfo.InvariantCulture);
                ElevationTextBox.Text = elevation.ToString(CultureInfo.InvariantCulture);
                TimeZoneTextBox.Text = timeZoneId;
            }
        }

        /// <summary>
        /// לחיצה ידנית על הכפתור - מציגה הודעות למשתמש בכל תוצאה (בניגוד
        /// לעדכון אוטומטי-שקט שעשוי להתבצע במקומות אחרים בתוכנה בלי לחיצה
        /// מפורשת, שאמור להישאר תמיד ללא הודעות): הרשאה חסרה/שירות מיקום
        /// כבוי → הודעה שמכוונת להגדרות Windows הרלוונטיות; כישלון אחר
        /// (timeout וכו') → הודעת שגיאה כללית; הצלחה → הודעה שאומרת איזו
        /// עיר נבחרה בפועל.
        /// </summary>
        private async void DetectLocationButton_Click(object sender, RoutedEventArgs e)
        {
            DetectLocationButton.IsEnabled = false;

            try
            {
                GeolocationAccessStatus accessStatus = await Geolocator.RequestAccessAsync();
                if (accessStatus != GeolocationAccessStatus.Allowed)
                {
                    AppMessageBoxWindow.Show(
                        "אין הרשאה לשירותי המיקום של Windows. יש לאפשר גישה למיקום עבור אפליקציות שולחן עבודה ב: הגדרות Windows ← פרטיות ואבטחה ← מיקום.",
                        "שירותי מיקום",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        this);
                    return;
                }

                string? matchedCityName = await DetectAndSelectNearestLocationAsync();

                if (matchedCityName is not null)
                {
                    AppMessageBoxWindow.Show(
                        $"המיקום זוהה בהצלחה - נבחרה העיר הקרובה ביותר: {matchedCityName}.",
                        "שירותי מיקום",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information,
                        this);
                }
                else
                {
                    AppMessageBoxWindow.Show(
                        "לא ניתן היה לאתר את המיקום הנוכחי. יש לוודא ששירותי המיקום של Windows מופעלים (הגדרות Windows ← פרטיות ואבטחה ← מיקום), ושהמחשב מחובר (Wi-Fi יכול לשפר דיוק), ולנסות שוב.",
                        "שירותי מיקום",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        this);
                }
            }
            catch
            {
                AppMessageBoxWindow.Show(
                    "לא ניתן היה לאתר את המיקום הנוכחי. יש לוודא ששירותי המיקום של Windows מופעלים (הגדרות Windows ← פרטיות ואבטחה ← מיקום), ושהמחשב מחובר (Wi-Fi יכול לשפר דיוק), ולנסות שוב.",
                    "שירותי מיקום",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    this);
            }
            finally
            {
                DetectLocationButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// הליבה המשותפת של איתור המיקום - "שקטה" תמיד (אף פעם לא מציגה
        /// הודעות בעצמה, לא הצלחה ולא כישלון): מיועדת הן ללחיצה הידנית על
        /// הכפתור (שמוסיפה הודעות משלה סביב הקריאה הזו, ראו DetectLocationButton_Click)
        /// והן לכל עדכון אוטומטי-ברקע עתידי שרוצה להישאר שקט לגמרי בלי
        /// שינוי נוסף. מאתרת קואורדינטות דרך שירותי המיקום של Windows,
        /// מוצאת את העיר הקרובה ביותר (מרחק קו-ישר לפי נוסחת Haversine)
        /// מתוך רשימת הערים המוגדרות מראש (LocationPresets), ובוחרת אותה
        /// ברשימה עצמה - הבחירה כבר ממלאת אוטומטית קו רוחב/אורך/גובה/אזור
        /// זמן (ראו LocationPresetComboBox_SelectionChanged), בדיוק כאילו
        /// המשתמש בחר את העיר הזו ידנית. מחזירה את שם העיר שנבחרה בהצלחה,
        /// או null אם האיתור נכשל מכל סיבה.
        /// </summary>
        private async Task<string?> DetectAndSelectNearestLocationAsync()
        {
            try
            {
                GeolocationAccessStatus accessStatus = await Geolocator.RequestAccessAsync();
                if (accessStatus != GeolocationAccessStatus.Allowed)
                {
                    return null;
                }

                var geolocator = new Geolocator { DesiredAccuracy = PositionAccuracy.High };
                Geoposition position = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(5),
                    timeout: TimeSpan.FromSeconds(15));

                double latitude = position.Coordinate.Point.Position.Latitude;
                double longitude = position.Coordinate.Point.Position.Longitude;

                int nearestIndex = 0;
                double nearestDistanceKm = double.MaxValue;
                for (int i = 0; i < LocationPresets.Length; i++)
                {
                    double distanceKm = HaversineDistanceKm(latitude, longitude, LocationPresets[i].Lat, LocationPresets[i].Lng);
                    if (distanceKm < nearestDistanceKm)
                    {
                        nearestDistanceKm = distanceKm;
                        nearestIndex = i;
                    }
                }

                LocationPresetComboBox.SelectedIndex = nearestIndex;
                return LocationPresets[nearestIndex].Name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>מרחק קו-ישר (ק"מ) בין שתי נקודות על פני כדור הארץ, לפי נוסחת Haversine - משמש לאיתור העיר הקרובה ביותר לקואורדינטות שהתקבלו משירות המיקום.</summary>
        private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;
            static double ToRadians(double deg) => deg * Math.PI / 180.0;

            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        private void UseCustomFontCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = UseCustomFontCheckBox.IsChecked == true;
            FontFamilyTextBox.IsEnabled = isChecked;
            FontSizeTextBox.IsEnabled = isChecked;
        }

        private void UseCustomColorCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            TextColorPicker.IsEnabled = UseCustomColorCheckBox.IsChecked == true;
        }

        private void UseCustomBackgroundCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            BackgroundColorPicker.IsEnabled = UseCustomBackgroundCheckBox.IsChecked == true;
        }

        private void UseWidgetBorderCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = UseWidgetBorderCheckBox.IsChecked == true;
            WidgetBorderColorPicker.IsEnabled = isChecked;
            WidgetBorderThicknessTextBox.IsEnabled = isChecked;
        }

        private void UseManualDateTimeCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ManualDateTimePanel.IsEnabled = UseManualDateTimeCheckBox.IsChecked == true;
        }

        private void SettingsPanelAppearanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            bool dark = SettingsPanelAppearanceComboBox.SelectedIndex == 1;
            ApplyPanelTheme(dark, DefaultDarkBackgroundHex);
        }

        private void AboutGitHubLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(UpdateService.RepositoryUrl) { UseShellExecute = true });
            }
            catch
            {
                // לא קריטי - אם משהו נכשל בפתיחת הדפדפן, אין צורך להציג שגיאה
            }
        }

        private void OnAvailableUpdateChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(RefreshUpdateUi);
        }

        private CancellationTokenSource? _updateDownloadCts;

        /// <summary>מציגה את מצב העדכונים הנוכחי (לפי UpdateService.AvailableUpdate) - נקראת בטעינה, ובכל פעם שהמצב משתנה (גם אם השינוי הגיע מבדיקה ברקע בזמן שהחלון כבר פתוח).</summary>
        private void RefreshUpdateUi()
        {
            UpdateInfo? available = UpdateService.AvailableUpdate;

            if (available is null)
            {
                UpdateAvailablePanel.Visibility = Visibility.Collapsed;
                UpdateStatusText.Text = UpdateService.LastCheckWasNetworkFailure
                    ? "אין חיבור לרשת"
                    : SettingsService.Current.LastUpdateCheckUtc is null
                        ? "לא נבדק עדיין בסשן הזה"
                        : "התוכנה מעודכנת לגרסה האחרונה";
            }
            else
            {
                UpdateStatusText.Text = $"גרסה {available.Version} זמינה";
                UpdateAvailableText.Text = $"קיימת גרסה חדשה ({available.Version}) - לחצו \"עדכן\" כדי להוריד ולהתקין אותה. כל ההגדרות וההתאמות האישיות שלכם (כולל ערכות קול וכו') יישמרו במלואן.";
                UpdateAvailablePanel.Visibility = Visibility.Visible;
                WhatsNewButton.Visibility = string.IsNullOrWhiteSpace(available.ReleaseNotes) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void WhatsNewButton_Click(object sender, RoutedEventArgs e)
        {
            AboutDialogHelper.ShowWhatsNew(this);
        }


        private async void CheckUpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateNowButton.IsEnabled = false;
            UpdateStatusText.Text = "בודק עדכונים...";

            await UpdateService.CheckForUpdateAsync();

            // RefreshUpdateUi כבר קובעת את הטקסט הנכון לכל מקרה (עדכון זמין /
            // מעודכן / לא נבדק / אין חיבור לרשת - ראו UpdateService.LastCheckWasNetworkFailure) -
            // אין צורך (ואסור, כדי לא לדרוס את הודעת "אין חיבור לרשת") בקביעה נוספת כאן.
            RefreshUpdateUi();

            CheckUpdateNowButton.IsEnabled = true;
        }

        private async void UpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo? available = UpdateService.AvailableUpdate;
            if (available is null)
            {
                return;
            }

            UpdateNowButton.IsEnabled = false;
            CheckForUpdatesCheckBox.IsEnabled = false;
            UpdateProgressBar.Visibility = Visibility.Visible;
            UpdateProgressText.Visibility = Visibility.Visible;
            UpdateProgressBar.Value = 0;
            UpdateProgressText.Text = "מוריד עדכון... 0%";

            var progress = new Progress<double>(percent =>
            {
                UpdateProgressBar.Value = percent;
                UpdateProgressText.Text = $"מוריד עדכון... {percent:0}%";
            });

            _updateDownloadCts = new CancellationTokenSource();

            string? downloadedPath = await UpdateService.DownloadUpdateAsync(available.DownloadUrl, progress, _updateDownloadCts.Token);

            if (downloadedPath is null)
            {
                UpdateProgressText.Text = "הורדת העדכון נכשלה. ניתן לנסות שוב מאוחר יותר.";
                UpdateNowButton.IsEnabled = true;
                CheckForUpdatesCheckBox.IsEnabled = true;
                return;
            }

            UpdateProgressText.Text = "ההורדה הושלמה - התוכנה תיסגר ותתעדכן כעת...";

            // שומרים את ההגדרות הנוכחיות (כולל שינויים לא-שמורים בחלון הזה,
            // כמו הפעלה/כיבוי של בדיקת עדכונים) לפני היציאה, ורק אז מפעילים
            // את תהליך ההחלה (שיוצא מהתוכנה לגמרי ומריץ אותה מחדש בסיום).
            await Task.Delay(800);
            UpdateService.ApplyUpdateAndRestart(downloadedPath);
        }

        /// <summary>
        /// מחליף בפועל את כל צבעי פאנל ההגדרות (רקע, טקסט, תוויות לשוניות,
        /// תיבות סימון וכו') - כל הרכיבים ב-XAML משתמשים ב-DynamicResource
        /// לצבעים האלה בדיוק כדי שהחלפה כאן תיכנס לתוקף מיידית על כל הפאנל,
        /// כולל בזמן אמת לפני לחיצה על "שמור".
        ///
        /// הצבעים במצב בהיר קבועים (מוגדרים כברירת מחדל ב-Window.Resources);
        /// במצב כהה, רק צבע הרקע עצמו הוא לפי בחירת המשתמש - שאר הצבעים
        /// (טקסט, גבולות וכו') עוברים לפלטת כהה קבועה שנבחרה כדי להבטיח
        /// ניגודיות טובה מול כל צבע רקע כהה סביר שהמשתמש עשוי לבחור.
        /// </summary>
        private void ApplyPanelTheme(bool dark, string darkBackgroundHex)
        {
            if (!dark)
            {
                SetBrush("WindowBackgroundBrush", "#F3F3F3");
                SetBrush("PrimaryForegroundBrush", "#1B1C1F");
                SetBrush("SecondaryForegroundBrush", "#5B5D63");
                SetBrush("AccentForegroundBrush", "#1A5FB4");
                SetBrush("ControlBackgroundBrush", "#FFFFFF");
                SetBrush("ControlBorderBrush", "#C6C6C9");
                SetBrush("TabItemBackgroundBrush", "#E3E3E6");
                SetBrush("TabItemForegroundBrush", "#3A3B40");
                SetBrush("TabItemSelectedBackgroundBrush", "#FFFFFF");
                SetBrush("TabItemSelectedForegroundBrush", "#1B1C1F");
                SetBrush("CheckBoxBoxBrush", "#FFFFFF");
                SetBrush("CheckBoxBorderBrush", "#8A8B90");
                SetBrush("CheckMarkBrush", "#1A5FB4");
                return;
            }

            string bg = string.IsNullOrWhiteSpace(darkBackgroundHex) ? "#1B1C1F" : darkBackgroundHex;
            string tabBg = LightenOrDarken(bg, 0.12);
            string controlBg = LightenOrDarken(bg, 0.08);

            SetBrush("WindowBackgroundBrush", bg);
            SetBrush("PrimaryForegroundBrush", "#F0F0F0");
            SetBrush("SecondaryForegroundBrush", "#B0B0B0");
            SetBrush("AccentForegroundBrush", "#9ECBFF");
            SetBrush("ControlBackgroundBrush", controlBg);
            SetBrush("ControlBorderBrush", "#4A4B50");
            SetBrush("TabItemBackgroundBrush", tabBg);
            // תוית לא-נבחרת: טקסט בהיר על רקע כהה. תווית נבחרת: כדי לשמור על
            // ניגודיות מובטחת בלי תלות בכהות הצבע הספציפי שהמשתמש בחר, הרקע
            // הנבחר תמיד בהיר-ניטרלי וטקסטו כהה - בדיוק כמו במצב הבהיר.
            SetBrush("TabItemForegroundBrush", "#E6E6E6");
            SetBrush("TabItemSelectedBackgroundBrush", "#F0F0F0");
            SetBrush("TabItemSelectedForegroundBrush", "#1B1C1F");
            SetBrush("CheckBoxBoxBrush", controlBg);
            SetBrush("CheckBoxBorderBrush", "#8A8B90");
            SetBrush("CheckMarkBrush", "#9ECBFF");
        }

        private void SetBrush(string resourceKey, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            Resources[resourceKey] = new SolidColorBrush(color);
        }

        /// <summary>מבהיר (amount חיובי) או מכהה (amount שלילי) צבע נתון, לשימוש בגוונים משניים (רקע לשוניות/פקדים) שנגזרים מצבע הרקע הכהה שהמשתמש בחר.</summary>
        private static string LightenOrDarken(string hex, double amount)
        {
            try
            {
                Color c = (Color)ColorConverter.ConvertFromString(hex);
                byte Adjust(byte channel)
                {
                    double value = channel + (255 - channel) * amount;
                    return (byte)Math.Clamp(value, 0, 255);
                }

                Color adjusted = Color.FromRgb(Adjust(c.R), Adjust(c.G), Adjust(c.B));
                return adjusted.ToString();
            }
            catch
            {
                return hex;
            }
        }

        private void ShowGregorianClockCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = ShowGregorianClockCheckBox.IsChecked == true;
            GregorianClockSidePanel.IsEnabled = isChecked;
            ShowGregorianSeparatorCheckBox.IsEnabled = isChecked;
        }


        private void ShowHolidayPanelCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = ShowHolidayPanelCheckBox.IsChecked == true;
            HolidayPanelSidePanel.IsEnabled = isChecked;
            ShowHolidaySeparatorCheckBox.IsEnabled = isChecked;
        }

        /// <summary>
        /// כפתור "יציאה" בתחתית פאנל ההגדרות - מבצע בדיוק את אותה פעולה כמו
        /// "יציאה" בתפריט ההקשר של הוידג'ט (ללא הודעת אישור נוספת, כדי
        /// שההתנהגות תהיה זהה במדויק, לא רק דומה). זמין גם כאשר החלון נפתח
        /// מכלי הגישה העצמאי (HebrewTaskbarWidgetSettings.exe) - במקרה כזה,
        /// האיתות הבין-תהליכי הוא הדרך היחידה לכבות את הוידג'ט הראשי הרץ
        /// ברקע בלי גישה ללחיצה עליו או לתפריט ההקשר שלו.
        /// </summary>
        private void ExitApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            CrossProcessSignal.BroadcastExitRequest();
            Close();
        }

        /// <summary>
        /// מפעיל/מכבה מיידית את הסתרת שעון Windows (ראו WindowsClockVisibilityService
        /// למידע על אופן הפעולה - הסתרה ישירה של חלון השעון, בלי הפעלה מחדש
        /// של Explorer ובלי הבהוב). בנוסף שומר את ערך המדיניות ב-Registry כרשת
        /// ביטחון משנית להפעלות עתידיות.
        /// </summary>
        private void HideWindowsClockCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            bool wantHidden = HideWindowsClockCheckBox.IsChecked == true;
            ReduceGapPanel.IsEnabled = wantHidden;

            WindowsClockVisibilityService.SetPolicyValue(wantHidden);
            WindowsClockVisibilityService.ApplyLiveVisibility(wantHidden);

            // אם מבטלים את ההסתרה כולה, אבל "צמצם גם את הרווח הריק" עדיין
            // מסומן - הרווח בפועל נשאר מצומצם (הוא נקבע ע"י Explorer בזמן
            // ההפעלה מחדש האחרונה שלו, ולא משתנה רק כי משנים את המדיניות
            // ב-Registry) - יש צורך בהפעלה מחדש נוספת כדי להחזיר את הרווח
            // התקין, בדיוק כמו ב-ReduceGapCheckBox_CheckedChanged למעלה.
            if (!wantHidden && ReduceGapCheckBox.IsChecked == true)
            {
                MessageBoxResult confirm = AppMessageBoxWindow.Show(
                    "'צמצם גם את הרווח הריק' עדיין מסומן, ולכן הרווח נשאר מצומצם גם אחרי ביטול ההסתרה - כדי להחזיר גם אותו לקדמותו יש להפעיל מחדש את Explorer (שולחן העבודה ושורת המשימות ייעלמו וייטענו מחדש לרגע). להפעיל מחדש עכשיו?",
                    "אישור הפעלה מחדש של Explorer",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    this);

                if (confirm == MessageBoxResult.Yes)
                {
                    WindowsClockVisibilityService.ApplyFullEffectWithRestart(false);
                }
            }
        }

        /// <summary>
        /// מצב "מלא" (מצמצם גם את הרווח הריק) - מצריך הפעלה מחדש חד-פעמית של
        /// Explorer, אך זו כבר לא מתבצעת מיד עם הסימון: בסימון התיבה מוצגת
        /// הודעה (עם כפתור אישור בלבד - זו הודעת מידע, לא שאלה) שמסבירה
        /// שהפעולה תצריך הפעלה מחדש של Explorer, וההפעלה מחדש בפועל תתבצע
        /// רק בעת שמירת ההגדרות (ראו SaveButton_Click), ורק אם התיבה אכן
        /// נשארה מסומנת עד אז. ביטול הסימון לא מציג הודעה ולא מתזמן הפעלה
        /// מחדש - אין צורך בה כדי רק להפסיק לבקש את המצב המצומצם.
        /// </summary>
        private void ReduceGapCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            bool wantReduceGap = ReduceGapCheckBox.IsChecked == true;

            if (!wantReduceGap)
            {
                _reduceGapRestartPendingOnSave = false;
                return;
            }

            _reduceGapRestartPendingOnSave = true;

            AppMessageBoxWindow.Show(
                "פעולה זו מצריכה הפעלה מחדש של Explorer (שולחן העבודה ושורת המשימות ייעלמו וייטענו מחדש לרגע). " +
                "זו הדרך היחידה הידועה לצמצם גם את הרווח הריק, אך התוצאה מובטחת ב-Windows 10 בלבד - ב-Windows 11 היא תלויה ב-build הספציפי. " +
                "ההפעלה מחדש בפועל תתבצע בעת שמירת ההגדרות, ורק אם תיבה זו תישאר מסומנת.",
                "נדרשת הפעלה מחדש של Explorer",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                this);
        }

        private void ExplorerAutoLaunchModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // אין צורך בפעולה מיידית כלשהי - הערך נקרא בפועל רק בשמירה
            // (SaveButton_Click); הבחירה עצמה לא משפיעה על שום דבר נראה
            // לעין בפאנל ההגדרות עצמו.
        }

        /// <summary>
        /// כפתור "הפעל" - פעולה מיידית וחד-פעמית: מפעילה מחדש את Explorer
        /// בפועל ברגע זה, בלי תלות ב"זיהוי חכם" (למקרה שהמשתמש בכל זאת
        /// רוצה לוודא שהמצב הנוכחי חל, גם אם התוכנה חושבת שזה לא נחוץ).
        /// </summary>
        private void LaunchExplorerNowButton_Click(object sender, RoutedEventArgs e)
        {
            WindowsClockVisibilityService.ApplyFullEffectWithRestart(HideWindowsClockCheckBox.IsChecked == true);
        }

        private void NotificationsEnabledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            NotificationsDetailsPanel.IsEnabled = NotificationsEnabledCheckBox.IsChecked == true;
        }

        /// <summary>
        /// "הודעה צפה" הוא המתג שקובע אם החלק המתקפל "הגדרות הודעה צפה"
        /// (מיקום/רקע/משך תצוגה/נודניק) מוצג בכלל - כל ההגדרות שם רלוונטיות
        /// רק אם יש בכלל הודעה צפה. אם הכותרת נסגרת בזמן שהיא פתוחה
        /// (המשתמש ביטל את "הודעה צפה" כשההגדרות היו גלויות) - מקפלים אותה
        /// אוטומטית גם כן, כדי לא להשאיר תוכן פתוח ללא כותרת נגישה מעליו.
        /// </summary>
        private void NotificationShowPopupCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool showPopup = NotificationShowPopupCheckBox.IsChecked == true;
            ToastSettingsAccordionHeader.Visibility = showPopup ? Visibility.Visible : Visibility.Collapsed;

            if (!showPopup)
            {
                ToastSettingsAccordionHeader.IsChecked = false;
            }
        }

        private void NotificationPlaySoundCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            NotificationSoundDetailsPanel.IsEnabled = NotificationPlaySoundCheckBox.IsChecked == true;
        }

        /// <summary>מציג/מסתיר את שדות X/Y (מיקום מותאם אישית) בשורת "היכן תצוף ההודעה", ואת טקסט ההסבר מתחתיה - בדיוק כמו התנהגות מקבילה בלשונית "שולחן עבודה".</summary>
        private void NotificationToastPositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isCustom = NotificationToastPositionComboBox.SelectedIndex == (int)ToastPositionMode.Custom;
            NotificationToastCustomPositionPanel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            NotificationToastCustomPositionExplanationText.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NotificationSoundSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            UpdateNotificationSoundSourcePanels();
        }

        /// <summary>מציגה רק את פאנל ההגדרות הרלוונטי לסוג ההתראה הקולית שנבחר כרגע בתפריט, ומסתירה את השאר לגמרי (לא רק disable) - לפי הסדר הקבוע: הקראה קולית / צליל קבוע / קובץ מהמחשב.</summary>
        private void UpdateNotificationSoundSourcePanels()
        {
            NotificationSoundSourceMode selected = GetSelectedNotificationSoundSource();

            NotificationVoiceKitComboBox.Visibility = selected == NotificationSoundSourceMode.Voice ? Visibility.Visible : Visibility.Collapsed;
            NotificationFixedSoundComboBox.Visibility = selected == NotificationSoundSourceMode.Fixed ? Visibility.Visible : Visibility.Collapsed;
            NotificationCustomSoundDetailsPanel.Visibility = selected == NotificationSoundSourceMode.CustomFile ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>בוחרת בתפריט את ערכת הקול השמורה בהגדרות (לפי שם התיקייה) - נקרא בטעינת ההגדרות, אחרי שהתפריט כבר מולא.</summary>
        private static void SelectVoiceKitInComboBox(ComboBox comboBox, string? savedKitFolderName)
        {
            if (string.IsNullOrEmpty(savedKitFolderName))
            {
                return;
            }

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item && (string?)item.Tag == savedKitFolderName)
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        /// <summary>ממלאת/מרעננת את רשימת ערכות הקול הזמינות (שמות תיקיות המשנה בתוך VoiceAnnouncements) - נסרקת בכל פתיחה של הרשימה, כך שתיקייה חדשה שנוספה בזמן שהחלון פתוח מזוהה מיד.</summary>
        private static void RefreshVoiceKitComboBox(ComboBox comboBox)
        {
            string? previouslySelected = (comboBox.SelectedItem as ComboBoxItem)?.Tag as string;

            comboBox.Items.Clear();

            IReadOnlyList<string> kits = VoiceAnnouncementService.GetAvailableKitFolders();
            foreach (string kitFolder in kits)
            {
                comboBox.Items.Add(new ComboBoxItem
                {
                    Content = FormatVoiceKitDisplayName(kitFolder),
                    Tag = kitFolder,
                });
            }

            if (comboBox.Items.Count == 0)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = "(לא נמצאו תיקיות קול)", Tag = null, IsEnabled = false });
                comboBox.SelectedIndex = 0;
                return;
            }

            int matchIndex = -1;
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item && (string?)item.Tag == previouslySelected)
                {
                    matchIndex = i;
                    break;
                }
            }

            comboBox.SelectedIndex = matchIndex >= 0 ? matchIndex : 0;
        }

        /// <summary>שם תיקייה כמו 'קול-א' מוצג בתפריט כ'קול א' (מקף הופך לרווח).</summary>
        private static string FormatVoiceKitDisplayName(string folderName)
        {
            return folderName.Replace('-', ' ');
        }

        private void NotificationVoiceKitComboBox_DropDownOpened(object sender, EventArgs e)
        {
            RefreshVoiceKitComboBox(NotificationVoiceKitComboBox);
        }

        private void AdvancedRuleVoiceKitComboBox_DropDownOpened(object sender, EventArgs e)
        {
            RefreshVoiceKitComboBox(AdvancedRuleVoiceKitComboBox);
        }

        private void NotificationBrowseSoundButton_Click(object sender, RoutedEventArgs e)
        {
            string? path = BrowseForSoundFile();
            if (path is not null)
            {
                NotificationCustomSoundPathTextBox.Text = path;
                NotificationSoundSourceComboBox.SelectedIndex = 2;
            }
        }

        /// <summary>בונה מחדש את רשימת ההתראות המתקדמות שנוספו (זמן, דקות לפני, ערוצים) - כל שורה עם סמלי כיבוי/הפעלה, עריכה ומחיקה.</summary>
        private void RefreshAdvancedRulesList()
        {
            AdvancedNotificationRulesPanel.Children.Clear();

            if (_workingAdvancedRules.Count == 0)
            {
                AdvancedNotificationRulesPanel.Children.Add(new TextBlock
                {
                    Style = (Style)FindResource("DescriptionText"),
                    Text = "לא נוספו התראות מתקדמות עדיין.",
                });
                return;
            }

            // מיפוי מפתח -> שם תצוגה (משקף התאמות אישיות/כפילות עדכניות) -
            // כדי שהשורה תציג את השם הנוכחי שהמשתמש רואה, לא את המפתח
            // הקנוני/ה-GUID הפנימי (ראו ZmanEntry.Key מול DisplayName).
            Dictionary<string, string> displayNameByKey = ComputeZmanEntriesForSettingsUi().ToDictionary(z => z.Key, z => z.DisplayName);

            foreach (AdvancedNotificationRule rule in _workingAdvancedRules)
            {
                var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var channelParts = new List<string>();
                if (rule.ShowPopup)
                {
                    channelParts.Add("הודעה");
                }

                if (rule.PlaySound)
                {
                    channelParts.Add("צליל");
                }

                string channels = channelParts.Count > 0 ? string.Join(" + ", channelParts) : "ללא תצוגה";

                string ruleDisplayName = displayNameByKey.TryGetValue(rule.ZmanName, out string? resolvedName) ? resolvedName : rule.ZmanName;

                var text = new TextBlock
                {
                    Text = $"{ruleDisplayName} · {FormatMinutesAsHhMm(rule.MinutesBefore)} לפני · {channels}" + (rule.Enabled ? string.Empty : " (כבוי)"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = rule.Enabled ? 1.0 : 0.55,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                Grid.SetColumn(text, 0);

                var toggleButton = new Button
                {
                    Content = rule.Enabled ? "\u23FB" : "\u25CB",
                    Style = (Style)FindResource("IconActionButtonStyle"),
                    ToolTip = rule.Enabled ? "כיבוי" : "הפעלה",
                    Tag = rule.Id,
                };
                toggleButton.Click += AdvancedRuleToggleButton_Click;
                Grid.SetColumn(toggleButton, 1);

                var editButton = new Button { Content = "\u270F", Style = (Style)FindResource("IconActionButtonStyle"), ToolTip = "עריכה", Tag = rule.Id };
                editButton.Click += AdvancedRuleEditButton_Click;
                Grid.SetColumn(editButton, 2);

                var deleteButton = new Button { Content = "\uD83D\uDDD1", Style = (Style)FindResource("IconActionButtonStyle"), ToolTip = "מחיקה", Tag = rule.Id };
                deleteButton.Click += AdvancedRuleDeleteButton_Click;
                Grid.SetColumn(deleteButton, 3);

                row.Children.Add(text);
                row.Children.Add(toggleButton);
                row.Children.Add(editButton);
                row.Children.Add(deleteButton);

                AdvancedNotificationRulesPanel.Children.Add(row);
            }
        }

        private void AdvancedRuleToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string id })
            {
                return;
            }

            AdvancedNotificationRule? rule = _workingAdvancedRules.FirstOrDefault(r => r.Id == id);
            if (rule is null)
            {
                return;
            }

            rule.Enabled = !rule.Enabled;
            RefreshAdvancedRulesList();
        }

        private void AdvancedRuleEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string id })
            {
                return;
            }

            AdvancedNotificationRule? rule = _workingAdvancedRules.FirstOrDefault(r => r.Id == id);
            if (rule is not null)
            {
                OpenAdvancedRuleEditor(rule);
            }
        }

        private void AdvancedRuleDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string id })
            {
                return;
            }

            _workingAdvancedRules.RemoveAll(r => r.Id == id);

            if (_editingAdvancedRuleId == id)
            {
                AdvancedRuleEditorBorder.Visibility = Visibility.Collapsed;
                _editingAdvancedRuleId = null;
            }

            RefreshAdvancedRulesList();
        }

        private void AddAdvancedNotificationButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAdvancedRuleEditor(null);
        }

        /// <summary>פותח את עורך ההתראה המתקדמת המשותף (להוספה כאשר existing==null, לעריכה כאשר יש קיים) וממלא אותו לפי הכלל הנתון.</summary>
        private void OpenAdvancedRuleEditor(AdvancedNotificationRule? existing)
        {
            _editingAdvancedRuleId = existing?.Id;
            AdvancedRuleEditorTitle.Text = existing is null ? "התראה חדשה" : "עריכת התראה";

            AdvancedRuleZmanComboBox.Items.Clear();
            var advancedRuleZmanEntries = ComputeZmanEntriesForSettingsUi();
            foreach (Services.ZmanEntry entry in advancedRuleZmanEntries)
            {
                AdvancedRuleZmanComboBox.Items.Add(new ComboBoxItem { Content = entry.DisplayName, Tag = entry.Key });
            }

            int zmanIndex = -1;
            if (existing is not null)
            {
                for (int i = 0; i < advancedRuleZmanEntries.Count; i++)
                {
                    if (advancedRuleZmanEntries[i].Key == existing.ZmanName)
                    {
                        zmanIndex = i;
                        break;
                    }
                }
            }

            AdvancedRuleZmanComboBox.SelectedIndex = Math.Max(0, zmanIndex);

            AdvancedRuleMinutesTextBox.Text = FormatMinutesAsHhMm(existing?.MinutesBefore ?? 10);

            AdvancedRuleShowPopupCheckBox.IsChecked = existing?.ShowPopup ?? true;
            AdvancedRuleToastDurationTextBox.Text = (existing?.ToastDurationSeconds ?? 15.0).ToString(CultureInfo.InvariantCulture);
            AdvancedRuleToastBackgroundComboBox.SelectedIndex = (existing?.ToastDarkBackground ?? true) ? 0 : 1;
            AdvancedRuleToastDurationPanel.IsEnabled = AdvancedRuleShowPopupCheckBox.IsChecked == true;

            AdvancedRulePlaySoundCheckBox.IsChecked = existing?.PlaySound ?? false;
            AdvancedRuleSoundDetailsPanel.IsEnabled = AdvancedRulePlaySoundCheckBox.IsChecked == true;

            AdvancedRuleSoundSourceComboBox.SelectedIndex = (existing?.SoundSource ?? NotificationSoundSourceMode.Fixed) switch
            {
                NotificationSoundSourceMode.Voice => 0,
                NotificationSoundSourceMode.CustomFile => 2,
                _ => 1,
            };

            AdvancedRuleCustomSoundPathTextBox.Text = existing?.SoundPath ?? string.Empty;

            int fixedIndex = existing is null ? -1 : Array.IndexOf(FixedSoundKeys, existing.FixedSoundName);
            AdvancedRuleFixedSoundComboBox.SelectedIndex = fixedIndex >= 0 ? fixedIndex : 0;

            RefreshVoiceKitComboBox(AdvancedRuleVoiceKitComboBox);
            SelectVoiceKitInComboBox(AdvancedRuleVoiceKitComboBox, existing?.VoiceKitFolderName);

            UpdateAdvancedRuleSoundSourcePanels();

            AdvancedRuleTestToggle.IsChecked = false;
            AdvancedRuleEditorBorder.Visibility = Visibility.Visible;
            ScrollElementIntoViewWhenReady(AdvancedRuleEditorBorder);
        }

        private void AdvancedRuleShowPopupCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            AdvancedRuleToastDurationPanel.IsEnabled = AdvancedRuleShowPopupCheckBox.IsChecked == true;
        }

        private void AdvancedRulePlaySoundCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            AdvancedRuleSoundDetailsPanel.IsEnabled = AdvancedRulePlaySoundCheckBox.IsChecked == true;
        }

        private void AdvancedRuleSoundSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAdvancedRuleSoundSourcePanels();
        }

        /// <summary>מקריא את סוג ההתראה הקולית הנבחר בעורך ההתראה המתקדם.</summary>
        private NotificationSoundSourceMode GetSelectedAdvancedRuleSoundSource()
        {
            return AdvancedRuleSoundSourceComboBox.SelectedIndex switch
            {
                0 => NotificationSoundSourceMode.Voice,
                2 => NotificationSoundSourceMode.CustomFile,
                _ => NotificationSoundSourceMode.Fixed,
            };
        }

        /// <summary>מציגה רק את פאנל ההגדרות הרלוונטי לסוג ההתראה הקולית שנבחר כרגע בעורך ההתראה המתקדם, ומסתירה את השאר לגמרי.</summary>
        private void UpdateAdvancedRuleSoundSourcePanels()
        {
            NotificationSoundSourceMode selected = GetSelectedAdvancedRuleSoundSource();

            AdvancedRuleVoiceKitComboBox.Visibility = selected == NotificationSoundSourceMode.Voice ? Visibility.Visible : Visibility.Collapsed;
            AdvancedRuleFixedSoundComboBox.Visibility = selected == NotificationSoundSourceMode.Fixed ? Visibility.Visible : Visibility.Collapsed;
            AdvancedRuleCustomSoundDetailsPanel.Visibility = selected == NotificationSoundSourceMode.CustomFile ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AdvancedRuleBrowseSoundButton_Click(object sender, RoutedEventArgs e)
        {
            string? path = BrowseForSoundFile();
            if (path is not null)
            {
                AdvancedRuleCustomSoundPathTextBox.Text = path;
                AdvancedRuleSoundSourceComboBox.SelectedIndex = 2;
            }
        }

        /// <summary>מדמה הצגת התראה בזמן אמת לפי ההגדרות הנוכחיות (עדיין לא שמורות) בעורך ההתראה המתקדמת.</summary>
        private void AdvancedRuleTestToggle_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (AdvancedRuleTestToggle.IsChecked != true)
            {
                return;
            }

            string zmanName = (AdvancedRuleZmanComboBox.SelectedItem as ComboBoxItem)?.Tag as string
                ?? Services.ZmanimCalendar.AllZmanNames[0];
            int minutesBefore = ParseHhMmToMinutes(AdvancedRuleMinutesTextBox.Text, 10);
            Services.ZmanEntry? testEntry = ResolveTestZmanEntry(zmanName);
            string testDisplayName = testEntry is not null ? Services.ZmanimCalendar.GetPopupDisplayName(testEntry) : zmanName;

            Action? replaySound = null;
            if (AdvancedRulePlaySoundCheckBox.IsChecked == true)
            {
                NotificationSoundSourceMode selectedSource = GetSelectedAdvancedRuleSoundSource();
                var testRule = new AdvancedNotificationRule
                {
                    PlaySound = true,
                    SoundSource = selectedSource,
                    SoundPath = selectedSource == NotificationSoundSourceMode.CustomFile ? AdvancedRuleCustomSoundPathTextBox.Text : null,
                    FixedSoundName = FixedSoundKeys[Math.Clamp(AdvancedRuleFixedSoundComboBox.SelectedIndex, 0, FixedSoundKeys.Length - 1)],
                    VoiceKitFolderName = (AdvancedRuleVoiceKitComboBox.SelectedItem as ComboBoxItem)?.Tag as string,
                    ZmanName = zmanName,
                    MinutesBefore = minutesBefore,
                };
                replaySound = () => NotificationSoundService.PlayForAdvancedRule(testRule, testEntry?.Time, testEntry?.VoiceKey);
            }

            if (AdvancedRuleShowPopupCheckBox.IsChecked == true)
            {
                double previewDuration = Math.Max(1, ParseDoubleOrDefault(AdvancedRuleToastDurationTextBox.Text, 15.0));
                bool previewDark = AdvancedRuleToastBackgroundComboBox.SelectedIndex != 1;
                ToastNotificationWindow.Show(testDisplayName, minutesBefore, ResolveTestTimeText(zmanName), isTest: true, previewDuration, previewDark, testEntry?.Time, replaySound);
            }

            replaySound?.Invoke();

            AdvancedRuleTestToggle.IsChecked = false;
        }

        private void AdvancedRuleEditorCancelButton_Click(object sender, RoutedEventArgs e)
        {
            AdvancedRuleEditorBorder.Visibility = Visibility.Collapsed;
            _editingAdvancedRuleId = null;
        }

        private void AdvancedRuleEditorSaveButton_Click(object sender, RoutedEventArgs e)
        {
            string zmanName = (AdvancedRuleZmanComboBox.SelectedItem as ComboBoxItem)?.Tag as string
                ?? Services.ZmanimCalendar.AllZmanNames[0];

            bool previousEnabled = true;
            if (_editingAdvancedRuleId is not null)
            {
                AdvancedNotificationRule? existing = _workingAdvancedRules.FirstOrDefault(r => r.Id == _editingAdvancedRuleId);
                if (existing is not null)
                {
                    previousEnabled = existing.Enabled;
                }
            }

            NotificationSoundSourceMode selectedSource = GetSelectedAdvancedRuleSoundSource();

            var rule = new AdvancedNotificationRule
            {
                Id = _editingAdvancedRuleId ?? Guid.NewGuid().ToString("N"),
                ZmanName = zmanName,
                MinutesBefore = ParseHhMmToMinutes(AdvancedRuleMinutesTextBox.Text, 10),
                Enabled = previousEnabled,
                ShowPopup = AdvancedRuleShowPopupCheckBox.IsChecked == true,
                ToastDurationSeconds = Math.Max(1, ParseDoubleOrDefault(AdvancedRuleToastDurationTextBox.Text, 15.0)),
                ToastDarkBackground = AdvancedRuleToastBackgroundComboBox.SelectedIndex != 1,
                PlaySound = AdvancedRulePlaySoundCheckBox.IsChecked == true,
                SoundSource = selectedSource,
                SoundPath = selectedSource == NotificationSoundSourceMode.CustomFile && !string.IsNullOrWhiteSpace(AdvancedRuleCustomSoundPathTextBox.Text)
                    ? AdvancedRuleCustomSoundPathTextBox.Text
                    : null,
                FixedSoundName = FixedSoundKeys[Math.Clamp(AdvancedRuleFixedSoundComboBox.SelectedIndex, 0, FixedSoundKeys.Length - 1)],
                VoiceKitFolderName = (AdvancedRuleVoiceKitComboBox.SelectedItem as ComboBoxItem)?.Tag as string,
            };

            int existingIndex = _editingAdvancedRuleId is null ? -1 : _workingAdvancedRules.FindIndex(r => r.Id == _editingAdvancedRuleId);
            if (existingIndex >= 0)
            {
                _workingAdvancedRules[existingIndex] = rule;
            }
            else
            {
                _workingAdvancedRules.Add(rule);
            }

            AdvancedRuleEditorBorder.Visibility = Visibility.Collapsed;
            _editingAdvancedRuleId = null;
            RefreshAdvancedRulesList();
        }

        /// <summary>
        /// גוללת אלמנט לתוך התצוגה ברגע שהוא באמת קיבל את הגודל/מיקום שלו
        /// בפריסה (DispatcherPriority.Loaded) - לא מיד עם שינוי ה-Visibility,
        /// כי במעמד הזה WPF עוד לא סיים את סבב המדידה/סידור, ו-BringIntoView
        /// עלול לפעול על גבולות ישנים/לא נכונים ולא לגלול בפועל לאן שצריך.
        /// </summary>
        private void ScrollElementIntoViewWhenReady(FrameworkElement element)
        {
            Dispatcher.BeginInvoke(new Action(() => element.BringIntoView()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // ===================== חלקים מתקפלים ("אקורדיון") =====================

        /// <summary>כל קבוצות האקורדיון הפעילות בחלון - "וידג'ט" (3 חלקים) ו"שולחן עבודה" (5 חלקים) - נשמרות כדי לאפשר קיפול-הכל בעת יציאה מהלשונית (ראו MainTabControl_SelectionChanged).</summary>
        private readonly List<List<ToggleButton>> _accordionGroups = new();

        /// <summary>
        /// מקשר קבוצת כותרות-אקורדיון (ToggleButton) לתכני ה-Border המתאימים
        /// שלהם: לחיצה על כותרת פותחת את התוכן שלה וסוגרת אוטומטית כל
        /// כותרת אחרת פתוחה באותה קבוצה (כך שרק חלק אחד פתוח בו-זמנית).
        /// </summary>
        private void ConfigureAccordionGroup(params (ToggleButton Header, Border Content)[] items)
        {
            var headers = items.Select(i => i.Header).ToList();
            _accordionGroups.Add(headers);

            foreach ((ToggleButton header, Border content) in items)
            {
                header.Checked += (_, _) =>
                {
                    content.Visibility = Visibility.Visible;
                    foreach ((ToggleButton otherHeader, Border otherContent) in items)
                    {
                        if (otherHeader != header && otherHeader.IsChecked == true)
                        {
                            otherHeader.IsChecked = false;
                        }
                    }

                    ScrollElementIntoViewWhenReady(content);
                };
                header.Unchecked += (_, _) => content.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>מגדיר את שלוש קבוצות האקורדיון הקיימות בפאנל - נקרא פעם אחת מהבנאי, אחרי InitializeComponent.</summary>
        private void ConfigureAccordionGroups()
        {
            ConfigureAccordionGroup(
                (FontColorAccordionHeader, FontColorAccordionContent),
                (BackgroundAccordionHeader, BackgroundAccordionContent),
                (BorderAccordionHeader, BorderAccordionContent));

            ConfigureAccordionGroup(
                (OverlayTimeAccordionHeader, OverlayTimeAccordionContent),
                (OverlayDayParashaAccordionHeader, OverlayDayParashaAccordionContent),
                (OverlayHebrewAccordionHeader, OverlayHebrewAccordionContent),
                (OverlayGregorianAccordionHeader, OverlayGregorianAccordionContent),
                (OverlayHolidayAccordionHeader, OverlayHolidayAccordionContent));

            ConfigureAccordionGroup(
                (ToastSettingsAccordionHeader, ToastSettingsAccordionContent));
        }

        /// <summary>מקפל מחדש את כל חלקי האקורדיון הפתוחים (בכל הקבוצות) - נקרא בכל מעבר לשונית, כדי לשמור על מראה נקי של פאנל ההגדרות (בקשת המשתמש: "כשיוצאים מהלשונית החלק הפתוח שוב נסגר").</summary>
        private void CollapseAllAccordionSections()
        {
            foreach (List<ToggleButton> group in _accordionGroups)
            {
                foreach (ToggleButton header in group)
                {
                    if (header.IsChecked == true)
                    {
                        header.IsChecked = false;
                    }
                }
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source == MainTabControl)
            {
                CollapseAllAccordionSections();
            }
        }

        /// <summary>מטפל בכל 4 תיבות הסימון "התאמה אישית לפריט זה" בהגדרות המתקדמות - מזהה איזו מהן נלחצה לפי sender ומפעיל/מכבה את הפאנל שלה בהתאם.</summary>
        private void OverlayItemCustomStyleCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb)
            {
                return;
            }

            bool isChecked = cb.IsChecked == true;

            if (cb == OverlayTimeCustomStyleCheckBox)
            {
                OverlayTimeStylePanel.IsEnabled = isChecked;
            }
            else if (cb == OverlayGregorianCustomStyleCheckBox)
            {
                OverlayGregorianStylePanel.IsEnabled = isChecked;
            }
            else if (cb == OverlayHebrewCustomStyleCheckBox)
            {
                OverlayHebrewStylePanel.IsEnabled = isChecked;
            }
            else if (cb == OverlayDayParashaCustomStyleCheckBox)
            {
                OverlayDayParashaStylePanel.IsEnabled = isChecked;
            }
            else if (cb == OverlayHolidayCustomStyleCheckBox)
            {
                OverlayHolidayStylePanel.IsEnabled = isChecked;
            }
        }

        private void OverlayEnabledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            OverlayDetailsPanel.IsEnabled = OverlayEnabledCheckBox.IsChecked == true;
        }

        private void OverlayPositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            bool isCustom = OverlayPositionComboBox.SelectedIndex == (int)OverlayPosition.Custom;
            OverlayCustomPositionGrid.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            OverlayCustomPositionExplanationText.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        }

        private static double ParseDoubleOrDefault(string text, double fallback)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : fallback;
        }

        /// <summary>בוחר את פריט תיבת הבחירה שה-Tag שלו (מחרוזת) תואם - אם אין התאמה, נופל לאינדקס ברירת המחדל שסופק.</summary>
        private static void SelectComboItemByTag(ComboBox combo, string? tag, int fallbackIndex)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item && (string?)item.Tag == tag)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }

            combo.SelectedIndex = fallbackIndex;
        }

        private static int ParseIntOrDefault(string text, int fallback)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        }

        /// <summary>מפרש שדה מספר-דקות אופציונלי: null אם השדה ריק/לא תקין (כדי שיהיה ניתן להבחין בין "לא הוגדר" לבין "הוגדר כ-0"), אחרת הערך המספרי המנותק.</summary>
        private static int? ParseNullableIntOrNull(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : null;
        }

        /// <summary>
        /// מפרש שדה "זמן לפני" בתבנית שעות:דקות (למשל "1:15" = 75 דקות) ומחזיר
        /// את הסך הכול בדקות. אם אין נקודתיים בטקסט, מתייחס לכל הטקסט כמספר
        /// דקות בלבד (למשל "25" = 25 דקות) - נוחות/תאימות לאחור. קלט לא תקין נופל לברירת המחדל.
        /// </summary>
        private static int ParseHhMmToMinutes(string text, int fallbackMinutes)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallbackMinutes;
            }

            string trimmed = text.Trim();
            int colonIndex = trimmed.IndexOf(':');

            if (colonIndex < 0)
            {
                return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int plainMinutes)
                    ? Math.Max(0, plainMinutes)
                    : fallbackMinutes;
            }

            string hoursPart = trimmed[..colonIndex];
            string minutesPart = trimmed[(colonIndex + 1)..];

            if (!int.TryParse(hoursPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours))
            {
                hours = 0;
            }

            if (!int.TryParse(minutesPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
            {
                minutes = 0;
            }

            return Math.Max(0, (hours * 60) + minutes);
        }

        /// <summary>מציג סך-הכול דקות כטקסט בתבנית שעות:דקות (למשל 75 דקות ← "1:15").</summary>
        private static string FormatMinutesAsHhMm(int totalMinutes)
        {
            int safeMinutes = Math.Max(0, totalMinutes);
            int hours = safeMinutes / 60;
            int minutes = safeMinutes % 60;
            return $"{hours}:{minutes:00}";
        }

        private WidgetPositionMode ResolvePositionMode()
        {
            if (PositionChevronRadio.IsChecked == true)
            {
                return WidgetPositionMode.ChevronAttached;
            }

            // האופציה השנייה מסומנת: אם המיקום החופשי הקודם (שנקבע ע"י גרירה)
            // עדיין "משומר" (המשתמש לא נגע בהגדרות המרחק המותאם אישית מאז
            // הטעינה) - משמרים אותו כפי שהוא. אחרת, זה מרחק מותאם אישית רגיל.
            if (_freeDragPreserved && _working.PositionMode == WidgetPositionMode.FreeDrag && _working.FreeDragLeft.HasValue && _working.FreeDragTop.HasValue)
            {
                return WidgetPositionMode.FreeDrag;
            }

            return WidgetPositionMode.CustomEdgeOffset;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) => SaveSettings(closeAfter: true);

        /// <summary>"החל" - שומר בדיוק כמו "שמור", אך משאיר את פאנל ההגדרות פתוח (למשל כדי להמשיך לערוך הגדרות נוספות בלי להיפתח מחדש).</summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e) => SaveSettings(closeAfter: false);

        private void SaveSettings(bool closeAfter)
        {
            DateTime manualDate = ManualDatePicker.SelectedDate?.Date ?? DateTime.Today;
            TimeSpan manualTime = TimeSpan.TryParse(ManualTimeTextBox.Text, CultureInfo.InvariantCulture, out TimeSpan parsedTime) ? parsedTime : DateTime.Now.TimeOfDay;
            DateTime manualDateTimeBase = manualDate + manualTime;

            // "הכל מסומן" נשמר כ-null (במקום רשימה מפורשת של כל השמות) - כדי
            // שזמנים חדשים שיתווספו בעתיד יוצגו אוטומטית, גם בלי לעדכן הגדרות ישנות.
            List<string> checkedZmanNames = _zmanVisibilityCheckBoxes
                .Where(kvp => kvp.Value.IsChecked == true)
                .Select(kvp => kvp.Key)
                .ToList();
            List<string>? visibleZmanNames = checkedZmanNames.Count == _zmanVisibilityCheckBoxes.Count ? null : checkedZmanNames;

            var s = new AppSettings
            {
                // --- מראה פאנל ההגדרות עצמו ---
                SettingsPanelDarkMode = SettingsPanelAppearanceComboBox.SelectedIndex == 1,
                SettingsPanelDarkColorHex = DefaultDarkBackgroundHex,

                // --- הוידג'ט: מיקום ---
                ShowWidget = ShowWidgetCheckBox.IsChecked == true,
                PositionMode = ResolvePositionMode(),
                CustomOffsetSide = CustomOffsetSideComboBox.SelectedIndex == 1 ? WidgetAttachSide.Left : WidgetAttachSide.Right,
                CustomOffsetPixels = ParseDoubleOrDefault(CustomOffsetPixelsTextBox.Text, 250.0),
                LockWidgetPosition = LockWidgetPositionCheckBox.IsChecked == true,
                LockOverlayPosition = LockOverlayPositionCheckBox.IsChecked == true,
                FreeDragLeft = _working.FreeDragLeft,
                FreeDragTop = _working.FreeDragTop,

                // --- הוידג'ט: שורות (אין יותר UI לעריכה - ראו AppSettings.ShowTopLine
                //     להסבר; שומרים את הערך הקיים כפי שהוא) ---
                ShowTopLine = _working.ShowTopLine,
                ShowBottomLine = _working.ShowBottomLine,
                SwapLineOrder = _working.SwapLineOrder,

                // --- הוידג'ט: גופן וצבע ---
                UseCustomFont = UseCustomFontCheckBox.IsChecked == true,
                FontFamilyName = string.IsNullOrWhiteSpace(FontFamilyTextBox.Text) ? "Segoe UI" : FontFamilyTextBox.Text.Trim(),
                FontSize = ParseDoubleOrDefault(FontSizeTextBox.Text, 12.0),
                UseCustomTextColor = UseCustomColorCheckBox.IsChecked == true,
                CustomTextColorHex = TextColorPicker.SelectedColorHex,

                // --- הפעלה אוטומטית / הסתרת תצוגה מקורית / שעון לועזי משולב ---
                ZmanimPopupAlignment = (ZmanimPopupAlignment)ZmanimPopupAlignmentComboBox.SelectedIndex,
                StartWithWindows = StartWithWindowsCheckBox.IsChecked == true,
                CheckForUpdates = CheckForUpdatesCheckBox.IsChecked == true,
                HideWindowsClock = HideWindowsClockCheckBox.IsChecked == true,
                HideWindowsClockReduceGap = ReduceGapCheckBox.IsChecked == true,
                ExplorerAutoLaunchMode = (ExplorerAutoLaunchMode)Math.Max(0, ExplorerAutoLaunchModeComboBox.SelectedIndex),
                ShowGregorianClock = ShowGregorianClockCheckBox.IsChecked == true,
                GregorianClockSide = GregorianSideLeftRadio.IsChecked == true ? WidgetAttachSide.Left : WidgetAttachSide.Right,
                ShowGregorianSeparator = ShowGregorianSeparatorCheckBox.IsChecked == true,
                GregorianDateFormat = (GregorianDateFormatComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "dd/MM/yyyy",
                ShowHolidayPanel = ShowHolidayPanelCheckBox.IsChecked == true,
                HolidayPanelSide = HolidaySideBetweenRadio.IsChecked == true ? HolidayPanelPosition.BetweenHebrewAndGregorian
                    : HolidaySideLeftRadio.IsChecked == true ? HolidayPanelPosition.FarLeft
                    : HolidayPanelPosition.FarRight,
                ShowHolidaySeparator = ShowHolidaySeparatorCheckBox.IsChecked == true,

                // --- הוידג'ט: רקע ---
                UseCustomBackgroundColor = UseCustomBackgroundCheckBox.IsChecked == true,
                WidgetBackgroundColorHex = BackgroundColorPicker.SelectedColorHex,
                WidgetBackgroundOpacity = BackgroundColorPicker.OpacityValue,

                // --- הוידג'ט: קו מתאר ---
                UseWidgetBorder = UseWidgetBorderCheckBox.IsChecked == true,
                WidgetBorderColorHex = WidgetBorderColorPicker.SelectedColorHex,
                WidgetBorderThickness = ParseDoubleOrDefault(WidgetBorderThicknessTextBox.Text, 1.0),

                // --- מיקום וזמנים ---
                LocationName = LocationPresetComboBox.SelectedIndex >= 0 && LocationPresetComboBox.SelectedIndex < LocationPresets.Length
                    ? LocationPresets[LocationPresetComboBox.SelectedIndex].Name
                    : "מיקום מותאם אישית",
                Latitude = ParseDoubleOrDefault(LatitudeTextBox.Text, 31.7683),
                Longitude = ParseDoubleOrDefault(LongitudeTextBox.Text, 35.2137),
                ElevationMeters = ParseDoubleOrDefault(ElevationTextBox.Text, 0),
                TimeZoneId = string.IsNullOrWhiteSpace(TimeZoneTextBox.Text) ? "Israel Standard Time" : TimeZoneTextBox.Text.Trim(),
                HebrewDayChangeMode = HebrewDayChangeTzeitRadio.IsChecked == true
                    ? HebrewDayChangeMode.AtTzeitHakochavim
                    : HebrewDayChangeSunsetRadio.IsChecked == true
                        ? HebrewDayChangeMode.AtSunset
                        : HebrewDayChangeMode.Midnight,
                CandleLightingMinutesBeforeSunset = _candleLightingMinutesBeforeSunset,
                TzeitHakochavimMinutesAfterSunset = _tzeitMinutesAfterSunsetOverride,
                ZmanCustomizations = _zmanCustomizations.Values.Select(c => new ZmanCustomization
                {
                    BaseZmanName = c.BaseZmanName,
                    CustomName = c.CustomName,
                    MethodOverride = c.MethodOverride,
                }).ToList(),
                ZmanDuplicateRows = _zmanDuplicateRows.Select(d => new ZmanDuplicateRow
                {
                    Id = d.Id,
                    BaseZmanName = d.BaseZmanName,
                    CustomName = d.CustomName,
                    Method = d.Method,
                }).ToList(),
                DefaultZmanCalculationMethod = (ZmanCalculationMethod)ZmanCalculationMethodComboBox.SelectedIndex,
                VisibleZmanNames = visibleZmanNames,

                // --- תאריך ושעה ---
                UseManualDateTime = UseManualDateTimeCheckBox.IsChecked == true,
                ManualDateTimeBaseTicks = manualDateTimeBase.Ticks,
                ManualDateTimeSetAtUtcTicks = DateTime.UtcNow.Ticks,
                Use12HourFormat = TimeFormat12Radio.IsChecked == true,
                ShowSecondsInTime = ShowSecondsCheckBox.IsChecked == true,

                // --- התראות ---
                NotificationsEnabled = NotificationsEnabledCheckBox.IsChecked == true,
                DisableNotificationsOnShabbatAndChagim = DisableNotificationsOnShabbatCheckBox.IsChecked == true,
                NotificationShowPopup = NotificationShowPopupCheckBox.IsChecked == true,
                NotificationToastDurationSeconds = Math.Max(1, ParseDoubleOrDefault(NotificationToastDurationTextBox.Text, 15.0)),
                SnoozeDurationMinutes = Math.Max(1, (int)ParseDoubleOrDefault(SnoozeDurationMinutesTextBox.Text, 2)),
                NotificationToastDarkBackground = NotificationToastBackgroundComboBox.SelectedIndex != 1,
                NotificationToastPositionMode = (ToastPositionMode)Math.Clamp(NotificationToastPositionComboBox.SelectedIndex, 0, Enum.GetValues<ToastPositionMode>().Length - 1),
                NotificationToastCustomX = ParseDoubleOrDefault(NotificationToastCustomXTextBox.Text, 100),
                NotificationToastCustomY = ParseDoubleOrDefault(NotificationToastCustomYTextBox.Text, 100),
                NotificationPlaySound = NotificationPlaySoundCheckBox.IsChecked == true,
                NotificationSoundSource = GetSelectedNotificationSoundSource(),
                NotificationFixedSoundName = FixedSoundKeys[Math.Clamp(NotificationFixedSoundComboBox.SelectedIndex, 0, FixedSoundKeys.Length - 1)],
                NotificationCustomSoundPath = string.IsNullOrWhiteSpace(NotificationCustomSoundPathTextBox.Text) ? null : NotificationCustomSoundPathTextBox.Text,
                NotificationVoiceKitFolderName = (NotificationVoiceKitComboBox.SelectedItem as ComboBoxItem)?.Tag as string,
                ZmanNotificationRules = _zmanRuleRows.Select(row =>
                {
                    (string? path, string? fixedName, bool useVoice) = ReadSoundComboSelection(row.SoundComboBox);
                    return new ZmanNotificationRule
                    {
                        ZmanName = row.ZmanName,
                        Enabled = row.EnabledCheckBox.IsChecked == true,
                        MinutesBefore = ParseHhMmToMinutes(row.MinutesTextBox.Text, 10),
                        SoundOverridePath = path,
                        SoundOverrideFixedName = fixedName,
                        SoundOverrideUseVoice = useVoice,
                    };
                }).ToList(),
                AdvancedNotificationRules = _workingAdvancedRules.Select(CloneAdvancedRule).ToList(),
                ZmanimPopupDarkMode = _working.ZmanimPopupDarkMode,

                // --- שולחן עבודה ---
                OverlayEnabled = OverlayEnabledCheckBox.IsChecked == true,
                OverlayShowTime = _overlayShowState.TryGetValue("Time", out bool showTime) && showTime,
                OverlayShowGregorianDate = _overlayShowState.TryGetValue("GregorianDate", out bool showGregorian) && showGregorian,
                OverlayShowHebrewDate = _overlayShowState.TryGetValue("HebrewDate", out bool showHebrew) && showHebrew,
                OverlayShowDayAndParasha = _overlayShowState.TryGetValue("DayParasha", out bool showDayParasha) && showDayParasha,
                OverlayShowHoliday = _overlayShowState.TryGetValue("Holiday", out bool showHoliday) && showHoliday,
                OverlayPositionMode = (OverlayPosition)Math.Max(0, OverlayPositionComboBox.SelectedIndex),
                OverlayCustomX = ParseDoubleOrDefault(OverlayCustomXTextBox.Text, 100),
                OverlayCustomY = ParseDoubleOrDefault(OverlayCustomYTextBox.Text, 100),
                OverlayFontFamilyName = string.IsNullOrWhiteSpace(OverlayFontFamilyTextBox.Text) ? "Segoe UI" : OverlayFontFamilyTextBox.Text.Trim(),
                OverlayFontSize = ParseDoubleOrDefault(OverlayFontSizeTextBox.Text, 26.0),
                OverlayTextColorHex = OverlayColorPicker.SelectedColorHex,
                OverlayAlwaysOnTop = OverlayAlwaysOnTopCheckBox.IsChecked == true,

                // --- שולחן עבודה: הגדרות מתקדמות (התאמה אישית לכל פריט) ---
                OverlayTimeStyle = SaveOverlayItemStyle(OverlayTimeCustomStyleCheckBox, OverlayTimeFontFamilyTextBox, OverlayTimeFontSizeTextBox, OverlayTimeColorPicker),
                OverlayGregorianDateStyle = SaveOverlayItemStyle(OverlayGregorianCustomStyleCheckBox, OverlayGregorianFontFamilyTextBox, OverlayGregorianFontSizeTextBox, OverlayGregorianColorPicker),
                OverlayHebrewDateStyle = SaveOverlayItemStyle(OverlayHebrewCustomStyleCheckBox, OverlayHebrewFontFamilyTextBox, OverlayHebrewFontSizeTextBox, OverlayHebrewColorPicker),
                OverlayDayParashaStyle = SaveOverlayItemStyle(OverlayDayParashaCustomStyleCheckBox, OverlayDayParashaFontFamilyTextBox, OverlayDayParashaFontSizeTextBox, OverlayDayParashaColorPicker),
                OverlayHolidayStyle = SaveOverlayItemStyle(OverlayHolidayCustomStyleCheckBox, OverlayHolidayFontFamilyTextBox, OverlayHolidayFontSizeTextBox, OverlayHolidayColorPicker),
                OverlayItemOrder = new List<string>(_workingOverlayOrder),
            };

            // "צמצם גם את הרווח הריק" - ראו ReduceGapCheckBox_CheckedChanged:
            // ההפעלה מחדש בפועל של Explorer נדחתה עד לרגע הזה, ומתבצעת רק אם
            // התיבה סומנה בסשן העריכה הנוכחי *וגם* נשארה מסומנת עד לשמירה
            // (וגם "הסתר את התצוגה המקורית" עדיין מסומן - בלעדיו אין טעם
            // בצמצום רווח של תצוגה שממילא לא מוסתרת).
            if (_reduceGapRestartPendingOnSave && s.HideWindowsClock && s.HideWindowsClockReduceGap)
            {
                WindowsClockVisibilityService.ApplyFullEffectWithRestart(true);
            }
            _reduceGapRestartPendingOnSave = false;

            SettingsService.Save(s);
            StartupService.SetEnabled(s.StartWithWindows);

            if (closeAfter)
            {
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = AppMessageBoxWindow.Show(
                "לשחזר את כל ההגדרות לברירת המחדל? פעולה זו תמחק את כל ההתאמות האישיות.",
                "שחזור ברירות מחדל",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                this);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // אם "צמצם גם את הרווח הריק" פעיל כרגע בפועל (לא בעותק העבודה
            // הלא-שמור, אלא בהגדרות השמורות שבאמת חלות על המערכת) - שחזור
            // לברירת המחדל יכבה את שתי ההגדרות (הסתרה + צמצום), אבל הרווח
            // עצמו יישאר מצומצם בפועל עד הפעלה מחדש נוספת של Explorer -
            // בדיוק כמו בביטול ידני של "הסתר" (ראו HideWindowsClockCheckBox_CheckedChanged).
            bool wasReducingGap = SettingsService.Current.HideWindowsClockReduceGap;

            _working = new AppSettings();
            LoadFromSettings(_working);

            if (wasReducingGap)
            {
                MessageBoxResult restoreConfirm = AppMessageBoxWindow.Show(
                    "'צמצם גם את הרווח הריק' היה מסומן - הרווח נשאר מצומצם גם אחרי שחזור ברירות המחדל, כדי להחזיר אותו לקדמותו יש להפעיל מחדש את Explorer (שולחן העבודה ושורת המשימות ייעלמו וייטענו מחדש לרגע). להפעיל מחדש עכשיו?",
                    "אישור הפעלה מחדש של Explorer",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    this);

                if (restoreConfirm == MessageBoxResult.Yes)
                {
                    WindowsClockVisibilityService.ApplyFullEffectWithRestart(false);
                }
            }
        }
    }
}
