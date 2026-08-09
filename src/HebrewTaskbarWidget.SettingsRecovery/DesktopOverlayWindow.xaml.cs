using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HebrewTaskbarWidget.Interop;
using HebrewTaskbarWidget.Models;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// תצוגה חופשית ("Overlay") של פרטי היום מעל שולחן העבודה - חלק מפאנל
    /// ההגדרות (חלק 3): "אפשרות להצגת פרטי היום כולל התאריך והשעה מעל מסך
    /// העבודה, במרכז או במיקום אחר, עם הגדרת צבע וגופן, ואפשרות 'עליון תמיד'".
    ///
    /// כמו הוידג'ט בשורת המשימות (MainWindow): "נראית" שקופה (Background
    /// כמעט-שקוף לגמרי, #01000000 - לא Transparent אמיתי/אלפא-0, כדי
    /// שהחלון כולו יישאר קליט-ללחיצות בכל שטחו, לא רק במקום שיש בו טקסט
    /// גלוי בפועל) אך **תמיד קולטת לחיצות כרגיל** - בלי שום מנגנון "שקיפות-
    /// ללחיצות" (WS_EX_TRANSPARENT) ברמת Win32. שלושה ניסיונות קודמים
    /// (Ctrl-בלבד, Hook גלובלי עם "בליעה", כיבוי-שקיפות-רגעי מתוך Hook)
    /// ניסו לשמר שקיפות-ללחיצות אמיתית עבור לחיצה שמאלית - אך כל אחד מהם
    /// יצר סוג אחר של תקלה בפתיחת תפריט ההקשר בלחיצה ימנית. הפתרון שהתברר
    /// הכי אמין: לוותר על השקיפות-ללחיצות לגמרי ולהתנהג בדיוק כמו
    /// MainWindow - שם זה כבר עובד מצוין, כולל תפריט הקשר תקין לגמרי,
    /// גם כשהוא ממוקם מעל שולחן העבודה. המשמעות: לחיצה שמאלית "רגילה"
    /// (בלי Ctrl) על שטח התצוגה נבלמת (לא עוברת דרך לאייקונים מתחת) אך לא
    /// עושה דבר בעצמה - רק Ctrl+גרירה שמאלית מזיזה את התצוגה, בדיוק כמו
    /// קודם.
    /// </summary>
    public partial class DesktopOverlayWindow : Window
    {
        private static readonly TimeSpan ContentInterval = TimeSpan.FromSeconds(1);
        private const int DragThresholdPhysicalPixels = 4;

        private readonly DispatcherTimer _contentTimer;

        private IntPtr _hwnd;

        private bool _isCtrlDragging;
        private bool _dragMoved;
        private NativeMethods.POINT _dragStartScreenPhysical;
        private int _dragStartWindowLeftPhysical;
        private int _dragStartWindowTopPhysical;

        /// <summary>נקרא כשנבחר "הגדרות..." בתפריט ההקשר של התצוגה - פותח את פאנל ההגדרות ישירות ללשונית "שולחן עבודה".</summary>
        private readonly Action _openSettingsToDesktopTab;

        public DesktopOverlayWindow(Action openSettingsToDesktopTab)
        {
            InitializeComponent();

            _openSettingsToDesktopTab = openSettingsToDesktopTab;

            _contentTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = ContentInterval };
            _contentTimer.Tick += (_, _) => RefreshContent();

            SourceInitialized += DesktopOverlayWindow_SourceInitialized;
            Loaded += (_, _) =>
            {
                RefreshContent();
                ApplyPosition();
                _contentTimer.Start();
            };

            SettingsService.SettingsChanged += (_, _) => Dispatcher.Invoke(() =>
            {
                ApplyTopmost();
                RefreshContent();
                ApplyPosition();
            });
        }

        private void DesktopOverlayWindow_SourceInitialized(object? sender, EventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;

            // WS_EX_TOOLWINDOW+WS_EX_NOACTIVATE: לא מופיע ב-Alt+Tab ולא
            // "גונב" מיקוד מחלונות אחרים בשולחן העבודה - אך עדיין קולטת
            // לחיצות עכבר כרגיל (בניגוד לגרסה הקודמת, אין כאן WS_EX_TRANSPARENT
            // בכלל יותר - ראו הערה מפורטת במחלקה למעלה).
            int exStyle = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_LAYERED;
            exStyle &= ~NativeMethods.WS_EX_APPWINDOW;
            NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE, exStyle);

            ApplyTopmost();

            HwndSource? source = HwndSource.FromHwnd(_hwnd);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
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

                case NativeMethods.WM_RBUTTONUP:
                    ShowContextMenu();
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        /// <summary>בונה ומציגה את תפריט ההקשר של התצוגה החופשית - זהה ברוחו לתפריט ההקשר של הוידג'ט בשורת המשימות (הגדרות/אודות), עם "כיבוי" ייעודי לתצוגה הזו בלבד.</summary>
        private void ShowContextMenu()
        {
            var menu = new ContextMenu { FlowDirection = FlowDirection.RightToLeft };

            var settingsItem = new MenuItem { Header = "הגדרות..." };
            settingsItem.Click += (_, _) => _openSettingsToDesktopTab();
            menu.Items.Add(settingsItem);

            var disableItem = new MenuItem { Header = "כיבוי" };
            disableItem.Click += (_, _) =>
            {
                AppSettings settings = SettingsService.Current;
                settings.OverlayEnabled = false;
                SettingsService.Save(settings);
            };
            menu.Items.Add(disableItem);

            menu.Items.Add(new Separator());

            var aboutItem = new MenuItem { Header = "אודות" };
            aboutItem.Click += (_, _) => AboutDialogHelper.Show(this);
            menu.Items.Add(aboutItem);

            // Placement=MousePoint: פותח את התפריט בדיוק במיקום הסמן הנוכחי,
            // ללא צורך בשום חישוב קואורדינטות ידני.
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;

            // ראו הערה מפורטת ב-Services.GlobalClickWatcher: תפריט הקשר על
            // חלון NOACTIVATE כמו זה לא תמיד נסגר באופן עקבי בלחיצה במקום
            // אחר על המסך דרך מנגנון ה-Popup-dismiss הרגיל של WPF - רשת
            // ביטחון נוספת ברמת Win32 גולמית.
            Services.GlobalClickWatcher? menuClickWatcher = null;
            menu.Opened += (_, _) =>
            {
                menu.Dispatcher.BeginInvoke(new Action(() =>
                {
                    IntPtr menuHandle = PresentationSource.FromVisual(menu) is HwndSource hwndSource ? hwndSource.Handle : IntPtr.Zero;
                    menuClickWatcher = new Services.GlobalClickWatcher(
                        onClickOutside: () => menu.Dispatcher.BeginInvoke(new Action(() => menu.IsOpen = false)),
                        getProtectedRootHandles: () => new[] { menuHandle });
                }), DispatcherPriority.Loaded);
            };
            menu.Closed += (_, _) =>
            {
                menuClickWatcher?.Dispose();
                menuClickWatcher = null;
            };

            menu.IsOpen = true;
        }

        private void OnRawLeftButtonDown(IntPtr hwnd, IntPtr lParam)
        {
            if ((NativeMethods.GetKeyState(NativeMethods.VK_CONTROL) & 0x8000) == 0)
            {
                return;
            }

            if (SettingsService.Current.LockOverlayPosition)
            {
                return;
            }

            _isCtrlDragging = true;
            _dragMoved = false;

            long raw = lParam.ToInt64();
            int x = unchecked((short)(raw & 0xFFFF));
            int y = unchecked((short)((raw >> 16) & 0xFFFF));
            var point = new NativeMethods.POINT { X = x, Y = y };
            NativeMethods.ClientToScreen(hwnd, ref point);
            _dragStartScreenPhysical = point;

            if (NativeMethods.GetWindowRect(hwnd, out RECT currentRect))
            {
                _dragStartWindowLeftPhysical = currentRect.Left;
                _dragStartWindowTopPhysical = currentRect.Top;
            }

            NativeMethods.SetCapture(hwnd);
        }

        private void OnRawMouseMove(IntPtr hwnd, IntPtr lParam)
        {
            if (!_isCtrlDragging)
            {
                return;
            }

            long raw = lParam.ToInt64();
            int x = unchecked((short)(raw & 0xFFFF));
            int y = unchecked((short)((raw >> 16) & 0xFFFF));
            var current = new NativeMethods.POINT { X = x, Y = y };
            NativeMethods.ClientToScreen(hwnd, ref current);

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
            if (!_isCtrlDragging)
            {
                return;
            }

            _isCtrlDragging = false;
            NativeMethods.ReleaseCapture();

            if (_dragMoved)
            {
                // שומרים את המיקום החדש כ"מיקום מותאם אישית" - בדיוק כמו
                // הגרירה החופשית הקיימת בוידג'ט בשורת המשימות.
                AppSettings settings = SettingsService.Current;
                settings.OverlayPositionMode = OverlayPosition.Custom;
                settings.OverlayCustomX = Left;
                settings.OverlayCustomY = Top;
                SettingsService.Save(settings);
            }

            _dragMoved = false;
        }

        private void ApplyTopmost()
        {
            Topmost = SettingsService.Current.OverlayAlwaysOnTop;
        }

        /// <summary>מיפוי מפתח פריט (כפי שמופיע ב-AppSettings.OverlayItemOrder) ל-TextBlock המתאים לו.</summary>
        private Dictionary<string, TextBlock> ItemTextBlocks => new()
        {
            ["DayParasha"] = DayParashaText,
            ["Holiday"] = HolidayText,
            ["HebrewDate"] = HebrewDateText,
            ["GregorianDate"] = GregorianDateText,
            ["Time"] = TimeText,
        };

        private void RefreshContent()
        {
            AppSettings settings = SettingsService.Current;
            DateTime now = AppTimeService.Now();

            FontFamily defaultFont = new(string.IsNullOrWhiteSpace(settings.OverlayFontFamilyName) ? "Segoe UI" : settings.OverlayFontFamilyName);
            Brush defaultBrush = TryParseColor(settings.OverlayTextColorHex, out Color color) ? new SolidColorBrush(color) : Brushes.White;

            DateTime hebrewDisplayDate = HebrewDayRolloverService.GetEffectiveHebrewDate(now, settings, SettingsService.BuildLocation());
            HebrewDateDisplay hebrewDisplay = HebrewDateFormatter.Format(hebrewDisplayDate);
            string? holidayName = HolidayService.GetHolidayName(hebrewDisplayDate);

            DayParashaText.Visibility = settings.OverlayShowDayAndParasha ? Visibility.Visible : Visibility.Collapsed;
            DayParashaText.Text = hebrewDisplay.TopLine;
            ApplyItemStyle(DayParashaText, settings.OverlayDayParashaStyle, defaultFont, defaultBrush, settings.OverlayFontSize * 0.55);

            HolidayText.Visibility = settings.OverlayShowHoliday && holidayName is not null ? Visibility.Visible : Visibility.Collapsed;
            HolidayText.Text = holidayName ?? string.Empty;
            ApplyItemStyle(HolidayText, settings.OverlayHolidayStyle, defaultFont, defaultBrush, settings.OverlayFontSize * 0.5);

            HebrewDateText.Visibility = settings.OverlayShowHebrewDate ? Visibility.Visible : Visibility.Collapsed;
            HebrewDateText.Text = hebrewDisplay.BottomLine;
            ApplyItemStyle(HebrewDateText, settings.OverlayHebrewDateStyle, defaultFont, defaultBrush, settings.OverlayFontSize * 0.7);

            GregorianDateText.Visibility = settings.OverlayShowGregorianDate ? Visibility.Visible : Visibility.Collapsed;
            // AppTimeService.GregorianDisplayCulture (לא CultureInfo.CurrentCulture/ברירת
            // המחדל) - כדי שזה יישאר תאריך לועזי אמיתי גם אם "לוח שנה" ב-
            // הגדרות אזור Windows מוגדר לעברי (ראו הערה מפורטת שם).
            GregorianDateText.Text = now.ToString("dd/MM/yyyy", AppTimeService.GregorianDisplayCulture);
            ApplyItemStyle(GregorianDateText, settings.OverlayGregorianDateStyle, defaultFont, defaultBrush, settings.OverlayFontSize * 0.5);

            TimeText.Visibility = settings.OverlayShowTime ? Visibility.Visible : Visibility.Collapsed;
            TimeText.Text = AppTimeService.FormatClockTime(now);
            TimeText.FontWeight = FontWeights.SemiBold;
            ApplyItemStyle(TimeText, settings.OverlayTimeStyle, defaultFont, defaultBrush, settings.OverlayFontSize);

            ApplyItemOrder(settings.OverlayItemOrder);

            var shadow = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 1,
                Opacity = 0.6,
                Color = Colors.Black,
            };
            LinesPanel.Effect = shadow;
        }

        /// <summary>מסדרת מחדש את סדר הילדים ב-LinesPanel לפי רשימת המפתחות הנתונה (מלמעלה למטה). מפתחות לא-מוכרים או חסרים מתעלמים בשקט.</summary>
        private void ApplyItemOrder(List<string> order)
        {
            Dictionary<string, TextBlock> map = ItemTextBlocks;

            LinesPanel.Children.Clear();

            foreach (string key in order)
            {
                if (map.TryGetValue(key, out TextBlock? block))
                {
                    LinesPanel.Children.Add(block);
                    map.Remove(key);
                }
            }

            // כל פריט שלא הוזכר ברשימה (מקרה קצה - הגדרות פגומות/ישנות) מתווסף בסוף, כדי לא "לאבד" אותו.
            foreach (TextBlock remaining in map.Values)
            {
                LinesPanel.Children.Add(remaining);
            }
        }

        private static void ApplyItemStyle(TextBlock block, OverlayItemStyle itemStyle, FontFamily defaultFont, Brush defaultBrush, double defaultSize)
        {
            if (itemStyle.UseCustomStyle)
            {
                block.FontFamily = new FontFamily(string.IsNullOrWhiteSpace(itemStyle.FontFamilyName) ? "Segoe UI" : itemStyle.FontFamilyName);
                block.FontSize = itemStyle.FontSize;
                block.Foreground = TryParseColor(itemStyle.ColorHex, out Color color) ? new SolidColorBrush(color) : defaultBrush;
            }
            else
            {
                block.FontFamily = defaultFont;
                block.FontSize = defaultSize;
                block.Foreground = defaultBrush;
            }
        }

        private void ApplyPosition()
        {
            UpdateLayout();

            AppSettings settings = SettingsService.Current;
            Rect workArea = SystemParameters.WorkArea;

            double width = ActualWidth > 0 ? ActualWidth : Width;
            double height = ActualHeight > 0 ? ActualHeight : Height;
            const double margin = 40.0;

            (double left, double top) = settings.OverlayPositionMode switch
            {
                OverlayPosition.TopLeft => (workArea.Left + margin, workArea.Top + margin),
                OverlayPosition.TopRight => (workArea.Right - width - margin, workArea.Top + margin),
                OverlayPosition.BottomLeft => (workArea.Left + margin, workArea.Bottom - height - margin),
                OverlayPosition.BottomRight => (workArea.Right - width - margin, workArea.Bottom - height - margin),
                OverlayPosition.Custom => (settings.OverlayCustomX, settings.OverlayCustomY),
                _ => (workArea.Left + (workArea.Width - width) / 2.0, workArea.Top + (workArea.Height - height) / 2.0),
            };

            // בזמן גרירה פעילה לא "מתקנים" את המיקום מחדש ביחס להגדרות - אחרת
            // הגרירה "תילחם" בעדכון האוטומטי. המיקום נשמר בפועל רק בשחרור.
            if (_isCtrlDragging)
            {
                return;
            }

            Left = left;
            Top = top;
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
    }
}
