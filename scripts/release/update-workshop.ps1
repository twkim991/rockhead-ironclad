<#
.SYNOPSIS
    Builds the stable/beta compatibility bundle, prepares the Steam Workshop
    workspace, and optionally updates the existing Workshop item.

.PARAMETER ChangeNote
    The Steam Workshop change note for this update.

.PARAMETER StableGamePath
    Optional Slay the Spire 2 stable v0.107.x path. When omitted, the cached
    reference under .workspace/game-references is used.

.PARAMETER BetaGamePath
    Optional Slay the Spire 2 public beta v0.109.x path. When omitted, the
    cached reference under .workspace/game-references is used.

.PARAMETER Upload
    After preparation succeeds, ask for confirmation and upload the existing
    Workshop item identified by mod_id.txt.

.EXAMPLE
    .\scripts\release\update-workshop.ps1 `
        -ChangeNote "v0.2.1 - 베타 버전 호환 패치"

.EXAMPLE
    .\scripts\release\update-workshop.ps1 `
        -ChangeNote "v0.2.1 - 베타 버전 호환 패치" `
        -Upload
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ChangeNote,

    [string]$StableGamePath,

    [string]$BetaGamePath,

    [switch]$Upload
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

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$manifestPath = Join-Path $repoRoot "src\ThrowRockIronclad.GameMod\ThrowRockIronclad.json"
$bundleScript = Join-Path $repoRoot "scripts\build\build-compat-bundle.ps1"
$packageRoot = Join-Path $repoRoot ".artifacts\packages"
$workshopRoot = Join-Path $repoRoot ".artifacts\workshop\ThrowRockIronclad"
$workshopContent = Join-Path $workshopRoot "content"
$workshopConfigPath = Join-Path $workshopRoot "workshop.json"
$workshopIdPath = Join-Path $workshopRoot "mod_id.txt"
$workshopImagePath = Join-Path $workshopRoot "image.png"
$workshopPreviewsPath = Join-Path $workshopRoot "previews"
$uploaderPath = Join-Path $repoRoot ".workspace\sdk\sts2-mod-uploader\ModUploader.exe"

Write-Step "Validating release and Workshop inputs"

Require-File -Path $manifestPath | Out-Null
Require-File -Path $bundleScript | Out-Null
$workshopConfigFile = Require-File -Path $workshopConfigPath
$workshopIdFile = Require-File -Path $workshopIdPath
$workshopImageFile = Require-File -Path $workshopImagePath

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$manifestVersion = [string]$manifest.version
if ($manifestVersion -notmatch '^v(\d+)\.(\d+)\.(\d+)$') {
    throw "Manifest version must use vX.Y.Z format. Current value: '$manifestVersion'"
}

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

Write-Step "Building the stable/beta compatibility bundle"

$bundleParameters = @{}
if (-not [string]::IsNullOrWhiteSpace($StableGamePath)) {
    $bundleParameters.StableGamePath = $StableGamePath
}
if (-not [string]::IsNullOrWhiteSpace($BetaGamePath)) {
    $bundleParameters.BetaGamePath = $BetaGamePath
}

& $bundleScript @bundleParameters
if ($LASTEXITCODE -ne 0) {
    throw "Compatibility bundle build failed with exit code $LASTEXITCODE."
}

$bundlePath = Join-Path $packageRoot "ThrowRockIronclad-$manifestVersion"
$zipPath = "$bundlePath.zip"
$requiredBundleFiles = @(
    "ThrowRockIronclad.dll",
    "ThrowRockIronclad.pck",
    "ThrowRockIronclad.json",
    "throw-rock-ironclad-variants.manifest",
    "lib\0.107.1\ThrowRockIronclad.dll",
    "lib\0.109.0\ThrowRockIronclad.dll"
)
foreach ($relativePath in $requiredBundleFiles) {
    Require-File -Path (Join-Path $bundlePath $relativePath) | Out-Null
}
$zipFile = Require-File -Path $zipPath

Write-Step "Synchronizing Workshop content"

$resolvedWorkshopRoot = [System.IO.Path]::GetFullPath($workshopRoot).TrimEnd('\') + '\'
$resolvedWorkshopContent = [System.IO.Path]::GetFullPath($workshopContent).TrimEnd('\') + '\'
if (-not $resolvedWorkshopContent.StartsWith(
    $resolvedWorkshopRoot,
    [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Workshop content path must remain under '$workshopRoot'."
}

if (Test-Path -LiteralPath $workshopContent) {
    Remove-Item -LiteralPath $workshopContent -Recurse -Force
}
New-Item -ItemType Directory -Path $workshopContent -Force | Out-Null
Copy-Item -Path (Join-Path $bundlePath "*") -Destination $workshopContent -Recurse -Force

foreach ($relativePath in $requiredBundleFiles) {
    Require-File -Path (Join-Path $workshopContent $relativePath) | Out-Null
}

Write-Step "Updating Workshop change note"

$workshopConfig = Get-Content -LiteralPath $workshopConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
Set-JsonProperty -Object $workshopConfig -Name "changeNote" -Value $ChangeNote
Set-JsonProperty -Object $workshopConfig -Name "visibility" -Value $null

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$workshopJson = $workshopConfig | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText(
    $workshopConfigPath,
    $workshopJson + [Environment]::NewLine,
    $utf8NoBom)

$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Version: $manifestVersion"
Write-Host "Bundle: $bundlePath"
Write-Host "ZIP: $zipPath"
Write-Host "ZIP size: $($zipFile.Length) bytes"
Write-Host "ZIP SHA-256: $zipHash"
Write-Host "Workshop visibility will remain unchanged."

if (-not $Upload) {
    Write-Host ""
    Write-Host "Preparation complete. No external upload was performed." -ForegroundColor Green
    Write-Host "Review README.md, CHANGELOG.md, and workshop.json, then run again with -Upload."
    exit 0
}

Write-Step "Ready to update the existing Steam Workshop item"

$uploaderFile = Require-File -Path $uploaderPath
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
