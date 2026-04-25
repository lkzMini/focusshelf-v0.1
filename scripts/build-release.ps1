param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",

    [string]$Version = "v0.1.0",

    [int]$SmokeTestSeconds = 8,

    [switch]$SkipSmokeTest
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "src\FocusShelf.App\FocusShelf.App.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$buildRoot = Join-Path $artifactsRoot "build\win-$($Platform.ToLowerInvariant())"
$packageRoot = Join-Path $artifactsRoot "package\win-$($Platform.ToLowerInvariant())"
$smokeRoot = Join-Path $artifactsRoot "smoke-test\win-$($Platform.ToLowerInvariant())"
$releaseRoot = Join-Path $artifactsRoot "release"
$normalizedVersion = if ($Version.StartsWith("v")) { $Version } else { "v$Version" }
$zipPath = Join-Path $releaseRoot "FocusShelf-$normalizedVersion-win-$($Platform.ToLowerInvariant()).zip"

function Remove-SafeArtifactPath {
    param([string]$PathToRemove)

    if (-not (Test-Path -LiteralPath $PathToRemove)) {
        return
    }

    $resolved = (Resolve-Path -LiteralPath $PathToRemove).Path
    $artifactsResolved = (Resolve-Path -LiteralPath $artifactsRoot).Path

    if (-not $resolved.StartsWith($artifactsResolved, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove outside artifacts root: $resolved"
    }

    Remove-Item -LiteralPath $resolved -Recurse -Force
}

function Copy-ReleasePayload {
    param(
        [string]$SourceRoot,
        [string]$DestinationRoot
    )

    $excludedExtensions = @(".pdb", ".binlog", ".log", ".ilk", ".iobj", ".ipdb", ".tmp", ".cache")
    $sourceRootInfo = Get-Item -LiteralPath $SourceRoot
    $sourcePrefix = $sourceRootInfo.FullName.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar

    Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Force | ForEach-Object {
        if ($excludedExtensions -contains $_.Extension.ToLowerInvariant()) {
            return
        }

        if (-not $_.FullName.StartsWith($sourcePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Unexpected file outside source root: $($_.FullName)"
        }

        $relativePath = $_.FullName.Substring($sourcePrefix.Length)
        $targetPath = Join-Path $DestinationRoot $relativePath
        $targetDirectory = Split-Path -Parent $targetPath

        New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Force
    }
}

function Assert-ZipPayload {
    param([string]$RootPath)

    $requiredFiles = @(
        "FocusShelf.App.exe",
        "FocusShelf.App.dll",
        "FocusShelf.App.deps.json",
        "FocusShelf.App.runtimeconfig.json",
        "FocusShelf.App.pri",
        "App.xbf",
        "Views\MainWindow.xbf",
        "runtimes\win-$($Platform.ToLowerInvariant())\native\Microsoft.WindowsAppRuntime.Bootstrap.dll"
    )

    foreach ($file in $requiredFiles) {
        $path = Join-Path $RootPath $file
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Release payload is missing required file: $path"
        }
    }

    $selfContainedMarkers = @(
        "Microsoft.ui.xaml.dll",
        "Microsoft.UI.Xaml.Controls.dll",
        "Microsoft.WindowsAppRuntime.dll",
        "WinUIEdit.dll"
    )

    foreach ($file in $selfContainedMarkers) {
        $path = Join-Path $RootPath $file
        if (Test-Path -LiteralPath $path) {
            throw "Release payload contains '$file'. That indicates self-contained/private Windows App SDK output, which is the output style that failed the clean-folder smoke test."
        }
    }
}

function Invoke-CleanFolderSmokeTest {
    param(
        [string]$ExecutablePath,
        [int]$TimeoutSeconds
    )

    $startTime = Get-Date
    $workingDirectory = Split-Path -Parent $ExecutablePath
    $exeName = [IO.Path]::GetFileName($ExecutablePath)

    $process = Start-Process -FilePath $ExecutablePath -WorkingDirectory $workingDirectory -PassThru

    try {
        Start-Sleep -Seconds $TimeoutSeconds

        $applicationErrors = Get-WinEvent -FilterHashtable @{
            LogName = "Application"
            StartTime = $startTime.AddSeconds(-2)
            Id = 1000
        } -ErrorAction SilentlyContinue | Where-Object {
            $_.Message -like "*$exeName*" -or
            $_.Message -like "*FocusShelf.App*" -or
            $_.Message -like "*Microsoft.UI.Xaml.dll*" -or
            $_.Message -like "*Microsoft.ui.xaml.dll*"
        }

        if ($applicationErrors) {
            $message = ($applicationErrors | Select-Object -First 1).Message
            throw "Clean-folder smoke test logged an Application Error. $message"
        }

        if ($process.HasExited) {
            throw "Clean-folder smoke test failed: $exeName exited within $TimeoutSeconds seconds with exit code $($process.ExitCode)."
        }
    }
    finally {
        if ($process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project file: $projectPath"
}

[xml]$projectXml = Get-Content -Raw -LiteralPath $projectPath
$windowsPackageType = $projectXml.Project.PropertyGroup.WindowsPackageType | Select-Object -First 1
$windowsAppSdkSelfContained = $projectXml.Project.PropertyGroup.WindowsAppSDKSelfContained | Select-Object -First 1

if ($windowsPackageType -ne "None") {
    throw "FocusShelf ZIP releases require WindowsPackageType=None. Current value: '$windowsPackageType'."
}

if ($windowsAppSdkSelfContained -ne "false") {
    throw "FocusShelf ZIP releases require WindowsAppSDKSelfContained=false to match the working framework-dependent Debug output style. Current value: '$windowsAppSdkSelfContained'."
}

New-Item -ItemType Directory -Force -Path $artifactsRoot, $releaseRoot | Out-Null
Remove-SafeArtifactPath $buildRoot
Remove-SafeArtifactPath $packageRoot
Remove-SafeArtifactPath $smokeRoot
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Force -Path $buildRoot, $packageRoot, $smokeRoot | Out-Null

Write-Host "Building FocusShelf Release ZIP payload for $Platform..."
dotnet build $projectPath `
    -c Release `
    -p:Platform=$Platform `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=false `
    -p:SelfContained=false `
    -p:GenerateAppxPackageOnBuild=false `
    -o $buildRoot

if ($LASTEXITCODE -ne 0) {
    throw "Release build failed with exit code $LASTEXITCODE."
}

Assert-ZipPayload -RootPath $buildRoot

Write-Host "Copying release payload..."
Copy-ReleasePayload -SourceRoot $buildRoot -DestinationRoot $packageRoot
Assert-ZipPayload -RootPath $packageRoot

Write-Host "Creating ZIP artifact..."
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath -Force

if (-not (Test-Path -LiteralPath $zipPath)) {
    throw "ZIP artifact was not created: $zipPath"
}

Write-Host "Extracting ZIP into clean smoke-test folder..."
Expand-Archive -LiteralPath $zipPath -DestinationPath $smokeRoot -Force
Assert-ZipPayload -RootPath $smokeRoot

$exe = Join-Path $smokeRoot "FocusShelf.App.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Could not find FocusShelf.App.exe in smoke-test output."
}

if ($SkipSmokeTest) {
    Write-Warning "Skipping clean-folder launch smoke test because -SkipSmokeTest was supplied."
}
else {
    Write-Host "Launching clean-folder smoke test..."
    Invoke-CleanFolderSmokeTest -ExecutablePath $exe -TimeoutSeconds $SmokeTestSeconds
}

Write-Host ""
Write-Host "ZIP artifact   : $zipPath"
Write-Host "Payload folder : $packageRoot"
Write-Host "Smoke-test dir : $smokeRoot"
