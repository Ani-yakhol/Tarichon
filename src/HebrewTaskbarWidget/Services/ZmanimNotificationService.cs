using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// שירות רקע שבודק אחת ל-30 שניות האם הגיע הזמן להתריע על זמן הלכתי
    /// קרוב, לפי שני מקורות עצמאיים: (1) הרשימה הראשית (ZmanNotificationRules) -
    /// שורה אחת לכל זמן, עם דקות-לפני וצליל אופציונלי משלה, ו-(2) "הגדרות
    /// מתקדמות" (AdvancedNotificationRules) - כללי התראה נוספים ועצמאיים,
    /// המאפשרים כמה התראות במקביל על אותו זמן. לכל שילוב של זמן/יום/כלל
    /// ניתנת התראה אחת בלבד (נשמר Set פנימי של "מפתחות" שכבר הותרעו).
    /// </summary>
    public static class ZmanimNotificationService
    {
        private static readonly DispatcherTimer Timer = new(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(30),
        };

        private static readonly HashSet<string> AlreadyNotifiedKeys = new();
        private static DateTime _lastCheckedDate = DateTime.MinValue;

        public static void Start()
        {
            Timer.Tick += (_, _) => CheckAndNotify();
            Timer.Start();

            // בכוונה לא קוראים ל-CheckAndNotify() באופן מיידי וסינכרוני כאן -
            // כדי שלא תופיע התראה "בבזק" מיד עם עליית התוכנה (עוד לפני
            // שהוידג'ט אפילו הספיק להופיע על המסך), אם במקרה "עכשיו" נופל
            // בתוך חלון התראה פעיל. הבדיקה הראשונה תתבצע בטיק הראשון של
            // הטיימר (עד 30 שניות מעליית התוכנה) - עיכוב קצר וסביר, שעדיין
            // שומר על האפשרות "לתפוס" התראה שכבר החל חלון הזמן הפעיל שלה.
        }

        public static void Stop()
        {
            Timer.Stop();
        }

        private static void CheckAndNotify()
        {
            AppSettings settings = SettingsService.Current;
            if (!settings.NotificationsEnabled)
            {
                return;
            }

            bool hasMainRules = settings.ZmanNotificationRules.Any(r => r.Enabled);
            bool hasAdvancedRules = settings.AdvancedNotificationRules.Any(r => r.Enabled);
            if (!hasMainRules && !hasAdvancedRules)
            {
                return;
            }

            DateTime today = AppTimeService.Today();
            if (today != _lastCheckedDate)
            {
                // יום חדש - מאפסים את רשימת ההתראות שכבר נשלחו
                AlreadyNotifiedKeys.Clear();
                _lastCheckedDate = today;
            }

            GeoLocation location = SettingsService.BuildLocation();
            IReadOnlyList<ZmanEntry> todaysZmanim = ZmanimCalendar.Calculate(today, location, SettingsService.Current.CandleLightingMinutesBeforeSunset);
            DateTime now = AppTimeService.Now();

            // --- הרשימה הראשית: שורה אחת לכל זמן, ללא חזרות ---
            if (settings.NotificationShowPopup || settings.NotificationPlaySound)
            {
                foreach (ZmanNotificationRule rule in settings.ZmanNotificationRules)
                {
                    if (!rule.Enabled)
                    {
                        continue;
                    }

                    ZmanEntry? entry = todaysZmanim.FirstOrDefault(z => z.Name == rule.ZmanName);
                    if (entry is null || entry.Time is null)
                    {
                        continue;
                    }

                    double minutesBefore = Math.Max(0, rule.MinutesBefore);
                    DateTime notifyAt = entry.Time.Value.AddMinutes(-minutesBefore);
                    string key = $"{today:yyyy-MM-dd}|main|{rule.ZmanName}";

                    if (AlreadyNotifiedKeys.Contains(key))
                    {
                        continue;
                    }

                    if (now >= notifyAt && now <= entry.Time.Value)
                    {
                        AlreadyNotifiedKeys.Add(key);

                        if (settings.NotificationShowPopup)
                        {
                            ShowToast(entry.Name, entry.Time.Value, minutesBefore, onSnoozeReplaySound: () =>
                                NotificationSoundService.PlayForZman(settings, rule.SoundOverridePath, rule.SoundOverrideFixedName, rule.ZmanName, (int)minutesBefore, entry.Time));
                        }

                        NotificationSoundService.PlayForZman(settings, rule.SoundOverridePath, rule.SoundOverrideFixedName, rule.ZmanName, (int)minutesBefore, entry.Time);
                    }
                }
            }

            // --- הגדרות מתקדמות: כללים עצמאיים, כמה על אותו זמן במקביל ---
            foreach (AdvancedNotificationRule rule in settings.AdvancedNotificationRules)
            {
                if (!rule.Enabled || (!rule.ShowPopup && !rule.PlaySound))
                {
                    continue;
                }

                ZmanEntry? entry = todaysZmanim.FirstOrDefault(z => z.Name == rule.ZmanName);
                if (entry is null || entry.Time is null)
                {
                    continue;
                }

                double minutesBefore = Math.Max(0, rule.MinutesBefore);
                DateTime notifyAt = entry.Time.Value.AddMinutes(-minutesBefore);
                string key = $"{today:yyyy-MM-dd}|adv|{rule.Id}";

                if (AlreadyNotifiedKeys.Contains(key))
                {
                    continue;
                }

                if (now >= notifyAt && now <= entry.Time.Value)
                {
                    AlreadyNotifiedKeys.Add(key);

                    if (rule.ShowPopup)
                    {
                        ShowToast(entry.Name, entry.Time.Value, minutesBefore, rule.ToastDurationSeconds, rule.ToastDarkBackground,
                            onSnoozeReplaySound: () => NotificationSoundService.PlayForAdvancedRule(rule, entry.Time));
                    }

                    NotificationSoundService.PlayForAdvancedRule(rule, entry.Time);
                }
            }
        }

        private static void ShowToast(string zmanName, DateTime zmanTime, double minutesBefore, double? durationSecondsOverride = null, bool? darkBackgroundOverride = null, Action? onSnoozeReplaySound = null)
        {
            string timeText = AppTimeService.FormatZmanTime(zmanTime);
            ToastNotificationWindow.Show(zmanName, minutesBefore, timeText, isTest: false, durationSecondsOverride, darkBackgroundOverride, zmanTime, onSnoozeReplaySound);
        }
    }
}
