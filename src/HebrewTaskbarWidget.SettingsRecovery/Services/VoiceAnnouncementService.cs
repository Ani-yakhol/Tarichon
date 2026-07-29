using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// "התראה קולית חכמה" (חלק 4 בהתראות): במקום צליל קבוע/קובץ יחיד, משמיעה
    /// הכרזה מדוברת מלאה שמורכבת מכמה קבצי שמע קצרים המושמעים ברצף, זה אחרי
    /// זה - למשל: "שקיעת החמה" ← "יהיה בעוד" ← "עשרים" ← "ו" ← "חמש" ← "דקות"
    /// ← "בשעה" ← "חמש" ← "ו" ← "שלושים" ← "ו" ← "שש" ← "דקות".
    ///
    /// תומכת בכמה "ערכות קול" שונות במקביל: כל קבצי השמע של ערכה נתונה
    /// יושבים בתת-תיקייה משלהם בתוך <see cref="FolderName"/> (למשל
    /// "VoiceAnnouncements\קול-א"), והמשתמש בוחר בפאנל ההגדרות לאיזו ערכה
    /// להשתמש - הרשימה נסרקת מחדש בכל פתיחה, כך שתיקיית ערכה חדשה מזוהה מיד.
    ///
    /// תיקיית ה-Root (<see cref="FolderPath"/>) וקובץ ההדרכה בתוכה משולבים
    /// כחלק מהפרוייקט עצמו ומועתקים אוטומטית לתיקיית הפלט בכל קימפול (ראו
    /// ה-Content item המתאים בקובץ ה-csproj) - אין צורך ביצירה/כתיבה בזמן ריצה.
    ///
    /// קובץ שחסר - פשוט מדולג בשקט, כדי שהכרזה חלקית עדיין תישמע גם אם עדיין
    /// לא סופקו כל הקבצים.
    /// </summary>
    public static class VoiceAnnouncementService
    {
        /// <summary>שם התיקייה (לצד קובץ ההרצה) שבה יושבות תתי-התיקיות של ערכות הקול.</summary>
        public const string FolderName = "VoiceAnnouncements";

        private static readonly string[] SupportedExtensions = { ".wav", ".mp3", ".wma" };

        public static string FolderPath => Path.Combine(AppContext.BaseDirectory, FolderName);

        /// <summary>
        /// מחזירה את שמות כל תתי-התיקיות הקיימות כרגע בתוך VoiceAnnouncements
        /// (כל אחת מייצגת ערכת קול נפרדת) - נסרקת מהדיסק בכל קריאה, כדי
        /// שתיקייה שנוספה זה עתה תזוהה מיד. רשימה ריקה אם התיקייה לא קיימת
        /// כלל או שאין בה תתי-תיקיות.
        /// </summary>
        public static IReadOnlyList<string> GetAvailableKitFolders()
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    return Array.Empty<string>();
                }

                return Directory.GetDirectories(FolderPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Select(name => name!)
                    .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// משמיעה הכרזה קולית מלאה עבור זמן והתראה נתונים - שם הזמן, "יהיה
        /// בעוד", משך הזמן במילים, ואם zmanTime ידוע - גם השעה המדוייקת
        /// במילים. אם minutesBefore אינו חיובי (למשל 0 = "עכשיו"), או שלא
        /// נבחרה ערכת קול, אין הכרזה קולית מתאימה ולא מושמע כלום.
        /// </summary>
        public static void Play(string zmanName, int minutesBefore, string? kitFolderName, DateTime? zmanTime)
        {
            if (minutesBefore <= 0)
            {
                return;
            }

            // אם לא נבחרה ערכת קול במפורש (למשל לפני שהמשתמש פתח ושמר את
            // ההגדרות פעם ראשונה) - נופלים בחזרה לערכה הראשונה הזמינה,
            // במקום פשוט לא להשמיע כלום.
            if (string.IsNullOrWhiteSpace(kitFolderName))
            {
                kitFolderName = GetAvailableKitFolders().FirstOrDefault();

                if (string.IsNullOrWhiteSpace(kitFolderName))
                {
                    return; // אין אף ערכת קול מותקנת בכלל
                }
            }

            string kitPath = Path.Combine(FolderPath, kitFolderName);

            List<string> fileKeys = BuildFileKeySequence(zmanName, minutesBefore, zmanTime);
            var resolvedPaths = new List<string>();

            // קובץ "התחלה" (אופציונלי) - אם קיים בערכת הקול, מושמע ראשון,
            // לפני שם הזמן עצמו. אין הגדרה ייעודית לזה - רק הימצאות הקובץ
            // בתיקיית הערכה קובעת אם הוא יושמע.
            string? startSoundPath = ResolveFilePath(kitPath, "התחלה");
            if (startSoundPath is not null)
            {
                resolvedPaths.Add(startSoundPath);
            }

            foreach (string key in fileKeys)
            {
                string? path = ResolveFilePath(kitPath, key);
                if (path is not null)
                {
                    resolvedPaths.Add(path);
                }
            }

            // קובץ "סוף" (אופציונלי) - אם קיים, מושמע אחרון, אחרי כל השאר.
            string? endSoundPath = ResolveFilePath(kitPath, "סוף");
            if (endSoundPath is not null)
            {
                resolvedPaths.Add(endSoundPath);
            }

            if (resolvedPaths.Count == 0)
            {
                return;
            }

            PlaySequential(resolvedPaths, 0);
        }

        /// <summary>בונה את רצף "מפתחות" הקבצים (בלי סיומת) הנדרש להכרזה השלמה - "כמה זמן נותר" ואחריו, אם ידוע, גם "באיזו שעה בדיוק".</summary>
        private static List<string> BuildFileKeySequence(string zmanName, int minutesBefore, DateTime? zmanTime)
        {
            var sequence = new List<string> { SanitizeFileName(zmanName), "יהיה-בעוד" };
            sequence.AddRange(BuildDurationWords(minutesBefore));

            if (zmanTime is DateTime time)
            {
                sequence.Add("בשעה");
                sequence.AddRange(BuildClockTimeWords(time));
            }

            return sequence;
        }

        /// <summary>"כמה זמן נותר" - שעות (אם יש) ואז דקות, לפי אותם כללי דקדוק של BuildMinutePhrase.</summary>
        private static IEnumerable<string> BuildDurationWords(int minutesBefore)
        {
            int hours = minutesBefore / 60;
            int minutes = minutesBefore % 60;

            if (hours > 0)
            {
                foreach (string word in BuildHourWords(hours))
                {
                    yield return word;
                }

                if (minutes > 0)
                {
                    yield return "ו";
                    foreach (string word in BuildMinutePhrase(minutes, hasPrefix: true))
                    {
                        yield return word;
                    }
                }
            }
            else
            {
                foreach (string word in BuildMinutePhrase(minutes, hasPrefix: false))
                {
                    yield return word;
                }
            }
        }

        /// <summary>"באיזו שעה" - שעה (1-12 בלבד, בלי קשר לפריסת 24/12 שהוגדרה בהגדרות) ואז דקות (אם יש) לפי אותם כללי דקדוק.</summary>
        private static IEnumerable<string> BuildClockTimeWords(DateTime time)
        {
            int hour12 = time.Hour % 12;
            if (hour12 == 0)
            {
                hour12 = 12;
            }

            yield return hour12.ToString(CultureInfo.InvariantCulture);

            if (time.Minute > 0)
            {
                yield return "ו";
                foreach (string word in BuildMinutePhrase(time.Minute, hasPrefix: true))
                {
                    yield return word;
                }
            }
        }

        private static IEnumerable<string> BuildHourWords(int hours)
        {
            if (hours == 1)
            {
                yield return "שעה";
                yield break;
            }

            if (hours == 2)
            {
                yield return "שעתיים";
                yield break;
            }

            yield return hours.ToString(CultureInfo.InvariantCulture);
            yield return "שעות";
        }

        /// <summary>
        /// בונה את "ביטוי הדקות": 1 דקה תמיד "דקה" (בלי "דקות"!) - ואם היא
        /// עומדת לבד (לא אחרי "שעה.../ו") מתלווה אליה גם "אחת" (יחד: "דקה
        /// אחת"). 2 דקות תמיד "שתי" ואז "דקות" (ולא "שתיים", הצורה הסמוכה
        /// הנכונה דקדוקית לפני שם עצם נקבה). 3 ומעלה: קובץ המספר הרגיל (ראו
        /// BuildMinuteNumberWords) ואחריו "דקות".
        /// </summary>
        private static IEnumerable<string> BuildMinutePhrase(int minutes, bool hasPrefix)
        {
            if (minutes == 1)
            {
                yield return "דקה";
                if (!hasPrefix)
                {
                    yield return "אחת";
                }

                yield break;
            }

            if (minutes == 2)
            {
                yield return "שתי";
                yield return "דקות";
                yield break;
            }

            foreach (string word in BuildMinuteNumberWords(minutes))
            {
                yield return word;
            }

            yield return "דקות";
        }

        /// <summary>3-19: קובץ יחיד למספר (כולל את שם המספר המורכב - למשל "15" הוא קובץ שאומר "חמש עשרה"). עשרות עגולות (20/30/40/50): קובץ יחיד. אחרת (21-59 לא עגול): קובץ עשרות + "ו" + קובץ יחידות.</summary>
        private static IEnumerable<string> BuildMinuteNumberWords(int minutes)
        {
            if (minutes <= 0)
            {
                yield break;
            }

            if (minutes < 20 || minutes % 10 == 0)
            {
                yield return minutes.ToString(CultureInfo.InvariantCulture);
                yield break;
            }

            int tens = (minutes / 10) * 10;
            int units = minutes % 10;
            yield return tens.ToString(CultureInfo.InvariantCulture);
            yield return "ו";
            yield return units.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>ממיר שם זמן לשם קובץ חוקי: מסיר גרשיים כפולים (התו היחיד האסור בשמות זמנים) ומחליף רווחים במקפים - למשל "שקיעת החמה" ← "שקיעת-החמה".</summary>
        private static string SanitizeFileName(string zmanName)
        {
            return zmanName.Replace("\"", string.Empty).Replace(' ', '-');
        }

        private static string? ResolveFilePath(string kitPath, string key)
        {
            foreach (string ext in SupportedExtensions)
            {
                string candidate = Path.Combine(kitPath, key + ext);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        // מוחזקים כדי שאובייקטי ה-MediaPlayer הפעילים לא ייאספו ע"י ה-GC
        // באמצע השמעה (אין להם שום רפרנס "חי" אחר מלבד המשתנה המקומי
        // בתוך PlaySequential, שיכול להיאסף עוד לפני שההשמעה האסינכרונית
        // בפועל הסתיימה - זו הייתה כנראה הסיבה לכך שההקראה הקולית נעצרה
        // באמצע לפעמים, בלי דפוס ברור: תזמון ה-GC הוא לא-דטרמיניסטי).
        private static MediaPlayer? _activeSequencePlayer;
        private static MediaPlayer? _activeSequenceNextPlayer;

        /// <summary>עוצרת מיידית כל הכרזה קולית שמתנגנת כרגע (אם יש) - למשל כשלוחצים על כפתור הסגירה/נודניק בהודעה הצפה.</summary>
        public static void Stop()
        {
            try
            {
                _activeSequencePlayer?.Stop();
                _activeSequencePlayer?.Close();
            }
            catch
            {
                // לא קריטי
            }

            try
            {
                _activeSequenceNextPlayer?.Stop();
                _activeSequenceNextPlayer?.Close();
            }
            catch
            {
                // לא קריטי
            }

            _activeSequencePlayer = null;
            _activeSequenceNextPlayer = null;
        }

        /// <summary>מנגן את רשימת הקבצים בזה-אחר-זה (לא בו-זמנית!). כדי לצמצם למינימום את הרווח הנשמע בין קובץ לקובץ (מורגש בעיקר בקבצים קצרים כמו 'ו') - בלי לחתוך אף חלקיק מהשמע עצמו - "מכינים" (Open בלבד, בלי Play) את הקובץ הבא כבר ברגע שהקובץ הנוכחי מתחיל להתנגן, כך שיש לו את כל משך הזמן הזה להיטען/להתמלא-בבאפר; כשהקובץ הנוכחי מסתיים בפועל, הקובץ הבא כבר "חם" ומתחיל להישמע כמעט מיידית, בלי עיכוב טעינה נוסף.</summary>
        private static void PlaySequential(List<string> paths, int index, MediaPlayer? preOpenedPlayer = null)
        {
            if (index >= paths.Count)
            {
                _activeSequencePlayer = null;
                _activeSequenceNextPlayer = null;
                return;
            }

            MediaPlayer player = preOpenedPlayer ?? new MediaPlayer();
            _activeSequencePlayer = player;

            if (preOpenedPlayer is null)
            {
                try
                {
                    player.Open(new Uri(paths[index], UriKind.Absolute));
                }
                catch
                {
                    PlaySequential(paths, index + 1);
                    return;
                }
            }

            MediaPlayer? nextPlayer = null;
            if (index + 1 < paths.Count)
            {
                try
                {
                    nextPlayer = new MediaPlayer();
                    nextPlayer.Open(new Uri(paths[index + 1], UriKind.Absolute));
                }
                catch
                {
                    nextPlayer = null;
                }
            }

            _activeSequenceNextPlayer = nextPlayer;

            player.MediaEnded += (_, _) =>
            {
                player.Close();
                PlaySequential(paths, index + 1, nextPlayer);
            };

            player.MediaFailed += (_, _) =>
            {
                player.Close();
                PlaySequential(paths, index + 1, nextPlayer);
            };

            player.Play();
        }
    }
}
