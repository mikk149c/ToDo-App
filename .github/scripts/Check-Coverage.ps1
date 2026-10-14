<#
.SYNOPSIS
    Summarises a Cobertura coverage report and optionally enforces a minimum line coverage.

.DESCRIPTION
    Writes a per-file table to the console and, on GitHub Actions, to the job summary.
    Fails (exit code 1) when -Threshold is above 0 and line coverage is below it.
    Branch coverage is reported but never enforced.

.EXAMPLE
    ./.github/scripts/Check-Coverage.ps1 -ReportPath coverage -Threshold 95
#>
param(
    # Folder containing exactly one coverage.cobertura.xml (searched recursively)
    [Parameter(Mandatory = $true)]
    [string] $ReportPath,

    # Minimum line coverage in percent. 0 means report only.
    [double] $Threshold = 0,

    # Markdown output file. Defaults to the GitHub Actions job summary when it exists.
    [string] $SummaryPath = $env:GITHUB_STEP_SUMMARY
)

$ErrorActionPreference = 'Stop'
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

function Format-Percent([double] $value) {
    return $value.ToString('0.0', $invariant) + '%'
}

$reports = @(Get-ChildItem -Path $ReportPath -Recurse -Filter 'coverage.cobertura.xml')
if ($reports.Count -ne 1) {
    throw "Expected exactly one coverage.cobertura.xml under '$ReportPath', found $($reports.Count)."
}

[xml] $xml = Get-Content -Raw -Path $reports[0].FullName
$coverage = $xml.coverage

$linesCovered = [int] $coverage.'lines-covered'
$linesValid = [int] $coverage.'lines-valid'
$lineRate = if ($linesValid -gt 0) { 100.0 * $linesCovered / $linesValid } else { 0.0 }
$branchRate = 100.0 * [double]::Parse($coverage.'branch-rate', $invariant)

# Per file: a file can hold several classes (including compiler-generated ones), so count unique line numbers
$rows = foreach ($file in ($coverage.packages.package.classes.class | Group-Object -Property filename | Sort-Object Name)) {
    $lines = @($file.Group | ForEach-Object { $_.lines.line })
    $total = @($lines | Select-Object -ExpandProperty number -Unique).Count
    $covered = @($lines | Where-Object { [int] $_.hits -gt 0 } | Select-Object -ExpandProperty number -Unique).Count
    [pscustomobject] @{
        File    = ($file.Name -replace '\\', '/')
        Covered = $covered
        Total   = $total
        Rate    = if ($total -gt 0) { 100.0 * $covered / $total } else { 100.0 }
    }
}

$enforced = $Threshold -gt 0
$passed = (-not $enforced) -or ($lineRate -ge $Threshold)

if (-not $enforced) {
    $status = 'Threshold not enforced (reporting only)'
} elseif ($passed) {
    $status = "Meets the $(Format-Percent $Threshold) threshold"
} else {
    $status = "Below the $(Format-Percent $Threshold) threshold"
}

$markdown = @(
    '## Code coverage (ToDo-App library)'
    ''
    "**Line coverage: $(Format-Percent $lineRate)** ($linesCovered of $linesValid lines). $status."
    ''
    "Branch coverage: $(Format-Percent $branchRate) (reported only, not enforced)."
    ''
    '| File | Covered lines | Line coverage |'
    '|---|---:|---:|'
)
$markdown += $rows | ForEach-Object { "| ``$($_.File)`` | $($_.Covered) / $($_.Total) | $(Format-Percent $_.Rate) |" }

$markdown | ForEach-Object { Write-Host $_ }
if ($SummaryPath) {
    Add-Content -Path $SummaryPath -Value ($markdown -join "`n") -Encoding utf8
}

# GitHub Actions annotations show up on the PR's checks without needing a write token
$message = "Line coverage $(Format-Percent $lineRate) ($linesCovered/$linesValid). $status."
if ($passed) {
    Write-Host "::notice title=Code coverage::$message"
    exit 0
}

Write-Host "::error title=Code coverage::$message"
exit 1
