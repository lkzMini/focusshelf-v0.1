param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",

    [string]$Version = "v0.1.0",

    [switch]$SkipSmokeTest
)

$ErrorActionPreference = "Stop"

Write-Warning "build-unpackaged-zip.ps1 is kept as a compatibility wrapper. The public ZIP release path is scripts\build-release.ps1."

& (Join-Path $PSScriptRoot "build-release.ps1") `
    -Platform $Platform `
    -Version $Version `
    -SkipSmokeTest:$SkipSmokeTest
