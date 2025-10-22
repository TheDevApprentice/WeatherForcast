# Script pour demarrer PostgreSQL (Docker) et creer les migrations

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  SETUP BASE DE DONNEES POSTGRESQL" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# 1. Verifier que Docker est installe et demarre
Write-Host "[1/5] Verification de Docker..." -ForegroundColor Yellow
$dockerRunning = docker info 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERREUR] Docker n'est pas demarre ou installe" -ForegroundColor Red
    Write-Host "Veuillez demarrer Docker Desktop et reessayer" -ForegroundColor Yellow
    exit 1
}
Write-Host "   [OK] Docker est pret" -ForegroundColor Green

# 2. Arrêter et supprimer les anciens containers/volumes
Write-Host "`n[2/5] Nettoyage des anciens containers..." -ForegroundColor Yellow
docker-compose.dev down -v 2>$null

# 3. Demarrer le container PostgreSQL
Write-Host "`n[3/5] Demarrage du container PostgreSQL..." -ForegroundColor Yellow
docker-compose.dev up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "   [OK] Container PostgreSQL demarre" -ForegroundColor Green
} else {
    Write-Host "   [ERREUR] Echec du demarrage du container" -ForegroundColor Red
    exit 1
}

# 4. Attendre que PostgreSQL soit pret
Write-Host "`n[4/5] Attente que PostgreSQL soit pret..." -ForegroundColor Yellow
$maxRetries = 30
$retries = 0
while ($retries -lt $maxRetries) {
    $healthCheck = docker inspect --format='{{.State.Health.Status}}' weatherforecast-db 2>$null
    if ($healthCheck -eq "healthy") {
        Write-Host "   [OK] PostgreSQL est pret" -ForegroundColor Green
        break
    }
    $retries++
    Write-Host "   Attente... ($retries/$maxRetries)" -ForegroundColor Gray
    Start-Sleep -Seconds 2
}

if ($retries -eq $maxRetries) {
    Write-Host "   [ERREUR] Timeout: PostgreSQL n'est pas devenu pret" -ForegroundColor Red
    exit 1
}

# 5. Installer dotnet-ef et creer les migrations
Write-Host "`n[5/6] Creation des migrations..." -ForegroundColor Yellow
Set-Location -Path "$PSScriptRoot\..\infra"

Write-Host "   Installation de dotnet-ef (si necessaire)..." -ForegroundColor Gray
dotnet tool install --global dotnet-ef 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "   dotnet-ef deja installe" -ForegroundColor Gray
}

Write-Host "   Suppression des anciennes migrations..." -ForegroundColor Gray
Remove-Item -Path ".\Data\Migrations" -Recurse -ErrorAction SilentlyContinue

Write-Host "   Verification du build..." -ForegroundColor Gray
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "   [ERREUR] Erreurs de compilation detectees ci-dessus" -ForegroundColor Red
    Set-Location -Path $PSScriptRoot
    exit 1
}

Write-Host "   Creation de la migration initiale..." -ForegroundColor Gray
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=weatherforecastdb;Username=weatheruser;Password=weatherpass"
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

if ($LASTEXITCODE -eq 0) {
    Write-Host "   [OK] Migration creee avec succes" -ForegroundColor Green
} else {
    Write-Host "   [ERREUR] Echec de la creation de la migration" -ForegroundColor Red
    Set-Location -Path $PSScriptRoot
    exit 1
}

# 6. Appliquer les migrations
Write-Host "`n[6/6] Application des migrations sur PostgreSQL..." -ForegroundColor Yellow
dotnet ef database update

if ($LASTEXITCODE -eq 0) {
    Write-Host "   [OK] Base de donnees PostgreSQL creee avec succes" -ForegroundColor Green
} else {
    Write-Host "   [ERREUR] Echec de l'application des migrations" -ForegroundColor Red
    Set-Location -Path $PSScriptRoot
    exit 1
}

Set-Location -Path $PSScriptRoot

# Resume
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  SETUP TERMINE AVEC SUCCES" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "[INFO] Base de donnees PostgreSQL prete:" -ForegroundColor White
Write-Host "   Host: localhost:5432" -ForegroundColor Gray
Write-Host "   Database: weatherforecastdb" -ForegroundColor Gray
Write-Host "   User: weatheruser" -ForegroundColor Gray

Write-Host "`n[INFO] Vous pouvez maintenant lancer les applications:" -ForegroundColor Yellow
Write-Host "   Terminal 1: cd application && dotnet run" -ForegroundColor Cyan
Write-Host "   Terminal 2: cd api && dotnet run" -ForegroundColor Cyan

Write-Host "`n[OK] Application et API partageront la meme base PostgreSQL`n" -ForegroundColor Green
