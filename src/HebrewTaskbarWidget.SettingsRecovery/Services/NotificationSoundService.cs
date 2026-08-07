using System;
using System.IO;
using System.Media;
using System.Text;
using System.Windows.Media;
using HebrewTaskbarWidget.Models;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// אחראי על השמעת צליל ההתראה בפועל. שלושה מקורות אפשריים:
    /// 1. צליל קבוע אחד מתוך רשימה קטנה של צלילים מסונתזים עצמאית (ראו
    ///    PlayFixedSound) - לא תלויים כלל בערכת הצלילים של Windows, ולכן
    ///    תמיד נשמעים ותמיד שונים זה מזה (בניגוד לצלילי המערכת - SystemSounds
    ///    - שחלקם עשויים להיות זהים או אף לא מוגדרים כלל, תלוי בערכת הצלילים
    ///    הפעילה במחשב המשתמש).
    /// 2. "התראה קולית חכמה" - הכרזה מדוברת מלאה, ראו VoiceAnnouncementService.
    /// 3. קובץ שמע שנבחר ע"י המשתמש מתוך תיקיה כלשהי במחשב (עיון).
    ///
    /// בכל המקרים, התאמה מיוחדת לזמן/כלל התראה ספציפי (SoundOverridePath /
    /// SoundOverrideFixedName) תמיד גוברת על ההגדרה הכללית.
    /// </summary>
    public static class NotificationSoundService
    {
        // מוחזק כדי שאובייקט ה-MediaPlayer לא ייאסף ע"י ה-GC לפני שסיים לנגן.
        private static MediaPlayer? _activePlayer;

        // מוחזק כדי שאובייקט ה-SoundPlayer (צליל מסונתז) לא ייאסף ע"י ה-GC לפני שסיים לנגן.
        private static SoundPlayer? _activeTonePlayer;

        /// <summary>עוצרת מיידית כל צליל/הכרזה קולית שמתנגנים כרגע, ללא תלות במקור (צליל קבוע/קובץ/הקראה קולית) - למשל כשלוחצים על כפתור הסגירה/נודניק בהודעה הצפה.</summary>
        public static void StopAllPlayback()
        {
            try
            {
                _activePlayer?.Stop();
                _activePlayer?.Close();
            }
            catch
            {
                // לא קריטי
            }

            try
            {
                _activeTonePlayer?.Stop();
            }
            catch
            {
                // לא קריטי
            }

            _activePlayer = null;
            _activeTonePlayer = null;

            VoiceAnnouncementService.Stop();
        }

        private const int SampleRateHz = 44100;

        /// <summary>
        /// משמיע את צליל ההתראה עבור זמן מסויים ברשימה הראשית: אם יש לזמן
        /// הזה קובץ צליל מיוחד משלו (soundOverridePath) הוא גובר; אחרת אם
        /// יש לו צליל קבוע מיוחד (soundOverrideFixedName) הוא גובר; אחרת
        /// משתמשים בהגדרה הכללית (הכרזה קולית / קובץ נבחר / צליל קבוע).
        /// zmanTime (אם ידוע) משמש רק להכרזה הקולית - כדי שתוכל להשמיע גם
        /// את שעת הזמן עצמו במילים, לא רק כמה זמן נותר עד אליו.
        /// </summary>
        public static void PlayForZman(AppSettings settings, string? soundOverridePath, string? soundOverrideFixedName, string zmanName, int minutesBefore, DateTime? zmanTime)
        {
            if (!settings.NotificationPlaySound)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(soundOverridePath))
            {
                PlayFile(soundOverridePath);
                return;
            }

            if (!string.IsNullOrWhiteSpace(soundOverrideFixedName))
            {
                PlayFixedSound(soundOverrideFixedName);
                return;
            }

            switch (settings.NotificationSoundSource)
            {
                case NotificationSoundSourceMode.Voice:
                    VoiceAnnouncementService.Play(zmanName, minutesBefore, settings.NotificationVoiceKitFolderName, zmanTime);
                    break;

                case NotificationSoundSourceMode.CustomFile:
                    if (!string.IsNullOrWhiteSpace(settings.NotificationCustomSoundPath))
                    {
                        PlayFile(settings.NotificationCustomSoundPath);
                    }
                    break;

                default:
                    PlayFixedSound(settings.NotificationFixedSoundName);
                    break;
            }
        }

        /// <summary>משמיע צליל עבור כלל התראה "מתקדם" - קובץ נבחר גובר על הצליל הקבוע.</summary>
        /// <summary>
        /// משמיע צליל עבור כלל התראה "מתקדם" - לפי SoundSource (הקראה קולית /
        /// קובץ נבחר / צליל קבוע), באותה סדר-עדיפות בדיוק כמו PlayForZman.
        /// voiceKey (ברירת מחדל: rule.ZmanName) - השם הקנוני להשתמש בו
        /// בפועל להכרזה הקולית; חשוב להעביר את ZmanEntry.VoiceKey של הקורא
        /// (לא rule.ZmanName ישירות) עבור שורת "זמן כפול", שבה rule.ZmanName
        /// הוא מזהה GUID פנימי בלבד, לא שם זמן קנוני שיש לו קובץ הקלטה.
        /// </summary>
        public static void PlayForAdvancedRule(AdvancedNotificationRule rule, DateTime? zmanTime = null, string? voiceKey = null)
        {
            if (!rule.PlaySound)
            {
                return;
            }

            switch (rule.SoundSource)
            {
                case NotificationSoundSourceMode.Voice:
                    VoiceAnnouncementService.Play(voiceKey ?? rule.ZmanName, rule.MinutesBefore, rule.VoiceKitFolderName, zmanTime);
                    break;

                case NotificationSoundSourceMode.CustomFile:
                    if (!string.IsNullOrWhiteSpace(rule.SoundPath))
                    {
                        PlayFile(rule.SoundPath);
                    }
                    break;

                default:
                    PlayFixedSound(rule.FixedSoundName);
                    break;
            }
        }

        private static void PlayFile(string filePath)
        {
            try
            {
                var player = new MediaPlayer();
                player.MediaEnded += (_, _) => player.Close();
                player.Open(new Uri(filePath, UriKind.Absolute));
                player.Play();
                _activePlayer = player;
            }
            catch
            {
                // קובץ פגום/פורמט לא נתמך/לא נגיש יותר - לא קריטי, פשוט לא מושמע כלום.
            }
        }

        /// <summary>
        /// משמיע אחד מ-5 "פעמונים" קצרים המסונתזים בזמן אמת (טונים טהורים,
        /// לא תלויים בקובץ חיצוני או בערכת הצלילים של Windows) - כל אחד
        /// דפוס תווים שונה לגמרי, כדי שבאמת יישמעו 5 צלילים מובחנים.
        /// (בעבר הסתמכנו על SystemSounds.* של Windows - אך במקרים רבים כמה
        /// מהם מוגדרים לאותו קובץ צליל בפועל בערכת הצלילים הפעילה, ולעיתים
        /// "Windows Question" לא מוגדר כלל החל מ-Windows Vista ואילך - ולכן
        /// לא הושמע ממנו שום דבר. הפתרון: לייצר את הצלילים בעצמנו.)
        /// </summary>
        private static void PlayFixedSound(string name)
        {
            (double FrequencyHz, int DurationMs)[] notes = name switch
            {
                "Asterisk" => new[] { (880.0, 150) },
                "Beep" => new[] { (523.0, 120), (659.0, 150) },
                "Exclamation" => new[] { (784.0, 90), (784.0, 90) },
                "Hand" => new[] { (659.0, 130), (494.0, 180) },
                "Question" => new[] { (440.0, 90), (554.0, 90), (659.0, 140) },
                "Chime" => new[] { (659.0, 90), (831.0, 90), (988.0, 160) },
                "Alert" => new[] { (349.0, 100), (349.0, 100) },
                "Bell" => new[] { (1046.0, 320) },
                "Notify" => new[] { (880.0, 130), (698.0, 180) },
                "Ding" => new[] { (1318.0, 110) },
                _ => new[] { (880.0, 150) },
            };

            try
            {
                byte[] wavBytes = BuildTonesWav(notes);
                var stream = new MemoryStream(wavBytes);
                var player = new SoundPlayer(stream);
                player.Load();
                player.Play();
                _activeTonePlayer = player;
            }
            catch
            {
                // בסביבה נדירה בלי התקן שמע פעיל - לא קריטי.
            }
        }

        /// <summary>בונה קובץ WAV (16-bit PCM, מונו) המשמיע ברצף כל אחד מהתווים שסופקו, עם רווח שקט קצר ביניהם ו-fade in/out קצר בכל תו כדי למנוע נקישה (click) בתחילתו/סופו.</summary>
        private static byte[] BuildTonesWav((double FrequencyHz, int DurationMs)[] notes)
        {
            using var pcmStream = new MemoryStream();
            using (var pcmWriter = new BinaryWriter(pcmStream, Encoding.ASCII, leaveOpen: true))
            {
                const int fadeSamples = 200;
                const int gapMs = 40;

                foreach ((double frequencyHz, int durationMs) in notes)
                {
                    int sampleCount = SampleRateHz * durationMs / 1000;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        double t = i / (double)SampleRateHz;
                        double fade = Math.Min(1.0, Math.Min(i / (double)fadeSamples, (sampleCount - i) / (double)fadeSamples));
                        double sampleValue = 0.5 * fade * Math.Sin(2 * Math.PI * frequencyHz * t);
                        pcmWriter.Write((short)(sampleValue * short.MaxValue));
                    }

                    int gapSamples = SampleRateHz * gapMs / 1000;
                    for (int i = 0; i < gapSamples; i++)
                    {
                        pcmWriter.Write((short)0);
                    }
                }
            }

            return WrapPcmAsWav(pcmStream.ToArray(), SampleRateHz, channels: 1, bitsPerSample: 16);
        }

        private static byte[] WrapPcmAsWav(byte[] pcmData, int sampleRate, short channels, short bitsPerSample)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + pcmData.Length);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(pcmData.Length);
            writer.Write(pcmData);

            return stream.ToArray();
        }
    }
}
