using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HebrewTaskbarWidget.Interop;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// חלונית התראה קטנה ("Toast"), צפה מעל הוידג'ט הראשי (באותו עיצוב
    /// ומיקום כמו חלונית זמני היום) ונעלמת אוטומטית לאחר מספר שניות. משמשת
    /// להתראה על התקרבות זמן הלכתי (חלק 3 - התראות), וכן להצגת "נסוי" של
    /// התראה (בלשונית ההגדרות) לפני שהיא קורית בפועל.
    ///
    /// ההודעה בנויה תמיד משתי שורות ממורכזות: "זמן X בעוד N דקות" (שם הזמן
    /// עצמו מודגש בצבע הדגשה כחול, כמו הזמן הקרוב המודגש בלוח הזמנים) ומתחתיו
    /// "בשעה HH:MM". אין כותרת קבועה - רק בהתראת "נסוי" מוצגת תווית קטנה
    /// "(נסוי)" מעל ההודעה, כדי להבהיר שזו לא התראה אמיתית.
    /// </summary>
    public partial class ToastNotificationWindow : Window
    {
        // ברירת מחדל בטוחה אם ההגדרה חסרה/לא תקינה - בפועל תמיד נלקח הערך
        // המוגדר בהגדרות (AppSettings.NotificationToastDurationSeconds).
        private const double DefaultVisibleDurationSeconds = 15.0;

        // מילות מספר בעברית, בלשון נקבה (מתאים לדקדוק "שעות") - 3 ומעלה
        // בלבד: לשעה אחת ולשעתיים יש מילים ייעודיות (ראו BuildDurationPhrase).
        private static readonly string[] FeminineHourWords =
        {
            "שלוש", "ארבע", "חמש", "שש", "שבע", "שמונה", "תשע", "עשר",
            "אחת עשרה", "שתים עשרה", "שלוש עשרה", "ארבע עשרה", "חמש עשרה",
            "שש עשרה", "שבע עשרה", "שמונה עשרה", "תשע עשרה",
        };

        private readonly DispatcherTimerWrapper _autoCloseTimer;

        // נשמרים כדי שכפתור ה"נודניק" יוכל לשחזר/לחשב מחדש התראה מעודכנת
        // בעוד שתי דקות - גם אחרי שהחלונית הנוכחית כבר נסגרה.
        private readonly string _zmanName;
        private readonly bool _isTest;
        private readonly double? _durationSecondsOverride;
        private readonly bool? _darkBackgroundOverride;
        private readonly DateTime? _zmanTime;
        private readonly Action? _onSnoozeReplaySound;

        public ToastNotificationWindow(string zmanName, double minutesBefore, string timeText, bool isTest, double? durationSecondsOverride = null, bool? darkBackgroundOverride = null, DateTime? zmanTime = null, Action? onSnoozeReplaySound = null)
        {
            InitializeComponent();

            // "עלות השחר"/"צאת הכוכבים" מוצגים כאן בלי הסיומת הטכנית
            // ("(16.1°)"/"(8.5°)") - בדיוק כמו בפופ-אפ לוח הזמנים (ראו
            // ZmanimCalendar.GetPopupDisplayName). הסיומת ממשיכה להופיע
            // רק ברשימת הבחירה בהגדרות "מיקום וזמנים", לשם דיוק. הקריאה
            // בטוחה לביצוע גם אם השם כבר נטול-סיומת (למשל בהחזרת נודניק).
            zmanName = ZmanimCalendar.GetPopupDisplayName(zmanName);

            _zmanName = zmanName;
            _isTest = isTest;
            _durationSecondsOverride = durationSecondsOverride;
            _darkBackgroundOverride = darkBackgroundOverride;
            _zmanTime = zmanTime;
            _onSnoozeReplaySound = onSnoozeReplaySound;

            SnoozeButton.ToolTip = $"נודניק - הזכר שוב בעוד {Math.Max(1, SettingsService.Current.SnoozeDurationMinutes)} דקות";

            bool darkBackground = darkBackgroundOverride ?? SettingsService.Current.NotificationToastDarkBackground;
            ApplyBackgroundTheme(darkBackground);

            if (isTest)
            {
                TitleText.Text = "(נסוי)";
                TitleText.Visibility = Visibility.Visible;
            }

            var zmanNameBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(darkBackground ? "#9ECBFF" : "#1A5FB4"));

            MessageText.Inlines.Clear();
            MessageText.Inlines.Add(new Run("זמן "));
            MessageText.Inlines.Add(new Run(zmanName) { Foreground = zmanNameBrush, FontWeight = FontWeights.SemiBold });
            MessageText.Inlines.Add(new Run($" {BuildDurationPhrase(minutesBefore)}"));
            MessageText.Inlines.Add(new LineBreak());
            MessageText.Inlines.Add(new Run($"בשעה {timeText}"));

            Opacity = 0;
            Loaded += (_, _) =>
            {
                PositionAboveWidget();
                BeginStoryboard((Storyboard)FindResource("FadeInStoryboard"));
            };

            double durationSeconds = durationSecondsOverride ?? SettingsService.Current.NotificationToastDurationSeconds;
            if (durationSeconds <= 0)
            {
                durationSeconds = DefaultVisibleDurationSeconds;
            }

            _autoCloseTimer = new DispatcherTimerWrapper(TimeSpan.FromSeconds(durationSeconds), BeginFadeOut);
        }

        /// <summary>מחיל את ערכת הרקע (כהה - ברירת מחדל, או בהיר) שנבחרה בהגדרות על חלונית ההתראה.</summary>
        private void ApplyBackgroundTheme(bool dark)
        {
            if (dark)
            {
                ToastBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0202225"));
                ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33FFFFFF"));
                TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9A9A9A"));
                MessageText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E4E4E4"));
                CloseButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9A9A9A"));
                SnoozeButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9A9A9A"));
            }
            else
            {
                ToastBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5FAFAFA"));
                ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22000000"));
                TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A8A8A"));
                MessageText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222222"));
                CloseButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A8A8A"));
                SnoozeButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A8A8A"));
            }
        }

        /// <summary>
        /// מפיקה את ביטוי "כמה זמן נותר" בעברית: מתחת לשעה - "בעוד N דקות"
        /// כרגיל. מעל שעה - "בעוד שעה ו-N דקות" / "בעוד שעתיים ו-N דקות" /
        /// "בעוד שלוש שעות ו-N דקות" וכן הלאה (בלי "ו-N דקות" כלל אם מדובר
        /// בשעה/שעתיים/וכו' עגולות, ללא יתרת דקות).
        /// </summary>
        private static string BuildDurationPhrase(double minutesBeforeRaw)
        {
            int totalMinutes = (int)Math.Round(minutesBeforeRaw);
            if (totalMinutes <= 0)
            {
                return "עכשיו";
            }

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            if (hours <= 0)
            {
                return $"בעוד {minutes} דקות";
            }

            string hourPhrase = hours switch
            {
                1 => "שעה",
                2 => "שעתיים",
                _ when hours - 3 < FeminineHourWords.Length => $"{FeminineHourWords[hours - 3]} שעות",
                _ => $"{hours} שעות",
            };

            return minutes > 0 ? $"בעוד {hourPhrase} ו {minutes} דקות" : $"בעוד {hourPhrase}";
        }

        /// <summary>יוצר ומציג התראה חדשה. נקודת הכניסה הנוחה משירות ההתראות (וכן מבדיקות "נסוי" בפאנל ההגדרות).</summary>
        public static void Show(string zmanName, double minutesBefore, string timeText, bool isTest = false, double? durationSecondsOverride = null, bool? darkBackgroundOverride = null, DateTime? zmanTime = null, Action? onSnoozeReplaySound = null)
        {
            var toast = new ToastNotificationWindow(zmanName, minutesBefore, timeText, isTest, durationSecondsOverride, darkBackgroundOverride, zmanTime, onSnoozeReplaySound);
            toast.Show();
        }

        /// <summary>
        /// ממקם את חלונית ההתראה צפה מעל הוידג'ט הראשי - באותו אופן בדיוק
        /// כמו חלונית זמני היום (ZmanimPopup.PositionAboveWidget), כדי
        /// שהעיצוב וההתנהגות יהיו עקביים: ההתראה "צצה" ממש מעל הוידג'ט, לא
        /// בפינת המסך.
        /// </summary>
        private void PositionAboveWidget()
        {
            UpdateLayout();

            double popupWidth = ActualWidth > 0 ? ActualWidth : Width;
            double popupHeight = ActualHeight > 0 ? ActualHeight : Height;
            const double gap = 6.0;

            if (System.Windows.Application.Current?.MainWindow is Window widget && widget.IsLoaded)
            {
                Left = widget.Left + (widget.ActualWidth / 2.0) - (popupWidth / 2.0);
                Top = widget.Top - popupHeight - gap;
                return;
            }

            // גיבוי (לא אמור לקרות בפועל): אם משום מה הוידג'ט הראשי לא נגיש,
            // ממקמים בפינה התחתונה של המסך במקום ליפול עם שגיאה.
            double screenRight = SystemParameters.WorkArea.Right;
            double screenBottom = SystemParameters.WorkArea.Bottom;
            Left = screenRight - popupWidth - 16.0;
            Top = screenBottom - popupHeight - 16.0;
        }

        private void BeginFadeOut()
        {
            Dispatcher.Invoke(() => BeginStoryboard((Storyboard)FindResource("FadeOutStoryboard")));
        }

        private void FadeOutStoryboard_Completed(object? sender, EventArgs e)
        {
            Close();
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _autoCloseTimer.Stop();
            BeginFadeOut();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer.Stop();
            NotificationSoundService.StopAllPlayback();
            BeginFadeOut();
        }

        /// <summary>
        /// "נודניק": סוגר את ההתראה הנוכחית, ומתזמן הופעה חוזרת בעוד שתי
        /// דקות בדיוק - עם זמן מעודכן (אם ידוע הזמן ההלכתי המקורי, מחשבים
        /// מחדש כמה זמן נותר וגם את שעת היעד המדוייקת בפועל, לא רק שוב
        /// את אותו תוכן ישן). הטיימר של הנודניק **לא** תלוי בחיי החלונית
        /// הנוכחית (שתיסגר מיד) - הוא רץ באופן עצמאי על ה-Dispatcher
        /// הראשי של האפליקציה, כך שהתזכורת תמיד תגיע גם אחרי שהחלונית
        /// הנוכחית כבר נעלמה מהמסך.
        /// </summary>
        private void SnoozeButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer.Stop();
            NotificationSoundService.StopAllPlayback();

            string zmanName = _zmanName;
            bool isTest = _isTest;
            double? durationOverride = _durationSecondsOverride;
            bool? darkOverride = _darkBackgroundOverride;
            DateTime? targetTime = _zmanTime;
            Action? replaySound = _onSnoozeReplaySound;

            int snoozeMinutes = Math.Max(1, SettingsService.Current.SnoozeDurationMinutes);

            var snoozeTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMinutes(snoozeMinutes) };
            snoozeTimer.Tick += (_, _) =>
            {
                snoozeTimer.Stop();

                double updatedMinutesBefore;
                string updatedTimeText;

                if (targetTime is DateTime actualTime)
                {
                    updatedMinutesBefore = Math.Max(0, (actualTime - DateTime.Now).TotalMinutes);
                    updatedTimeText = AppTimeService.FormatZmanTime(actualTime);
                }
                else
                {
                    // אין זמן הלכתי ידוע (למשל התראת "נסוי") - פשוט מציגים
                    // שוב את אותה הודעה, כ"תזכורת חוזרת" גנרית.
                    updatedMinutesBefore = snoozeMinutes;
                    updatedTimeText = AppTimeService.FormatZmanTime(DateTime.Now.AddMinutes(snoozeMinutes));
                }

                Show(zmanName, updatedMinutesBefore, updatedTimeText, isTest, durationOverride, darkOverride, targetTime, replaySound);

                // משמיעים שוב את אותו ערוץ קול/הקראה קולית שהוגדר במקור
                // (אם היה בכלל) - נודניק אמור לחזור בדיוק על אותה התראה,
                // לא רק על התצוגה החזותית שלה.
                replaySound?.Invoke();
            };
            snoozeTimer.Start();

            BeginFadeOut();
        }

        protected override void OnClosed(EventArgs e)
        {
            _autoCloseTimer.Stop();
            base.OnClosed(e);
        }
    }

    /// <summary>עטיפה קטנה סביב DispatcherTimer להפעלת פעולה חד-פעמית לאחר השהיה.</summary>
    internal sealed class DispatcherTimerWrapper
    {
        private readonly System.Windows.Threading.DispatcherTimer _timer;

        public DispatcherTimerWrapper(TimeSpan delay, Action callback)
        {
            _timer = new System.Windows.Threading.DispatcherTimer { Interval = delay };
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                callback();
            };
            _timer.Start();
        }

        public void Stop() => _timer.Stop();
    }
}
