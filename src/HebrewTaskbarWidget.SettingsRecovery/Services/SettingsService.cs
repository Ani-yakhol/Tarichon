using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// טעינה/שמירה של הגדרות האפליקציה כ-JSON תחת תיקיית ה-AppData של המשתמש,
    /// וחשיפת מופע יחיד (Singleton) בזיכרון שכל חלקי האפליקציה קוראים/כותבים
    /// אליו ישירות. כל שינוי שנשמר מפעיל את אירוע <see cref="SettingsChanged"/>
    /// כדי שהוידג'ט, החלונית ותצוגת שולחן העבודה יתעדכנו מיידית.
    /// </summary>
    public static class SettingsService
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HebrewTaskbarWidget");

        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        public static AppSettings Current { get; private set; } = Load();

        /// <summary>מופעל בכל פעם ש-<see cref="Save"/> נקרא בהצלחה, וגם כשההגדרות נטענות מחדש מהדיסק (<see cref="ReloadFromDisk"/>).</summary>
        public static event EventHandler? SettingsChanged;

        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (loaded is not null)
                    {
                        // מיגרציה חד-פעמית: קובצי הגדרות ישנים (לפני החלפת
                        // תיבת הסימון בתפריט התלת-מצבי) שמרו את ההעדפה כ-
                        // AutoRestartExplorerOnLaunch בוליאני. אם קיים ערך
                        // ישן כזה, ממירים אותו לערך המקביל בתפריט החדש, ואז
                        // מנקים את השדה הישן כדי שלא ימשיך "לנצח" שינויים
                        // עתידיים שייעשו דרך התפריט החדש.
                        if (loaded.AutoRestartExplorerOnLaunch is bool legacyAutoRestart)
                        {
                            loaded.ExplorerAutoLaunchMode = legacyAutoRestart
                                ? ExplorerAutoLaunchMode.Automatic
                                : ExplorerAutoLaunchMode.AskEachTime;
                            loaded.AutoRestartExplorerOnLaunch = null;
                        }

                        // מיגרציה חד-פעמית: קובצי הגדרות ישנים עשויים לשמור
                        // הפניות לשם הקודם של "צאת הכוכבים" (עם סיומת הזווית
                        // הטכנית, "צאת הכוכבים (8.5°)") - ברשימת הזמנים
                        // הגלויים ובכללי ההתראה - לפני שהשם הקנוני שונה
                        // (הוסרה הסיומת, ראו ZmanimCalendar.NameTzeitHakochavim).
                        // בלי המיגרציה הזו, הפניות ישנות כאלה פשוט "יפסיקו
                        // להתאים" בשקט (הזמן ייעלם מהרשימה/הכלל יפסיק לפעול).
                        const string legacyTzeitName = "צאת הכוכבים (8.5°)";
                        if (loaded.VisibleZmanNames is not null)
                        {
                            for (int i = 0; i < loaded.VisibleZmanNames.Count; i++)
                            {
                                if (loaded.VisibleZmanNames[i] == legacyTzeitName)
                                {
                                    loaded.VisibleZmanNames[i] = ZmanimCalendar.NameTzeitHakochavim;
                                }
                            }
                        }

                        foreach (ZmanNotificationRule rule in loaded.ZmanNotificationRules)
                        {
                            if (rule.ZmanName == legacyTzeitName)
                            {
                                rule.ZmanName = ZmanimCalendar.NameTzeitHakochavim;
                            }
                        }

                        return loaded;
                    }
                }
            }
            catch
            {
                // קובץ הגדרות פגום/לא קריא - נופלים חזרה לברירות המחדל בשקט,
                // כדי שהאפליקציה תמשיך לעבוד גם אם קובץ ה-JSON נפגם.
            }

            return new AppSettings();
        }

        /// <summary>שומר את ההגדרות הנוכחיות לדיסק ומפעיל את אירוע השינוי.</summary>
        public static void Save(AppSettings settings)
        {
            Current = settings;

            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // כשל בשמירה (לדוגמה: הרשאות) - לא קריטי, ההגדרות עדיין בתוקף
                // בזיכרון עבור ריצה נוכחית זו.
            }

            SettingsChanged?.Invoke(null, EventArgs.Empty);

            // מודיע לתהליכים אחרים (בפרט: הוידג'ט הראשי, אם השמירה בוצעה
            // מכלי הגישה העצמאי להגדרות שרץ כתהליך נפרד) שיש הגדרות חדשות
            // לטעון - כדי שהשינוי ייכנס לתוקף מיידית, בלי צורך בהפעלה מחדש.
            CrossProcessSignal.BroadcastSettingsChanged();
        }

        /// <summary>
        /// טוען מחדש את ההגדרות מהדיסק לתוך הזיכרון של התהליך הנוכחי, ומפעיל
        /// את אירוע השינוי - נקרא כתגובה לאיתות בין-תהליכי מתהליך אחר ששמר
        /// הגדרות חדשות (ראו <see cref="CrossProcessSignal"/>).
        /// </summary>
        public static void ReloadFromDisk()
        {
            Current = Load();
            SettingsChanged?.Invoke(null, EventArgs.Empty);
        }

        public static Models.GeoLocation BuildLocation() => new()
        {
            Name = Current.LocationName,
            LatitudeDegrees = Current.Latitude,
            LongitudeDegrees = Current.Longitude,
            ElevationMeters = Current.ElevationMeters,
            TimeZoneId = Current.TimeZoneId,
        };
    }
}
