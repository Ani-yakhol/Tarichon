using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HebrewTaskbarWidget.Models;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// חלונית סיור היכרות ראשוני - קרוסלת תמונות (screenshots) שמדגימה את
    /// יכולות התוכנה. מוצגת אוטומטית בפעם הראשונה שהתוכנה עולה במחשב חדש
    /// (ראו MainWindow.ShowOnboardingTourIfNeeded), וגם נגישה בכל עת דרך
    /// כפתור ייעודי בלשונית "כללי" (אחרי חלק העדכונים).
    ///
    /// --- הטמעת תמונות הסיור (למי שמעדכן את התוכן) ---
    ///
    /// התמונות עצמן **אינן** חלק מקוד המקור - יש להניח אותן בתיקיית
    /// Assets/Onboarding/ (תת-תיקייה חדשה, יש ליצור אותה), בשמות:
    ///
    ///     onboarding-1.png, onboarding-2.png, onboarding-3.png, ...
    ///
    /// (מספור רציף החל מ-1, בלי דילוגים). התוכנה סורקת את השמות האלה
    /// בזמן ריצה ומציגה רק את מה שקיים בפועל על הדיסק - אפשר להתחיל עם
    /// פחות מ-6 תמונות בלי לשנות קוד כלל.
    ///
    /// גודל מומלץ לכל תמונה: **1920×1200 פיקסלים, יחס 16:10, PNG**.
    /// התמונה מוצגת בפועל בתוך תיבה בגודל 640×400 (עם Stretch="Uniform" -
    /// שומר על יחס הגובה-רוחב המקורי, לא מעוות) - כלומר גודל התצוגה בפועל
    /// הוא פי 3 קטן מהתמונה המקורית, מה שמבטיח חדות גבוהה גם על מסכים
    /// עם קנה-מידה (DPI Scaling) גבוה (עד פי 3). תמונות שאינן ביחס 16:10
    /// עדיין יוצגו כהלכה (ה-Stretch="Uniform" ימרכז ויתאים אותן, בלי
    /// לעוות), אך ליחס הזה בדיוק אין "עטיפה ריקה" (letterboxing) כלל.
    ///
    /// --- להוסיף יותר מ-6 תמונות ---
    ///
    /// 1) להוסיף עוד קבצי onboarding-N.png (7, 8, ...) לתיקיית
    ///    Assets/Onboarding/ בפועל.
    /// 2) להוסיף אותם ל-ItemGroup הרלוונטי ב-HebrewTaskbarWidget.csproj
    ///    (ליד onboarding-6.png הקיים), עם אותו CopyToOutputDirectory -
    ///    אחרת הם לא ייכללו בפועל בפרסום/בהרצה.
    /// 3) להגדיל את הקבוע MaxImageSlots למטה למספר החדש (או פשוט להשאיר
    ///    אותו גדול מהצורך בפועל - חיפוש קבצים חסרים מעבר לכמות שקיימת
    ///    בפועל הוא זול וללא תופעות לוואי, ראו FindAvailableImagePaths).
    ///
    /// אין צורך בשום שינוי קוד נוסף מעבר לזה - מספר הנקודות, הניווט,
    /// והזמינות דרך לשונית "כללי" כולם מתעדכנים אוטומטית לפי מה שנמצא.
    /// </summary>
    public partial class OnboardingTourWindow : Window
    {
        // תקרה סבירה לכמות התמונות שהתוכנה תחפש בפועל - ראו הערה מפורטת
        // בראש הקובץ. אפשר להגדיל אותה בלי חשש גם "ליתר ביטחון" (המחיר
        // היחיד הוא כמה קריאות File.Exists שמחזירות false - זניח לחלוטין).
        private const int MaxImageSlots = 24;

        private static readonly string ImagesFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Onboarding");

        private readonly List<string> _imagePaths;
        private readonly List<Ellipse> _dots = new();
        private int _currentIndex;

        public OnboardingTourWindow()
        {
            InitializeComponent();

            _imagePaths = FindAvailableImagePaths();

            BuildDots();
            ShowSlide(0);
        }

        /// <summary>
        /// true אם קיימת בפועל, על הדיסק, לפחות תמונת סיור אחת - נבדק לפני
        /// פתיחת החלונית (הן בהצגה האוטומטית והן דרך הכפתור בלשונית
        /// "כללי"), כדי לא להציג חלונית ריקה/שבורה אם התמונות טרם הוטמעו.
        /// </summary>
        public static bool HasAnyImages() => FindAvailableImagePaths().Count > 0;

        private static List<string> FindAvailableImagePaths()
        {
            var found = new List<string>();

            for (int i = 1; i <= MaxImageSlots; i++)
            {
                string path = System.IO.Path.Combine(ImagesFolder, $"onboarding-{i}.png");
                if (File.Exists(path))
                {
                    found.Add(path);
                }
            }

            return found;
        }

        private void BuildDots()
        {
            DotsPanel.Children.Clear();
            _dots.Clear();

            // פחות משתי תמונות - אין טעם בנקודות ניווט כלל (ואין גם לאן
            // לנווט), אבל עדיין מציגים את התמונה היחידה שיש.
            if (_imagePaths.Count < 2)
            {
                PrevButton.Visibility = Visibility.Collapsed;
                NextButton.Visibility = Visibility.Collapsed;
                return;
            }

            for (int i = 0; i < _imagePaths.Count; i++)
            {
                int capturedIndex = i;

                var dot = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Margin = new Thickness(5, 0, 5, 0),
                    Fill = (Brush)FindResource("OnboardingDotInactiveBrush"),
                    Cursor = Cursors.Hand,
                };
                dot.MouseLeftButtonUp += (_, _) => ShowSlide(capturedIndex);

                _dots.Add(dot);
                DotsPanel.Children.Add(dot);
            }
        }

        private void ShowSlide(int index)
        {
            if (_imagePaths.Count == 0)
            {
                return;
            }

            _currentIndex = Math.Clamp(index, 0, _imagePaths.Count - 1);

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(_imagePaths[_currentIndex], UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                SlideImage.Source = bitmap;
            }
            catch
            {
                // תמונה בודדת פגומה/לא נגישה - לא מפילים את כל הסיור בגללה,
                // פשוט משאירים את השקופית הקודמת מוצגת.
            }

            for (int i = 0; i < _dots.Count; i++)
            {
                _dots[i].Fill = i == _currentIndex
                    ? (Brush)FindResource("OnboardingAccentBrush")
                    : (Brush)FindResource("OnboardingDotInactiveBrush");
            }

            PrevButton.IsEnabled = _currentIndex > 0;
            NextButton.IsEnabled = _currentIndex < _imagePaths.Count - 1;
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e) => ShowSlide(_currentIndex - 1);

        private void NextButton_Click(object sender, RoutedEventArgs e) => ShowSlide(_currentIndex + 1);

        /// <summary>"דלג" - לא להציג את הסיור שוב אוטומטית לעולם (אלא אם המשתמש יפעיל אותו מפורשות דרך לשונית "כללי").</summary>
        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            AppSettings settings = SettingsService.Current;
            settings.OnboardingTourSkipped = true;
            SettingsService.Save(settings);
            Close();
        }

        /// <summary>"הזכר לי אחר כך" - לא נוגעים ב-OnboardingTourSkipped בכוונה (נשאר false), כך שהסיור יוצג שוב אוטומטית בפעם הבאה שהתוכנה עולה.</summary>
        private void RemindLaterButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnboardingTourWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                // שמאל = "הבא" ("<" ימני = הקודם, ">" שמאלי = הבא - ראו הערה ב-XAML).
                case Key.Left:
                    ShowSlide(_currentIndex + 1);
                    break;
                case Key.Right:
                    ShowSlide(_currentIndex - 1);
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
        }
    }
}
