#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for EtwExplorer solution
.DESCRIPTION
    Builds the EtwExplorer Visual Studio solution from the command line using MSBuild.
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)
.PARAMETER Project
    Specific project name to build. If not specified, builds the entire solution.
.PARAMETER Clean
    Clean before building
.PARAMETER Rebuild
    Rebuild all projects
.PARAMETER Restore
    Restore NuGet packages before building
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Configuration Release
    .\build.ps1 -Project EtwExplorer
    .\build.ps1 -Configuration Release -Rebuild
    .\build.ps1 -Restore -Configuration Release
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$Project = "",

    [switch]$Clean,

    [switch]$Rebuild,

    [switch]$Restore
)

$ErrorActionPreference = "Stop"

# Solution file path
$SolutionFile = Join-Path $PSScriptRoot "EtwExplorer.sln"

# Check if solution exists
if (-not (Test-Path $SolutionFile)) {
    Write-Error "Solution file not found: $SolutionFile"
    exit 1
}

# Find MSBuild
$MSBuildPath = $null

# Try to find vswhere (Visual Studio locator)
$VsWherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (Test-Path $VsWherePath) {
    $VsPath = & $VsWherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($VsPath) {
        # Try different MSBuild paths
        $PossiblePaths = @(
            "$VsPath\MSBuild\Current\Bin\MSBuild.exe",
            "$VsPath\MSBuild\Current\Bin\amd64\MSBuild.exe"
        )

        foreach ($Path in $PossiblePaths) {
            if (Test-Path $Path) {
                $MSBuildPath = $Path
                break
            }
        }
    }
}

# Fallback to PATH
if (-not $MSBuildPath) {
    $MSBuildPath = (Get-Command msbuild.exe -ErrorAction SilentlyContinue).Source
}

if (-not $MSBuildPath) {
    Write-Error "MSBuild not found. Please ensure Visual Studio or Build Tools are installed."
    exit 1
}

# Find NuGet
$NuGetPath = $null
$NuGetPossiblePaths = @(
    "$VsPath\Common7\IDE\CommonExtensions\Microsoft\NuGet\nuget.exe",
    "${env:ProgramFiles(x86)}\NuGet\nuget.exe",
    "C:\tools\nuget.exe"
    (Get-Command nuget.exe -ErrorAction SilentlyContinue).Source
)

foreach ($Path in $NuGetPossiblePaths) {
    if ($Path -and (Test-Path $Path)) {
        $NuGetPath = $Path
        break
    }
}

Write-Host "Using MSBuild: $MSBuildPath" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Platform: Any CPU" -ForegroundColor Cyan

# Restore NuGet packages if requested or if packages folder doesn't exist
$PackagesFolder = Join-Path $PSScriptRoot "packages"
if ($Restore -or -not (Test-Path $PackagesFolder)) {
    if ($NuGetPath) {
        Write-Host "NuGet: $NuGetPath" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Restoring NuGet packages..." -ForegroundColor Green
        & $NuGetPath restore $SolutionFile
        if ($LASTEXITCODE -ne 0) {
            Write-Host "NuGet restore failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
        Write-Host ""
    }
    else {
        Write-Warning "NuGet not found. Skipping package restore. Install NuGet CLI or restore packages in Visual Studio."
        Write-Host ""
    }
}

# Determine what to build
$BuildTarget = $SolutionFile
if ($Project) {
    # Find the project file
    $ProjectFile = Get-ChildItem -Path $PSScriptRoot -Filter "$Project.csproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $ProjectFile) {
        Write-Error "Project '$Project' not found (searched for .csproj)"
        exit 1
    }
    $BuildTarget = $ProjectFile.FullName
    Write-Host "Project: $Project" -ForegroundColor Cyan
}
else {
    Write-Host "Building entire solution" -ForegroundColor Cyan
}

# Determine build action
$Target = "Build"
if ($Rebuild) {
    $Target = "Rebuild"
    Write-Host "Action: Rebuild" -ForegroundColor Cyan
}
elseif ($Clean) {
    $Target = "Clean"
    Write-Host "Action: Clean" -ForegroundColor Cyan
}
else {
    Write-Host "Action: Build" -ForegroundColor Cyan
}

Write-Host ""

# Build arguments
$BuildArgs = @(
    $BuildTarget,
    "/t:$Target",
    "/p:Configuration=$Configuration",
    "/p:Platform=`"Any CPU`"",
    "/m",  # Multi-processor build
    "/v:minimal"  # Minimal verbosity
)

# Execute build
Write-Host "Building..." -ForegroundColor Green
& $MSBuildPath $BuildArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Build succeeded!" -ForegroundColor Green

    # Show output location
    if (-not $Project -or $Project -eq "") {
        Write-Host "Output locations:" -ForegroundColor Cyan
        Write-Host "  - EtwManifestParsing: EtwManifestParsing\bin\$Configuration\" -ForegroundColor Cyan
        Write-Host "  - EtwExplorer: EtwExplorer\bin\$Configuration\EtwExplorer.exe" -ForegroundColor Cyan
    }
    else {
        if ($Project -eq "EtwExplorer") {
            Write-Host "Output: EtwExplorer\bin\$Configuration\EtwExplorer.exe" -ForegroundColor Cyan
        }
        else {
            Write-Host "Output: $Project\bin\$Configuration\$Project.dll" -ForegroundColor Cyan
        }
    }
}
else {
    Write-Host ""
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
