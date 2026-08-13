; =============================================================================
;  Tarichon-Setup.iss
;  סקריפט Inno Setup להתקנת "תאריכון" (Tarichon) - וידג'ט תאריך עברי לשורת
;  המשימות של Windows, יחד עם כלי הגישה העצמאי להגדרות ("הגדרות תאריכון").
;
;  לפני קימפול (ראו גם המדריך המצורף Guide-Installer.md):
;    1. יש לפרסם (dotnet publish) את שני הפרוייקטים במצב Release, ולהניח
;       את הקבצים בתיקיות המקומיות המתאימות (ראו טבלה במדריך):
;         App-Files\              <- כל תוכן הפרסום של HebrewTaskbarWidget
;         SettingsRecovery-Files\ <- רק הקובץ HebrewTaskbarWidgetSettings.exe
;    2. יש להחליט על מצב הכללת .NET Desktop Runtime (מקוון/לא-מקוון) -
;       ראו #define IncludeDotNetRuntime למטה, ואת ההסבר המלא במדריך.
;    3. יש להשלים את פרטי המפרסם/זכויות היוצרים בסעיף ההגדרות למטה,
;       ובקובץ License\License-he.txt.
;    4. פתיחת קובץ זה ב-Inno Setup Compiler (או הרצת ISCC.exe עליו) תיצור
;       את קובץ ההתקנה הסופי בתיקיית Output\.
; =============================================================================

; --- הגדרות בסיסיות של התוכנה - לעדכן לפי הצורך ---
#define MyAppName "תאריכון"
#define MyAppNameEn "Tarichon"
#define MyAppVersion "0.5.1"
#define MyAppPublisher "ישראל אמיתי"
#define MyAppURL "https://github.com/Ani-yakhol/Tarichon"
#define MyAppExeName "HebrewTaskbarWidget.exe"
#define MySettingsExeName "HebrewTaskbarWidgetSettings.exe"
#define MySettingsDisplayName "הגדרות תאריכון"

; מזהה ייחודי וקבוע לתוכנה - נוצר פעם אחת ולעולם לא משתנה בגרסאות עתידיות
; (זהו מה שמאפשר לתוכנית ההתקנה לזהות "שדרוג גרסה" ולא "תוכנה חדשה").
#define MyAppId "{A6E2C9F3-8B7D-4E5A-9C1F-2D4B6A8E0F3C}"

; --- שם ערך ה-Run key שהתוכנה עצמה משתמשת בו להפעלה אוטומטית עם Windows
;     (ראו Services/StartupService.cs) - **חייב** להישאר זהה בדיוק, אחרת
;     הסנכרון בין ההתקנה לבין הגדרות התוכנה לא יעבוד. ---
#define StartupRunValueName "HebrewTaskbarWidget"
#define StartupArgument "--autostart"

; --- נתיבי הקבצים המקומפלים - תיקיות מקומיות בתוך תיקיית ההתקנה עצמה.
;     יש להניח בהן את תוצאות ה-dotnet publish (ראו המדריך). ---
#define MainFilesDir "App-Files"
#define SettingsFilesDir "SettingsRecovery-Files"

; --- מצב הכללת .NET Desktop Runtime 8 x64 ---
; שימו לב: **לא מספיק** להניח את קובץ המתקין בתיקיית Redist - יש **גם**
; לשנות את הערך למטה ל-true, אחרת הוא לא ייכלל בקובץ ההתקנה בכלל (וזה
; יקרה בלי שום שגיאת קימפול - רק בשקט לא ייכלל)!
; true  = מכלילים את מתקין ה-Runtime בתוך קובץ ההתקנה עצמו (offline/לא-מקוון) -
;         יש להניח את הקובץ windowsdesktop-runtime-8.0-win-x64.exe בתיקיית
;         Redist לפני הקימפול (ראו המדריך - סעיף "שני מצבי הפצה") -
;         **וגם** לשנות את הערך הבא ל-true.
; false = לא מכלילים אותו כלל בקובץ ההתקנה (קובץ קטן יותר) - אם ה-Runtime
;         חסר במחשב היעד, המשתמש יישאל בזמן ההתקנה (עם אישור מפורש) אם
;         להוריד אותו אוטומטית מאתר מיקרוסופט (נדרש חיבור אינטרנט אז).
#define IncludeDotNetRuntime false

; שם קובץ מתקין ה-.NET Desktop Runtime (משמש בשני המצבים)
#define DotNetRuntimeFileName "windowsdesktop-runtime-8.0-win-x64.exe"

; קישור ההורדה הרשמי והקבוע של מיקרוסופט (aka.ms) ל-.NET Desktop Runtime 8 x64 -
; קישור זה תמיד מפנה לגרסה היציבה (Patch) העדכנית ביותר של Runtime 8.
#define DotNetRuntimeDownloadUrl "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"

; =============================================================================

[Setup]
AppId={{#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; --- רישוי (ראו License\License-he.txt - כולל גם קרדיט לרכיבי צד שלישי) ---
LicenseFile=License\License-he.txt
; --- הרשאות: התקנה עבור המשתמש הנוכחי בלבד, ללא צורך בהרשאות מנהל
;     ובלי לשאול (התקנה ל-{localappdata}\Programs, לא ל-Program Files) ---
PrivilegesRequired=lowest
; --- ארכיטקטורה: 64-סיביות בלבד, תואם ל-win-x64 שפורסם ---
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; --- קובץ הפלט הסופי ---
OutputDir=Output
OutputBaseFilename=Tarichon-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; --- אייקונים: ראו הסבר מלא במדריך (Guide-Installer.md) על ההבדל בין השניים ---
SetupIconFile=Icons\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardSmallImageFile=Icons\WizardSmall.png
; --- מטא-דאטה כללי ---
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=תוכנית התקנה עבור {#MyAppName}
ShowLanguageDialog=no
UninstallDisplayName={#MyAppName}
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "hebrew"; MessagesFile: "Languages\Hebrew.isl"

[Tasks]
; --- הפעלה אוטומטית עם עליית Windows - מסונכרנת עם הגדרת "הפעל את
;     התוכנה אוטומטית עם עליית Windows" בתוך התוכנה עצמה (ברירת המחדל
;     שם היא "מסומן", ולכן גם כאן) - ראו הסבר מלא בסעיף [Code] למטה. ---
Name: "launchatstartup"; Description: "{cm:LaunchAtStartupTaskDescription}"; GroupDescription: "אפשרויות הפעלה:"
; --- קיצורי דרך על שולחן העבודה - אופציונליים, לשני היישומים בנפרד ---
Name: "desktopicon_main"; Description: "יצירת קיצור דרך ל{#MyAppName} על שולחן העבודה"; GroupDescription: "קיצורי דרך נוספים:"
Name: "desktopicon_settings"; Description: "יצירת קיצור דרך ל{#MySettingsDisplayName} על שולחן העבודה"; GroupDescription: "קיצורי דרך נוספים:"

[Files]
; --- כל קבצי הפרסום (publish) של התוכנה הראשית, כולל תיקיית ההתראות הקוליות ---
Source: "{#MainFilesDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; --- קובץ ה-exe של כלי ההגדרות העצמאי בלבד - שאר הקבצים שלו (DLL-ים
;     משותפים) כבר הועתקו למעלה, כי שני הפרוייקטים חולקים את אותה תשתית ---
Source: "{#SettingsFilesDir}\{#MySettingsExeName}"; DestDir: "{app}"; Flags: ignoreversion

; --- מתקין ה-.NET Desktop Runtime, רק במצב "הכללה מקומית" (ראו IncludeDotNetRuntime למעלה) ---
#if IncludeDotNetRuntime
Source: "Redist\{#DotNetRuntimeFileName}"; DestDir: "{tmp}"; Flags: dontcopy
#endif

[Icons]
; --- תפריט התחל: תמיד נוצרים (לא אופציונליים) ---
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MySettingsDisplayName}"; Filename: "{app}\{#MySettingsExeName}"; IconFilename: "{app}\{#MySettingsExeName}"
Name: "{group}\הסרת התקנה"; Filename: "{uninstallexe}"

; --- שולחן העבודה: אופציונלי, לפי הסימון בעמוד "פעולות נוספות" ---
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon_main
Name: "{autodesktop}\{#MySettingsDisplayName}"; Filename: "{app}\{#MySettingsExeName}"; IconFilename: "{app}\{#MySettingsExeName}"; Tasks: desktopicon_settings

[Registry]
; --- רישום התוכנה ב-Registry (מעבר לרישום ההסרה האוטומטי הסטנדרטי של
;     Inno Setup) - HKA משמעו "אוטומטי": HKLM אם ההתקנה בוצעה עבור כל
;     המשתמשים, או HKCU אם עבור המשתמש הנוכחי בלבד (בהתאם לבחירה בעמוד
;     ההרשאות למעלה). ---
Root: HKA; Subkey: "Software\{#MyAppNameEn}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\{#MyAppNameEn}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"
Root: HKA; Subkey: "Software\{#MyAppNameEn}"; ValueType: string; ValueName: "DisplayName"; ValueData: "{#MyAppName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "הפעלת {#MyAppName} כעת"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; מנקה קבצי הגדרות/יומן שהתוכנה עצמה יוצרת בזמן ריצה (לא רק את קבצי ההתקנה)
Type: filesandordirs; Name: "{userappdata}\HebrewTaskbarWidget"

; =============================================================================
[Code]

// -----------------------------------------------------------------------
// בדיקה: האם מותקן .NET Desktop Runtime בגרסה 8.x (כלשהי) עבור x64?
// שיטת הבדיקה: קריאת מפתחות המשנה תחת מפתח הרישום שבו .NET Runtime עצמו
// רושם את כל הגרסאות המותקנות שלו (זו השיטה המתועדת/מומלצת ע"י מיקרוסופט
// לזיהוי תוכניתי של Runtime-ים מותקנים, ללא צורך בהרצת dotnet.exe חיצוני).
// -----------------------------------------------------------------------
// -----------------------------------------------------------------------
// בדיקה: האם מותקן .NET Desktop Runtime בגרסה 8.x (כלשהי) עבור x64?
// שלוש שיטות בדיקה עצמאיות, כל אחת "רשת ביטחון" לקודמת - כי נצפה בפועל
// (בין builds/גרסאות שונות של מתקין ה-.NET הרשמי) ששיטת הרישום המדוייקת
// לא תמיד עקבית: לפעמים הגרסאות המותקנות רשומות כתת-מפתחות (SubKeys),
// לפעמים כערכים (Value Names) תחת אותו מפתח, ולפעמים אף אחת מהשתיים לא
// עדכנית/נגישה - במקרה כזה בודקים ישירות בדיסק, בנתיב הקבוע שבו .NET
// תמיד מתקין Runtime-ים ברמת המחשב, ללא תלות ברישום בכלל.
// -----------------------------------------------------------------------
function IsDotNetDesktopRuntime8Installed(): Boolean;
var
  SubKeyNames: TArrayOfString;
  ValueNames: TArrayOfString;
  I: Integer;
  RegPath: string;
  SharedFxDir: string;
  FindRec: TFindRec;
begin
  Result := False;
  RegPath := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';

  // שיטה 1: תת-מפתחות תחת מפתח הרישום (הפורמט הנפוץ/המתועד)
  if RegGetSubkeyNames(HKLM64, RegPath, SubKeyNames) then
  begin
    for I := 0 to GetArrayLength(SubKeyNames) - 1 do
    begin
      if (Length(SubKeyNames[I]) > 0) and (SubKeyNames[I][1] = '8') then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;

  // שיטה 2: ערכים (Value Names, לא תת-מפתחות) תחת אותו מפתח - נצפה
  // כפורמט חלופי בחלק מגרסאות מתקין ה-.NET.
  if RegGetValueNames(HKLM64, RegPath, ValueNames) then
  begin
    for I := 0 to GetArrayLength(ValueNames) - 1 do
    begin
      if (Length(ValueNames[I]) > 0) and (ValueNames[I][1] = '8') then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;

  // שיטה 3 (רשת ביטחון סופית): בדיקה ישירה בדיסק - .NET Desktop Runtime
  // מותקן תמיד (בהתקנה ברמת המחשב, x64) לנתיב הקבוע הזה, לגמרי ללא תלות
  // ברישום. אם יש כאן תיקייה שמתחילה ב-"8." - ה-Runtime בהחלט מותקן.
  SharedFxDir := ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if DirExists(SharedFxDir) then
  begin
    if FindFirst(SharedFxDir + '\8.*', FindRec) then
    begin
      try
        Result := True;
      finally
        FindClose(FindRec);
      end;
    end;
  end;
end;

// -----------------------------------------------------------------------
// הרצת מתקין ה-.NET Desktop Runtime באופן שקט (בלי שום חלונית מטעמו) -
// עובד זהה עבור הקובץ המקומי (מצב "הכללה") וגם עבור הקובץ שהורד (מצב
// "הורדה"), כי בשני המקרים זהו אותו מתקין רשמי של מיקרוסופט.
// -----------------------------------------------------------------------
function RunDotNetRuntimeInstallerSilently(const InstallerPath: string): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec(InstallerPath, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if Result then
    Result := (ResultCode = 0) or (ResultCode = 3010); // 3010 = הצליח, אך נדרשת הפעלה מחדש
end;

#if IncludeDotNetRuntime
// --- מצב "הכללה מקומית": הקובץ כבר ארוז בתוך קובץ ההתקנה (עם dontcopy),
//     מחלצים אותו לתיקייה הזמנית ומריצים אותו משם. ---
procedure EnsureDotNetDesktopRuntime();
var
  LocalInstallerPath: string;
begin
  if IsDotNetDesktopRuntime8Installed() then
  begin
    Exit;
  end;

  ExtractTemporaryFile('{#DotNetRuntimeFileName}');
  LocalInstallerPath := ExpandConstant('{tmp}\{#DotNetRuntimeFileName}');

  if not RunDotNetRuntimeInstallerSilently(LocalInstallerPath) then
  begin
    MsgBox(ExpandConstant('{cm:DotNetInstallFailedLabel}'), mbError, MB_OK);
  end;
end;
#else
// --- מצב "הורדה": הקובץ אינו כלול כלל בקובץ ההתקנה - מורידים אותו
//     מהאתר הרשמי של מיקרוסופט (urlmon.dll, מובנה ב-Windows, בלי צורך
//     בתוסף/רכיב חיצוני נוסף ל-Inno Setup) ורק אז מריצים אותו. ---
function URLDownloadToFile(pCaller: Longint; szURL: AnsiString; szFileName: AnsiString;
  dwReserved: Longint; lpfnCB: Longint): Longint;
  external 'URLDownloadToFileA@urlmon.dll stdcall';

procedure EnsureDotNetDesktopRuntime();
var
  DownloadedPath: string;
  DownloadResult: Longint;
begin
  if IsDotNetDesktopRuntime8Installed() then
  begin
    Exit;
  end;

  // מצב "הורדה": ה-Runtime לא כלול בקובץ ההתקנה - לפני שמתחילים בהורדה
  // (שדורשת אינטרנט ולוקחת זמן), שואלים את המשתמש אישור מפורש, במקום
  // להוריד בשקט מבלי לשאול.
  if MsgBox(ExpandConstant('{cm:DotNetMissingConfirmLabel}'), mbConfirmation, MB_YESNO) = IDNO then
  begin
    Exit;
  end;

  WizardForm.StatusLabel.Caption := ExpandConstant('{cm:DotNetDownloadingLabel}');
  WizardForm.Refresh;

  DownloadedPath := ExpandConstant('{tmp}\{#DotNetRuntimeFileName}');
  DownloadResult := URLDownloadToFile(0, '{#DotNetRuntimeDownloadUrl}', DownloadedPath, 0, 0);

  if DownloadResult <> 0 then
  begin
    MsgBox(ExpandConstant('{cm:DotNetDownloadFailedLabel}'), mbError, MB_OK);
    Exit;
  end;

  WizardForm.StatusLabel.Caption := ExpandConstant('{cm:DotNetInstallingLabel}');
  WizardForm.Refresh;

  if not RunDotNetRuntimeInstallerSilently(DownloadedPath) then
  begin
    MsgBox(ExpandConstant('{cm:DotNetInstallFailedLabel}'), mbError, MB_OK);
  end;
end;
#endif

// -----------------------------------------------------------------------
// סנכרון "הפעלה אוטומטית עם Windows" עם הגדרות התוכנה עצמה
// -----------------------------------------------------------------------
// התוכנה עצמה (ראו Services/StartupService.cs) קובעת אם היא עולה עם
// Windows ע"י כתיבה/מחיקה של ערך בשם קבוע (StartupRunValueName למעלה,
// עם דגל "{#StartupArgument}" שמאפשר לה לזהות שהיא עלתה כחלק מעליית
// Windows עצמה) תחת מפתח ה-Run הרגיל של המשתמש הנוכחי. כדי שהבחירה כאן
// בהתקנה תהיה **זהה בדיוק** למה שהתוכנה עצמה הייתה כותבת/מוחקת אילו
// המשתמש היה משנה את ההגדרה הזו מתוך התוכנה - כותבים/מוחקים בדיוק את
// אותו ערך, באותו מפתח, באותו פורמט.
// -----------------------------------------------------------------------
procedure ApplyStartupTaskToRegistry();
var
  RunKeyPath: string;
  ExeCommand: string;
begin
  RunKeyPath := 'Software\Microsoft\Windows\CurrentVersion\Run';

  if WizardIsTaskSelected('launchatstartup') then
  begin
    ExeCommand := '"' + ExpandConstant('{app}\{#MyAppExeName}') + '" {#StartupArgument}';
    RegWriteStringValue(HKCU, RunKeyPath, '{#StartupRunValueName}', ExeCommand);
  end
  else
  begin
    // מוחקים גם אם לא היה קיים בכלל - כדי לכסות גם מקרה של התקנה חוזרת
    // (Repair/Upgrade) שבה המשתמש מבטל סימון שהיה מסומן בעבר.
    if RegValueExists(HKCU, RunKeyPath, '{#StartupRunValueName}') then
      RegDeleteValue(HKCU, RunKeyPath, '{#StartupRunValueName}');
  end;
end;

// -----------------------------------------------------------------------
// יוצר קובץ settings.json מינימלי עם שדה StartWithWindows התואם לבחירה
// בהתקנה - **רק אם** קובץ הגדרות עדיין לא קיים בכלל (התקנה ראשונה נקייה).
// במתכוון לא נוגעים בקובץ קיים (התקנת עדכון/חוזרת) כדי לא לקחת שום סיכון
// לפגוע בהגדרות אישיות שכבר קיימות - במקרה כזה הסנכרון בפועל (מה
// שבאמת קורה עם Windows) עדיין מתבצע במלואו דרך הרישום למעלה; רק
// התצוגה בתוך חלונית ההגדרות של התוכנה עצמה עשויה שלא לשקף את זה
// עד שהמשתמש יפתח וישמור אותה פעם אחת.
// -----------------------------------------------------------------------
procedure SyncStartupSettingToSettingsFile();
var
  SettingsDir: string;
  SettingsFile: string;
  Content: string;
  BoolText: string;
begin
  SettingsDir := ExpandConstant('{userappdata}\HebrewTaskbarWidget');
  SettingsFile := SettingsDir + '\settings.json';

  if FileExists(SettingsFile) then
  begin
    Exit;
  end;

  if WizardIsTaskSelected('launchatstartup') then
    BoolText := 'true'
  else
    BoolText := 'false';

  Content := '{' + #13#10 + '  "StartWithWindows": ' + BoolText + #13#10 + '}' + #13#10;

  if not DirExists(SettingsDir) then
  begin
    ForceDirectories(SettingsDir);
  end;

  SaveStringToFile(SettingsFile, Content, False);
end;

// -----------------------------------------------------------------------
// נקודת הכניסה: מופעל ממש לפני שלב העתקת הקבצים בפועל (ssInstall) -
// כך שאם ה-Runtime חסר, הוא יותקן (או יתחיל הורדה) לפני שהמשתמש רואה
// את עמוד ההתקדמות של התקנת התוכנה עצמה. שלב ssPostInstall מריץ את
// סנכרון ה"הפעלה האוטומטית" אחרי שהקבצים כבר הועתקו (כדי ש-{app}\...
// כבר יהיה נתיב תקין ל-EXE שנכתב לרישום).
// -----------------------------------------------------------------------
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    EnsureDotNetDesktopRuntime();
  end;

  if CurStep = ssPostInstall then
  begin
    ApplyStartupTaskToRegistry();
    SyncStartupSettingToSettingsFile();
  end;
end;

// -----------------------------------------------------------------------
// סוגרת בכפייה את כל התהליכים הרצים של התוכנה (הוידג'ט הראשי + כלי
// ההגדרות העצמאי, אם הוא פתוח) - נקראת ראשונה, לפני כל שלב אחר של הסרת
// ההתקנה, כדי שקבצי ה-exe/DLL לא יהיו "נעולים" ע"י תהליך רץ כשתוכנית
// ההסרה מנסה למחוק אותם (זו הייתה בדיוק הסיבה לכישלון הסרת התקנה בזמן
// שתאריכון פועל - "לא הצליח להסיר..."). /F = סגירה כפויה (ללא בקשת אישור
// מהתהליך עצמו), /IM = לפי שם קובץ ה-exe. אם התהליך כלל לא רץ, taskkill
// פשוט מחזיר קוד שגיאה לא-קריטי שמתעלמים ממנו (ResultCode לא נבדק) -
// זה תרחיש תקין (התוכנה כבר לא פעילה), לא כישלון אמיתי.
// -----------------------------------------------------------------------
procedure TerminateRunningAppProcesses();
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM "{#MyAppExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM "{#MySettingsExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // רגע קצר, כדי לתת ל-Windows לשחרר בפועל את הנעילה על הקבצים לפני
  // שממשיכים לשלב שמנסה למחוק אותם.
  Sleep(300);
end;

// -----------------------------------------------------------------------
// הסרת התקנה יסודית: מחזירה את שורת המשימות למצב הרגיל שלה לגמרי -
// לא רק "מוחקת קבצים", אלא גם מבטלת בפועל את כל מה שהתוכנה שינתה
// במערכת: מנקה את ערך המדיניות HideClock (אם התוכנה קבעה אותו - כלומר
// אם המשתמש השתמש אי-פעם באפשרות "הסתר את תצוגת התאריך/שעה המקורית")
// **ומפעילה מחדש את Explorer** כדי שהשינוי ייכנס לתוקף מיידית - זה
// בדיוק המנגנון היחיד שגם מחזיר את הרווח שצומצם (לא רק את הנראות),
// תואם ל-ApplyFullEffectWithRestart(false) שבתוכנה עצמה. בנוסף, מנקה
// את רשומת ההפעלה האוטומטית עם Windows, אם קיימת.
// -----------------------------------------------------------------------
procedure RestoreWindowsTaskbarToDefaults();
var
  PolicyKeyPath: string;
  ResultCode: Integer;
begin
  PolicyKeyPath := 'Software\Microsoft\Windows\CurrentVersion\Policies\Explorer';

  if RegValueExists(HKCU, PolicyKeyPath, 'HideClock') then
  begin
    RegDeleteValue(HKCU, PolicyKeyPath, 'HideClock');

    // הפעלה מחדש אמיתית של Explorer - זהה למנגנון RestartExplorer בתוכנה
    // עצמה: סוגרים את כל תהליכי explorer.exe הקיימים, ממתינים רגע קצר,
    // ואז מפעילים אותו מחדש - כדי שהתצוגה **וגם** הרווח שהוקצה לה
    // (לא רק הנראות) יחזרו למצב המקורי, הרגיל, של Windows.
    Exec('taskkill.exe', '/F /IM explorer.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(400);
    Exec('explorer.exe', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    // ראשית דבר - לפני כל פעולה אחרת - כדי שהמחיקה בפועל של הקבצים
    // (שמתבצעת ע"י Inno Setup עצמו, מיד לאחר שלב usUninstall) לא תיתקל
    // בקבצים נעולים ע"י תהליך רץ.
    TerminateRunningAppProcesses();

    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#StartupRunValueName}');
    RestoreWindowsTaskbarToDefaults();
  end;
end;
