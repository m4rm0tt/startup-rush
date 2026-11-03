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

## Prochaine action (reprise)

1. Relancer `WriteBalanceReport` (5 graines, ~45 min) après le changement de prix Makers et l'objectif final, mettre à jour `BALANCE_REPORT.md`.
2. Traduire les panneaux de jeu (Projets, Équipe, Aménagement, Finances, Développement), puis tutoriel et notifications du moteur.
3. Vérifier par simulation le délai pour atteindre 2 M de valorisation.
4. Passer la liste Windows de `BUGFIX_VALIDATION.md` (section « Phase version Steam »).
