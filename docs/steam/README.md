# Préparation Steam (rien n'est publié, aucun identifiant inventé)

État vérifié le 4 novembre 2026 sur ce poste : **aucun** compte Steamworks partenaire, **aucun** App ID, **aucun** SDK
(`steam_api*`, `steamcmd`, `steam_appid.txt`, paquet NuGet Steamworks : introuvables). Rien ci-dessous ne peut donc être
testé contre Steam ; tout est préparé pour être branché le jour où ces éléments existent.

## Prérequis à fournir (par toi)

| Élément | Statut | Où |
|---|---|---|
| Compte Steamworks partenaire (éditeur, fiscalité, banque, frais par jeu) | **manquant** | partner.steamgames.com |
| App ID du jeu (et de la démo si prévue) | **manquant** | Steamworks › App Admin |
| SDK : Steamworks.NET **ou** Facepunch.Steamworks (NuGet) | **non ajouté** (rien à brancher sans App ID) | — |
| Build Windows testé à la souris | compilation seulement (voir `BUGFIX_VALIDATION.md`) | `dotnet publish UI/UI.csproj -c Release -r win-x64 --self-contained` |
| Icônes de succès 256 × 256 (gagné / non gagné), capsules, captures 1920 × 1080, bande-annonce | **à produire** | — |

## Ce qui est prêt

- **Succès** : `Campaign.AchievementDefs` (13 succès, `Core/campaign/Campaign.Achievements.cs`) déduits de l'état de la partie, donc
  sans nouvelle donnée de sauvegarde et sans risque d'envoi en double ; titres et descriptions fr/en dans `achievements.json`
  (test `AchievementsTest` : les deux listes doivent rester identiques). Branchement futur : après chaque jour ou chaque
  sauvegarde, `foreach (name in campaign.EarnedAchievements) SteamUserStats.SetAchievement(name)` puis `StoreStats()`, avec repli
  silencieux si Steam n'est pas lancé.
- **Steam Cloud** (Auto-Cloud, à saisir dans Steamworks › Cloud) : racine `WinAppDataRoaming`, sous-dossier `StartupRush`.
  À synchroniser : `campaigns/**/*.json` (parties, sauvegardes automatiques, sauvegarde de sortie) et `settings.json`.
  À exclure : `*.tmp`, `*.bak`, `*.corrupt`, `*.v?.json` (copies de migration), `logs/`, `deleted/`, `campaigns/**/crash.json`
  (sauvegarde d'urgence propre à la machine). Quota conseillé : 20 Mo (une partie fait quelques centaines de Ko).
  Attention : deux machines qui jouent la même partie produisent un conflit ; Steam propose alors de choisir.
- **Dépôt SteamPipe** : `app_build.vdf.template` (valeurs `<APP_ID>` et `<DEPOT_ID>` à remplacer ; rien d'inventé).
- **Page boutique** : textes fr/en dans `store_page.md` (à relire, aucune promesse non vérifiée).

## Ce qui reste (bloqué par les prérequis)

1. Créer le compte et l'App ID, saisir succès et Cloud, produire les visuels.
2. Ajouter le SDK et l'initialisation (`steam_appid.txt` en développement), brancher `EarnedAchievements`.
3. Tester sous Windows avec Steam lancé, puis sans Steam.
4. Déposer un build sur une branche privée, tester via Steam, puis seulement publier (non fait ici, non autorisé).
