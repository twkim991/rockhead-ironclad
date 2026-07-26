[CmdletBinding()]
param(
    [string]$StableGamePath,

    [string]$BetaGamePath,

    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$mainProjectRoot = Join-Path $repositoryRoot 'src\ThrowRockIronclad.GameMod'
$loaderProjectRoot = Join-Path $repositoryRoot 'src\ThrowRockIronclad.Loader'
$mainProject = Join-Path $mainProjectRoot 'ThrowRockIronclad.csproj'
$loaderProject = Join-Path $loaderProjectRoot 'ThrowRockIronclad.Loader.csproj'
$manifestPath = Join-Path $mainProjectRoot 'ThrowRockIronclad.json'
$buildOutput = Join-Path $mainProjectRoot '.godot\mono\temp\bin\Release'
$loaderOutput = Join-Path $loaderProjectRoot 'bin\Release\net9.0\ThrowRockIronclad.Loader.dll'
$loaderPdbOutput = Join-Path $loaderProjectRoot 'bin\Release\net9.0\ThrowRockIronclad.Loader.pdb'
$referenceRoot = Join-Path $repositoryRoot '.workspace\game-references'
$dotnet = Join-Path $repositoryRoot '.workspace\sdk\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) {
    $dotnet = 'dotnet'
}

function Assert-GameVersion {
    param(
        [string]$GamePath,
        [string]$ExpectedPrefix
    )

    $resolvedGamePath = (Resolve-Path -LiteralPath $GamePath).Path
    $releaseInfoPath = Join-Path $resolvedGamePath 'release_info.json'
    $gameAssemblyPath = Join-Path $resolvedGamePath 'data_sts2_windows_x86_64\sts2.dll'
    if (-not (Test-Path -LiteralPath $releaseInfoPath) -or -not (Test-Path -LiteralPath $gameAssemblyPath)) {
        throw "Slay the Spire 2 files are incomplete at '$resolvedGamePath'."
    }

    $releaseInfo = Get-Content -LiteralPath $releaseInfoPath -Raw | ConvertFrom-Json
    $normalizedVersion = ([string]$releaseInfo.version).TrimStart('v', 'V')
    if (-not $normalizedVersion.StartsWith($ExpectedPrefix, [System.StringComparison]::Ordinal)) {
        throw "Expected game version '$ExpectedPrefix*' at '$resolvedGamePath', found '$normalizedVersion'."
    }

    return $resolvedGamePath
}

function Find-CachedGameVersion {
    param(
        [string]$ExpectedPrefix,
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $referenceRoot -PathType Container)) {
        throw @"
No cached $Label game reference was found under '$referenceRoot'.
While that game branch is installed, run:
  .\scripts\setup\cache-game-reference.ps1 -GamePath '<Slay the Spire 2 path>'
"@
    }

    $matches = @(
        foreach ($candidate in Get-ChildItem -LiteralPath $referenceRoot -Directory) {
            $releaseInfoPath = Join-Path $candidate.FullName 'release_info.json'
            $gameAssemblyPath = Join-Path $candidate.FullName 'data_sts2_windows_x86_64\sts2.dll'
            if (-not (Test-Path -LiteralPath $releaseInfoPath -PathType Leaf) -or
                -not (Test-Path -LiteralPath $gameAssemblyPath -PathType Leaf)) {
                continue
            }

            try {
                $releaseInfo = Get-Content -LiteralPath $releaseInfoPath -Raw | ConvertFrom-Json
                $normalizedVersion = ([string]$releaseInfo.version).TrimStart('v', 'V')
                if ($normalizedVersion.StartsWith($ExpectedPrefix, [System.StringComparison]::Ordinal)) {
                    [pscustomobject]@{
                        Path = $candidate.FullName
                        Version = [Version]$normalizedVersion
                    }
                }
            }
            catch {
                continue
            }
        }
    )

    $match = $matches |
        Sort-Object -Property Version -Descending |
        Select-Object -First 1
    if ($null -eq $match) {
        throw @"
No cached $Label game reference matching '$ExpectedPrefix*' was found under '$referenceRoot'.
While that game branch is installed, run:
  .\scripts\setup\cache-game-reference.ps1 -GamePath '<Slay the Spire 2 path>'
"@
    }

    Write-Host "Using cached $Label game reference v$($match.Version): $($match.Path)"
    return $match.Path
}

function Invoke-VariantBuild {
    param(
        [string]$GamePath,
        [string]$CompatibilityTarget,
        [string]$Destination
    )

    & $dotnet build $mainProject `
        -c Release `
        --no-restore `
        "-p:Sts2Path=$GamePath" `
        "-p:Sts2CompatibilityTarget=$CompatibilityTarget" `
        -p:InstallModOnBuild=false
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for compatibility target $CompatibilityTarget."
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $buildOutput 'ThrowRockIronclad.dll') -Destination $Destination -Force
    $pdbPath = Join-Path $buildOutput 'ThrowRockIronclad.pdb'
    if (Test-Path -LiteralPath $pdbPath) {
        Copy-Item -LiteralPath $pdbPath -Destination $Destination -Force
    }
    [System.IO.File]::WriteAllText(
        (Join-Path $Destination 'compat-target.txt'),
        "$CompatibilityTarget`r`n",
        (New-Object System.Text.UTF8Encoding($false)))
}

$stableInputPath = $StableGamePath
if ([string]::IsNullOrWhiteSpace($stableInputPath)) {
    $stableInputPath = Find-CachedGameVersion -ExpectedPrefix '0.107.' -Label 'stable'
}
$betaInputPath = $BetaGamePath
if ([string]::IsNullOrWhiteSpace($betaInputPath)) {
    $betaInputPath = Find-CachedGameVersion -ExpectedPrefix '0.109.' -Label 'public beta'
}

$resolvedStableGamePath = Assert-GameVersion `
    -GamePath $stableInputPath `
    -ExpectedPrefix '0.107.'
$resolvedBetaGamePath = Assert-GameVersion `
    -GamePath $betaInputPath `
    -ExpectedPrefix '0.109.'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $manifestVersion = ([string]$manifest.version).TrimStart('v', 'V')
    if ($manifestVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "Manifest version must use vX.Y.Z format. Current value: '$($manifest.version)'"
    }
    $OutputDirectory = Join-Path $repositoryRoot ".artifacts\packages\ThrowRockIronclad-v$manifestVersion"
}

$publishRoot = (Join-Path $repositoryRoot '.artifacts\packages')
$fullOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$fullPublishRoot = [System.IO.Path]::GetFullPath($publishRoot).TrimEnd('\') + '\'
if (-not ($fullOutputDirectory.TrimEnd('\') + '\').StartsWith(
    $fullPublishRoot,
    [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must remain under '$publishRoot'."
}

if (Test-Path -LiteralPath $fullOutputDirectory) {
    Remove-Item -LiteralPath $fullOutputDirectory -Recurse -Force
}

$stableDestination = Join-Path $fullOutputDirectory 'lib\0.107.1'
$betaDestination = Join-Path $fullOutputDirectory 'lib\0.109.0'
Invoke-VariantBuild -GamePath $resolvedStableGamePath -CompatibilityTarget '0.107.1' -Destination $stableDestination
Invoke-VariantBuild -GamePath $resolvedBetaGamePath -CompatibilityTarget '0.109.0' -Destination $betaDestination

& $dotnet build $loaderProject -c Release "-p:Sts2Path=$resolvedBetaGamePath"
if ($LASTEXITCODE -ne 0) {
    throw 'Compatibility loader build failed.'
}

Copy-Item -LiteralPath $loaderOutput `
    -Destination (Join-Path $fullOutputDirectory 'ThrowRockIronclad.dll') `
    -Force
if (Test-Path -LiteralPath $loaderPdbOutput) {
    Copy-Item -LiteralPath $loaderPdbOutput `
        -Destination (Join-Path $fullOutputDirectory 'ThrowRockIronclad.pdb') `
        -Force
}
Copy-Item -LiteralPath $manifestPath -Destination $fullOutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $buildOutput 'ThrowRockIronclad.pck') `
    -Destination $fullOutputDirectory `
    -Force

$variantManifest = [ordered]@{
    schema = 1
    variants = @(
        [ordered]@{
            compatibility_target = '0.107.1'
            assembly = 'lib/0.107.1/ThrowRockIronclad.dll'
            sha256 = (Get-FileHash -LiteralPath (Join-Path $stableDestination 'ThrowRockIronclad.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
        },
        [ordered]@{
            compatibility_target = '0.109.0'
            assembly = 'lib/0.109.0/ThrowRockIronclad.dll'
            sha256 = (Get-FileHash -LiteralPath (Join-Path $betaDestination 'ThrowRockIronclad.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    )
}
$variantManifestJson = $variantManifest | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText(
    (Join-Path $fullOutputDirectory 'throw-rock-ironclad-variants.manifest'),
    $variantManifestJson,
    (New-Object System.Text.UTF8Encoding($false)))

$zipPath = "$fullOutputDirectory.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive `
    -Path (Join-Path $fullOutputDirectory '*') `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal

Write-Output "Compatibility bundle: $fullOutputDirectory"
Write-Output "Archive: $zipPath"
