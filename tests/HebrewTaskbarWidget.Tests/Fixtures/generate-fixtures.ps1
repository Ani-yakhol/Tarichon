# מוריד מ-Hebcal את לוח המועדים והפרשות (ארץ ישראל וחו"ל) ושומר כקבצי TSV דחוסים לבדיקות.
# הרצה: powershell -ExecutionPolicy Bypass -File generate-fixtures.ps1
# הנתונים של Hebcal.com ברישיון CC BY 4.0 - ראו README.md בתיקייה הזו.

param(
    [int]$FirstHebrewYear = 5760,
    [int]$LastHebrewYear = 5860
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression

# mod=off: בלי ימים לאומיים. ykk/mvch: יום כיפור קטן ושבת מברכים.
$baseUrl = 'https://www.hebcal.com/hebcal?v=1&cfg=json&maj=on&min=on&nx=on&mf=on&ss=on&s=on&o=on&mod=off&ykk=on&mvch=on&yt=H'

function Get-HebcalLines([string]$israelFlag) {
    $lines = New-Object System.Collections.Generic.List[string]
    for ($year = $FirstHebrewYear; $year -le $LastHebrewYear; $year++) {
        $url = "$baseUrl&i=$israelFlag&year=$year"
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing
        # מפענחים UTF-8 ידנית - PowerShell 5.1 לא תמיד מזהה את הקידוד לבד.
        $json = [System.Text.Encoding]::UTF8.GetString($response.RawContentStream.ToArray()) | ConvertFrom-Json
        foreach ($item in $json.items) {
            $date = ([string]$item.date).Substring(0, 10)
            $lines.Add("$date`t$($item.category)`t$($item.title)")
        }
        Start-Sleep -Milliseconds 300
    }
    return $lines
}

function Save-Gzip([string]$path, [System.Collections.Generic.List[string]]$lines) {
    $text = [string]::Join("`n", $lines) + "`n"
    $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($text)
    $file = [System.IO.File]::Create($path)
    try {
        $gzip = New-Object System.IO.Compression.GZipStream($file, [System.IO.Compression.CompressionLevel]::Optimal)
        try { $gzip.Write($bytes, 0, $bytes.Length) } finally { $gzip.Dispose() }
    } finally { $file.Dispose() }
}

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Save-Gzip (Join-Path $here 'hebcal-israel.tsv.gz') (Get-HebcalLines 'on')
Save-Gzip (Join-Path $here 'hebcal-diaspora.tsv.gz') (Get-HebcalLines 'off')
Write-Host "Saved Hebrew years $FirstHebrewYear-$LastHebrewYear."
