#!/usr/bin/env pwsh
#
# Publish the self-contained ClipRef.exe (win-x64) and zip it for distribution.
# The Windows analogue of scripts/release.sh — minus signing/notarization, which
# Windows does not require here and for which no certificate is available.
#
# Output: build/release/ClipRef-<version>-win-x64.zip  (one self-contained exe)
#
# Version: an explicit -Version wins (CI passes the git tag, e.g. 0.1.0),
# otherwise it falls back to 0.0.0. The version is stamped into ClipRef.exe
# (File/Product version) via -p:Version, mirroring the macOS MARKETING_VERSION.
#
# Run:  pwsh scripts/release-win.ps1 -Version 0.1.0

[CmdletBinding()]
param([string]$Version)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$project  = Join-Path $repoRoot 'windows/ClipRef/ClipRef.csproj'

if ([string]::IsNullOrWhiteSpace($Version)) { $Version = '0.0.0' }

$outDir     = Join-Path $repoRoot 'build/release'
$publishDir = Join-Path $repoRoot 'windows/ClipRef/bin/Release/net8.0-windows/win-x64/publish'
$exe        = Join-Path $publishDir 'ClipRef.exe'
$zip        = Join-Path $outDir "ClipRef-$Version-win-x64.zip"

Write-Host "==> Publishing ClipRef $Version (win-x64, self-contained)..."
# --disable-build-servers pre-empts a stale MSBuild node holding a GenerateBundle
# file lock on the single-file bundle (a known local flake; a no-op on a fresh CI VM).
# IncludeSourceRevisionInInformationalVersion=false keeps the exe's product version a
# clean "0.1.0" instead of "0.1.0+<git-sha>", matching the macOS version display.
dotnet publish $project -p:PublishProfile=win-x64 -p:Version=$Version `
  -p:IncludeSourceRevisionInInformationalVersion=false --disable-build-servers
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }
if (-not (Test-Path $exe)) { throw "Published exe not found: $exe" }

Write-Host "==> Zipping -> $zip"
if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Compress-Archive -Path $exe -DestinationPath $zip -Force

Write-Host "OK -> $zip"
