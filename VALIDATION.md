# Validation : version « tycoon »

Environnement : Windows 11 x64, .NET 9.0.302, Avalonia 12.1.3. Travail fait **sur le PC local** (pas dans le cloud) : le SDK y était déjà installé, et le jeu y a été compilé, testé et lancé.

## Avant / après

| | Avant (simulation à prestations) | Après |
|---|---|---|
| Démarrage | 7 000 CHF ; il fallait signer des prestations pour survivre | 18 000 CHF ; le premier projet sort en environ 3 semaines, sans aucun client |
| Créer un produit | Éditeur de pages : pages, menus, modules, options, textes | Choisir un type et un public, puis des composants à niveaux dans un atelier |
| Production | Affecter chaque personne à un contrat, au développement, au support ou à la maintenance | Personne en « Auto » sur le projet prioritaire ; design, développement et tests s'enchaînent ; l'entretien prend une part fixe du temps |
| Retours | Tableau de facteurs à interpréter | Avis d'utilisateurs groupés par tendance, avec un bouton vers le composant ou le réglage concerné |
| Progression | Jalons sans effet | Arbre de développement : 5 branches, 17 nœuds à effet réel, points gagnés par de vrais succès |
| Équipe | Affectations multiples, augmentations libres | Auto ou un projet ; congés ; formation ; deux augmentations par an au plus, coût affiché |
| Menus | 7 menus denses (Produits et Projets désignaient deux choses différentes) | 5 menus : Projets, Équipe, Aménagement, Développement, Finances. Vue immédiate, détails repliés, aide d'une phrase la première fois |
| Notifications | Une ligne par événement | Problèmes regroupés et résolus automatiquement ; trois catégories |

## Tests (65 lancés par défaut, plus la campagne d'équilibrage et 8 sondes)

| Thème | Tests |
|---|---|
| Début sans contrats | `TheFirstProjectIsReadyInAFewWeeksSolo`, `AFounderAloneShipsAndEarnsWithoutClientWork` (première sortie entre 10 et 60 jours et premiers revenus avant 90 jours, sur 3 graines) |
| Production automatique | `TheFounderWorksAutomaticallyOnTheNewProject…`, `DesignThenCodeThenTestsFollowWithoutReassignment`, `AutomaticModeFollowsThePortfolioPriority`, `APersonWorksOnOneProjectAtATime`, `UpkeepIsAutomaticAndKeepsALiveProjectHealthy` |
| Composants | `ComponentsRaiseAppealWithDiminishingReturns`, `TheProjectColorDoesNotChangeTheSimulation`, `QueueRulesExplainWhatIsMissing`, `CancellingAComponentAlsoCancels…`, `AnUpdateAfterLaunchRaisesTheVersion`, `ALaunchNeedsAtLeastTwoComponents` |
| Jeux | `AGameSellsItsWishlistOnLaunchDay` |
| Avis reliés au réel | `AMissingFeatureShowsUpAndPointsToARealComponent`, `BuildingTheSuggestedComponentMakesTheComplaintGoAway`, `SlowServersAndUnansweredUsersAreReported`, `ThereIsAlsoGoodNewsAndTheFeedStaysShort` |
| Arbre | `TheStartOffersAFirstChoice…`, `UnlockingNeedsPointsAndPrerequisites`, `AnUnlockedTypeBecomesPlayable`, `EmptyProjectsEarnNoPointsAndEachAchievementCountsOnce` (pas d'abus par des projets vides) |
| Équipe | `VacationRestoresEnergy…`, `RaisesShowTheirCostAndAreLimited`, `TrainingIsCheaperWithTheAcademy`, `ANewHireWithoutAWorkstationDoesNotProduce` |
| Marché | `MarketingBringsUsersWithDiminishingReturns`, `AutomaticHostingFollowsTheLoadUnderItsCap`, `InsufficientHostingCausesOutages…`, `PriceMattersAccordingToTheAudience`, `ForecastGivesARange`, `ClosingAProjectStopsItsCosts` |
| Argent et faillite | `ASlightlyNegativeBalanceIsNotAGameOver`, `PersistentUnpaidObligationsLeadToNoticeThenLiquidation` (alertes regroupées), `PayingTheArrearsLiftsTheNotice`, `TheRecoveryPlan…`, `HiringBeforeAnyRevenueLeadsToBankruptcy` |
| Notifications | `RepeatedProblemsAreGroupedAndResolvedOnceFixed` |
| Sauvegardes | `ReloadingRestoresTheExactStateAndTheSameFuture`, `ComponentsQueuesAndSettingsSurviveAReload`, `AnOldSaveWithContractsIsMigratedWithoutLosingMoney`, `ACorruptSaveIsKept…`, `CampaignsAreStoredSeparately…`, `TheLegacyRoguelikeProfile…` |
| Local | Tests d'aménagement, de chemins, de postes et d'atlas (inchangés) |

## Parcours réel dans le jeu

`StartupRush.exe --capture` joue dans l'interface réelle. Les captures sont dans `docs/captures/` ; le rapport dans `docs/captures/capture_report.txt`.

1. Partie neuve, portefeuille vide, écran « Nouveau projet » (captures 02 à 04).
2. Projet créé ; le fondateur s'y met seul (05, 06). **Prêt en 21 jours, sans aucune affectation manuelle.**
3. « Prêt à sortir », puis lancement (07). 50 jours plus tard : 94 utilisateurs, 43 payants. L'argent est passé de 18 000 à 14 101 CHF, le projet ne couvre pas encore les dépenses.
4. Avis : « Rien ne me donne envie de revenir », avec un lien vers **Messagerie**, marquée « Demandé dans les avis » dans l'atelier (08, 09).
5. Réglages et aperçu (10, 11). La sauvegarde puis le rechargement donnent **exactement le même état**.
6. Campagne avancée jouée par un bot pendant 16 mois : 4 personnes, petit bureau, 3 projets en ligne, 8 nœuds débloqués, **72 décisions en 16 mois** (12 à 19).
7. Petite fenêtre 960 × 540, 1080p, pause, faillite (sauvegarde conservée), menus d'accueil (20 à 25).

## Équilibrage (BALANCE_REPORT.md)

Le protocole : 8 façons de jouer, 5 graines chacune, 18 mois de jeu.

- **Trois approches viables ou plus** : se concentrer sur un projet (`un_projet`), plusieurs petits projets (`plusieurs_petits`), un jeu ambitieux avec une embauche tôt (`ambitieux`), un petit jeu (`jeu_simple`). Aucune faillite sur 5 graines.
- **Le projet ambitieux est risqué** : son pire creux passe par le découvert, mais il paie au lancement.
- **Les erreurs persistantes mènent à la faillite**, 5 fois sur 5 :
  - 6 000 CHF de marketing par mois pour une petite application ;
  - trois embauches avant le moindre revenu ;
  - cinq projets ouverts d'un coup avec beaucoup de promotion.
- **On peut se rattraper** : la même erreur de marketing, corrigée dès que l'argent devient tendu (`exces_puis_correction`), ne mène à aucune faillite (pire creux −399 CHF).
- **Quantité d'actions** : 25 à 67 décisions en 18 mois selon la stratégie, soit quelques-unes par mois. Tout le reste est automatique.

### Limites de ces mesures

- Les bots appliquent des règles simples : ils lisent la première tendance d'avis, font une amélioration à la fois et coupent le marketing quand l'argent devient tendu. **Le plaisir de jeu humain n'a pas été mesuré.**
- La stratégie à plusieurs petits projets rapporte nettement plus que les autres sur 18 mois. C'est voulu (la diversification paie), mais l'écart reste à vérifier avec de vraies parties.
- `jeu_simple` vivote : un petit jeu à 6 CHF ne suffit pas à lui seul.
- Les valeurs sont toutes dans `Core/campaign/Tuning.cs` et les catalogues. Rejouer le bilan prend environ 25 secondes.

## Ce qui n'a pas été vérifié

- La mise à l'échelle Windows au-delà de 100 %.
- Les sons à l'oreille.
- L'intégration des nouveaux assets (pas reçus) et la simulation du matériel ou de la fabrication (non demandée maintenant).
