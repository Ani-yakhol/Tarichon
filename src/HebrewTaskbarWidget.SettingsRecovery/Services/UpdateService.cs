using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>מידע על גרסה חדשה שזוהתה ב-GitHub Releases.</summary>
    public sealed class UpdateInfo
    {
        public string Version { get; init; } = "";
        public string DownloadUrl { get; init; } = "";
        public string ReleaseUrl { get; init; } = "";
        public string ReleaseNotes { get; init; } = "";
    }

    /// <summary>
    /// בדיקה, הורדה, והחלה של עדכוני תוכנה דרך GitHub Releases. ראו מדריך
    /// נפרד (docs/מדריך-עדכונים.md) שמסביר בדיוק מה להעלות ל-GitHub כדי
    /// שהמנגנון הזה יעבוד.
    ///
    /// עקרון הפעולה (מגרסה 0.5.2 ואילך - ראו הערה היסטורית למטה): התוכנה
    /// שואלת את ה-API הציבורי של GitHub (לא דורש אימות/מפתח כלשהו, כל עוד
    /// המאגר ציבורי) מה ה-Release העדכני ביותר, משווה את מספר הגרסה שלו
    /// לגרסה המותקנת, ואם יש חדשה יותר - מאתרת בתוך אותו Release את קובץ
    /// ה-EXE של מתקין ה-Inno Setup (לפי תבנית שם - ראו InstallerAssetPattern)
    /// ומורידה אותו. ההחלה בפועל (ApplyUpdateAndRestart) יוצאת מהתוכנה
    /// לגמרי, מריצה את המתקין שהורד במצב שקט-לגמרי (ללא שום חלון/שאלה),
    /// וממתינה שיסיים לפני שהיא מפעילה את התוכנה מחדש. מכיוון שמדובר
    /// במתקין Inno Setup אמיתי (לא בחילוץ ZIP ידני), הוא תמיד מזהה לבד את
    /// תיקיית ההתקנה הקיימת (לפי מזהה קבוע - AppId - הרשום מההתקנה
    /// הקודמת, ללא תלות אם הותקן במיקום ברירת המחדל או לא) ולעולם לא נוגע
    /// בקבצים שאינם חלק מהפרסום עצמו - כך שגם קובץ ההגדרות (settings.json)
    /// וגם כל ערכת קול מותאמת-אישית שנוספה נשארים בדיוק כפי שהיו.
    ///
    /// הערה היסטורית (גרסאות 0.5.1 ומטה): מנגנון קודם הוריד קובץ ZIP "נייד"
    /// וחילץ אותו ידנית מעל תיקיית ההתקנה. זה הוחלף לגמרי במנגנון המתקין
    /// המתואר למעלה, בין השאר כדי לפתור בעיה מתמשכת של שמות תיקיות/קבצים
    /// בעברית (בפרט תיקיית ערכות הקול) שיצאו משובשים אחרי חילוץ - גם אחרי
    /// שהוחלף מנגנון החילוץ עצמו לקידוד UTF-8 מפורש (בגרסה 0.5.1), הבעיה
    /// חזרה כי היא נבעה כבר מיצירת קובץ ה-ZIP המקורי (בכלים לא-אמינים כמו
    /// "שלח אל -> תיקייה דחוסה" של סייר הקבצים). מתקין Inno Setup אמיתי לא
    /// סובל מהבעיה הזו כלל - הוא קורא/כותב שמות קבצים ישירות דרך ממשקי
    /// המערכת של Windows (לא דרך פורמט ZIP עם קידוד תווים שצריך "לנחש").
    /// </summary>
    public static class UpdateService
    {
        // *** יש לעדכן לפני הפצה: בעלים/שם המאגר ב-GitHub. ראו מדריך העדכונים. ***
        private const string GitHubOwner = "Ani-yakhol";
        private const string GitHubRepo = "Tarichon";

        /// <summary>
        /// תבנית שם קובץ המתקין המצורף ל-Release ב-GitHub - בניגוד לגרסאות
        /// קודמות (שם קבוע לגמרי), שם קובץ המתקין **חייב** לכלול את מספר
        /// הגרסה (לדוגמה: Tarichon-Setup-0.5.2.exe) - זו בדיוק התוצאה
        /// הטבעית של קימפול Tarichon-Setup.iss ללא שינוי (ראו
        /// OutputBaseFilename שם), כך שאין צורך בשום שינוי-שם ידני נוסף
        /// לפני העלאה. מאתרים לפי Regex: מתחיל ב-"Tarichon-Setup-" ומסתיים
        /// ב-".exe" (לא תלוי רישיות אותיות).
        /// </summary>
        private static readonly Regex InstallerAssetPattern =
            new(@"^Tarichon-Setup-.*\.exe$", RegexOptions.IgnoreCase);

        /// <summary>כתובת דף המאגר ב-GitHub - נגזרת מאותם קבועים בדיוק ששולטים גם בבדיקת העדכונים, כדי שיהיה מקור אמת יחיד.</summary>
        public const string RepositoryUrl = $"https://github.com/{GitHubOwner}/{GitHubRepo}";

        private static readonly HttpClient Client = BuildHttpClient();

        /// <summary>עדכון שזוהה בבדיקה האחרונה (אם היה) - נשמר לזיכרון בלבד למשך ריצת התוכנה, כדי שפאנל ההגדרות יוכל להציג אותו גם אם נפתח אחרי שהבדיקה כבר רצה ברקע.</summary>
        public static UpdateInfo? AvailableUpdate { get; private set; }

        /// <summary>מופעל כשמתגלה עדכון חדש (או כשהעדכון שהיה מפסיק להיות רלוונטי) - כדי שממשק המשתמש יוכל להתעדכן בלי לבצע polling.</summary>
        public static event EventHandler? AvailableUpdateChanged;

        private static HttpClient BuildHttpClient()
        {
            var client = new HttpClient();
            // ל-GitHub API חובה User-Agent תקין, אחרת הבקשה נדחית.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TarichonUpdater/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.Timeout = TimeSpan.FromSeconds(20);
            return client;
        }

        /// <summary>האם עברו יותר מ-24 שעות מהבדיקה האחרונה (או שמעולם לא נבדק).</summary>
        public static bool IsDailyCheckDue()
        {
            DateTime? last = SettingsService.Current.LastUpdateCheckUtc;
            return last is null || (DateTime.UtcNow - last.Value) > TimeSpan.FromHours(24);
        }

        /// <summary>
        /// בודקת מול GitHub אם יש גרסה חדשה יותר מהמותקנת. שקטה לגמרי -
        /// כל כשל (אין אינטרנט, GitHub לא זמין, וכו') נבלע ומוחזר null,
        /// בלי שום הודעת שגיאה למשתמש (ההתנהגות המבוקשת ל"בדיקה שקטה").
        /// מעדכנת בכל מקרה (הצלחה או כשל) את זמן הבדיקה האחרונה.
        /// </summary>
        public static async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            AppSettings settings = SettingsService.Current;
            settings.LastUpdateCheckUtc = DateTime.UtcNow;
            SettingsService.Save(settings);

            try
            {
                string url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
                string json = await Client.GetStringAsync(url).ConfigureAwait(false);

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                string tagName = root.TryGetProperty("tag_name", out JsonElement tagEl) ? tagEl.GetString() ?? "" : "";
                string versionText = tagName.TrimStart('v', 'V');

                if (!Version.TryParse(versionText, out Version? latestVersion))
                {
                    SetAvailableUpdate(null);
                    return null;
                }

                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);

                if (latestVersion.CompareTo(currentVersion) <= 0)
                {
                    SetAvailableUpdate(null);
                    return null;
                }

                string? downloadUrl = null;
                if (root.TryGetProperty("assets", out JsonElement assets))
                {
                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string? name = asset.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() : null;
                        if (name is not null && InstallerAssetPattern.IsMatch(name))
                        {
                            downloadUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlEl) ? urlEl.GetString() : null;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    // זוהתה גרסה חדשה, אך אין בה קובץ מתקין בשם המצופה - אין
                    // מה להציע (המשתמש עדיין יכול להוריד/להתקין ידנית).
                    SetAvailableUpdate(null);
                    return null;
                }

                string releaseUrl = root.TryGetProperty("html_url", out JsonElement htmlUrlEl) ? htmlUrlEl.GetString() ?? "" : "";
                string notes = root.TryGetProperty("body", out JsonElement bodyEl) ? bodyEl.GetString() ?? "" : "";

                var info = new UpdateInfo
                {
                    Version = latestVersion.ToString(3),
                    DownloadUrl = downloadUrl!,
                    ReleaseUrl = releaseUrl,
                    ReleaseNotes = notes,
                };

                SetAvailableUpdate(info);
                return info;
            }
            catch
            {
                // כשל שקט - אין אינטרנט, GitHub לא זמין, וכו'. הבדיקה הבאה
                // תנסה שוב (עד 24 שעות מהניסיון הזה).
                return null;
            }
        }

        private static void SetAvailableUpdate(UpdateInfo? info)
        {
            bool changed = (AvailableUpdate?.Version != info?.Version);
            AvailableUpdate = info;
            if (changed)
            {
                AvailableUpdateChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>מורידה את קובץ המתקין תוך דיווח התקדמות (0-100). מחזירה את הנתיב המקומי של הקובץ שהורד, או null אם ההורדה נכשלה/בוטלה.</summary>
        public static async Task<string?> DownloadUpdateAsync(string downloadUrl, IProgress<double> progress, CancellationToken cancellationToken)
        {
            string tempInstallerPath = Path.Combine(Path.GetTempPath(), "Tarichon-Update-Setup.exe");

            try
            {
                using HttpResponseMessage response = await Client
                    .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                long readBytes = 0;

                await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using FileStream fileStream = new(tempInstallerPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    readBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        progress.Report(Math.Min(100.0, readBytes * 100.0 / totalBytes));
                    }
                }

                return tempInstallerPath;
            }
            catch
            {
                try
                {
                    if (File.Exists(tempInstallerPath))
                    {
                        File.Delete(tempInstallerPath);
                    }
                }
                catch
                {
                    // לא קריטי
                }

                return null;
            }
        }

        /// <summary>
        /// שלב אחרון: כותבת סקריפט PowerShell עוזר-עדכון לתיקייה זמנית,
        /// מפעילה אותו כתהליך נפרד (שישרוד אחרי שהתוכנה עצמה תיסגר), ורק
        /// אז יוצאת מהתוכנה לגמרי. הסקריפט ממתין שהתוכנה תיסגר בפועל,
        /// מריץ את המתקין שהורד במצב שקט-לגמרי (ללא שום חלון, שאלה, או
        /// צורך בהרשאות מנהל - התוכנה מותקנת ממילא ללא הרשאות מנהל, ראו
        /// PrivilegesRequired=lowest ב-Tarichon-Setup.iss), ממתין שהמתקין
        /// יסיים לגמרי, מוחק את קובץ המתקין הזמני, ורק אז מפעיל את התוכנה
        /// מחדש (המתקין עצמו **לא** עושה זאת בהרצה שקטה - שורת ה-[Run]
        /// שמפעילה את התוכנה בסיום מסומנת "skipifsilent", בדיוק כדי לא
        /// להפתיע משתמש שמריץ את המתקין ידנית ושקט מסיבות אחרות).
        ///
        /// שני עקרונות חשובים שהמתקין (Inno Setup) כבר מטפל בהם לבד, בלי
        /// שום קוד נוסף כאן:
        /// - מיקום התקנה לא-ברירת-מחדל: המתקין מזהה את ההתקנה הקיימת לפי
        ///   מזהה קבוע (AppId, זהה בכל הגרסאות) ומשתמש אוטומטית באותה
        ///   תיקיית התקנה בדיוק שבה המשתמש בחר בפעם הראשונה - כל עוד לא
        ///   מעבירים לו פרמטר "/DIR" מפורש (ואיננו מעבירים), אין שום סיכון
        ///   שהוא "יתקין מחדש" למיקום ברירת המחדל במקום.
        /// - שימור הגדרות/ערכות קול: קטע ה-[Files] במתקין רק מוסיף/מחליף
        ///   קבצים שרשומים בו במפורש - הוא **לעולם לא** מוחק קבצים קיימים
        ///   שאינם ברשימה (זה קורה רק בהסרת התקנה מפורשת, [UninstallDelete],
        ///   שלא מופעלת כאן כלל). קובץ ההגדרות עצמו (settings.json) ממילא
        ///   נמצא ב-%AppData% - מיקום נפרד לגמרי מתיקיית ההתקנה שהמתקין
        ///   נוגע בה - כך שהוא לא נחשף למתקין בכלל.
        /// </summary>
        public static void ApplyUpdateAndRestart(string downloadedInstallerPath)
        {
            string installDir = AppContext.BaseDirectory.TrimEnd('\\');
            string exePath = Path.Combine(installDir, "HebrewTaskbarWidget.exe");
            string scriptPath = Path.Combine(Path.GetTempPath(), "Tarichon-ApplyUpdate.ps1");
            int currentProcessId = Environment.ProcessId;

            string script =
                "$ErrorActionPreference = 'Stop'\r\n" +
                $"try {{ Wait-Process -Id {currentProcessId} -Timeout 15 }} catch {{}}\r\n" +
                "Start-Sleep -Seconds 1\r\n" +
                $"$installerPath = '{downloadedInstallerPath}'\r\n" +
                // /VERYSILENT: בלי שום חלון/פס התקדמות של המתקין (התוכנה
                // עצמה כבר הציגה פס התקדמות להורדה - החלק הזה אמור להיות
                // "בלתי-נראה" למשתמש). /SUPPRESSMSGBOXES: בלי תיבות הודעה
                // כלשהן (למשל אישור Runtime חסר - לא רלוונטי כי המתקין
                // שמפורסם לעדכונים נבנה בלי הכללת Runtime, ראו מדריך
                // העדכונים). /NORESTART: לעולם לא להפעיל מחדש את המחשב
                // אוטומטית, גם אם משהו "יבקש" זאת. /SP-: מדלג על שאלת
                // "?Continue" שחלק מגרסאות Inno Setup עשויות להציג.
                "$installerArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'\r\n" +
                "Start-Process -FilePath $installerPath -ArgumentList $installerArgs -Wait\r\n" +
                "Remove-Item -LiteralPath $installerPath -Force -ErrorAction SilentlyContinue\r\n" +
                $"Start-Process -FilePath '{exePath}'\r\n";

            File.WriteAllText(scriptPath, script, System.Text.Encoding.UTF8);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            Process.Start(psi);

            Environment.Exit(0);
        }
    }
}
