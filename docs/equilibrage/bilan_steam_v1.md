# Équilibrage des activités : bilan du lot 4 de la version Steam (3 novembre 2026)

## Protocole

- Sonde `Tests/balance/SteamBalanceProbe.cs` (tests `Explicit`, lancés à la main), qui réutilise `BalanceReportTest.Play` : les
  bots de `Core/bots/Autopilot.cs` jouent chaque stratégie avec les commandes de l'interface, une décision par jour ouvré,
  fondateur polyvalent.
- `Paths18Months` : 12 parcours × 3 graines (11, 23, 37) × 18 mois. `MakersCard36Months` : la carte d'entrée de gamme
  (stratégie `materiel`) sur 36 mois, mêmes graines, avec l'économie de la carte en fin de partie.
- Pas de rapport complet (`WriteBalanceReport`, ~45 minutes sur le VPS) dans ce lot : faute de temps, 3 graines au lieu de 5.
  Les chiffres de `BALANCE_REPORT.md` (5 graines, 36 mois) servent de contrôle, pas de vérité.

## Résultats : les parcours sur 18 mois (avant changement)

Graines 11, 23, 37 · 18 mois · fondateur polyvalent

| Parcours | Faillites | Argent final méd. | min – max | 1re sortie (j) | 1er mois bénéficiaire (j) | Revenus projets / mois méd. | Argent par trimestre (graine 11) |
|---|---|---|---|---|---|---|---|
| un_projet | 0/3 | 99 837 | 65 378 – 111 932 | 8 | 150 (3/3) | 38 095 | 12 473 → 16 635 → 31 625 → 36 327 → 40 647 → 65 378 |
| plusieurs_petits | 0/3 | 71 030 | 68 405 – 77 855 | 8 | 120 (3/3) | 25 571 | 12 614 → 16 445 → 22 336 → 31 629 → 37 842 → 68 405 |
| studio_xr | 0/3 | 60 229 | 48 437 – 76 310 | 8 | 120 (3/3) | 21 733 | 12 614 → 16 834 → 24 076 → 30 478 → 45 132 → 76 310 |
| materiel | 0/3 | 14 589 | 11 600 – 29 826 | 8 | 120 (3/3) | 23 318 | 12 614 → 16 840 → 24 274 → 29 672 → 32 716 → 11 600 |
| materiel_industrie | 0/3 | 33 526 | 26 894 – 34 155 | 8 | 120 (3/3) | 45 097 | 12 614 → 16 840 → 24 274 → 6 468 → 6 866 → 34 155 |
| materiel_pc | 0/3 | 78 764 | 57 446 – 130 625 | 8 | 120 (3/3) | 109 974 | 12 614 → 16 840 → 24 274 → 3 668 → 47 429 → 130 625 |
| materiel_gpu | 0/3 | 56 988 | 39 453 – 57 017 | 8 | 120 (3/3) | 23 309 | 12 614 → 16 789 → 23 299 → 31 268 → 33 884 → 57 017 |
| ia | 0/3 | 62 358 | 54 642 – 62 783 | 8 | 120 (3/3) | 21 996 | 12 623 → 17 773 → 27 110 → 28 979 → 38 030 → 62 783 |
| ia_salle_serveur | 0/3 | 21 233 | 14 465 – 28 245 | 8 | 120 (3/3) | 23 588 | 12 623 → 17 773 → 26 210 → 27 384 → 13 509 → 28 245 |
| quantique | 0/3 | 64 564 | 34 817 – 70 330 | 8 | 120 (3/3) | 27 297 | 12 614 → 16 789 → 23 380 → 32 251 → 37 330 → 70 330 |
| marketing_excessif | 2/3 | 58 | -1 450 – 100 145 | 8 | 150 (3/3) | 7 357 | 3 703 → -1 450 |
| exces_puis_correction | 0/3 | 129 206 | 122 244 – 131 187 | 8 | 120 (3/3) | 29 219 | 3 574 → 8 192 → 27 774 → 40 395 → 74 606 → 131 187 |

Lecture : aucune faillite dans les parcours joués correctement, sur aucune graine ; le marketing intensif fait faillite 2 fois
sur 3 et sa correction à temps sauve la partie (129 206 CHF). Sur 18 mois, le matériel d'entrée de gamme (`materiel`,
14 589 CHF) et la salle serveur pour l'IA (`ia_salle_serveur`, 21 233) finissent bien en dessous du logiciel seul : ce sont
des investissements. La salle serveur se rattrape en partie sur 36 mois (212 906 contre 355 631 pour `ia`, rapport du
3 novembre) : plus lente, pas une impasse. Le quantique ne paie qu'après 3 à 5 ans (rapport du 3 novembre, 60 mois). La
carte pour makers, elle, ne se rattrape jamais (ci-dessous).

## L'impasse démontrée : la carte pour makers

Constat, cohérent sur toutes les graines :
- `BALANCE_REPORT.md` du 3 novembre, 36 mois, 5 graines sur 5 : `materiel` finit à 37 380 CHF (médiane) contre 449 411 pour
  `plusieurs_petits`, avec moins de revenus par mois (30 067 contre 38 980). Les autres matériels (industrie, PC, serveurs,
  GPU) se rattrapent sur 36 mois ; la carte pour makers, jamais.
- Sonde mois par mois (graine 11) : la carte se vend 69 à 72 CHF pour un coût unitaire de 35 à 38 CHF, soit ~31 CHF de
  marge, et la ligne en écoule ~126 par mois : ~3 900 CHF de marge par mois, moins que le loyer de l'atelier (2 600 CHF) plus
  le salaire d'un opérateur. La carte ne rembourse pas son atelier (ou à peine) : l'argent de ce parcours vient du logiciel.

Changement (un seul) : prix de référence du public « Makers » 65 → 95 CHF (`Core/products/ProductCatalog.cs`). La sensibilité
au prix, la taille du marché et l'adoption ne changent pas ; la carte reste de loin la moins chère du jeu (industrie 220 CHF,
PC 750 à 1 100 CHF). À prix relatif égal, la demande est la même et la marge par carte passe d'environ 31 à environ 57 CHF.

Mesure avant/après, `MakersCard36Months` (stratégie `materiel`, 36 mois, mêmes graines, même code des bots) :

Avant (65 CHF) :

| Graine | Issue | Argent final | Revenus / mois | La carte en fin de partie |
|---|---|---|---|---|
| 11 | en activité | 18 545 | 28 967 | prix 75 · coût 38 · vendues 30 j 156 · fabriquées 3026 · en ligne True |
| 23 | en activité | 61 920 | 27 351 | prix 65 · coût 38 · vendues 30 j 161 · fabriquées 2674 · en ligne True |
| 37 | en activité | 73 070 | 30 775 | prix 91 · coût 38 · vendues 30 j 166 · fabriquées 2294 · en ligne True |

Après (95 CHF) :

| Graine | Issue | Argent final | Revenus / mois | La carte en fin de partie |
|---|---|---|---|---|
| 11 | en activité | 107 034 | 33 719 | prix 105 · coût 38 · vendues 30 j 158 · fabriquées 3034 · en ligne True |
| 23 | en activité | 72 210 | 31 952 | prix 95 · coût 38 · vendues 30 j 160 · fabriquées 2674 · en ligne True |
| 37 | en activité | 84 561 | 37 550 | prix 133 · coût 38 · vendues 30 j 166 · fabriquées 2288 · en ligne True |

Résultat : les trois graines s'améliorent (médiane 61 920 → 84 561 CHF ; 18 545 → 107 034, 61 920 → 72 210,
73 070 → 84 561), pour le même volume (158 à 166 cartes par mois) : seule la marge change. La carte reste un parcours
modeste, très en dessous du logiciel seul sur 36 mois (449 411 CHF au rapport du 3 novembre) : elle devient viable sans
devenir la meilleure voie.

## Contrôle : les 12 parcours sur 18 mois après le changement

Mêmes graines, même durée (`Paths18Months`) : **seul `materiel` change** (médiane 14 589 → 16 103 CHF ; graine 11 :
11 600 → 16 103) ; les onze autres parcours donnent exactement les mêmes chiffres qu'avant (Studio/XR et quantique ne vendent
pas encore la carte pour makers dans les 18 premiers mois). Sur 18 mois, l'atelier vient d'ouvrir (mois 16 à 17 pour la
graine 11) : le gain se voit surtout sur 36 mois (ci-dessus). Tests du moteur : 247/247.

## Risques et avantages de chaque activité (pour le joueur)

- **Logiciel seul** (un ou plusieurs projets) : la voie la plus sûre et la plus rentable sur la durée ; peu d'investissement,
  mais il faut taper et suivre les avis.
- **Studio et XR** : un investissement de matériel et de formation qui améliore le contenu et les applications XR ; rentable
  plus lentement que le logiciel seul.
- **Carte pour makers** : l'entrée la moins chère dans le matériel, de gros volumes à petite marge ; l'atelier et l'opérateur
  coûtent tous les mois, la carte met des mois à les couvrir.
- **Carte pour l'industrie** : moins de clients mais une marge large ; se rattrape sur 36 mois.
- **PC** : le matériel le plus rentable mesuré (au niveau du logiciel seul sur 36 mois), mais une chaîne plus chère et des
  creux de trésorerie.
- **GPU** : demande l'industrialisation (plus de capital) ; rentable plus tard.
- **IA** : un modèle loué puis publié rapporte vite si un produit l'utilise ; la salle serveur dédiée coûte plus qu'elle ne
  rapporte sur 18 mois.
- **Quantique** : la fin de progression, très chère ; ne paie qu'après 3 à 5 ans (60 mois : 0,48 à 1,06 million CHF selon les graines, rapport du 3 novembre).
- **Marketing intensif** : 6 000 CHF par mois sur une petite application mène à la faillite ; corriger à temps sauve la partie.

## Hypothèses, compromis, limites

- 3 graines seulement, et des bots qui ne jouent pas comme des joueurs (pas d'ajustement fin des prix, investissement aux
  seuils fixes de `Strategies.cs`). Un changement n'est retenu que s'il est cohérent sur toutes les graines mesurées.
- Seul le public « Makers » est touché ; les stratégies qui ne l'utilisent pas ne changent pas (même code, mêmes graines).
- Compromis : une carte à 95 CHF est moins « petit prix » qu'à 65, mais 95 CHF reste un prix de kit d'entrée réaliste et la
  carte devient une vraie première marche vers le matériel au lieu d'un piège.
- Les bots prennent le public « Makers » par défaut pour le matériel (`Autopilot.HardwareSegment`) : Studio/XR et
  quantique y passeront aussi sur une partie plus longue. Pas d'effet mesuré sur 18 mois.
- À refaire : le rapport complet (`WriteBalanceReport`) sur 5 graines, et une partie humaine du parcours matériel.
