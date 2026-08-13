; *** Inno Setup version 6.X Hebrew messages ***
;
; קובץ שפה עברי (RTL מלא) עבור Inno Setup 6 - נכתב עבור מעטפת ההתקנה של
; "תאריכון". יש לשמור קובץ זה בקידוד UTF-8 (בלי BOM או עם BOM - שתיהן
; נתמכות ע"י Inno Setup 6 המודרני/Unicode).
;
; להערות/עדכונים עתידיים: קובץ זה מכסה את כל מזהי ההודעות הסטנדרטיים
; שקובץ Default.isl האנגלי המקורי של Inno Setup דורש. אם גרסה עתידית של
; Inno Setup תוסיף מזהים חדשים, ה-Compiler יתריע על כך בזמן קימפול
; ("Missing message: ...") - במקרה כזה יש להוסיף את המזהה החסר כאן.
;
; קובץ זה משמש הן את תוכנית ההתקנה (Setup.exe) והן, באופן אוטומטי, את
; תוכנית ההסרה (Uninstall.exe) שנוצרת ממנה - שתיהן ישתמשו באותה שפה
; עברית מלאה עם יישור RTL אמיתי (RightToLeft=yes למטה).

[LangOptions]
LanguageName=<05E2><05D1><05E8><05D9><05EA>
LanguageID=$040D
LanguageCodePage=0
DialogFontName=Segoe UI
DialogFontSize=9
WelcomeFontName=Segoe UI
WelcomeFontSize=12
RightToLeft=yes

[Messages]

; *** Application titles
SetupAppTitle=התקנה
SetupWindowTitle=‫התקנת %1‬
UninstallAppTitle=הסרת התקנה
UninstallAppFullTitle=‫הסרת התקנה של %1‬

; *** Misc. common
InformationTitle=מידע
ConfirmTitle=אישור
ErrorTitle=שגיאה

; *** SetupLdr messages
SetupLdrStartupMessage=‫תוכנית זו תתקין את %1. האם ברצונך להמשיך?‬
LdrCannotCreateTemp=לא ניתן ליצור קובץ זמני. ההתקנה בוטלה
LdrCannotExecTemp=לא ניתן להפעיל קובץ בתיקייה הזמנית. ההתקנה בוטלה
HelpTextNote=

; *** Startup error messages
LastErrorMessage=‎%1‎.%n%nשגיאה ‎%2‎: ‎%3‎
SetupFileMissing=הקובץ ‎%1‎ חסר בתיקיית ההתקנה. יש לתקן את הבעיה או להשיג עותק חדש של התוכנה.
SetupFileCorrupt=קבצי ההתקנה פגומים. יש להשיג עותק חדש של התוכנה.
SetupFileCorruptOrWrongVer=קבצי ההתקנה פגומים, או שאינם תואמים לגרסה זו של תוכנית ההתקנה. יש לתקן את הבעיה או להשיג עותק חדש של התוכנה.
InvalidParameter=הועבר פרמטר שגוי בשורת הפקודה:%n%n‎%1‎
SetupAlreadyRunning=תוכנית ההתקנה כבר פועלת.
WindowsVersionNotSupported=תוכנה זו אינה תומכת בגרסת ה-Windows המותקנת במחשב זה.
WindowsServicePackRequired=תוכנה זו דורשת ‎%1‎ Service Pack ‎%2‎ ומעלה.
NotOnThisPlatform=תוכנה זו לא תפעל תחת ‎%1‎.
OnlyOnThisPlatform=תוכנה זו חייבת לפעול תחת ‎%1‎.
OnlyOnTheseArchitectures=תוכנה זו יכולה להיות מותקנת רק בגרסאות Windows המיועדות לארכיטקטורות המעבד הבאות:%n%n‎%1‎
WinVersionTooLowError=תוכנה זו דורשת ‎%1‎ גרסה ‎%2‎ ומעלה.
WinVersionTooHighError=לא ניתן להתקין תוכנה זו על ‎%1‎ גרסה ‎%2‎ ומעלה.
AdminPrivilegesRequired=יש להתחבר כמנהל מערכת (Administrator) בעת התקנת תוכנה זו.
PowerUserPrivilegesRequired=יש להתחבר כמנהל מערכת או כחבר בקבוצת "משתמשי-על" (Power Users) בעת התקנת תוכנה זו.
SetupAppRunningError=תוכנית ההתקנה זיהתה כי ‎%1‎ פועלת כרגע.%n%nיש לסגור את כל המופעים שלה כעת, ולאחר מכן ללחוץ על "אישור" כדי להמשיך, או על "ביטול" כדי לצאת.
UninstallAppRunningError=תוכנית ההסרה זיהתה כי ‎%1‎ פועלת כרגע.%n%nיש לסגור את כל המופעים שלה כעת, ולאחר מכן ללחוץ על "אישור" כדי להמשיך, או על "ביטול" כדי לצאת.

; *** Startup questions
PrivilegesRequiredOverrideTitle=בחירת מצב התקנה
PrivilegesRequiredOverrideInstruction=בחירת מצב התקנה
PrivilegesRequiredOverrideText1=ניתן להתקין את ‎%1‎ עבור כל המשתמשים במחשב זה (נדרשות הרשאות מנהל מערכת), או רק עבור המשתמש הנוכחי בלבד.
PrivilegesRequiredOverrideText2=ניתן להתקין את ‎%1‎ רק עבור המשתמש הנוכחי בלבד, או עבור כל המשתמשים במחשב זה (נדרשות הרשאות מנהל מערכת).
PrivilegesRequiredOverrideAllUsers=התקנה עבור &כל המשתמשים
PrivilegesRequiredOverrideAllUsersRecommended=התקנה עבור &כל המשתמשים (מומלץ)
PrivilegesRequiredOverrideCurrentUser=התקנה עבור המשתמש ה&נוכחי בלבד
PrivilegesRequiredOverrideCurrentUserRecommended=התקנה עבור המשתמש ה&נוכחי בלבד (מומלץ)

; *** Misc. errors
ErrorCreatingDir=תוכנית ההתקנה לא הצליחה ליצור את התיקייה "‎%1‎"
ErrorTooManyFilesInDir=לא ניתן ליצור קובץ בתיקייה "‎%1‎" משום שהיא מכילה יותר מדי קבצים

; *** Setup common messages
ExitSetupTitle=יציאה מתוכנית ההתקנה
ExitSetupMessage=ההתקנה טרם הושלמה. אם תצא כעת, התוכנה לא תותקן.%n%nניתן להריץ את תוכנית ההתקנה שוב במועד מאוחר יותר כדי להשלים את ההתקנה.%n%nלצאת מתוכנית ההתקנה?
AboutSetupMenuItem=&אודות תוכנית ההתקנה...
AboutSetupTitle=אודות תוכנית ההתקנה
AboutSetupMessage=‎%1‎ גרסה ‎%2‎%n‎%3‎%n%n‎%1‎ דף בית:%n‎%4‎
AboutSetupNote=
TranslatorNote=

; *** Buttons
ButtonBack=< &הקודם
ButtonNext=&הבא >
ButtonInstall=&התקן
ButtonOK=אישור
ButtonCancel=ביטול
ButtonYes=&כן
ButtonYesToAll=כן ל&כל
ButtonNo=&לא
ButtonNoToAll=לא לכ&ל
ButtonFinish=&סיום
ButtonBrowse=&עיון...
ButtonWizardBrowse=ע&יון...
ButtonNewFolder=יצירת תיקייה חדשה

; *** "Select Language" dialog messages
SelectLanguageTitle=בחירת שפת ההתקנה
SelectLanguageLabel=יש לבחור את השפה בה יוצג תהליך ההתקנה:

; *** Common wizard text
ClickNext=יש ללחוץ על "הבא" כדי להמשיך, או על "ביטול" כדי לצאת מתוכנית ההתקנה.
BeveledLabel=
BrowseDialogTitle=עיון אחר תיקייה
BrowseDialogLabel=יש לבחור תיקייה מהרשימה שלהלן, ולאחר מכן ללחוץ על "אישור".
NewFolderName=תיקייה חדשה

; *** "Welcome" wizard page
WelcomeLabel1=ברוך הבא להתקנת [name]
WelcomeLabel2=תוכנית זו תתקין את [name/ver] במחשב זה.%n%nמומלץ לסגור את כל התוכניות האחרות הפועלות כרגע לפני ההמשך.

; *** "Password" wizard page
WizardPassword=סיסמה
PasswordLabel1=התקנה זו מוגנת בסיסמה.
PasswordLabel3=יש להזין את הסיסמה, ולאחר מכן ללחוץ על "הבא" כדי להמשיך. הקפד על רישיות (אותיות גדולות/קטנות) נכונה.
PasswordEditLabel=&סיסמה:
IncorrectPassword=הסיסמה שהוזנה אינה נכונה. יש לנסות שוב.

; *** "License Agreement" wizard page
WizardLicense=הסכם רישיון שימוש
LicenseLabel=נא לקרוא את המידע החשוב הבא לפני ההמשך.
LicenseLabel3=נא לקרוא את הסכם הרישיון הבא. יש לאשר את תנאי הסכם זה לפני שניתן יהיה להמשיך בהתקנה.
LicenseAccepted=&אני מסכים לתנאי ההסכם
LicenseNotAccepted=&אינני מסכים לתנאי ההסכם

; *** "Information" wizard pages
WizardInfoBefore=מידע
InfoBeforeLabel=נא לקרוא את המידע החשוב הבא לפני ההמשך.
InfoBeforeClickLabel=כאשר אתה מוכן להמשיך בהתקנה, יש ללחוץ על "הבא".
WizardInfoAfter=מידע
InfoAfterLabel=נא לקרוא את המידע החשוב הבא לפני ההמשך.
InfoAfterClickLabel=כאשר אתה מוכן להמשיך, יש ללחוץ על "הבא".

; *** "User Information" wizard page
WizardUserInfo=פרטי משתמש
UserInfoDesc=נא להזין את הפרטים שלך.
UserInfoName=&שם משתמש:
UserInfoOrg=&ארגון:
UserInfoSerial=&מספר סידורי:
UserInfoNameRequired=יש להזין שם.

; *** "Select Destination Location" wizard page
WizardSelectDir=בחירת תיקיית היעד
SelectDirDesc=היכן להתקין את ‎תאריכון‎?
SelectDirLabel3=תוכנית ההתקנה תתקין את ‎תאריכון בתיקייה הבאה.
SelectDirBrowseLabel=כדי להמשיך, יש ללחוץ על "הבא". אם ברצונך לבחור תיקייה אחרת, יש ללחוץ על "עיון".
DiskSpaceGBLabel=נדרש שטח פנוי של לפחות [gb] ג'יגה-בייט בכונן.
DiskSpaceMBLabel=נדרש שטח פנוי של לפחות [mb] מגה-בייט בכונן.
CannotInstallToNetworkDrive=לא ניתן להתקין לכונן רשת.
CannotInstallToUNCPath=לא ניתן להתקין לנתיב UNC.
InvalidPath=יש להזין נתיב מלא, כולל אות הכונן; לדוגמה:%n%nC:\APP%n%nאו נתיב UNC בתבנית:%n%n\\server\share
InvalidDrive=הכונן או שיתוף ה-UNC שנבחרו אינם קיימים, או שאינם נגישים. יש לבחור נתיב אחר.
DiskSpaceWarningTitle=אין מספיק שטח פנוי בכונן
DiskSpaceWarning=תוכנית ההתקנה דורשת לפחות ‎%1‎ ק"ב של שטח פנוי כדי להתקין, אך בכונן שנבחר יש רק ‎%2‎ ק"ב זמינים.%n%nהאם ברצונך להמשיך בכל זאת?
DirNameTooLong=שם התיקייה או הנתיב ארוך מדי.
InvalidDirName=שם התיקייה אינו תקין.
BadDirName32=שמות תיקיות אינם יכולים לכלול אף אחד מהתווים הבאים:%n%n‎%1‎
DirExistsTitle=התיקייה קיימת
DirExists=התיקייה:%n%n‎%1‎%n%nכבר קיימת. האם ברצונך להתקין לתוך תיקייה זו בכל מקרה?
DirDoesntExistTitle=התיקייה אינה קיימת
DirDoesntExist=התיקייה:%n%n‎%1‎%n%nאינה קיימת. האם ברצונך שהתיקייה תיווצר?

; *** "Select Components" wizard page
WizardSelectComponents=בחירת רכיבים
SelectComponentsDesc=אילו רכיבים ברצונך להתקין?
SelectComponentsLabel2=יש לבחור את הרכיבים שברצונך להתקין; יש לבטל את הסימון של רכיבים שאינך מעוניין להתקין. לאחר מכן יש ללחוץ על "הבא" כדי להמשיך.
FullInstallation=התקנה מלאה
CompactInstallation=התקנה מצומצמת
CustomInstallation=התקנה מותאמת אישית
NoUninstallWarningTitle=רכיבים קיימים
NoUninstallWarning=תוכנית ההתקנה זיהתה כי הרכיבים הבאים כבר מותקנים במחשב זה:%n%n‎%1‎%n%nביטול הסימון של רכיבים אלה לא יסיר אותם.%n%nהאם להמשיך בכל זאת?
ComponentSize1=‎%1‎ ק"ב
ComponentSize2=‎%1‎ מ"ב
ComponentsDiskSpaceGBLabel=הבחירה הנוכחית דורשת לפחות [gb] ג'יגה-בייט בכונן.
ComponentsDiskSpaceMBLabel=הבחירה הנוכחית דורשת לפחות [mb] מגה-בייט בכונן.

; *** "Select Additional Tasks" wizard page
WizardSelectTasks=בחירת פעולות נוספות
SelectTasksDesc=אילו פעולות נוספות לבצע?
SelectTasksLabel2=יש לבחור את הפעולות הנוספות שברצונך שתוכנית ההתקנה תבצע בעת התקנת ‎תאריכון, ולאחר מכן ללחוץ על "הבא".

; *** "Select Start Menu Folder" wizard page
WizardSelectProgramGroup=בחירת תיקיית תפריט התחל
SelectStartMenuFolderDesc=היכן על תוכנית ההתקנה להציב את קיצורי הדרך של התוכנית?
SelectStartMenuFolderLabel3=תוכנית ההתקנה תיצור את קיצורי הדרך לתוכנית בתיקיית תפריט ההתחלה הבאה.
SelectStartMenuFolderBrowseLabel=כדי להמשיך, יש ללחוץ על "הבא". אם ברצונך לבחור תיקייה אחרת, יש ללחוץ על "עיון".
MustEnterGroupName=יש להזין שם תיקייה.
GroupNameTooLong=שם התיקייה או הנתיב ארוך מדי.
InvalidGroupName=שם התיקייה אינו תקין.
BadGroupName=שם התיקייה אינו יכול לכלול אף אחד מהתווים הבאים:%n%n‎%1‎
NoProgramGroupCheck2=&לא ליצור תיקייה בתפריט התחל

; *** "Ready to Install" wizard page
WizardReady=מוכן להתקנה
ReadyLabel1=תוכנית ההתקנה מוכנה כעת להתחיל בהתקנת ‎תאריכון‎ במחשב זה.
ReadyLabel2a=יש ללחוץ על "התקן" כדי להמשיך בהתקנה, או על "הקודם" אם ברצונך לעיין בהגדרות שוב או לשנותן.
ReadyLabel2b=יש ללחוץ על "התקן" כדי להמשיך בהתקנה.
ReadyMemoUserInfo=פרטי משתמש:
ReadyMemoDir=תיקיית יעד:
ReadyMemoType=סוג התקנה:
ReadyMemoComponents=רכיבים נבחרים:
ReadyMemoGroup=תיקיית תפריט התחל:
ReadyMemoTasks=פעולות נוספות:

; *** TDownloadWizardPage wizard page and DownloadTemporaryFile
ButtonStopDownload=&עצירת ההורדה
StopDownload=האם אכן לעצור את ההורדה?
ErrorDownloadaborted=ההורדה בוטלה
ErrorDownloadFailed=ההורדה נכשלה: ‎%1‎ ‎%2‎
ErrorDownloadSizeFailed=קבלת גודל הקובץ נכשלה: ‎%1‎ ‎%2‎
ErrorProgress=התקדמות לא תקינה: ‎%1‎ מתוך ‎%2‎
ErrorFileSize=גודל קובץ לא תקין - צפוי: ‎%1‎, בפועל: ‎%2‎

; *** TExtractionWizardPage wizard page and ExtractArchive
ExtractingLabel=מחלץ קבצים נוספים...
ButtonStopExtraction=&עצירת החילוץ
StopExtraction=האם אכן לעצור את החילוץ?
ErrorExtractionAborted=החילוץ בוטל
ErrorExtractionFailed=החילוץ נכשל: ‎%1‎

; *** Archive extraction failure details
ArchiveIncorrectPassword=הסיסמה אינה נכונה
ArchiveIsCorrupted=הארכיון פגום
ArchiveUnsupportedFormat=פורמט הארכיון אינו נתמך

; *** "Preparing to Install" wizard page
WizardPreparing=מכין להתקנה
PreparingDesc=תוכנית ההתקנה מכינה כעת את התקנת תאריכון במחשב זה.
PreviousInstallNotCompleted=התקנה/הסרה קודמת של תוכנה לא הושלמה. יש להפעיל מחדש את המחשב כדי להשלים התקנה זו.%n%nלאחר הפעלה מחדש של המחשב, יש להריץ שוב את תוכנית ההתקנה כדי להשלים את התקנת ‎%1‎.
CannotContinue=לא ניתן להמשיך בהתקנה. יש ללחוץ על "ביטול" כדי לצאת.
ApplicationsFound=היישומים הבאים משתמשים בקבצים שתוכנית ההתקנה צריכה לעדכן. מומלץ לאפשר לתוכנית ההתקנה לסגור יישומים אלה באופן אוטומטי.
ApplicationsFound2=היישומים הבאים משתמשים בקבצים שתוכנית ההתקנה צריכה לעדכן. מומלץ לאפשר לתוכנית ההתקנה לסגור יישומים אלה באופן אוטומטי. בתום ההתקנה, תוכנית ההתקנה תנסה להפעיל מחדש את היישומים.
CloseApplications=&סגירה אוטומטית של היישומים
DontCloseApplications=&אל תסגור את היישומים
ErrorCloseApplications=תוכנית ההתקנה לא הצליחה לסגור את כל היישומים באופן אוטומטי. מומלץ לסגור את כל היישומים המשתמשים בקבצים שיש לעדכן לפני ההמשך.
PrepareToInstallNeedsRestart=יש להפעיל מחדש את המחשב. לאחר ההפעלה מחדש, יש להריץ שוב את תוכנית ההתקנה כדי להשלים את התקנת ‎%1‎.%n%nהאם להפעיל מחדש כעת?

; *** "Installing" wizard page
WizardInstalling=מתקין
InstallingLabel=נא להמתין בזמן שהתוכנה ‎תאריכון מותקנת במחשב זה.

; *** "Setup Completed" wizard page
FinishedHeadingLabel=משלים את אשף ההתקנה של [name]
FinishedLabelNoIcons=ההתקנה של [name] הושלמה במחשב זה.
FinishedLabel=ההתקנה של [name] הושלמה במחשב זה. ניתן להפעיל את היישום באמצעות קיצורי הדרך שהותקנו.
ClickFinish=יש ללחוץ על "סיום" כדי לצאת מתוכנית ההתקנה.
FinishedRestartLabel=כדי להשלים את התקנת [name], יש להפעיל מחדש את המחשב. האם להפעיל מחדש כעת?
FinishedRestartMessage=כדי להשלים את התקנת [name], יש להפעיל מחדש את המחשב.%n%nהאם להפעיל מחדש כעת?
ShowReadmeCheck=כן, ברצוני להציג את קובץ ה-README
YesRadio=&כן, הפעל מחדש את המחשב כעת
NoRadio=&לא, אפעיל מחדש את המחשב מאוחר יותר
RunEntryExec=הפעלת ‎%1‎
RunEntryShellExec=הצגת ‎%1‎

; *** "Setup Needs the Next Disk" stuff
ChangeDiskTitle=תוכנית ההתקנה זקוקה לתקליטור/כרך הבא
SelectDiskLabel2=יש להכניס כעת את כרך ‎%1‎ וללחוץ על "אישור".%n%nאם הקבצים בכרך זה נמצאים בתיקייה שאינה זו המוצגת להלן, יש להזין את הנתיב הנכון או ללחוץ על "עיון".
PathLabel=&נתיב:
FileNotInDir2=הקובץ "‎%1‎" לא נמצא בתיקייה "‎%2‎". יש להכניס את הכרך הנכון, או לבחור תיקייה אחרת.
SelectDirectoryLabel=נא לציין את מיקום הכרך הבא.

; *** Installation phase messages
SetupAborted=ההתקנה לא הושלמה.%n%nיש לתקן את הבעיה ולהריץ שוב את תוכנית ההתקנה.
AbortRetryIgnoreSelectAction=נא לבחור פעולה
AbortRetryIgnoreRetry=&ניסיון חוזר
AbortRetryIgnoreIgnore=&התעלמות מהשגיאה והמשך
AbortRetryIgnoreCancel=ביטול ההתקנה

; *** Installation status messages
StatusClosingApplications=סוגר יישומים...
StatusCreateDirs=יוצר תיקיות...
StatusExtractFiles=מחלץ קבצים...
StatusCreateIcons=יוצר קיצורי דרך...
StatusCreateIniEntries=יוצר רשומות INI...
StatusCreateRegistryEntries=יוצר רשומות רישום (Registry)...
StatusRegisterFiles=רושם קבצים...
StatusSavingUninstall=שומר מידע להסרת ההתקנה...
StatusRunProgram=משלים את ההתקנה...
StatusRestartingApplications=מפעיל יישומים מחדש...
StatusRollback=מבטל שינויים...

; *** Misc. errors
ErrorInternal2=שגיאה פנימית: ‎%1‎
ErrorFunctionFailedNoCode=‎%1‎ נכשל
ErrorFunctionFailed=‎%1‎ נכשל; קוד ‎%2‎
ErrorFunctionFailedWithMessage=‎%1‎ נכשל; קוד ‎%2‎.%n‎%3‎
ErrorExecutingProgram=לא ניתן להפעיל את הקובץ:%n‎%1‎

; *** Registry errors
ErrorRegOpenKey=שגיאה בפתיחת מפתח רישום:%n‎%1‎\‎%2‎
ErrorRegCreateKey=שגיאה ביצירת מפתח רישום:%n‎%1‎\‎%2‎
ErrorRegWriteKey=שגיאה בכתיבה למפתח רישום:%n‎%1‎\‎%2‎

; *** INI errors
ErrorIniEntry=שגיאה ביצירת רשומת INI בקובץ "‎%1‎".

; *** File copying errors
FileAbortRetryIgnoreSkipNotRecommended=&דילוג על קובץ זה (לא מומלץ)
FileAbortRetryIgnoreIgnoreNotRecommended=&התעלמות מהשגיאה והמשך (לא מומלץ)
SourceIsCorrupted=קובץ המקור פגום
SourceDoesntExist=קובץ המקור "‎%1‎" אינו קיים
ExistingFileReadOnly2=לא ניתן להחליף את הקובץ הקיים משום שהוא מסומן כקריאה בלבד (Read-Only).
ExistingFileReadOnlyRetry=&הסרת המאפיין "קריאה בלבד" וניסיון חוזר
ExistingFileReadOnlyKeepExisting=&שמירת הקובץ הקיים
ErrorReadingExistingDest=אירעה שגיאה בעת ניסיון לקרוא את הקובץ הקיים:
FileExistsSelectAction=נא לבחור פעולה
FileExists2=הקובץ כבר קיים.
FileExistsOverwriteExisting=&שכתוב על הקובץ הקיים
FileExistsKeepExisting=&שמירת הקובץ הקיים
FileExistsOverwriteOrKeepAll=&ביצוע פעולה זו עבור כל ההתנגשויות הבאות
ExistingFileNewerSelectAction=נא לבחור פעולה
ExistingFileNewer2=הקובץ הקיים חדש יותר מהקובץ שתוכנית ההתקנה מנסה להתקין.
ExistingFileNewerOverwriteExisting=&שכתוב על הקובץ הקיים
ExistingFileNewerKeepExisting=&שמירת הקובץ הקיים (מומלץ)
ExistingFileNewerOverwriteOrKeepAll=&ביצוע פעולה זו עבור כל ההתנגשויות הבאות
ErrorChangingAttr=אירעה שגיאה בעת ניסיון לשנות את מאפייני הקובץ הקיים:
ErrorCreatingTemp=אירעה שגיאה בעת ניסיון ליצור קובץ בתיקיית היעד:
ErrorReadingSource=אירעה שגיאה בעת ניסיון לקרוא את קובץ המקור:
ErrorCopying=אירעה שגיאה בעת ניסיון להעתיק קובץ:
ErrorReplacingExistingFile=אירעה שגיאה בעת ניסיון להחליף את הקובץ הקיים:
ErrorRestartReplace=הפעלה מחדש-והחלפה (RestartReplace) נכשלה:
ErrorRenamingTemp=אירעה שגיאה בעת ניסיון לשנות שם קובץ בתיקיית היעד:
ErrorRegisterServer=לא ניתן לרשום את ה-DLL/OCX: ‎%1‎
ErrorRegSvr32Failed=RegSvr32 נכשל עם קוד יציאה ‎%1‎
ErrorRegisterTypeLib=לא ניתן לרשום את ספריית סוגי הנתונים (Type Library): ‎%1‎

; *** Post-install errors
ErrorOpeningReadme=אירעה שגיאה בעת ניסיון לפתוח את קובץ ה-README.
ErrorRestartingComputer=תוכנית ההתקנה לא הצליחה להפעיל מחדש את המחשב. יש לעשות זאת ידנית.

; *** Uninstaller messages
UninstallNotFound=הקובץ "‎%1‎" אינו קיים. לא ניתן להסיר את ההתקנה.
UninstallOpenError=לא ניתן לפתוח את הקובץ "‎%1‎". לא ניתן להסיר את ההתקנה
UninstallUnsupportedVer=קובץ יומן ההסרה "‎%1‎" הוא בפורמט שאינו מזוהה על-ידי גרסה זו של תוכנית ההסרה. לא ניתן להסיר את ההתקנה
UninstallUnknownEntry=נתקלה רשומה לא-מזוהה (‎%1‎) ביומן ההסרה
ConfirmUninstall=האם ברצונך להסיר לחלוטין את ‎%1‎ ואת כל הרכיבים הנלווים לו?
UninstallOnlyOnWin64=ניתן להסיר התקנה זו רק על Windows 64-סיביות.
OnlyAdminCanUninstall=ניתן להסיר התקנה זו רק על-ידי משתמש בעל הרשאות מנהל מערכת.
UninstallStatusLabel=נא להמתין בזמן ש-‎%1‎ מוסר מהמחשב.
UninstalledAll=‎%1‎ הוסר בהצלחה מהמחשב.
UninstalledMost=הסרת ‎%1‎ הושלמה.%n%nלא ניתן היה להסיר חלק מהפריטים. ניתן להסירם ידנית.
UninstalledAndNeedsRestart=כדי להשלים את הסרת ‎%1‎, יש להפעיל מחדש את המחשב.%n%nהאם להפעיל מחדש כעת?
UninstallDataCorrupted=הקובץ "‎%1‎" פגום. לא ניתן להסיר את ההתקנה

; *** Uninstallation phase messages
ConfirmDeleteSharedFileTitle=האם להסיר קובץ משותף?
ConfirmDeleteSharedFile2=המערכת מציינת כי הקובץ המשותף הבא כבר אינו בשימוש על-ידי אף תוכנית אחרת. האם ברצונך להסיר קובץ משותף זה?%n%nאם ישנן תוכניות אחרות שעדיין משתמשות בקובץ זה והוא יוסר, ייתכן שתוכניות אלה לא יפעלו כראוי. אם אינך בטוח, יש לבחור "לא". השארת הקובץ במערכת לא תגרום כל נזק.
SharedFileNameLabel=שם קובץ:
SharedFileLocationLabel=מיקום:
WizardUninstalling=מצב הסרת ההתקנה
StatusUninstalling=מסיר את ‎%1‎...

; *** Shutdown block reasons
ShutdownBlockReasonInstallingApp=מתקין את ‎%1‎.
ShutdownBlockReasonUninstallingApp=מסיר את ‎%1‎.

; The custom messages below aren't used by Setup itself, but if you make
; use of them in your scripts, you'll want to translate them.

[CustomMessages]

NameAndVersion=‎%1‎ גרסה ‎%2‎
AdditionalIcons=קיצורי דרך נוספים:
CreateDesktopIcon=יצירת קיצור דרך על &שולחן העבודה
CreateQuickLaunchIcon=יצירת קיצור דרך בסרגל ה&הפעלה המהירה
ProgramOnTheWeb=‎%1‎ באינטרנט
UninstallProgram=הסרת ‎%1‎
LaunchProgram=הפעלת ‎%1‎
AssocFileExtension=&שיוך ‎%1‎ לסיומת הקובץ ‎%2‎
AssocingFileExtension=משייך את ‎%1‎ לסיומת הקובץ ‎%2‎...
AutoStartProgramGroupDescription=הפעלה אוטומטית:
AutoStartProgram=הפעלה אוטומטית של ‎%1‎
AddonHostProgramNotFound=לא ניתן היה לאתר את ‎%1‎ בתיקייה שנבחרה.%n%nהאם ברצונך להמשיך בכל זאת?

; *** הודעות מותאמות-אישית עבור תוכנית ההתקנה של "תאריכון"
LaunchAtStartupTaskDescription=הפעלת תאריכון אוטומטית עם עליית Windows
DotNetCheckingLabel=בודק אם .NET Desktop Runtime 8 מותקן במחשב...
DotNetMissingLabel=לא נמצא .NET Desktop Runtime 8 (נדרש להרצת התוכנה).
DotNetMissingConfirmLabel=לא נמצא במחשב זה .NET Desktop Runtime 8, הנדרש כדי להריץ את תאריכון. האם להוריד ולהתקין אותו כעת (נדרש חיבור אינטרנט)?%nאם תבחר "לא", ההתקנה תמשיך, אך ייתכן שהתוכנה לא תפעל כראוי עד שתתקין אותו ידנית.
DotNetDownloadingLabel=מוריד את .NET Desktop Runtime 8 הנדרש...
DotNetInstallingLabel=מתקין את .NET Desktop Runtime 8 (אנא המתן, פעולה זו עשויה להימשך מספר דקות)...
DotNetInstallFailedLabel=התקנת .NET Desktop Runtime 8 נכשלה. ניתן להתקין אותו ידנית מהכתובת:%nhttps://dotnet.microsoft.com/download/dotnet/8.0%n%nההתקנה של תאריכון תמשיך, אך ייתכן שהתוכנה לא תפעל כראוי עד להתקנת ה-Runtime.
DotNetDownloadFailedLabel=הורדת .NET Desktop Runtime 8 נכשלה. יש לוודא שקיים חיבור אינטרנט תקין, או להתקין אותו ידנית מהכתובת:%nhttps://dotnet.microsoft.com/download/dotnet/8.0%n%nההתקנה של תאריכון תמשיך, אך ייתכן שהתוכנה לא תפעל כראוי עד להתקנת ה-Runtime.
DotNetAlreadyInstalledLabel=.NET Desktop Runtime 8 כבר מותקן - מדלג על שלב זה.
