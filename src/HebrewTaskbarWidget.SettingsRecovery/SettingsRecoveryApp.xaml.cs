using System.Windows;

namespace HebrewTaskbarWidget.SettingsRecovery
{
    /// <summary>
    /// יישום עצמאי, קטן, שכל תפקידו הוא לפתוח ישירות את פאנל ההגדרות הרשמי
    /// (HebrewTaskbarWidget.SettingsWindow) - זוהי דרך גישה רשמית ומלאה
    /// לפאנל ההגדרות, ללא צורך לעבור דרך תפריט ההקשר של הוידג'ט. שימושי
    /// בפרט כאשר הוידג'ט הראשי אינו נגיש על המסך (למשל אחרי ניתוק צג או
    /// שינוי רזולוציה) - במקרה כזה, פתיחה מחדש של התוכנה הראשית לא תעזור
    /// (הוידג'ט כבר רץ ברקע, פשוט לא נראה), ולכן יש צורך בישום נפרד שפותח
    /// ישירות את פאנל ההגדרות הרשמי.
    ///
    /// שינויים שנשמרים כאן זהים ב-100% לשמירה מתוך התוכנה הראשית (אותה
    /// מחלקת SettingsWindow בדיוק), ונכנסים לתוקף מיידית גם אם הוידג'ט
    /// הראשי כבר רץ ברקע - ראו <see cref="HebrewTaskbarWidget.Services.CrossProcessSignal"/>.
    ///
    /// קובץ זה (SettingsRecoveryApp.xaml/.cs) חי באותה תיקיית קוד מקור
    /// בדיוק כמו כל שאר האפליקציה (ראו HebrewTaskbarWidget.SettingsRecovery.csproj
    /// לגבי איך זה נשאר פרוייקט/קובץ הרצה נפרד למרות זאת - הקובץ נקרא
    /// "SettingsRecoveryApp" ולא סתם "App" כדי שיוכל לשבת פיזית לצד קובץ
    /// ה-App.xaml של הפרוייקט הראשי, באותה תיקייה, בלי התנגשות שמות).
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settingsWindow = new HebrewTaskbarWidget.SettingsWindow();
            MainWindow = settingsWindow;
            settingsWindow.Show();
        }
    }
}
