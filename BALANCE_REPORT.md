# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 8 façons de jouer × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu, fondateur polyvalent, réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. Aucune prestation client n'existe dans cette version.

## Résumé

| Stratégie | Faillites | Argent final (médiane) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) |
|---|---|---|---|---|---|---|---|---|---|
| un_projet | 0/5 | 50 036 | 11 123 | 23 | 150 (5/5) | 24 791 | 3 179 | 10 | 38 |
| plusieurs_petits | 0/5 | 172 230 | 10 677 | 23 | 210 (5/5) | 38 135 | 1 675 | 16 | 67 |
| ambitieux | 0/5 | 92 034 | -1 902 | 24 | 30 (5/5) | 15 326 | 7 637 | 13 | 30 |
| jeu_simple | 0/5 | 9 892 | 7 826 | 30 | 330 (5/5) | 3 016 | 3 583 | 8 | 25 |
| marketing_excessif | 5/5 | -736 | -1 987 | 23 | 150 (5/5) | 5 527 | 897 | 6 | 20 |
| exces_puis_correction | 0/5 | 50 001 | -399 | 23 | 150 (5/5) | 19 347 | 2 479 | 10 | 35 |
| recrutement_precoce | 5/5 | -916 | -1 999 | 11 | 60 (4/5) | 974 | 229 | 7 | 24 |
| trop_de_projets | 5/5 | -502 | -1 632 | 29 | 120 (5/5) | 947 | 499 | 5 | 23 |

## Façons de jouer

- **un_projet** : Se concentrer sur une application, l'améliorer d'après les avis, embaucher quand elle rapporte.
- **plusieurs_petits** : Plusieurs petits projets l'un après l'autre (application, jeu, application pour PME).
- **ambitieux** : Un jeu ambitieux pour passionnés : beaucoup de composants, une embauche dès le départ, promotion avant la sortie.
- **jeu_simple** : Un petit jeu grand public, puis des mises à jour régulières.
- **marketing_excessif** : Erreur : 6 000 CHF de marketing par mois dès la sortie d'une petite application.
- **exces_puis_correction** : Même erreur de marketing, mais corrigée dès que l'argent devient tendu (budget coupé, prêt si possible).
- **recrutement_precoce** : Erreur : trois embauches dès le départ, avant le moindre revenu.
- **trop_de_projets** : Erreur : cinq projets ouverts d'un coup, chacun avec 1 500 CHF de promotion par mois avant même sa sortie.

## Détail

| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |
|---|---|---|---|---|---|---|---|
| un_projet | 11 | en activité | 540 | 12 436 → 13 871 → 28 481 → 34 149 → 36 893 → 54 120 | 2 | Ton appartement, Prilly | 8 |
| un_projet | 23 | en activité | 540 | 12 550 → 14 956 → 30 712 → 35 810 → 36 549 → 54 096 | 2 | Ton appartement, Prilly | 8 |
| un_projet | 37 | en activité | 540 | 12 592 → 14 879 → 29 146 → 23 098 → 25 810 → 24 926 | 2 | Ton appartement, Prilly | 8 |
| un_projet | 41 | en activité | 540 | 12 458 → 13 859 → 28 175 → 35 961 → 37 279 → 50 036 | 2 | Ton appartement, Prilly | 8 |
| un_projet | 59 | en activité | 540 | 12 511 → 14 423 → 28 565 → 28 425 → 28 845 → 43 069 | 2 | Ton appartement, Prilly | 8 |
| plusieurs_petits | 11 | en activité | 540 | 12 467 → 12 027 → 16 746 → 35 048 → 90 016 → 172 230 | 1 | Ton appartement, Prilly | 8 |
| plusieurs_petits | 23 | en activité | 540 | 12 572 → 12 219 → 17 443 → 37 652 → 96 006 → 178 111 | 1 | Ton appartement, Prilly | 8 |
| plusieurs_petits | 37 | en activité | 540 | 12 612 → 12 243 → 18 661 → 38 283 → 97 245 → 176 460 | 1 | Ton appartement, Prilly | 8 |
| plusieurs_petits | 41 | en activité | 540 | 12 488 → 11 460 → 15 011 → 39 675 → 92 644 → 170 871 | 1 | Ton appartement, Prilly | 8 |
| plusieurs_petits | 59 | en activité | 540 | 12 535 → 12 186 → 17 393 → 37 291 → 92 076 → 158 416 | 1 | Ton appartement, Prilly | 8 |
| ambitieux | 11 | en activité | 540 | 10 953 → 13 802 → 29 010 → 46 456 → 62 340 → 71 660 | 1 | Ton appartement, Prilly | 8 |
| ambitieux | 23 | en activité | 540 | 11 995 → 17 521 → 36 168 → 57 935 → 77 846 → 92 034 | 1 | Ton appartement, Prilly | 8 |
| ambitieux | 37 | en activité | 540 | 9 726 → 21 990 → 9 432 → 5 437 → 2 884 → 9 994 | 1 | Ton appartement, Prilly | 7 |
| ambitieux | 41 | en activité | 540 | 17 060 → 30 993 → 57 064 → 86 232 → 113 957 → 133 790 | 1 | Ton appartement, Prilly | 8 |
| ambitieux | 59 | en activité | 540 | 16 934 → 24 716 → 44 346 → 65 895 → 86 047 → 101 768 | 1 | Ton appartement, Prilly | 8 |
| jeu_simple | 11 | en activité | 540 | 14 499 → 12 022 → 12 449 → 15 446 → 20 200 → 25 398 | 0 | Ton appartement, Prilly | 8 |
| jeu_simple | 23 | en activité | 540 | 13 956 → 10 483 → 9 292 → 9 266 → 9 497 → 9 892 | 0 | Ton appartement, Prilly | 7 |
| jeu_simple | 37 | en activité | 540 | 14 108 → 10 928 → 9 666 → 9 333 → 9 244 → 9 685 | 0 | Ton appartement, Prilly | 7 |
| jeu_simple | 41 | en activité | 540 | 13 767 → 10 085 → 8 538 → 8 159 → 8 397 → 9 302 | 0 | Ton appartement, Prilly | 7 |
| jeu_simple | 59 | en activité | 540 | 14 473 → 12 372 → 13 268 → 17 013 → 21 552 → 26 071 | 0 | Ton appartement, Prilly | 8 |
| marketing_excessif | 11 | faillite | 176 | 6 446 → -87 | 0 | Ton appartement, Prilly | 5 |
| marketing_excessif | 23 | faillite | 207 | 6 585 → 284 | 0 | Ton appartement, Prilly | 6 |
| marketing_excessif | 37 | faillite | 207 | 6 648 → 741 | 0 | Ton appartement, Prilly | 6 |
| marketing_excessif | 41 | faillite | 176 | 6 458 → -1 742 | 0 | Ton appartement, Prilly | 5 |
| marketing_excessif | 59 | faillite | 176 | 6 533 → -920 | 0 | Ton appartement, Prilly | 5 |
| exces_puis_correction | 11 | en activité | 540 | 6 444 → 2 855 → 12 357 → 19 886 → 25 661 → 30 117 | 0 | Ton appartement, Prilly | 5 |
| exces_puis_correction | 23 | en activité | 540 | 6 583 → 4 030 → 20 307 → 29 750 → 36 659 → 56 138 | 1 | Ton appartement, Prilly | 8 |
| exces_puis_correction | 37 | en activité | 540 | 6 646 → 4 444 → 22 151 → 30 942 → 45 533 → 72 551 | 1 | Ton appartement, Prilly | 8 |
| exces_puis_correction | 41 | en activité | 540 | 6 456 → 2 918 → 17 268 → 30 223 → 34 601 → 50 001 | 1 | Ton appartement, Prilly | 8 |
| exces_puis_correction | 59 | en activité | 540 | 6 531 → 4 044 → 20 095 → 25 832 → 18 039 → 20 385 | 1 | Ton appartement, Prilly | 8 |
| recrutement_precoce | 11 | faillite | 72 | -1 652 | 0 | Petit bureau, Flon | 5 |
| recrutement_precoce | 23 | faillite | 162 | -257 → 997 | 0 | Ton appartement, Prilly | 6 |
| recrutement_precoce | 37 | faillite | 72 | -1 471 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 41 | faillite | 72 | -916 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 59 | faillite | 72 | -60 | 0 | Petit bureau, Flon | 6 |
| trop_de_projets | 11 | faillite | 146 | -1 528 | 0 | Ton appartement, Prilly | 5 |
| trop_de_projets | 23 | faillite | 146 | -1 522 | 0 | Ton appartement, Prilly | 5 |
| trop_de_projets | 37 | faillite | 146 | -1 479 | 0 | Ton appartement, Prilly | 5 |
| trop_de_projets | 41 | faillite | 146 | -1 519 | 0 | Ton appartement, Prilly | 5 |
| trop_de_projets | 59 | faillite | 146 | -1 505 | 0 | Ton appartement, Prilly | 5 |
