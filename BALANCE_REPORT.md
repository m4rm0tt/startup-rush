# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 8 façons de jouer × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu, fondateur polyvalent, réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. Aucune prestation client n'existe dans cette version.

## Résumé

| Stratégie | Faillites | Argent final (médiane) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) |
|---|---|---|---|---|---|---|---|---|---|
| un_projet | 0/5 | 29 759 | 11 806 | 22 | 150 (5/5) | 16 492 | 2 135 | 10 | 38 |
| plusieurs_petits | 0/5 | 204 277 | 12 428 | 22 | 120 (5/5) | 39 671 | 1 838 | 16 | 69 |
| ambitieux | 0/5 | 105 530 | 9 641 | 24 | 30 (5/5) | 14 209 | 8 272 | 13 | 30 |
| jeu_simple | 0/5 | 7 133 | 4 848 | 26 | 270 (3/5) | 2 748 | 3 306 | 8 | 26 |
| marketing_excessif | 2/5 | 29 921 | -2 000 | 22 | 180 (5/5) | 18 338 | 2 353 | 9 | 35 |
| exces_puis_correction | 0/5 | 43 897 | 291 | 22 | 150 (5/5) | 19 584 | 2 531 | 10 | 38 |
| recrutement_precoce | 5/5 | -997 | -1 990 | 10 | 60 (5/5) | 1 265 | 270 | 7 | 26 |
| trop_de_projets | 5/5 | -1 861 | -1 991 | 26 | 120 (5/5) | 1 145 | 577 | 6 | 24 |

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
| un_projet | 11 | en activité | 540 | 12 779 → 16 887 → 27 429 → 30 898 → 30 141 → 30 486 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 23 | en activité | 540 | 12 871 → 15 998 → 23 675 → 32 180 → 29 098 → 28 677 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 37 | en activité | 540 | 12 811 → 15 749 → 23 180 → 30 691 → 23 162 → 29 759 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 41 | en activité | 540 | 12 832 → 16 519 → 24 910 → 26 948 → 25 821 → 26 727 | 2 | Ta chambre, Prilly | 8 |
| un_projet | 59 | en activité | 540 | 12 813 → 15 221 → 21 930 → 31 116 → 34 976 → 35 738 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 11 | en activité | 540 | 12 852 → 15 921 → 27 085 → 56 435 → 126 218 → 208 151 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 23 | en activité | 540 | 12 940 → 14 281 → 24 688 → 54 630 → 122 103 → 204 277 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 37 | en activité | 540 | 12 880 → 14 486 → 24 523 → 53 321 → 121 954 → 205 751 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 41 | en activité | 540 | 12 903 → 14 494 → 21 262 → 39 994 → 54 138 → 84 553 | 1 | Ta chambre, Prilly | 8 |
| plusieurs_petits | 59 | en activité | 540 | 12 885 → 15 086 → 25 351 → 53 092 → 117 364 → 193 695 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 11 | en activité | 540 | 18 503 → 33 804 → 63 050 → 97 083 → 125 761 → 145 985 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 23 | en activité | 540 | 18 724 → 32 248 → 57 035 → 78 879 → 93 725 → 99 767 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 37 | en activité | 540 | 15 362 → 22 763 → 38 448 → 50 889 → 56 311 → 57 692 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 41 | en activité | 540 | 18 160 → 31 825 → 56 778 → 83 500 → 109 090 → 129 602 | 1 | Ta chambre, Prilly | 8 |
| ambitieux | 59 | en activité | 540 | 15 209 → 26 537 → 51 371 → 77 459 → 95 132 → 105 530 | 1 | Ta chambre, Prilly | 8 |
| jeu_simple | 11 | en activité | 540 | 14 666 → 12 596 → 13 127 → 16 234 → 19 998 → 23 826 | 0 | Ta chambre, Prilly | 8 |
| jeu_simple | 23 | en activité | 540 | 13 832 → 9 676 → 7 062 → 5 978 → 5 314 → 4 998 | 0 | Ta chambre, Prilly | 5 |
| jeu_simple | 37 | en activité | 540 | 14 192 → 10 787 → 7 556 → 6 135 → 5 302 → 4 925 | 0 | Ta chambre, Prilly | 5 |
| jeu_simple | 41 | en activité | 540 | 13 922 → 10 090 → 7 318 → 6 792 → 6 958 → 7 133 | 0 | Ta chambre, Prilly | 7 |
| jeu_simple | 59 | en activité | 540 | 14 601 → 12 445 → 11 867 → 16 111 → 22 070 → 27 967 | 0 | Ta chambre, Prilly | 8 |
| marketing_excessif | 11 | en activité | 540 | 6 694 → 3 785 → -361 → 1 071 → 8 545 → 31 250 | 0 | Ta chambre, Prilly | 7 |
| marketing_excessif | 23 | faillite | 207 | 6 829 → 1 198 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 37 | en activité | 540 | 6 764 → 3 811 → -290 → 1 635 → 11 050 → 36 895 | 0 | Ta chambre, Prilly | 7 |
| marketing_excessif | 41 | faillite | 237 | 6 765 → 2 715 | 0 | Ta chambre, Prilly | 5 |
| marketing_excessif | 59 | en activité | 540 | 6 754 → 3 234 → 3 826 → -41 → 5 914 → 29 921 | 0 | Ta chambre, Prilly | 7 |
| exces_puis_correction | 11 | en activité | 540 | 6 692 → 7 184 → 23 332 → 27 417 → 34 079 → 55 297 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 23 | en activité | 540 | 6 827 → 4 952 → 13 350 → 23 123 → 33 449 → 36 239 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 37 | en activité | 540 | 6 761 → 7 227 → 22 245 → 25 394 → 27 042 → 43 897 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 41 | en activité | 540 | 6 762 → 6 280 → 16 295 → 26 603 → 33 495 → 37 418 | 1 | Ta chambre, Prilly | 8 |
| exces_puis_correction | 59 | en activité | 540 | 6 752 → 6 718 → 22 078 → 29 231 → 33 732 → 53 462 | 1 | Ta chambre, Prilly | 8 |
| recrutement_precoce | 11 | faillite | 72 | -150 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 23 | faillite | 129 | -992 | 0 | Ta chambre, Prilly | 6 |
| recrutement_precoce | 37 | faillite | 72 | -997 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 41 | faillite | 72 | -1 963 | 0 | Petit bureau, Flon | 6 |
| recrutement_precoce | 59 | faillite | 72 | -1 173 | 0 | Petit bureau, Flon | 6 |
| trop_de_projets | 11 | faillite | 146 | -1 365 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 23 | faillite | 146 | -1 406 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 37 | faillite | 146 | -1 387 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 41 | faillite | 146 | -1 385 | 0 | Ta chambre, Prilly | 5 |
| trop_de_projets | 59 | faillite | 146 | -1 376 | 0 | Ta chambre, Prilly | 5 |
