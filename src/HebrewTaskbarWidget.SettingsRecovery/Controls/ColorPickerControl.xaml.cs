using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HebrewTaskbarWidget.Controls
{
    /// <summary>
    /// בורר צבע משותף המשמש בכל מקום באפליקציה שבו בוחרים צבע (טקסט הוידג'ט,
    /// רקע הוידג'ט, טקסט תצוגת שולחן העבודה) - בחירה מתוך לוח/ערכת צבעים
    /// מובנית (בלחיצה על עיגול), **וגם** הזנה חופשית של קוד צבע (#RRGGBB)
    /// ישירות בתיבת הטקסט שמתחת ללוח - כל אחת מהשתיים מעדכנת את השנייה.
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

        private readonly System.Collections.Generic.List<Border> _swatchBorders = new();
        private string _selectedHex = "#FFFFFF";
        private bool _suppressEvents;

        // true בזמן שהקוד מעדכן את HexCodeTextBox.Text בעצמו (למשל בעקבות
        // בחירת עיגול, או LoadSilently) - מונע מ-HexCodeTextBox_TextChanged
        // "לפרש" את העדכון התכנותי הזה כהקלדה של המשתמש (שהיה גורם ללולאה
        // מיותרת, ובעיקר להתנהגות מוזרה כמו קפיצת סמן הקלדה).
        private bool _isUpdatingTextProgrammatically;

        /// <summary>מופעל בכל פעם שהצבע ו/או השקיפות משתנים (בחירת עיגול, הקלדת קוד תקין, או הזזת מחוון השקיפות).</summary>
        public event EventHandler? ColorChanged;

        public ColorPickerControl()
        {
            InitializeComponent();
            BuildSwatches();
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
            set => SetSelectedColor(value, raiseEvent: false);
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
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(3),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(color),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x58, 0x60)),
                    BorderThickness = new Thickness(1),
                    Tag = hex,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = hex,
                };

                border.MouseLeftButtonUp += Swatch_MouseLeftButtonUp;

                _swatchBorders.Add(border);
                SwatchesPanel.Children.Add(border);
            }
        }

        private void Swatch_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string hex)
            {
                SetSelectedColor(hex, raiseEvent: true);
            }
        }

        private void SetSelectedColor(string hex, bool raiseEvent)
        {
            if (!TryNormalizeHex(hex, out string normalized))
            {
                normalized = "#FFFFFF";
            }

            _selectedHex = normalized;

            Color color = ParseColorOrWhite(normalized);
            PreviewBrush.Color = color;

            _isUpdatingTextProgrammatically = true;
            HexCodeTextBox.Text = normalized;
            _isUpdatingTextProgrammatically = false;

            HighlightMatchingSwatch(normalized);

            if (raiseEvent && !_suppressEvents)
            {
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// מופעלת בכל שינוי בתיבת הטקסט (כולל תוך כדי הקלדה) - כל עוד הטקסט
        /// עדיין לא קוד ‎#RRGGBB תקין, פשוט לא עושים כלום (לא "נלחמים"
        /// במשתמש שעדיין מקליד) - ברגע שהוא הופך לתקין, הצבע מתעדכן מיד
        /// (תצוגה מקדימה, סימון עיגול תואם בערכה אם יש, ואירוע ColorChanged).
        /// </summary>
        private void HexCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingTextProgrammatically)
            {
                return;
            }

            if (!TryNormalizeHex(HexCodeTextBox.Text, out string normalized))
            {
                return;
            }

            _selectedHex = normalized;
            PreviewBrush.Color = ParseColorOrWhite(normalized);
            HighlightMatchingSwatch(normalized);

            if (!_suppressEvents)
            {
                ColorChanged?.Invoke(this, EventArgs.Empty);
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

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_suppressEvents)
            {
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>טוען צבע ושקיפות בלי להפעיל את אירוע <see cref="ColorChanged"/> (למשל בעת טעינת הגדרות).</summary>
        public void LoadSilently(string hex, double? opacity = null)
        {
            _suppressEvents = true;
            SetSelectedColor(hex, raiseEvent: false);
            if (opacity.HasValue)
            {
                OpacitySlider.Value = opacity.Value;
            }
            _suppressEvents = false;
        }

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

