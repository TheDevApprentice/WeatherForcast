# ========================================
# Script de Génération de Certificat
# Pour Pipeline CI/CD (Windows/PowerShell)
# ========================================

param(
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./certificates",
    
    [Parameter(Mandatory=$false)]
    [string]$CertificateName = "weatherforecast-dataprotection",
    
    [Parameter(Mandatory=$false)]
    [int]$ValidityYears = 5,
    
    [Parameter(Mandatory=$false)]
    [string]$CertificatePassword = $env:CERTIFICATE_PASSWORD
)

# Vérifier que le mot de passe est fourni
if ([string]::IsNullOrEmpty($CertificatePassword)) {
    Write-Error "❌ CERTIFICATE_PASSWORD environment variable is required!"
    Write-Host "Usage: `$env:CERTIFICATE_PASSWORD='YourSecurePassword'; .\generate-certificate.ps1"
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Génération Certificat Data Protection" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Créer le dossier de sortie
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "✅ Dossier créé : $OutputPath" -ForegroundColor Green
}

# Générer le certificat
Write-Host "🔐 Génération du certificat..." -ForegroundColor Yellow

try {
    $cert = New-SelfSignedCertificate `
        -Subject "CN=WeatherForecast DataProtection Production" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -KeyLength 4096 `
        -KeyAlgorithm RSA `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears($ValidityYears) `
        -FriendlyName "WeatherForecast DataProtection (Generated: $(Get-Date -Format 'yyyy-MM-dd'))" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")
    
    Write-Host "✅ Certificat créé avec succès" -ForegroundColor Green
    
    # Afficher les détails
    Write-Host ""
    Write-Host "📋 Détails du Certificat :" -ForegroundColor Cyan
    Write-Host "   Subject      : $($cert.Subject)" -ForegroundColor White
    Write-Host "   Thumbprint   : $($cert.Thumbprint)" -ForegroundColor Yellow
    Write-Host "   NotBefore    : $($cert.NotBefore)" -ForegroundColor White
    Write-Host "   NotAfter     : $($cert.NotAfter)" -ForegroundColor White
    Write-Host "   SerialNumber : $($cert.SerialNumber)" -ForegroundColor White
    Write-Host ""
    
    # Exporter en .pfx (avec clé privée)
    $pfxPath = Join-Path $OutputPath "$CertificateName.pfx"
    $password = ConvertTo-SecureString -String $CertificatePassword -Force -AsPlainText
    
    Write-Host "💾 Export .pfx (avec clé privée)..." -ForegroundColor Yellow
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password | Out-Null
    Write-Host "✅ Exporté : $pfxPath" -ForegroundColor Green
    
    # Exporter en .cer (clé publique uniquement - pour backup)
    $cerPath = Join-Path $OutputPath "$CertificateName.cer"
    Write-Host "💾 Export .cer (clé publique)..." -ForegroundColor Yellow
    Export-Certificate -Cert $cert -FilePath $cerPath -Type CERT | Out-Null
    Write-Host "✅ Exporté : $cerPath" -ForegroundColor Green
    
    # Créer un fichier texte avec le thumbprint
    $thumbprintFile = Join-Path $OutputPath "thumbprint.txt"
    $cert.Thumbprint | Out-File -FilePath $thumbprintFile -Encoding UTF8
    Write-Host "✅ Thumbprint sauvegardé : $thumbprintFile" -ForegroundColor Green
    
    # Créer un fichier .env pour Docker
    $envFile = Join-Path $OutputPath ".env.production"
    @"
# Généré automatiquement le $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
DATAPROTECTION_CERTIFICATE_THUMBPRINT=$($cert.Thumbprint)
CERTIFICATE_PASSWORD=$CertificatePassword
"@ | Out-File -FilePath $envFile -Encoding UTF8
    Write-Host "✅ Fichier .env créé : $envFile" -ForegroundColor Green
    
    # Afficher les instructions
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  ✅ Certificat Généré avec Succès" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📂 Fichiers générés :" -ForegroundColor Cyan
    Write-Host "   - $pfxPath" -ForegroundColor White
    Write-Host "   - $cerPath" -ForegroundColor White
    Write-Host "   - $thumbprintFile" -ForegroundColor White
    Write-Host "   - $envFile" -ForegroundColor White
    Write-Host ""
    Write-Host "🔑 Thumbprint (à utiliser dans la config) :" -ForegroundColor Cyan
    Write-Host "   $($cert.Thumbprint)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "📋 Prochaines Étapes :" -ForegroundColor Cyan
    Write-Host "   1. Copier le fichier .pfx dans le volume Docker certificates" -ForegroundColor White
    Write-Host "   2. Ajouter le thumbprint dans .env : DATAPROTECTION_CERTIFICATE_THUMBPRINT=$($cert.Thumbprint)" -ForegroundColor White
    Write-Host "   3. Redémarrer les containers : docker-compose up -d" -ForegroundColor White
    Write-Host ""
    Write-Host "⚠️  IMPORTANT : Sauvegarder le fichier .pfx en lieu sûr (Azure Key Vault, HashiCorp Vault, etc.)" -ForegroundColor Yellow
    Write-Host ""
    
    # Définir les variables d'environnement pour la pipeline
    if ($env:GITHUB_ACTIONS -eq "true") {
        Write-Host "🔧 Configuration GitHub Actions..." -ForegroundColor Cyan
        Write-Output "CERTIFICATE_THUMBPRINT=$($cert.Thumbprint)" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding UTF8 -Append
        Write-Host "✅ Variable CERTIFICATE_THUMBPRINT exportée" -ForegroundColor Green
    }
    
    if ($env:TF_BUILD -eq "True") {
        Write-Host "🔧 Configuration Azure DevOps..." -ForegroundColor Cyan
        Write-Host "##vso[task.setvariable variable=CERTIFICATE_THUMBPRINT;isOutput=true]$($cert.Thumbprint)"
        Write-Host "✅ Variable CERTIFICATE_THUMBPRINT exportée" -ForegroundColor Green
    }
    
    # Retourner le thumbprint pour utilisation dans la pipeline
    return $cert.Thumbprint
    
} catch {
    Write-Error "❌ Erreur lors de la génération du certificat : $_"
    exit 1
}
