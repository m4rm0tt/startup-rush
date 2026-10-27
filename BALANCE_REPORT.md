# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 9 façons de jouer × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu, fondateur polyvalent, réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. Aucune prestation client n'existe dans cette version.

## Résumé

| Stratégie | Faillites | Argent final (médiane) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) | Mots tapés (méd.) |
|---|---|---|---|---|---|---|---|---|---|---|
| un_projet | 0/5 | 30 827 | 11 838 | 22 | 150 (5/5) | 16 027 | 2 087 | 10 | 38 | 8 501 |
| plusieurs_petits | 0/5 | 182 961 | 12 438 | 22 | 120 (5/5) | 37 301 | 1 789 | 16 | 70 | 11 503 |
| ambitieux | 0/5 | 120 464 | 9 641 | 24 | 30 (5/5) | 16 142 | 8 345 | 13 | 30 | 3 298 |
| jeu_simple | 0/5 | 11 895 | 5 281 | 26 | 330 (5/5) | 3 206 | 3 894 | 8 | 26 | 5 618 |
| delegation | 0/5 | 102 864 | 12 438 | 22 | 120 (5/5) | 34 051 | 1 611 | 16 | 43 | 6 166 |
| marketing_excessif | 2/5 | 35 088 | -1 996 | 22 | 180 (5/5) | 19 299 | 2 437 | 9 | 35 | 9 157 |
| exces_puis_correction | 0/5 | 42 209 | 331 | 22 | 150 (5/5) | 17 666 | 2 263 | 10 | 38 | 7 928 |
| recrutement_precoce | 5/5 | -989 | -1 988 | 10 | 60 (5/5) | 1 279 | 276 | 7 | 26 | 1 401 |
| trop_de_projets | 5/5 | -1 834 | -1 995 | 26 | 120 (5/5) | 1 161 | 583 | 6 | 24 | 3 150 |

## Façons de jouer

- **un_projet** : Se concentrer sur une application, l'améliorer d'après les avis, embaucher quand elle rapporte.
- **plusieurs_petits** : Plusieurs petits projets l'un après l'autre (application, jeu, application pour PME).
- **ambitieux** : Un jeu ambitieux pour passionnés : beaucoup de composants, une embauche dès le départ, promotion avant la sortie.
- **jeu_simple** : Un petit jeu grand public, puis des mises à jour régulières.
- **delegation** : Comme plusieurs_petits, mais le fondateur arrête de taper dès la première embauche : l'équipe développe, lance et prépare les petites mises à jour.
- **marketing_excessif** : Erreur : 6 000 CHF de marketing par mois dès la sortie d'une petite application.
- **exces_puis_correction** : Même erreur de marketing, mais corrigée dès que l'argent devient tendu (budget coupé, prêt si possible).
- **recrutement_precoce** : Erreur : trois embauches dès le départ, avant le moindre revenu.
- **trop_de_projets** : Erreur : cinq projets ouverts d'un coup, chacun avec 1 500 CHF de promotion par mois avant même sa sortie.

## Détail

| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |
|---|---|---|---|---|---|---|---|
| un_projet | 11 | en activité | 540 | 12 794 → 16 699 → 27 749 → 26 684 → 31 533 → 30 910 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 23 | en activité | 540 | 12 887 → 15 676 → 23 333 → 29 450 → 23 946 → 29 567 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 37 | en activité | 540 | 12 833 → 16 809 → 27 652 → 23 602 → 26 517 → 30 502 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 41 | en activité | 540 | 12 848 → 16 126 → 24 169 → 27 658 → 28 021 → 31 707 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 59 | en activité | 540 | 12 828 → 15 073 → 22 112 → 31 326 → 24 779 → 30 827 | 2 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 11 | en activité | 540 | 12 867 → 16 040 → 27 449 → 55 769 → 111 247 → 182 961 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 23 | en activité | 540 | 12 955 → 14 387 → 25 091 → 59 871 → 136 588 → 229 700 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 37 | en activité | 540 | 12 896 → 14 624 → 24 694 → 44 122 → 91 762 → 161 524 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 41 | en activité | 540 | 12 918 → 14 602 → 21 556 → 39 744 → 63 689 → 105 319 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 59 | en activité | 540 | 12 899 → 15 195 → 25 737 → 54 564 → 125 000 → 212 323 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 11 | en activité | 540 | 18 626 → 34 217 → 63 877 → 98 404 → 127 537 → 148 112 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 23 | en activité | 540 | 18 840 → 32 641 → 57 810 → 80 070 → 95 276 → 101 584 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 37 | en activité | 540 | 15 457 → 23 203 → 39 198 → 56 307 → 69 077 → 75 712 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 41 | en activité | 540 | 18 263 → 32 218 → 57 591 → 84 783 → 110 850 → 131 761 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 59 | en activité | 540 | 15 311 → 26 921 → 52 011 → 78 141 → 102 791 → 120 464 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 11 | en activité | 540 | 14 682 → 12 658 → 13 290 → 16 550 → 20 504 → 24 491 | 0 | Ta chambre, Prilly | 8 |
| jeu_simple | 23 | en activité | 540 | 13 847 → 9 726 → 7 188 → 6 194 → 5 638 → 5 451 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 37 | en activité | 540 | 14 208 → 10 858 → 7 708 → 6 375 → 5 653 → 5 408 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 41 | en activité | 540 | 13 937 → 10 176 → 8 368 → 8 882 → 10 302 → 11 895 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 59 | en activité | 540 | 14 617 → 12 106 → 11 173 → 15 178 → 20 867 → 26 986 | 0 | Ta chambre, Prilly | 8 |
| delegation | 11 | en activité | 540 | 12 867 → 16 121 → 27 344 → 37 534 → 65 050 → 123 832 | 2 | Ta chambre, Prilly | 8 |
| delegation | 23 | en activité | 540 | 12 955 → 14 284 → 23 869 → 35 224 → 62 916 → 102 864 | 2 | Ta chambre, Prilly | 8 |
| delegation | 37 | en activité | 540 | 12 896 → 14 369 → 23 680 → 34 819 → 64 459 → 121 615 | 2 | Ta chambre, Prilly | 8 |
| delegation | 41 | en activité | 540 | 12 918 → 14 758 → 21 725 → 28 880 → 29 156 → 28 064 | 1 | Ta chambre, Prilly | 8 |
| delegation | 59 | en activité | 540 | 12 899 → 15 186 → 24 865 → 35 531 → 27 586 → 18 467 | 2 | Ta chambre, Prilly | 8 |
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
| recrutement_precoce | 11 | faillite | 72 | -138 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 23 | faillite | 129 | -960 | 0 | Ta chambre, Prilly | 6 |
| recrutement_precoce | 37 | faillite | 72 | -989 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 41 | faillite | 72 | -1 947 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 59 | faillite | 72 | -1 157 | 0 | Petit bureau, Flon | 6 |
| trop_de_projets | 11 | faillite | 146 | -1 359 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 23 | faillite | 146 | -1 401 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 37 | faillite | 146 | -1 381 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 41 | faillite | 146 | -1 380 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 59 | faillite | 146 | -1 370 | 0 | Ta chambre, Prilly | 5 |
