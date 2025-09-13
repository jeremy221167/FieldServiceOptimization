# Field Service Intelligence System - Package Creator
# This script creates a distributable ZIP package of the complete system

Write-Host "Creating Field Service Intelligence System Package..." -ForegroundColor Green

# Define paths
$sourceDir = "FieldServiceIntelligence-Package"
$zipPath = "FieldServiceIntelligence-System-v1.1.0.zip"

# Check if source directory exists
if (-not (Test-Path $sourceDir)) {
    Write-Host "Error: Source directory '$sourceDir' not found!" -ForegroundColor Red
    exit 1
}

# Remove existing zip if it exists
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
    Write-Host "Removed existing package: $zipPath" -ForegroundColor Yellow
}

# Create the ZIP archive
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($sourceDir, $zipPath)

    Write-Host "Package created successfully!" -ForegroundColor Green
    Write-Host "Location: $(Get-Item $zipPath | Select-Object -ExpandProperty FullName)" -ForegroundColor Cyan
    Write-Host "Size: $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB" -ForegroundColor Cyan

    Write-Host "`nPackage Contents:" -ForegroundColor Yellow
    Write-Host "- Complete source code (15+ services)" -ForegroundColor White
    Write-Host "- Comprehensive API documentation (1,200+ lines)" -ForegroundColor White
    Write-Host "- NuGet package source" -ForegroundColor White
    Write-Host "- Blazor demo application" -ForegroundColor White
    Write-Host "- Ready-to-use .NET 9.0 project" -ForegroundColor White

} catch {
    Write-Host "Error creating package: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`nTo use the package:" -ForegroundColor Green
Write-Host "1. Extract the ZIP file" -ForegroundColor White
Write-Host "2. Open ML.csproj in Visual Studio" -ForegroundColor White
Write-Host "3. Run 'dotnet restore' and 'dotnet build'" -ForegroundColor White
Write-Host "4. See PACKAGE_CONTENTS.md for detailed instructions" -ForegroundColor White