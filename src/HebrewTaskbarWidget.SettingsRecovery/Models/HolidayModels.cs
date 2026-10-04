namespace HebrewTaskbarWidget.Models
{
    /// <summary>סוג המועד - קובע אם הוא מוצג (לפי ההגדרות) ומה סדר העדיפות שלו.</summary>
    public enum HolidayCategory
    {
        YomTov,
        MajorFast,
        CholHamoed,
        Chanukah,
        Purim,
        MinorFast,
        RoshChodesh,
        ErevChag,
        SpecialShabbat,
        MinorHoliday,

        /// <summary>מועדי עדות - מימונה (ספרד ועדות המזרח).</summary>
        CommunityCustom,
        Omer,
        ShabbatMevarchim,
        IsruChag,
        YomKippurKatan,

        /// <summary>תעניות בה"ב (שני-חמישי-שני) אחרי החגים - מנהג אשכנז.</summary>
        Behab,

        /// <summary>תחילת אמירת הסליחות - ליל סליחות באשכנז, ב' באלול בספרד.</summary>
        LeilSelichot,
    }

    /// <summary>מנהג הקהילה - קובע מועדים שתלויים במנהג.</summary>
    public enum CommunityMinhag
    {
        Ashkenaz,

        /// <summary>ספרד ועדות המזרח.</summary>
        Sephardi,
    }

    /// <summary>לפי איזה לוח לחשב מועדים ופרשות.</summary>
    public enum HolidayRegionMode
    {
        /// <summary>לפי אזור הזמן של המיקום שנבחר.</summary>
        Auto,
        Israel,
        Diaspora,
    }

    /// <summary>באיזה יום חוגגים פורים.</summary>
    public enum PurimObservanceMode
    {
        /// <summary>ירושלים - שושן פורים; כל השאר - פורים בי"ד.</summary>
        Auto,

        /// <summary>ערים שאינן מוקפות חומה - פורים בי"ד.</summary>
        Regular,

        /// <summary>ירושלים וערים מוקפות חומה - שושן פורים בט"ו, כולל פורים המשולש.</summary>
        Walled,
    }

    /// <summary>
    /// מועד אחד ביום מסוים. Key הוא מזהה יציב באנגלית (לבדיקות), Name הוא הטקסט המוצג,
    /// ו-NameWithDayNumber הוא הגרסה עם מספר היום (חנוכה, חול המועד, ראש חודש של יומיים).
    /// Minhag מוגדר רק למועד שתלוי במנהג הקהילה.
    /// </summary>
    public sealed record HolidayEvent(
        string Key,
        string Name,
        HolidayCategory Category,
        bool IsYomTov = false,
        string? NameWithDayNumber = null,
        CommunityMinhag? Minhag = null)
    {
        public string DisplayName(bool withDayNumber) =>
            withDayNumber && NameWithDayNumber is not null ? NameWithDayNumber : Name;
    }

    /// <summary>מועד ביום מסוים - לרשימת "המועדים הקרובים".</summary>
    public sealed record DatedHolidayEvent(System.DateTime Date, HolidayEvent Event);

    /// <summary>הגדרות הלוח העברי והמועדים, נשמרות כחלק מ-AppSettings.</summary>
    public sealed class HolidaySettings
    {
        public HolidayRegionMode Region { get; set; } = HolidayRegionMode.Auto;
        public PurimObservanceMode Purim { get; set; } = PurimObservanceMode.Auto;
        public CommunityMinhag Minhag { get; set; } = CommunityMinhag.Ashkenaz;

        public bool ShowErevChag { get; set; } = true;
        public bool ShowFasts { get; set; } = true;
        public bool ShowRoshChodesh { get; set; } = true;
        public bool ShowMinorHolidays { get; set; } = true;
        public bool ShowSpecialShabbatot { get; set; } = true;
        public bool ShowOmer { get; set; } = false;
        public bool ShowShabbatMevarchim { get; set; } = false;
        public bool ShowIsruChag { get; set; } = false;
        public bool ShowYomKippurKatan { get; set; } = false;
        public bool ShowLeilSelichot { get; set; } = false;
        public bool ShowCommunityCustoms { get; set; } = true;
        public bool ShowBehab { get; set; } = false;

        /// <summary>מספר היום בחנוכה ובחול המועד, למשל "חנוכה (יום ג')".</summary>
        public bool ShowDayNumbers { get; set; } = true;

        /// <summary>בוידג'ט מוצג רק המועד החשוב ביותר של היום, כדי לחסוך מקום.</summary>
        public bool WidgetPrimaryOnly { get; set; } = true;

        public HolidaySettings Clone() => (HolidaySettings)MemberwiseClone();

        /// <summary>האם להציג מועדים מהקטגוריה הזו. ימים טובים, חול המועד, צום תשעה באב, חנוכה ופורים מוצגים תמיד.</summary>
        public bool IsVisible(HolidayCategory category) => category switch
        {
            HolidayCategory.ErevChag => ShowErevChag,
            HolidayCategory.MinorFast => ShowFasts,
            HolidayCategory.RoshChodesh => ShowRoshChodesh,
            HolidayCategory.MinorHoliday => ShowMinorHolidays,
            HolidayCategory.SpecialShabbat => ShowSpecialShabbatot,
            HolidayCategory.Omer => ShowOmer,
            HolidayCategory.ShabbatMevarchim => ShowShabbatMevarchim,
            HolidayCategory.IsruChag => ShowIsruChag,
            HolidayCategory.YomKippurKatan => ShowYomKippurKatan,
            HolidayCategory.LeilSelichot => ShowLeilSelichot,
            HolidayCategory.CommunityCustom => ShowCommunityCustoms,
            HolidayCategory.Behab => ShowBehab,
            _ => true,
        };

        /// <summary>האם להציג את המועד - לפי הקטגוריה ולפי מנהג הקהילה שנבחר.</summary>
        public bool IsVisible(HolidayEvent holiday) =>
            IsVisible(holiday.Category) && (holiday.Minhag is null || holiday.Minhag == Minhag);
    }
}
