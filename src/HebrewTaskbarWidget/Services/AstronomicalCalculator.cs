using System;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// מחשב מיקום השמש (זריחה/שקיעה עבור זווית נתונה מתחת/מעל האופק) לפי אלגוריתם
    /// השמש הסטנדרטי של NOAA (מבוסס על הנוסחאות של Jean Meeus) - אותה שיטת חישוב
    /// שעליה מבוססים גם ספריית KosherJava וגם מחשבון השמש הרשמי של NOAA.
    ///
    /// כל הפונקציות כאן הן חישוב אסטרונומי טהור (מיקום השמש בזמן נתון), ללא תלות
    /// בהלכה עצמה - השכבה ההלכתית (אלות השחר, נץ, שקיעה, צאת הכוכבים וכו') נמצאת
    /// ב-ZmanimCalendar.cs.
    /// </summary>
    public static class AstronomicalCalculator
    {
        /// <summary>זווית "אופק גיאומטרי" - 90 מעלות בין נקודת הצפייה לשמש.</summary>
        public const double GeometricZenith = 90.0;

        /// <summary>
        /// מחשב את שעת החציה של השמש בזווית נתונה (זנית) ביום מסויים, עבור מיקום
        /// גיאוגרפי נתון. מחזיר null אם השמש אינה חוצה את הזווית ביום זה (למשל
        /// באזורים קוטביים בקיץ/חורף - לא רלוונטי לישראל אך נשמר לשלמות הפתרון).
        /// </summary>
        /// <param name="date">התאריך (משמש רק לצורך היום הקלנדרי, בשעון מקומי).</param>
        /// <param name="latitude">קו רוחב במעלות (חיובי = צפון).</param>
        /// <param name="longitude">קו אורך במעלות (חיובי = מזרח).</param>
        /// <param name="zenithDegrees">הזווית ממנה מחושב הזמן (90 = אופק גיאומטרי).</param>
        /// <param name="isSunrise">true לזריחה (חצי מזרחי), false לשקיעה (חצי מערבי).</param>
        /// <param name="timeZone">אזור הזמן להצגת התוצאה.</param>
        public static DateTime? CalculateSunEvent(
            DateTime date,
            double latitude,
            double longitude,
            double zenithDegrees,
            bool isSunrise,
            TimeZoneInfo timeZone)
        {
            double julianDay = ToJulianDay(date.Year, date.Month, date.Day);
            double utcOffsetHours = timeZone.GetUtcOffset(date).TotalHours;

            double? minutesUtc = CalculateSunEventUtcMinutes(julianDay, latitude, longitude, zenithDegrees, isSunrise);
            if (minutesUtc is null)
            {
                return null;
            }

            double localMinutes = minutesUtc.Value + utcOffsetHours * 60.0;

            // נירמול לטווח היום (יכול "לגלוש" ליום שלפני/אחרי ליד חצות באזורי זמן קיצוניים)
            DateTime midnight = date.Date;
            return midnight.AddMinutes(localMinutes);
        }

        private static double? CalculateSunEventUtcMinutes(
            double julianDay,
            double latitude,
            double longitude,
            double zenithDegrees,
            bool isSunrise)
        {
            double t = JulianCentury(julianDay);

            double eqTime = EquationOfTimeMinutes(t);
            double solarDec = SunDeclinationDegrees(t);

            double hourAngleMagnitude = HourAngleMagnitudeDegrees(latitude, solarDec, zenithDegrees);
            if (double.IsNaN(hourAngleMagnitude))
            {
                return null; // השמש לא מגיעה לזווית הזו ביום זה במיקום זה
            }

            // צהרי חמה (Solar Noon) ב-UTC, ואז זריחה = לפניו, שקיעה = אחריו,
            // במרחק של זווית השעה (מומרת לדקות: 4 דקות למעלה).
            double solarNoonUtcMinutes = 720.0 - 4.0 * longitude - eqTime;
            double timeUtcMinutes = isSunrise
                ? solarNoonUtcMinutes - 4.0 * hourAngleMagnitude
                : solarNoonUtcMinutes + 4.0 * hourAngleMagnitude;

            return timeUtcMinutes;
        }

        private static double ToJulianDay(int year, int month, int day)
        {
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }

            double a = Math.Floor(year / 100.0);
            double b = 2 - a + Math.Floor(a / 4.0);

            double jd = Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + b - 1524.5;
            return jd;
        }

        private static double JulianCentury(double julianDay) => (julianDay - 2451545.0) / 36525.0;

        private static double SunGeometricMeanLongitudeDegrees(double t)
        {
            double l0 = 280.46646 + t * (36000.76983 + 0.0003032 * t);
            return NormalizeDegrees(l0);
        }

        private static double SunGeometricMeanAnomalyDegrees(double t) => 357.52911 + t * (35999.05029 - 0.0001537 * t);

        private static double EarthOrbitEccentricity(double t) => 0.016708634 - t * (0.000042037 + 0.0000001267 * t);

        private static double SunEquationOfCenterDegrees(double t)
        {
            double m = SunGeometricMeanAnomalyDegrees(t);
            double mRad = DegreesToRadians(m);

            return Math.Sin(mRad) * (1.914602 - t * (0.004817 + 0.000014 * t))
                 + Math.Sin(2 * mRad) * (0.019993 - 0.000101 * t)
                 + Math.Sin(3 * mRad) * 0.000289;
        }

        private static double SunTrueLongitudeDegrees(double t) => SunGeometricMeanLongitudeDegrees(t) + SunEquationOfCenterDegrees(t);

        private static double SunApparentLongitudeDegrees(double t)
        {
            double trueLongitude = SunTrueLongitudeDegrees(t);
            double omega = 125.04 - 1934.136 * t;
            return trueLongitude - 0.00569 - 0.00478 * Math.Sin(DegreesToRadians(omega));
        }

        private static double MeanObliquityOfEclipticDegrees(double t)
        {
            double seconds = 21.448 - t * (46.815 + t * (0.00059 - t * 0.001813));
            return 23.0 + (26.0 + seconds / 60.0) / 60.0;
        }

        private static double ObliquityCorrectionDegrees(double t)
        {
            double e0 = MeanObliquityOfEclipticDegrees(t);
            double omega = 125.04 - 1934.136 * t;
            return e0 + 0.00256 * Math.Cos(DegreesToRadians(omega));
        }

        private static double SunDeclinationDegrees(double t)
        {
            double e = ObliquityCorrectionDegrees(t);
            double lambda = SunApparentLongitudeDegrees(t);

            double sinDec = Math.Sin(DegreesToRadians(e)) * Math.Sin(DegreesToRadians(lambda));
            return RadiansToDegrees(Math.Asin(sinDec));
        }

        private static double EquationOfTimeMinutes(double t)
        {
            double epsilon = ObliquityCorrectionDegrees(t);
            double l0 = SunGeometricMeanLongitudeDegrees(t);
            double e = EarthOrbitEccentricity(t);
            double m = SunGeometricMeanAnomalyDegrees(t);

            double y = Math.Tan(DegreesToRadians(epsilon) / 2.0);
            y *= y;

            double l0Rad = DegreesToRadians(l0);
            double mRad = DegreesToRadians(m);

            double sin2L0 = Math.Sin(2.0 * l0Rad);
            double sinM = Math.Sin(mRad);
            double cos2L0 = Math.Cos(2.0 * l0Rad);
            double sin4L0 = Math.Sin(4.0 * l0Rad);
            double sin2M = Math.Sin(2.0 * mRad);

            double eTime = y * sin2L0
                - 2.0 * e * sinM
                + 4.0 * e * y * sinM * cos2L0
                - 0.5 * y * y * sin4L0
                - 1.25 * e * e * sin2M;

            return RadiansToDegrees(eTime) * 4.0; // דקות
        }

        /// <summary>
        /// גודל (ללא סימן) זווית השעה (Hour Angle) בה השמש חוצה את זווית הזנית
        /// המבוקשת, במעלות - כלומר המרחק הזוויתי מצהרי החמה בשני הכיוונים
        /// (זריחה וגם שקיעה נמצאים באותו מרחק, בכיוונים הפוכים). מחזיר NaN אם
        /// הערך אינו בטווח המתמטי התקף (השמש לא מגיעה לזווית זו ביום/במיקום זה).
        /// </summary>
        private static double HourAngleMagnitudeDegrees(double latitude, double solarDecDegrees, double zenithDegrees)
        {
            double latRad = DegreesToRadians(latitude);
            double decRad = DegreesToRadians(solarDecDegrees);
            double zenithRad = DegreesToRadians(zenithDegrees);

            double cosHourAngle = (Math.Cos(zenithRad) - Math.Sin(latRad) * Math.Sin(decRad))
                                 / (Math.Cos(latRad) * Math.Cos(decRad));

            if (cosHourAngle < -1.0 || cosHourAngle > 1.0)
            {
                return double.NaN;
            }

            return RadiansToDegrees(Math.Acos(cosHourAngle));
        }

        private static double NormalizeDegrees(double degrees)
        {
            double result = degrees % 360.0;
            return result < 0 ? result + 360.0 : result;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

        private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;
    }
}
