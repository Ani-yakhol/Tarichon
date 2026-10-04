# נתוני ייחוס לבדיקות

`hebcal-israel.tsv.gz` ו-`hebcal-diaspora.tsv.gz` מכילים את לוח המועדים, ראשי החודשים, שבתות מברכים, ספירת העומר ופרשות השבוע לשנים ה'תש"ס עד ה'תתר"ס (1999–2100), לפי לוח ארץ ישראל ולוח חו"ל.

הנתונים הורדו מה-REST API של [Hebcal.com](https://www.hebcal.com) ומופצים ברישיון [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/). הקבצים נוצרו ב-`generate-fixtures.ps1`. ימים לאומיים לא הורדו (`mod=off`).

פורמט: שורה לכל רשומה, `תאריך<TAB>קטגוריה<TAB>כותרת`, בקידוד UTF-8 ודחיסת gzip.
