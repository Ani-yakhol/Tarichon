using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace HebrewTaskbarWidget.Tests;

/// <summary>רשומה אחת מקובץ הייחוס של Hebcal.</summary>
public sealed record HebcalItem(DateTime Date, string Category, string Title);

/// <summary>טוען את קבצי הייחוס (Fixtures/*.tsv.gz) שנוצרו ב-generate-fixtures.ps1.</summary>
public static class HebcalFixture
{
    private static readonly Dictionary<bool, IReadOnlyList<HebcalItem>> Cache = new();

    public static IReadOnlyList<HebcalItem> Load(bool israel)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(israel, out IReadOnlyList<HebcalItem>? cached))
            {
                return cached;
            }

            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", israel ? "hebcal-israel.tsv.gz" : "hebcal-diaspora.tsv.gz");
            using var file = File.OpenRead(path);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);

            // כל שנה עברית הורדה בנפרד, ולכן ערב ראש השנה מופיע פעמיים - מסירים כפילויות.
            var items = new HashSet<HebcalItem>();
            while (reader.ReadLine() is string line)
            {
                string[] parts = line.Split('\t');
                if (parts.Length == 3)
                {
                    items.Add(new HebcalItem(DateTime.ParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture), parts[1], parts[2]));
                }
            }

            IReadOnlyList<HebcalItem> result = items.OrderBy(i => i.Date).ToList();
            Cache[israel] = result;
            return result;
        }
    }
}
