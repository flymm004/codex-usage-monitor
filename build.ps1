[CmdletBinding()]
param(
    [string]$Version = "0.1.0-alpha.1"
)

$ErrorActionPreference = "Stop"
if ($Version -notmatch '^[0-9A-Za-z][0-9A-Za-z.-]*$') {
    throw "Version must contain only letters, numbers, dots, and hyphens."
}

$repoRoot = $PSScriptRoot
$sourceRoot = Join-Path $repoRoot "src\CodexUsageMonitor"
$artifactRoot = Join-Path $repoRoot "artifacts"
$buildRoot = Join-Path $artifactRoot "build"
$packageRoot = Join-Path $artifactRoot ("package-" + $Version + "-win-x64")
$releaseRoot = Join-Path $artifactRoot "release"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw "Could not find the .NET Framework C# compiler. Install .NET Framework 4.8 developer tools."
}

New-Item -ItemType Directory -Force -Path $buildRoot,$releaseRoot | Out-Null
$artifactParent = [System.IO.Path]::GetFullPath($artifactRoot).TrimEnd('\') + '\'
$packageFullPath = [System.IO.Path]::GetFullPath($packageRoot)
if (-not $packageFullPath.StartsWith($artifactParent, [System.StringComparison]::OrdinalIgnoreCase) -or
    -not (Split-Path -Leaf $packageFullPath).StartsWith("package-")) {
    throw "Refusing to clean a package path outside the artifacts directory."
}
if (Test-Path -LiteralPath $packageFullPath) {
    Remove-Item -LiteralPath $packageFullPath -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $packageFullPath | Out-Null
$exePath = Join-Path $buildRoot "CodexUsageMonitor.exe"
$arguments = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    ("/win32manifest:" + (Join-Path $sourceRoot "CodexUsageMonitor.manifest")),
    "/reference:System.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Web.Extensions.dll",
    ("/out:" + $exePath),
    (Join-Path $sourceRoot "codex_usage_monitor.cs"),
    (Join-Path $sourceRoot "ui_text.cs")
)

& $compiler @arguments
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $sourceRoot "App.config") -Destination ($exePath + ".config") -Force
Copy-Item -LiteralPath $exePath -Destination (Join-Path $packageRoot "CodexUsageMonitor.exe") -Force
Copy-Item -LiteralPath ($exePath + ".config") -Destination (Join-Path $packageRoot "CodexUsageMonitor.exe.config") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination (Join-Path $packageRoot "README.md") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "README.zh-CN.md") -Destination (Join-Path $packageRoot "README.zh-CN.md") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination (Join-Path $packageRoot "LICENSE") -Force

$archivePath = Join-Path $artifactRoot ("CodexUsageMonitor-" + $Version + "-win-x64.zip")
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $archivePath -Force
$checksum = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = $archivePath + ".sha256"
Set-Content -LiteralPath $checksumPath -Value ($checksum + "  " + (Split-Path -Leaf $archivePath)) -Encoding Ascii

$releaseExeName = "CodexUsageMonitor-$Version-win-x64.exe"
$releaseExePath = Join-Path $releaseRoot $releaseExeName
Copy-Item -LiteralPath $exePath -Destination $releaseExePath -Force
Copy-Item -LiteralPath ($exePath + ".config") -Destination ($releaseExePath + ".config") -Force
$exeChecksum = (Get-FileHash -LiteralPath $releaseExePath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath ($releaseExePath + ".sha256") -Value ($exeChecksum + "  " + $releaseExeName) -Encoding Ascii

Write-Host "Built: $exePath"
Write-Host "Package: $archivePath"
Write-Host "SHA-256: $checksum"
Write-Host "Standalone EXE: $releaseExePath"
Write-Host "EXE companion config: $releaseExePath.config"
