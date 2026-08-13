using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HebrewTaskbarWidget.Controls
{
    /// <summary>
    /// בורר צבע משותף המשמש בכל מקום באפליקציה שבו בוחרים צבע (טקסט הוידג'ט,
    /// רקע הוידג'ט, טקסט תצוגת שולחן העבודה, וכו') - מוצג כמתג קטן "בחר
    /// צבע" + קוביית תצוגה מקדימה, ובלחיצה עליו נפתחת חלונית קופצת עם לוח
    /// צבעים מלא (גוון/רוויה/בהירות + מחוון גוון), קוד צבע, שדות RGB,
    /// ודוגמיות ערכה מוכנות - בדומה לבוררי צבע סטנדרטיים.
    ///
    /// ה-API הציבורי (SelectedColorHex, ColorChanged, ShowOpacitySlider,
    /// OpacityValue, LoadSilently) נשאר זהה לגרסה הקודמת (שהציגה לוח
    /// דוגמיות תמיד-פתוח בתוך הפאנל עצמו) - כך שכל שאר הקוד באפליקציה
    /// (SettingsWindow.xaml.cs) ממשיך לעבוד בלי שינוי.
    /// </summary>
    public partial class ColorPickerControl : UserControl
    {
        // ערכת צבעים מובנית קבועה - גיוון נייטרלי + מבחר צבעים נפוצים, כולל
        // לבן/שחור עבור טקסט על רקעים בהירים/כהים.
        private static readonly string[] PaletteHex =
        {
            "#FFFFFF", "#000000", "#1B1C1F", "#25262A", "#8F8F8F", "#C9AA5E",
            "#9ECBFF", "#4A90E2", "#2ECC71", "#27AE60", "#F1C40F", "#E67E22",
            "#E74C3C", "#C0392B", "#9B59B6", "#8E44AD", "#1ABC9C", "#16A085",
            "#ECF0F1", "#95A5A6",
        };

        private readonly List<Border> _swatchBorders = new();

        // מצב הצבע הנוכחי, מיוצג הן כ-RGB (מקור האמת עבור ColorChanged/
        // SelectedColorHex) והן כ-HSV (נוח יותר לעדכון מלוח הגוון/רוויה/
        // בהירות ומחוון הגוון בלי "לקפוץ" בין ערכים שקולים בקצוות).
        private double _hue;        // 0..360
        private double _saturation; // 0..1
        private double _value;      // 0..1
        private string _selectedHex = "#FFFFFF";

        private bool _suppressEvents;

        // true בזמן שהקוד מעדכן שדה כלשהו (טקסט/מחוון) בעצמו - מונע
        // מהמאזינים המתאימים "לפרש" את העדכון התכנותי הזה כהקלדה/גרירה
        // של המשתמש (שהיה גורם ללולאת עדכונים מיותרת).
        private bool _isUpdatingProgrammatically;

        private bool _isDraggingSv;
        private bool _isDraggingHue;

        // חלון האב (SettingsWindow, בפועל) שבו מוצגת הבקרה כרגע - נרשם רק
        // בזמן שהחלונית הקופצת פתוחה, כדי לזהות לחיצה "בחוץ" (ראו הערה
        // מפורטת ב-XAML ליד StaysOpen="True").
        private Window? _hostWindow;

        /// <summary>מופעל בכל פעם שהצבע ו/או השקיפות משתנים (בחירה מהלוח/מהמחוונים/מהערכה, או הקלדת קוד/RGB תקין).</summary>
        public event EventHandler? ColorChanged;

        public ColorPickerControl()
        {
            // מגן על InitializeComponent() עם _isUpdatingProgrammatically=true:
            // ל-HexCodeTextBox יש ערך התחלתי מפורש ב-XAML (Text="#FFFFFF"),
            // וקביעת ערך התחלתי כזה ב-XAML מפעילה בפועל את TextChanged שלו
            // כבר תוך כדי InitializeComponent() עצמו (לפני שהגענו לשורה
            // הבאה בבנאי הזה) - ולולא ההגנה הזו, הטיפול היה מנסה לעדכן את
            // RedTextBox/GreenTextBox/BlueTextBox (המוגדרים ב-XAML *אחרי*
            // HexCodeTextBox) לפני שהם בכלל מחוברים לשדות שלהם - זריקת
            // NullReferenceException בתוך InitializeComponent(), שגורמת
            // (כש-ColorPickerControl מוטמע בתוך SettingsWindow) לכישלון
            // שקט לגמרי בפתיחת כל פאנל ההגדרות (הפאנל "פשוט לא נפתח").
            _isUpdatingProgrammatically = true;
            InitializeComponent();
            _isUpdatingProgrammatically = false;

            BuildSwatches();
            SetColorFromRgb(Colors.White, raiseEvent: false);

            SizeChanged += (_, _) => UpdateCursorPositions();
        }

        /// <summary>האם להציג גם מחוון שקיפות (Alpha) - רלוונטי לבחירת צבעי רקע.</summary>
        public bool ShowOpacitySlider
        {
            get => OpacityRow.Visibility == Visibility.Visible;
            set => OpacityRow.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>ערך השקיפות הנוכחי (0 עד 1). רלוונטי רק כאשר <see cref="ShowOpacitySlider"/> מופעל.</summary>
        public double OpacityValue
        {
            get => OpacitySlider.Value;
            set => OpacitySlider.Value = value;
        }

        /// <summary>קוד הצבע הנבחר, בפורמט ‎#RRGGBB.</summary>
        public string SelectedColorHex
        {
            get => _selectedHex;
            set => SetColorFromHex(value, raiseEvent: false);
        }

        private void BuildSwatches()
        {
            SwatchesPanel.Children.Clear();
            _swatchBorders.Clear();

            foreach (string hex in PaletteHex)
            {
                Color color = ParseColorOrWhite(hex);

                var border = new Border
                {
                    Width = 22,
                    Height = 22,
                    Margin = new Thickness(3),
                    CornerRadius = new CornerRadius(11),
                    Background = new SolidColorBrush(color),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x58, 0x60)),
                    BorderThickness = new Thickness(1),
                    Tag = hex,
                    Cursor = Cursors.Hand,
                    ToolTip = hex,
                };

                border.MouseLeftButtonUp += Swatch_MouseLeftButtonUp;

                _swatchBorders.Add(border);
                SwatchesPanel.Children.Add(border);
            }
        }

        private void Swatch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border { Tag: string hex })
            {
                SetColorFromHex(hex, raiseEvent: true);
            }
        }

        // ===================== מתג "בחר צבע" + חלונית קופצת =====================

        private void PickerToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            // ToggleButton כבר הפך את IsChecked באופן אוטומטי לפני שהגענו
            // לכאן (חלק מהתנהגות ה-Click המובנית שלו) - פשוט מיישרים את
            // מצב החלונית הקופצת בהתאם.
            PickerPopup.IsOpen = PickerToggleButton.IsChecked == true;
        }

        private void PreviewSwatchBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            PickerPopup.IsOpen = !PickerPopup.IsOpen;
            PickerToggleButton.IsChecked = PickerPopup.IsOpen;
        }

        private void PickerPopup_Opened(object? sender, EventArgs e)
        {
            PickerToggleButton.IsChecked = true;
            UpdateCursorPositions();

            _hostWindow = Window.GetWindow(this);
            if (_hostWindow is not null)
            {
                _hostWindow.PreviewMouseDown += HostWindow_PreviewMouseDown;
            }
        }

        private void PickerPopup_Closed(object? sender, EventArgs e)
        {
            PickerToggleButton.IsChecked = false;

            if (_hostWindow is not null)
            {
                _hostWindow.PreviewMouseDown -= HostWindow_PreviewMouseDown;
                _hostWindow = null;
            }
        }

        /// <summary>
        /// סוגר את החלונית הקופצת בלחיצה בכל מקום מחוץ לה, למעט על מתג
        /// "בחר צבע" או קוביית התצוגה המקדימה עצמם - שם הלחיצה כבר מטופלת
        /// ישירות ע"י ה-Click/MouseUp המתאימים שלהם (ראו הערה מפורטת
        /// ב-XAML). מאזין רק בזמן שהחלונית פתוחה (נרשם/מוסר ב-Opened/Closed).
        /// </summary>
        private void HostWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!PickerPopup.IsOpen)
            {
                return;
            }

            if (e.OriginalSource is DependencyObject source &&
                (IsWithin(source, PickerToggleButton) ||
                 IsWithin(source, PreviewSwatchBorder) ||
                 IsWithin(source, PopupContentRoot)))
            {
                return;
            }

            PickerPopup.IsOpen = false;
        }

        private static bool IsWithin(DependencyObject node, DependencyObject ancestor)
        {
            DependencyObject? current = node;
            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = current is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        // ===================== לוח גוון/רוויה/בהירות (SV) =====================

        private void SvCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSv = true;
            SvCanvas.CaptureMouse();
            UpdateSvFromPoint(e.GetPosition(SvCanvas));
        }

        private void SvCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingSv)
            {
                UpdateSvFromPoint(e.GetPosition(SvCanvas));
            }
        }

        private void SvCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSv = false;
            SvCanvas.ReleaseMouseCapture();
        }

        private void UpdateSvFromPoint(Point p)
        {
            double width = Math.Max(SvCanvas.ActualWidth, 1);
            double height = Math.Max(SvCanvas.ActualHeight, 1);

            double s = Clamp01(p.X / width);
            double v = 1.0 - Clamp01(p.Y / height);

            _saturation = s;
            _value = v;

            ApplyCurrentHsv(raiseEvent: true);
        }

        // ===================== מחוון גוון (Hue) =====================

        private void HueSlider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingHue = true;
            HueSlider.CaptureMouse();
            UpdateHueFromPoint(e.GetPosition(HueSlider));
        }

        private void HueSlider_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingHue)
            {
                UpdateHueFromPoint(e.GetPosition(HueSlider));
            }
        }

        private void HueSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingHue = false;
            HueSlider.ReleaseMouseCapture();
        }

        private void UpdateHueFromPoint(Point p)
        {
            double width = Math.Max(HueSlider.ActualWidth, 1);
            _hue = Clamp01(p.X / width) * 360.0;

            ApplyCurrentHsv(raiseEvent: true);
        }

        // ===================== קוד צבע (Hex) =====================

        private void HexCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProgrammatically)
            {
                return;
            }

            if (!TryNormalizeHex(HexCodeTextBox.Text, out string normalized))
            {
                return;
            }

            SetColorFromHex(normalized, raiseEvent: true);
        }

        // ===================== שדות RGB =====================

        private void RgbTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProgrammatically)
            {
                return;
            }

            if (!TryParseByte(RedTextBox.Text, out byte r) ||
                !TryParseByte(GreenTextBox.Text, out byte g) ||
                !TryParseByte(BlueTextBox.Text, out byte b))
            {
                return;
            }

            SetColorFromRgb(Color.FromRgb(r, g, b), raiseEvent: true);
        }

        // ===================== שקיפות =====================

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_suppressEvents)
            {
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ===================== ליבת עדכון הצבע (משותפת לכל הנתיבים) =====================

        /// <summary>מעדכן צבע מתוך ערכי H/S/V הנוכחיים (_hue/_saturation/_value) - הנתיב שמשמש את לוח ה-SV ואת מחוון הגוון.</summary>
        private void ApplyCurrentHsv(bool raiseEvent)
        {
            Color rgb = HsvToRgb(_hue, _saturation, _value);
            _selectedHex = ToHex(rgb);

            UpdateAllVisuals(updateSvCursor: false, updateHueCursor: false, updateHexAndRgb: true);

            if (raiseEvent && !_suppressEvents)
            {
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SetColorFromHex(string hex, bool raiseEvent)
        {
            if (!TryNormalizeHex(hex, out string normalized))
            {
                normalized = "#FFFFFF";
            }

            SetColorFromRgb(ParseColorOrWhite(normalized), raiseEvent);
        }

        private void SetColorFromRgb(Color rgb, bool raiseEvent)
        {
            _selectedHex = ToHex(rgb);
            (_hue, _saturation, _value) = RgbToHsv(rgb);

            UpdateAllVisuals(updateSvCursor: true, updateHueCursor: true, updateHexAndRgb: true);

            if (raiseEvent && !_suppressEvents)
            {
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>מרענן את כל הרכיבים הוויזואליים (תצוגה מקדימה, שדה קוד צבע, שדות RGB, סימון עיגול תואם בערכה, שכבת הגוון בלוח ה-SV, ובמקרה הצורך גם מיקום סמני ה-SV/גוון) לפי המצב הפנימי הנוכחי.</summary>
        private void UpdateAllVisuals(bool updateSvCursor, bool updateHueCursor, bool updateHexAndRgb)
        {
            Color rgb = ParseColorOrWhite(_selectedHex);

            PreviewBrush.Color = rgb;
            PopupPreviewBrush.Color = rgb;

            Color pureHue = HsvToRgb(_hue, 1.0, 1.0);
            SvHueLayer.Fill = new SolidColorBrush(pureHue);

            HighlightMatchingSwatch(_selectedHex);

            if (updateHexAndRgb)
            {
                _isUpdatingProgrammatically = true;
                HexCodeTextBox.Text = _selectedHex;
                RedTextBox.Text = rgb.R.ToString();
                GreenTextBox.Text = rgb.G.ToString();
                BlueTextBox.Text = rgb.B.ToString();
                _isUpdatingProgrammatically = false;
            }

            if (updateSvCursor || updateHueCursor)
            {
                UpdateCursorPositions();
            }
        }

        /// <summary>ממקם את סמני לוח ה-SV ומחוון הגוון לפי המצב הפנימי הנוכחי - נקרא גם בכל שינוי גודל (SizeChanged), כי המיקום היחסי תלוי ברוחב/גובה בפועל של הרכיבים.</summary>
        private void UpdateCursorPositions()
        {
            double svWidth = SvCanvas.ActualWidth;
            double svHeight = SvCanvas.ActualHeight;
            if (svWidth > 0 && svHeight > 0)
            {
                SvCursor.Margin = new Thickness(_saturation * svWidth - 7, (1.0 - _value) * svHeight - 7, 0, 0);
            }

            double hueWidth = HueSlider.ActualWidth;
            if (hueWidth > 0)
            {
                HueCursor.Margin = new Thickness((_hue / 360.0) * hueWidth - 3, 0, 0, 0);
            }
        }

        /// <summary>מדגיש (מסגרת מודגשת) את עיגול הערכה התואם לקוד הנתון, אם יש - אחרת כל העיגולים חוזרים למסגרת הרגילה (קוד מותאם אישית, לא מהערכה).</summary>
        private void HighlightMatchingSwatch(string normalizedHex)
        {
            foreach (Border border in _swatchBorders)
            {
                bool isMatch = border.Tag is string tagHex &&
                               string.Equals(tagHex, normalizedHex, StringComparison.OrdinalIgnoreCase);

                border.BorderBrush = isMatch
                    ? new SolidColorBrush(Color.FromRgb(0x9E, 0xCB, 0xFF))
                    : new SolidColorBrush(Color.FromRgb(0x55, 0x58, 0x60));
                border.BorderThickness = new Thickness(isMatch ? 2.5 : 1);
            }
        }

        /// <summary>טוען צבע ושקיפות בלי להפעיל את אירוע <see cref="ColorChanged"/> (למשל בעת טעינת הגדרות).</summary>
        public void LoadSilently(string hex, double? opacity = null)
        {
            _suppressEvents = true;
            SetColorFromHex(hex, raiseEvent: false);
            if (opacity.HasValue)
            {
                OpacitySlider.Value = opacity.Value;
            }
            _suppressEvents = false;
        }

        // ===================== המרות צבע ועזרים =====================

        private static Color ParseColorOrWhite(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Colors.White;
            }
        }

        private static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        /// <summary>ממיר RGB ל-HSV (H: 0..360, S/V: 0..1).</summary>
        private static (double H, double S, double V) RgbToHsv(Color c)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h;
            if (delta < 0.00001)
            {
                h = 0;
            }
            else if (max == r)
            {
                h = 60 * (((g - b) / delta) % 6);
            }
            else if (max == g)
            {
                h = 60 * (((b - r) / delta) + 2);
            }
            else
            {
                h = 60 * (((r - g) / delta) + 4);
            }

            if (h < 0)
            {
                h += 360;
            }

            double s = max <= 0 ? 0 : delta / max;
            double v = max;

            return (h, s, v);
        }

        /// <summary>ממיר HSV (H: 0..360, S/V: 0..1) ל-RGB.</summary>
        private static Color HsvToRgb(double h, double s, double v)
        {
            h = ((h % 360) + 360) % 360;
            s = Clamp01(s);
            v = Clamp01(v);

            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double r1, g1, b1;
            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            byte r = (byte)Math.Round((r1 + m) * 255);
            byte g = (byte)Math.Round((g1 + m) * 255);
            byte b = (byte)Math.Round((b1 + m) * 255);

            return Color.FromRgb(r, g, b);
        }

        private static bool TryParseByte(string? input, out byte value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            if (!int.TryParse(input.Trim(), out int parsed) || parsed < 0 || parsed > 255)
            {
                return false;
            }

            value = (byte)parsed;
            return true;
        }

        /// <summary>
        /// מנסה לפרש קלט חופשי כקוד צבע ‎#RRGGBB תקין - מקבל גם בלי "#"
        /// בהתחלה (מוסיפים אוטומטית), אך דורש בדיוק 6 ספרות הקס אחריו (לא
        /// תומך בקיצור 3-ספרות או בערוץ שקיפות/אלפא כאן - זה נשלט בנפרד
        /// ע"י מחוון השקיפות היכן שרלוונטי). התוצאה תמיד באותיות גדולות.
        /// </summary>
        private static bool TryNormalizeHex(string? input, out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string candidate = input.Trim();
            if (!candidate.StartsWith("#", StringComparison.Ordinal))
            {
                candidate = "#" + candidate;
            }

            if (candidate.Length != 7)
            {
                return false;
            }

            for (int i = 1; i < candidate.Length; i++)
            {
                if (!Uri.IsHexDigit(candidate[i]))
                {
                    return false;
                }
            }

            normalized = candidate.ToUpperInvariant();
            return true;
        }
    }
}
