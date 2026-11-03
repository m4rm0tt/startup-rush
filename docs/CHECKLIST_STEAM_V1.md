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
| Format des nombres centralisé (fr-CH) | à faire | |
| Texte agrandi | à faire | |
| Résolutions 1280 × 720 à 4K | à faire | |
| Performances (grande partie, nuit) | à faire | |
| Journaux d'erreur, dossier, dernière sauvegarde valide | à faire | |
| Plein écran / fenêtré, volumes | à faire | |

## Lot 4 — Équilibrage

| Tâche | Statut | Détail |
|---|---|---|
| Logiciel, Studio/XR, matériel, IA/quantique, marketing intensif | à faire | Derniers chiffres : `BALANCE_REPORT.md` du 3 novembre (à revérifier) |

## Lot 5 — Prise en main et objectifs

| Tâche | Statut | Détail |
|---|---|---|
| Tutoriel, explications, objectifs, objectif final, bilan, continuer | à faire | Tutoriel de 11 étapes déjà en place |

## Lot 6 — Anglais et distribution

| Tâche | Statut | Détail |
|---|---|---|
| Ressources de traduction, anglais, crédits, documents | à faire | |

## Steam (après les six lots)

Steamworks : aucun identifiant ni SDK sur ce poste. Prérequis à documenter.
