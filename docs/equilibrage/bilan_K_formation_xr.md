# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 20 façons de jouer × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu, fondateur polyvalent, réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. Aucune prestation client n'existe dans cette version.

## Résumé

| Stratégie | Faillites | Argent final (médiane) | Argent final (min – max) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) | Mots tapés (méd.) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| un_projet | 0/5 | 49 684 | 35 978 – 63 327 | 11 574 | 22 | 150 (5/5) | 25 681 | 3 155 | 10 | 44 | 8 538 |
| plusieurs_petits | 0/5 | 51 992 | 37 226 – 66 295 | 12 332 | 22 | 120 (5/5) | 17 478 | 1 033 | 16 | 60 | 11 509 |
| ambitieux | 0/5 | 189 262 | 138 573 – 203 743 | 9 641 | 24 | 30 (5/5) | 16 532 | 10 187 | 13 | 33 | 3 684 |
| jeu_simple | 0/5 | 24 871 | 19 121 – 34 408 | 14 362 | 26 | 120 (5/5) | 6 034 | 6 798 | 11 | 34 | 8 614 |
| delegation | 0/5 | 19 578 | 11 282 – 29 351 | 11 041 | 22 | 120 (5/5) | 8 049 | 658 | 13 | 43 | 8 036 |
| hebergement_maison | 0/5 | 20 214 | 19 539 – 40 721 | 8 194 | 22 | 120 (5/5) | 25 042 | 1 271 | 19 | 87 | 11 509 |
| materiel | 0/5 | 16 775 | 3 966 – 31 600 | 3 249 | 22 | 120 (5/5) | 24 243 | 1 268 | 17 | 70 | 11 509 |
| ia | 0/5 | 57 922 | 37 554 – 80 986 | 12 355 | 22 | 120 (5/5) | 19 809 | 1 306 | 20 | 77 | 11 503 |
| studio_xr | 0/5 | 34 471 | 31 131 – 49 018 | 12 332 | 22 | 120 (5/5) | 16 144 | 1 757 | 18 | 81 | 11 479 |
| quantique | 0/5 | 42 417 | 28 463 – 47 188 | 12 332 | 22 | 120 (5/5) | 20 040 | 1 140 | 16 | 61 | 11 509 |
| un_projet_pme | 0/5 | 100 420 | 49 055 – 118 115 | 12 149 | 22 | 120 (5/5) | 35 863 | 1 341 | 10 | 44 | 7 093 |
| materiel_industrie | 0/5 | 33 608 | 10 567 – 58 502 | 1 371 | 22 | 120 (5/5) | 47 126 | 1 183 | 19 | 112 | 11 509 |
| materiel_pc | 0/5 | 74 647 | 51 907 – 101 897 | -1 855 | 22 | 120 (5/5) | 98 992 | 1 063 | 19 | 94 | 11 509 |
| materiel_serveur | 0/5 | 34 863 | 18 274 – 40 150 | -1 821 | 22 | 120 (5/5) | 78 208 | 920 | 20 | 102 | 11 509 |
| materiel_gpu | 0/5 | 38 596 | 33 210 – 55 918 | 12 332 | 22 | 120 (5/5) | 20 382 | 1 200 | 16 | 63 | 11 509 |
| ia_salle_serveur | 0/5 | 19 996 | 11 002 – 27 374 | -1 523 | 22 | 120 (5/5) | 20 111 | 1 181 | 21 | 103 | 11 503 |
| marketing_excessif | 5/5 | -114 | -981 – 239 | -1 995 | 22 | 150 (5/5) | 7 791 | 1 052 | 7 | 25 | 4 353 |
| exces_puis_correction | 0/5 | 90 402 | 62 420 – 91 580 | -105 | 22 | 150 (5/5) | 24 149 | 2 976 | 10 | 44 | 9 406 |
| recrutement_precoce | 5/5 | -295 | -1 333 – 245 | -1 994 | 10 | 60 (5/5) | 1 148 | 248 | 7 | 26 | 1 386 |
| trop_de_projets | 5/5 | -264 | -734 – -168 | -1 988 | 26 | 120 (5/5) | 2 124 | 1 083 | 7 | 25 | 3 150 |

## Façons de jouer

- **un_projet** : Se concentrer sur une application, l'améliorer d'après les avis, embaucher quand elle rapporte.
- **plusieurs_petits** : Plusieurs petits projets l'un après l'autre (application, jeu, application pour PME).
- **ambitieux** : Un jeu ambitieux pour passionnés : beaucoup de composants, une embauche dès le départ, promotion avant la sortie.
- **jeu_simple** : Un petit jeu grand public, puis des mises à jour régulières.
- **delegation** : Comme plusieurs_petits, mais le fondateur arrête de taper dès la première embauche : l'équipe développe, lance et prépare les petites mises à jour.
- **hebergement_maison** : Comme plusieurs_petits, puis une salle serveur dès 40 000 CHF : les projets passent sur ses serveurs, le stockage et l'hébergement libres sont vendus.
- **materiel** : Comme plusieurs_petits, puis le matériel dès 50 000 CHF (une carte à 65 CHF met plusieurs mois à couvrir l'atelier) : un atelier, la recherche « Conception de cartes », une carte pour les makers fabriquée sur une ligne d'assemblage, vendue en ligne.
- **ia** : Comme plusieurs_petits, puis l'IA dès 30 000 CHF : un petit modèle d'analyse (calcul loué), évalué, publié, un produit d'IA pour les analystes, une seconde version sur données achetées.
- **studio_xr** : Comme plusieurs_petits, un studio au siège dès 20 000 CHF, puis le matériel et la XR : zone de test au siège et une application XR pour joueurs en quatrième projet ; la compétence qui manque au studio ou à la zone XR (design, tests) s’acquiert par une formation.
- **quantique** : Comme plusieurs_petits, tout l'arbre vers le quantique (matériel, usine, salle propre, laboratoire avancé, offres de calcul), un laboratoire dès 230 000 CHF (de quoi l’équiper d’un bloc et payer six mois de loyer), équipé au fil de la trésorerie.
- **un_projet_pme** : Comme un_projet, mais une application pour PME : isole l'effet du segment (prix 39 CHF, marché de 30 000).
- **materiel_industrie** : Comme materiel, mais la carte est vendue à l'industrie (220 CHF, marché de 25 000) plutôt qu'aux makers.
- **materiel_pc** : Comme materiel, mais des PC pour joueurs : recherche « Validation de PC » (banc de test), PC assemblés sur la ligne.
- **materiel_serveur** : Comme materiel_pc, puis « Serveurs et NAS » : des serveurs pour l'auto-hébergement.
- **materiel_gpu** : Comme materiel, mais dès 70 000 CHF (la chaîne coûte plus) : l'industrialisation, « Assemblage de cartes en série », « Cartes graphiques », des GPU pour joueurs.
- **ia_salle_serveur** : Comme ia, avec une salle serveur dès 40 000 CHF et un serveur GPU : l'IA entraîne et sert sur ses propres GPU.
- **marketing_excessif** : Erreur : 6 000 CHF de marketing par mois dès la sortie d'une petite application.
- **exces_puis_correction** : Même erreur de marketing, mais corrigée dès que l'argent devient tendu (budget coupé, prêt si possible).
- **recrutement_precoce** : Erreur : trois embauches dès le départ, avant le moindre revenu.
- **trop_de_projets** : Erreur : cinq projets ouverts d'un coup, chacun avec 1 500 CHF de promotion par mois avant même sa sortie.

## Jalons du lot 6

Jour où l'étape est atteinte pour la première fois (médiane des graines qui l'atteignent). Mini-jeux d'évaluation et de mise en route joués à 75 %.

| Stratégie | Jalon | Atteint | Jour (méd.) |
|---|---|---|---|
| ia | ia_publiee | 5/5 | 312 |
| ia | produit_ia | 5/5 | 312 |
| studio_xr | studio_pose | 5/5 | 248 |
| studio_xr | studio_complet | 5/5 | 248 |
| studio_xr | zone_xr_posee | 5/5 | 284 |
| studio_xr | zone_xr_complete | 5/5 | 288 |
| ia_salle_serveur | ia_publiee | 5/5 | 315 |
| ia_salle_serveur | produit_ia | 5/5 | 315 |

## Horizon long : 36 mois

| Stratégie | Faillites | Argent final (médiane) | Argent final (min – max) | Écart à plusieurs_petits (médiane) | Revenus / mois (méd.) | Jalons atteints |
|---|---|---|---|---|---|---|
| plusieurs_petits | 0/5 | 398 497 | 340 861 – 431 717 | 0 | 24 739 | — |
| hebergement_maison | 0/5 | 308 825 | 189 293 – 407 821 | -89 672 | 30 027 | — |
| materiel | 1/5 | 38 443 | 3 453 – 80 610 | -360 054 | 27 771 | — |
| materiel_industrie | 0/5 | 326 255 | 113 067 – 429 258 | -72 242 | 53 122 | — |
| materiel_pc | 0/5 | 371 105 | 277 237 – 486 631 | -27 392 | 68 304 | — |
| materiel_serveur | 0/5 | 233 711 | 169 155 – 285 941 | -164 786 | 48 806 | — |
| materiel_gpu | 0/5 | 318 661 | 256 439 – 408 265 | -79 836 | 49 546 | — |
| ia | 0/5 | 302 620 | 243 867 – 362 018 | -95 877 | 24 604 | ia_publiee 5/5, produit_ia 5/5 |
| ia_salle_serveur | 0/5 | 150 240 | 119 655 – 227 733 | -248 257 | 27 064 | ia_publiee 5/5, produit_ia 5/5 |
| studio_xr | 0/5 | 174 593 | 133 679 – 200 574 | -223 904 | 17 904 | studio_pose 5/5, studio_complet 5/5, zone_xr_posee 5/5, zone_xr_complete 5/5 |
| quantique | 0/5 | 44 141 | 25 227 – 67 847 | -354 356 | 24 976 | labo_avance 5/5, calibration_cryo 4/5 |

### Quantique sur 60 mois, graine par graine

| Graine | Issue | Argent par trimestre | Nœuds | Jalons |
|---|---|---|---|---|
| 11 | en activité | 12 716 → 17 337 → 22 914 → 28 191 → 29 236 → 47 188 → 94 182 → 146 426 → 209 527 → 64 710 → 64 860 → 67 847 → 61 547 → 58 999 → 62 185 → 69 409 → 79 482 → 35 382 → 100 652 → 171 505 | 11 | labo_avance (j840), calibration_cryo (j953), quantique_en_service (j1582) |
| 23 | en activité | 12 799 → 15 386 → 20 966 → 31 452 → 32 637 → 42 417 → 89 248 → 135 100 → 178 663 → 211 930 → 57 972 → 63 232 → 67 186 → 134 584 → 206 171 → 283 412 → 366 792 → 459 537 → 547 501 → 640 676 | 11 | labo_avance (j952), calibration_cryo (j1025), quantique_en_service (j1093) |
| 37 | en activité | 12 746 → 15 528 → 19 677 → 28 422 → 25 930 → 28 463 → 59 520 → 90 879 → 133 372 → 179 890 → 211 442 → 44 141 → 32 760 → 36 547 → 42 813 → 50 561 → 58 809 → 66 415 → 46 696 → 107 940 | 11 | labo_avance (j1054), calibration_cryo (j1109), quantique_en_service (j1642) |
| 41 | en activité | 12 761 → 15 652 → 20 560 → 28 833 → 27 336 → 33 589 → 71 603 → 109 257 → 157 126 → 209 803 → 51 459 → 39 055 → 40 043 → 45 233 → 53 175 → 62 783 → 11 079 → 69 821 → 128 522 → 192 827 | 11 | labo_avance (j933), calibration_cryo (j1044), quantique_en_service (j1522) |
| 59 | en activité | 12 753 → 17 047 → 23 178 → 29 805 → 31 108 → 44 314 → 88 878 → 137 263 → 192 998 → 53 478 → 34 875 → 25 227 → 21 883 → 24 644 → 32 227 → 43 315 → 56 666 → 70 811 → 55 875 → 124 931 | 11 | labo_avance (j896), calibration_cryo (j969), quantique_en_service (j1641) |

## Détail

| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |
|---|---|---|---|---|---|---|---|
| un_projet | 11 | en activité | 540 | 12 640 → 15 728 → 28 726 → 31 274 → 31 789 → 49 684 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 23 | en activité | 540 | 12 724 → 14 561 → 24 463 → 29 658 → 34 284 → 38 336 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 37 | en activité | 540 | 12 668 → 14 465 → 25 631 → 29 498 → 30 068 → 35 978 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 41 | en activité | 540 | 12 686 → 15 706 → 28 635 → 29 927 → 37 787 → 63 327 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 59 | en activité | 540 | 12 677 → 15 450 → 29 625 → 34 247 → 35 709 → 55 684 | 2 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 11 | en activité | 540 | 12 716 → 17 754 → 25 119 → 28 044 → 34 734 → 55 830 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 23 | en activité | 540 | 12 799 → 15 290 → 19 239 → 29 948 → 36 290 → 51 992 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 37 | en activité | 540 | 12 746 → 15 732 → 20 532 → 30 293 → 27 092 → 37 226 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 41 | en activité | 540 | 12 761 → 15 264 → 19 164 → 31 227 → 29 596 → 40 983 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 59 | en activité | 540 | 12 753 → 16 760 → 22 944 → 30 958 → 40 005 → 66 295 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 11 | en activité | 540 | 22 884 → 48 962 → 91 441 → 133 644 → 168 282 → 189 262 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 23 | en activité | 540 | 23 219 → 50 094 → 93 964 → 135 546 → 166 169 → 180 161 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 37 | en activité | 540 | 18 213 → 33 246 → 64 104 → 94 988 → 120 352 → 138 573 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 41 | en activité | 540 | 22 048 → 46 452 → 87 270 → 129 685 → 164 967 → 190 309 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 59 | en activité | 540 | 19 063 → 40 928 → 83 140 → 129 843 → 171 674 → 203 743 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 11 | en activité | 540 | 17 130 → 21 449 → 28 481 → 37 213 → 27 055 → 19 121 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 23 | en activité | 540 | 15 494 → 14 988 → 15 719 → 17 003 → 25 268 → 34 408 | 0 | Ta chambre, Prilly | 8 |
| jeu_simple | 37 | en activité | 540 | 16 203 → 17 597 → 19 839 → 22 592 → 31 186 → 20 077 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 41 | en activité | 540 | 15 672 → 16 008 → 16 615 → 16 952 → 23 028 → 30 270 | 0 | Ta chambre, Prilly | 8 |
| jeu_simple | 59 | en activité | 540 | 16 997 → 19 581 → 23 309 → 27 721 → 39 019 → 24 871 | 1 | Ta chambre, Prilly | 8 |
| delegation | 11 | en activité | 540 | 12 716 → 18 094 → 26 151 → 28 269 → 28 316 → 25 584 | 1 | Ta chambre, Prilly | 8 |
| delegation | 23 | en activité | 540 | 12 799 → 15 200 → 18 828 → 29 109 → 27 165 → 19 440 | 1 | Ta chambre, Prilly | 8 |
| delegation | 37 | en activité | 540 | 12 746 → 15 598 → 19 330 → 27 778 → 25 781 → 19 578 | 1 | Ta chambre, Prilly | 8 |
| delegation | 41 | en activité | 540 | 12 761 → 15 084 → 18 552 → 27 947 → 22 054 → 11 282 | 1 | Ta chambre, Prilly | 7 |
| delegation | 59 | en activité | 540 | 12 753 → 16 863 → 22 716 → 32 372 → 31 719 → 29 351 | 1 | Ta chambre, Prilly | 8 |
| hebergement_maison | 11 | en activité | 540 | 12 716 → 17 466 → 23 159 → 27 717 → 20 346 → 40 721 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 23 | en activité | 540 | 12 799 → 15 367 → 20 798 → 26 222 → 13 600 → 24 723 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 37 | en activité | 540 | 12 746 → 15 646 → 19 950 → 29 348 → 11 486 → 20 045 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 41 | en activité | 540 | 12 761 → 15 654 → 20 426 → 30 760 → 32 987 → 20 214 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 59 | en activité | 540 | 12 753 → 16 966 → 22 265 → 34 329 → 9 788 → 19 539 | 1 | Ta chambre, Prilly | 9 |
| materiel | 11 | en activité | 540 | 12 716 → 17 466 → 23 159 → 27 717 → 29 688 → 11 698 | 3 | Ta chambre, Prilly | 9 |
| materiel | 23 | en activité | 540 | 12 799 → 15 367 → 20 798 → 26 222 → 28 620 → 16 775 | 2 | Ta chambre, Prilly | 9 |
| materiel | 37 | en activité | 540 | 12 746 → 15 646 → 19 950 → 29 348 → 29 743 → 30 924 | 2 | Ta chambre, Prilly | 9 |
| materiel | 41 | en activité | 540 | 12 761 → 15 654 → 20 426 → 30 760 → 32 987 → 31 600 | 2 | Ta chambre, Prilly | 9 |
| materiel | 59 | en activité | 540 | 12 753 → 16 966 → 22 265 → 34 329 → 33 346 → 3 966 | 3 | Ta chambre, Prilly | 9 |
| ia | 11 | en activité | 540 | 12 761 → 18 702 → 27 494 → 27 835 → 37 367 → 62 516 | 1 | Ta chambre, Prilly | 8 |
| ia | 23 | en activité | 540 | 12 851 → 15 966 → 23 027 → 30 307 → 37 710 → 57 922 | 1 | Ta chambre, Prilly | 8 |
| ia | 37 | en activité | 540 | 12 797 → 16 980 → 23 797 → 26 301 → 27 152 → 41 234 | 1 | Ta chambre, Prilly | 8 |
| ia | 41 | en activité | 540 | 12 807 → 16 751 → 23 189 → 28 314 → 26 808 → 37 554 | 1 | Ta chambre, Prilly | 8 |
| ia | 59 | en activité | 540 | 12 800 → 18 184 → 27 286 → 29 186 → 48 758 → 80 986 | 1 | Ta chambre, Prilly | 8 |
| studio_xr | 11 | en activité | 540 | 12 716 → 17 467 → 22 096 → 30 073 → 28 903 → 38 335 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 23 | en activité | 540 | 12 799 → 15 367 → 21 973 → 31 227 → 37 483 → 49 018 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 37 | en activité | 540 | 12 746 → 15 646 → 21 176 → 32 151 → 28 547 → 32 046 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 41 | en activité | 540 | 12 761 → 15 654 → 21 520 → 28 103 → 27 222 → 31 131 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 59 | en activité | 540 | 12 753 → 16 966 → 21 439 → 26 690 → 23 422 → 34 471 | 1 | Ta chambre, Prilly | 9 |
| quantique | 11 | en activité | 540 | 12 716 → 17 337 → 22 914 → 28 191 → 29 236 → 47 188 | 2 | Ta chambre, Prilly | 8 |
| quantique | 23 | en activité | 540 | 12 799 → 15 386 → 20 966 → 31 452 → 32 637 → 42 417 | 2 | Ta chambre, Prilly | 8 |
| quantique | 37 | en activité | 540 | 12 746 → 15 528 → 19 677 → 28 422 → 25 930 → 28 463 | 2 | Ta chambre, Prilly | 8 |
| quantique | 41 | en activité | 540 | 12 761 → 15 652 → 20 560 → 28 833 → 27 336 → 33 589 | 2 | Ta chambre, Prilly | 8 |
| quantique | 59 | en activité | 540 | 12 753 → 17 047 → 23 178 → 29 805 → 31 108 → 44 314 | 2 | Ta chambre, Prilly | 8 |
| un_projet_pme | 11 | en activité | 540 | 13 329 → 20 426 → 30 272 → 34 339 → 57 876 → 100 420 | 2 | Ta chambre, Prilly | 8 |
| un_projet_pme | 23 | en activité | 540 | 13 548 → 22 026 → 34 502 → 38 443 → 66 741 → 118 115 | 2 | Ta chambre, Prilly | 8 |
| un_projet_pme | 37 | en activité | 540 | 13 353 → 19 752 → 26 905 → 29 325 → 32 707 → 49 055 | 2 | Ta chambre, Prilly | 7 |
| un_projet_pme | 41 | en activité | 540 | 12 933 → 18 063 → 33 823 → 34 029 → 46 439 → 77 040 | 2 | Ta chambre, Prilly | 8 |
| un_projet_pme | 59 | en activité | 540 | 13 174 → 20 368 → 35 375 → 40 344 → 64 405 → 107 146 | 2 | Ta chambre, Prilly | 8 |
| materiel_industrie | 11 | en activité | 540 | 12 716 → 17 466 → 23 159 → 4 530 → 26 253 → 52 472 | 2 | Ta chambre, Prilly | 9 |
| materiel_industrie | 23 | en activité | 540 | 12 799 → 15 367 → 20 798 → 23 534 → 11 495 → 33 608 | 2 | Ta chambre, Prilly | 9 |
| materiel_industrie | 37 | en activité | 540 | 12 746 → 15 646 → 19 950 → 29 348 → 26 931 → 58 502 | 2 | Ta chambre, Prilly | 9 |
| materiel_industrie | 41 | en activité | 540 | 12 761 → 15 654 → 20 426 → 30 760 → 13 504 → 10 567 | 2 | Ta chambre, Prilly | 9 |
| materiel_industrie | 59 | en activité | 540 | 12 753 → 16 966 → 22 265 → 34 329 → 3 463 → 23 867 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 11 | en activité | 540 | 12 716 → 17 466 → 23 159 → 21 230 → 26 338 → 101 897 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 23 | en activité | 540 | 12 799 → 15 367 → 20 798 → 20 734 → 24 648 → 93 155 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 37 | en activité | 540 | 12 746 → 15 646 → 19 950 → 29 348 → 757 → 74 647 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 41 | en activité | 540 | 12 761 → 15 654 → 20 426 → 30 760 → 1 781 → 51 907 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 59 | en activité | 540 | 12 753 → 16 966 → 22 265 → 34 329 → 20 410 → 59 746 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 11 | en activité | 540 | 12 716 → 17 466 → 23 159 → 21 230 → 2 301 → 40 150 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 23 | en activité | 540 | 12 799 → 15 367 → 20 798 → 20 734 → 1 762 → 38 858 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 37 | en activité | 540 | 12 746 → 15 646 → 19 950 → 29 348 → 773 → 34 863 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 41 | en activité | 540 | 12 761 → 15 654 → 20 426 → 30 760 → 4 033 → 18 274 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 59 | en activité | 540 | 12 753 → 16 966 → 22 265 → 34 329 → 2 327 → 19 544 | 2 | Ta chambre, Prilly | 9 |
| materiel_gpu | 11 | en activité | 540 | 12 716 → 17 337 → 23 004 → 30 294 → 30 645 → 37 332 | 2 | Ta chambre, Prilly | 10 |
| materiel_gpu | 23 | en activité | 540 | 12 799 → 15 386 → 20 742 → 30 391 → 33 193 → 55 918 | 2 | Ta chambre, Prilly | 10 |
| materiel_gpu | 37 | en activité | 540 | 12 746 → 15 528 → 19 665 → 29 248 → 25 520 → 33 210 | 2 | Ta chambre, Prilly | 10 |
| materiel_gpu | 41 | en activité | 540 | 12 761 → 15 652 → 20 591 → 29 388 → 26 586 → 38 596 | 2 | Ta chambre, Prilly | 10 |
| materiel_gpu | 59 | en activité | 540 | 12 753 → 17 047 → 23 119 → 29 024 → 30 796 → 51 514 | 2 | Ta chambre, Prilly | 10 |
| ia_salle_serveur | 11 | en activité | 540 | 12 761 → 18 702 → 27 107 → 24 872 → 29 927 → 21 948 | 1 | Ta chambre, Prilly | 11 |
| ia_salle_serveur | 23 | en activité | 540 | 12 851 → 15 966 → 22 716 → 27 459 → 16 358 → 27 374 | 1 | Ta chambre, Prilly | 11 |
| ia_salle_serveur | 37 | en activité | 540 | 12 797 → 16 980 → 23 604 → 27 379 → 32 790 → 11 723 | 1 | Ta chambre, Prilly | 11 |
| ia_salle_serveur | 41 | en activité | 540 | 12 807 → 16 751 → 23 244 → 25 378 → 29 116 → 11 002 | 1 | Ta chambre, Prilly | 11 |
| ia_salle_serveur | 59 | en activité | 540 | 12 800 → 18 184 → 26 945 → 31 492 → 4 960 → 19 996 | 1 | Ta chambre, Prilly | 11 |
| marketing_excessif | 11 | faillite | 207 | 6 501 → 1 209 | 0 | Ta chambre, Prilly | 6 |
| marketing_excessif | 23 | faillite | 176 | 6 625 → -838 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 37 | faillite | 176 | 6 538 → -981 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 41 | faillite | 207 | 6 568 → 1 267 | 0 | Ta chambre, Prilly | 6 |
| marketing_excessif | 59 | faillite | 207 | 6 554 → 1 381 | 0 | Ta chambre, Prilly | 6 |
| exces_puis_correction | 11 | en activité | 540 | 6 499 → 4 880 → 20 262 → 36 127 → 56 025 → 91 211 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 23 | en activité | 540 | 6 623 → 3 864 → 16 087 → 29 087 → 36 243 → 62 420 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 37 | en activité | 540 | 6 536 → 3 699 → 16 305 → 32 623 → 41 957 → 69 147 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 41 | en activité | 540 | 6 565 → 4 959 → 20 069 → 32 613 → 51 621 → 90 402 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 59 | en activité | 540 | 6 552 → 5 036 → 21 539 → 35 412 → 54 917 → 91 580 | 1 | Ta chambre, Prilly | 8 |
| recrutement_precoce | 11 | faillite | 72 | -295 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 23 | faillite | 129 | -1 305 | 0 | Ta chambre, Prilly | 6 |
| recrutement_precoce | 37 | faillite | 72 | -1 159 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 41 | faillite | 72 | -148 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 59 | faillite | 72 | -1 333 | 0 | Petit bureau, Flon | 6 |
| trop_de_projets | 11 | faillite | 146 | -1 005 | 0 | Ta chambre, Prilly | 6 |
| trop_de_projets | 23 | faillite | 146 | -1 085 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 37 | faillite | 146 | -1 068 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 41 | faillite | 146 | -1 045 | 0 | Ta chambre, Prilly | 6 |
| trop_de_projets | 59 | faillite | 146 | -1 031 | 0 | Ta chambre, Prilly | 6 |
