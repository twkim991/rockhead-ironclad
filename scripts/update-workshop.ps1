<#
.SYNOPSIS
    Prepares a ThrowRockIronclad release and optionally updates its existing
    Steam Workshop item without invoking the dotnet CLI.

.DESCRIPTION
    This script expects the Release DLL and PCK to have already been produced
    by the editor/IDE workflow that works on this machine. It validates that
    those artifacts are newer than their corresponding source files, checks
    that the manifest, project, and DLL versions match, then:

      1. Copies DLL/PCK/JSON into the Workshop content directory.
      2. Creates publish/ThrowRockIronclad-vX.Y.Z.zip with exactly those files.
      3. Updates workshop.json's changeNote.
      4. Sets workshop.json visibility to null so an existing public/private
         setting is not changed accidentally.
      5. Optionally runs Mega Crit's ModUploader after explicit confirmation.

.PARAMETER ChangeNote
    The Steam Workshop change note for this update.

.PARAMETER Upload
    After preparation succeeds, ask for confirmation and upload the existing
    Workshop item identified by mod_id.txt.

.PARAMETER SkipFreshnessCheck
    Skip source/artifact timestamp checks. Version and file validation still
    run. Use only when timestamps are known to be misleading.

.EXAMPLE
    .\scripts\update-workshop.ps1 `
        -ChangeNote "v0.2.0 - 신규 바위 카드 4종 추가"

.EXAMPLE
    .\scripts\update-workshop.ps1 `
        -ChangeNote "v0.2.0 - 신규 바위 카드 4종 추가" `
        -Upload
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ChangeNote,

    [switch]$Upload,

    [switch]$SkipFreshnessCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Require-File {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }

    $item = Get-Item -LiteralPath $Path
    if ($item.Length -le 0) {
        throw "Required file is empty: $Path"
    }

    return $item
}

function Set-JsonProperty {
    param(
        [Parameter(Mandatory = $true)][psobject]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][object]$Value
    )

    if ($null -ne $Object.PSObject.Properties[$Name]) {
        $Object.$Name = $Value
    }
    else {
        $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
    }
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$manifestPath = Join-Path $repoRoot "ThrowRockIronclad.json"
$projectPath = Join-Path $repoRoot "ThrowRockIronclad.csproj"
$releaseDir = Join-Path $repoRoot ".godot\mono\temp\bin\Release"
$releaseDllPath = Join-Path $releaseDir "ThrowRockIronclad.dll"
$releasePckPath = Join-Path $releaseDir "ThrowRockIronclad.pck"
$workshopRoot = Join-Path $repoRoot "workshop\staging\ThrowRockIronclad"
$workshopContent = Join-Path $workshopRoot "content"
$workshopConfigPath = Join-Path $workshopRoot "workshop.json"
$workshopIdPath = Join-Path $workshopRoot "mod_id.txt"
$workshopImagePath = Join-Path $workshopRoot "image.png"
$workshopPreviewsPath = Join-Path $workshopRoot "previews"
$uploaderPath = Join-Path $repoRoot ".tools\sts2-mod-uploader\ModUploader.exe"
$publishDir = Join-Path $repoRoot "publish"

Write-Step "Validating versions and Release artifacts"

$manifestFile = Require-File -Path $manifestPath
$projectFile = Require-File -Path $projectPath
$releaseDll = Require-File -Path $releaseDllPath
$releasePck = Require-File -Path $releasePckPath

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$manifestVersion = [string]$manifest.version
if ($manifestVersion -notmatch '^v(\d+)\.(\d+)\.(\d+)$') {
    throw "Manifest version must use vX.Y.Z format. Current value: '$manifestVersion'"
}
$plainVersion = $manifestVersion.Substring(1)

$projectText = Get-Content -LiteralPath $projectPath -Raw -Encoding UTF8
$projectVersionMatch = [regex]::Match($projectText, '<Version>\s*([^<]+)\s*</Version>')
if (-not $projectVersionMatch.Success) {
    throw "Could not find <Version> in $projectPath"
}
$projectVersion = $projectVersionMatch.Groups[1].Value.Trim()
if ($projectVersion -ne $plainVersion) {
    throw "Version mismatch: manifest=$manifestVersion, project=$projectVersion"
}

$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($releaseDllPath).Version
$assemblySemVer = "$($assemblyVersion.Major).$($assemblyVersion.Minor).$($assemblyVersion.Build)"
if ($assemblySemVer -ne $plainVersion) {
    throw "Release DLL is stale or has the wrong version: DLL=$assemblySemVer, expected=$plainVersion"
}

if (-not $SkipFreshnessCheck) {
    $codeRoot = Join-Path $repoRoot "ThrowRockIroncladCode"
    $codeSources = @(
        Get-ChildItem -LiteralPath $codeRoot -Recurse -File -Filter "*.cs"
        $projectFile
    )
    $latestCodeSource = $codeSources |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($releaseDll.LastWriteTimeUtc -lt $latestCodeSource.LastWriteTimeUtc) {
        throw ("Release DLL is older than source '{0}'. Build Release in the editor/IDE first." -f $latestCodeSource.FullName)
    }

    $contentRoot = Join-Path $repoRoot "ThrowRockIronclad"
    $contentSources = @(
        Get-ChildItem -LiteralPath $contentRoot -Recurse -File
        Get-Item -LiteralPath (Join-Path $repoRoot "project.godot")
        Get-Item -LiteralPath (Join-Path $repoRoot "export_presets.cfg")
    )
    $latestContentSource = $contentSources |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($releasePck.LastWriteTimeUtc -lt $latestContentSource.LastWriteTimeUtc) {
        throw ("Release PCK is older than content '{0}'. Build Release in the editor/IDE first." -f $latestContentSource.FullName)
    }
}

Write-Host "Version: $manifestVersion"
Write-Host "DLL: $($releaseDll.Length) bytes, $($releaseDll.LastWriteTime)"
Write-Host "PCK: $($releasePck.Length) bytes, $($releasePck.LastWriteTime)"

Write-Step "Validating Workshop workspace"

$workshopConfigFile = Require-File -Path $workshopConfigPath
$workshopIdFile = Require-File -Path $workshopIdPath
$workshopImageFile = Require-File -Path $workshopImagePath
$uploaderFile = Require-File -Path $uploaderPath

$workshopId = (Get-Content -LiteralPath $workshopIdPath -Raw -Encoding UTF8).Trim()
if ($workshopId -notmatch '^\d+$') {
    throw "mod_id.txt does not contain a valid numeric Workshop item ID."
}

$steamImageLimit = 1000000
if ($workshopImageFile.Length -ge $steamImageLimit) {
    throw "Workshop image.png must be under 1 MB: $workshopImagePath"
}

if (Test-Path -LiteralPath $workshopPreviewsPath -PathType Container) {
    $oversizedPreviews = @(
        Get-ChildItem -LiteralPath $workshopPreviewsPath -File |
            Where-Object { $_.Length -ge $steamImageLimit }
    )
    if ($oversizedPreviews.Count -gt 0) {
        $previewList = ($oversizedPreviews.FullName -join [Environment]::NewLine)
        throw "Workshop preview images must be under 1 MB:`n$previewList"
    }
}

if (-not (Test-Path -LiteralPath $workshopContent -PathType Container)) {
    New-Item -ItemType Directory -Path $workshopContent | Out-Null
}

$artifactNames = @(
    "ThrowRockIronclad.dll",
    "ThrowRockIronclad.pck",
    "ThrowRockIronclad.json"
)

$unexpectedContent = @(
    Get-ChildItem -LiteralPath $workshopContent -Force |
        Where-Object { $artifactNames -notcontains $_.Name }
)
if ($unexpectedContent.Count -gt 0) {
    $unexpectedList = ($unexpectedContent.FullName -join [Environment]::NewLine)
    throw "Unexpected files exist in Workshop content. Remove or relocate them first:`n$unexpectedList"
}

Write-Step "Synchronizing Workshop content"

Copy-Item -LiteralPath $releaseDllPath -Destination (Join-Path $workshopContent "ThrowRockIronclad.dll") -Force
Copy-Item -LiteralPath $releasePckPath -Destination (Join-Path $workshopContent "ThrowRockIronclad.pck") -Force
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $workshopContent "ThrowRockIronclad.json") -Force

foreach ($artifactName in $artifactNames) {
    Require-File -Path (Join-Path $workshopContent $artifactName) | Out-Null
}

Write-Step "Updating Workshop change note"

$workshopConfig = Get-Content -LiteralPath $workshopConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
Set-JsonProperty -Object $workshopConfig -Name "changeNote" -Value $ChangeNote
Set-JsonProperty -Object $workshopConfig -Name "visibility" -Value $null

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$workshopJson = $workshopConfig | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($workshopConfigPath, $workshopJson + [Environment]::NewLine, $utf8NoBom)

Write-Host "Workshop visibility will remain unchanged."

Write-Step "Creating GitHub release ZIP"

if (-not (Test-Path -LiteralPath $publishDir -PathType Container)) {
    New-Item -ItemType Directory -Path $publishDir | Out-Null
}

$zipPath = Join-Path $publishDir "ThrowRockIronclad-$manifestVersion.zip"
if (Test-Path -LiteralPath $zipPath -PathType Leaf) {
    Remove-Item -LiteralPath $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open(
    $zipPath,
    [System.IO.Compression.ZipArchiveMode]::Create
)
try {
    foreach ($artifactName in $artifactNames) {
        $sourcePath = Join-Path $workshopContent $artifactName
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip,
            $sourcePath,
            $artifactName,
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }
}
finally {
    $zip.Dispose()
}

$zipFile = Require-File -Path $zipPath
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()

Write-Host "ZIP: $zipPath"
Write-Host "Size: $($zipFile.Length) bytes"
Write-Host "SHA-256: $zipHash"

if (-not $Upload) {
    Write-Host ""
    Write-Host "Preparation complete. No external upload was performed." -ForegroundColor Green
    Write-Host "Review README.md, CHANGELOG.md, and workshop.json, then run again with -Upload."
    exit 0
}

Write-Step "Ready to update the existing Steam Workshop item"

$steamProcess = Get-Process -Name "steam" -ErrorAction SilentlyContinue
if ($null -eq $steamProcess) {
    throw "Steam is not running. Start Steam, sign in to the publishing account, and try again."
}

Write-Host "Workshop item: $workshopId"
Write-Host "Change note: $ChangeNote"
Write-Host "Uploader: $($uploaderFile.FullName)"
Write-Host ""
$confirmation = Read-Host "Type UPLOAD to publish this update"
if ($confirmation -cne "UPLOAD") {
    Write-Warning "Upload canceled. Prepared files were kept."
    exit 0
}

Push-Location -LiteralPath $uploaderFile.DirectoryName
try {
    & $uploaderPath upload -w $workshopRoot
    $uploaderExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($uploaderExitCode -ne 0) {
    throw "ModUploader failed with exit code $uploaderExitCode. Check mod-uploader.log."
}

Write-Host ""
Write-Host "Steam Workshop update completed successfully." -ForegroundColor Green
