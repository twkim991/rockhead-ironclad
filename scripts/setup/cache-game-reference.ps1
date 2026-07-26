[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]]$GamePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$referenceRoot = Join-Path $repositoryRoot '.workspace\game-references'
$fullReferenceRoot = [System.IO.Path]::GetFullPath($referenceRoot)
New-Item -ItemType Directory -Path $fullReferenceRoot -Force | Out-Null

foreach ($sourcePath in $GamePath) {
    $resolvedGamePath = (Resolve-Path -LiteralPath $sourcePath).Path
    $releaseInfoPath = Join-Path $resolvedGamePath 'release_info.json'
    $gameDataPath = Join-Path $resolvedGamePath 'data_sts2_windows_x86_64'
    $gameAssemblyPath = Join-Path $gameDataPath 'sts2.dll'

    if (-not (Test-Path -LiteralPath $releaseInfoPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $gameAssemblyPath -PathType Leaf)) {
        throw "Slay the Spire 2 files are incomplete at '$resolvedGamePath'."
    }

    $releaseInfo = Get-Content -LiteralPath $releaseInfoPath -Raw | ConvertFrom-Json
    $normalizedVersion = ([string]$releaseInfo.version).TrimStart('v', 'V')
    if ($normalizedVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "Unsupported game version format '$($releaseInfo.version)' at '$resolvedGamePath'."
    }
    if (-not ($normalizedVersion.StartsWith('0.107.', [System.StringComparison]::Ordinal) -or
        $normalizedVersion.StartsWith('0.109.', [System.StringComparison]::Ordinal))) {
        throw "Only supported game versions 0.107.x and 0.109.x can be cached. Found '$normalizedVersion'."
    }

    $targetPath = [System.IO.Path]::GetFullPath(
        (Join-Path $fullReferenceRoot $normalizedVersion))
    $stagingPath = [System.IO.Path]::GetFullPath(
        (Join-Path $fullReferenceRoot ".incoming-$normalizedVersion-$PID"))
    $referenceRootPrefix = $fullReferenceRoot.TrimEnd('\') + '\'

    foreach ($candidatePath in @($targetPath, $stagingPath)) {
        if (-not ($candidatePath.TrimEnd('\') + '\').StartsWith(
            $referenceRootPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Reference cache path escaped '$fullReferenceRoot': $candidatePath"
        }
    }

    if (Test-Path -LiteralPath $stagingPath) {
        Remove-Item -LiteralPath $stagingPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null

    try {
        Copy-Item -LiteralPath $releaseInfoPath -Destination $stagingPath -Force
        Copy-Item -LiteralPath $gameDataPath -Destination $stagingPath -Recurse -Force

        $metadata = [ordered]@{
            schema = 1
            version = $normalizedVersion
            source_path = $resolvedGamePath
            cached_at_utc = [DateTime]::UtcNow.ToString('o')
        }
        $metadataJson = $metadata | ConvertTo-Json -Depth 3
        [System.IO.File]::WriteAllText(
            (Join-Path $stagingPath 'reference-cache.json'),
            $metadataJson + [Environment]::NewLine,
            (New-Object System.Text.UTF8Encoding($false)))

        if (Test-Path -LiteralPath $targetPath) {
            Remove-Item -LiteralPath $targetPath -Recurse -Force
        }
        Move-Item -LiteralPath $stagingPath -Destination $targetPath
    }
    finally {
        if (Test-Path -LiteralPath $stagingPath) {
            Remove-Item -LiteralPath $stagingPath -Recurse -Force
        }
    }

    $cachedSize = (
        Get-ChildItem -LiteralPath $targetPath -Recurse -File |
            Measure-Object -Property Length -Sum
    ).Sum
    Write-Output (
        "Cached Slay the Spire 2 v{0}: {1} ({2:N1} MB)" -f
            $normalizedVersion,
            $targetPath,
            ($cachedSize / 1MB))
}
