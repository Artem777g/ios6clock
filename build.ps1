#requires -version 2.0
<#
    Сборка iOS6Clock.exe без Visual Studio: используется компилятор C#,
    который уже входит в .NET Framework 4.x в Windows.
#>
param(
    [string]$OutDir = (Join-Path $PSScriptRoot "dist")
)

$ErrorActionPreference = "Stop"

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "Не найден csc.exe (.NET Framework 4.x)." }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$sources = @(
    "src\Native.cs",
    "src\Theme.cs",
    "src\Assets.cs",
    "src\ClockOverlayForm.cs",
    "src\ClockPopupForm.cs",
    "src\App.cs"
) | ForEach-Object { Join-Path $PSScriptRoot $_ }

$resources = @(
    "face.png", "hour.png", "minute.png", "second.png",
    "face_dark.png", "hour_dark.png", "minute_dark.png", "second_dark.png",
    "shadow.png"
)

$cscArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    ("/out:" + (Join-Path $OutDir "iOS6Clock.exe")),
    ("/win32icon:" + (Join-Path $PSScriptRoot "res\app.ico")),
    "/r:System.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll"
)

foreach ($name in $resources) {
    $cscArgs += ("/resource:" + (Join-Path $PSScriptRoot ("res\" + $name)) + "," + $name)
}

$cscArgs += $sources

& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "Сборка завершилась с ошибкой ($LASTEXITCODE)." }

Write-Host ("Готово: " + (Join-Path $OutDir "iOS6Clock.exe"))
