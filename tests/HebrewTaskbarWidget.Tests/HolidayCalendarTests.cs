using System.Text.RegularExpressions;
using HebrewTaskbarWidget.Models;
using HebrewTaskbarWidget.Services;
using Xunit;

namespace HebrewTaskbarWidget.Tests;

public class HolidayCalendarTests
{
    private static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };

    // מועדים שלנו שאין להם מקבילה ב-Hebcal, או שתלויים במנהג/בעיר ונבדקים בנפרד.
    private static readonly HashSet<string> NotInHebcal = new() { "Isru Chag", "Mimouna", "Selichot Sephardi", "Behab", "Purim Meshulash" };

    /// <summary>ממפה כותרת של Hebcal למפתח שלנו. null = מועד שאנחנו לא מציגים בכוונה.</summary>
    private static string? MapHebcalTitle(string title)
    {
        title = title.Replace('’', '\'');

        if (Regex.Match(title, @"^Rosh Hashana \d{4}$").Success) return "Rosh Hashana 1";
        if (title == "Rosh Hashana II") return "Rosh Hashana 2";
        if (title.StartsWith("Rosh Chodesh ")) return "Rosh Chodesh";
        if (title.StartsWith("Mevarchim Chodesh ")) return "Shabbat Mevarchim";

        Match festival = Regex.Match(title, @"^(Sukkot|Pesach|Shavuot) ?(I|II|III|IV|V|VI|VII|VIII)?(?: \(.*\))?$");
        if (festival.Success)
        {
            int day = festival.Groups[2].Success ? Array.IndexOf(Roman, festival.Groups[2].Value) + 1 : 1;
            return $"{festival.Groups[1].Value} {day}";
        }

        Match candles = Regex.Match(title, @"^Chanukah: (\d) Candles?$");
        if (candles.Success)
        {
            // Hebcal מתארך את הנר לפי הערב שבו מדליקים; אצלנו היום שאחריו הוא יום החנוכה.
            int candle = int.Parse(candles.Groups[1].Value);
            return candle == 1 ? "Erev Chanukah" : $"Chanukah {candle - 1}";
        }

        Match omer = Regex.Match(title, @"^(\d+)(st|nd|rd|th) day of the Omer$");
        if (omer.Success) return $"Omer {omer.Groups[1].Value}";

        return title switch
        {
            "Chanukah: 8th Day" => "Chanukah 8",
            "Asara B'Tevet" => "Asara BTevet",
            "Ta'anit Esther" => "Taanit Esther",
            "Ta'anit Bechorot" => "Taanit Bechorot",
            "Erev Tish'a B'Av" => "Erev Tisha BAv",
            "Tish'a B'Av" or "Tish'a B'Av (observed)" => "Tisha BAv",
            "Tu B'Av" => "Tu BAv",
            "Purim Meshulash" => "Purim Meshulash",
            "Chag HaBanot" or "Rosh Hashana LaBehemot" => null,
            "Erev Rosh Hashana" or "Tzom Gedaliah" or "Shabbat Shuva" or "Erev Yom Kippur" or "Yom Kippur"
                or "Erev Sukkot" or "Shmini Atzeret" or "Simchat Torah" or "Yom Kippur Katan" or "Tu BiShvat"
                or "Shabbat Shirah" or "Purim Katan" or "Shushan Purim Katan" or "Shabbat Shekalim" or "Shabbat Zachor"
                or "Erev Purim" or "Purim" or "Shushan Purim" or "Shabbat Parah" or "Shabbat HaChodesh"
                or "Shabbat HaGadol" or "Erev Pesach" or "Pesach Sheni" or "Lag BaOmer" or "Erev Shavuot"
                or "Tzom Tammuz" or "Shabbat Chazon" or "Shabbat Nachamu" or "Leil Selichot" or "Birkat Hachamah" => title,
            _ => throw new InvalidOperationException($"כותרת לא מוכרת מ-Hebcal: {title}"),
        };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AllHolidays_MatchHebcal_OverOneHundredYears(bool israel)
    {
        IReadOnlyList<HebcalItem> fixture = HebcalFixture.Load(israel);
        var expected = new HashSet<(DateTime, string)>();
        foreach (HebcalItem item in fixture.Where(i => i.Category != "parashat"))
        {
            string? key = MapHebcalTitle(item.Title);
            if (key is not null && !NotInHebcal.Contains(key))
            {
                expected.Add((item.Date, key));
            }
        }

        var actual = new HashSet<(DateTime, string)>();
        DateTime first = fixture[0].Date;
        DateTime last = fixture[^1].Date;
        for (DateTime day = first; day <= last; day = day.AddDays(1))
        {
            foreach (HolidayEvent holiday in HolidayCalendar.GetAllEvents(day, israel, walledCity: false))
            {
                if (!NotInHebcal.Contains(holiday.Key))
                {
                    actual.Add((day, holiday.Key));
                }
            }
        }

        string missing = string.Join("\n", expected.Except(actual).OrderBy(x => x.Item1).Take(25).Select(x => $"חסר: {x.Item1:yyyy-MM-dd} {x.Item2}"));
        string extra = string.Join("\n", actual.Except(expected).OrderBy(x => x.Item1).Take(25).Select(x => $"עודף: {x.Item1:yyyy-MM-dd} {x.Item2}"));
        Assert.True(missing.Length == 0 && extra.Length == 0, missing + "\n" + extra);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FestivalDays_HaveTheRightCategory(bool israel)
    {
        foreach (HebcalItem item in HebcalFixture.Load(israel).Where(i => Regex.IsMatch(i.Title, @"^(Sukkot|Pesach|Shavuot)( [IV]+)?( \(.*\))?$")))
        {
            string key = MapHebcalTitle(item.Title)!;
            HolidayEvent holiday = HolidayCalendar.GetAllEvents(item.Date, israel, false).Single(e => e.Key == key);
            bool cholHamoed = item.Title.Contains("CH") || item.Title.Contains("Hoshana");
            Assert.Equal(cholHamoed ? HolidayCategory.CholHamoed : HolidayCategory.YomTov, holiday.Category);
            Assert.Equal(!cholHamoed, holiday.IsYomTov);
        }
    }

    [Fact]
    public void PurimMeshulash_InWalledCities_WhenShushanPurimFallsOnShabbat()
    {
        List<DateTime> meshulashDates = HebcalFixture.Load(true).Where(i => i.Title == "Purim Meshulash").Select(i => i.Date).ToList();
        Assert.NotEmpty(meshulashDates);

        foreach (DateTime sunday in meshulashDates)
        {
            Assert.Equal(DayOfWeek.Sunday, sunday.DayOfWeek);
            Assert.Contains(HolidayCalendar.GetAllEvents(sunday, true, walledCity: true), e => e.Key == "Purim Meshulash");
            Assert.DoesNotContain(HolidayCalendar.GetAllEvents(sunday, true, walledCity: false), e => e.Key == "Purim Meshulash");
            Assert.Contains(HolidayCalendar.GetAllEvents(sunday.AddDays(-2), true, walledCity: true), e => e.Name.StartsWith("פורים המשולש"));
        }

        // שנה רגילה: אין פורים המשולש גם בירושלים.
        DateTime purim2027 = new(2027, 3, 23);
        Assert.Equal("פורים", HolidayCalendar.GetAllEvents(purim2027, true, true).Single(e => e.Category == HolidayCategory.Purim).Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NationalDays_AreNeverIncluded(bool israel)
    {
        string[] forbidden =
        {
            "HaShoah", "HaZikaron", "Atzma", "Yerushalayim", "Sigd", "Aliyah", "Herzl", "Ben-Gurion", "Jabotinsky", "Rabin",
            "השואה", "הזיכרון", "הזכרון", "העצמאות", "ירושלים", "סיגד", "העלייה", "הרצל", "בן-גוריון", "בן גוריון", "ז'בוטינסקי", "רבין",
        };

        for (DateTime day = new(2020, 1, 1); day < new DateTime(2040, 1, 1); day = day.AddDays(1))
        {
            foreach (bool walled in new[] { false, true })
            {
                foreach (HolidayEvent holiday in HolidayCalendar.GetAllEvents(day, israel, walled))
                {
                    Assert.DoesNotContain(forbidden, f => holiday.Key.Contains(f) || holiday.Name.Contains(f));
                }
            }
        }
    }

    [Fact]
    public void SimchatTorah_IsOneDayInIsraelAndTwoInDiaspora()
    {
        DateTime shminiAtzeret = new(2026, 10, 3);
        Assert.Equal("שמיני עצרת ושמחת תורה", HolidayCalendar.GetAllEvents(shminiAtzeret, true, false)[0].Name);
        Assert.Equal("שמיני עצרת", HolidayCalendar.GetAllEvents(shminiAtzeret, false, false)[0].Name);
        Assert.Equal("שמחת תורה", HolidayCalendar.GetAllEvents(shminiAtzeret.AddDays(1), false, false)[0].Name);
        Assert.Equal("אסרו חג", HolidayCalendar.GetAllEvents(shminiAtzeret.AddDays(1), true, false)[0].Name);
    }

    [Fact]
    public void MinhagDependentDays_FollowTheChosenMinhag()
    {
        var ashkenaz = new HolidayOptions(true, false, new HolidaySettings { ShowLeilSelichot = true, ShowBehab = true, Minhag = CommunityMinhag.Ashkenaz });
        var sephardi = new HolidayOptions(true, false, new HolidaySettings { ShowLeilSelichot = true, ShowBehab = true, Minhag = CommunityMinhag.Sephardi });

        // ה'תשפ"ז: ב' באלול = 4.9.2027, ליל סליחות אשכנז = 25.9.2027, אסרו חג פסח = 29.4.2027.
        DateTime elul2 = new(2027, 9, 4);
        DateTime leilSelichot = new(2027, 9, 25);
        DateTime isruChag = new(2027, 4, 29);

        Assert.Contains(HolidayCalendar.GetEvents(leilSelichot, ashkenaz), e => e.Key == "Leil Selichot");
        Assert.DoesNotContain(HolidayCalendar.GetEvents(leilSelichot, sephardi), e => e.Key == "Leil Selichot");
        Assert.Contains(HolidayCalendar.GetEvents(elul2, sephardi), e => e.Key == "Selichot Sephardi");
        Assert.DoesNotContain(HolidayCalendar.GetEvents(elul2, ashkenaz), e => e.Key == "Selichot Sephardi");
        Assert.Contains(HolidayCalendar.GetEvents(isruChag, sephardi), e => e.Key == "Mimouna");
        Assert.DoesNotContain(HolidayCalendar.GetEvents(isruChag, ashkenaz), e => e.Key == "Mimouna");

        int behabAshkenaz = 0;
        int behabSephardi = 0;
        for (DateTime day = new(2026, 9, 12); day < new DateTime(2027, 10, 2); day = day.AddDays(1))
        {
            behabAshkenaz += HolidayCalendar.GetEvents(day, ashkenaz).Count(e => e.Key == "Behab");
            behabSephardi += HolidayCalendar.GetEvents(day, sephardi).Count(e => e.Key == "Behab");
        }

        Assert.Equal(6, behabAshkenaz);
        Assert.Equal(0, behabSephardi);
    }

    [Fact]
    public void Behab_FallsOnMondayThursdayMonday()
    {
        for (DateTime day = new(2000, 1, 1); day < new DateTime(2060, 1, 1); day = day.AddDays(1))
        {
            foreach (HolidayEvent holiday in HolidayCalendar.GetAllEvents(day, true, false).Where(e => e.Key == "Behab"))
            {
                Assert.Contains(day.DayOfWeek, new[] { DayOfWeek.Monday, DayOfWeek.Thursday });
                HebrewDate hebrew = HebrewCalendarMath.FromGregorian(day);
                Assert.Contains(hebrew.Month, new[] { HebrewMonth.Cheshvan, HebrewMonth.Iyar });
                Assert.False(hebrew.Month == HebrewMonth.Iyar && hebrew.Day == 14, "בה\"ב לא חל בפסח שני");
            }
        }
    }

    [Fact]
    public void DisplayText_RespectsContextAndSettings()
    {
        // 10.12.2026: יום ו' של חנוכה וא' דראש חודש טבת.
        DateTime day = new(2026, 12, 10);
        var settings = new HolidaySettings();
        var options = new HolidayOptions(true, false, settings);

        Assert.Equal("חנוכה (יום ו')", HolidayCalendar.GetDisplayText(day, HolidayDisplayContext.Widget, options));
        Assert.Equal("חנוכה (יום ו') · ראש חודש טבת (יום א')", HolidayCalendar.GetDisplayText(day, HolidayDisplayContext.Popup, options));

        settings.ShowDayNumbers = false;
        settings.ShowRoshChodesh = false;
        Assert.Equal("חנוכה", HolidayCalendar.GetDisplayText(day, HolidayDisplayContext.Popup, options));

        settings.WidgetPrimaryOnly = false;
        settings.ShowRoshChodesh = true;
        Assert.Equal("חנוכה · ראש חודש טבת", HolidayCalendar.GetDisplayText(day, HolidayDisplayContext.Widget, options));

        Assert.Equal("זאת חנוכה", HolidayCalendar.GetAllEvents(new DateTime(2026, 12, 12), true, false)[0].DisplayName(true));
        Assert.Null(HolidayCalendar.GetDisplayText(new DateTime(2026, 11, 3), HolidayDisplayContext.Popup, options));
    }

    [Fact]
    public void PostponedAndAdvancedFasts_AreLabelled()
    {
        // ה'תשפ"ב: ט' באב חל בשבת 6.8.2022 - הצום נדחה ליום ראשון.
        Assert.Equal("תשעה באב (נדחה)", HolidayCalendar.GetAllEvents(new DateTime(2022, 8, 7), true, false).Single(e => e.Key == "Tisha BAv").Name);
        Assert.Contains(HolidayCalendar.GetAllEvents(new DateTime(2022, 8, 6), true, false), e => e.Key == "Erev Tisha BAv");

        // ה'תשפ"ה: פורים ביום שישי, ולכן תענית אסתר ביום חמישי 13.3.2025 כרגיל.
        Assert.Equal("תענית אסתר", HolidayCalendar.GetAllEvents(new DateTime(2025, 3, 13), true, false).Single(e => e.Key == "Taanit Esther").Name);

        // ה'תשפ"ד: י"ג באדר ב' חל בשבת 23.3.2024 - התענית הוקדמה ליום חמישי 21.3.2024.
        Assert.Equal("תענית אסתר (מוקדמת)", HolidayCalendar.GetAllEvents(new DateTime(2024, 3, 21), true, false).Single(e => e.Key == "Taanit Esther").Name);

        // ה'תשפ"א: ערב פסח חל בשבת 27.3.2021 - תענית בכורות הוקדמה ליום חמישי 25.3.2021.
        Assert.Equal("תענית בכורות (מוקדמת)", HolidayCalendar.GetAllEvents(new DateTime(2021, 3, 25), true, false).Single(e => e.Key == "Taanit Bechorot").Name);
    }

    [Fact]
    public void CandleLighting_BeforeSunsetOrAfterTzeit()
    {
        // ה'תשפ"ז: ר"ה בשבת 12.9.2026 ובראשון 13.9.2026.
        Assert.Equal(CandleLightingKind.BeforeSunset, HolidayCalendar.GetCandleLighting(new DateTime(2026, 9, 11), true));
        Assert.Equal(CandleLightingKind.AfterTzeit, HolidayCalendar.GetCandleLighting(new DateTime(2026, 9, 12), true));
        Assert.Equal(CandleLightingKind.None, HolidayCalendar.GetCandleLighting(new DateTime(2026, 9, 13), true));

        // סוכות ה'תשפ"ז ביום שבת 26.9.2026: בחו"ל מדליקים במוצאי שבת ליום טוב שני; בישראל לא.
        Assert.Equal(CandleLightingKind.AfterTzeit, HolidayCalendar.GetCandleLighting(new DateTime(2026, 9, 26), false));
        Assert.Equal(CandleLightingKind.None, HolidayCalendar.GetCandleLighting(new DateTime(2026, 9, 27), true));

        // שבועות ה'תשפ"ז ביום שישי 11.6.2027: מדליקים לשבת לפני השקיעה.
        Assert.Equal(CandleLightingKind.BeforeSunset, HolidayCalendar.GetCandleLighting(new DateTime(2027, 6, 10), true));
        Assert.Equal(CandleLightingKind.BeforeSunset, HolidayCalendar.GetCandleLighting(new DateTime(2027, 6, 11), true));

        // יום כיפור: אין הדלקה במוצאיו.
        Assert.Equal(CandleLightingKind.None, HolidayCalendar.GetCandleLighting(new DateTime(2026, 9, 21), true));
        Assert.Equal(CandleLightingKind.BeforeSunset, HolidayCalendar.GetCandleLighting(new DateTime(2026, 9, 20), true));
    }

    [Fact]
    public void ShabbatOrYomTov_DependsOnRegion()
    {
        DateTime simchatTorahDiaspora = new(2026, 10, 4);
        Assert.True(HolidayCalendar.IsShabbatOrYomTov(simchatTorahDiaspora, israel: false));
        Assert.False(HolidayCalendar.IsShabbatOrYomTov(simchatTorahDiaspora, israel: true));
        Assert.True(HolidayCalendar.IsShabbatOrYomTov(new DateTime(2026, 10, 10), israel: true));
    }

    [Fact]
    public void AutoRegionAndPurim_FollowTheLocation()
    {
        var settings = new AppSettings { LocationName = "ירושלים", TimeZoneId = "Israel Standard Time" };
        HolidayOptions options = HolidayOptions.FromSettings(settings);
        Assert.True(options.Israel);
        Assert.True(options.WalledCity);

        settings.LocationName = "ניו יורק, ארה״ב";
        settings.TimeZoneId = "Eastern Standard Time";
        options = HolidayOptions.FromSettings(settings);
        Assert.False(options.Israel);
        Assert.False(options.WalledCity);

        settings.Holidays.Region = HolidayRegionMode.Israel;
        settings.Holidays.Purim = PurimObservanceMode.Walled;
        options = HolidayOptions.FromSettings(settings);
        Assert.True(options.Israel);
        Assert.True(options.WalledCity);
    }

    [Fact]
    public void Upcoming_SkipsOmerAndRespectsCount()
    {
        var options = new HolidayOptions(true, false, new HolidaySettings { ShowOmer = true });
        IReadOnlyList<DatedHolidayEvent> upcoming = HolidayCalendar.GetUpcoming(new DateTime(2027, 4, 22), 12, options);
        Assert.True(upcoming.Count >= 12);
        Assert.DoesNotContain(upcoming, u => u.Event.Category == HolidayCategory.Omer);
        Assert.Equal("פסח", upcoming[0].Event.Name);
    }
}
