using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HebrewTaskbarWidget.Models;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// חלונית קטנה לעריכת התאמה אישית לזמן בודד - נפתחת בלחיצה על שם הזמן
    /// ברשימת "אילו זמנים להציג" (לשונית "מיקום וזמנים"). מאפשרת: שם מותאם
    /// אישית, דריסת שיטת חישוב (לזמנים "רגילים"), הגדרות מיוחדות ל"הדלקת
    /// נרות"/"צאת הכוכבים", והוספת/הסרת "שורת זמן כפולה" (אותו זמן פעם
    /// נוספת, בשיטת החישוב השנייה - ראו AppSettings.ZmanDuplicateRow). שתי
    /// השורות (הראשית והכפולה, כשיש) מוצגות בסימטריה - לכל אחת סמליל מחיקה
    /// משלה; מחיקת אחת מהן משאירה את השנייה כתצורה החדשה, היחידה, של הזמן.
    ///
    /// כל ההגדרות כאן משפיעות על כל מקום בתוכנה שמציג/מתריע על הזמן הזה
    /// (לוח הזמנים, כללי התראה) - ראו ZmanEntry.Key מול ZmanEntry.DisplayName.
    /// </summary>
    public partial class ZmanEditDialog : Window
    {
        private readonly string _baseZmanName;
        private readonly bool _isCandleLighting;
        private readonly bool _isTzeit;
        private readonly ZmanCalculationMethod _globalDefaultMethod;
        private readonly Func<ZmanCalculationMethod, DateTime?> _computeTimeForMethod;

        private ZmanDuplicateRow? _duplicate;

        public string? ResultCustomName { get; private set; }
        public ZmanCalculationMethod? ResultMethodOverride { get; private set; }
        public int ResultCandleLightingMinutes { get; private set; }
        public int? ResultTzeitMinutesOverride { get; private set; }
        public ZmanDuplicateRow? ResultDuplicateRow { get; private set; }

        public ZmanEditDialog(
            string baseZmanName,
            string? currentCustomName,
            ZmanCalculationMethod? currentMethodOverride,
            int currentCandleLightingMinutes,
            int? currentTzeitMinutesOverride,
            ZmanDuplicateRow? existingDuplicate,
            ZmanCalculationMethod globalDefaultMethod,
            Func<ZmanCalculationMethod, DateTime?> computeTimeForMethod)
        {
            InitializeComponent();

            _baseZmanName = baseZmanName;
            _isCandleLighting = baseZmanName == ZmanimCalendar.NameCandleLighting;
            _isTzeit = baseZmanName == ZmanimCalendar.NameTzeitHakochavim;
            _globalDefaultMethod = globalDefaultMethod;
            _computeTimeForMethod = computeTimeForMethod;
            _duplicate = existingDuplicate is null ? null : new ZmanDuplicateRow
            {
                Id = existingDuplicate.Id,
                BaseZmanName = existingDuplicate.BaseZmanName,
                CustomName = existingDuplicate.CustomName,
                Method = existingDuplicate.Method,
            };

            string displayNameForTitle = string.IsNullOrWhiteSpace(currentCustomName) ? baseZmanName : currentCustomName!;
            TitleText.Text = $"עריכת \"{displayNameForTitle}\"";
            CustomNameTextBox.Text = currentCustomName ?? baseZmanName;

            if (_isCandleLighting)
            {
                MethodPanel.Visibility = Visibility.Collapsed;
                TzeitMinutesPanel.Visibility = Visibility.Collapsed;
                DuplicateSectionPanel.Visibility = Visibility.Collapsed;
                PrimaryMethodText.Visibility = Visibility.Collapsed;
                PrimaryDeleteButton.Visibility = Visibility.Collapsed;
                CandleLightingMinutesPanel.Visibility = Visibility.Visible;
                CandleLightingMinutesTextBox.Text = currentCandleLightingMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture);
                PrimaryTimeText.Text = FormatTimeOrDash(_computeTimeForMethod(_globalDefaultMethod));
            }
            else
            {
                CandleLightingMinutesPanel.Visibility = Visibility.Collapsed;
                MethodPanel.Visibility = Visibility.Visible;
                MethodComboBox.SelectedIndex = currentMethodOverride switch
                {
                    null => 0,
                    ZmanCalculationMethod.Gra => 1,
                    _ => 2,
                };

                TzeitMinutesPanel.Visibility = _isTzeit ? Visibility.Visible : Visibility.Collapsed;
                if (_isTzeit)
                {
                    TzeitMinutesTextBox.Text = currentTzeitMinutesOverride?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                }

                RefreshDuplicateUi();
            }
        }

        private static string FormatTimeOrDash(DateTime? time) =>
            "זמן נוכחי: " + (time.HasValue ? AppTimeService.FormatZmanTime(time.Value) : "—");

        /// <summary>מתרגם בחירת ComboBox (0=כללי, 1=גר"א, 2=KosherJava) לשיטה אפקטיבית בפועל (לצורך "השיטה השנייה" של כפילה, ולצורך תצוגת הזמן הנוכחי).</summary>
        private ZmanCalculationMethod EffectivePrimaryMethod()
        {
            return MethodComboBox.SelectedIndex switch
            {
                1 => ZmanCalculationMethod.Gra,
                2 => ZmanCalculationMethod.Mga72Zmaniyos,
                _ => _globalDefaultMethod,
            };
        }

        private static string MethodLabel(ZmanCalculationMethod method) =>
            method == ZmanCalculationMethod.Gra ? "רגילה (זווית שמש)" : "72 דקות זמניות";

        // מונע מ-DuplicateToggle_CheckedChanged להגיב לעדכון התכנותי של
        // DuplicateToggle.IsChecked בתוך RefreshDuplicateUi עצמה - כדי לא
        // ליצור מעגליות/כפילות מיותרת.
        private bool _suppressToggleEvent;

        private void RefreshDuplicateUi()
        {
            _suppressToggleEvent = true;
            DuplicateToggle.IsChecked = _duplicate is not null;
            _suppressToggleEvent = false;

            bool hasDuplicate = _duplicate is not null;
            DuplicateDetailsPanel.Visibility = hasDuplicate ? Visibility.Visible : Visibility.Collapsed;

            // כשיש כפילה, שתי השורות (הראשית והכפולה) מוצגות בסימטריה - לכל
            // אחת תווית שיטה משלה וסמליל מחיקה משלה. בלי כפילה, אלה מוחבאים
            // (השורה הראשית היא היחידה, אין צורך להסביר "שיטה: X" בנפרד).
            PrimaryMethodText.Visibility = hasDuplicate ? Visibility.Visible : Visibility.Collapsed;
            PrimaryDeleteButton.Visibility = hasDuplicate ? Visibility.Visible : Visibility.Collapsed;

            PrimaryMethodText.Text = "שיטת חישוב: " + MethodLabel(EffectivePrimaryMethod());
            PrimaryTimeText.Text = FormatTimeOrDash(_computeTimeForMethod(EffectivePrimaryMethod()));

            if (hasDuplicate)
            {
                DuplicateNameTextBox.Text = _duplicate!.CustomName;
                DuplicateMethodText.Text = "שיטת חישוב: " + MethodLabel(_duplicate.Method);
                DuplicateTimeText.Text = FormatTimeOrDash(_computeTimeForMethod(_duplicate.Method));
            }
        }

        /// <summary>קובעת אוטומטית שהכפילה תמיד תהיה בשיטה השונה מהשורה הראשית - לא ניתן לבחור זאת ידנית (כדי שלא ייווצר מצב של שני זמנים כפולים זהים).</summary>
        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_duplicate is not null)
            {
                ZmanCalculationMethod primary = EffectivePrimaryMethod();
                _duplicate.Method = primary == ZmanCalculationMethod.Gra ? ZmanCalculationMethod.Mga72Zmaniyos : ZmanCalculationMethod.Gra;
            }

            // גם בלי כפילה, מעדכנים את תצוגת הזמן הראשית - היא רלוונטית תמיד
            // (לא רק כשיש כפילה), למרות שהתוויות הנלוות (PrimaryMethodText/
            // PrimaryDeleteButton) מוצגות רק כשיש כפילה.
            PrimaryTimeText.Text = FormatTimeOrDash(_computeTimeForMethod(EffectivePrimaryMethod()));

            if (_duplicate is not null)
            {
                DuplicateMethodText.Text = "שיטת חישוב: " + MethodLabel(_duplicate.Method);
                DuplicateTimeText.Text = FormatTimeOrDash(_computeTimeForMethod(_duplicate.Method));
                PrimaryMethodText.Text = "שיטת חישוב: " + MethodLabel(EffectivePrimaryMethod());
            }
        }

        /// <summary>הפעלת/כיבוי המתג "הוסף שורת זמן כפולה" - ראו הערה מפורטת ב-AppSettings.ZmanDuplicateRow.</summary>
        private void DuplicateToggle_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressToggleEvent)
            {
                return;
            }

            if (DuplicateToggle.IsChecked == true)
            {
                ZmanCalculationMethod primary = EffectivePrimaryMethod();
                ZmanCalculationMethod otherMethod = primary == ZmanCalculationMethod.Gra ? ZmanCalculationMethod.Mga72Zmaniyos : ZmanCalculationMethod.Gra;

                string baseDisplayName = string.IsNullOrWhiteSpace(CustomNameTextBox.Text) ? _baseZmanName : CustomNameTextBox.Text.Trim();
                string suggestedName = otherMethod == ZmanCalculationMethod.Gra
                    ? $"{baseDisplayName} (זווית שמש)"
                    : $"{baseDisplayName} (72 דקות)";

                _duplicate = new ZmanDuplicateRow
                {
                    BaseZmanName = _baseZmanName,
                    CustomName = suggestedName,
                    Method = otherMethod,
                };
            }
            else
            {
                _duplicate = null;
            }

            RefreshDuplicateUi();
        }

        /// <summary>מוחקת את השורה ה"כפולה" (השנייה) - השורה הראשית נשארת כפי שהיא, ללא שינוי.</summary>
        private void DeleteDuplicateButton_Click(object sender, RoutedEventArgs e)
        {
            _duplicate = null;
            RefreshDuplicateUi();
        }

        /// <summary>
        /// מוחקת את השורה ה"ראשית" - מקדמת את הכפילה להיות השורה היחידה/
        /// החדשה (שם ושיטה שלה הופכים לשם/שיטה הראשיים), בדיוק כאילו זו
        /// הייתה ההגדרה מההתחלה. ראו דרישה: "החלונית חוזרת להיראות כמו
        /// שהיה לפני הוספת הזמן הכפול, כששורת הזמן שנותרה היא זו שמוצגת".
        /// </summary>
        private void PrimaryDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_duplicate is null)
            {
                return;
            }

            CustomNameTextBox.Text = _duplicate.CustomName;
            MethodComboBox.SelectedIndex = _duplicate.Method == ZmanCalculationMethod.Gra ? 1 : 2;
            _duplicate = null;
            RefreshDuplicateUi();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string customName = CustomNameTextBox.Text.Trim();
            ResultCustomName = (string.IsNullOrWhiteSpace(customName) || customName == _baseZmanName) ? null : customName;

            if (_isCandleLighting)
            {
                int minutes = ParseClampedInt(CandleLightingMinutesTextBox.Text, fallback: 40, min: 0, max: 60);
                ResultCandleLightingMinutes = minutes;
            }
            else
            {
                ResultMethodOverride = MethodComboBox.SelectedIndex switch
                {
                    1 => ZmanCalculationMethod.Gra,
                    2 => ZmanCalculationMethod.Mga72Zmaniyos,
                    _ => (ZmanCalculationMethod?)null,
                };

                if (_isTzeit)
                {
                    string tzeitText = TzeitMinutesTextBox.Text.Trim();
                    ResultTzeitMinutesOverride = string.IsNullOrEmpty(tzeitText)
                        ? null
                        : ParseClampedInt(tzeitText, fallback: 0, min: 0, max: 999);
                }

                if (_duplicate is not null)
                {
                    _duplicate.CustomName = string.IsNullOrWhiteSpace(DuplicateNameTextBox.Text)
                        ? _duplicate.CustomName
                        : DuplicateNameTextBox.Text.Trim();
                }

                ResultDuplicateRow = _duplicate;
            }

            DialogResult = true;
            Close();
        }

        private static int ParseClampedInt(string text, int fallback, int min, int max)
        {
            if (!int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int value))
            {
                return fallback;
            }

            return Math.Clamp(value, min, max);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
