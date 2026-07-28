using System.Text;

namespace HebrewTaskbarWidget.Services
{
    /// <summary>
    /// המרת מספרים (יום בחודש, שנה עברית) לאותיות עבריות לפי כללי הגימטריה המקובלים,
    /// כולל הוספת גרש (') לאות בודדת וגרשיים (") לפני האות האחרונה במספר מורכב,
    /// וכן הטיפול המקובל במקרים 15 ו-16 (ט"ו, ט"ז במקום יה/יו).
    /// </summary>
    public static class HebrewGematria
    {
        private static readonly (int Value, string Letter)[] HundredsMap =
        {
            (400, "ת"), (300, "ש"), (200, "ר"), (100, "ק"),
        };

        private static readonly (int Value, string Letter)[] TensMap =
        {
            (90, "צ"), (80, "פ"), (70, "ע"), (60, "ס"), (50, "נ"),
            (40, "מ"), (30, "ל"), (20, "כ"), (10, "י"),
        };

        private static readonly (int Value, string Letter)[] UnitsMap =
        {
            (9, "ט"), (8, "ח"), (7, "ז"), (6, "ו"), (5, "ה"),
            (4, "ד"), (3, "ג"), (2, "ב"), (1, "א"),
        };

        /// <summary>
        /// ממיר מספר (1 עד 999) לרצף אותיות עבריות ללא סימני פיסוק (גרש/גרשיים).
        /// </summary>
        public static string ToLetters(int number)
        {
            if (number <= 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            int remaining = number;

            // מאות מעל 400 - מצטברות כמכפלות של ת' (למשל 500 = ת + ק)
            while (remaining >= 400)
            {
                sb.Append('ת');
                remaining -= 400;
            }

            if (remaining >= 100)
            {
                foreach (var (value, letter) in HundredsMap)
                {
                    if (value == 400)
                    {
                        continue; // כבר טופל בלולאה למעלה
                    }

                    if (remaining >= value)
                    {
                        sb.Append(letter);
                        remaining -= value;
                        break;
                    }
                }
            }

            // מקרים מיוחדים: 15 ו-16 נכתבים ט"ו / ט"ז כדי להימנע מכתיבת שם ה'
            if (remaining == 15)
            {
                sb.Append("טו");
                remaining = 0;
            }
            else if (remaining == 16)
            {
                sb.Append("טז");
                remaining = 0;
            }
            else
            {
                if (remaining >= 10)
                {
                    foreach (var (value, letter) in TensMap)
                    {
                        if (remaining >= value)
                        {
                            sb.Append(letter);
                            remaining -= value;
                            break;
                        }
                    }
                }

                if (remaining >= 1)
                {
                    foreach (var (value, letter) in UnitsMap)
                    {
                        if (remaining >= value)
                        {
                            sb.Append(letter);
                            remaining -= value;
                            break;
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// מוסיף פיסוק עברי תקני: גרש (') אחרי אות בודדת, או גרשיים (") לפני האות האחרונה
        /// כאשר מדובר ביותר מאות אחת. לדוגמה: "כח" הופך ל- כ"ח, ו-"ה" הופך ל- ה'.
        /// </summary>
        public static string Punctuate(string letters)
        {
            if (string.IsNullOrEmpty(letters))
            {
                return letters;
            }

            if (letters.Length == 1)
            {
                return letters + "'";
            }

            return letters[..^1] + "\"" + letters[^1..];
        }

        /// <summary>
        /// מפרמט יום בחודש (1-30) בתור מספר עברי מפוסק, לדוגמה 28 -> כ"ח.
        /// </summary>
        public static string FormatDay(int day) => Punctuate(ToLetters(day));

        /// <summary>
        /// מפרמט שנה עברית מלאה (למשל 5786) לפי המוסכמה המקובלת: אלפים בגרש בודד,
        /// והשארית עם גרשיים לפני האות האחרונה. לדוגמה: 5786 -> ה'תשפ"ו.
        /// </summary>
        public static string FormatYear(int hebrewYear)
        {
            int thousands = hebrewYear / 1000;
            int remainder = hebrewYear % 1000;

            string thousandsPart = thousands > 0 ? ToLetters(thousands) + "'" : string.Empty;
            string remainderPart = Punctuate(ToLetters(remainder));

            return thousandsPart + remainderPart;
        }
    }
}
