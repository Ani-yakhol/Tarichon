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
        // ברור חזותית שזו חלונית משנית ולא עוד עותק של פאנל ההגדרות. הורחב
        // (מ-440 ל-560) כי תוכן ארוך ומובנה (כמו "מה חדש בגרסה זו", עם
        // כותרות ורשימות-תבליטים) קריא משמעותית יותר ברוחב עמודה נוח מאשר
        // דחוס לרוחב חלונית-הודעה רגילה וקצרה.
        private const double LargeModeWidth = 560.0;
        private const double LargeModeHeight = 620.0;
        private const double LargeModeScrollMaxHeight = 460.0;

        private bool _isLargeScrollable;

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
            IconText.Text = IconGlyphFor(icon);
            IconText.Visibility = icon == MessageBoxImage.None ? Visibility.Collapsed : Visibility.Visible;

            _isLargeScrollable = largeScrollable;

            if (largeScrollable)
            {
                // מצב "גלילה גדולה" (למשל "מה חדש בגרסה זו"): במקום להיפתח
                // בגודל שמתאים בדיוק לתוכן (שעלול לצאת ענק אם רשימת השיפורים
                // ארוכה, ואף לחרוג ממסך) - נפתחת בגודל בינוני קבוע, וה-
                // FlowDocumentScrollViewer סביב הטקסט מקבל MaxHeight כדי שרק
                // הוא (לא כל החלונית) יגלול את המשך התוכן שלא נכנס.
                //
                // התוכן עצמו מוצג דרך MessageFlowViewer (FlowDocument) ולא
                // MessageTextBlock הרגיל - ראו BuildReleaseNotesDocument:
                // FlowDocument תומך ב-RTL וברשימות-תבליטים כהלכה (כולל הזחה
                // תלויה נכונה לשורות עטופות), מה ש-TextBlock פשוט לא יודע
                // לעשות בכלל - זו הייתה הסיבה האמיתית לכך שהטקסט נראה שבור.
                SizeToContent = SizeToContent.Width;
                Height = LargeModeHeight;
                RootBorder.Width = LargeModeWidth;
                RootBorder.MaxWidth = LargeModeWidth;

                MessageScrollViewer.Visibility = Visibility.Collapsed;
                MessageFlowViewer.Visibility = Visibility.Visible;
                MessageFlowViewer.FlowDirection = FlowDirection.RightToLeft;
                MessageFlowViewer.Language = System.Windows.Markup.XmlLanguage.GetLanguage("he-IL");
                MessageFlowViewer.MaxHeight = LargeModeScrollMaxHeight;
                MessageFlowViewer.Document = BuildReleaseNotesDocument(text);
            }
            else
            {
                MessageTextBlock.Text = text;
            }

            BuildButtons(button);
            ApplyTheme(SettingsService.Current.SettingsPanelDarkMode);
        }

        /// <summary>
        /// בונה FlowDocument מובנה מתוך טקסט Markdown גולמי כפי שמגיע מ-GitHub
        /// Releases - כותרות ("#"/"##"/"###"), תבליטי רשימה ("- "/"* ") הופכים
        /// לרשימת-תבליטים אמיתית (List/ListItem, עם הזחה תלויה נכונה לשורות
        /// עטופות - זה בדיוק מה ש-TextBlock פשוט לא ידע לעשות), ופסקאות רגילות
        /// נשארות פסקאות. FlowDirection="RightToLeft" על המסמך כולו נותן יישור
        /// RTL נכון ואמיתי (ולא רק "בערך", כמו שניסיון קודם עם TextBlock+RLM
        /// marks נתן) - בלי צורך בשום תחבולה נוספת.
        /// </summary>
        private static FlowDocument BuildReleaseNotesDocument(string rawText)
        {
            // Language="he-IL" (בנוסף ל-FlowDirection) - ראו התיעוד הרשמי של
            // WPF ל-BIDI: הדוגמאות שם תמיד מציינות את שתי התכונות יחד על
            // אותו אלמנט. בלי Language, מנוע יישור-הטקסט (Uniscribe/DirectWrite)
            // עשוי לפתור תווים "חלשים"/ניטרליים (כמו ספרות בתוך "0.6.4",
            // סימני פיסוק) לפי הנחת-ברירת-מחדל לא-עברית, מה שעלול לגרום
            // להם "לברוח" למקום הלא-נכון בתוך פסקה בעברית - זה כנראה מה
            // שנראה כמו "יישור RTL לא הושג" למרות ש-FlowDirection כשלעצמו
            // כן הוגדר נכון. מוגדר כאן במפורש בכל רמה (מסמך, כל פסקה/רשימה/
            // Run) ולא מוסתמך רק על ירושה - ליתר ביטחון, אחרי שתי הודעות
            // קודמות שבהן הבעיה חזרה על עצמה. כרשת ביטחון נוספת (זולה
            // וללא תופעות לוואי גלויות), כל שורת טקסט מקבלת גם תו RLM
            // (Right-to-Left Mark, U+200F) בלתי-נראה בתחילתה - כדי לעגן את
            // כיוון הבסיס של הפסקה כ-RTL באופן חד-משמעי מבחינת האלגוריתם,
            // ולא רק דרך FlowDirection/Language שאמורים להספיק לבד.
            const char Rlm = '\u200F';

            var hebrew = System.Windows.Markup.XmlLanguage.GetLanguage("he-IL");

            var document = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                Language = hebrew,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                PagePadding = new Thickness(0),
                TextAlignment = TextAlignment.Right,
            };

            var accentBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x5F, 0xB4));

            string[] lines = rawText.Replace("\r\n", "\n").Split('\n');
            List? currentList = null;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                string trimmedStart = line.TrimStart();

                if (trimmedStart.Length == 0)
                {
                    // שורה ריקה = מפרידה בין קבוצות תבליטים/פסקאות (לא
                    // מוסיפים כלום בעצמה - המרווח בין הבלוקים כבר מגיע
                    // מה-Margin של כל Paragraph/List בנפרד).
                    currentList = null;
                    continue;
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
                    document.Blocks.Add(new Paragraph(new Run(Rlm + headingText) { FlowDirection = FlowDirection.RightToLeft, Language = hebrew })
                    {
                        FlowDirection = FlowDirection.RightToLeft,
                        TextAlignment = TextAlignment.Right,
                        Language = hebrew,
                        FontWeight = FontWeights.Bold,
                        FontSize = hashCount <= 2 ? 15.5 : 13.5,
                        Foreground = accentBrush,
                        Margin = new Thickness(0, hashCount <= 2 ? 16 : 10, 0, 5),
                    });
                    currentList = null;
                    continue;
                }

                // תבליטי רשימה ("- " / "* ")
                if (trimmedStart.StartsWith("- ", StringComparison.Ordinal) ||
                    trimmedStart.StartsWith("* ", StringComparison.Ordinal))
                {
                    string bulletText = trimmedStart.Substring(2).Trim();

                    if (currentList is null)
                    {
                        currentList = new List
                        {
                            FlowDirection = FlowDirection.RightToLeft,
                            Language = hebrew,
                            MarkerStyle = TextMarkerStyle.Disc,
                            Margin = new Thickness(0, 2, 0, 8),
                            Padding = new Thickness(22, 0, 0, 0),
                        };
                        document.Blocks.Add(currentList);
                    }

                    currentList.ListItems.Add(new ListItem(new Paragraph(new Run(Rlm + bulletText) { FlowDirection = FlowDirection.RightToLeft, Language = hebrew })
                    {
                        FlowDirection = FlowDirection.RightToLeft,
                        TextAlignment = TextAlignment.Right,
                        Language = hebrew,
                        Margin = new Thickness(0),
                    })
                    {
                        FlowDirection = FlowDirection.RightToLeft,
                        Language = hebrew,
                    });
                    continue;
                }

                // פסקה רגילה
                currentList = null;
                document.Blocks.Add(new Paragraph(new Run(Rlm + trimmedStart) { FlowDirection = FlowDirection.RightToLeft, Language = hebrew })
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    TextAlignment = TextAlignment.Right,
                    Language = hebrew,
                    Margin = new Thickness(0, 2, 0, 2),
                });
            }

            return document;
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

                if (_isLargeScrollable)
                {
                    MessageFlowViewer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6D6D8"));
                }

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

                if (_isLargeScrollable)
                {
                    MessageFlowViewer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3B40"));
                }
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
