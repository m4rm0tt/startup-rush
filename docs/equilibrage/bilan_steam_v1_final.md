# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 20 façons de jouer × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu, fondateur polyvalent, réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. Aucune prestation client n'existe dans cette version.

## Résumé

| Stratégie | Faillites | Argent final (médiane) | Argent final (min – max) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) | Mots tapés (méd.) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| un_projet | 0/5 | 87 760 | 57 707 – 111 932 | 11 512 | 8 | 150 (5/5) | 34 414 | 4 138 | 13 | 59 | 6 943 |
| plusieurs_petits | 0/5 | 71 030 | 63 720 – 89 538 | 12 224 | 8 | 120 (5/5) | 25 571 | 1 412 | 21 | 85 | 11 429 |
| ambitieux | 0/5 | 194 736 | 124 682 – 240 879 | 13 426 | 11 | 30 (5/5) | 16 961 | 10 045 | 14 | 40 | 3 338 |
| jeu_simple | 0/5 | 25 840 | 8 462 – 67 395 | 8 226 | 9 | 90 (5/5) | 10 707 | 16 762 | 16 | 44 | 4 782 |
| delegation | 0/5 | 20 914 | 18 096 – 28 195 | 12 223 | 8 | 120 (5/5) | 9 913 | 785 | 19 | 65 | 6 929 |
| hebergement_maison | 0/5 | 22 608 | 13 567 – 37 391 | 9 840 | 8 | 120 (5/5) | 24 767 | 1 305 | 24 | 115 | 11 429 |
| materiel | 0/5 | 19 829 | 14 589 – 32 230 | 7 945 | 8 | 120 (5/5) | 23 318 | 1 316 | 22 | 100 | 11 429 |
| ia | 0/5 | 62 358 | 42 098 – 68 718 | 12 304 | 8 | 120 (5/5) | 21 996 | 1 442 | 27 | 113 | 11 421 |
| studio_xr | 0/5 | 60 229 | 36 218 – 76 310 | 12 222 | 8 | 120 (5/5) | 21 733 | 1 903 | 24 | 113 | 11 399 |
| quantique | 0/5 | 37 824 | 34 817 – 70 330 | 12 222 | 8 | 120 (5/5) | 24 385 | 1 317 | 21 | 91 | 11 429 |
| un_projet_pme | 0/5 | 120 127 | 75 012 – 157 229 | 11 914 | 8 | 120 (5/5) | 39 954 | 1 472 | 13 | 58 | 6 494 |
| materiel_industrie | 0/5 | 34 155 | 26 894 – 50 906 | 3 597 | 8 | 120 (5/5) | 45 097 | 1 284 | 25 | 148 | 11 429 |
| materiel_pc | 0/5 | 78 764 | 57 446 – 133 011 | -1 724 | 8 | 120 (5/5) | 113 917 | 1 179 | 25 | 133 | 11 429 |
| materiel_serveur | 0/5 | 30 794 | 13 254 – 61 575 | -1 975 | 8 | 120 (5/5) | 90 508 | 916 | 26 | 141 | 11 429 |
| materiel_gpu | 0/5 | 39 453 | 34 884 – 57 017 | 12 222 | 8 | 120 (5/5) | 23 309 | 1 313 | 21 | 89 | 11 429 |
| ia_salle_serveur | 0/5 | 21 233 | 10 366 – 36 805 | -1 953 | 8 | 120 (5/5) | 23 588 | 1 356 | 27 | 137 | 11 421 |
| marketing_excessif | 4/5 | 58 | -1 812 – 100 145 | -1 993 | 8 | 150 (5/5) | 7 357 | 1 016 | 8 | 35 | 3 629 |
| exces_puis_correction | 0/5 | 122 244 | 96 466 – 131 187 | 2 805 | 8 | 120 (5/5) | 27 673 | 3 304 | 13 | 60 | 7 327 |
| recrutement_precoce | 5/5 | -768 | -1 799 – -198 | -1 990 | 5 | 90 (2/5) | 748 | 194 | 6 | 29 | 1 299 |
| trop_de_projets | 5/5 | -624 | -967 – -345 | -1 996 | 22 | 120 (5/5) | 2 243 | 989 | 7 | 30 | 3 150 |

## Dispersion sur 20 graines (18 mois)

Argent final : 10 % des parties finissent sous P10, 10 % au-dessus de P90. Une faillite compte avec son solde au jour de la liquidation.

| Stratégie | Faillites | P10 | Médiane | P90 | Revenus / mois (méd.) |
|---|---|---|---|---|---|
| un_projet | 0/20 | 57 707 | 84 103 | 107 690 | 34 414 |
| plusieurs_petits | 0/20 | 63 720 | 78 491 | 95 951 | 27 079 |
| jeu_simple | 0/20 | 11 241 | 27 613 | 53 297 | 10 576 |
| delegation | 0/20 | 16 116 | 18 722 | 29 755 | 10 216 |
| marketing_excessif | 17/20 | -1 450 | -409 | 100 145 | 7 207 |
| exces_puis_correction | 0/20 | 97 752 | 121 241 | 131 187 | 27 773 |
| materiel | 0/20 | 6 437 | 18 085 | 31 368 | 26 682 |
| materiel_pc | 0/20 | 60 831 | 100 180 | 130 625 | 113 543 |
| ia | 0/20 | 51 054 | 61 438 | 69 196 | 22 746 |
| studio_xr | 0/20 | 42 520 | 60 229 | 77 202 | 23 635 |
| hebergement_maison | 0/20 | 19 845 | 22 608 | 45 943 | 24 767 |

## Façons de jouer

- **un_projet** : Se concentrer sur une application, l'améliorer d'après les avis, embaucher quand elle rapporte.
- **plusieurs_petits** : Plusieurs petits projets l'un après l'autre (application, jeu, application pour PME).
- **ambitieux** : Un jeu ambitieux pour passionnés : beaucoup de composants, une embauche dès le départ, promotion avant la sortie.
- **jeu_simple** : Un petit jeu grand public, puis des mises à jour régulières.
- **delegation** : Comme plusieurs_petits, mais le fondateur arrête de taper dès la première embauche : l'équipe développe, lance et prépare les petites mises à jour.
- **hebergement_maison** : Comme plusieurs_petits, puis une salle serveur dès 40 000 CHF : les projets passent sur ses serveurs, le stockage et l'hébergement libres sont vendus.
- **materiel** : Comme plusieurs_petits, puis le matériel dès 50 000 CHF (une carte à 95 CHF met plusieurs mois à couvrir l'atelier) : un atelier, la recherche « Conception de cartes », une carte pour les makers fabriquée sur une ligne d'assemblage, vendue en ligne.
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
| ia | ia_publiee | 5/5 | 315 |
| ia | produit_ia | 5/5 | 315 |
| studio_xr | studio_pose | 5/5 | 245 |
| studio_xr | studio_complet | 5/5 | 245 |
| studio_xr | zone_xr_posee | 5/5 | 289 |
| studio_xr | zone_xr_complete | 5/5 | 291 |
| ia_salle_serveur | ia_publiee | 5/5 | 319 |
| ia_salle_serveur | produit_ia | 5/5 | 319 |

## Horizon long : 36 mois

| Stratégie | Faillites | Argent final (médiane) | Argent final (min – max) | Écart à plusieurs_petits (médiane) | Revenus / mois (méd.) | Jalons atteints |
|---|---|---|---|---|---|---|
| plusieurs_petits | 0/5 | 449 411 | 391 934 – 456 874 | 0 | 38 980 | — |
| hebergement_maison | 0/5 | 313 576 | 109 678 – 440 631 | -135 835 | 38 882 | — |
| materiel | 0/5 | 107 034 | 72 210 – 133 814 | -342 377 | 36 591 | — |
| materiel_industrie | 0/5 | 303 504 | 269 534 – 437 829 | -145 907 | 46 220 | — |
| materiel_pc | 0/5 | 458 782 | 395 942 – 593 007 | +9 371 | 80 208 | — |
| materiel_serveur | 0/5 | 356 142 | 321 086 – 421 876 | -93 269 | 57 169 | — |
| materiel_gpu | 0/5 | 342 951 | 257 913 – 375 560 | -106 460 | 48 672 | — |
| ia | 0/5 | 355 631 | 296 298 – 401 345 | -93 780 | 27 987 | ia_publiee 5/5, produit_ia 5/5 |
| ia_salle_serveur | 0/5 | 212 906 | 160 062 – 291 704 | -236 505 | 29 965 | ia_publiee 5/5, produit_ia 5/5 |
| studio_xr | 0/5 | 228 090 | 149 875 – 301 224 | -221 321 | 19 218 | studio_pose 5/5, studio_complet 5/5, zone_xr_posee 5/5, zone_xr_complete 5/5 |
| quantique | 0/5 | 54 953 | 19 171 – 144 734 | -394 458 | 31 317 | labo_avance 5/5, calibration_cryo 3/5, quantique_en_service 2/5 |

### Quantique sur 60 mois, graine par graine

| Graine | Issue | Argent par trimestre | Nœuds | Jalons |
|---|---|---|---|---|
| 11 | en activité | 12 614 → 16 789 → 23 380 → 32 251 → 37 330 → 70 330 → 126 616 → 163 660 → 212 024 → 57 892 → 68 068 → 144 734 → 227 555 → 311 128 → 433 169 → 559 417 → 686 578 → 809 739 → 932 616 → 1 060 693 | 11 | labo_avance (j840), calibration_cryo (j900), quantique_en_service (j913) |
| 23 | en activité | 12 742 → 15 333 → 19 630 → 29 392 → 34 210 → 64 564 → 126 423 → 168 563 → 199 420 → 55 491 → 53 130 → 71 443 → 69 496 → 147 995 → 228 817 → 317 620 → 420 889 → 539 299 → 663 381 → 796 274 | 11 | labo_avance (j893), calibration_cryo (j962), quantique_en_service (j1095) |
| 37 | en activité | 12 873 → 15 930 → 20 382 → 31 516 → 26 935 → 34 817 → 72 701 → 105 211 → 129 579 → 157 983 → 194 278 → 54 953 → 52 890 → 69 206 → 54 562 → 130 529 → 203 943 → 276 466 → 365 099 → 477 178 | 11 | labo_avance (j1074), calibration_cryo (j1149), quantique_en_service (j1278) |
| 41 | en activité | 12 647 → 14 558 → 17 009 → 28 772 → 33 417 → 37 824 → 73 540 → 108 094 → 136 042 → 166 001 → 203 571 → 53 407 → 54 351 → 18 287 → 88 248 → 172 003 → 247 365 → 329 387 → 426 146 → 546 520 | 11 | labo_avance (j1050), calibration_cryo (j1130), quantique_en_service (j1257) |
| 59 | en activité | 12 797 → 16 788 → 22 923 → 29 411 → 28 698 → 37 234 → 84 123 → 115 429 → 147 116 → 189 410 → 57 048 → 19 171 → 91 520 → 164 221 → 236 353 → 325 668 → 429 950 → 547 819 → 664 928 → 784 248 | 11 | labo_avance (j983), calibration_cryo (j1064), quantique_en_service (j1074) |

## Détail

| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |
|---|---|---|---|---|---|---|---|
| un_projet | 11 | en activité | 540 | 12 473 → 16 635 → 31 625 → 36 327 → 40 647 → 65 378 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 23 | en activité | 540 | 12 604 → 17 219 → 31 507 → 34 801 → 54 571 → 99 837 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 37 | en activité | 540 | 12 740 → 19 332 → 34 742 → 36 592 → 62 445 → 111 932 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 41 | en activité | 540 | 12 509 → 16 162 → 31 089 → 34 022 → 37 429 → 57 707 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 59 | en activité | 540 | 12 661 → 17 418 → 29 599 → 32 427 → 49 563 → 87 760 | 2 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 11 | en activité | 540 | 12 614 → 16 445 → 22 336 → 31 629 → 37 842 → 68 405 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 23 | en activité | 540 | 12 742 → 15 458 → 20 348 → 32 596 → 39 870 → 71 030 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 37 | en activité | 540 | 12 873 → 16 352 → 22 072 → 28 959 → 40 849 → 77 855 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 41 | en activité | 540 | 12 647 → 14 699 → 18 305 → 28 734 → 34 720 → 63 720 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 59 | en activité | 540 | 12 797 → 16 584 → 22 270 → 30 402 → 44 912 → 89 538 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 11 | en activité | 540 | 20 407 → 53 095 → 114 083 → 170 320 → 212 239 → 240 879 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 23 | en activité | 540 | 17 800 → 44 057 → 91 107 → 129 005 → 146 954 → 153 180 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 37 | en activité | 540 | 17 803 → 34 539 → 67 819 → 96 060 → 114 356 → 124 682 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 41 | en activité | 540 | 20 529 → 50 409 → 106 962 → 159 246 → 196 859 → 222 701 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 59 | en activité | 540 | 18 030 → 42 999 → 93 331 → 139 734 → 172 753 → 194 736 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 11 | en activité | 540 | 18 420 → 29 162 → 36 077 → 49 876 → 62 985 → 67 395 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 23 | en activité | 540 | 16 524 → 22 456 → 27 693 → 25 723 → 20 936 → 13 430 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 37 | en activité | 540 | 17 350 → 26 101 → 30 444 → 31 118 → 30 442 → 25 840 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 41 | en activité | 540 | 17 075 → 23 713 → 29 355 → 24 743 → 17 929 → 8 462 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 59 | en activité | 540 | 18 294 → 28 828 → 33 986 → 40 143 → 44 055 → 41 027 | 1 | Ta chambre, Prilly | 8 |
| delegation | 11 | en activité | 540 | 12 614 → 17 123 → 24 043 → 26 811 → 24 819 → 20 914 | 1 | Ta chambre, Prilly | 8 |
| delegation | 23 | en activité | 540 | 12 742 → 15 382 → 19 729 → 29 627 → 24 311 → 18 627 | 1 | Ta chambre, Prilly | 8 |
| delegation | 37 | en activité | 540 | 12 873 → 16 322 → 21 058 → 28 381 → 23 692 → 18 096 | 1 | Ta chambre, Prilly | 8 |
| delegation | 41 | en activité | 540 | 12 647 → 14 176 → 16 267 → 27 353 → 27 130 → 22 178 | 1 | Ta chambre, Prilly | 8 |
| delegation | 59 | en activité | 540 | 12 797 → 16 824 → 22 445 → 29 806 → 29 959 → 28 195 | 1 | Ta chambre, Prilly | 8 |
| hebergement_maison | 11 | en activité | 540 | 12 614 → 16 840 → 24 274 → 29 672 → 20 004 → 33 853 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 23 | en activité | 540 | 12 742 → 15 345 → 20 745 → 29 054 → 13 962 → 22 608 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 37 | en activité | 540 | 12 873 → 15 883 → 20 197 → 31 462 → 25 888 → 13 567 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 41 | en activité | 540 | 12 647 → 14 767 → 18 071 → 29 509 → 29 185 → 19 845 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 59 | en activité | 540 | 12 797 → 16 785 → 22 575 → 31 197 → 19 828 → 37 391 | 1 | Ta chambre, Prilly | 9 |
| materiel | 11 | en activité | 540 | 12 614 → 16 840 → 24 274 → 29 672 → 32 716 → 16 103 | 3 | Ta chambre, Prilly | 9 |
| materiel | 23 | en activité | 540 | 12 742 → 15 345 → 20 745 → 29 054 → 33 941 → 14 589 | 2 | Ta chambre, Prilly | 9 |
| materiel | 37 | en activité | 540 | 12 873 → 15 883 → 20 197 → 31 462 → 25 888 → 29 826 | 2 | Ta chambre, Prilly | 9 |
| materiel | 41 | en activité | 540 | 12 647 → 14 767 → 18 071 → 29 509 → 29 185 → 32 230 | 2 | Ta chambre, Prilly | 9 |
| materiel | 59 | en activité | 540 | 12 797 → 16 785 → 22 575 → 31 197 → 12 110 → 19 829 | 3 | Ta chambre, Prilly | 9 |
| ia | 11 | en activité | 540 | 12 623 → 17 773 → 27 110 → 28 979 → 38 030 → 62 783 | 1 | Ta chambre, Prilly | 8 |
| ia | 23 | en activité | 540 | 12 754 → 16 141 → 22 894 → 27 709 → 32 487 → 54 642 | 1 | Ta chambre, Prilly | 8 |
| ia | 37 | en activité | 540 | 12 889 → 16 736 → 22 255 → 26 414 → 34 739 → 62 358 | 1 | Ta chambre, Prilly | 8 |
| ia | 41 | en activité | 540 | 12 659 → 15 627 → 19 810 → 26 592 → 24 789 → 42 098 | 1 | Ta chambre, Prilly | 8 |
| ia | 59 | en activité | 540 | 12 810 → 17 747 → 25 121 → 30 106 → 39 843 → 68 718 | 1 | Ta chambre, Prilly | 8 |
| studio_xr | 11 | en activité | 540 | 12 614 → 16 834 → 24 076 → 30 478 → 45 132 → 76 310 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 23 | en activité | 540 | 12 742 → 15 345 → 20 621 → 28 557 → 36 743 → 60 229 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 37 | en activité | 540 | 12 873 → 15 883 → 20 234 → 31 537 → 30 362 → 48 437 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 41 | en activité | 540 | 12 647 → 14 767 → 17 822 → 31 647 → 29 408 → 36 218 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 59 | en activité | 540 | 12 797 → 16 785 → 23 379 → 31 415 → 44 424 → 73 350 | 1 | Ta chambre, Prilly | 9 |
| quantique | 11 | en activité | 540 | 12 614 → 16 789 → 23 380 → 32 251 → 37 330 → 70 330 | 2 | Ta chambre, Prilly | 11 |
| quantique | 23 | en activité | 540 | 12 742 → 15 333 → 19 630 → 29 392 → 34 210 → 64 564 | 2 | Ta chambre, Prilly | 10 |
| quantique | 37 | en activité | 540 | 12 873 → 15 930 → 20 382 → 31 516 → 26 935 → 34 817 | 2 | Ta chambre, Prilly | 10 |
| quantique | 41 | en activité | 540 | 12 647 → 14 558 → 17 009 → 28 772 → 33 417 → 37 824 | 2 | Ta chambre, Prilly | 10 |
| quantique | 59 | en activité | 540 | 12 797 → 16 788 → 22 923 → 29 411 → 28 698 → 37 234 | 2 | Ta chambre, Prilly | 11 |
| un_projet_pme | 11 | en activité | 540 | 13 179 → 23 673 → 33 569 → 48 960 → 92 299 → 157 229 | 2 | Ta chambre, Prilly | 8 |
| un_projet_pme | 23 | en activité | 540 | 13 437 → 23 930 → 36 402 → 47 139 → 83 553 → 140 915 | 2 | Ta chambre, Prilly | 8 |
| un_projet_pme | 37 | en activité | 540 | 13 189 → 23 152 → 39 461 → 42 409 → 69 839 → 115 093 | 2 | Ta chambre, Prilly | 8 |
| un_projet_pme | 41 | en activité | 540 | 12 676 → 18 265 → 32 756 → 34 284 → 45 258 → 75 012 | 2 | Ta chambre, Prilly | 8 |
| un_projet_pme | 59 | en activité | 540 | 13 001 → 21 661 → 37 628 → 43 500 → 72 040 → 120 127 | 2 | Ta chambre, Prilly | 8 |
| materiel_industrie | 11 | en activité | 540 | 12 614 → 16 840 → 24 274 → 6 468 → 6 866 → 34 155 | 2 | Ta chambre, Prilly | 9 |
| materiel_industrie | 23 | en activité | 540 | 12 742 → 15 345 → 20 745 → 5 871 → 18 637 → 33 526 | 2 | Ta chambre, Prilly | 9 |
| materiel_industrie | 37 | en activité | 540 | 12 873 → 15 883 → 20 197 → 31 462 → 18 895 → 26 894 | 2 | Ta chambre, Prilly | 9 |
| materiel_industrie | 41 | en activité | 540 | 12 647 → 14 767 → 18 071 → 29 509 → 21 694 → 50 906 | 2 | Ta chambre, Prilly | 9 |
| materiel_industrie | 59 | en activité | 540 | 12 797 → 16 785 → 22 575 → 7 959 → 28 470 → 50 253 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 11 | en activité | 540 | 12 614 → 16 840 → 24 274 → 3 668 → 47 429 → 130 625 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 23 | en activité | 540 | 12 742 → 15 345 → 20 745 → 23 071 → 6 715 → 78 764 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 37 | en activité | 540 | 12 873 → 15 883 → 20 197 → 31 462 → 917 → 57 446 | 2 | Ta chambre, Prilly | 9 |
| materiel_pc | 41 | en activité | 540 | 12 647 → 14 767 → 18 071 → 29 509 → 8 341 → 62 919 | 3 | Ta chambre, Prilly | 9 |
| materiel_pc | 59 | en activité | 540 | 12 797 → 16 785 → 22 575 → 5 159 → 43 927 → 133 011 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 11 | en activité | 540 | 12 614 → 16 840 → 24 274 → 3 668 → 2 682 → 61 575 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 23 | en activité | 540 | 12 742 → 15 345 → 20 745 → 23 071 → 2 446 → 13 254 | 1 | Ta chambre, Prilly | 9 |
| materiel_serveur | 37 | en activité | 540 | 12 873 → 15 883 → 20 197 → 31 462 → 789 → 24 976 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 41 | en activité | 540 | 12 647 → 14 767 → 18 071 → 29 509 → -503 → 30 794 | 2 | Ta chambre, Prilly | 9 |
| materiel_serveur | 59 | en activité | 540 | 12 797 → 16 785 → 22 575 → 5 159 → -1 110 → 41 925 | 2 | Ta chambre, Prilly | 9 |
| materiel_gpu | 11 | en activité | 540 | 12 614 → 16 789 → 23 299 → 31 268 → 33 884 → 57 017 | 2 | Ta chambre, Prilly | 10 |
| materiel_gpu | 23 | en activité | 540 | 12 742 → 15 333 → 19 666 → 31 133 → 32 975 → 56 988 | 2 | Ta chambre, Prilly | 10 |
| materiel_gpu | 37 | en activité | 540 | 12 873 → 15 930 → 20 330 → 29 534 → 31 488 → 39 453 | 2 | Ta chambre, Prilly | 10 |
| materiel_gpu | 41 | en activité | 540 | 12 647 → 14 558 → 16 952 → 28 880 → 29 397 → 34 884 | 2 | Ta chambre, Prilly | 10 |
| materiel_gpu | 59 | en activité | 540 | 12 797 → 16 788 → 23 011 → 30 648 → 31 760 → 38 551 | 2 | Ta chambre, Prilly | 10 |
| ia_salle_serveur | 11 | en activité | 540 | 12 623 → 17 773 → 26 210 → 27 384 → 13 509 → 28 245 | 1 | Ta chambre, Prilly | 11 |
| ia_salle_serveur | 23 | en activité | 540 | 12 754 → 16 141 → 22 766 → 25 507 → 6 706 → 21 233 | 1 | Ta chambre, Prilly | 11 |
| ia_salle_serveur | 37 | en activité | 540 | 12 889 → 16 736 → 22 126 → 26 975 → 31 327 → 14 465 | 1 | Ta chambre, Prilly | 11 |
| ia_salle_serveur | 41 | en activité | 540 | 12 659 → 15 627 → 19 612 → 30 368 → 32 886 → 10 366 | 1 | Ta chambre, Prilly | 11 |
| ia_salle_serveur | 59 | en activité | 540 | 12 810 → 17 747 → 25 058 → 31 469 → 15 762 → 36 805 | 1 | Ta chambre, Prilly | 11 |
| marketing_excessif | 11 | faillite | 176 | 3 703 → -1 450 | 0 | Ta chambre, Prilly | 6 |
| marketing_excessif | 23 | faillite | 176 | 3 886 → 58 | 0 | Ta chambre, Prilly | 7 |
| marketing_excessif | 37 | en activité | 540 | 4 074 → 2 754 → 8 965 → 33 246 → 65 212 → 100 145 | 0 | Ta chambre, Prilly | 8 |
| marketing_excessif | 41 | faillite | 176 | 3 755 → -1 812 | 0 | Ta chambre, Prilly | 6 |
| marketing_excessif | 59 | faillite | 176 | 3 976 → 705 | 0 | Ta chambre, Prilly | 7 |
| exces_puis_correction | 11 | en activité | 540 | 3 574 → 8 192 → 27 774 → 40 395 → 74 606 → 131 187 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 23 | en activité | 540 | 3 747 → 9 910 → 31 076 → 44 903 → 80 847 → 129 206 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 37 | en activité | 540 | 3 925 → 9 964 → 30 895 → 44 710 → 81 773 → 122 244 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 41 | en activité | 540 | 3 628 → 7 664 → 24 361 → 35 368 → 57 239 → 96 466 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 59 | en activité | 540 | 3 828 → 9 657 → 30 023 → 39 967 → 70 239 → 112 766 | 1 | Ta chambre, Prilly | 8 |
| recrutement_precoce | 11 | faillite | 72 | -740 | 0 | Petit bureau, Flon | 5 |
| recrutement_precoce | 23 | faillite | 129 | -1 859 | 0 | Ta chambre, Prilly | 7 |
| recrutement_precoce | 37 | faillite | 72 | -1 521 | 0 | Petit bureau, Flon | 5 |
| recrutement_precoce | 41 | faillite | 72 | -768 | 0 | Petit bureau, Flon | 5 |
| recrutement_precoce | 59 | faillite | 72 | -1 799 | 0 | Petit bureau, Flon | 7 |
| trop_de_projets | 11 | faillite | 146 | -1 174 | 0 | Ta chambre, Prilly | 7 |
| trop_de_projets | 23 | faillite | 146 | -1 283 | 0 | Ta chambre, Prilly | 6 |
| trop_de_projets | 37 | faillite | 146 | -1 166 | 0 | Ta chambre, Prilly | 6 |
| trop_de_projets | 41 | faillite | 146 | -1 227 | 0 | Ta chambre, Prilly | 6 |
| trop_de_projets | 59 | faillite | 146 | -1 221 | 0 | Ta chambre, Prilly | 6 |
