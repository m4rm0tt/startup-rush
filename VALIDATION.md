# Validation de la refonte

Ce qui a été vérifié, comment, et ce qui ne l'a pas été. Environnement : Windows 11 x64, .NET 9.0.302, Avalonia 12.1.3.

## Comment

- **64 tests NUnit** (`dotnet test Tests/Tests.csproj`) sur le moteur sans interface : 54 lancés par défaut, plus la campagne d'équilibrage et 9 sondes lancées à la main.
- **Parcours dans le vrai jeu** : `StartupRush.exe --capture <dossier>` ouvre le jeu, joue un scénario avec les mêmes écrans et commandes qu'un joueur, enregistre des captures de l'interface rendue et un rapport (`docs/captures/capture_report.txt`). Ce ne sont pas des images conceptuelles.
- **Campagne d'équilibrage** : 9 stratégies × 5 graines × 18 mois (`BALANCE_REPORT.md`).

Les parcours d'interface ne sont **pas** couverts par des tests automatiques (pas de banc Avalonia headless dans le projet) : ils sont vérifiés par le parcours de captures et par la lecture des captures.

## Critères d'acceptation

| # | Critère | État | Preuve |
|---|---|---|---|
| 1 | Plus de HUD à colonnes, le local domine, menus ouvrables, retour, raccourcis | ✅ | Captures 02, 07, 12 (au repos : barre du haut, scène, barre d'outils) ; 03, 05, 08, 10 (un seul menu, en-tête avec retour et fermeture) ; Échap : `GameScreen.Escape` (fenêtre → outil → menu → sélection → pause) |
| 2 | Placement, obstruction et attribution agissent sur la capacité réelle | ✅ | `ADeskNeedsAChairBehind…`, `AChairAloneIsNot…`, `FurnitureCannotOverlap…`, `APlacementThatWouldCutOff…`, `BusinessFurnitureStaysOut…`, `ANewHireWithoutAWorkstationDoesNotProduce`, `WithoutAComputerNothingGetsDone` ; captures 03 et 04 (fantôme vert, puis refus en partie privée) |
| 3 | Travail limité par le temps, les rôles, les moyens ; pas de double affectation | ✅ | `TheFounderProducesOnlyAt…WorkHours`, `APersonHasOneAssignment…`, `TasksWaitForTheirDependencies`, `AnOversizedTeamLosesEfficiency…`, `BetterComputersHelpWithDiminishingReturns` |
| 4 | Contrats, encaissements, charges, retards, loyers cohérents ; rien ne se déclenche deux fois | ✅ | `AnInvoiceIsAReceivable…`, `DeliveringOnTime…`, `ARushedPromise…Cancellation`, `NegotiatingASaferDeadline…`, `CapitalizingCosts…`, `AnUnreliableClientMayNeverPay`, `SalariesAndRentAreBilledOncePerMonth…` |
| 5 | Pages et modules personnalisables, aperçu et état de publication différents selon les choix, conservés après reprise | ✅ | `VisualChoicesDoNotChange…`, `UsefulModulesRaiseAppeal…`, `ModulesOnAnUnlinkedPage…`, `TooManyModules…`, `TheDraftBecomesWork…`, `OnlyTheChangesAreRebuilt`, `PagesDraftsAndVersionsSurviveAReload` ; captures 05 (deux pages, thèmes différents, réglages visibles dans l'aperçu) et 06 (plan de travail et prévision) |
| 6 | Qualité, infrastructure, demande, prix, marketing : effets mesurables et expliqués | ✅ | `MarketingBringsUsersWithDiminishingReturns`, `InsufficientHosting…Outages…`, `WithoutPaymentModule…`, `TheMarketplaceNeedsSellers…`, `PriceMatters…`, `ForecastGivesARange…` ; capture 09 (causes listées) |
| 7 | Redressement possible dans un scénario, vraie défaite dans un autre | ✅ | `PayingTheArrearsLiftsTheNotice`, `TheRecoveryPlanNeedsRecurringRevenue…`, `PersistentUnpaidObligations…Liquidation`, `HiringAndSpendingWithoutRevenue…`, `ASlightlyNegativeBalanceIsNotAGameOver` ; capture 14 (bilan de liquidation) ; bilan : 2 stratégies sur 9 n'ont jamais été liquidées, 3 l'ont été à chaque fois |
| 8 | Sauvegarde, redémarrage : même état ; campagnes isolées | ✅ | `ReloadingRestoresTheExactStateAndTheSameFuture` (deux copies avancées de 20 jours restent identiques), `ReloadingDoesNotDuplicate…`, `CampaignsAreStoredSeparately…` ; rapport de capture : « Rechargement identique : True » |
| 9 | Anciennes sauvegardes préservées, sauvegarde abîmée gérée | ✅ | `TheLegacyRoguelikeProfileIsRead…NeverModified`, `AnOldRoguelikeFile…`, `ACorruptSaveIsKept…` |
| 10 | Temps indépendant du framerate, pause et vitesses fiables, pas d'inflation au chargement, listes bornées | ✅ en partie | Le temps est le temps réel mesuré × vitesse, plafonné à 0,1 s par image (`GameSession.Update`) ; rechargement identique (8). Listes bornées : notifications 150, mouvements 800, historique produit 400 jours, contrats et factures anciens élagués. **Pas de test de longue durée de la mémoire de l'interface.** |
| 11 | Lisible en petite fenêtre et en 1080p, clavier et souris, mise à l'échelle, pas de clic au travers | ✅ en partie | Captures 11 (960 × 540) et 12 (1920 × 1080) ; échelle de l'interface automatique ou réglée ; les cadres des menus captent la souris (`PixelPanel.HitTest`), la scène ne reçoit que les clics hors des menus. Mise à l'échelle Windows : zoom entier calculé en pixels physiques ; **seulement vu à 100 %**. |
| 12 | Ressources présentes et sons déclenchés depuis le build | ✅ en partie | Rapport de capture : 16 sons, 4 musiques et 25 sprites chargés depuis les ressources embarquées ; `EveryFurnitureHasASprite…`. Les sons partent (liste dans le rapport) mais **n'ont pas été écoutés**. |

## Boucle de jeu

La tranche verticale demandée est jouable de bout en bout et a été rejouée par le parcours de captures : nouvelle campagne → appartement → poste posé (chaise, bureau, PC) → contrat accepté → produit créé → deux pages construites → version planifiée avec prévision → sauvegarde → rechargement identique. Les bots ont joué la suite (développement, publication, premiers clients et coûts, embauches, déménagement, redressement, liquidation) sur 18 mois.

## Ce que dit l'équilibrage (voir les chiffres dans BALANCE_REPORT.md)

- **Une vraie période de survie** : 7 000 CHF et 2 360 CHF de charges ; sans contrat dans les premières semaines, la trésorerie part dans le découvert.
- **Choix réel produit contre revenus immédiats** : le fondateur seul sur un produit (produit_tot) finit avec environ 3 000 CHF de revenus récurrents après 18 mois mais une trésorerie maigre ; le freelance prudent accumule plus de 100 000 CHF mais n'a rien de récurrent ni d'équipe.
- **La croissance coûte** : embaucher vite donne des trajectoires très dispersées (une partie liquidée, une autre très rentable) ; la croissance financée par du marketing sur une app communautaire a été liquidée 5 fois sur 5 ; le marketing agressif sur une place de marché aussi.
- **Erreurs rattrapables, erreurs fatales** : impayés passagers suivis d'un retour à la normale dans plusieurs parties (mois en impayés > 0 sans liquidation) ; accumulation de charges sans revenus → liquidation.
- **Pas de séquence d'achats dominante** : le matériel améliore un rendement précis avec plafonds ; aucune stratégie ne gagne en achetant.

### Limites de ces mesures

- Les bots jouent des règles simples, une décision par jour ouvré. Ils ne relancent pas un client, n'ajustent pas les prix, ne lisent pas les explications. Un humain fera mieux sur certaines stratégies, moins bien sur d'autres.
- **Déséquilibre connu** : sur 18 mois, l'agence bien gérée rapporte nettement plus que les produits, et la stratégie produit la plus soignée a été liquidée 3 fois sur 5 (elle embauche avant d'avoir des revenus). Les produits ont été renforcés pendant le réglage mais restent plus lents à payer ; à recalibrer avec de vraies parties (tailles de marché, conversion, salaires).
- Les valeurs proposées sont toutes dans `Core/campaign/Tuning.cs` et les catalogues ; rejouer la campagne prend environ 1 min 30.
