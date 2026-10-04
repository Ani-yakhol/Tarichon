using System.Windows;
using System.Windows.Controls;

namespace HebrewTaskbarWidget.Controls
{
    /// <summary>
    /// כרטיס הגדרה בסגנון Windows 11: סמל, כותרת ותיאור בצד אחד, הפקד (Content) בצד השני,
    /// ופקדים נוספים (Details) מתחת. העיצוב מוגדר ב-Themes/SettingsTheme.xaml.
    /// </summary>
    public class SettingsCard : ContentControl
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(SettingsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty DetailsProperty =
            DependencyProperty.Register(nameof(Details), typeof(object), typeof(SettingsCard), new PropertyMetadata(null, OnDetailsChanged));

        /// <summary>מילות חיפוש נוספות שלא מופיעות בכותרת או בתיאור.</summary>
        public static readonly DependencyProperty KeywordsProperty =
            DependencyProperty.Register(nameof(Keywords), typeof(string), typeof(SettingsCard), new PropertyMetadata(null));

        public string? Header
        {
            get => (string?)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>תו מהגופן Segoe Fluent Icons / Segoe MDL2 Assets.</summary>
        public string? Glyph
        {
            get => (string?)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public object? Details
        {
            get => GetValue(DetailsProperty);
            set => SetValue(DetailsProperty, value);
        }

        public string? Keywords
        {
            get => (string?)GetValue(KeywordsProperty);
            set => SetValue(KeywordsProperty, value);
        }

        // הפקדים שב-Details צריכים להיות חלק מהעץ הלוגי, כדי ש-x:Name, חיפוש ו-DataContext יעבדו כרגיל.
        private static void OnDetailsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (SettingsCard)d;
            if (e.OldValue is not null)
            {
                card.RemoveLogicalChild(e.OldValue);
            }

            if (e.NewValue is not null)
            {
                card.AddLogicalChild(e.NewValue);
            }
        }

        protected override System.Collections.IEnumerator LogicalChildren
        {
            get
            {
                var children = new System.Collections.ArrayList();
                System.Collections.IEnumerator baseChildren = base.LogicalChildren;
                while (baseChildren?.MoveNext() == true)
                {
                    children.Add(baseChildren.Current);
                }

                if (Details is not null)
                {
                    children.Add(Details);
                }

                return children.GetEnumerator();
            }
        }
    }
}
