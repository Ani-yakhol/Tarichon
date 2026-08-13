using System;
using Microsoft.Win32;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מסתיר/מציג את תצוגת התאריך/שעה המקורית של Windows בשורת המשימות.
    ///
    /// גרסה 0.3.6 ומטה הסתמכו אך ורק על ערך המדיניות (Group Policy) הלא-רשמי
    /// "HideClock" (תחת Software\Microsoft\Windows\CurrentVersion\Policies\Explorer),
    /// יחד עם הפעלה מחדש כפויה של explorer.exe. זו אכן ההגדרה הרשמית-לא-מתועדת
    /// ("Remove Clock from the system notification area" ב-gpedit), ועבדה
    /// באופן עקבי ב-Windows 10 - אבל שני דברים גרמו לכך שבפועל "לא עבד" אצל
    /// חלק מהמשתמשים:
    ///
    /// 1) ב-Windows 11 (מ-22H2 ומעלה בפרט) תצוגת השעון יושמה מחדש כרכיב XAML
    ///    (Composition/DirectUI) - וב-builds מסויימים explorer.exe פשוט לא
    ///    קורא את ערך ה-HideClock הזה יותר בזמן אתחול, ולכן גם הפעלה מחדש
    ///    של Explorer לא הביאה לשום שינוי נראה לעין.
    /// 2) גם כשהערך כן נקרא, זה קורה רק בזמן *אתחול* חדש של Explorer - כלומר
    ///    לא ניתן "לצייר את זה בחזרה" ברגע שהוא כבר רץ בלי להפעילו מחדש, מה
    ///    שגורם להבהוב לא נעים של שולחן העבודה כולו.
    ///
    /// הפתרון בגרסה זו: הסתרה/הצגה **ישירה** של חלון השעון עצמו (TrayClockWClass,
    /// שאותו האפליקציה כבר מאתרת גם כדי למקם את הוידג'ט - ראו TaskbarClockLocator)
    /// באמצעות ShowWindow(SW_HIDE/SW_SHOW). זו קריאת Win32 חוצה-תהליכים רגילה,
    /// שאינה תלויה בקריאת Registry ע"י explorer.exe בזמן האתחול, ולכן עובדת
    /// מיידית (בלי הבהוב, בלי הפעלה מחדש) הן ב-Windows 10 והן ב-Windows 11 -
    /// כל עוד ה-HWND של השעון קיים ונגיש (המקרה הנפוץ). כדי להתמודד עם
    /// המקרים שבהם Explorer "מחזיר" את החלון לגלוי מיוזמתו (למשל אחרי קריסה/
    /// הפעלה מחדש של עצמו, שינוי DPI, או רענון של שורת המשימות) - הקריאה
    /// חוזרת על עצמה מעת לעת ע"י טיימר ב-MainWindow, בדיוק כמו מנגנון
    /// ה"עליון תמיד" (Topmost) הקיים כבר לוידג'ט עצמו.
    ///
    /// ערך המדיניות ב-Registry עדיין נשמר בנוסף (SetPolicyValue) - לא בשביל
    /// האפקט המיידי, אלא כרשת ביטחון: אם התוכנה לא רצה בעליית Windows הבאה
    /// (למשל המשתמש כיבה אותה), הערך עשוי עדיין לגרום להסתרה ב-Windows 10
    /// בעצמו, בלי תלות בתוכנה בכלל.
    ///
    /// הערה היסטורית (גרסה 0.5.0 עד 0.5.3): נוסתה גישה חלופית **ל-Windows 11
    /// בלבד** שהסתירה את השעון ע"י "כיסוי" חזותי (חלון עוקב, טופ-מוסט) במקום
    /// נגיעה בחלון השעון האמיתי - מתוך הנחה שההסתרה/הצמצום הישירים אינם
    /// אמינים שם. בגרסה 0.5.4 הגישה הזו הוסרה לגמרי: היא יצרה קירוב חזותי
    /// גרוע יותר (בפרט לא הצליחה לצמצם רווח אמיתי), ולמעשה ההסתרה/הצמצום
    /// הישירים המתוארים למעלה עובדים היטב גם ב-Windows 11 ברוב המקרים - כך
    /// שמ-0.5.4 ואילך Windows 11 חוזר להתנהג בדיוק כמו Windows 10 (בדיוק כפי
    /// שהיה עד גרסה 0.4.9), בלי שום טיפול מיוחד לפי גרסת Windows.
    /// </summary>
    public static class WindowsClockVisibilityService
    {
        private const string PolicyKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
        private const string ValueName = "HideClock";

        /// <summary>קורא את ערך המדיניות מה-Registry (לא בהכרח משקף את המצב הנראה בפועל, ראו הערה למעלה).</summary>
        public static bool IsPolicyValueSet()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PolicyKeyPath, writable: false);
            return key?.GetValue(ValueName) is int value && value == 1;
        }

        /// <summary>
        /// מגדיר/מנקה את ערך המדיניות ב-Registry בלבד (כרשת ביטחון להפעלות
        /// עתידיות של Windows/Explorer בלי תלות בתוכנה - ראו הערה בראש
        /// הקובץ) - **לא** מפעיל מחדש את Explorer ולא נותן אפקט מיידי; לכך
        /// משמשת <see cref="ApplyLiveVisibility"/>.
        /// </summary>
        public static void SetPolicyValue(bool hidden)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(PolicyKeyPath, writable: true)
                                         ?? Registry.CurrentUser.CreateSubKey(PolicyKeyPath, writable: true)!;

                if (hidden)
                {
                    key.SetValue(ValueName, 1, RegistryValueKind.DWord);
                }
                else if (key.GetValue(ValueName) is not null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }
            }
            catch
            {
                // לא קריטי - זו רק רשת ביטחון משנית, ראו הערה בראש הקובץ.
            }
        }

        /// <summary>
        /// מחיל בפועל, מיידית, את מצב ההסתרה/הצגה על חלון שעון המערכת עצמו.
        /// בטוח לקריאה חוזרת ותכופה (למשל מטיימר) - אם השעון לא נמצא כרגע
        /// (שורת המשימות בתהליך אתחול מחדש וכו') הפעולה פשוט לא עושה כלום.
        /// </summary>
        public static void ApplyLiveVisibility(bool hidden)
        {
            // מ-0.5.4: אותה גישה בדיוק ב-Windows 10 וב-Windows 11 כאחד (ראו
            // הערה היסטורית בראש הקובץ) - הסתרה/הצגה ישירה של חלון השעון.
            if (!TaskbarClockLocator.TryLocateClockWindow(out IntPtr clockWnd) || clockWnd == IntPtr.Zero)
            {
                return;
            }

            Interop.NativeMethods.ShowWindow(clockWnd, hidden ? Interop.NativeMethods.SW_HIDE : Interop.NativeMethods.SW_SHOW);
        }

        /// <summary>
        /// מפעילה מחדש את explorer.exe - פעולה גלויה (הבהוב קצר של שולחן
        /// העבודה ושורת המשימות), משמשת רק במפורש (ראו ApplyFullEffectWithRestart)
        /// ולעולם לא באופן שקט/אוטומטי, כדי לא להפתיע את המשתמש.
        /// </summary>
        public static void RestartExplorer()
        {
            try
            {
                foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcessesByName("explorer"))
                {
                    process.Kill();
                }

                System.Threading.Thread.Sleep(400);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe") { UseShellExecute = true });
            }
            catch
            {
                // אם משהו נכשל, Windows בדרך כלל מפעיל מחדש את explorer.exe
                // בעצמו תוך שניות (יש לו מנגנון פנימי לכך) - אין צורך לזרוק שגיאה למשתמש.
            }
        }

        /// <summary>
        /// המצב "המלא" (מצמצם גם את הרווח הריק שנשאר אחרי הסתרה חיה בלבד -
        /// ראו ApplyLiveVisibility): קובע את ערך המדיניות ב-Registry *וגם*
        /// מפעיל מחדש את Explorer מיידית. זהו המנגנון היחיד שגורם בפועל
        /// ל-explorer.exe לא להקצות כלל שטח לתצוגת השעון בזמן שהוא בונה
        /// מחדש את שורת המשימות (בניגוד להסתרת חלון קיים, שרק מסתירה אותו
        /// אחרי ששטח כבר הוקצה לו) - ולכן זו הדרך היחידה הידועה לצמצם את
        /// הרווח הריק. מובטח לעבוד ב-Windows 10; ב-Windows 11 (בפרט
        /// גרסאות עדכניות) התוצאה תלויה ב-build הספציפי ואינה מובטחת - אך
        /// מ-0.5.4 ואילך Windows 11 מפעיל בדיוק את אותו מנגנון כמו Windows 10
        /// (ראו הערה היסטורית בראש הקובץ), בלי שום התנהגות חלופית.
        /// </summary>
        public static void ApplyFullEffectWithRestart(bool hidden)
        {
            SetPolicyValue(hidden);
            RestartExplorer();
            RecordCurrentExplorerStartTime();
        }

        /// <summary>
        /// זיהוי חכם: האם באמת יש צורך להפעיל מחדש את Explorer עכשיו, כדי
        /// שהוא "יקרא" את ערך המדיניות (HideClock) מחדש? אם explorer.exe
        /// כבר עלה (מסיבה כלשהי - הפעלה מחדש של המחשב, קריסה שהתאוששה,
        /// הפעלה מחדש ידנית) מאז הפעם האחרונה שהתוכנה בדקה/הפעילה אותו,
        /// זה כבר קרא את ערך המדיניות בעצמו בעלייתו החדשה - ואין צורך
        /// לבצע עוד הפעלה מחדש (מיותרת ומטרידה) כדי להשיג את אותה תוצאה.
        ///
        /// ההשוואה מבוססת על זמן העלייה (StartTime) של תהליך explorer.exe
        /// הנוכחי מול הזמן שנשמר בפעם האחרונה (LastKnownExplorerStartTimeUtc) -
        /// אם התהליך הנוכחי "חדש יותר", Explorer כבר התאתחל מחדש בינתיים.
        /// </summary>
        public static bool NeedsExplorerRestart()
        {
            DateTime? currentStartTimeUtc = TryGetExplorerStartTimeUtc();
            if (currentStartTimeUtc is null)
            {
                // לא הצלחנו לקרוא את זמן העלייה (הרשאות/תזמון) - ליתר בטחון
                // מניחים שכן צריך הפעלה מחדש, כדי לא "לפספס" מצב שבאמת דורש אותה.
                return true;
            }

            DateTime? lastKnown = SettingsService.Current.LastKnownExplorerStartTimeUtc;

            // הפרש-סבילות קטן (שנייה) כדי להימנע מהשוואת חותמות-זמן שוות
            // כמעט-לגמרי שנופלות בצד הלא-נכון בגלל דיוק/עיגול.
            return lastKnown is null || currentStartTimeUtc.Value > lastKnown.Value.AddSeconds(1);
        }

        /// <summary>שומרת בהגדרות את זמן העלייה הנוכחי של explorer.exe - נקרא אחרי הפעלה מחדש בפועל, וגם כשמזהים שהפעלה מחדש לא הייתה נחוצה (כדי "לעדכן את קו הבסיס" לפעם הבאה).</summary>
        public static void RecordCurrentExplorerStartTime()
        {
            DateTime? currentStartTimeUtc = TryGetExplorerStartTimeUtc();
            if (currentStartTimeUtc is null)
            {
                return;
            }

            try
            {
                AppSettings settings = SettingsService.Current;
                if (settings.LastKnownExplorerStartTimeUtc == currentStartTimeUtc)
                {
                    return; // כבר מעודכן - אין טעם לכתוב שוב לדיסק
                }

                settings.LastKnownExplorerStartTimeUtc = currentStartTimeUtc;
                SettingsService.Save(settings);
            }
            catch
            {
                // לא קריטי - במקרה הגרוע נבדוק שוב (ואולי נציע הפעלה מחדש
                // מיותרת) בהפעלה הבאה של התוכנה.
            }
        }

        private static DateTime? TryGetExplorerStartTimeUtc()
        {
            try
            {
                System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("explorer");
                if (processes.Length == 0)
                {
                    return null;
                }

                try
                {
                    return processes[0].StartTime.ToUniversalTime();
                }
                finally
                {
                    foreach (System.Diagnostics.Process process in processes)
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
