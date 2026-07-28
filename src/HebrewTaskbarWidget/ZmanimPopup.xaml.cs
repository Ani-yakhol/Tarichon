using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HebrewTaskbarWidget.Models;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// פריט תצוגה בודד ברשימת הזמנים - עוטף <see cref="ZmanEntry"/> עם מחרוזת
    /// זמן מפורמטת (או "—" אם לא ניתן היה לחשב), נוחה ל-Binding ב-XAML.
    /// </summary>
    public sealed class ZmanDisplayItem
    {
        public required string Name { get; init; }
        public required string DisplayTime { get; init; }

        /// <summary>true עבור הזמן הקרוב ביותר שעוד לא עבר (רק כאשר היום המוצג הוא היום בפועל) - מוצג בצבע הדגשה (ברירת מחדל: כחול, כמו "חזרה להיום").</summary>
        public bool IsNext { get; init; }

        public static ZmanDisplayItem From(ZmanEntry entry, bool isNext) => new()
        {
            Name = entry.Name,
            DisplayTime = entry.Time.HasValue ? AppTimeService.FormatZmanTime(entry.Time.Value) : "—",
            IsNext = isNext,
        };
    }

    /// <summary>
    /// חלונית הפופ-אפ (חלק 2 בפרוייקט): מציגה את פרטי היום הנבחר ואת רשימת זמני
    /// היום ההלכתיים, עם ניווט בין ימים וקפיצה לתאריך ספציפי (בעזרת בורר תאריך
    /// עברי מלא - ראו Controls.HebrewCalendarPicker). נפתחת בלחיצה שמאלית על
    /// הוידג'ט הראשי, ונסגרת אוטומטית כשמאבדת פוקוס (בדומה לפופ-אפים רגילים
    /// של Windows כמו תצוגת התאריך המובנית בשורת המשימות).
    /// </summary>
    public partial class ZmanimPopup : Window
    {
        // מיקום החישוב נלקח מהגדרות המשתמש (פאנל ההגדרות, חלק 3); ברירת המחדל היא ירושלים.
        private GeoLocation _location = SettingsService.BuildLocation();

        private DateTime _selectedDate = AppTimeService.Today();

        // מיקום ורוחב הוידג'ט שמעליו נפתח הפופ-אפ - נשמרים כדי שניתן יהיה
        // לחשב מחדש את המיקום האנכי בכל פעם שגובה החלונית משתנה (למשל
        // כשמתווספת/מוסרת שורת חג, או כשנפתח/נסגר בורר התאריך), ולא רק
        // בפעם הראשונה שהחלונית נפתחת.
        private double _widgetLeft;
        private double _widgetTop;
        private double _widgetWidth;
        private bool _positioned;

        private static readonly string[] DayOfWeekNames =
        {
            "יום ראשון", "יום שני", "יום שלישי", "יום רביעי", "יום חמישי", "יום שישי", "יום שבת",
        };

        public ZmanimPopup()
        {
            InitializeComponent();

            ApplyTheme(SettingsService.Current.ZmanimPopupDarkMode);
            RefreshDisplay();

            SettingsService.SettingsChanged += SettingsService_SettingsChanged;
            Closed += (_, _) => SettingsService.SettingsChanged -= SettingsService_SettingsChanged;
        }

        /// <summary>
        /// נקרא באופן סינכרוני כחלק מסבב המדידה/סידור (Arrange) של WPF עצמו -
        /// לפני שהפריים מוצג בפועל על המסך. זה קריטי: אם היינו מגיבים לשינוי
        /// הגובה דרך אירוע ה-SizeChanged הרגיל (שעלול "לפגר" פריים אחד או
        /// יותר מאחורי הרינדור בפועל, בעיקר כשכמה שינויי תוכן קורים יחד -
        /// למשל רשימת הזמנים מתחלפת **וגם** שורת החג נעלמת בו-זמנית, כמו
        /// כשעוברים מיום עם מועד ליום בלי מועד), המשתמש היה עלול לראות פריים
        /// אחד עם הגובה/מיקום הישן והתוכן החדש - "קפיצה" חזותית קצרה
        /// בתחתית החלונית. מיקום מחדש כאן, בתוך סבב הסידור עצמו, מבטיח
        /// שהמסך תמיד יציג את הגובה והמיקום המתואמים יחד, באותו פריים בדיוק.
        /// </summary>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            RepositionKeepingBottomAnchored();
        }

        private void SettingsService_SettingsChanged(object? sender, EventArgs e)
        {
            _location = SettingsService.BuildLocation();
            Dispatcher.Invoke(() =>
            {
                ApplyTheme(SettingsService.Current.ZmanimPopupDarkMode);
                RefreshDisplay();
            });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.OpenSettings();
            }
        }

        /// <summary>
        /// מחליף בין רקע כהה לרקע בהיר בלוח הזמנים - נשמר מיידית (לא ממתין
        /// לפאנל ההגדרות הכללי), כדי שההעדפה תישמר גם אם הפופ-אפ נסגר.
        /// </summary>
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            AppSettings settings = SettingsService.Current;
            bool newDarkMode = !settings.ZmanimPopupDarkMode;

            settings.ZmanimPopupDarkMode = newDarkMode;
            SettingsService.Save(settings);

            ApplyTheme(newDarkMode);
        }

        /// <summary>
        /// מחליפה את כל צבעי לוח הזמנים (כולל בורר התאריך העברי המשותף) בין
        /// ערכת נושא כהה לבהירה, ומעדכנת את סמל כפתור ההחלפה עצמו (שמש/ירח).
        /// </summary>
        private void ApplyTheme(bool dark)
        {
            if (dark)
            {
                SetBrush("PopupBackgroundBrush", "#F0202225");
                SetBrush("PopupBorderBrush", "#33FFFFFF");
                SetBrush("PopupPrimaryTextBrush", "#FFFFFF");
                SetBrush("PopupSecondaryTextBrush", "#CFCFCF");
                SetBrush("PopupTertiaryTextBrush", "#8F8F8F");
                SetBrush("PopupMutedTextBrush", "#7A7A7A");
                SetBrush("PopupSeparatorBrush", "#33FFFFFF");
                SetBrush("PopupButtonHoverBrush", "#33FFFFFF");
                SetBrush("PopupAccentBrush", "#9ECBFF");
                SetBrush("PopupAccentTextBrush", "#1B1C1F");
                SetBrush("PopupDatePickerHostBrush", "#141517");

                SetBrush("CalPrimaryTextBrush", "#FFFFFF");
                SetBrush("CalSecondaryTextBrush", "#8F8F8F");
                SetBrush("CalHoverBrush", "#33FFFFFF");
                SetBrush("CalAccentBrush", "#9ECBFF");
                SetBrush("CalAccentTextBrush", "#1B1C1F");

                ThemeToggleButton.Content = "\u2600"; // ☀ - לחיצה עוברת למצב בהיר
                ThemeToggleButton.ToolTip = "עבור למצב בהיר";
            }
            else
            {
                SetBrush("PopupBackgroundBrush", "#F5F5F5F5");
                SetBrush("PopupBorderBrush", "#33000000");
                SetBrush("PopupPrimaryTextBrush", "#1B1C1F");
                SetBrush("PopupSecondaryTextBrush", "#5B5D63");
                SetBrush("PopupTertiaryTextBrush", "#787878");
                SetBrush("PopupMutedTextBrush", "#8A8A8A");
                SetBrush("PopupSeparatorBrush", "#22000000");
                SetBrush("PopupButtonHoverBrush", "#14000000");
                SetBrush("PopupAccentBrush", "#1A5FB4");
                SetBrush("PopupAccentTextBrush", "#FFFFFF");
                SetBrush("PopupDatePickerHostBrush", "#E8E8EA");

                SetBrush("CalPrimaryTextBrush", "#1B1C1F");
                SetBrush("CalSecondaryTextBrush", "#787878");
                SetBrush("CalHoverBrush", "#14000000");
                SetBrush("CalAccentBrush", "#1A5FB4");
                SetBrush("CalAccentTextBrush", "#FFFFFF");

                ThemeToggleButton.Content = "\u263D"; // ☽ - לחיצה עוברת למצב כהה
                ThemeToggleButton.ToolTip = "עבור למצב כהה";
            }
        }

        private void SetBrush(string resourceKey, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            Resources[resourceKey] = new SolidColorBrush(color);
        }

        /// <summary>
        /// ממקמת את חלונית הפופ-אפ מעל הוידג'ט הראשי, ממורכזת אופקית ביחסו,
        /// כפי שמקובל בפופ-אפים הנפתחים משורת המשימות של Windows.
        /// </summary>
        public void PositionAboveWidget(double widgetLeft, double widgetTop, double widgetWidth)
        {
            _widgetLeft = widgetLeft;
            _widgetTop = widgetTop;
            _widgetWidth = widgetWidth;

            // ממתינים למידות בפועל של החלון (SizeToContent) לפני חישוב המרכוז הסופי
            UpdateLayout();

            double popupWidth = ActualWidth > 0 ? ActualWidth : Width;
            double popupHeight = ActualHeight > 0 ? ActualHeight : Height;

            const double gap = 6.0;

            Left = widgetLeft + (widgetWidth / 2.0) - (popupWidth / 2.0);
            Top = widgetTop - popupHeight - gap;

            _positioned = true;
        }

        /// <summary>
        /// ממקמת מחדש את הפופ-אפ בכל פעם שגובהו משתנה (למשל כשמתווספת שורת
        /// חג, או כשבורר התאריך נפתח/נסגר), כך שהקצה התחתון יישאר קבוע צמוד
        /// לוידג'ט - וההתארכות תתבצע כלפי מעלה בלבד ולא כלפי מטה (שם היא
        /// הייתה נחתכת ע"י שורת המשימות/נעלמת מתחתיה).
        /// </summary>
        private void RepositionKeepingBottomAnchored()
        {
            if (!_positioned)
            {
                return;
            }

            double popupWidth = ActualWidth > 0 ? ActualWidth : Width;
            double popupHeight = ActualHeight > 0 ? ActualHeight : Height;

            const double gap = 6.0;

            Left = _widgetLeft + (_widgetWidth / 2.0) - (popupWidth / 2.0);
            Top = _widgetTop - popupHeight - gap;
        }

        private void RefreshDisplay()
        {
            HebrewDateDisplay hebrewDisplay = HebrewDateFormatter.Format(_selectedDate);
            DateTime today = AppTimeService.Today();

            DayHeaderText.Text = DayOfWeekNames[(int)_selectedDate.DayOfWeek];

            string? parashaName = ParashaService.GetParashaName(_selectedDate);
            ParashaHeaderText.Text = parashaName is null ? string.Empty : $"פרשת {parashaName}";
            ParashaHeaderText.Visibility = parashaName is null ? Visibility.Collapsed : Visibility.Visible;

            HebrewDateHeaderText.Text = hebrewDisplay.BottomLine;
            GregorianDateHeaderText.Text = "· " + _selectedDate.ToString("dd/MM/yyyy");

            string? holidayName = HolidayService.GetHolidayName(_selectedDate);
            HolidayHeaderText.Text = holidayName ?? string.Empty;
            HolidayHeaderText.Visibility = holidayName is null ? Visibility.Collapsed : Visibility.Visible;

            IReadOnlyList<ZmanEntry> zmanim = ZmanimCalendar.Calculate(_selectedDate, _location);

            // הזמן "הקרוב" (מודגש בצבע הדגשה) - רק כאשר היום המוצג הוא היום
            // בפועל; הראשון ברשימה (שממילא בנויה בסדר כרונולוגי) שזמנו עדיין
            // לא הגיע.
            ZmanEntry? nextEntry = null;
            if (_selectedDate.Date == today)
            {
                DateTime now = AppTimeService.Now();
                nextEntry = zmanim.FirstOrDefault(z => z.Time.HasValue && z.Time.Value > now);
            }

            var displayItems = new List<ZmanDisplayItem>(zmanim.Count);
            foreach (ZmanEntry entry in zmanim)
            {
                displayItems.Add(ZmanDisplayItem.From(entry, isNext: entry == nextEntry));
            }

            ZmanimList.ItemsSource = displayItems;

            TodayButton.Visibility = _selectedDate.Date != today ? Visibility.Visible : Visibility.Collapsed;

            HebrewDatePicker.ShowMonthContaining(_selectedDate);
        }

        private void PrevDayButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedDate = _selectedDate.AddDays(-1);
            RefreshDisplay();
        }

        private void NextDayButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedDate = _selectedDate.AddDays(1);
            RefreshDisplay();
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedDate = AppTimeService.Today();
            RefreshDisplay();
        }

        private void DatePickerButton_Click(object sender, RoutedEventArgs e)
        {
            bool willShow = DatePickerHost.Visibility != Visibility.Visible;
            DatePickerHost.Visibility = willShow ? Visibility.Visible : Visibility.Collapsed;

            if (willShow)
            {
                HebrewDatePicker.ShowMonthContaining(_selectedDate);
            }
        }

        private void HebrewDatePicker_DateSelected(object? sender, DateTime pickedDate)
        {
            if (pickedDate.Date != _selectedDate.Date)
            {
                _selectedDate = pickedDate.Date;
                RefreshDisplay();
            }

            DatePickerHost.Visibility = Visibility.Collapsed;
        }

        private void ZmanimPopup_Deactivated(object? sender, EventArgs e)
        {
            // התנהגות פופ-אפ סטנדרטית: נסגר כשלוחצים במקום אחר על המסך
            Close();
        }
    }
}
