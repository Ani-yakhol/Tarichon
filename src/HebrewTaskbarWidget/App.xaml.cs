using System;
using System.Threading;
using System.Windows;
using HebrewTaskbarWidget.Services;

namespace HebrewTaskbarWidget
{
    /// <summary>
    /// נקודת הכניסה לאפליקציה. מוודא שרק מופע אחד של הוידג'ט רץ בו-זמנית,
    /// ומאתחל את חלון הוידג'ט הראשי, את שירות ההתראות על זמני היום, ואת
    /// תצוגת שולחן העבודה החופשית (אם מופעלת בהגדרות).
    /// </summary>
    public partial class App : Application
    {
        // שם ה-Mutex ייחודי לאפליקציה - מונע הרצה כפולה בו-זמנית
        private const string MutexName = "HebrewTaskbarWidget_SingleInstance_Mutex_9F2C6C4E";
        private Mutex? _singleInstanceMutex;
        private MainWindow? _mainWindow;
        private DesktopOverlayWindow? _overlayWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool isNewInstance);

            if (!isNewInstance)
            {
                // כבר יש מופע פעיל של הוידג'ט - אין טעם לפתוח מופע נוסף
                AppMessageBoxWindow.Show(
                    "תאריכון כבר פועל.\nניתן למצוא אותו צמוד לשעון בשורת המשימות.",
                    "תאריכון",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Shutdown();
                return;
            }

            _mainWindow = new MainWindow();
            MainWindow = _mainWindow;
            _mainWindow.Show();

            ZmanimNotificationService.Start();

            ApplyOverlaySettings();
            SettingsService.SettingsChanged += (_, _) => Dispatcher.Invoke(ApplyOverlaySettings);

            RunDailyUpdateCheckIfDue();
        }

        /// <summary>
        /// בדיקת עדכונים שקטה, אחת ליום לכל היותר - רצה ברקע (fire-and-forget),
        /// לא מציגה שום דבר למשתמש בעצמה; אם נמצא עדכון, פאנל ההגדרות
        /// (לשונית "כללי") יציג זאת בפעם הבאה שייפתח, דרך UpdateService.AvailableUpdate.
        /// </summary>
        private static void RunDailyUpdateCheckIfDue()
        {
            if (!SettingsService.Current.CheckForUpdates || !UpdateService.IsDailyCheckDue())
            {
                return;
            }

            _ = UpdateService.CheckForUpdateAsync();
        }

        /// <summary>
        /// יוצר/מסגר/סוגר את חלון תצוגת שולחן העבודה החופשית לפי ההגדרה הנוכחית
        /// (מופעל בעליית האפליקציה, ובכל פעם שההגדרות משתנות בפאנל ההגדרות).
        /// </summary>
        private void ApplyOverlaySettings()
        {
            bool shouldShow = SettingsService.Current.OverlayEnabled;

            if (shouldShow && _overlayWindow is null)
            {
                _overlayWindow = new DesktopOverlayWindow();
                _overlayWindow.Show();
            }
            else if (!shouldShow && _overlayWindow is not null)
            {
                _overlayWindow.Close();
                _overlayWindow = null;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ZmanimNotificationService.Stop();
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
