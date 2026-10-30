# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 14 façons de jouer × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu, fondateur polyvalent, réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. Aucune prestation client n'existe dans cette version.

## Résumé

| Stratégie | Faillites | Argent final (médiane) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) | Mots tapés (méd.) |
|---|---|---|---|---|---|---|---|---|---|---|
| un_projet | 0/5 | 30 149 | 11 774 | 22 | 150 (5/5) | 21 166 | 2 715 | 10 | 38 | 7 264 |
| plusieurs_petits | 0/5 | 198 467 | 12 420 | 22 | 120 (5/5) | 39 443 | 1 856 | 16 | 68 | 11 530 |
| ambitieux | 0/5 | 115 144 | 918 | 25 | 30 (5/5) | 14 298 | 8 411 | 13 | 30 | 3 289 |
| jeu_simple | 0/5 | 14 533 | 5 919 | 26 | 240 (5/5) | 3 343 | 4 093 | 8 | 26 | 4 925 |
| delegation | 0/5 | 86 898 | 1 918 | 22 | 120 (5/5) | 32 329 | 1 578 | 16 | 43 | 6 130 |
| hebergement_maison | 0/5 | 85 817 | 12 420 | 22 | 120 (5/5) | 32 040 | 1 574 | 18 | 107 | 11 530 |
| materiel | 0/5 | 11 940 | -269 | 22 | 120 (5/5) | 21 762 | 1 131 | 19 | 294 | 11 530 |
| ia | 0/5 | 159 758 | 12 448 | 22 | 120 (5/5) | 34 043 | 1 814 | 21 | 81 | 11 522 |
| studio_xr | 0/5 | 37 547 | 12 420 | 22 | 120 (5/5) | 14 608 | 1 202 | 16 | 84 | 11 530 |
| quantique | 0/5 | 73 103 | 12 420 | 22 | 120 (5/5) | 32 058 | 1 691 | 16 | 76 | 11 530 |
| marketing_excessif | 4/5 | -251 | -1 997 | 22 | 180 (5/5) | 5 833 | 734 | 6 | 28 | 4 417 |
| exces_puis_correction | 0/5 | 33 640 | 41 | 22 | 150 (5/5) | 12 879 | 1 690 | 10 | 38 | 10 412 |
| recrutement_precoce | 5/5 | 320 | -1 978 | 10 | 90 (5/5) | 6 984 | 1 070 | 8 | 36 | 2 839 |
| trop_de_projets | 5/5 | -286 | -1 984 | 26 | 120 (5/5) | 1 154 | 576 | 6 | 25 | 3 150 |

## Façons de jouer

- **un_projet** : Se concentrer sur une application, l'améliorer d'après les avis, embaucher quand elle rapporte.
- **plusieurs_petits** : Plusieurs petits projets l'un après l'autre (application, jeu, application pour PME).
- **ambitieux** : Un jeu ambitieux pour passionnés : beaucoup de composants, une embauche dès le départ, promotion avant la sortie.
- **jeu_simple** : Un petit jeu grand public, puis des mises à jour régulières.
- **delegation** : Comme plusieurs_petits, mais le fondateur arrête de taper dès la première embauche : l'équipe développe, lance et prépare les petites mises à jour.
- **hebergement_maison** : Comme plusieurs_petits, puis une salle serveur dès 40 000 CHF : les projets passent sur ses serveurs, le stockage et l'hébergement libres sont vendus.
- **materiel** : Comme plusieurs_petits, puis le matériel dès 35 000 CHF : un atelier, la recherche « Conception de cartes », une carte pour les makers fabriquée sur une ligne d'assemblage, vendue en ligne.
- **ia** : Comme plusieurs_petits, puis l'IA dès 30 000 CHF : un petit modèle d'analyse (calcul loué), évalué, publié, un produit d'IA pour les analystes, une seconde version sur données achetées.
- **studio_xr** : Comme plusieurs_petits, un studio au siège dès 20 000 CHF, puis le matériel et la XR : zone de test au siège et une application XR pour joueurs en quatrième projet.
- **quantique** : Comme plusieurs_petits, tout l'arbre vers le quantique (matériel, usine, salle propre, laboratoire avancé, offres de calcul), un laboratoire dès 300 000 CHF, équipé au fil de la trésorerie.
- **marketing_excessif** : Erreur : 6 000 CHF de marketing par mois dès la sortie d'une petite application.
- **exces_puis_correction** : Même erreur de marketing, mais corrigée dès que l'argent devient tendu (budget coupé, prêt si possible).
- **recrutement_precoce** : Erreur : trois embauches dès le départ, avant le moindre revenu.
- **trop_de_projets** : Erreur : cinq projets ouverts d'un coup, chacun avec 1 500 CHF de promotion par mois avant même sa sortie.

## Jalons du lot 6

Jour où l'étape est atteinte pour la première fois (médiane des graines qui l'atteignent). Mini-jeux d'évaluation et de mise en route joués à 75 %.

| Stratégie | Jalon | Atteint | Jour (méd.) |
|---|---|---|---|
| ia | ia_publiee | 5/5 | 277 |
| ia | produit_ia | 5/5 | 277 |
| studio_xr | studio_pose | 5/5 | 233 |
| studio_xr | studio_complet | 5/5 | 233 |
| studio_xr | zone_xr_posee | 5/5 | 287 |
| studio_xr | zone_xr_complete | 5/5 | 287 |

### Horizon long : quantique sur 36 mois

| Graine | Issue | Argent par trimestre | Nœuds | Jalons |
|---|---|---|---|---|
| 11 | en activité | 12 837 → 14 525 → 21 842 → 35 887 → 59 991 → 113 230 → 213 390 → 45 214 → 91 072 → 145 648 → 198 607 → 250 898 | 9 | labo_avance (j718), calibration_cryo (j764) |
| 23 | en activité | 12 985 → 15 537 → 27 829 → 46 833 → 90 930 → 149 525 → 235 840 → 64 713 → 95 975 → 140 771 → 190 061 → 240 400 | 9 | labo_avance (j707), calibration_cryo (j758) |
| 37 | en activité | 13 099 → 15 866 → 26 577 → 28 056 → 17 951 → 25 918 → 62 026 → 89 958 → 138 929 → 200 927 → 270 661 → 48 274 | 9 | labo_avance (j1024), calibration_cryo (j1071) |
| 41 | en activité | 12 869 → 14 168 → 20 276 → 34 235 → 21 488 → 16 133 → 38 356 → 50 989 → 78 495 → 121 764 → 176 634 → 238 047 | 8 | aucun |
| 59 | en activité | 12 999 → 15 380 → 24 764 → 32 857 → 42 738 → 73 103 → 149 098 → 229 514 → 61 939 → 93 046 → 140 133 → 191 505 | 9 | labo_avance (j798), calibration_cryo (j890) |

## Détail

| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |
|---|---|---|---|---|---|---|---|
| un_projet | 11 | en activité | 540 | 12 764 → 14 706 → 21 113 → 29 888 → 33 538 → 34 492 | 1 | Ta chambre, Prilly | 8 |
| un_projet | 23 | en activité | 540 | 12 916 → 17 390 → 29 649 → 26 617 → 26 500 → 29 114 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 37 | en activité | 540 | 13 033 → 18 578 → 29 780 → 28 511 → 29 646 → 45 026 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 41 | en activité | 540 | 12 797 → 14 345 → 19 448 → 26 668 → 34 254 → 30 149 | 1 | Ta chambre, Prilly | 8 |
| un_projet | 59 | en activité | 540 | 12 929 → 17 370 → 28 893 → 24 216 → 29 081 → 29 976 | 2 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 11 | en activité | 540 | 12 837 → 14 536 → 23 903 → 53 746 → 104 421 → 169 047 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 23 | en activité | 540 | 12 985 → 15 436 → 27 925 → 64 704 → 149 351 → 247 625 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 37 | en activité | 540 | 13 099 → 15 920 → 26 995 → 54 970 → 121 550 → 198 467 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 41 | en activité | 540 | 12 869 → 14 547 → 22 163 → 37 638 → 66 127 → 110 870 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 59 | en activité | 540 | 12 999 → 15 642 → 27 622 → 64 026 → 140 161 → 228 663 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 11 | en activité | 540 | 11 249 → 17 164 → 31 594 → 47 142 → 57 182 → 62 698 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 23 | en activité | 540 | 17 482 → 30 138 → 55 573 → 84 856 → 105 820 → 115 144 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 37 | en activité | 540 | 6 250 → 18 678 → 16 674 → 11 965 → 5 723 → 1 220 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 41 | en activité | 540 | 15 086 → 25 343 → 49 795 → 80 242 → 111 027 → 133 335 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 59 | en activité | 540 | 17 528 → 29 403 → 52 934 → 79 322 → 103 319 → 121 006 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 11 | en activité | 540 | 14 685 → 13 884 → 17 098 → 22 902 → 30 582 → 37 904 | 0 | Ta chambre, Prilly | 8 |
| jeu_simple | 23 | en activité | 540 | 13 888 → 10 217 → 7 147 → 6 297 → 6 207 → 6 499 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 37 | en activité | 540 | 14 261 → 11 913 → 11 796 → 12 212 → 13 210 → 14 533 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 41 | en activité | 540 | 14 263 → 10 910 → 9 458 → 10 945 → 12 501 → 14 387 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 59 | en activité | 540 | 14 716 → 13 423 → 15 030 → 19 311 → 25 584 → 32 662 | 0 | Ta chambre, Prilly | 8 |
| delegation | 11 | en activité | 540 | 12 837 → 14 397 → 22 134 → 34 382 → 52 148 → 86 898 | 2 | Ta chambre, Prilly | 8 |
| delegation | 23 | en activité | 540 | 12 985 → 15 463 → 26 330 → 38 028 → 74 912 → 136 962 | 2 | Ta chambre, Prilly | 8 |
| delegation | 37 | en activité | 540 | 13 099 → 15 751 → 25 486 → 35 324 → 19 920 → 50 919 | 2 | Ta chambre, Prilly | 8 |
| delegation | 41 | en activité | 540 | 12 869 → 14 436 → 20 969 → 34 110 → 37 979 → 39 632 | 1 | Ta chambre, Prilly | 8 |
| delegation | 59 | en activité | 540 | 12 999 → 15 392 → 25 028 → 35 643 → 66 103 → 126 790 | 2 | Ta chambre, Prilly | 8 |
| hebergement_maison | 11 | en activité | 540 | 12 837 → 14 525 → 21 749 → 19 085 → 46 348 → 96 116 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 23 | en activité | 540 | 12 985 → 15 537 → 27 628 → 19 583 → 48 186 → 98 015 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 37 | en activité | 540 | 13 099 → 15 866 → 26 880 → 22 509 → 31 726 → 68 675 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 41 | en activité | 540 | 12 869 → 14 168 → 20 502 → 15 425 → 23 661 → 67 010 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 59 | en activité | 540 | 12 999 → 15 380 → 25 593 → 23 121 → 46 353 → 85 817 | 1 | Ta chambre, Prilly | 9 |
| materiel | 11 | en activité | 540 | 12 837 → 14 525 → 21 749 → 5 883 → 13 536 → 18 353 | 1 | Ta chambre, Prilly | 9 |
| materiel | 23 | en activité | 540 | 12 985 → 15 537 → 27 628 → 9 389 → 18 092 → 11 940 | 2 | Ta chambre, Prilly | 9 |
| materiel | 37 | en activité | 540 | 13 099 → 15 866 → 26 880 → 11 927 → 20 514 → 3 561 | 2 | Ta chambre, Prilly | 9 |
| materiel | 41 | en activité | 540 | 12 869 → 14 168 → 20 502 → 3 399 → 4 694 → 58 642 | 1 | Ta chambre, Prilly | 9 |
| materiel | 59 | en activité | 540 | 12 999 → 15 380 → 25 593 → 17 425 → 21 938 → 3 018 | 2 | Ta chambre, Prilly | 9 |
| ia | 11 | en activité | 540 | 12 889 → 15 789 → 27 095 → 43 893 → 83 738 → 137 587 | 1 | Ta chambre, Prilly | 8 |
| ia | 23 | en activité | 540 | 13 042 → 16 448 → 29 287 → 56 089 → 101 912 → 159 758 | 1 | Ta chambre, Prilly | 8 |
| ia | 37 | en activité | 540 | 13 158 → 17 153 → 29 669 → 44 536 → 97 487 → 175 557 | 1 | Ta chambre, Prilly | 8 |
| ia | 41 | en activité | 540 | 12 966 → 16 218 → 26 459 → 35 578 → 61 903 → 96 037 | 1 | Ta chambre, Prilly | 8 |
| ia | 59 | en activité | 540 | 13 055 → 16 332 → 29 769 → 55 324 → 114 037 → 193 105 | 1 | Ta chambre, Prilly | 8 |
| studio_xr | 11 | en activité | 540 | 12 837 → 14 525 → 18 180 → 29 439 → 28 429 → 31 183 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 23 | en activité | 540 | 12 985 → 15 537 → 24 467 → 35 414 → 50 974 → 81 453 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 37 | en activité | 540 | 13 099 → 15 866 → 23 523 → 34 333 → 39 044 → 45 538 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 41 | en activité | 540 | 12 869 → 14 168 → 16 973 → 27 807 → 21 078 → 17 258 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 59 | en activité | 540 | 12 999 → 15 380 → 21 280 → 30 919 → 32 453 → 37 547 | 1 | Ta chambre, Prilly | 9 |
| quantique | 11 | en activité | 540 | 12 837 → 14 525 → 21 842 → 35 887 → 59 991 → 113 230 | 2 | Ta chambre, Prilly | 8 |
| quantique | 23 | en activité | 540 | 12 985 → 15 537 → 27 829 → 46 833 → 90 930 → 149 525 | 2 | Ta chambre, Prilly | 8 |
| quantique | 37 | en activité | 540 | 13 099 → 15 866 → 26 577 → 28 056 → 17 951 → 25 918 | 2 | Ta chambre, Prilly | 7 |
| quantique | 41 | en activité | 540 | 12 869 → 14 168 → 20 276 → 34 235 → 21 488 → 16 133 | 2 | Ta chambre, Prilly | 7 |
| quantique | 59 | en activité | 540 | 12 999 → 15 380 → 24 764 → 32 857 → 42 738 → 73 103 | 2 | Ta chambre, Prilly | 8 |
| marketing_excessif | 11 | faillite | 176 | 6 619 → -1 101 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 23 | faillite | 207 | 6 870 → 2 448 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 37 | en activité | 540 | 7 050 → -797 → 4 159 → 8 703 → 18 262 → 42 758 | 0 | Ta chambre, Prilly | 7 |
| marketing_excessif | 41 | faillite | 176 | 6 676 → -1 069 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 59 | faillite | 207 | 6 878 → 898 | 0 | Ta chambre, Prilly | 5 |
| exces_puis_correction | 11 | en activité | 540 | 6 616 → 3 482 → 10 094 → 18 524 → 28 588 → 33 823 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 23 | en activité | 540 | 6 867 → 5 981 → 16 384 → 27 676 → 34 419 → 33 613 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 37 | en activité | 540 | 9 047 → 10 226 → 27 131 → 26 889 → 35 561 → 62 337 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 41 | en activité | 540 | 6 674 → 3 563 → 10 381 → 19 110 → 28 813 → 33 640 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 59 | en activité | 540 | 6 875 → 4 807 → 12 177 → 21 027 → 31 511 → 32 792 | 1 | Ta chambre, Prilly | 8 |
| recrutement_precoce | 11 | faillite | 162 | 1 033 → 320 | 0 | Ta chambre, Prilly | 7 |
| recrutement_precoce | 23 | faillite | 72 | -594 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 37 | faillite | 284 | 172 → 560 → -440 | 0 | Petit bureau, Flon | 8 |
| recrutement_precoce | 41 | faillite | 284 | 4 648 → -1 707 → 5 083 | 0 | Petit bureau, Flon | 8 |
| recrutement_precoce | 59 | faillite | 72 | -1 454 | 0 | Petit bureau, Flon | 6 |
| trop_de_projets | 11 | faillite | 146 | -1 355 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 23 | faillite | 146 | -1 348 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 37 | faillite | 146 | -1 386 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 41 | faillite | 146 | -1 320 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 59 | faillite | 146 | -1 433 | 0 | Ta chambre, Prilly | 5 |
