param(
    [string]$Configuration = "Release",
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$solutionPath = Join-Path $rootPath "src\RDCS.EmployeeAgent.sln"

Write-Host "Building RDCS Employee Agent..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Solution: $solutionPath" -ForegroundColor Yellow

if ($Clean) {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
    dotnet clean $solutionPath
}

Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $solutionPath

Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build $solutionPath --configuration $Configuration --no-restore

Write-Host "Build completed successfully!" -ForegroundColor Green
