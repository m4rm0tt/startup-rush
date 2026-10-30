# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 14 façons de jouer × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu, fondateur polyvalent, réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. Aucune prestation client n'existe dans cette version.

## Résumé

| Stratégie | Faillites | Argent final (médiane) | Argent final (min – max) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) | Mots tapés (méd.) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| un_projet | 0/5 | 21 673 | 16 308 – 23 578 | 11 838 | 22 | 150 (5/5) | 10 453 | 1 343 | 10 | 43 | 10 306 |
| plusieurs_petits | 0/5 | 171 601 | 86 205 – 235 211 | 12 438 | 22 | 120 (5/5) | 34 331 | 1 630 | 16 | 75 | 11 503 |
| ambitieux | 0/5 | 144 673 | 92 793 – 170 120 | 9 641 | 24 | 30 (5/5) | 16 939 | 9 775 | 13 | 33 | 3 637 |
| jeu_simple | 0/5 | 2 722 | 1 010 – 22 042 | -173 | 26 | 480 (5/5) | 3 156 | 2 763 | 8 | 32 | 8 408 |
| delegation | 0/5 | 121 960 | 20 413 – 137 814 | 12 438 | 22 | 120 (5/5) | 42 281 | 1 940 | 16 | 43 | 6 166 |
| hebergement_maison | 0/5 | 66 955 | 24 867 – 133 925 | 12 384 | 22 | 120 (5/5) | 32 057 | 1 530 | 19 | 124 | 11 503 |
| materiel | 0/5 | 10 937 | 4 316 – 14 822 | 1 914 | 22 | 120 (5/5) | 20 885 | 1 113 | 19 | 214 | 11 503 |
| ia | 0/5 | 120 552 | 106 141 – 264 346 | 12 465 | 22 | 120 (5/5) | 27 652 | 1 579 | 22 | 89 | 11 499 |
| studio_xr | 0/5 | 39 944 | 35 791 – 54 437 | 12 438 | 22 | 120 (5/5) | 15 164 | 1 222 | 16 | 89 | 11 503 |
| quantique | 0/5 | 83 316 | 10 580 – 144 700 | 10 062 | 22 | 120 (5/5) | 30 017 | 1 454 | 16 | 83 | 11 503 |
| marketing_excessif | 2/5 | 516 | -1 688 – 5 740 | -1 990 | 22 | 180 (5/5) | 8 002 | 994 | 7 | 40 | 11 472 |
| exces_puis_correction | 0/5 | 28 539 | 14 872 – 36 395 | 331 | 22 | 150 (5/5) | 14 471 | 1 823 | 10 | 44 | 9 815 |
| recrutement_precoce | 5/5 | -1 006 | -1 963 – 1 456 | -1 995 | 10 | 60 (5/5) | 1 280 | 276 | 7 | 26 | 1 401 |
| trop_de_projets | 5/5 | -1 834 | -1 952 – -23 | -1 995 | 26 | 120 (5/5) | 1 161 | 583 | 6 | 24 | 3 150 |

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
| ia | ia_publiee | 5/5 | 281 |
| ia | produit_ia | 5/5 | 281 |
| studio_xr | studio_pose | 5/5 | 248 |
| studio_xr | studio_complet | 5/5 | 248 |
| studio_xr | zone_xr_posee | 5/5 | 288 |
| studio_xr | zone_xr_complete | 4/5 | 317 |

### Horizon long : quantique sur 36 mois

| Graine | Issue | Argent par trimestre | Nœuds | Jalons |
|---|---|---|---|---|
| 11 | en activité | 12 867 → 16 231 → 25 657 → 29 609 → 22 852 → 10 580 → 32 003 → 49 814 → 71 916 → 100 396 → 146 834 → 201 699 | 8 | aucun |
| 23 | en activité | 12 955 → 14 173 → 25 088 → 44 459 → 93 242 → 144 700 → 208 675 → 250 931 → 291 554 → 50 878 → 70 388 → 94 507 | 9 | labo_avance (j833), calibration_cryo (j892) |
| 37 | en activité | 12 896 → 14 476 → 22 190 → 33 434 → 51 726 → 83 316 → 132 268 → 160 687 → 186 120 → 223 517 → 272 584 → 51 197 | 8 | labo_avance (j1043) |
| 41 | en activité | 12 918 → 14 668 → 21 284 → 25 815 → 23 357 → 25 823 → 49 249 → 60 447 → 73 215 → 84 833 → 109 085 → 147 012 | 8 | aucun |
| 59 | en activité | 12 899 → 14 912 → 23 536 → 31 245 → 53 904 → 90 113 → 146 617 → 184 909 → 219 815 → 268 609 → 52 168 → 49 643 | 9 | labo_avance (j954), calibration_cryo (j1037) |

## Détail

| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |
|---|---|---|---|---|---|---|---|
| un_projet | 11 | en activité | 540 | 12 794 → 16 699 → 28 031 → 24 539 → 15 661 → 22 018 | 1 | Ta chambre, Prilly | 8 |
| un_projet | 23 | en activité | 540 | 12 887 → 15 676 → 23 583 → 33 371 → 24 423 → 16 308 | 1 | Ta chambre, Prilly | 7 |
| un_projet | 37 | en activité | 540 | 12 833 → 16 809 → 27 925 → 25 111 → 20 131 → 23 578 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 41 | en activité | 540 | 12 848 → 16 126 → 24 467 → 33 947 → 20 936 → 17 136 | 1 | Ta chambre, Prilly | 8 |
| un_projet | 59 | en activité | 540 | 12 828 → 15 073 → 22 361 → 31 401 → 33 516 → 21 673 | 1 | Ta chambre, Prilly | 6 |
| plusieurs_petits | 11 | en activité | 540 | 12 867 → 16 040 → 27 449 → 55 761 → 108 641 → 171 601 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 23 | en activité | 540 | 12 955 → 14 387 → 25 091 → 59 762 → 138 834 → 235 211 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 37 | en activité | 540 | 12 896 → 14 624 → 24 694 → 43 282 → 79 929 → 136 162 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 41 | en activité | 540 | 12 918 → 14 602 → 21 556 → 39 802 → 61 690 → 86 205 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 59 | en activité | 540 | 12 899 → 15 195 → 25 737 → 53 600 → 119 102 → 202 122 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 11 | en activité | 540 | 18 707 → 34 861 → 68 589 → 110 420 → 148 990 → 170 120 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 23 | en activité | 540 | 18 898 → 35 003 → 67 035 → 101 798 → 129 340 → 144 673 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 37 | en activité | 540 | 15 452 → 23 494 → 45 661 → 68 084 → 83 237 → 92 793 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 41 | en activité | 540 | 18 325 → 31 634 → 54 674 → 74 995 → 94 419 → 109 401 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 59 | en activité | 540 | 15 342 → 27 988 → 59 201 → 98 331 → 138 112 → 166 798 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 11 | en activité | 540 | 14 682 → 12 756 → 9 169 → 7 411 → 12 769 → 22 042 | 0 | Ta chambre, Prilly | 8 |
| jeu_simple | 23 | en activité | 540 | 13 847 → 9 919 → 4 763 → 1 032 → 514 → 1 010 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 37 | en activité | 540 | 14 208 → 11 112 → 5 822 → 2 007 → 1 949 → 2 722 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 41 | en activité | 540 | 13 937 → 10 207 → 4 851 → 2 043 → 1 033 → 1 543 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 59 | en activité | 540 | 14 617 → 12 347 → 8 221 → 5 445 → 8 151 → 13 366 | 0 | Ta chambre, Prilly | 7 |
| delegation | 11 | en activité | 540 | 12 867 → 16 121 → 27 344 → 37 536 → 71 040 → 137 814 | 2 | Ta chambre, Prilly | 8 |
| delegation | 23 | en activité | 540 | 12 955 → 14 284 → 23 869 → 35 283 → 63 307 → 121 960 | 2 | Ta chambre, Prilly | 8 |
| delegation | 37 | en activité | 540 | 12 896 → 14 369 → 23 680 → 34 808 → 65 458 → 125 669 | 2 | Ta chambre, Prilly | 8 |
| delegation | 41 | en activité | 540 | 12 918 → 14 758 → 21 725 → 28 880 → 29 226 → 28 300 | 1 | Ta chambre, Prilly | 8 |
| delegation | 59 | en activité | 540 | 12 899 → 15 186 → 24 865 → 35 562 → 27 842 → 20 413 | 2 | Ta chambre, Prilly | 8 |
| hebergement_maison | 11 | en activité | 540 | 12 867 → 16 240 → 26 013 → 22 393 → 60 251 → 133 925 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 23 | en activité | 540 | 12 955 → 14 169 → 24 512 → 17 171 → 16 496 → 24 867 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 37 | en activité | 540 | 12 896 → 14 480 → 23 291 → 22 400 → 30 056 → 57 299 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 41 | en activité | 540 | 12 918 → 14 667 → 21 638 → 16 103 → 34 523 → 94 418 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 59 | en activité | 540 | 12 899 → 15 030 → 22 736 → 17 112 → 25 339 → 66 955 | 1 | Ta chambre, Prilly | 9 |
| materiel | 11 | en activité | 540 | 12 867 → 16 240 → 26 013 → 10 881 → 22 660 → 12 728 | 2 | Ta chambre, Prilly | 9 |
| materiel | 23 | en activité | 540 | 12 955 → 14 169 → 24 512 → 9 735 → 12 292 → 14 822 | 1 | Ta chambre, Prilly | 9 |
| materiel | 37 | en activité | 540 | 12 896 → 14 480 → 23 291 → 13 839 → 22 633 → 4 316 | 2 | Ta chambre, Prilly | 9 |
| materiel | 41 | en activité | 540 | 12 918 → 14 667 → 21 638 → 4 469 → 7 401 → 4 335 | 1 | Ta chambre, Prilly | 9 |
| materiel | 59 | en activité | 540 | 12 899 → 15 030 → 22 736 → 5 266 → 10 496 → 10 937 | 1 | Ta chambre, Prilly | 9 |
| ia | 11 | en activité | 540 | 12 919 → 17 218 → 30 204 → 47 865 → 119 314 → 223 260 | 1 | Ta chambre, Prilly | 8 |
| ia | 23 | en activité | 540 | 13 016 → 15 074 → 29 185 → 60 579 → 142 942 → 264 346 | 1 | Ta chambre, Prilly | 8 |
| ia | 37 | en activité | 540 | 12 956 → 15 615 → 27 371 → 41 561 → 78 417 → 120 552 | 1 | Ta chambre, Prilly | 8 |
| ia | 41 | en activité | 540 | 12 973 → 15 532 → 24 925 → 39 460 → 70 303 → 106 141 | 1 | Ta chambre, Prilly | 8 |
| ia | 59 | en activité | 540 | 12 953 → 15 926 → 26 681 → 41 477 → 76 718 → 117 307 | 1 | Ta chambre, Prilly | 8 |
| studio_xr | 11 | en activité | 540 | 12 867 → 16 240 → 23 203 → 31 320 → 32 526 → 37 238 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 23 | en activité | 540 | 12 955 → 14 169 → 21 131 → 33 583 → 42 438 → 54 437 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 37 | en activité | 540 | 12 896 → 14 480 → 19 985 → 30 960 → 34 597 → 39 973 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 41 | en activité | 540 | 12 918 → 14 667 → 18 570 → 29 524 → 30 852 → 35 791 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 59 | en activité | 540 | 12 899 → 15 030 → 19 971 → 29 506 → 32 929 → 39 944 | 1 | Ta chambre, Prilly | 9 |
| quantique | 11 | en activité | 540 | 12 867 → 16 231 → 25 657 → 29 609 → 22 852 → 10 580 | 2 | Ta chambre, Prilly | 7 |
| quantique | 23 | en activité | 540 | 12 955 → 14 173 → 25 088 → 44 459 → 93 242 → 144 700 | 2 | Ta chambre, Prilly | 8 |
| quantique | 37 | en activité | 540 | 12 896 → 14 476 → 22 190 → 33 434 → 51 726 → 83 316 | 2 | Ta chambre, Prilly | 8 |
| quantique | 41 | en activité | 540 | 12 918 → 14 668 → 21 284 → 25 815 → 23 357 → 25 823 | 1 | Ta chambre, Prilly | 7 |
| quantique | 59 | en activité | 540 | 12 899 → 14 912 → 23 536 → 31 245 → 53 904 → 90 113 | 2 | Ta chambre, Prilly | 8 |
| marketing_excessif | 11 | en activité | 540 | 6 713 → 3 370 → -540 → 1 864 → 1 177 → 516 | 0 | Ta chambre, Prilly | 6 |
| marketing_excessif | 23 | faillite | 207 | 6 853 → 1 055 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 37 | en activité | 540 | 6 784 → 3 436 → 411 → 4 651 → 5 478 → 5 740 | 0 | Ta chambre, Prilly | 7 |
| marketing_excessif | 41 | faillite | 207 | 6 784 → 2 242 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 59 | en activité | 540 | 6 774 → 3 061 → -1 347 → 148 → -1 959 → 777 | 0 | Ta chambre, Prilly | 6 |
| exces_puis_correction | 11 | en activité | 540 | 6 711 → 6 732 → 21 205 → 28 491 → 21 523 → 28 539 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 23 | en activité | 540 | 6 850 → 4 837 → 13 661 → 24 231 → 33 876 → 27 407 | 1 | Ta chambre, Prilly | 6 |
| exces_puis_correction | 37 | en activité | 540 | 6 781 → 6 890 → 22 020 → 24 651 → 12 577 → 14 872 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 41 | en activité | 540 | 6 782 → 5 870 → 16 043 → 26 288 → 34 027 → 36 395 | 1 | Ta chambre, Prilly | 6 |
| exces_puis_correction | 59 | en activité | 540 | 6 772 → 6 583 → 22 969 → 28 064 → 23 017 → 34 914 | 1 | Ta chambre, Prilly | 8 |
| recrutement_precoce | 11 | faillite | 72 | -154 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 23 | faillite | 129 | -960 | 0 | Ta chambre, Prilly | 6 |
| recrutement_precoce | 37 | faillite | 72 | -1 006 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 41 | faillite | 72 | -1 963 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 59 | faillite | 72 | -1 172 | 0 | Petit bureau, Flon | 6 |
| trop_de_projets | 11 | faillite | 146 | -1 359 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 23 | faillite | 146 | -1 401 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 37 | faillite | 146 | -1 381 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 41 | faillite | 146 | -1 380 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 59 | faillite | 146 | -1 370 | 0 | Ta chambre, Prilly | 5 |
