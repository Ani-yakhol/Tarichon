using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        private const double LargeModeWidth = 560.0;
        private const double LargeModeHeight = 620.0;
        private const double LargeModeScrollMaxHeight = 460.0;

        private bool _isLargeScrollable;

        private AppMessageBoxWindow(string text, string caption, MessageBoxButton button, MessageBoxImage icon, Window? owner, bool largeScrollable, string? extraButtonText = null)
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
            IconText.Text = IconGlyphFor(icon);
            IconText.Visibility = icon == MessageBoxImage.None ? Visibility.Collapsed : Visibility.Visible;

            _isLargeScrollable = largeScrollable;

            if (largeScrollable)
            {
                // מצב "גלילה גדולה" (למשל "מה חדש בגרסה זו"): במקום להיפתח
                // בגודל שמתאים בדיוק לתוכן (שעלול לצאת ענק אם רשימת השיפורים
                // ארוכה, ואף לחרוג ממסך) - נפתחת בגודל בינוני קבוע, וה-
                // ScrollViewer סביב הטקסט מקבל MaxHeight כדי שרק הוא (לא כל
                // החלונית) יגלול את המשך התוכן שלא נכנס.
                //
                // הערה חשובה על הבחירה ב-TextBlock פשוט (לא FlowDocument):
                // בגרסה 0.5.0 (הגרסה הראשונה שבה נוספה חלונית "מה חדש")
                // הטקסט הוצג ביישור RTL תקין ב-MessageTextBlock פשוט הזה
                // בדיוק (TextAlignment="Center", ירושת FlowDirection מה-
                // Window בלבד, בלי שום תוספת). ניסיון מאוחר יותר לשפר את
                // העיצוב (כותרות/תבליטים מסודרים) עבר ל-FlowDocumentScrollViewer
                // - ואז יישור ה-RTL "התקלקל", למרות כמה ניסיונות תיקון שונים
                // (TextAlignment, Language, RLM marks) שלא עזרו. לכן כאן
                // חוזרים במפורש למנגנון המקורי המוכח - TextBlock פשוט - ובונים
                // בו תוכן מסודר (כותרות מודגשות, תבליטים) ידנית דרך Inlines
                // (ראו PopulateReleaseNotesInlines), במקום FlowDocument -
                // כדי לשמר גם את הנראות המשופרת וגם את יישור ה-RTL התקין.
                SizeToContent = SizeToContent.Width;
                Height = LargeModeHeight;
                RootBorder.Width = LargeModeWidth;
                RootBorder.MaxWidth = LargeModeWidth;
                MessageScrollViewer.MaxHeight = LargeModeScrollMaxHeight;

                MessageTextBlock.TextAlignment = TextAlignment.Right;
                PopulateReleaseNotesInlines(MessageTextBlock, text);
            }
            else
            {
                MessageTextBlock.Text = text;
            }

            BuildButtons(button, extraButtonText);
            ApplyTheme(SettingsService.Current.SettingsPanelDarkMode);
        }

        /// <summary>
        /// בונה תוכן מסודר (כותרות מודגשות בצבע הדגשה, תבליטים עם "•") מתוך
        /// טקסט Markdown גולמי כפי שמגיע מ-GitHub Releases, ישירות לתוך
        /// Inlines של TextBlock פשוט - ראו הערה מפורטת למה זה בכוונה לא
        /// FlowDocument (בקונסטרוקטור למעלה). מגבלה ידועה ומקובלת: שורת
        /// תבליט שנעטפת לשורה נוספת לא מקבלת הזחה תלויה (TextBlock פשוט לא
        /// תומך בזה כלל) - פשרה סבירה בהחלט לטובת יישור RTL תקין ומוכח.
        /// </summary>
        private static void PopulateReleaseNotesInlines(TextBlock target, string rawText)
        {
            target.Inlines.Clear();

            var accentBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x5F, 0xB4));

            string[] lines = rawText.Replace("\r\n", "\n").Split('\n');
            bool isFirstLine = true;
            bool previousWasBlank = false;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                string trimmedStart = line.TrimStart();

                if (trimmedStart.Length == 0)
                {
                    previousWasBlank = true;
                    continue;
                }

                if (!isFirstLine)
                {
                    target.Inlines.Add(new LineBreak());
                    if (previousWasBlank)
                    {
                        target.Inlines.Add(new LineBreak());
                    }
                }

                // כותרות Markdown ("#", "##", "###" ...)
                int hashCount = 0;
                while (hashCount < trimmedStart.Length && trimmedStart[hashCount] == '#')
                {
                    hashCount++;
                }

                if (hashCount > 0 && hashCount < trimmedStart.Length && trimmedStart[hashCount] == ' ')
                {
                    string headingText = trimmedStart.Substring(hashCount + 1).Trim();
                    target.Inlines.Add(new Run(headingText)
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = hashCount <= 2 ? 14.5 : 13,
                        Foreground = accentBrush,
                    });
                }
                else if (trimmedStart.StartsWith("- ", StringComparison.Ordinal) ||
                         trimmedStart.StartsWith("* ", StringComparison.Ordinal))
                {
                    string bulletText = trimmedStart.Substring(2).Trim();
                    target.Inlines.Add(new Run("• " + bulletText));
                }
                else
                {
                    target.Inlines.Add(new Run(trimmedStart));
                }

                isFirstLine = false;
                previousWasBlank = false;
            }
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
            bool largeScrollable = false,
            string? extraButtonText = null)
        {
            var dialog = new AppMessageBoxWindow(text, caption, button, icon, owner, largeScrollable, extraButtonText);
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

        private void BuildButtons(MessageBoxButton button, string? extraButtonText = null)
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

                    // extraButtonText: כפתור שלישי אופציונלי (למשל "מה חדש"
                    // בהודעת "עדכון תוכנה זמין") - משתמש ב-MessageBoxResult.Cancel
                    // כערך ה"אות" שלו, כי אינו בשימוש כלל במצב YesNo הרגיל,
                    // כך שאין דו-משמעות מול הכן/לא האמיתיים.
                    if (!string.IsNullOrWhiteSpace(extraButtonText))
                    {
                        AddButton(extraButtonText, MessageBoxResult.Cancel, primary: false);
                    }

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
