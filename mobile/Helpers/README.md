# DeviceHelper

Helper réutilisable pour détecter le type d'appareil, la plateforme et l'orientation dans une application .NET MAUI.

## 📋 Fonctionnalités

### Types d'appareils détectés
- **Phone** : Téléphone mobile (< 600dp)
- **Tablet** : Tablette (≥ 600dp)
- **Desktop** : Windows ou MacCatalyst

### Plateformes supportées
- Android
- iOS
- Windows
- MacCatalyst

### Orientations
- Portrait (hauteur > largeur)
- Landscape (largeur > hauteur)

## 🚀 Utilisation

### Méthodes principales

```csharp
using mobile.Helpers;

// Détection du type d'appareil
var deviceType = DeviceHelper.GetDeviceType(); // Phone, Tablet, Desktop
bool isPhone = DeviceHelper.IsPhone();
bool isTablet = DeviceHelper.IsTablet();
bool isDesktop = DeviceHelper.IsDesktop();

// Détection de l'orientation
var orientation = DeviceHelper.GetOrientation(); // Portrait, Landscape
bool isPortrait = DeviceHelper.IsPortrait();
bool isLandscape = DeviceHelper.IsLandscape();

// Détection de la plateforme
var platform = DeviceHelper.GetPlatform(); // Android, iOS, Windows, MacCatalyst

// Combinaisons utiles
bool isTabletLandscape = DeviceHelper.IsTabletLandscape();
bool isPhonePortrait = DeviceHelper.IsPhonePortrait();

// Déterminer le layout approprié
bool useDesktopLayout = DeviceHelper.ShouldUseDesktopLayout(); // Desktop ou Tablette paysage
bool useMobileLayout = DeviceHelper.ShouldUseMobileLayout();   // Téléphone ou Tablette portrait

// Dimensions de l'écran
double width = DeviceHelper.GetScreenWidth();   // en dp
double height = DeviceHelper.GetScreenHeight(); // en dp

// Informations complètes
string info = DeviceHelper.GetDeviceInfo();
// Exemple: "Platform: Android, Type: Tablet, Orientation: Landscape, Size: 1024x768dp"
```

## 💡 Exemples d'utilisation

### Exemple 1 : Layout responsive

```csharp
private void ApplyResponsiveLayout()
{
    if (DeviceHelper.ShouldUseDesktopLayout())
    {
        // Layout desktop : popup en bas à droite
        this.HorizontalOptions = LayoutOptions.End;
        this.VerticalOptions = LayoutOptions.End;
        this.Margin = new Thickness(0, 0, 20, 20);
        ChatWindow.MaximumWidthRequest = 360;
        ChatWindow.MaximumHeightRequest = 500;
    }
    else
    {
        // Layout mobile : plein écran
        this.HorizontalOptions = LayoutOptions.Fill;
        this.VerticalOptions = LayoutOptions.Fill;
        this.Margin = new Thickness(0);
        ChatWindow.MaximumWidthRequest = double.PositiveInfinity;
        ChatWindow.MaximumHeightRequest = double.PositiveInfinity;
    }
}
```

### Exemple 2 : Réagir aux changements d'orientation

```csharp
public MyControl()
{
    InitializeComponent();
    
    // Appliquer le layout initial
    ApplyResponsiveLayout();
    
    // S'abonner aux changements d'orientation
    DeviceDisplay.MainDisplayInfoChanged += (s, e) =>
    {
        MainThread.BeginInvokeOnMainThread(() => ApplyResponsiveLayout());
    };
}
```

### Exemple 3 : Adapter le contenu selon l'appareil

```csharp
private void LoadContent()
{
    if (DeviceHelper.IsPhone())
    {
        // Afficher une version simplifiée pour téléphone
        ShowSimplifiedView();
    }
    else if (DeviceHelper.IsTabletLandscape())
    {
        // Afficher une vue en colonnes pour tablette paysage
        ShowMultiColumnView();
    }
    else
    {
        // Vue par défaut
        ShowDefaultView();
    }
}
```

### Exemple 4 : Logging et debug

```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    
    // Afficher les informations de l'appareil
    System.Diagnostics.Debug.WriteLine($"📱 {DeviceHelper.GetDeviceInfo()}");
    
    // Exemple de sortie:
    // 📱 Platform: Android, Type: Tablet, Orientation: Landscape, Size: 1024x768dp
}
```

## 🎯 Cas d'usage

### Layout adaptatif
Utilisez `ShouldUseDesktopLayout()` pour décider entre un layout compact (desktop/tablette paysage) ou plein écran (mobile).

### Navigation
Adaptez la navigation selon l'appareil (menu latéral sur desktop, bottom bar sur mobile).

### Colonnes
Affichez plusieurs colonnes sur tablette paysage et desktop, une seule sur mobile.

### Taille des éléments
Ajustez la taille des boutons, marges et espacements selon le type d'appareil.

## 📐 Seuil de détection

Le helper utilise **600dp** comme seuil pour distinguer téléphone et tablette, conformément aux recommandations Android et iOS :
- **< 600dp** : Téléphone
- **≥ 600dp** : Tablette

## ⚡ Performance

Le helper est optimisé pour être léger :
- Pas de cache (les valeurs sont recalculées à chaque appel)
- Utilise `DeviceDisplay.MainDisplayInfo` natif de .NET MAUI
- Calculs simples basés sur la densité d'écran

## 🔄 Réactivité

Pour réagir aux changements d'orientation en temps réel, abonnez-vous à l'événement `DeviceDisplay.MainDisplayInfoChanged` :

```csharp
DeviceDisplay.MainDisplayInfoChanged += OnDisplayInfoChanged;

private void OnDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
{
    MainThread.BeginInvokeOnMainThread(() => ApplyResponsiveLayout());
}
```

## 📝 Notes

- Les valeurs sont en **dp (density-independent pixels)**, pas en pixels physiques
- Desktop (Windows/MacCatalyst) est toujours considéré comme devant utiliser le layout desktop
- Le helper fonctionne sur toutes les plateformes .NET MAUI

## 🛠️ Fichiers

- **DeviceHelper.cs** : Classe helper principale
- **README.md** : Documentation (ce fichier)
