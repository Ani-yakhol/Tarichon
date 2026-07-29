using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
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
    /// עקרון הפעולה: התוכנה שואלת את ה-API הציבורי של GitHub (לא דורש
    /// אימות/מפתח כלשהו, כל עוד המאגר ציבורי) מה ה-Release העדכני ביותר,
    /// משווה את מספר הגרסה שלו לגרסה המותקנת, ואם יש חדשה יותר - מאתרת
    /// בתוך אותו Release קובץ ZIP "נייד" בשם קבוע ומורידה אותו. ההחלה
    /// בפועל (ApplyUpdateAndRestart) יוצאת מהתוכנה לגמרי ומריצה סקריפט
    /// PowerShell עוזר שמחלץ את ה-ZIP מעל תיקיית ההתקנה (**לא** נוגע כלל
    /// בתיקיית ה-AppData שבה שמורות הגדרות המשתמש - הן נמצאות במיקום
    /// נפרד לגמרי, ולכן משתמרות אוטומטית מבלי שצריך לעשות דבר במיוחד).
    /// </summary>
    public static class UpdateService
    {
        // *** יש לעדכן לפני הפצה: בעלים/שם המאגר ב-GitHub. ראו מדריך העדכונים. ***
        private const string GitHubOwner = "Ani-yakhol";
        private const string GitHubRepo = "Tarichon";

        /// <summary>שם קבוע שחייב להינתן בדיוק לקובץ ה-ZIP המצורף לכל Release ב-GitHub - ראו מדריך העדכונים.</summary>
        public const string PortableAssetName = "Tarichon-Portable-x64.zip";

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
                        if (string.Equals(name, PortableAssetName, StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlEl) ? urlEl.GetString() : null;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    // זוהתה גרסה חדשה, אך אין בה קובץ עדכון בשם המצופה - אין
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

        /// <summary>מורידה את קובץ העדכון תוך דיווח התקדמות (0-100). מחזירה את הנתיב המקומי של הקובץ שהורד, או null אם ההורדה נכשלה/בוטלה.</summary>
        public static async Task<string?> DownloadUpdateAsync(string downloadUrl, IProgress<double> progress, CancellationToken cancellationToken)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), "Tarichon-Update.zip");

            try
            {
                using HttpResponseMessage response = await Client
                    .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                long readBytes = 0;

                await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using FileStream fileStream = new(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None);

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

                return tempZipPath;
            }
            catch
            {
                try
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
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
        /// שלב אחרון: כותב סקריפט PowerShell עוזר-עדכון לתיקייה זמנית,
        /// מפעיל אותו כתהליך נפרד (שישרוד אחרי שהתוכנה עצמה תיסגר), ורק
        /// אז יוצאת מהתוכנה לגמרי. הסקריפט ממתין שהתוכנה תיסגר בפועל,
        /// מחלץ את קובץ ה-ZIP שהורד לתוך תיקיית ההתקנה (מעל הקבצים
        /// הקיימים - שינוי/הוספה בלבד, לא מחיקה גורפת) ומריץ אותה מחדש.
        /// שים לב: פעולה זו **לא נוגעת כלל** בתיקיית ה-AppData של הגדרות
        /// המשתמש - היא נמצאת במיקום נפרד לגמרי מתיקיית ההתקנה, ולכן כל
        /// ההגדרות וההתאמות (כולל ערכות קול שנוספו וכו') משתמרות אוטומטית.
        /// </summary>
        public static void ApplyUpdateAndRestart(string downloadedZipPath)
        {
            string installDir = AppContext.BaseDirectory.TrimEnd('\\');
            string exePath = Path.Combine(installDir, "HebrewTaskbarWidget.exe");
            string scriptPath = Path.Combine(Path.GetTempPath(), "Tarichon-ApplyUpdate.ps1");
            int currentProcessId = Environment.ProcessId;

            // -----------------------------------------------------------
            // חשוב: **לא** משתמשים בפקודת ה-PowerShell המובנית Expand-Archive
            // (או ב-ZipFile.ExtractToDirectory הפשוט) - שתיהן, בייחוד תחת
            // Windows PowerShell 5.1 (powershell.exe הרגיל, לא pwsh.exe),
            // עלולות "לפרש" שמות קבצים בעברית (ובכלל כל שם לא-ASCII) לפי
            // קידוד המערכת הישן (CP437/ANSI) במקום UTF-8 - וכך נוצרים שמות
            // קבצים/תיקיות משובשים (Mojibake) בפועל בדיסק, גם אם הארכיון
            // המקורי תקין לגמרי. הפתרון: פותחים את הארכיון במפורש עם קידוד
            // UTF-8 (ZipFile.Open עם פרמטר Encoding, זמין כבר מ-.NET
            // Framework 4.5), ומחלצים כל קובץ בעצמנו (ExtractToFile עם
            // overwrite=true) - כך שמות הקבצים תמיד מפוענחים נכון.
            // -----------------------------------------------------------
            string script =
                "$ErrorActionPreference = 'Stop'\r\n" +
                $"try {{ Wait-Process -Id {currentProcessId} -Timeout 15 }} catch {{}}\r\n" +
                "Start-Sleep -Seconds 1\r\n" +
                "Add-Type -AssemblyName System.IO.Compression.FileSystem\r\n" +
                "Add-Type -AssemblyName System.IO.Compression\r\n" +
                $"$zipPath = '{downloadedZipPath}'\r\n" +
                $"$destDir = '{installDir}'\r\n" +
                "$archive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Read, [System.Text.Encoding]::UTF8)\r\n" +
                "try {\r\n" +
                "  foreach ($entry in $archive.Entries) {\r\n" +
                "    if ([string]::IsNullOrEmpty($entry.Name)) { continue }\r\n" +
                "    $destPath = Join-Path $destDir $entry.FullName\r\n" +
                "    $destParent = Split-Path $destPath -Parent\r\n" +
                "    if (-not (Test-Path -LiteralPath $destParent)) {\r\n" +
                "      New-Item -ItemType Directory -Path $destParent -Force | Out-Null\r\n" +
                "    }\r\n" +
                "    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destPath, $true)\r\n" +
                "  }\r\n" +
                "} finally {\r\n" +
                "  $archive.Dispose()\r\n" +
                "}\r\n" +
                $"Remove-Item -LiteralPath '{downloadedZipPath}' -Force -ErrorAction SilentlyContinue\r\n" +
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
