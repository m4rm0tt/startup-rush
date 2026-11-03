# Préparation de la distribution (état au 3 novembre 2026)

## Documents obsolètes

`docs/Startup_Rush_Dossier.pdf` et `docs/Startup_Rush_Guide.pdf` décrivent la **version 2.1** (vue de dessus, fondateur
automatique, packs Kenney et 2dPig). Ils **ne correspondent pas** au jeu actuel (vue isométrique du pack original, terminal,
versions préparées puis publiées, sites, matériel, IA, quantique). Ils sont gardés pour l'historique : **ne pas les diffuser**,
ni les présenter comme le guide du jeu. À refaire après le lot 5 (prise en main et objectifs), à partir du jeu réel et de captures
(`UiTests/Shots.cs`). Le générateur `tools/build_dossier.py` (non commité) produisait l'ancien dossier.

Références à jour : `README.md`, `docs/CHECKLIST_STEAM_V1.md`, `docs/CREDITS.md`, `THIRD_PARTY_NOTICES.md`.

## Steam : rien n'est intégré

Aucun identifiant ni SDK Steamworks sur ce poste. Prérequis, dans l'ordre :

1. **Compte Steamworks partenaire** au nom de l'éditeur (frais d'application par jeu, informations fiscales et bancaires).
2. **App ID** du jeu (et App ID de la démo si une démo est prévue).
3. **Build Windows x64** : `dotnet publish UI/UI.csproj -c Release -r win-x64 --self-contained` (à valider sous Windows : sons
   NAudio.WinMM, polices, `%AppData%\StartupRush`), puis signature éventuelle de l'exécutable.
4. **Depot et branche** dans Steamworks, envoi avec SteamPipe (`steamcmd`, script de build `app_build_<appid>.vdf`).
5. **SDK** : Steamworks.NET ou Facepunch.Steamworks, `steam_appid.txt` en développement, initialisation au démarrage avec repli
   propre si Steam n'est pas lancé.
6. **Page boutique** : textes en français et en anglais, captures 1920 × 1080, capsules, bande-annonce, questionnaire sur le
   contenu généré par IA (voir `docs/CREDITS.md`), classification d'âge.
7. **Après les six lots** : succès Steam (liste à définir à partir des objectifs du lot 5) et Steam Cloud (dossier
   `%AppData%\StartupRush\campaigns` et `settings.json` ; exclure `logs` et `deleted`).

## Traduction

Fait (lot 6, `UI/Loc.cs`, `UI/Lang/fr.json` et `en.json`) : écran titre, nouvelle partie, chargement et suppression d'une partie,
réglages (avec le choix de la langue), menu pause, barre d'outils du bas, infobulles et étiquettes de la barre du haut.

Pas encore traduit (reste en français en anglais) : tous les panneaux de jeu (Projets, Équipe, Aménagement, Développement,
Finances, Sites, IA, Recherche, Production, Journal), le terminal et les mini-jeux, le tutoriel, les notifications et le journal
générés par le moteur (`Core`), les catalogues (composants, publics, objets, locaux), les noms des jours (« Lun »), l'état d'une
sauvegarde (« Saine »), le motif de pause automatique. Voir `docs/LOT6_ANGLAIS.md`.
