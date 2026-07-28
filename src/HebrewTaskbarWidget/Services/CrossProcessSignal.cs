using System;
using System.Threading;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מנגנון תקשורת בין-תהליכית, מבוסס Named EventWaitHandle (אובייקט
    /// סנכרון בשם, ברמת מערכת ההפעלה) - כדי שכלי הגישה העצמאי לפאנל
    /// ההגדרות (HebrewTaskbarWidgetSettings.exe, תהליך נפרד לגמרי) יוכל
    /// "לדבר" עם הוידג'ט הראשי אם הוא רץ ברקע: גם כדי שהגדרות שנשמרו
    /// יכנסו לתוקף מיידית, וגם כדי לאפשר יציאה מלאה מהתוכנה גם כשאין גישה
    /// לתפריט ההקשר של הוידג'ט עצמו.
    ///
    /// גרסה קודמת של המנגנון הזה הייתה מבוססת הודעת Win32 משודרת
    /// (RegisterWindowMessage + PostMessage אל HWND_BROADCAST). זה עבד
    /// בבירור כאשר חלון ההגדרות נפתח מתוך התהליך הראשי עצמו (כי אז השמירה
    /// גם מעדכנת ישירות את אותו Singleton בזיכרון, באותו תהליך - האיתות
    /// הבין-תהליכי לא באמת נדרש שם), אבל בפועל לא היה אמין מספיק כשהשמירה
    /// הגיעה מתהליך אחר לגמרי: הודעות Windows משודרות (HWND_BROADCAST)
    /// כפופות ל-UIPI (User Interface Privilege Isolation) - Windows חוסם
    /// אותן בשקט (בלי שגיאה גלויה) כאשר יש הבדל ברמת ההרשאות בין התהליך
    /// השולח לתהליך המקבל (למשל אם אחד מהם רץ אי-פעם, ולו פעם אחת, "כמנהל
    /// מערכת"). זו הסיבה המדוייקת לכך שהשינוי כן נשמר לדיסק בהצלחה (ולכן
    /// נכנס לתוקף בהפעלה הבאה של הוידג'ט), אך לא הוחל באופן מיידי בתהליך
    /// הראשי הרץ ברקע.
    ///
    /// Named EventWaitHandle בהיקף ה-Session של המשתמש (ללא קידומת
    /// "Global\\") אינו כפוף למגבלת UIPI הזו, ולכן אמין משמעותית יותר
    /// להעברת איתות "מצב השתנה" בין תהליכים באותה סביבת משתמש.
    /// </summary>
    public static class CrossProcessSignal
    {
        private const string SettingsChangedEventName = "HebrewTaskbarWidget_SettingsChangedEvent_9F2C6C4E";
        private const string ExitRequestEventName = "HebrewTaskbarWidget_ExitRequestEvent_9F2C6C4E";

        private static readonly EventWaitHandle SettingsChangedEvent =
            new(initialState: false, mode: EventResetMode.AutoReset, name: SettingsChangedEventName);

        private static readonly EventWaitHandle ExitRequestEvent =
            new(initialState: false, mode: EventResetMode.AutoReset, name: ExitRequestEventName);

        /// <summary>מאותת לכל התהליכים המאזינים (בפרט: הוידג'ט הראשי, אם רץ ברקע) שיש הגדרות חדשות לטעון מהדיסק.</summary>
        public static void BroadcastSettingsChanged() => SettingsChangedEvent.Set();

        /// <summary>מאותת לכל התהליכים המאזינים שיש לבצע כיבוי מלא ומסודר של הוידג'ט הראשי.</summary>
        public static void BroadcastExitRequest() => ExitRequestEvent.Set();

        /// <summary>
        /// מתחיל להאזין (בתהליכון רקע ייעודי, לא תהליכון ה-UI) לשני האיתותים
        /// למעלה, וקורא ל-callback המתאים (על תהליכון הרקע - באחריות הקורא
        /// להעביר ל-Dispatcher.Invoke אם נדרשת גישה לרכיבי UI). נועד להיקרא
        /// פעם אחת בלבד, מהתהליך הראשי, מיד עם עליית הוידג'ט.
        /// </summary>
        public static void StartListening(Action onSettingsChanged, Action onExitRequested)
        {
            var thread = new Thread(() =>
            {
                var handles = new WaitHandle[] { SettingsChangedEvent, ExitRequestEvent };

                while (true)
                {
                    int index = WaitHandle.WaitAny(handles);

                    if (index == 0)
                    {
                        onSettingsChanged();
                    }
                    else if (index == 1)
                    {
                        onExitRequested();
                    }
                }
            })
            {
                IsBackground = true,
                Name = "TarichonCrossProcessSignalListener",
            };

            thread.Start();
        }
    }
}
