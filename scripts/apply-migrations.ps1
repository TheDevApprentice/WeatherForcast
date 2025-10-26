# ========================================
# Script d'Application des Migrations
# Pour Dev, Staging et Production
# ========================================

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory=$false)]
    [string]$DbHost = "localhost",
    
    [Parameter(Mandatory=$false)]
    [int]$DbPort = 5432,
    
    [Parameter(Mandatory=$false)]
    [string]$DbName = "weatherforecastdb",
    
    [Parameter(Mandatory=$false)]
    [string]$DbUser = "weatheruser",
    
    [Parameter(Mandatory=$false)]
    [string]$DbPassword = "weatherpass"
)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  APPLICATION DES MIGRATIONS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Environnement : $Environment" -ForegroundColor Yellow
Write-Host "Base de données : ${DbHost}:${DbPort}/${DbName}" -ForegroundColor Yellow
Write-Host ""

# Aller dans le dossier infra
Set-Location -Path "$PSScriptRoot\..\infra"

# Construire la connection string
$connectionString = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$DbUser;Password=$DbPassword"
$env:ConnectionStrings__DefaultConnection = $connectionString

# Vérifier que PostgreSQL est accessible
Write-Host "[1/3] Vérification de la connexion à PostgreSQL..." -ForegroundColor Yellow
try {
    $testConnection = & docker exec weatherforecast-db pg_isready -U $DbUser 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ PostgreSQL accessible" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠️  PostgreSQL non accessible via Docker, tentative de connexion directe..." -ForegroundColor Yellow
}

# Vérifier le build
Write-Host "`n[2/3] Vérification du build..." -ForegroundColor Yellow
dotnet build -c Release --nologo | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build réussi" -ForegroundColor Green
} else {
    Write-Host "   ❌ Erreur de build" -ForegroundColor Red
    Set-Location -Path $PSScriptRoot
    exit 1
}

# Appliquer les migrations
Write-Host "`n[3/3] Application des migrations..." -ForegroundColor Yellow
dotnet ef database update --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Migrations appliquées avec succès" -ForegroundColor Green
} else {
    Write-Host "   ❌ Échec de l'application des migrations" -ForegroundColor Red
    Set-Location -Path $PSScriptRoot
    exit 1
}

Set-Location -Path $PSScriptRoot

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "✅ Migrations appliquées avec succès !" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Green
