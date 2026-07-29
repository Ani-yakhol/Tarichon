using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HebrewTaskbarWidget.Controls
{
    /// <summary>
    /// בורר צבע משותף המשמש בכל מקום באפליקציה שבו בוחרים צבע (טקסט הוידג'ט,
    /// רקע הוידג'ט, טקסט תצוגת שולחן העבודה) - לפי דרישה: הבחירה תמיד מתוך
    /// לוח/ערכת צבעים מובנית (בלחיצה), ומתחת ללוח מוצג קוד הצבע שנבחר.
    /// אין תיבת טקסט לעריכה חופשית - כדי שלא יהיה ניתן להזין קוד לא תקין.
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

        /// <summary>מופעל בכל פעם שהצבע ו/או השקיפות משתנים (בחירת עיגול, או הזזת מחוון השקיפות).</summary>
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
            if (string.IsNullOrWhiteSpace(hex))
            {
                hex = "#FFFFFF";
            }

            _selectedHex = hex;

            Color color = ParseColorOrWhite(hex);
            PreviewBrush.Color = color;
            HexCodeText.Text = hex.ToUpperInvariant();

            foreach (Border border in _swatchBorders)
            {
                bool isMatch = border.Tag is string tagHex &&
                               string.Equals(tagHex, hex, StringComparison.OrdinalIgnoreCase);

                border.BorderBrush = isMatch
                    ? new SolidColorBrush(Color.FromRgb(0x9E, 0xCB, 0xFF))
                    : new SolidColorBrush(Color.FromRgb(0x55, 0x58, 0x60));
                border.BorderThickness = new Thickness(isMatch ? 2.5 : 1);
            }

            if (raiseEvent && !_suppressEvents)
            {
                ColorChanged?.Invoke(this, EventArgs.Empty);
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
    }
}
