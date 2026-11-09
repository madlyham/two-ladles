# ==============================================================================
# TWO LADLES — AUTOMATED TWO-BOT QA AUTOPLAY RUNNER
# ==============================================================================
# Executes automated two-bot autoplay playtesting across kitchen culinary stages,
# tracking reputation health, order success rates, dish pipelines, and writing
# structured audit reports to qa_reports/.
# ==============================================================================

param (
    [int]$Seed = 42,
    [string]$Days = "1-5",
    [float]$TimeScale = 2.0,
    [switch]$Headless = $true
)

$ErrorActionPreference = "Continue"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "   TWO LADLES — AUTOMATED TWO-BOT QA / PLAYTESTING RUNNER         " -ForegroundColor Yellow
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "Parameters:"
Write-Host "  - Seed:       $Seed"
Write-Host "  - Days:       $Days"
Write-Host "  - TimeScale:  ${TimeScale}x"
Write-Host "  - Headless:   $Headless"
Write-Host "------------------------------------------------------------------"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $ScriptRoot "unity\TwoLadles\Two Ladles"
$ReportsDir = Join-Path $ScriptRoot "qa_reports"
$LogFile = Join-Path $ReportsDir "qa_execution.log"

if (-not (Test-Path $ReportsDir)) {
    New-Item -ItemType Directory -Path $ReportsDir -Force | Out-Null
}

# Locate Unity 2022.3.62f3
$UnityPaths = @(
    "C:\Users\madly\AppData\Local\Programs\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe",
    "C:\Program Files\Unity\Editor\2022.3.62f3\Editor\Unity.exe"
)

$UnityExe = $null
foreach ($path in $UnityPaths) {
    if (Test-Path $path) {
        $UnityExe = $path
        break
    }
}

if (-not $UnityExe) {
    Write-Host "[ERROR] Could not find Unity 2022.3.62f3 executable!" -ForegroundColor Red
    exit 1
}

Write-Host "Found Unity at: $UnityExe" -ForegroundColor Green

$ArgsList = @(
    "-projectPath", "`"$ProjectPath`"",
    "-executeMethod", "BuildScript.RunAutoplayQABatch",
    "-autoplayQA",
    "-seed", "$Seed",
    "-days", "$Days",
    "-timescale", "$TimeScale",
    "-logFile", "`"$LogFile`""
)

if ($Headless) {
    $ArgsList += "-batchmode"
    $ArgsList += "-quit"
}

Write-Host "Launching QA Simulation..." -ForegroundColor Cyan
$Process = Start-Process -FilePath $UnityExe -ArgumentList $ArgsList -Wait -PassThru -NoNewWindow

$ExitCode = $Process.ExitCode
Write-Host "Unity Process exited with code: $ExitCode"

# Display latest summary if available
$SummaryFile = Join-Path $ReportsDir "latest_summary.txt"
if (Test-Path $SummaryFile) {
    Write-Host "`n------------------------------------------------------------------" -ForegroundColor Cyan
    Get-Content $SummaryFile
    Write-Host "------------------------------------------------------------------" -ForegroundColor Cyan
}

$JsonFile = Join-Path $ReportsDir "latest_results.json"
if (Test-Path $JsonFile) {
    Write-Host "QA Results JSON: $JsonFile" -ForegroundColor Green
}

$HistoryFile = Join-Path $ReportsDir "run_history.csv"
if (Test-Path $HistoryFile) {
    Write-Host "Run History CSV: $HistoryFile" -ForegroundColor Green
}

exit $ExitCode
