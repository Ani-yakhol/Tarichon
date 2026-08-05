using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget.Controls
{
    /// <summary>
    /// בורר תאריך בפריסה עברית מלאה: חודשים עבריים (עם ניווט חודש עברי קודם/
    /// הבא), ותאריכים עבריים (בגימטריה) בתאי הימים - בניגוד לפקד ה-Calendar
    /// המובנה של WPF, שאינו תומך רשמית בלוח העברי (רק גרגוריאני/הג'רי/יפני
    /// וכו') ולכן לא יכול להציג חודשים/ימים עבריים בעצמו.
    ///
    /// עובד תמיד מול תאריכים גרגוריאנים בפועל (SelectedDate, שהוא מה שכל שאר
    /// האפליקציה משתמשת בו) - ה"עברי" הוא רק שכבת התצוגה/הניווט.
    /// </summary>
    public partial class HebrewCalendarPicker : UserControl
    {
        private static readonly System.Globalization.HebrewCalendar Calendar = HebrewDateFormatter.Calendar;

        private int _displayedHebrewYear;
        private int _displayedHebrewMonth;

        public DateTime? SelectedDate { get; private set; }

        /// <summary>מופעל כאשר המשתמש בוחר יום ברשת (לא כאשר רק מנווטים בין חודשים).</summary>
        public event EventHandler<DateTime>? DateSelected;

        public HebrewCalendarPicker()
        {
            InitializeComponent();
        }

        /// <summary>מציג את החודש העברי שמכיל את התאריך הגרגוריאני הנתון, ומסמן אותו כנבחר.</summary>
        public void ShowMonthContaining(DateTime gregorianDate)
        {
            SelectedDate = gregorianDate.Date;
            _displayedHebrewYear = Calendar.GetYear(gregorianDate);
            _displayedHebrewMonth = Calendar.GetMonth(gregorianDate);
            Rebuild();
        }

        private void PrevMonthButton_Click(object sender, RoutedEventArgs e)
        {
            StepMonth(-1);
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            StepMonth(1);
        }

        private void StepMonth(int delta)
        {
            int monthsInYear = Calendar.GetMonthsInYear(_displayedHebrewYear);
            int newMonth = _displayedHebrewMonth + delta;

            if (newMonth < 1)
            {
                _displayedHebrewYear -= 1;
                _displayedHebrewMonth = Calendar.GetMonthsInYear(_displayedHebrewYear);
            }
            else if (newMonth > monthsInYear)
            {
                _displayedHebrewYear += 1;
                _displayedHebrewMonth = 1;
            }
            else
            {
                _displayedHebrewMonth = newMonth;
            }

            Rebuild();
        }

        private void Rebuild()
        {
            bool isLeap = Calendar.IsLeapYear(_displayedHebrewYear);
            string monthName = HebrewDateFormatter.GetMonthName(_displayedHebrewMonth, isLeap);
            string yearGematria = HebrewGematria.FormatYear(_displayedHebrewYear);
            MonthYearText.Text = $"{monthName} {yearGematria}";

            int daysInMonth = Calendar.GetDaysInMonth(_displayedHebrewYear, _displayedHebrewMonth);
            DateTime firstOfMonthGregorian = Calendar.ToDateTime(_displayedHebrewYear, _displayedHebrewMonth, 1, 0, 0, 0, 0);
            int startColumn = (int)firstOfMonthGregorian.DayOfWeek; // 0=ראשון ... 6=שבת, תואם לעמודות הרשת (ראשון בעמודה 0)

            DaysGrid.Children.Clear();
            DaysGrid.RowDefinitions.Clear();

            int neededRows = (int)Math.Ceiling((startColumn + daysInMonth) / 7.0);
            for (int r = 0; r < neededRows; r++)
            {
                DaysGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            DateTime today = AppTimeService.Today();

            for (int day = 1; day <= daysInMonth; day++)
            {
                int cellIndex = startColumn + day - 1;
                int row = cellIndex / 7;
                int col = cellIndex % 7;

                DateTime cellGregorianDate = Calendar.ToDateTime(_displayedHebrewYear, _displayedHebrewMonth, day, 0, 0, 0, 0);

                var button = new Button
                {
                    Content = HebrewGematria.FormatDay(day),
                    Style = (Style)FindResource("DayCellButtonStyle"),
                    Tag = SelectedDate.HasValue && cellGregorianDate.Date == SelectedDate.Value.Date
                        ? "Selected"
                        : (cellGregorianDate.Date == today ? "Today" : null),
                };

                button.Click += (_, _) =>
                {
                    SelectedDate = cellGregorianDate.Date;
                    Rebuild();
                    DateSelected?.Invoke(this, cellGregorianDate.Date);
                };

                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);
                DaysGrid.Children.Add(button);
            }
        }
    }
}
