using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// חלונית הודעה מותאמת-עיצוב (קצוות מעוגלים, כפתורים תואמים, יישור RTL
    /// אמיתי) - במקום MessageBox הגולמי של Windows, כדי שכל ההודעות בתוכנה
    /// (אישור הפעלה מחדש של Explorer, "התוכנה כבר פועלת", "אודות" וכו')
    /// ייראו כמו חלק אינטגרלי מממשק התוכנה, לא כמו תיבת דו-שיח מערכתית
    /// גנרית. מחליפה את RtlMessageBox (ששימש קודם לכן) עם אותה חתימת Show
    /// בדיוק, כך שהחלפה בכל מקום שבו נעשה בו שימוש היא פשוטה.
    /// </summary>
    public partial class AppMessageBoxWindow : Window
    {
        private MessageBoxResult _result = MessageBoxResult.None;

        // גודל "בינוני" למצב גלילה גדולה - קטן מעט מגודל ברירת המחדל של
        // פאנל ההגדרות עצמו (480x660, ראו SettingsWindow.xaml), כדי שיהיה
        // ברור חזותית שזו חלונית משנית ולא עוד עותק של פאנל ההגדרות.
        private const double LargeModeWidth = 440.0;
        private const double LargeModeHeight = 600.0;
        private const double LargeModeScrollMaxHeight = 420.0;

        private AppMessageBoxWindow(string text, string caption, MessageBoxButton button, MessageBoxImage icon, Window? owner, bool largeScrollable)
        {
            InitializeComponent();

            if (owner is not null)
            {
                Owner = owner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            Title = caption;
            TitleText.Text = caption;
            MessageTextBlock.Text = text;
            IconText.Text = IconGlyphFor(icon);
            IconText.Visibility = icon == MessageBoxImage.None ? Visibility.Collapsed : Visibility.Visible;

            if (largeScrollable)
            {
                // מצב "גלילה גדולה" (למשל "מה חדש בגרסה זו"): במקום להיפתח
                // בגודל שמתאים בדיוק לתוכן (שעלול לצאת ענק אם רשימת השיפורים
                // ארוכה, ואף לחרוג ממסך) - נפתחת בגודל בינוני קבוע, וה-
                // ScrollViewer סביב הטקסט מקבל MaxHeight כדי שרק הוא (לא כל
                // החלונית) יגלול את המשך התוכן שלא נכנס.
                SizeToContent = SizeToContent.Width;
                Height = LargeModeHeight;
                RootBorder.Width = LargeModeWidth;
                RootBorder.MaxWidth = LargeModeWidth;
                MessageScrollViewer.MaxHeight = LargeModeScrollMaxHeight;

                // טקסט ארוך רב-שורות (רשימת שיפורים) קריא יותר מיושר לימין
                // (כברירת המחדל הטבעית לעברית) מאשר ממורכז, בניגוד להודעות
                // הקצרות הרגילות שממשיכות להיות ממורכזות כרגיל.
                MessageTextBlock.TextAlignment = TextAlignment.Right;
            }

            BuildButtons(button);
            ApplyTheme(SettingsService.Current.SettingsPanelDarkMode);
        }

        /// <summary>
        /// מציגה הודעה מודאלית בעיצוב התואם לתוכנה. owner, אם צוין, ממקם
        /// את ההודעה מרכזית ביחס לחלון הקורא ומקשר אליו כחלון-אב (מודאלי).
        /// largeScrollable, אם true, פותח את החלונית בגודל בינוני קבוע עם
        /// גלילה פנימית לתוכן ארוך (ראו הערה למעלה) - מיועד לתוכן ארוך
        /// במיוחד כמו "מה חדש בגרסה זו"; ברירת המחדל (false) משמרת את
        /// ההתנהגות הרגילה (התאמת גודל אוטומטית לתוכן קצר).
        /// </summary>
        public static MessageBoxResult Show(
            string text,
            string caption,
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            Window? owner = null,
            bool largeScrollable = false)
        {
            var dialog = new AppMessageBoxWindow(text, caption, button, icon, owner, largeScrollable);
            dialog.ShowDialog();
            return dialog._result;
        }

        private static string IconGlyphFor(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Error => "⛔",
                MessageBoxImage.Question => "❓",
                MessageBoxImage.Warning => "⚠",
                MessageBoxImage.Information => "ℹ",
                _ => string.Empty,
            };
        }

        private void BuildButtons(MessageBoxButton button)
        {
            ButtonsPanel.Children.Clear();

            switch (button)
            {
                case MessageBoxButton.OKCancel:
                    AddButton("אישור", MessageBoxResult.OK, primary: true);
                    AddButton("ביטול", MessageBoxResult.Cancel, primary: false);
                    break;

                case MessageBoxButton.YesNo:
                    AddButton("כן", MessageBoxResult.Yes, primary: true);
                    AddButton("לא", MessageBoxResult.No, primary: false);
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton("כן", MessageBoxResult.Yes, primary: true);
                    AddButton("לא", MessageBoxResult.No, primary: false);
                    AddButton("ביטול", MessageBoxResult.Cancel, primary: false);
                    break;

                default:
                    AddButton("אישור", MessageBoxResult.OK, primary: true);
                    break;
            }
        }

        private void AddButton(string content, MessageBoxResult result, bool primary)
        {
            var button = new Button
            {
                Content = content,
                Style = (Style)FindResource(primary ? "PrimaryDialogButtonStyle" : "DialogButtonStyle"),
                IsDefault = primary,
            };

            button.Click += (_, _) =>
            {
                _result = result;
                Close();
            };

            ButtonsPanel.Children.Add(button);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        /// <summary>מחילה ערכת נושא בהירה (ברירת מחדל) או כהה, תואמת לצבעי פאנל ההגדרות עצמו.</summary>
        private void ApplyTheme(bool dark)
        {
            if (dark)
            {
                RootBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B1C1F"));
                RootBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3B40"));
                TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                MessageTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6D6D8"));
                ButtonAreaBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33FFFFFF"));

                foreach (Button btn in FindButtons())
                {
                    if (btn.Style == (Style)FindResource("DialogButtonStyle"))
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2B30"));
                        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#44454A"));
                        btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                    }
                }
            }
            else
            {
                RootBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                RootBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D5D5D8"));
                TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B1C1F"));
                MessageTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3B40"));
                ButtonAreaBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E4E4E6"));
            }
        }

        private System.Collections.Generic.IEnumerable<Button> FindButtons()
        {
            foreach (object child in ButtonsPanel.Children)
            {
                if (child is Button btn)
                {
                    yield return btn;
                }
            }
        }
    }
}
