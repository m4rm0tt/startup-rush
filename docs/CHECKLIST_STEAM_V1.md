# Checklist : première version Steam (phase du 3 novembre 2026)

Périmètre : six lots dans l'ordre (consigne du 3 novembre 2026, qui remplace la liste des « 100 améliorations », gardée comme
réserve d'idées). Branche `new-assets`, commits sous `m4rm0tt` / `esteban.madrazo-bb@proton.me`.

Statuts : **fait** (vérifié par un test automatique ou une capture regardée, précisé à chaque ligne), **partiel**, **à faire**,
**bloqué**. **Windows** = reste à valider à la souris sous Windows (jamais coché ici : la session tourne sur un VPS Linux sans écran).

## État vérifié avant de commencer

| Point | Constat (vérifié dans le code, pas seulement dans les anciennes notes) |
|---|---|
| Curseur qui clignote (BUG-001) | Cause supprimée (`UiPatch`, curseur du pack sur tout l'écran), tests `StabilityTest`. **Windows** |
| Double-clic qui déplace un objet (BUG-004) | `ClickGuard` (350 ms), test `ADoubleClickDoesNotGoThroughToAButtonThatJustAppeared`. **Windows** |
| Nom d'entreprise obligatoire | Fait (`Campaign.CompanyNameError`, `TesterRequestsTest`, capture `lot1_newgame_error`) |
| Déménagement qui ouvre la mauvaise section (BUG-014) | Fait (`BuildPanel.AtCartons`, test `OpenTheCartonsGoesToTheCartonsSection`). **Windows** |
| `moving_box` | **Déjà intégré** (commit `9e1cb36`) : cartons à l'entrée du siège tant que des objets attendent d'être déballés, clic = contenu réel et ouverture de l'aménagement. `V1_ETAT.md` disait encore « sans fonction » : obsolète |
| 4e icône de vitesse (BUG-013) | Confirmé : la vitesse 4 réutilisait l'icône 3 |
| Personnages assis manquants, opérateurs debout | Non reproduit / déjà fait (voir lot 1) |
| Sauvegardes | Une campagne = un fichier ; à vérifier (emplacements, sauvegardes automatiques, écriture atomique) |
| Format des nombres | Confirmé : dépend de la langue du système (« 492,406 CHF » sur ce VPS en anglais) |

## Lot 1 — Bugs et interactions

| Tâche | Statut | Détail |
|---|---|---|
| 4e icône de vitesse (BUG-013) | fait | `PackLibrary.SpeedFour` : chevron exact de `icon_speed_3`, répété 4 fois. Test `SpeedIconTest`, capture regardée |
| Personnages assis manquants | vérifié, non reproduit | Moteur : chaque personne au travail a sa chaise (`SeatedProbe`, 96 vérifications, grand local, 6 et 10 recrues). Rendu : chaque personne assise est dessinée (`SeatedRenderTest`, 40 personnes). Une recrue sans poste complet attend à la porte avec une alerte (BUG-015), c'est voulu. Tests gardés en régression. **Windows** : regarder une vraie partie |
| Opérateurs de production à leur station | déjà fait | Assis devant leur station, sur la case de travail du pack (`RoomRenderer.OperatorSeat`) ; la note « bulle près d'une station » de `V1_ETAT.md` était périmée |
| Lisibilité, marges | partiel | Relu en capture (BUG-009) ; tailles de fenêtre au lot 3 |
| Liste `BUGFIX_VALIDATION.md` | **Windows** | Validation à la souris, impossible ici |

## Lot 2 — Sauvegardes fiables

Déjà là avant cette phase (vérifié dans `SaveStore.cs` et `SaveTest`) : un dossier par partie, sauvegardes manuelles nommées,
3 automatiques tournantes, sauvegarde de sortie, écriture par fichier temporaire + remplacement avec `.bak`, fichier abîmé
gardé en `.corrupt` et signalé dans la liste, migrations des formats 4 à 7 testées, copie d'avant migration (`.v<n>.json`).

| Tâche | Statut | Détail |
|---|---|---|
| Plusieurs parties (nom, date, chargement) | fait (existant) | Écran « Charger » : une rubrique par partie (nom — entreprise), date de jeu, date d'écriture. Capture `steam_load` regardée |
| Trois sauvegardes automatiques tournantes | fait (existant) | `CampaignsAreStoredSeparatelyAndAutosavesRotate` |
| Écriture robuste | fait | Temporaire vidé jusqu'au disque (`Flush(true)`) avant remplacement ; en cas d'échec, l'ancien fichier reste intact et le temporaire est effacé (`AFailedWriteLeavesThePreviousSaveIntactAndNoTemporaryFile`) |
| Récupération depuis une sauvegarde automatique | fait | Fichier puis `.bak` illisibles : la sauvegarde lisible la plus récente de la même partie est chargée, avec un message (`WhenTheSaveAndItsBackupAreUnreadableTheLatestAutosaveIsLoaded`) |
| Anciennes sauvegardes | fait (existant) | Formats 4 à 7 testés ; les copies d'avant migration ne sont plus listées comme sauvegardes (« Automatique 7 », `MigrationCopiesAreNotListedAsSaves`) |
| Confirmation avant suppression | fait | « Supprimer la partie » → confirmation → dossier déplacé dans `StartupRush/deleted/` (rien d'effacé). `DeletingACampaignMovesItOutOfTheListWithoutErasingIt`, `LoadScreenTest` |
| **Windows** | à valider | Écriture et remplacement sur NTFS (`File.Replace`), dossier `%AppData%\StartupRush` |

## Lot 3 — Interface, stabilité, performances

| Tâche | Statut | Détail |
|---|---|---|
| Format des nombres centralisé | fait | `Core.GameCulture` (suisse romand : « 15 840 CHF », « 2,5 % », espace fine insécable présente dans les deux polices), installée au démarrage du jeu, des tests et du rapport d'équilibrage. `GameCultureTest` (système en anglais, allemand, français), captures regardées |
| Texte agrandi | fait | Réglage « Texte agrandi » : texte courant, boutons et onglets de 12 à 18 px, appliqué sans relancer. Six menus et la scène relus en capture (rien de coupé). Moins net que la grille de 12 du pack (décision BUG-010 toujours ouverte pour le défaut) |
| Résolutions | fait | `PixelText.AutoScale` : jamais sous 1 dès 1280 × 720 (avant : 0,84, flou), 4K à 100 % → échelle 2 (avant : 1). Bulles de notification qui ne couvrent plus la barre d'outils en 1280 × 720. `AutoScaleTest` ; captures 1280 × 720, 1366 × 768, 1920 × 1080, 2560 × 1080, 3840 × 2160 regardées. **Windows** : mise à l'échelle 125 / 150 % |
| Performances | mesuré | `PerfProbe` : simulation ~1 ms par image, 5 jours ~85 ms, interface seule ~12 ms ; une image complète ~500 ms **sur ce VPS sans carte graphique**, même avec une scène vide (redessin de toute la fenêtre au processeur), donc pas un goulot du code. Essai « bitmap figé » sans effet, retiré. `AnAdvancedCampaignRendersAFrameQuickly` reste en échec ici. **Windows** : mesurer avec une carte graphique |
| Journaux d'erreur, dossier, sauvegarde d'urgence | fait | `CrashLog` : journal dans `%AppData%\StartupRush\logs` (pile, version, système, partie), sauvegarde d'urgence `crash.json` sans toucher aux autres ; bouton « Ouvrir le dossier des journaux ». `CrashLogTest` |
| Plein écran / fenêtré, volumes | fait | Plein écran dans les réglages et F11, retenu ; volumes musique et bruitages déjà présents |
| Écran de chargement | non nécessaire | Démarrage rapide : pas de barre ajoutée |

## Lot 4 — Équilibrage (agent, fusionné)

| Tâche | Statut | Détail |
|---|---|---|
| Parcours mesurés | fait (3 graines) | 12 parcours × 3 graines × 18 mois (`Tests/balance/SteamBalanceProbe.cs`, sur demande) : aucune faillite dans les parcours joués correctement ; marketing intensif 2 faillites sur 3, corrigé à temps 129 206 CHF ; IA en salle serveur et quantique lents mais viables. Détail : `docs/equilibrage/bilan_steam_v1.md` |
| Impasse corrigée : carte pour makers | fait | Prix de référence du public Makers 65 → 95 CHF (seul changement) ; sur 36 mois, graines 11/23/37 : 18 545 → 107 034, 61 920 → 72 210, 73 070 → 84 561 CHF ; 11 autres parcours inchangés sur 18 mois |
| Rapport complet 5 graines | à faire | `WriteBalanceReport` (~45 min sur le VPS) pas relancé après le changement |

## Lot 5 — Prise en main et objectifs (agent + moi, fusionné)

| Tâche | Statut | Détail |
|---|---|---|
| Objectif final | fait | 2 000 000 CHF de valorisation (`Campaign.Valuation`, celle des investisseurs), sans impayés, un produit en ligne ; une seule fois ; `Campaign.Goals.cs`, `GoalsTest` (6) |
| Objectifs progressifs | fait | Les 3 prochains jalons existants, dans Finances › Objectifs, sans récompense nouvelle |
| Bilan réussite / faillite | fait | Durée, argent, revenus et dépenses, pire creux, utilisateurs, équipe, valorisation, 5 premiers produits ; « Continuer la partie » ou « Menu principal ». `GoalsUiTest`, captures regardées |
| Charges, revenus, causes d'échec | fait | « ? » dans Finances ; étape « L'argent » du tutoriel réécrite (capture `steam_tutorial_money`) |
| Seuil de 2 M vérifié par simulation | à faire | Lu dans le rapport 36 mois, pas simulé |

## Lot 6 — Anglais et distribution (agent, fusionné)

| Tâche | Statut | Détail |
|---|---|---|
| Traduction | partiel | `UI/Loc.cs`, `UI/Lang/fr.json` / `en.json`, langue dans les réglages (défaut français) : menus (titre, nouvelle partie, charger/supprimer, réglages, pause), barres du haut et du bas. **Pas traduits** : panneaux de jeu, terminal, tutoriel, notifications et catalogues du moteur. `LocalizationTest` (3), captures anglaises regardées |
| Sauvegardes et langue | fait | Changer de langue ne modifie aucune sauvegarde ; textes déjà générés gardés dans leur langue (documenté dans `docs/LOT6_ANGLAIS.md`) |
| Crédits et licences | fait | `docs/CREDITS.md` (sons et musiques Juhani Junkala CC0 vérifiés par empreinte, polices), `THIRD_PARTY_NOTICES.md` complété (Inter, SkiaSharp/HarfBuzz) |
| Documents obsolètes | fait | `docs/DISTRIBUTION.md` : les deux PDF décrivent la version 2.1, à ne pas distribuer |

## Steam (après les six lots)

Steamworks : **bloqué**, aucun compte partenaire, App ID ni SDK sur ce poste. Prérequis listés dans `docs/DISTRIBUTION.md` ; rien n'est intégré.

## État de la reprise du 4 novembre 2026 (session interrompue par la limite d'usage)

### Terminé et vérifié (tests automatiques ou captures regardées)
- **Texte courant à 18 px** (12 px seulement pour les touches minuscules) ; **texte agrandi à 24 px**, barres du haut et du bas laissées à 18 px (`PixelText.Hud`) ; arbre de développement en colonnes qui passent à la ligne ; état du terminal sur plusieurs lignes ; nom du local tronqué dans la barre du haut. Captures FR et EN regardées en 1280 × 720 et 1600 × 900 (`FinalShots`, catégorie Shots).
- **Localisation anglaise complète des panneaux, du terminal, du tutoriel et des messages du moteur** : moteur de phrases (`Loc.Translate`, `docs/LOT6_PHRASES.md`), ~4 500 entrées (`UI/Lang/phrases/en/`), 3 zones faites par agents puis fusionnées, conflits résolus (test `PhraseFilesDoNotContradictEachOther…`). Couverture : tests par zone + `EnglishCoverage_NoticesTest` (journal de 8 parties de bots) + `EnglishCoverage_AllPanelsTest` (tous les panneaux, tutoriel, pause, bilans, menus). Reste volontairement en français : noms propres, adresses, mots à taper du terminal.
- **Rapport d'équilibrage complet, 5 graines** (`docs/equilibrage/bilan_steam_v1_final.md` + CSV, révision e4a20d1) : identique à l'ancien rapport sauf `materiel` (prix Makers 95 CHF : médiane 14 589 → 19 829 CHF à 18 mois ; 36 mois 37 380 → 107 034). **À relancer** après la correction ci-dessous (la valorisation sert aux offres d'investisseurs).
- **Bug corrigé** : le résultat d'un produit matériel ne comptait ni les matières vendues ni les garanties (le coût du jour était écrasé dans `Campaign.Daily.cs`) : la valorisation du matériel était gonflée de 3 à 4 fois (PC : 3,6 M à 24 mois, 1,8 M corrigé). Test `HardwareResultTest`. Moteur 259/259.
- **Objectif final ajusté à 2,5 M de valorisation tenus 30 jours de suite** (avant : 2 M, un seul jour). Mesures : `docs/equilibrage/objectif_*.md` (sonde `GoalTimeProbe`). Médianes des bots : plusieurs_petits 44 mois (2 M) / 50 mois (2,5 M) ; quantique 52 / 61 ; un_projet 30 / 57 (2 graines sur 3) ; IA, XR, matériel : jamais en 72 mois (pic 1,1 à 1,8 M) parce que les bots n'embauchent ni ne s'agrandissent. Durée de jeu réelle : **estimation seulement** (modèle : jours simulés × 24 à 36 s selon la vitesse × 0,7 pour les nuits accélérées + mots tapés × 1,5 s + décisions × 45 s) ≈ 12 à 18 h pour 2 M, ≈ 17 à 22 h pour 2,5 M sur la voie « plusieurs projets ». Non démontré par une partie humaine : **procédure de validation** ci-dessous.
- **Tests** : moteur 259/259 ; interface 473/474 avant les derniers changements (puis ciblés : 35/35 couverture et objectifs). Seul échec connu : `AnAdvancedCampaignRendersAFrameQuickly`.
- **Succès Steam préparés** (13, `Campaign.AchievementDefs`, déduits de l'état) + Cloud, dépôt SteamPipe, boutique : `docs/steam/` (aucun identifiant inventé).
- **Build Windows** : `dotnet publish UI/UI.csproj -c Release -r win-x64 --self-contained` réussit sur le VPS (UI.exe PE32+, 219 Mo, avant la traduction) ; **non lancé sous Windows**. À refaire avec la commande monofichier du README.

### Performance : conclusion corrigée
Mesures du 4 novembre (`PerfProbe`) : Skia seul remplit 1600 × 900 en 0,5 ms ; une fenêtre Avalonia vide ou la vraie `MainWindow` vide coûte 46 à 49 ms par image capturée ; dans l'écran de jeu, dès que `SceneCanvas` dessine quoi que ce soit (même 50 × 50 px) l'image coûte 500 à 700 ms, et 8 à 20 ms si elle ne dessine rien. **L'échec n'est donc pas expliqué par la seule absence de GPU** (conclusion du 3 novembre à retirer) ; cause non trouvée sans profileur. À mesurer sur Windows avec carte graphique puis profiler.

### Reste à faire
1. Relancer `WriteBalanceReport` (5 graines, ~70 min) sur le dernier commit, mettre à jour `BALANCE_REPORT.md`, relancer `GoalTimeProbe` (étiquette `apres2`, 2,5 M) et écrire `docs/equilibrage/objectif_final.md` (tableau avant/après, hypothèses ; fichiers bruts dans le scratchpad de la session précédente, à regénérer).
2. Mettre à jour `docs/LOT5_OBJECTIFS.md` (« Pourquoi 2 millions ») et `docs/PASSATION_IA.md` pour 2,5 M / 30 jours.
3. Vérification visuelle : texte agrandi + anglais en 1920 × 1080 et 4K ; stat « Bureaux en place » coupée dans BuildPanel en texte agrandi ; relire les captures `FinalShots` des autres panneaux.
4. Relancer la suite UI complète ; profiler le rendu de la scène (voir ci-dessus).
5. Mettre à jour `docs/BUGFIX_VALIDATION.md` (section Windows : 18 px, texte agrandi, anglais, objectif 30 jours) et `docs/DISTRIBUTION.md` (traduction terminée, PDF : voir `docs/GUIDE_JOUEUR.md`).
6. Les PDF : `tools/build_dossier.py` demande Python, reportlab, Pillow et les polices Windows (absents ici) ; aucun script pour le guide illustré.
7. Les 100 idées « vraiment intéressantes » demandées : **non faites** (l'ancienne liste est dans `docs/IDEES_100_AMELIORATIONS.md`).

### À valider par toi (Windows) et à fournir
- Liste Windows de `docs/BUGFIX_VALIDATION.md` (curseur, double-clic, déménagement, personnages assis, sauvegardes, mise à l'échelle 125/150 %, plein écran).
- Une partie réelle chronométrée pour valider l'objectif : noter l'heure de début, la vitesse habituelle, et l'heure où le bilan « Objectif atteint » s'affiche (cible 15 à 25 h) ; la sauvegarde et la durée aident à recaler `FinalGoalValuation`.
- Steamworks : compte partenaire, App ID, SDK (`docs/steam/README.md`).

