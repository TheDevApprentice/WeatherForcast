# ============================================================================
# MAUI Self-Signed Build Script for Testing (PowerShell)
# Builds Release version for Android (APK) and Windows Desktop (EXE)
# ============================================================================

param(
    [string]$KeystoreName = "mauiapp_selfsigned",
    [string]$KeyAlias = "mauiapp",
    [string]$KeyPassword = "SelfSignedTestPassword123",
    [string]$AppId = "com.companyname.mauiapp",
    [string]$AppTitle = "MAUI App",
    [string]$AppVersion = "1"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ScriptsDir = Split-Path -Parent $ScriptDir
$RootDir = Split-Path -Parent $ScriptsDir
$MobileFolder = Join-Path $RootDir "mobile"
$KeystorePath = Join-Path $MobileFolder "$KeystoreName.keystore"
$OutputDir = Join-Path $MobileFolder "bin\Release"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MAUI SELF-SIGNED BUILD SCRIPT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $MobileFolder)) {
    Write-Host "ERROR: The 'mobile' folder does not exist!" -ForegroundColor Red
    exit 1
}

Write-Host "Mobile folder: $MobileFolder" -ForegroundColor Yellow

Write-Host ""
Write-Host "--- Step 1: Creating Self-Signed Keystore ---" -ForegroundColor Green

if (Test-Path $KeystorePath) {
    Write-Host "WARNING: Keystore already exists: $KeystorePath" -ForegroundColor Yellow
    $response = Read-Host "Do you want to regenerate it? (y/n)"
    if ($response -eq "y") {
        Remove-Item $KeystorePath -Force
        Write-Host "Old keystore removed" -ForegroundColor Yellow
    }
    else {
        Write-Host "Using existing keystore" -ForegroundColor Green
    }
}

if (-not (Test-Path $KeystorePath)) {
    Write-Host "Generating self-signed keystore..." -ForegroundColor Cyan
    
    try {
        keytool -help | Out-Null
    }
    catch {
        Write-Host "ERROR: keytool not found. Ensure JDK is installed." -ForegroundColor Red
        exit 1
    }
    
    $cmd = @(
        "-genkeypair",
        "-v",
        "-keystore", $KeystorePath,
        "-alias", $KeyAlias,
        "-keyalg", "RSA",
        "-keysize", "2048",
        "-validity", "10000",
        "-dname", "CN=Self-Signed Test,OU=Testing,O=Company,L=City,ST=State,C=FR",
        "-storepass", $KeyPassword,
        "-keypass", $KeyPassword
    )
    
    & keytool @cmd
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to create keystore" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Keystore created successfully" -ForegroundColor Green
}

Write-Host "Keystore ready: $KeystorePath" -ForegroundColor Green

Write-Host ""
Write-Host "--- Step 2: Building Release ---" -ForegroundColor Green

Push-Location $MobileFolder

Write-Host "Building Release version..." -ForegroundColor Cyan
dotnet build -c Release -v normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Release build failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "Release build completed" -ForegroundColor Green

Write-Host ""
Write-Host "--- Step 3: Publishing Android (APK) ---" -ForegroundColor Green

Write-Host "Generating signed APK..." -ForegroundColor Cyan

$dotnetPublishCmd = @(
    "publish",
    "-f", "net9.0-android",
    "-c", "Release",
    "-p:ApplicationId=$AppId",
    "-p:ApplicationTitle=$AppTitle",
    "-p:ApplicationVersion=$AppVersion",
    "-p:AndroidKeyStore=true",
    "-p:AndroidSigningKeyStore=$KeystorePath",
    "-p:AndroidSigningKeyAlias=$KeyAlias",
    "-p:AndroidSigningKeyPass=$KeyPassword",
    "-p:AndroidSigningStorePass=$KeyPassword",
    "-p:AndroidPackageFormats=apk"
)

& dotnet @dotnetPublishCmd

if ($LASTEXITCODE -eq 0) {
    Write-Host "APK created successfully" -ForegroundColor Green
    $apkPath = Get-ChildItem -Path "$OutputDir\net9.0-android\publish" -Filter "*-signed.apk" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($apkPath) {
        Write-Host "APK: $($apkPath.FullName)" -ForegroundColor Cyan
    }
}
else {
    Write-Host "ERROR: APK generation failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host ""
Write-Host "--- Step 4: Publishing Windows Desktop ---" -ForegroundColor Green

Write-Host "Generating Windows executable..." -ForegroundColor Cyan

$dotnetPublishWin = @(
    "publish",
    "-f", "net9.0-windows10.0.19041.0",
    "-c", "Release"
)

& dotnet @dotnetPublishWin

if ($LASTEXITCODE -eq 0) {
    Write-Host "Windows build created successfully" -ForegroundColor Green
    $exePath = Get-ChildItem -Path "$OutputDir\net9.0-windows10.0.19041.0\publish" -Filter "*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($exePath) {
        Write-Host "EXE: $($exePath.FullName)" -ForegroundColor Cyan
    }
}
else {
    Write-Host "ERROR: Windows EXE generation failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "BUILD COMPLETED SUCCESSFULLY" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  - Keystore: $KeystorePath"
Write-Host "  - Alias: $KeyAlias"
Write-Host "  - Output folder: $OutputDir"
Write-Host ""
Write-Host "Generated files:" -ForegroundColor Yellow
Write-Host "  - APK Android: $OutputDir\net9.0-android\publish\*-signed.apk"
Write-Host "  - EXE Windows: $OutputDir\net9.0-windows10.0.19041.0\publish\*.exe"
Write-Host ""
Write-Host "WARNING: These files are self-signed for testing only." -ForegroundColor Yellow
Write-Host "For production, use a proper signing certificate!" -ForegroundColor Yellow
Write-Host ""