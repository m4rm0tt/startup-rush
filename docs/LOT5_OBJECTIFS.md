# Lot 5 : objectifs, bilan de partie, explications (version Steam, 3 novembre 2026)

Branche de travail `lot5-objectifs` (partie de `new-assets` à `3d19455`).

## Ce qui est fait

| Point | État | Détail |
|---|---|---|
| Objectif final explicite | fait | « Une entreprise valorisée 2 millions de CHF » : `Campaign.Valuation()` (la valorisation déjà utilisée par les investisseurs : actif net + 36 mois du bénéfice récurrent des produits) ≥ 2 000 000 CHF, entreprise saine (pas d'impayés), au moins un produit en ligne. Pas de bourse. `Core/campaign/Campaign.Goals.cs` |
| Objectifs progressifs | fait | Les trois prochains jalons non atteints (`Campaign.MilestoneDefs`, ordre existant : premier lancement, première embauche, vrais locaux, 1 000 utilisateurs…). **Aucune récompense nouvelle** : les jalons gardent leurs points habituels (0 à 2). Affichés dans Finances › Objectifs, avec la progression vers l'objectif final |
| Bilan de réussite | fait | Une seule fois quand l'objectif est atteint : durée, argent, revenus et dépenses mensuels, pire creux de trésorerie, utilisateurs, équipe, valorisation, cinq principaux produits. « Continuer la partie » (la partie continue normalement, le bilan ne revient pas) ou « Menu principal » |
| Bilan de faillite | fait | L'écran « Entreprise liquidée » affiche le même bilan sous le rapport de liquidation |
| Charges, revenus, causes d'échec | fait | Finances : « ? » sur « Où va l'argent chaque mois » (charges fixes, salaires le 25, impayés → mise en demeure après 20 jours → faillite) et « D'où il vient » (revenus des projets en ligne, montée progressive des utilisateurs) |
| Tutoriel | inchangé | Le tutoriel de 11 étapes existant (`Campaign.Tutorial.cs`) n'a pas été repris dans ce lot, faute de temps |

État sauvegardé (propriétés avec setters, valeurs par défaut sûres pour une ancienne sauvegarde) : `FinalGoalReachedAt` (null),
`FinalGoalAcknowledged` (false), `LowestCash` (null jusqu'au premier soir). Le format de sauvegarde n'a pas changé de numéro :
une ancienne sauvegarde se charge avec ces valeurs par défaut.

## Pourquoi 2 millions

D'après `BALANCE_REPORT.md` (horizon 36 mois, 5 graines) : `plusieurs_petits` finit à ≈ 449 000 CHF avec ≈ 39 000 CHF de revenus
par mois ; avec le bénéfice des produits sur 36 mois, sa valorisation est de l'ordre de 1,7 à 1,9 M à 3 ans, donc l'objectif tombe
vers 3 à 4 ans. `materiel_pc` (≈ 80 000 CHF/mois) l'atteint plus tôt. Les stratégies faibles (`materiel` d'entrée, `quantique`)
restent loin à 36 mois. **Non vérifié par une simulation dédiée** : ces chiffres sont lus dans le rapport, pas mesurés pour
ce lot. À confirmer en ajoutant la date d'atteinte de l'objectif au rapport d'équilibrage (lot 4).

## Tests

- Moteur : `Tests/campaign/GoalsTest.cs` (6 tests : pas atteint au départ, l'argent seul sans produit ne suffit pas, atteint une
  seule fois et la partie continue, rechargement, prochains jalons, bilan). Namespaces `Tests.campaign` (79) et
  `Tests.persistence` (18) passent.
- Interface : `UiTests/GoalsUiTest.cs` (bilan une seule fois puis « Continuer », objectifs dans Finances). Suite UiTests complète :
  437 / 438, seul échec `AnAdvancedCampaignRendersAFrameQuickly` (rendu processeur du VPS, déjà en échec avant ce lot).
- La campagne avancée des tests (`PanelsTest.Advanced`, 3 M CHF) atteint l'objectif : son bilan est marqué déjà vu pour ne pas
  interrompre les tests des menus.
- Captures regardées : `GoalsShots` (bilan de réussite, section Objectifs).

## Limites et validations Windows restantes

- **Windows** : voir le bilan à la souris, « Continuer la partie » puis reprendre au clavier ; charger une sauvegarde antérieure à ce lot.
- Le bilan de réussite met le temps en pause comme la faillite ; « Continuer » rend la main au rythme choisi avant.
- Pas de nouvel objectif après l'objectif final (la partie continue en bac à sable).
- Tutoriel non retravaillé ; traductions (lot 6) non faites pour ces textes.
