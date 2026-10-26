# Startup Rush

**De fauché à roi du monde.** Un roguelike de gestion en pixel art pour Windows.

Tu commences dans une chambre miteuse, avec 150 CHF et un vieux portable. Tu charbonnes, tu acceptes des contrats et tu paies le loyer chaque mois. Peu à peu, tu améliores ton matériel, tu déménages, tu fondes des boîtes, tu embauches et tu investis, jusqu'à 1 milliard de CHF et une holding. Deux factures impayées d'affilée, et la run est perdue.

## Jouer

1. Décompresse l'archive `StartupRush-<version>-win-x64.zip` où tu veux.
2. Lance `StartupRush.exe`.

Rien d'autre à installer : .NET est inclus dans l'exécutable (Windows 10 ou 11, 64 bits).

## Contrôles

| Touche | Action |
|---|---|
| Espace, ou clic sur la scène | Charbonner (travail en plus et petit gain). Une touche maintenue ne compte qu'une fois |
| 1 à 4 | Choisir une carte d'atout ou une option d'événement |
| R | Relancer le tirage d'atouts |
| Entrée | Continuer, lancer la run |
| Échap | Pause, retour |
| F11 | Plein écran |

Tout se joue aussi à la souris.

## Réglages et sauvegarde

- **Réglages** : depuis la pause, tu changes le volume de la musique et celui des bruitages (de 0 à 100 % par pas de 20 %), et tu peux réduire les secousses et les flashs.
- **Sauvegarde** : le profil (lingots, Héritage, personnages débloqués, records, réglages) est dans `%AppData%\StartupRush\profile.json`. Il est réécrit sans risque de fichier à moitié écrit. Le supprimer remet tout à zéro.
- **Runs** : une run n'est pas sauvegardée en cours de partie. Quitter le jeu l'abandonne.

## Construire le jeu

Il faut le SDK .NET 9.

```
dotnet build StartupRush.sln
dotnet test Tests/Tests.csproj
dotnet run --project UI
```

Pour l'archive Windows autonome :

```
dotnet publish UI/UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o publish/StartupRush
```

Sons, sprites et polices sont embarqués dans l'exécutable. À côté, il ne reste que les licences des polices et `THIRD_PARTY_NOTICES.md`.

## Projet

| Dossier | Contenu |
|---|---|
| `Core` | Toute la logique du jeu, sans interface (simulation, roguelike, méta-progression) |
| `UI` | L'interface Avalonia : rendu pixel, HUD, sons |
| `Tests` | Tests NUnit, bot de jeu et campagne d'équilibrage |
| `assets` | Les packs originaux téléchargés (non versionnés) |
| `tools/build_assets.py` | Copie et adapte les ressources utilisées dans `UI/Assets` |

Documents :
- `ASSET_USAGE.md` : ressources utilisées, adaptées et laissées de côté.
- `THIRD_PARTY_NOTICES.md` : licences et crédits.
- `BALANCE_REPORT.md` : protocole et résultats de l'équilibrage.

## Limites connues

- **Testé seulement sur Windows 11 x64.**
- **Audio non écouté** : sons et musiques se chargent sans erreur, mais je ne les ai pas écoutés. Volumes et choix sont à valider à l'oreille.
- **Équilibrage par bot** : il a été mesuré avec un bot. Un humain joue plus lentement, seules de vraies parties confirmeront la difficulté.
- **Pression 3** : les styles de jeu prudents y gagnent encore toujours ; seul le joueur qui dépense sans réserve y perd.
