# Lot 6 : anglais et préparation de la distribution (3 novembre 2026)

## Fait

| Tâche | Détail | Vérification |
|---|---|---|
| Ressources de traduction | `UI/Loc.cs` : `Loc.T(clé, arguments…)` ; tables `UI/Lang/fr.json` (référence) et `UI/Lang/en.json`, embarquées (`UI.Lang.<langue>.json`). Repli : langue choisie → français → la clé elle-même (visible, donc repérable). Arguments formatés au format des nombres du jeu (`Core.GameCulture`) | `LocalizationTest` |
| Langue choisie | `Settings.Language` (« fr » par défaut ; un ancien `settings.json` sans ce champ reste en français). Réglages › Langue : Français / English, appliqué tout de suite (`MainWindow.SetLanguage` : réglage enregistré, barre d'outils reconstruite en jeu, menus reconstruits) | `SwitchingToEnglishRebuildsTheMenusAndTheToolbarAndIsRemembered` |
| Écrans traduits | Titre, nouvelle partie (dont les 3 erreurs du nom d'entreprise, `Loc.CompanyNameError`, le moteur garde ses messages français), chargement (libellés des sauvegardes automatiques, de sortie et d'urgence recalculés d'après le fichier), suppression et sa confirmation, réglages, menu pause, barre d'outils du bas, barre du haut (infobulles des vitesses, pause, journal, menu ; étiquette « nuit accélérée ») | Captures `EnglishShots` regardées : titre, nouvelle partie, chargement, réglages, jeu, pause ; aucun texte coupé ni débordant en 1600 × 900 |
| Cohérence des clés | Chaque clé française a son texte anglais, non vide, avec les mêmes arguments `{0}`, `{1}`… | `EveryFrenchKeyHasAnEnglishTextWithTheSameArguments` |
| Crédits | `docs/CREDITS.md` : seulement ce qui est embarqué, provenance vérifiée par empreinte MD5 (16 bruitages, 4 musiques), polices, bibliothèques (dont Inter, SIL OFL, chargée par `WithInterFont`, absente de l'ancienne notice) | Comparaison des fichiers |
| Distribution | `docs/DISTRIBUTION.md` : les deux PDF (version 2.1) déclarés obsolètes et à ne pas diffuser, prérequis Steamworks (rien d'intégré), état de la traduction | — |

Les icônes des boutons étaient choisies d'après leur libellé français (`Ui.IconFor`) : les boutons traduits passent l'icône du
libellé français (`icon: Ui.IconFor(Loc.French(clé))`), elles restent donc en anglais. Le tutoriel désigne les outils par leur
nom français (`CoachTool`) : ce nom reste la clé interne, seule la barre affiche la traduction.

## Sauvegardes et changement de langue

Rien de traduit n'est enregistré par l'interface. Ce qui est dans une sauvegarde reste dans la langue où il a été écrit :
noms choisis par le joueur (entreprise, produits, sauvegardes manuelles), journal et notifications déjà générés par le moteur
(en français), résumé d'une sauvegarde (`SaveSummary.Situation`, en français). Changer de langue ne modifie aucune sauvegarde.

## Glossaire anglais

Campagne / partie → campaign / game ; Aménagement → Furnish ; Développement (arbre) → Development ; Projets → Projects ;
Équipe → Team ; Journal → Journal ; Sauvegarde automatique → Autosave ; Sauvegarde de sortie → Exit save ; Sauvegarde
d'urgence → Emergency save ; Nuit accélérée → Fast night ; Terminal → terminal ; Graine → seed ; Tutoriel → tutorial.

## Partiel, limites

- **Seuls les menus sont traduits** : panneaux de jeu, terminal, mini-jeux, tutoriel, notifications, catalogues et tout le texte
  produit par `Core` restent en français (liste dans `docs/DISTRIBUTION.md`). Pour la suite : extraire panneau par panneau
  (mêmes tables), puis donner au moteur des clés au lieu de phrases pour les notifications.
- Noms des jours (« Lun ») et dates : `GameTime` écrit en français ; format des nombres suisse romand dans les deux langues.
- Les noms de teintes (infobulles des cases de couleur) restent en français.
- Débordements vérifiés seulement en 1600 × 900, texte normal ; à revoir avec « Texte agrandi » et en 1280 × 720.

## Validations Windows restantes

- Changer de langue en jeu puis dans le menu pause, à la souris ; relancer le jeu : la langue est retenue.
- Relire chaque écran traduit en 1280 × 720 et 1920 × 1080, avec et sans « Texte agrandi ».
