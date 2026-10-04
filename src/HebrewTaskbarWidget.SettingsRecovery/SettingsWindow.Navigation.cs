using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HebrewTaskbarWidget.Controls;
using HebrewTaskbarWidget.Models;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// חלון ההגדרות: ניווט וחיפוש, מעקב אחרי שינויים שלא נשמרו, ועמוד "לוח עברי ומועדים".
    /// </summary>
    public partial class SettingsWindow
    {
        private static readonly string[] HebrewDayNames = { "ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת" };

        /// <summary>פריט בחיפוש: כרטיס או חלק נפתח בעמוד מסוים (Target ריק = העמוד עצמו).</summary>
        private sealed record SearchEntry(FrameworkElement? Target, int PageIndex, string Title, string PageName, string SearchText);

        private readonly List<SearchEntry> _searchIndex = new();
        private bool _hasUnsavedChanges;
        private bool _skipUnsavedChangesPrompt;

        private void InitializeNavigationAndSearch()
        {
            BuildSearchIndex();

            // כל שינוי בפקד שמשתמש עורך מסמן שיש שינויים שלא נשמרו.
            AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnAnyControlChanged));
            AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(OnAnyControlChanged));
            AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) => OnAnyControlChanged(s, e)));
            AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler((s, e) => OnAnyControlChanged(s, e)));
            AddHandler(RangeBase.ValueChangedEvent, new RoutedPropertyChangedEventHandler<double>((s, e) => OnAnyControlChanged(s, e)));

            PreviewKeyDown += SettingsWindow_PreviewKeyDown;

            // פקדים מסוימים (למשל בורר התאריך) מעדכנים את עצמם אחרי הטעינה - מתחילים נקי רק כשהחלון רגוע.
            Dispatcher.BeginInvoke(() => SetUnsavedChanges(false), DispatcherPriority.ContextIdle);
        }

        // ===================== שינויים שלא נשמרו =====================

        private void OnAnyControlChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoading || !IsLoaded)
            {
                return;
            }

            // לא כל לחיצה היא שינוי הגדרה: פתיחת חלק נפתח, ניווט, חיפוש, גלילה וכפתורי "נסוי".
            object source = e.OriginalSource;
            if ((source is ToggleButton toggle && toggle is not CheckBox && toggle is not RadioButton)
                || source is ScrollBar
                || source == NavigationList || source == MainTabControl
                || source == SettingsSearchBox || source == SearchResultsList)
            {
                return;
            }

            // פקדים מאתחלים את עצמם כשעמוד מוצג לראשונה; שינוי אמיתי מגיע מפקד שהמשתמש עובד איתו.
            if (source is UIElement element && !element.IsKeyboardFocusWithin && !element.IsMouseOver)
            {
                return;
            }

            SetUnsavedChanges(true);
        }

        private void SetUnsavedChanges(bool value)
        {
            _hasUnsavedChanges = value;
            UnsavedChangesIndicator.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_hasUnsavedChanges && !_skipUnsavedChangesPrompt)
            {
                MessageBoxResult result = AppMessageBoxWindow.Show(
                    "יש שינויים שלא נשמרו. לשמור אותם לפני הסגירה?",
                    "שינויים שלא נשמרו",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question,
                    this);

                if (result == MessageBoxResult.Yes)
                {
                    SaveSettings(closeAfter: false);
                }
                else if (result != MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
        }

        private void SettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }

            if (e.Key == Key.F)
            {
                SettingsSearchBox.Focus();
                SettingsSearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.S)
            {
                SaveSettings(closeAfter: false);
                e.Handled = true;
            }
        }

        // ===================== חיפוש =====================

        private void BuildSearchIndex()
        {
            _searchIndex.Clear();
            for (int pageIndex = 0; pageIndex < MainTabControl.Items.Count; pageIndex++)
            {
                if (MainTabControl.Items[pageIndex] is not TabItem page)
                {
                    continue;
                }

                string pageName = page.Header as string ?? string.Empty;
                _searchIndex.Add(new SearchEntry(null, pageIndex, pageName, "עמוד", Normalize($"{pageName} {page.Tag}")));

                foreach (FrameworkElement element in LogicalDescendants(page))
                {
                    if (element is SettingsCard card && !string.IsNullOrWhiteSpace(card.Header))
                    {
                        _searchIndex.Add(new SearchEntry(card, pageIndex, card.Header!, pageName,
                            Normalize($"{card.Header} {card.Description} {card.Keywords}")));
                    }
                    else if (element is ToggleButton accordion && accordion is not CheckBox && accordion is not RadioButton
                             && accordion.Content is string accordionTitle && accordion.Tag is string)
                    {
                        _searchIndex.Add(new SearchEntry(accordion, pageIndex, accordionTitle, pageName, Normalize(accordionTitle)));
                    }
                }
            }
        }

        private static IEnumerable<FrameworkElement> LogicalDescendants(DependencyObject root)
        {
            foreach (object child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is FrameworkElement element)
                {
                    yield return element;
                    foreach (FrameworkElement descendant in LogicalDescendants(element))
                    {
                        yield return descendant;
                    }
                }
            }
        }

        /// <summary>מנרמל טקסט לחיפוש: בלי גרשיים, מקפים ותווי ניקוד, ובאותיות קטנות.</summary>
        private static string Normalize(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c is '"' or '\'' or '׳' or '״' or '-' or '־' || (c >= '֑' && c <= 'ׇ'))
                {
                    continue;
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private void SettingsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SettingsSearchPlaceholder.Visibility = SettingsSearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

            string[] words = Normalize(SettingsSearchBox.Text).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            SearchResultsList.Items.Clear();
            if (words.Length == 0)
            {
                SearchResultsPopup.IsOpen = false;
                return;
            }

            // התאמה בכותרת קודמת להתאמה בתיאור או במילות המפתח.
            IEnumerable<SearchEntry> matches = _searchIndex
                .Where(entry => words.All(entry.SearchText.Contains))
                .OrderByDescending(entry => words.All(Normalize(entry.Title).Contains))
                .Take(10);

            foreach (SearchEntry entry in matches)
            {
                var title = new TextBlock { Text = entry.Title, TextWrapping = TextWrapping.Wrap };
                var page = new TextBlock
                {
                    Text = entry.PageName,
                    FontSize = 11,
                    Foreground = (Brush)FindResource("SecondaryForegroundBrush"),
                };
                var content = new StackPanel();
                content.Children.Add(title);
                content.Children.Add(page);
                SearchResultsList.Items.Add(new ListBoxItem { Content = content, Tag = entry });
            }

            SearchNoResultsText.Visibility = SearchResultsList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            SearchResultsPopup.IsOpen = true;
        }

        private void SettingsSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down when SearchResultsList.Items.Count > 0:
                    SearchResultsList.SelectedIndex = 0;
                    (SearchResultsList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
                    e.Handled = true;
                    break;
                case Key.Enter when SearchResultsList.Items.Count > 0:
                    NavigateToSearchResult((ListBoxItem)SearchResultsList.Items[0]);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    SettingsSearchBox.Clear();
                    e.Handled = true;
                    break;
            }
        }

        private void SearchResultsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(SearchResultsList, (DependencyObject)e.OriginalSource) is ListBoxItem item)
            {
                NavigateToSearchResult(item);
            }
        }

        private void SearchResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResultsList.SelectedItem is ListBoxItem item)
            {
                NavigateToSearchResult(item);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SearchResultsPopup.IsOpen = false;
                SettingsSearchBox.Focus();
                e.Handled = true;
            }
        }

        private void NavigateToSearchResult(ListBoxItem item)
        {
            if (item.Tag is not SearchEntry entry)
            {
                return;
            }

            SearchResultsPopup.IsOpen = false;
            SettingsSearchBox.Clear();
            MainTabControl.SelectedIndex = entry.PageIndex;

            if (entry.Target is ToggleButton accordion)
            {
                accordion.IsChecked = true;
            }

            if (entry.Target is not null)
            {
                FrameworkElement target = entry.Target;
                Dispatcher.BeginInvoke(() =>
                {
                    target.BringIntoView(new Rect(0, -24, target.ActualWidth, target.ActualHeight + 48));
                    HighlightBriefly(target);
                }, DispatcherPriority.Loaded);
            }
        }

        /// <summary>מבליט לרגע את הכרטיס שנמצא בחיפוש, כדי שיהיה ברור לאן הגענו.</summary>
        private void HighlightBriefly(FrameworkElement target)
        {
            if (target is not Control control)
            {
                return;
            }

            control.BorderBrush = (Brush)FindResource("AccentForegroundBrush");
            control.BorderThickness = new Thickness(2);
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.6) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                control.ClearValue(BorderBrushProperty);
                control.ClearValue(BorderThicknessProperty);
            };
            timer.Start();
        }

        private void GoToCalendarPage_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = CalendarTabIndex;
        }

        // ===================== לוח עברי ומועדים =====================

        private HolidaySettings ReadHolidaySettingsFromControls() => new()
        {
            Region = (HolidayRegionMode)Math.Max(0, HolidayRegionComboBox.SelectedIndex),
            Purim = (PurimObservanceMode)Math.Max(0, PurimModeComboBox.SelectedIndex),
            Minhag = (CommunityMinhag)Math.Max(0, MinhagComboBox.SelectedIndex),
            ShowFasts = HolidayShowFastsCheckBox.IsChecked == true,
            ShowRoshChodesh = HolidayShowRoshChodeshCheckBox.IsChecked == true,
            ShowErevChag = HolidayShowErevChagCheckBox.IsChecked == true,
            ShowMinorHolidays = HolidayShowMinorCheckBox.IsChecked == true,
            ShowSpecialShabbatot = HolidayShowSpecialShabbatotCheckBox.IsChecked == true,
            ShowOmer = HolidayShowOmerCheckBox.IsChecked == true,
            ShowShabbatMevarchim = HolidayShowMevarchimCheckBox.IsChecked == true,
            ShowIsruChag = HolidayShowIsruChagCheckBox.IsChecked == true,
            ShowYomKippurKatan = HolidayShowYomKippurKatanCheckBox.IsChecked == true,
            ShowLeilSelichot = HolidayShowSelichotCheckBox.IsChecked == true,
            ShowBehab = HolidayShowBehabCheckBox.IsChecked == true,
            ShowCommunityCustoms = HolidayShowCommunityCheckBox.IsChecked == true,
            WidgetPrimaryOnly = HolidayWidgetPrimaryOnlyCheckBox.IsChecked == true,
            ShowDayNumbers = HolidayDayNumbersCheckBox.IsChecked == true,
        };

        private void HolidaySettingControl_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoading)
            {
                RefreshHolidayPage();
            }
        }

        /// <summary>מעדכן את ההסברים והתצוגה המקדימה בעמוד הלוח העברי לפי הפקדים (כולל מיקום שעוד לא נשמר).</summary>
        private void RefreshHolidayPage()
        {
            if (_isLoading || UpcomingHolidaysGrid is null)
            {
                return;
            }

            HolidaySettings display = ReadHolidaySettingsFromControls();
            int presetIndex = LocationPresetComboBox.SelectedIndex;
            string locationName = presetIndex >= 0 && presetIndex < LocationPresets.Length ? LocationPresets[presetIndex].Name : "מיקום מותאם אישית";
            HolidayOptions options = HolidayOptions.FromSettings(new AppSettings
            {
                LocationName = locationName,
                TimeZoneId = TimeZoneTextBox.Text.Trim(),
                Holidays = display,
            });

            string regionText = options.Israel
                ? "ארץ ישראל: יום טוב אחד, ושמחת תורה ביחד עם שמיני עצרת"
                : "חוץ לארץ: יום טוב שני של גלויות, ושמחת תורה למחרת שמיני עצרת";
            HolidayRegionStatusText.Text = display.Region == HolidayRegionMode.Auto ? $"לפי המיקום ({locationName}) - {regionText}" : regionText;

            string purimText = options.WalledCity
                ? "שושן פורים בט\"ו באדר. כשט\"ו חל בשבת - פורים המשולש: שישי, שבת וראשון."
                : "פורים בי\"ד באדר.";
            PurimStatusText.Text = display.Purim == PurimObservanceMode.Auto ? $"לפי המיקום ({locationName}) - {purimText}" : purimText;

            bool ashkenaz = display.Minhag == CommunityMinhag.Ashkenaz;
            SelichotCard.Description = ashkenaz
                ? "ליל סליחות: מוצאי השבת שלפני ראש השנה (לפחות ארבעה ימים לפניו)"
                : "מב' באלול - הלילה שאחרי ראש חודש אלול";
            BehabCard.Visibility = ashkenaz ? Visibility.Visible : Visibility.Collapsed;
            CommunityCustomsCard.Visibility = ashkenaz ? Visibility.Collapsed : Visibility.Visible;

            DateTime today = AppTimeService.Today();
            ParashaPreviewText.Text = ParashaService.GetParashaName(today, options.Israel) is string parasha
                ? $"פרשת {parasha}"
                : "השבת קוראים קריאת חג";

            FillUpcomingHolidays(today, options);
        }

        private void FillUpcomingHolidays(DateTime today, HolidayOptions options)
        {
            UpcomingHolidaysGrid.Children.Clear();
            UpcomingHolidaysGrid.RowDefinitions.Clear();
            var secondary = (Brush)FindResource("SecondaryForegroundBrush");

            var days = HolidayCalendar.GetUpcoming(today, 14, options)
                .GroupBy(item => item.Date)
                .Take(8)
                .ToList();

            if (days.Count == 0)
            {
                UpcomingHolidaysGrid.Children.Add(new TextBlock { Text = "אין מועדים בשנה הקרובה לפי הבחירה הנוכחית", Foreground = secondary });
                return;
            }

            for (int row = 0; row < days.Count; row++)
            {
                DateTime date = days[row].Key;
                HebrewDate hebrew = HebrewCalendarMath.FromGregorian(date);
                string when = (date - today).Days switch
                {
                    0 => "היום",
                    1 => "מחר",
                    _ => $"יום {HebrewDayNames[(int)date.DayOfWeek]}, {date.ToString("dd/MM/yyyy", AppTimeService.GregorianDisplayCulture)}",
                };
                string names = string.Join(" · ", days[row].Select(item => item.Event.DisplayName(options.Display.ShowDayNumbers)));

                UpcomingHolidaysGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddUpcomingCell($"{HebrewGematria.FormatDay(hebrew.Day)} {HebrewCalendarMath.MonthName(hebrew.Year, hebrew.Month)}", row, 0, null, FontWeights.SemiBold);
                AddUpcomingCell(when, row, 1, secondary, FontWeights.Normal);
                AddUpcomingCell(names, row, 2, null, FontWeights.Normal);
            }
        }

        private void AddUpcomingCell(string text, int row, int column, Brush? foreground, FontWeight weight)
        {
            var cell = new TextBlock
            {
                Text = text,
                FontWeight = weight,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, column < 2 ? 20 : 0, 4),
            };

            if (foreground is not null)
            {
                cell.Foreground = foreground;
            }

            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, column);
            UpcomingHolidaysGrid.Children.Add(cell);
        }
    }
}
