using System;
using System.Collections.Generic;
using System.Linq;

namespace HebrewTaskbarWidget.Models
{
    /// <summary>צד ההצמדה של הוידג'ט ביחס לתצוגת התאריך/שעה המקורית של Windows, וכן צד המרחק המותאם אישית מקצה שורת המשימות.</summary>
    public enum WidgetAttachSide
    {
        /// <summary>ברירת המחדל: הוידג'ט מוצמד מימין לתצוגת התאריך/שעה המקורית (או ממוקם ביחס לקצה הימני, במצב מרחק מותאם אישית).</summary>
        Right,
        Left,
    }

    /// <summary>
    /// היכן ממקמים את הוידג'ט ביחס לשורת המשימות/מגש המערכת.
    /// </summary>
    public enum WidgetPositionMode
    {
        /// <summary>
        /// ברירת המחדל: צמוד לכפתור "^" (הצג סמלים מוסתרים) במגש המערכת -
        /// בדיוק כמו BatteryBar. נע אוטומטית יחד עם הכפתור כשמראים/מסתירים
        /// סמלים (הכפתור עצמו זז כשאזור הסמלים הגלויים מתרחב/מתכווץ), כי
        /// המיקום נבדק ומעודכן מחדש כל חצי שנייה (ראו UpdatePosition).
        /// </summary>
        ChevronAttached,

        /// <summary>מרחק קבוע (בפיקסלים) מקצה שורת המשימות (ימין/שמאל), בתוך גובה שורת המשימות עצמה - ללא תלות במיקום הכפתור/השעון.</summary>
        CustomEdgeOffset,

        /// <summary>מיקום חופשי לגמרי, שנקבע ע"י המשתמש בגרירה (Ctrl + גרירת עכבר). נשמר כקואורדינטות מסך מוחלטות.</summary>
        FreeDrag,
    }

    /// <summary>מיקום חלק "חג ומועד" בתוך הוידג'ט.</summary>
    public enum HolidayPanelPosition
    {
        /// <summary>בצד הימני ביותר של הוידג'ט.</summary>
        FarRight,

        /// <summary>בצד השמאלי ביותר של הוידג'ט.</summary>
        FarLeft,

        /// <summary>בין התאריך העברי ללועזי (אם השעון הלועזי אינו מוצג, מתנהג כמו הצמדה לתאריך העברי מהצד שבו היה אמור להיות הלועזי).</summary>
        BetweenHebrewAndGregorian,
    }

    /// <summary>מיקום תצוגת המידע החופשית מעל שולחן העבודה (חלק 3).</summary>
    public enum OverlayPosition
    {
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Custom,
    }

    /// <summary>
    /// היכן ההודעה הצפה (ToastNotificationWindow) תוצג על המסך.
    /// AboveWidget (ברירת המחדל) - עוקבת אחרי מיקומו האמיתי הנוכחי של
    /// הוידג'ט על המסך, בין אם הוא נגרר למקום מותאם אישית ובין אם הוא
    /// בהיצמדות אוטומטית לכפתור "^"; שאר האפשרויות (Center/TopLeft/וכו')
    /// קבועות ביחס למסך כולו, בדיוק כמו OverlayPosition, ולא תלויות במיקום
    /// הוידג'ט בכלל.
    /// </summary>
    public enum ToastPositionMode
    {
        AboveWidget,
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Custom,
    }

    /// <summary>מתי "היום" מבחינת התאריך העברי המוצג מתקדם ליום הבא (ראו Services/HebrewDayRolloverService).</summary>
    public enum HebrewDayChangeMode
    {
        /// <summary>ברירת המחדל: בחצות הלילה (00:00), כמו התאריך הלועזי הרגיל.</summary>
        Midnight,

        /// <summary>עם שקיעת החמה של אותו יום.</summary>
        AtSunset,

        /// <summary>עם צאת הכוכבים של אותו יום (ברירת המחדל המבוססת-מעלות, או הערך שהוגדר בהגדרות - ראו AppSettings.TzeitHakochavimMinutesAfterSunset).</summary>
        AtTzeitHakochavim,
    }

    /// <summary>איך לוח הזמנים (ZmanimPopup) ממוקם אופקית ביחס לוידג'ט כשהוא נפתח.</summary>
    public enum ZmanimPopupAlignment
    {
        /// <summary>
        /// ברירת המחדל: בדרך כלל ממורכז מעל הוידג'ט - אך אם הוידג'ט צמוד
        /// לקצה מסך (ימין/שמאל), מיושר לקו אחד עם אותו קצה במקום. ראו
        /// MainWindow._cachedEdgeSnapAlignment.
        /// </summary>
        Auto,

        /// <summary>ממורכז מעל הוידג'ט - תמיד, ללא תלות במיקום הוידג'ט.</summary>
        Center,

        /// <summary>קצה ימין של הלוח מיושר בקו אחד עם קצה ימין של הוידג'ט.</summary>
        RightEdge,

        /// <summary>קצה שמאל של הלוח מיושר בקו אחד עם קצה שמאל של הוידג'ט.</summary>
        LeftEdge,
    }

    /// <summary>
    /// התאמה אישית (גופן/גודל/צבע) לפריט בודד בתצוגה החופשית מעל שולחן
    /// העבודה - חלק מ"הגדרות מתקדמות" (מתקפל, סגור כברירת מחדל). כאשר
    /// UseCustomStyle = false (ברירת המחדל), הפריט משתמש בגופן/גודל/צבע
    /// המשותפים של כל התצוגה (OverlayFontFamilyName/OverlayFontSize/
    /// OverlayTextColorHex) - בדיוק כמו ההתנהגות הקודמת, לפני התוספת הזו.
    /// </summary>
    public sealed class OverlayItemStyle
    {
        public bool UseCustomStyle { get; set; } = false;
        public string FontFamilyName { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 26.0;
        public string ColorHex { get; set; } = "#FFFFFF";
    }

    /// <summary>
    /// שלוש אפשרויות לניהול הפעלה מחדש אוטומטית של Explorer בעליית התוכנה
    /// (רלוונטי רק כאשר גם ההסתרה החיה וגם "צמצום הרווח הריק" מופעלים -
    /// ראו MainWindow.MainWindow_ContentRendered ו-WindowsClockVisibilityService).
    /// בכל שלושת המצבים ממשיך לפעול אותו "זיהוי חכם" הקיים כבר (IsAutoStartLaunch
    /// / NeedsExplorerRestart) - כלומר לעולם לא מפעילים מחדש (ולא שואלים) אם
    /// זה בכלל לא נחוץ (למשל Explorer כבר עלה מחדש מאז, או שהתוכנה עצמה
    /// עולה כחלק מעליית Windows).
    /// </summary>
    public enum ExplorerAutoLaunchMode
    {
        /// <summary>ברירת המחדל: כשנדרשת הפעלה מחדש, שואלים אישור מהמשתמש בכל פעם.</summary>
        AskEachTime,

        /// <summary>לעולם לא לשאול ולא להפעיל מחדש אוטומטית - המשתמש יכול תמיד להפעיל ידנית (ראו המתג הצמוד בפאנל ההגדרות).</summary>
        Never,

        /// <summary>הפעלה מחדש אוטומטית לגמרי, בלי לשאול.</summary>
        Automatic,
    }

    /// <summary>מקור הצליל הכללי להתראות (ראו AppSettings.NotificationSoundSource).</summary>
    public enum NotificationSoundSourceMode
    {
        /// <summary>ברירת המחדל: צליל קבוע אחד מתוך רשימה סגורה (מזוהה, לא תלוי בערכת השמע של Windows).</summary>
        Fixed,

        /// <summary>הכרזה קולית חכמה, מורכבת מכמה קבצי שמע קצרים המושמעים ברצף (שם הזמן + משך הזמן במילים) - ראו Services/VoiceAnnouncementService.</summary>
        Voice,

        /// <summary>קובץ שמע יחיד שנבחר ע"י המשתמש מהמחשב (עיון בתיקיה).</summary>
        CustomFile,
    }

    /// <summary>
    /// הגדרת התראה עבור זמן הלכתי בודד, כפי שמוצג בשורה המתאימה בלשונית
    /// "התראות" (הרשימה הראשית - זמן, האם מופעל, כמה דקות לפני, וצליל מיוחד
    /// אופציונלי לזמן הזה בלבד שגובר על הצליל הכללי).
    /// </summary>
    public sealed class ZmanNotificationRule
    {
        public string ZmanName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = false;
        public int MinutesBefore { get; set; } = 10;

        /// <summary>נתיב לקובץ צליל מיוחד לזמן הזה בלבד (עיון בתיקיה) - גובר על ההגדרה הכללית. null/ריק = לא הוגדר קובץ מיוחד.</summary>
        public string? SoundOverridePath { get; set; }

        /// <summary>צליל קבוע מיוחד לזמן הזה בלבד (מזהה פנימי, כמו NotificationFixedSoundName) - רלוונטי רק כאשר SoundOverridePath ריק. null = אין צליל מיוחד, משתמשים בהגדרה הכללית.</summary>
        public string? SoundOverrideFixedName { get; set; }

        /// <summary>true = הזמן הזה תמיד ישתמש בהכרזה קולית (עם ערכת הקול הכללית, NotificationVoiceKitFolderName), ללא תלות במקור הצליל הכללי שנבחר - גובר על SoundOverridePath/SoundOverrideFixedName אם שניהם ריקים. false (ברירת המחדל) = לא נבחרה הכרזה קולית מיוחדת לזמן הזה.</summary>
        public bool SoundOverrideUseVoice { get; set; } = false;
    }

    /// <summary>
    /// התאמה אישית לזמן בודד (נערכת מהחלונית הקטנה שנפתחת בלחיצה על שם
    /// הזמן ב"אילו זמנים להציג", לשונית "מיקום וזמנים") - שם מותאם אישית
    /// ו/או שיטת חישוב שונה מהשיטה הכללית (AppSettings.DefaultZmanCalculationMethod).
    /// מפתח: BaseZmanName - השם הקנוני של הזמן (כמו ZmanimCalendar.NameAlotHaShachar),
    /// **לא** משתנה גם אם CustomName מוגדר - כל שאר האפליקציה (כללי התראה,
    /// רשימת "אילו זמנים להציג", HebrewDayRolloverService) ממשיכה להתייחס
    /// לזמן לפי השם הקנוני שלו; רק התצוגה בפועל (בלוח הזמנים, בהתראות)
    /// מציגה את CustomName אם הוגדר - ראו ZmanEntry.Key מול ZmanEntry.DisplayName.
    /// </summary>
    public sealed class ZmanCustomization
    {
        public string BaseZmanName { get; set; } = string.Empty;

        /// <summary>null = השם הקנוני הרגיל (ללא התאמה אישית).</summary>
        public string? CustomName { get; set; }

        /// <summary>null = משתמש בשיטה הכללית (AppSettings.DefaultZmanCalculationMethod). לא רלוונטי ל"הדלקת נרות" (אין לו שיטת חישוב בכלל).</summary>
        public ZmanCalculationMethod? MethodOverride { get; set; }
    }

    /// <summary>
    /// שורת "זמן כפול" - אותו זמן בסיסי (BaseZmanName) מוצג פעם נוספת ברשימה,
    /// עם שיטת חישוב שונה מהשורה ה"ראשית" של אותו זמן (ולכן חייב שם מותאם
    /// אישית משלו, כדי להבדיל בין השתיים ברשימה/בהתראות) - ראו "הוסף שורת
    /// זמן כפולה" בחלונית עריכת הזמן. Id הוא המפתח היציב לזיהוי השורה הזו
    /// (ראו ZmanEntry.Key) - לא תלוי בשם המוצג, כדי ששינוי שם לא "ישבור"
    /// כללי התראה/נראות קיימים עבורה.
    /// </summary>
    public sealed class ZmanDuplicateRow
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string BaseZmanName { get; set; } = string.Empty;
        public string CustomName { get; set; } = string.Empty;
        public ZmanCalculationMethod Method { get; set; }
    }

    /// <summary>
    /// כלל התראה "מתקדם" - מאפשר כמה התראות במקביל על אותו זמן (למשל 40,
    /// 30 ו-5 דקות לפני אותו זמן), כל אחד עם הגדרות תצוגה/צליל עצמאיות
    /// לגמרי משלו (ולא תלוי בהגדרה הכללית או ברשימה הראשית).
    /// </summary>
    public sealed class AdvancedNotificationRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ZmanName { get; set; } = string.Empty;
        public int MinutesBefore { get; set; } = 10;
        public bool Enabled { get; set; } = true;
        public bool ShowPopup { get; set; } = true;

        /// <summary>כמה שניות ההודעה הצפה של ההתראה הזו תוצג לפני שהיא נעלמת אוטומטית.</summary>
        public double ToastDurationSeconds { get; set; } = 15.0;

        /// <summary>רקע ההודעה הצפה של ההתראה הזו - כהה (ברירת מחדל) או בהיר.</summary>
        public bool ToastDarkBackground { get; set; } = true;

        public bool PlaySound { get; set; } = false;

        /// <summary>מקור הצליל להתראה הזו - הקראה קולית / צליל קבוע / קובץ מהמחשב.</summary>
        public NotificationSoundSourceMode SoundSource { get; set; } = NotificationSoundSourceMode.Voice;

        /// <summary>נתיב לקובץ צליל שנבחר מהמחשב - רלוונטי רק כאשר SoundSource = CustomFile.</summary>
        public string? SoundPath { get; set; }
        public string FixedSoundName { get; set; } = "Asterisk";

        /// <summary>שם תיקיית ערכת הקול הנבחרת - רלוונטי רק כאשר SoundSource = Voice.</summary>
        public string? VoiceKitFolderName { get; set; }
    }

    /// <summary>
    /// שיטת חישוב הזמנים (עלות השחר/צאת הכוכבים, ולכן גם הזמנים המבוססים
    /// עליהם - סוף זמן ק"ש/תפילה מג"א): שתי שיטות נפוצות ומקובלות, אינן
    /// פוסקות הלכה למעשה.
    /// </summary>
    public enum ZmanCalculationMethod
    {
        /// <summary>
        /// ברירת המחדל ההיסטורית של התוכנה: עלות השחר/צאת הכוכבים לפי זווית
        /// שקיעת החמה מתחת לאופק (16.1°/8.5° בהתאמה, גישה גיאומטרית-אסטרונומית
        /// טהורה).
        /// </summary>
        Gra,

        /// <summary>
        /// מבוסס על ספריית KosherJava Zmanim (חישוב זמני היום בהלכה, מאת
        /// אליהו הרשפלד) - דרך ה-port ל-.NET של Yitzchok/Zmanim (LGPL 2.1,
        /// https://github.com/Yitzchok/Zmanim). עלות השחר/צאת הכוכבים
        /// מחושבים לפי "72 דקות זמניות" (גר"א/בעל התניא) - לא 72 דקות שעון
        /// קבועות, אלא 1.2 שעות זמניות (יחסיות לאורך היום בפועל) לפני
        /// הנץ/אחרי השקיעה (ברמת פני הים). הנוסחה המדוייקת מ-KosherJava:
        /// Alos72 = SeaLevelSunrise − (ShaahZmanisGra × 1.2), כאשר
        /// ShaahZmanisGra = (SeaLevelSunset − SeaLevelSunrise) ÷ 12 - ראו
        /// ComplexZmanimCalendar.GetAlos72 בספריית המקור. Tzais72 המקביל
        /// (סימטרי): SeaLevelSunset + (ShaahZmanisGra × 1.2).
        /// </summary>
        Mga72Zmaniyos,
    }

    /// <summary>
    /// כלל ההגדרות הניתנות לשמירה של האפליקציה - נשמר כ-JSON תחת
    /// %AppData%\HebrewTaskbarWidget\settings.json ונטען מחדש עם עליית התוכנה.
    /// </summary>
    public sealed class AppSettings
    {
        // --- הוידג'ט בשורת המשימות ---
        /// <summary>מתג-על: האם להציג את הוידג'ט בכלל בשורת המשימות. ברירת מחדל: מוצג. כשכבוי, שאר התוכנה (למשל התראות זמנים) ממשיכה לפעול כרגיל - רק התצוגה החזותית מוסתרת.</summary>
        public bool ShowWidget { get; set; } = true;
        public WidgetPositionMode PositionMode { get; set; } = WidgetPositionMode.ChevronAttached;
        /// <summary>איך לוח הזמנים ממוקם אופקית ביחס לוידג'ט כשהוא נפתח (מיקום וזמנים, לשונית "כללי").</summary>
        public ZmanimPopupAlignment ZmanimPopupAlignment { get; set; } = ZmanimPopupAlignment.Auto;

        // --- מצב "מרחק מותאם אישית מקצה שורת המשימות" (WidgetPositionMode.CustomEdgeOffset) ---
        public WidgetAttachSide CustomOffsetSide { get; set; } = WidgetAttachSide.Left;
        public double CustomOffsetPixels { get; set; } = 250.0;

        /// <summary>נועל את מיקום הוידג'ט בשורת המשימות - מונע גרירה (Ctrl + גרירת עכבר) כשמסומן. כבוי כברירת מחדל.</summary>
        public bool LockWidgetPosition { get; set; } = false;

        /// <summary>נועל את מיקום תצוגת שולחן העבודה החופשית - מונע גרירה (Ctrl + גרירת עכבר) כשמסומן. כבוי כברירת מחדל.</summary>
        public bool LockOverlayPosition { get; set; } = false;

        // --- מצב "מיקום חופשי" שנקבע ע"י גרירה עם Ctrl (WidgetPositionMode.FreeDrag) ---
        // נשמר כ-null עד שהמשתמש גורר בפועל בפעם הראשונה.
        public double? FreeDragLeft { get; set; }
        public double? FreeDragTop { get; set; }

        /// <summary>
        /// ShowTopLine/ShowBottomLine/SwapLineOrder: אין יותר UI לעריכה
        /// בפאנל ההגדרות (הוסר לגמרי - "שורות מוצגות" בלשונית "וידג'ט") -
        /// נשארו רק כערכי-נתונים לתאימות לאחור (למשל למי ששינה אותם לפני
        /// ההסרה). ברירת המחדל (שתי השורות מוצגות, בסדר הרגיל) היא היחידה
        /// שבפועל ניתנת להשגה כרגע דרך הממשק.
        /// </summary>
        public bool ShowTopLine { get; set; } = true;
        public bool ShowBottomLine { get; set; } = true;
        public bool SwapLineOrder { get; set; } = false;
        public bool UseCustomFont { get; set; } = false;
        public string? FontFamilyName { get; set; }
        public double FontSize { get; set; } = 12.0;
        public bool UseCustomTextColor { get; set; } = false;
        public string CustomTextColorHex { get; set; } = "#FFFFFF";

        // --- מראה פאנל ההגדרות עצמו (לא קשור לצבע קוביית הוידג'ט) ---
        // ברירת המחדל: רקע בהיר, כדי שכל השורות/הלשוניות יהיו קריאות בבירור.
        // ניתן לעבור לרקע כהה ולבחור צבע מותאם אישית עבורו.
        public bool SettingsPanelDarkMode { get; set; } = false;
        public string SettingsPanelDarkColorHex { get; set; } = "#1B1C1F";

        // --- הפעלה אוטומטית עם עליית Windows ---
        public bool StartWithWindows { get; set; } = true;

        // --- עדכוני תוכנה (GitHub Releases) ---
        /// <summary>האם לבדוק אוטומטית פעם ביום אם קיימת גרסה חדשה (בשקט, ברקע).</summary>
        public bool CheckForUpdates { get; set; } = true;
        /// <summary>מתי בוצעה בדיקת העדכונים האחרונה (UTC) - null אם עדיין לא בוצעה בדיקה מעולם.</summary>
        public DateTime? LastUpdateCheckUtc { get; set; }

        // --- הסתרת תצוגת התאריך/שעה המקורית של Windows (best-effort, ראו
        // הערת מגבלות ב-WindowsClockVisibilityService) ---
        public bool HideWindowsClock { get; set; } = false;
        /// <summary>מצב "מלא" - מצמצם גם את הרווח הריק שההסתרה החיה בלבד משאירה, ע"י שילוב מדיניות Registry + הפעלה מחדש חד-פעמית של Explorer (ראו WindowsClockVisibilityService.ApplyFullEffectWithRestart).</summary>
        public bool HideWindowsClockReduceGap { get; set; } = false;

        /// <summary>כאשר גם ההסתרה החיה וגם צמצום הרווח מופעלים - כיצד לנהוג לגבי הפעלה מחדש של Explorer בכל עליית התוכנה: שאל בכל פעם (ברירת המחדל) / לעולם אל תשאל / הפעל אוטומטית תמיד. ראו ExplorerAutoLaunchMode.</summary>
        public ExplorerAutoLaunchMode ExplorerAutoLaunchMode { get; set; } = ExplorerAutoLaunchMode.AskEachTime;

        /// <summary>
        /// שדה ישן/מיושן - נשמר אך ורק לצורך מיגרציה חד-פעמית (ראו
        /// SettingsService.Load) מקובצי הגדרות שנשמרו לפני שהוחלף בתפריט
        /// התלת-מצבי ExplorerAutoLaunchMode למעלה. אין להשתמש בו בקוד חדש.
        /// </summary>
        public bool? AutoRestartExplorerOnLaunch { get; set; }

        /// <summary>
        /// זמן העלייה (UTC) של תהליך explorer.exe שהיה פעיל בפעם האחרונה
        /// שהתוכנה בדקה/ביצעה הפעלה מחדש שלו - משמש לזיהוי חכם אם Explorer
        /// כבר עלה מחדש מאז (בעצמו, למשל בעקבות הפעלה מחדש של המחשב, או
        /// הפעלה מחדש ידנית) - ואם כן, אין צורך להפעיל אותו מחדש שוב, כי
        /// הוא כבר קרא את ערך המדיניות (HideClock) בעלייתו החדשה. ראו
        /// Services/WindowsClockVisibilityService.NeedsExplorerRestart.
        /// </summary>
        public DateTime? LastKnownExplorerStartTimeUtc { get; set; }

        // --- שעון/תאריך לועזי המשולב בתוך הוידג'ט עצמו (מחקה את התצוגה
        // המקורית של Windows), לצד השורות העבריות - זמין תמיד, ללא תלות
        // בהצלחת ההסתרה של התצוגה המקורית. ---
        public bool ShowGregorianClock { get; set; } = false;
        /// <summary>לאיזה צד ממוקם השעון הלועזי ביחס לשורות העבריות (בתפיסה חזותית).</summary>
        public WidgetAttachSide GregorianClockSide { get; set; } = WidgetAttachSide.Left;
        /// <summary>האם להציג קו מפריד דק בין התאריך העברי לתאריך/שעון הלועזי. ברירת מחדל: כן.</summary>
        public bool ShowGregorianSeparator { get; set; } = true;
        /// <summary>
        /// פורמט תצוגת התאריך הלועזי (בוידג'ט) - תבנית .NET DateTime סטנדרטית
        /// (ראו אפשרויות ב-SettingsWindow.xaml, GregorianDateFormatComboBox).
        /// ברירת המחדל "dd/MM/yyyy" מציגה למשל "05/08/2026" (עם אפסים
        /// מובילים) - בניגוד לפורמט "d" הקצר של האזור (Region) שהיה בשימוש
        /// קודם, שיכול להיראות שונה מאוד בין אזורים (כולל בלי אפסים
        /// מובילים, למשל "5.8.2026"). תמיד מיושם עם AppTimeService.GregorianDisplayCulture
        /// (לוח שנה לועזי נעול) - ראו הערה מפורטת שם.
        /// </summary>
        public string GregorianDateFormat { get; set; } = "dd/MM/yyyy";


        // --- חלק "חג ומועד" - מוצג רק בימים שיש בהם חג/מועד, לא מוצג
        // כברירת מחדל (המשתמש צריך להפעיל אותו במפורש). ---
        public bool ShowHolidayPanel { get; set; } = false;
        /// <summary>לאיזה צד ממוקם חלק "חג ומועד" ביחס לשאר הוידג'ט (בתפיסה חזותית) - ברירת מחדל: הצד הימני ביותר.</summary>
        public HolidayPanelPosition HolidayPanelSide { get; set; } = HolidayPanelPosition.FarRight;
        /// <summary>האם להציג קו מפריד דק לצד חלק "חג ומועד". ברירת מחדל: כן.</summary>
        public bool ShowHolidaySeparator { get; set; } = true;

        // --- רקע קוביית הוידג'ט (ברירת מחדל: שקוף לגמרי, כמו קודם) ---
        public bool UseCustomBackgroundColor { get; set; } = false;
        public string WidgetBackgroundColorHex { get; set; } = "#202020";
        /// <summary>שקיפות הרקע: 0 = שקוף לגמרי, 1 = אטום לגמרי.</summary>
        public double WidgetBackgroundOpacity { get; set; } = 0.55;

        // --- קו מתאר (מסגרת) לקוביית הוידג'ט (ברירת מחדל: כבוי) ---
        public bool UseWidgetBorder { get; set; } = false;
        public string WidgetBorderColorHex { get; set; } = "#FFFFFF";
        public double WidgetBorderThickness { get; set; } = 1.0;

        // --- מיקום גיאוגרפי לחישוב זמני היום ---
        public string LocationName { get; set; } = "ירושלים";
        public double Latitude { get; set; } = 31.7683;
        public double Longitude { get; set; } = 35.2137;
        public double ElevationMeters { get; set; } = 754;
        public string TimeZoneId { get; set; } = "Israel Standard Time";

        /// <summary>כמה דקות לפני השקיעה זמן "הדלקת נרות" (מוצג רק בערבי שבת/חג רלוונטיים - ראו HolidayService.IsErevCandleLighting).</summary>
        public int CandleLightingMinutesBeforeSunset { get; set; } = 40;

        /// <summary>
        /// כמה דקות אחרי השקיעה זמן "צאת הכוכבים" - null (ברירת המחדל) פירושו
        /// להמשיך להשתמש בחישוב המבוסס-מעלות הרגיל (8.5°, ראו ZmanimCalendar) -
        /// רק אם המשתמש הזין ערך מפורש בהגדרות ("מיקום וזמנים"), הוא זה
        /// שמוצג במקום. משפיע רק על הזמן *המוצג* בשם הזה - לא על חישובים
        /// פנימיים אחרים (כמו סוף זמן ק"ש/תפילה מג"א), שממשיכים תמיד
        /// להתבסס על החישוב המבוסס-מעלות המקורי.
        /// </summary>
        public int? TzeitHakochavimMinutesAfterSunset { get; set; }

        /// <summary>
        /// שיטת החישוב הכללית לעלות השחר/צאת הכוכבים (ולזמנים התלויים בהם -
        /// סוף זמן ק"ש/תפילה מג"א) - ראו ZmanCalculationMethod למעלה.
        /// </summary>
        public ZmanCalculationMethod DefaultZmanCalculationMethod { get; set; } = ZmanCalculationMethod.Gra;

        /// <summary>התאמות אישיות (שם/שיטת חישוב) לזמנים בודדים - ראו ZmanCustomization.</summary>
        public List<ZmanCustomization> ZmanCustomizations { get; set; } = new();

        /// <summary>שורות "זמן כפול" - ראו ZmanDuplicateRow.</summary>
        public List<ZmanDuplicateRow> ZmanDuplicateRows { get; set; } = new();

        /// <summary>
        /// אילו זמנים (מתוך ZmanimCalendar.AllZmanNames) יוצגו בלוח הזמנים
        /// (הפופ-אפ) וברשימות הבחירה בלשונית התראות - null = כולם מוצגים
        /// (ברירת המחדל בפועל; לא ממלאים רשימה מפורשת של "הכל" כדי שזמנים
        /// חדשים שיתווספו בעתיד יופיעו אוטומטית, בלי לדרוש עדכון הגדרות קיימות).
        /// </summary>
        public List<string>? VisibleZmanNames { get; set; }

        /// <summary>האם זמן נתון (לפי שמו הקנוני) מוגדר להצגה - כברירת מחדל (VisibleZmanNames == null) כולם מוצגים.</summary>
        /// <summary>האם זמן נתון (לפי ה-Key הקנוני שלו - ראו ZmanEntry.Key) מוגדר להצגה - כברירת מחדל (VisibleZmanNames == null) כולם מוצגים. שורות "זמן כפול" (ZmanDuplicateRows) נשלטות בדיוק כמו כל שורה אחרת, לפי ה-Id הייחודי שלהן.</summary>
        public bool IsZmanVisible(string zmanKey) => VisibleZmanNames is null || VisibleZmanNames.Contains(zmanKey);

        // --- תאריך ושעה (ראו Services/AppTimeService) ---
        /// <summary>אם מופעל, "עכשיו" בכל האפליקציה נלקח מהתאריך/שעה הידניים למטה (עם המשך זרימת זמן טבעית מרגע ההגדרה) ולא מהתאריך/שעה של המחשב.</summary>
        public bool UseManualDateTime { get; set; } = false;
        /// <summary>התאריך/שעה שנקבעו ידנית (Ticks, DateTimeKind.Unspecified) - "עוגן" שממנו הזמן ממשיך לזרום קדימה כרגיל.</summary>
        public long ManualDateTimeBaseTicks { get; set; }
        /// <summary>הזמן (UTC, Ticks) שבו העוגן שלמעלה נקבע בפועל - משמש לחישוב כמה זמן "אמיתי" עבר מאז, כדי שהזמן הידני ימשיך לתקתק קדימה ולא יישאר קפוא.</summary>
        public long ManualDateTimeSetAtUtcTicks { get; set; }
        /// <summary>false (ברירת מחדל) = פריסת 24 שעות; true = פריסת 12 שעות (AM/PM).</summary>
        public bool Use12HourFormat { get; set; } = false;
        /// <summary>האם להציג שניות בתצוגות השעה (וידג'ט/שולחן עבודה) - לא רלוונטי ללוח הזמנים ולהתראות, שם תמיד מוצגות דקות בלבד.</summary>
        public bool ShowSecondsInTime { get; set; } = false;

        /// <summary>מתי "היום" מבחינת התאריך העברי המוצג בפועל מתקדם ליום העברי הבא - ברירת מחדל: חצות הלילה (כמו התאריך הלועזי). ראו HebrewDayRolloverService.</summary>
        public HebrewDayChangeMode HebrewDayChangeMode { get; set; } = HebrewDayChangeMode.Midnight;

        // --- התראות על זמני היום ---
        public bool NotificationsEnabled { get; set; } = false;

        /// <summary>
        /// ברירת המחדל: מסומן. כשמסומן, כל ההתראות על זמני היום מושתקות
        /// בימים שהם עצמם שבת, או אחד מהחגים שיש לפניהם זמן "הדלקת נרות"
        /// (ראו HolidayService.IsErevCandleLighting) - כלומר לא כולל חנוכה,
        /// פורים, ראש חודש וכדומה, שממשיכים לקבל התראות רגיל בהם. יום שישי/
        /// ערב חג עצמו (כולל התראת "הדלקת נרות") **אינו** מושתק - רק היום
        /// שבו כבר "בתוך" השבת/החג.
        /// </summary>
        public bool DisableNotificationsOnShabbatAndChagim { get; set; } = true;

        /// <summary>האם להציג הודעה קופצת (צפה מעל הוידג'ט). ניתן לסמן זאת ו/או השמעת צליל, אחד או שניהם.</summary>
        public bool NotificationShowPopup { get; set; } = true;

        /// <summary>כמה שניות ההודעה הצפה תוצג לפני שהיא נעלמת אוטומטית.</summary>
        public double NotificationToastDurationSeconds { get; set; } = 15.0;

        /// <summary>כמה דקות "הנודניק" (כפתור הפעמון בהודעה הצפה) דוחה את התזכורת קדימה.</summary>
        public int SnoozeDurationMinutes { get; set; } = 2;

        /// <summary>רקע ההודעה הצפה - כהה (ברירת מחדל) או בהיר.</summary>
        public bool NotificationToastDarkBackground { get; set; } = true;

        /// <summary>היכן ההודעה הצפה תוצג על המסך - ראו ToastPositionMode.</summary>
        public ToastPositionMode NotificationToastPositionMode { get; set; } = ToastPositionMode.AboveWidget;

        /// <summary>מרחק (בפיקסלים) מהקצה השמאלי של המסך, למיקום מותאם אישית של ההודעה הצפה - רלוונטי רק כאשר NotificationToastPositionMode = Custom.</summary>
        public double NotificationToastCustomX { get; set; } = 100;

        /// <summary>מרחק (בפיקסלים) מהקצה העליון של המסך, למיקום מותאם אישית של ההודעה הצפה - רלוונטי רק כאשר NotificationToastPositionMode = Custom.</summary>
        public double NotificationToastCustomY { get; set; } = 100;

        /// <summary>האם להשמיע צליל (הגדרה כללית - חלה על כל הזמנים שאין להם צליל מיוחד משלהם).</summary>
        public bool NotificationPlaySound { get; set; } = false;

        /// <summary>מקור הצליל הכללי - צליל קבוע מתוך רשימה, הכרזה קולית חכמה, או קובץ שמע שנבחר מהמחשב. ראו NotificationSoundSourceMode.</summary>
        public NotificationSoundSourceMode NotificationSoundSource { get; set; } = NotificationSoundSourceMode.Voice;

        /// <summary>שם הצליל הקבוע הנבחר (מזהה פנימי - ראו FixedSoundKeys ב-SettingsWindow.xaml.cs), רלוונטי רק כאשר NotificationSoundSource = Fixed.</summary>
        public string NotificationFixedSoundName { get; set; } = "Asterisk";

        /// <summary>נתיב לקובץ צליל כללי שנבחר מהמחשב (עיון בתיקיה) - רלוונטי רק כאשר NotificationSoundSource = CustomFile.</summary>
        public string? NotificationCustomSoundPath { get; set; }

        /// <summary>שם תיקיית ערכת הקול הנבחרת (תת-תיקייה בתוך VoiceAnnouncements, למשל "קול-א") - רלוונטי רק כאשר NotificationSoundSource = Voice. null = לא נבחרה ערכה (אין הכרזה קולית).</summary>
        public string? NotificationVoiceKitFolderName { get; set; }

        /// <summary>
        /// רשימת ההגדרות הראשית, שורה לכל זמן הלכתי אפשרי: האם מופעל, כמה
        /// דקות לפני יתריע, וצליל מיוחד אופציונלי לזמן הזה בלבד. כברירת
        /// מחדל, 4 הזמנים הנפוצים ביותר מופעלים עם 10 דקות לפני.
        /// </summary>
        public List<ZmanNotificationRule> ZmanNotificationRules { get; set; } = new()
        {
            new() { ZmanName = HebrewTaskbarWidget.Services.ZmanimCalendar.NameNetz, Enabled = true, MinutesBefore = 10 },
            new() { ZmanName = HebrewTaskbarWidget.Services.ZmanimCalendar.NameMinchaKetana, Enabled = true, MinutesBefore = 10 },
            new() { ZmanName = HebrewTaskbarWidget.Services.ZmanimCalendar.NameShkia, Enabled = true, MinutesBefore = 10 },
            new() { ZmanName = HebrewTaskbarWidget.Services.ZmanimCalendar.NameTzeitHakochavim, Enabled = true, MinutesBefore = 10 },
        };

        /// <summary>
        /// "הגדרות מתקדמות": כללי התראה נוספים, עצמאיים לגמרי מהרשימה
        /// הראשית - מאפשרים כמה התראות במקביל על אותו זמן (למשל 40, 30
        /// ו-5 דקות לפני אותו זמן), כל אחד עם הגדרות תצוגה/צליל משלו.
        /// </summary>
        public List<AdvancedNotificationRule> AdvancedNotificationRules { get; set; } = new();

        // --- לוח הזמנים (ZmanimPopup) ---
        /// <summary>true (ברירת מחדל) = רקע כהה; false = רקע בהיר. מוחלף ע"י הכפתור שמש/ירח שליד סמל ההגדרות בלוח הזמנים.</summary>
        public bool ZmanimPopupDarkMode { get; set; } = true;

        // --- תצוגה חופשית מעל שולחן העבודה ---
        public bool OverlayEnabled { get; set; } = false;
        public bool OverlayShowTime { get; set; } = true;
        public bool OverlayShowGregorianDate { get; set; } = true;
        public bool OverlayShowHebrewDate { get; set; } = true;
        public bool OverlayShowDayAndParasha { get; set; } = true;
        /// <summary>האם להציג חג/מועד עברי (אם יש כזה בתאריך הנוכחי). לא כולל את יום הזיכרון ויום העצמאות - ראו HolidayService.</summary>
        public bool OverlayShowHoliday { get; set; } = true;
        public OverlayPosition OverlayPositionMode { get; set; } = OverlayPosition.Center;
        public double OverlayCustomX { get; set; } = 100;
        public double OverlayCustomY { get; set; } = 100;
        public string OverlayFontFamilyName { get; set; } = "Segoe UI";
        public double OverlayFontSize { get; set; } = 26.0;
        public string OverlayTextColorHex { get; set; } = "#FFFFFF";
        public bool OverlayAlwaysOnTop { get; set; } = false;

        // --- תצוגה חופשית: הגדרות מתקדמות (מתקפל, סגור כברירת מחדל) -
        // התאמה אישית נפרדת (גופן/גודל/צבע) לכל אחד מ-4 הפריטים המוצגים.
        // כברירת מחדל כולם UseCustomStyle=false, כלומר משתמשים בגופן/גודל/
        // צבע המשותפים למעלה - בדיוק כמו ההתנהגות לפני התוספת הזו.
        public OverlayItemStyle OverlayTimeStyle { get; set; } = new();
        // "יום ופרשת שבוע" ו"תאריך לועזי": ברירת המחדל של גודל הטקסט
        // (בעת הפעלת "התאמה אישית לפריט זה") היא 20 ו-19 בהתאמה - נקבע
        // במפורש כאן, ולא רק דרך ברירת המחדל הכללית (26) של OverlayItemStyle.
        public OverlayItemStyle OverlayGregorianDateStyle { get; set; } = new() { FontSize = 19.0 };
        public OverlayItemStyle OverlayHebrewDateStyle { get; set; } = new();
        public OverlayItemStyle OverlayDayParashaStyle { get; set; } = new() { FontSize = 20.0 };
        public OverlayItemStyle OverlayHolidayStyle { get; set; } = new();

        /// <summary>
        /// סדר הצגת הפריטים בתצוגה החופשית מעל שולחן העבודה (מי מוצג מעל מי,
        /// מלמעלה למטה) - חלק מ"הגדרות מתקדמות". המפתחות התקינים הם בדיוק:
        /// "DayParasha", "Holiday", "HebrewDate", "GregorianDate", "Time".
        /// פריט שלא מופיע ברשימה (למשל בהגדרות ישנות שנשמרו לפני התוספת) יתווסף
        /// בסוף אוטומטית - ראו DesktopOverlayWindow.ApplyItemOrder.
        /// </summary>
        public List<string> OverlayItemOrder { get; set; } = new()
        {
            "Time", "DayParasha", "HebrewDate", "GregorianDate", "Holiday",
        };
    }
}
