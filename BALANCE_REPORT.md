# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 14 façons de jouer × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu, fondateur polyvalent, réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. Aucune prestation client n'existe dans cette version.

## Résumé

| Stratégie | Faillites | Argent final (médiane) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) | Mots tapés (méd.) |
|---|---|---|---|---|---|---|---|---|---|---|
| un_projet | 0/5 | 30 827 | 11 838 | 22 | 150 (5/5) | 16 027 | 2 087 | 10 | 38 | 8 501 |
| plusieurs_petits | 0/5 | 191 938 | 12 438 | 22 | 120 (5/5) | 37 141 | 1 802 | 16 | 69 | 11 503 |
| ambitieux | 0/5 | 120 469 | 9 641 | 24 | 30 (5/5) | 16 142 | 8 344 | 13 | 30 | 3 296 |
| jeu_simple | 0/5 | 11 895 | 5 281 | 26 | 330 (5/5) | 3 206 | 3 894 | 8 | 26 | 5 618 |
| delegation | 0/5 | 103 308 | 12 438 | 22 | 120 (5/5) | 34 258 | 1 617 | 16 | 43 | 6 166 |
| hebergement_maison | 0/5 | 77 865 | 12 384 | 22 | 120 (5/5) | 31 291 | 1 466 | 19 | 103 | 11 503 |
| materiel | 0/5 | 8 570 | 1 914 | 22 | 120 (5/5) | 20 198 | 1 085 | 19 | 225 | 11 503 |
| ia | 0/5 | 124 868 | 12 465 | 22 | 120 (5/5) | 33 164 | 1 993 | 22 | 87 | 11 499 |
| studio_xr | 0/5 | 41 635 | 12 438 | 22 | 120 (5/5) | 15 318 | 1 318 | 16 | 86 | 11 503 |
| quantique | 0/5 | 76 499 | 12 438 | 22 | 120 (5/5) | 30 514 | 1 585 | 16 | 76 | 11 503 |
| marketing_excessif | 2/5 | 35 088 | -1 996 | 22 | 180 (5/5) | 19 299 | 2 437 | 9 | 35 | 9 157 |
| exces_puis_correction | 0/5 | 42 209 | 331 | 22 | 150 (5/5) | 17 666 | 2 263 | 10 | 38 | 7 928 |
| recrutement_precoce | 5/5 | -1 006 | -1 995 | 10 | 60 (5/5) | 1 280 | 276 | 7 | 26 | 1 401 |
| trop_de_projets | 5/5 | -1 834 | -1 995 | 26 | 120 (5/5) | 1 161 | 583 | 6 | 24 | 3 150 |

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
| 11 | en activité | 12 867 → 16 231 → 25 657 → 29 554 → 33 196 → 33 073 → 63 717 → 89 098 → 132 458 → 197 097 → 274 725 → 64 275 | 8 | labo_avance (j1019) |
| 23 | en activité | 12 955 → 14 173 → 25 088 → 44 212 → 84 938 → 138 337 → 212 781 → 288 284 → 78 628 → 121 843 → 172 619 → 224 965 | 9 | labo_avance (j742), calibration_cryo (j801) |
| 37 | en activité | 12 896 → 14 476 → 22 190 → 33 229 → 48 119 → 76 499 → 146 019 → 217 664 → 42 662 → 82 717 → 133 436 → 185 093 | 9 | labo_avance (j809), calibration_cryo (j854) |
| 41 | en activité | 12 918 → 14 668 → 21 284 → 25 796 → 23 320 → 26 567 → 52 942 → 68 800 → 101 720 → 150 881 → 210 286 → 275 356 | 8 | aucun |
| 59 | en activité | 12 899 → 14 912 → 23 536 → 31 595 → 57 823 → 96 733 → 163 002 → 227 399 → 50 404 → 72 394 → 105 876 → 145 953 | 9 | labo_avance (j805), calibration_cryo (j886) |

## Détail

| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |
|---|---|---|---|---|---|---|---|
| un_projet | 11 | en activité | 540 | 12 794 → 16 699 → 27 749 → 26 684 → 31 533 → 30 910 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 23 | en activité | 540 | 12 887 → 15 676 → 23 333 → 29 450 → 23 946 → 29 567 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 37 | en activité | 540 | 12 833 → 16 809 → 27 652 → 23 602 → 26 517 → 30 502 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 41 | en activité | 540 | 12 848 → 16 126 → 24 169 → 27 658 → 28 021 → 31 707 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 59 | en activité | 540 | 12 828 → 15 073 → 22 112 → 31 326 → 24 779 → 30 827 | 2 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 11 | en activité | 540 | 12 867 → 16 040 → 27 449 → 55 904 → 117 244 → 191 938 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 23 | en activité | 540 | 12 955 → 14 387 → 25 091 → 59 765 → 135 729 → 228 466 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 37 | en activité | 540 | 12 896 → 14 624 → 24 694 → 43 175 → 80 946 → 138 375 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 41 | en activité | 540 | 12 918 → 14 602 → 21 556 → 39 746 → 63 711 → 105 371 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 59 | en activité | 540 | 12 899 → 15 195 → 25 737 → 53 566 → 117 384 → 199 536 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 11 | en activité | 540 | 18 626 → 34 217 → 63 877 → 98 404 → 127 537 → 148 112 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 23 | en activité | 540 | 18 840 → 32 641 → 57 810 → 80 070 → 95 276 → 101 584 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 37 | en activité | 540 | 15 452 → 23 133 → 39 103 → 56 195 → 68 955 → 75 586 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 41 | en activité | 540 | 18 261 → 32 207 → 57 576 → 84 766 → 110 831 → 131 742 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 59 | en activité | 540 | 15 312 → 26 923 → 52 014 → 78 145 → 102 795 → 120 469 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 11 | en activité | 540 | 14 682 → 12 658 → 13 290 → 16 550 → 20 504 → 24 491 | 0 | Ta chambre, Prilly | 8 |
| jeu_simple | 23 | en activité | 540 | 13 847 → 9 726 → 7 188 → 6 194 → 5 638 → 5 451 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 37 | en activité | 540 | 14 208 → 10 858 → 7 708 → 6 375 → 5 653 → 5 408 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 41 | en activité | 540 | 13 937 → 10 176 → 8 368 → 8 882 → 10 302 → 11 895 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 59 | en activité | 540 | 14 617 → 12 106 → 11 173 → 15 178 → 20 867 → 26 986 | 0 | Ta chambre, Prilly | 8 |
| delegation | 11 | en activité | 540 | 12 867 → 16 121 → 27 344 → 37 536 → 64 665 → 122 431 | 2 | Ta chambre, Prilly | 8 |
| delegation | 23 | en activité | 540 | 12 955 → 14 284 → 23 869 → 35 283 → 62 933 → 103 308 | 2 | Ta chambre, Prilly | 8 |
| delegation | 37 | en activité | 540 | 12 896 → 14 369 → 23 680 → 34 808 → 64 230 → 120 620 | 2 | Ta chambre, Prilly | 8 |
| delegation | 41 | en activité | 540 | 12 918 → 14 758 → 21 725 → 28 880 → 29 131 → 28 008 | 1 | Ta chambre, Prilly | 8 |
| delegation | 59 | en activité | 540 | 12 899 → 15 186 → 24 865 → 35 562 → 27 879 → 19 012 | 2 | Ta chambre, Prilly | 8 |
| hebergement_maison | 11 | en activité | 540 | 12 867 → 16 240 → 26 013 → 22 363 → 54 838 → 115 431 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 23 | en activité | 540 | 12 955 → 14 169 → 24 512 → 17 171 → 20 578 → 28 185 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 37 | en activité | 540 | 12 896 → 14 480 → 23 291 → 22 480 → 40 655 → 75 772 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 41 | en activité | 540 | 12 918 → 14 667 → 21 638 → 16 047 → 34 484 → 77 865 | 1 | Ta chambre, Prilly | 9 |
| hebergement_maison | 59 | en activité | 540 | 12 899 → 15 030 → 22 736 → 17 177 → 33 589 → 79 347 | 1 | Ta chambre, Prilly | 9 |
| materiel | 11 | en activité | 540 | 12 867 → 16 240 → 26 013 → 10 881 → 22 115 → 16 052 | 2 | Ta chambre, Prilly | 9 |
| materiel | 23 | en activité | 540 | 12 955 → 14 169 → 24 512 → 9 735 → 11 755 → 11 447 | 1 | Ta chambre, Prilly | 9 |
| materiel | 37 | en activité | 540 | 12 896 → 14 480 → 23 291 → 13 839 → 20 916 → 4 307 | 2 | Ta chambre, Prilly | 9 |
| materiel | 41 | en activité | 540 | 12 918 → 14 667 → 21 638 → 4 469 → 6 221 → 2 891 | 1 | Ta chambre, Prilly | 9 |
| materiel | 59 | en activité | 540 | 12 899 → 15 030 → 22 736 → 5 266 → 10 015 → 8 570 | 1 | Ta chambre, Prilly | 9 |
| ia | 11 | en activité | 540 | 12 919 → 17 218 → 30 204 → 47 865 → 119 851 → 219 308 | 1 | Ta chambre, Prilly | 8 |
| ia | 23 | en activité | 540 | 13 016 → 15 074 → 29 185 → 60 579 → 142 432 → 257 078 | 1 | Ta chambre, Prilly | 8 |
| ia | 37 | en activité | 540 | 12 956 → 15 615 → 27 371 → 41 561 → 78 095 → 122 468 | 1 | Ta chambre, Prilly | 8 |
| ia | 41 | en activité | 540 | 12 973 → 15 532 → 24 925 → 39 460 → 70 145 → 105 706 | 1 | Ta chambre, Prilly | 8 |
| ia | 59 | en activité | 540 | 12 953 → 15 926 → 26 681 → 41 477 → 76 302 → 124 868 | 1 | Ta chambre, Prilly | 8 |
| studio_xr | 11 | en activité | 540 | 12 867 → 16 240 → 23 203 → 31 320 → 32 460 → 36 679 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 23 | en activité | 540 | 12 955 → 14 169 → 21 131 → 33 583 → 42 114 → 51 291 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 37 | en activité | 540 | 12 896 → 14 480 → 19 985 → 30 960 → 34 477 → 42 648 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 41 | en activité | 540 | 12 918 → 14 667 → 18 570 → 29 524 → 30 831 → 34 726 | 1 | Ta chambre, Prilly | 9 |
| studio_xr | 59 | en activité | 540 | 12 899 → 15 030 → 19 971 → 29 506 → 32 741 → 41 635 | 1 | Ta chambre, Prilly | 9 |
| quantique | 11 | en activité | 540 | 12 867 → 16 231 → 25 657 → 29 554 → 33 196 → 33 073 | 2 | Ta chambre, Prilly | 7 |
| quantique | 23 | en activité | 540 | 12 955 → 14 173 → 25 088 → 44 212 → 84 938 → 138 337 | 2 | Ta chambre, Prilly | 8 |
| quantique | 37 | en activité | 540 | 12 896 → 14 476 → 22 190 → 33 229 → 48 119 → 76 499 | 2 | Ta chambre, Prilly | 8 |
| quantique | 41 | en activité | 540 | 12 918 → 14 668 → 21 284 → 25 796 → 23 320 → 26 567 | 2 | Ta chambre, Prilly | 7 |
| quantique | 59 | en activité | 540 | 12 899 → 14 912 → 23 536 → 31 595 → 57 823 → 96 733 | 2 | Ta chambre, Prilly | 8 |
| marketing_excessif | 11 | en activité | 540 | 6 713 → 3 370 → -673 → 1 317 → 12 280 → 39 168 | 0 | Ta chambre, Prilly | 7 |
| marketing_excessif | 23 | faillite | 207 | 6 853 → 1 055 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 37 | en activité | 540 | 6 784 → 3 436 → -458 → 1 802 → 12 298 → 39 632 | 0 | Ta chambre, Prilly | 7 |
| marketing_excessif | 41 | faillite | 207 | 6 784 → 2 242 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 59 | en activité | 540 | 6 774 → 3 061 → -1 485 → -164 → 9 479 → 35 088 | 0 | Ta chambre, Prilly | 7 |
| exces_puis_correction | 11 | en activité | 540 | 6 711 → 6 732 → 20 884 → 27 861 → 28 432 → 42 209 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 23 | en activité | 540 | 6 850 → 4 837 → 13 371 → 23 037 → 28 886 → 31 621 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 37 | en activité | 540 | 6 781 → 6 890 → 21 905 → 25 242 → 27 301 → 44 791 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 41 | en activité | 540 | 6 782 → 5 870 → 15 826 → 26 439 → 34 702 → 40 439 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 59 | en activité | 540 | 6 772 → 6 583 → 23 159 → 28 417 → 36 287 → 60 220 | 1 | Ta chambre, Prilly | 8 |
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
