using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HebrewTaskbarWidget.Controls
{
    /// <summary>
    /// מנגנון קלט "מסכה" לשדות שעה/משך-זמן בפורמט H:MM או HH:MM (כמו שעון
    /// דיגיטלי סגמנטי): התו ':' לעולם לא נמחק ולא ניתן לדריסה; מיקום הסמן
    /// (בלחיצה או ב-Tab לתוך השדה) תמיד "מכחיל" (בוחר) ספרה בודדת; הקלדת
    /// ספרה מחליפה את הספרה הנבחרת ומתקדמת מיד לספרה הבאה; מקש Backspace/
    /// Delete מאפס את הספרה הנבחרת ל-'0' במקום למחוק תו מהמחרוזת. כן דואג
    /// שלא ניתן יהיה להזין ערך לא-תקין: הספרה הראשונה של הדקות (מיד אחרי
    /// ה-':') מוגבלת ל-0-5 (כלומר דקות 00-59 בלבד, לעולם לא, למשל, 1:65) -
    /// ובתבנית "HH:MM" (שעון 24 שעות אמיתי, מזוהה אוטומטית לפי מיקום ה-':')
    /// גם השעות מוגבלות ל-00-23. משמש את שדות משך-הזמן להתראות ("H:MM")
    /// ואת שדה השעה הידנית ("HH:MM").
    /// </summary>
    internal static class TimeMaskTextBoxBehavior
    {
        public static void Attach(TextBox textBox)
        {
            textBox.FlowDirection = FlowDirection.LeftToRight;
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            textBox.GotKeyboardFocus += (_, _) => SelectDigitNear(textBox, 0);
            textBox.PreviewMouseLeftButtonDown += OnMouseDown;
            textBox.PreviewMouseLeftButtonUp += OnMouseUp;
            DataObject.AddPastingHandler(textBox, OnPaste);
        }

        // -----------------------------------------------------------------
        // תיקון באג קריטי: TextBox רגיל "לוכד" (Mouse Capture) את העכבר
        // בלחיצה (MouseLeftButtonDown) כדי לתמוך בגרירת-בחירה, ומשחרר אותו
        // רק בטיפול הפנימי הרגיל שלו ב-MouseLeftButtonUp. מאחר שהמנגנון
        // הזה טיפל (Handled) רק ב-MouseLeftButtonUp כדי לממש "בחירת ספרה
        // בודדת" משלו, הטיפול הפנימי הרגיל של ה-TextBox ב-Up מעולם לא רץ -
        // כך שהלכידה שהתחילה ב-Down נשארת פעילה לצמיתות, וכל הלחיצות הבאות
        // בכל מקום בפאנל ממשיכות "להיתפס" ע"י תיבת הטקסט הזו בלבד (בדיוק
        // הבאג שדווח: "העכבר נתקע"). התיקון: מטפלים גם ב-Down (כדי שהמנגנון
        // הדיפולטיבי של TextBox לעולם לא ילכוד את העכבר מלכתחילה), וגם
        // משחררים במפורש כל לכידה קיימת ב-Up, כרשת ביטחון נוספת.
        // -----------------------------------------------------------------
        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var tb = (TextBox)sender;

            if (!tb.IsKeyboardFocused)
            {
                tb.Focus();
            }

            int charIndex = tb.GetCharacterIndexFromPoint(e.GetPosition(tb), true);
            if (charIndex >= 0)
            {
                SelectDigitNear(tb, charIndex);
            }

            e.Handled = true;
        }

        private static void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var tb = (TextBox)sender;

            if (Mouse.Captured == tb)
            {
                tb.ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;

            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                int pos = CurrentDigitIndex(tb);
                if (pos >= 0)
                {
                    ReplaceDigit(tb, pos, '0');
                }

                e.Handled = true;
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;

            if (e.Text.Length != 1 || !char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
                return;
            }

            int pos = CurrentDigitIndex(tb);
            if (pos < 0)
            {
                e.Handled = true;
                return;
            }

            char digit = e.Text[0];

            if (!IsValidDigitForPosition(tb, pos, digit))
            {
                e.Handled = true;
                return;
            }

            ReplaceDigit(tb, pos, digit);
            AdvanceToNextDigit(tb, pos);

            e.Handled = true;
        }

        /// <summary>
        /// בודקת שהספרה שהוקלדה לא תיצור ערך לא-תקין: דקות (הספרה מיד אחרי
        /// ה-':') מוגבלות ל-0-5 תמיד; ובתבנית "HH:MM" (שתי ספרות שעה לפני
        /// ה-':', מזוהה לפי המיקום שלו - 2 - מתאים לשדה שעון אמיתי, לא
        /// למשך-זמן) גם השעות מוגבלות ל-00-23.
        /// </summary>
        private static bool IsValidDigitForPosition(TextBox tb, int pos, char digit)
        {
            bool isMinutesTensDigit = pos > 0 && tb.Text[pos - 1] == ':';
            if (isMinutesTensDigit)
            {
                return digit <= '5';
            }

            int colonIndex = tb.Text.IndexOf(':');
            bool isTwentyFourHourClockField = colonIndex == 2;

            if (isTwentyFourHourClockField)
            {
                if (pos == 0)
                {
                    return digit <= '2';
                }

                if (pos == 1 && tb.Text[0] == '2')
                {
                    return digit <= '3';
                }
            }

            return true;
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            // לא מאפשרים הדבקה חופשית - הייתה עלולה לשבור את תקינות המסכה
            // (למשל להכניס תווים לא-מספריים, או למחוק את ה-':').
            e.CancelCommand();
        }

        /// <summary>אינדקס הספרה שנבחרה/ממוקמת כרגע (מדלג אוטומטית מעל ':') - או -1 אם אין שדה תקין.</summary>
        private static int CurrentDigitIndex(TextBox tb)
        {
            if (tb.Text.Length == 0)
            {
                return -1;
            }

            int pos = Math.Clamp(tb.SelectionStart, 0, tb.Text.Length - 1);
            if (tb.Text[pos] == ':')
            {
                pos = pos + 1 < tb.Text.Length ? pos + 1 : pos - 1;
            }

            return pos >= 0 && pos < tb.Text.Length && tb.Text[pos] != ':' ? pos : -1;
        }

        private static void ReplaceDigit(TextBox tb, int index, char digit)
        {
            char[] chars = tb.Text.ToCharArray();
            chars[index] = digit;
            tb.Text = new string(chars);
            tb.Select(index, 1);
        }

        private static void AdvanceToNextDigit(TextBox tb, int fromIndex)
        {
            int next = fromIndex + 1;
            if (next < tb.Text.Length && tb.Text[next] == ':')
            {
                next += 1;
            }

            tb.Select(next < tb.Text.Length ? next : fromIndex, 1);
        }

        private static void SelectDigitNear(TextBox tb, int index)
        {
            if (tb.Text.Length == 0)
            {
                return;
            }

            int pos = Math.Clamp(index, 0, tb.Text.Length - 1);
            if (tb.Text[pos] == ':')
            {
                pos = pos + 1 < tb.Text.Length ? pos + 1 : pos - 1;
            }

            tb.Select(Math.Max(pos, 0), 1);
        }
    }
}
