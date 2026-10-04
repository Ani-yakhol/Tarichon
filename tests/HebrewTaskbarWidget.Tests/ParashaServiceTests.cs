using HebrewTaskbarWidget.Services;
using Xunit;

namespace HebrewTaskbarWidget.Tests;

public class ParashaServiceTests
{
    // השמות כפי שהם מופיעים ב-Hebcal, לפי הסדר (0 = בראשית).
    private static readonly string[] HebcalNames =
    {
        "Bereshit", "Noach", "Lech-Lecha", "Vayera", "Chayei Sara", "Toldot", "Vayetzei", "Vayishlach", "Vayeshev", "Miketz",
        "Vayigash", "Vayechi", "Shemot", "Vaera", "Bo", "Beshalach", "Yitro", "Mishpatim", "Terumah", "Tetzaveh",
        "Ki Tisa", "Vayakhel", "Pekudei", "Vayikra", "Tzav", "Shmini", "Tazria", "Metzora", "Achrei Mot", "Kedoshim",
        "Emor", "Behar", "Bechukotai", "Bamidbar", "Nasso", "Beha'alotcha", "Sh'lach", "Korach", "Chukat", "Balak",
        "Pinchas", "Matot", "Masei", "Devarim", "Vaetchanan", "Eikev", "Re'eh", "Shoftim", "Ki Teitzei", "Ki Tavo",
        "Nitzavim", "Vayeilech", "Ha'azinu",
    };

    private static int[] ParseHebcalTitle(string title)
    {
        string name = title.Replace('’', '\'')["Parashat ".Length..];
        int single = Array.IndexOf(HebcalNames, name);
        if (single >= 0)
        {
            return new[] { single };
        }

        int dash = name.LastIndexOf('-');
        int first = Array.IndexOf(HebcalNames, name[..dash]);
        int second = Array.IndexOf(HebcalNames, name[(dash + 1)..]);
        Assert.True(first >= 0 && second == first + 1, $"פרשה לא מוכרת: {title}");
        return new[] { first, second };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EveryShabbat_MatchesHebcal_OverOneHundredYears(bool israel)
    {
        IReadOnlyList<HebcalItem> fixture = HebcalFixture.Load(israel);
        Dictionary<DateTime, int[]> expected = fixture
            .Where(i => i.Category == "parashat")
            .ToDictionary(i => i.Date, i => ParseHebcalTitle(i.Title));

        var errors = new List<string>();
        DateTime first = HebrewCalendarMath.SaturdayOnOrAfter(fixture[0].Date);
        for (DateTime shabbat = first; shabbat <= fixture[^1].Date; shabbat = shabbat.AddDays(7))
        {
            int[]? actual = ParashaService.GetReading(shabbat, israel);
            expected.TryGetValue(shabbat, out int[]? wanted);
            string actualText = actual is null ? "-" : string.Join("+", actual.Select(i => HebcalNames[i]));
            string wantedText = wanted is null ? "-" : string.Join("+", wanted.Select(i => HebcalNames[i]));
            if (actualText != wantedText)
            {
                errors.Add($"{shabbat:yyyy-MM-dd}: צפוי {wantedText}, התקבל {actualText}");
            }
        }

        Assert.True(errors.Count == 0, $"{errors.Count} שבתות שגויות:\n" + string.Join("\n", errors.Take(30)));
    }

    [Fact]
    public void Name_IsForTheComingShabbatAndJoinsCombinedParashot()
    {
        // 2.8.2027 יום שני: השבת הקרובה 7.8.2027 - דברים (שבת חזון).
        Assert.Equal("דברים", ParashaService.GetParashaName(new DateTime(2027, 8, 2), israel: true));

        // 31.7.2027: מטות-מסעי מחוברות.
        Assert.Equal("מטות־מסעי", ParashaService.GetParashaName(new DateTime(2027, 7, 31), israel: true));

        // 12.6.2027: בישראל נשא, ובחו"ל יום טוב שני של שבועות.
        Assert.Equal("נשא", ParashaService.GetParashaName(new DateTime(2027, 6, 12), israel: true));
        Assert.Null(ParashaService.GetParashaName(new DateTime(2027, 6, 12), israel: false));

        // שבת חול המועד סוכות - אין פרשה.
        Assert.Null(ParashaService.GetParashaName(new DateTime(2026, 9, 28), israel: true));
    }
}
