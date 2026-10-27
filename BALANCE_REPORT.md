# Bilan d'équilibrage mesuré

Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).
Protocole : 9 stratégies × 5 graines (11, 23, 37, 41, 59), 18 mois de jeu chacune, fondateur polyvalent, valeurs de `Core/campaign/Tuning.cs`. Les stratégies sont jouées par `Core/bots/Autopilot.cs`, une décision par jour ouvré, avec les mêmes commandes que l'interface.

## Résumé par stratégie

| Stratégie | Liquidations | Trésorerie finale (médiane) | Pire creux | Mois en impayés (moy.) | Recettes 18 mois (méd.) | Revenus récurrents finaux (méd.) | Équipe max | Contrats livrés / rompus |
|---|---|---|---|---|---|---|---|---|
| freelance | 0/5 | 112 967 | 4 800 | 0.0 | 148 287 | 0 | 0 | 79 / 0 |
| agence | 1/5 | 92 268 | -1 580 | 1.0 | 528 577 | 0 | 5 | 170 / 25 |
| produit_tot | 0/5 | 2 143 | -788 | 0.0 | 41 777 | 3 027 | 0 | 7 / 0 |
| produit_soigne | 3/5 | -1 363 | -1 975 | 2.4 | 140 663 | 2 263 | 3 | 71 / 0 |
| croissance | 5/5 | -553 | -1 568 | 3.0 | 49 260 | 409 | 3 | 45 / 0 |
| embauche_rapide | 2/5 | 18 281 | -1 951 | 2.2 | 824 327 | 0 | 6 | 172 / 42 |
| marketing | 5/5 | -1 058 | -1 996 | 2.8 | 84 041 | 596 | 3 | 56 / 0 |
| sous_qualite | 5/5 | 3 801 | -1 983 | 4.2 | 88 590 | 941 | 2 | 60 / 1 |
| mixte | 1/5 | 1 | -1 988 | 1.8 | 300 346 | 7 318 | 4 | 89 / 0 |

## Stratégies jouées

- **freelance** : Freelance prudent : seulement des prestations, délai sûr, personne d'embauché.
- **agence** : Agence : prestations en continu, code capitalisé, embauches dès que la trésorerie le permet.
- **produit_tot** : Produit lancé tôt : un SaaS minimal dès la 2e semaine, le fondateur dessus, quelques prestations pour survivre.
- **produit_soigne** : Produit très soigné : prestations d'abord, puis un SaaS complet avec support et correctifs.
- **croissance** : Croissance financée : embauches rapides et marketing agressif sur un produit communautaire.
- **embauche_rapide** : Embauche rapide sans produit : beaucoup de salaires, des prestations pour les payer.
- **marketing** : Marketing agressif sur une place de marché, sans gros soin du produit.
- **sous_qualite** : Sous-investissement dans la qualité : SaaS sans tests de support ni correctifs, promesses rapides.
- **mixte** : Stratégie mixte : prestations pour financer, SaaS PME bien construit, croissance mesurée.

## Détail des parties

| Stratégie | Graine | Issue | Jours | Trésorerie par trimestre | Produit : utilisateurs / payants / bugs | Local final | Équipe finale | Matériel acheté |
|---|---|---|---|---|---|---|---|---|
| freelance | 11 | en activité | 540 | 15 285 → 37 396 → 54 276 → 73 129 → 104 392 → 122 225 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 0 |
| freelance | 23 | en activité | 540 | 19 194 → 35 981 → 53 014 → 77 199 → 103 460 → 133 309 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 0 |
| freelance | 37 | en activité | 540 | 16 582 → 45 337 → 53 343 → 73 323 → 94 249 → 111 604 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 0 |
| freelance | 41 | en activité | 540 | 19 878 → 28 730 → 49 326 → 65 969 → 76 337 → 112 967 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 0 |
| freelance | 59 | en activité | 540 | 18 275 → 34 735 → 56 314 → 81 217 → 106 375 → 107 005 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 0 |
| agence | 11 | liquidée | 376 | 17 141 → 19 946 → 25 951 → -1 576 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 4 230 |
| agence | 23 | en activité | 540 | 18 279 → 33 156 → 108 769 → 71 675 → -1 580 → 4 821 | 0 / 0 / 0 | Petit bureau, Flon | 5 | 9 950 |
| agence | 37 | en activité | 540 | 17 197 → 34 524 → 33 915 → 39 643 → 96 391 → 92 268 | 0 / 0 / 0 | Petit bureau, Flon | 5 | 9 950 |
| agence | 41 | en activité | 540 | 17 042 → 29 975 → 68 234 → 73 736 → 141 637 → 180 135 | 0 / 0 / 0 | Petit bureau, Flon | 5 | 9 950 |
| agence | 59 | en activité | 540 | 17 895 → 35 823 → 14 475 → 55 043 → 151 820 → 124 435 | 0 / 0 / 0 | Petit bureau, Flon | 5 | 9 950 |
| produit_tot | 11 | en activité | 540 | 6 360 → 1 795 → 5 803 → 4 405 → 3 336 → 2 143 | 333 / 148 / 0 | Ton appartement, Prilly | 0 | 0 |
| produit_tot | 23 | en activité | 540 | 7 035 → 2 018 → 3 555 → 2 282 → 1 880 → 1 414 | 357 / 158 / 0 | Ton appartement, Prilly | 0 | 0 |
| produit_tot | 37 | en activité | 540 | 4 522 → 692 → 3 174 → 2 991 → 3 061 → 2 808 | 363 / 161 / 0 | Ton appartement, Prilly | 0 | 0 |
| produit_tot | 41 | en activité | 540 | 5 311 → 1 432 → 938 → 1 813 → 2 829 → 3 673 | 365 / 162 / 0 | Ton appartement, Prilly | 0 | 0 |
| produit_tot | 59 | en activité | 540 | 4 885 → 434 → 1 566 → 1 298 → 1 341 → 1 111 | 350 / 155 / 0 | Ton appartement, Prilly | 0 | 0 |
| produit_soigne | 11 | liquidée | 406 | 16 931 → 40 811 → 36 186 → -1 249 | 248 / 122 / 12 | Petit bureau, Flon | 0 | 4 470 |
| produit_soigne | 23 | liquidée | 497 | 21 373 → 32 458 → 24 065 → 23 176 → -1 266 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 2 980 |
| produit_soigne | 37 | en activité | 540 | 18 439 → 25 751 → 21 713 → 15 511 → 36 149 → -1 088 | 242 / 108 / 73 | Ton appartement, Prilly | 0 | 2 980 |
| produit_soigne | 41 | en activité | 540 | 22 096 → 26 383 → 17 325 → 19 295 → 518 → -1 512 | 337 / 173 / 59 | Ton appartement, Prilly | 0 | 2 980 |
| produit_soigne | 59 | liquidée | 468 | 20 271 → 32 000 → 40 314 → 5 614 → -564 | 310 / 148 / 23 | Ton appartement, Prilly | 0 | 2 980 |
| croissance | 11 | liquidée | 162 | 3 380 → -774 | 750 / 0 / 16 | Petit bureau, Flon | 0 | 4 470 |
| croissance | 23 | liquidée | 162 | 12 211 → 2 470 | 673 / 0 / 12 | Petit bureau, Flon | 0 | 4 470 |
| croissance | 37 | liquidée | 162 | 10 994 → 318 | 847 / 0 / 7 | Petit bureau, Flon | 0 | 4 470 |
| croissance | 41 | liquidée | 162 | 7 997 → -1 546 | 709 / 0 / 6 | Petit bureau, Flon | 0 | 4 470 |
| croissance | 59 | liquidée | 162 | 7 849 → -553 | 629 / 0 / 18 | Petit bureau, Flon | 0 | 4 470 |
| embauche_rapide | 11 | en activité | 540 | 5 709 → 50 478 → 145 518 → 256 885 → 330 440 → 386 796 | 0 / 0 / 0 | Petit bureau, Flon | 6 | 9 180 |
| embauche_rapide | 23 | en activité | 540 | 783 → 69 614 → 58 496 → 4 621 → 36 274 → 18 281 | 0 / 0 / 0 | Petit bureau, Flon | 6 | 8 940 |
| embauche_rapide | 37 | liquidée | 192 | -1 264 → -1 863 | 0 / 0 / 0 | Petit bureau, Flon | 0 | 4 710 |
| embauche_rapide | 41 | en activité | 540 | -1 317 → 12 722 → 50 762 → 71 526 → 111 490 → 156 912 | 0 / 0 / 0 | Petit bureau, Flon | 6 | 8 940 |
| embauche_rapide | 59 | liquidée | 131 | 1 669 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 2 980 |
| marketing | 11 | liquidée | 253 | 16 931 → 5 860 → -1 768 | 450 / 5 / 7 | Petit bureau, Flon | 0 | 4 470 |
| marketing | 23 | liquidée | 253 | 19 883 → 2 234 → 446 | 327 / 3 / 18 | Petit bureau, Flon | 0 | 4 470 |
| marketing | 37 | liquidée | 223 | 16 949 → -1 433 | 333 / 3 / 15 | Petit bureau, Flon | 0 | 4 470 |
| marketing | 41 | liquidée | 237 | 20 606 → 199 | 369 / 4 / 6 | Petit bureau, Flon | 0 | 4 470 |
| marketing | 59 | liquidée | 207 | 18 781 → -360 | 269 / 3 / 8 | Petit bureau, Flon | 0 | 4 470 |
| sous_qualite | 11 | liquidée | 310 | 18 762 → 15 130 → -307 | 171 / 72 / 35 | Ton appartement, Prilly | 0 | 2 980 |
| sous_qualite | 23 | liquidée | 343 | 24 608 → 21 469 → -785 → 845 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 2 980 |
| sous_qualite | 37 | liquidée | 253 | 21 169 → 1 753 → 4 790 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 2 980 |
| sous_qualite | 41 | liquidée | 253 | 22 910 → -1 714 → 5 944 | 184 / 79 / 17 | Ton appartement, Prilly | 0 | 2 980 |
| sous_qualite | 59 | liquidée | 310 | 25 171 → 6 335 → -1 229 | 128 / 50 / 34 | Ton appartement, Prilly | 0 | 2 980 |
| mixte | 11 | en activité | 540 | 17 141 → 40 084 → 20 358 → 34 253 → 968 → 9 871 | 274 / 120 / 47 | Petit bureau, Flon | 0 | 8 460 |
| mixte | 23 | en activité | 540 | 18 279 → 25 142 → 49 069 → 27 713 → 741 → -1 614 | 623 / 340 / 0 | Petit bureau, Flon | 3 | 8 460 |
| mixte | 37 | en activité | 540 | 17 197 → 26 661 → 10 137 → 24 574 → 2 849 → 1 | 261 / 118 / 0 | Ton appartement, Prilly | 1 | 4 230 |
| mixte | 41 | liquidée | 468 | 17 042 → 33 611 → 13 586 → 11 963 → -1 582 | 0 / 0 / 0 | Ton appartement, Prilly | 0 | 4 230 |
| mixte | 59 | en activité | 540 | 17 895 → 27 209 → 8 041 → 8 644 → 46 261 → 7 803 | 425 / 223 / 0 | Petit bureau, Flon | 4 | 8 460 |
