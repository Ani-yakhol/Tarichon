using HebrewTaskbarWidget.Services;
using Xunit;

namespace HebrewTaskbarWidget.Tests;

public class HebrewCalendarMathTests
{
    [Fact]
    public void RoundTrip_EveryDayForFiftyYears()
    {
        for (DateTime day = new(2000, 1, 1); day < new DateTime(2050, 1, 1); day = day.AddDays(1))
        {
            HebrewDate hebrew = HebrewCalendarMath.FromGregorian(day);
            Assert.Equal(day, HebrewCalendarMath.ToGregorian(hebrew.Year, hebrew.Month, hebrew.Day));
            Assert.True(hebrew.Month != HebrewMonth.AdarII || HebrewCalendarMath.IsLeapYear(hebrew.Year));
        }
    }

    [Fact]
    public void KnownDates()
    {
        // ראש השנה ה'תשפ"ז, פסח ה'תשפ"ז (שנה מעוברת), פורים ה'תשפ"ה (שנה פשוטה).
        Assert.Equal(new HebrewDate(5787, HebrewMonth.Tishrei, 1), HebrewCalendarMath.FromGregorian(new DateTime(2026, 9, 12)));
        Assert.Equal(new HebrewDate(5787, HebrewMonth.Nisan, 15), HebrewCalendarMath.FromGregorian(new DateTime(2027, 4, 22)));
        Assert.Equal(new HebrewDate(5787, HebrewMonth.AdarII, 14), HebrewCalendarMath.FromGregorian(new DateTime(2027, 3, 23)));
        Assert.Equal(new HebrewDate(5785, HebrewMonth.Adar, 14), HebrewCalendarMath.FromGregorian(new DateTime(2025, 3, 14)));
        Assert.Equal(HebrewMonth.AdarII, HebrewCalendarMath.PurimAdar(5787));
        Assert.Equal(HebrewMonth.Adar, HebrewCalendarMath.PurimAdar(5785));
    }

    [Fact]
    public void MonthNames_DistinguishAdarInLeapYears()
    {
        Assert.Equal("אדר א'", HebrewCalendarMath.MonthName(5787, HebrewMonth.Adar));
        Assert.Equal("אדר ב'", HebrewCalendarMath.MonthName(5787, HebrewMonth.AdarII));
        Assert.Equal("אדר", HebrewCalendarMath.MonthName(5785, HebrewMonth.Adar));
    }

    [Fact]
    public void SaturdayHelpers()
    {
        DateTime wednesday = new(2026, 10, 7);
        Assert.Equal(new DateTime(2026, 10, 10), HebrewCalendarMath.SaturdayOnOrAfter(wednesday));
        Assert.Equal(new DateTime(2026, 10, 3), HebrewCalendarMath.SaturdayOnOrBefore(wednesday));
        Assert.Equal(new DateTime(2026, 10, 10), HebrewCalendarMath.SaturdayOnOrBefore(new DateTime(2026, 10, 10)));
    }
}
