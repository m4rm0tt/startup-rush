# Équilibrage de la finition : causes, réglages et résultats

Protocole : `Tests/balance/BalanceReportTest.cs`, mêmes graines partout (11, 23, 37, 41, 59 ; 15 de plus pour la dispersion),
fondateur polyvalent, bots de `Core/bots/Autopilot.cs`. Chaque groupe de réglages a été mesuré seul, dans l'ordre, sur tout le
bilan (`runs_<groupe>.csv`, une ligne par partie). Référence : commit `0dfda9a` (`reference_avant.md`, `runs_finition_avant.csv`,
et `runs_reference_w20.csv` : 20 graines rejouées sur le code de la référence, extrait par `git archive`). Résultat final :
`BALANCE_REPORT.md` et `runs_finition_final.csv`.

Les bots ne mesurent pas le plaisir de jeu ; ils montrent qu'une façon de jouer raisonnable tient ou ne tient pas, et pourquoi.

## Résultat : avant / après

### 18 mois, 20 graines (mêmes graines, code de la référence contre code final)

| Stratégie | Faillites avant | Médiane avant (P10 – P90) | Faillites après | Médiane après (P10 – P90) |
|---|---|---|---|---|
| un_projet | 0/20 | 33 518 (26 618 – 41 524) | 0/20 | 43 277 (34 330 – 58 808) |
| plusieurs_petits | 0/20 | **200 771** (116 043 – 226 994) | 0/20 | 55 740 (42 713 – 75 627) |
| jeu_simple | 0/20 | 18 866 (9 835 – 28 786) | 0/20 | 24 871 (18 416 – 34 408) |
| delegation | 0/20 | 77 333 (14 703 – 136 962) | 0/20 | 17 732 (11 282 – 22 213) |
| marketing_excessif | 15/20 | 200 (−1 216 – 37 337) | 18/20 | −342 (−1 549 – 1 027) |
| exces_puis_correction | 0/20 | 38 027 (32 770 – 59 351) | 0/20 | 76 954 (60 505 – 91 211) |
| materiel | 0/20 | 13 168 (918 – 35 488) | 0/20 | 27 829 (11 699 – 31 600) |
| ia | 0/20 | 154 005 (104 773 – 241 735) | 0/20 | 57 922 (37 928 – 76 467) |
| studio_xr | 0/20 | 39 175 (25 127 – 52 814) | 0/20 | 33 852 (24 708 – 46 609) |
| hebergement_maison | 0/20 | 96 116 (33 544 – 149 206) | 0/20 | 21 120 (14 592 – 31 359) |

### Diversifications sur 36 mois et quantique sur 60 mois (5 graines)

| Stratégie | Faillites | Médiane (min – max) | Revenus / mois à 36 mois | Rapport à `plusieurs_petits` |
|---|---|---|---|---|
| plusieurs_petits | 0/5 | 398 497 (340 861 – 431 717) | 24 739 | base |
| materiel_pc | 0/5 | 371 105 (277 237 – 486 631) | 68 304 | −7 % |
| materiel_industrie | 0/5 | 326 254 (113 067 – 436 826) | 50 268 | −18 % |
| materiel_gpu | 0/5 | 318 661 (256 439 – 408 265) | 49 546 | −20 % |
| hebergement_maison | 0/5 | 308 825 (189 293 – 407 821) | 30 027 | −22 % |
| ia | 0/5 | 302 620 (243 867 – 362 018) | 24 604 | −24 % |
| materiel_serveur | 0/5 | 233 711 (169 155 – 285 941) | 48 806 | −41 % |
| studio_xr | 0/5 | 174 593 (133 679 – 200 574) | 17 904 | −56 % |
| ia_salle_serveur | 0/5 | 150 240 (119 655 – 227 733) | 27 064 | −62 % |
| materiel (cartes makers) | 1/5 | 38 808 (3 435 – 80 610) | 27 771 | −90 % |
| quantique (60 mois) | 0/5 | 171 505 ; **en service 5/5** (jours 1 093 à 1 642) | — | — |

Avant : `materiel_industrie`, `materiel_pc` et `materiel_serveur` faisaient 2 à 4 faillites sur 5 à 36 mois au moment où ils ont
été mesurés pour la première fois (groupes C et D2), `materiel` 5/5 ; le quantique n'était en service dans aucune des parties
suivies à 36 mois, et dans 3 graines sur 5 à 60 mois après le groupe G.

Lecture : plus aucune stratégie ne domine (à 18 mois, de 25 k pour `jeu_simple` à 77 k pour `exces_puis_correction`, contre un
rapport de 1 à 11 avant) ; les diversifications sont des **investissements** : elles coûtent à 18 mois et rapportent ensuite
(revenus mensuels à 36 mois égaux ou supérieurs à la base, sauf le studio XR). Le matériel (PC, GPU, industrie) est la voie la
plus rentable à long terme, au prix d'un creux de trésorerie (pire creux proche de zéro).

## Groupes de réglages, dans l'ordre, et leur effet mesuré

Médianes sur les 5 graines du bilan (k = milliers de CHF ; « f » = faillites).

| Stratégie (mois) | réf. | A | B | C | D2 | D3 | E | F | G | H | I | J | K | final |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| un_projet (18) | 22 k | 61 k | 50 k | 50 k | 50 k | 50 k | 50 k | 50 k | 50 k | 50 k | 50 k | 50 k | 50 k | 50 k |
| plusieurs_petits (18) | 172 k | 186 k | 132 k | 52 k | 52 k | 52 k | 52 k | 52 k | 52 k | 52 k | 52 k | 52 k | 52 k | 52 k |
| jeu_simple (18) | 3 k | 10 k | 25 k | 25 k | 25 k | 25 k | 25 k | 25 k | 25 k | 25 k | 25 k | 25 k | 25 k | 25 k |
| delegation (18) | 122 k | 122 k | 16 k | 20 k | 20 k | 20 k | 20 k | 20 k | 20 k | 20 k | 20 k | 20 k | 20 k | 20 k |
| marketing_excessif (18) | 1 k (2f) | 60 k (2f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) | 0 (5f) |
| materiel (18) | 11 k | 21 k | 6 k | 2 k | 12 k | 10 k | 10 k | 10 k | 10 k | 3 k | 2 k | 17 k | 17 k | 17 k |
| materiel (36) | — | — | — | 0 (5f) | 0 (5f) | −1 k (5f) | −1 k (5f) | −1 k (5f) | −1 k (5f) | −1 k (5f) | −1 k (3f) | 38 k (1f) | 38 k (1f) | 39 k (1f) |
| materiel_industrie (36) | — | — | — | — | 11 k (2f) | 4 k (4f) | 4 k (4f) | 4 k (4f) | 4 k (4f) | 96 k | 326 k | 326 k | 326 k | 326 k |
| materiel_pc (36) | — | — | — | 0 (4f) | 204 k | 229 k | 229 k | 229 k | 229 k | 271 k | 371 k | 371 k | 371 k | 371 k |
| hebergement_maison (36) | — | — | — | 360 k | 360 k | 360 k | 360 k | 309 k | 309 k | 309 k | 309 k | 309 k | 309 k | 309 k |
| ia (36) | — | — | — | 303 k | 303 k | 303 k | 303 k | 303 k | 303 k | 303 k | 303 k | 303 k | 303 k | 303 k |
| ia_salle_serveur (36) | — | — | — | 181 k | 181 k | 181 k | 181 k | 150 k | 150 k | 150 k | 150 k | 150 k | 150 k | 150 k |
| studio_xr (36) | — | — | — | 81 k | 81 k | 81 k | 109 k | 158 k | 158 k | 158 k | 158 k | 7 k (1f) | 175 k | 175 k |
| quantique (60) | — | — | — | — | — | — | — | — | 390 k | 390 k | 172 k | 172 k | 172 k | 172 k |

(« — » : scénario ou horizon pas encore mesuré à ce stade. Les scénarios `un_projet_pme`, `materiel_*` et `ia_salle_serveur`
ont été ajoutés au premier groupe pour isoler un segment, chaque famille de matériel et l'IA sur ses propres GPU.)

- **A. Défauts des bots** (`Autopilot`) : au-delà de 12 bugs, plus de nouvelle fonction (le temps libre va à la maintenance,
  comme un joueur qui lit « plein de bugs ») ; un composant matériel qui ferait passer les matières au-delà de 60 % du prix du
  public n'est plus ajouté. Effet : `un_projet` 22 → 61 k, `jeu_simple` 3 → 10 k. C'était le bot, pas le jeu.
- **B. Largeur du public** (`Market.SegmentBreadth`, `Tuning.Products`) : présence naturelle et marketing multipliés par
  √(taille / 100 000), bornés à 0,35–2. **Cause prouvée de la domination de `plusieurs_petits`** : chaque public recevait le même
  nombre de curieux par jour quelle que soit sa taille ; l'application pour 30 000 PME (39 CHF) se remplissait aussi vite qu'un jeu
  pour 400 000 joueurs (`un_projet_pme` : 503 k en 18 mois au groupe A). Effet : `plusieurs_petits` 186 → 132 k,
  `jeu_simple` 10 → 25 k (le grand public des jeux reçoit ×2), `delegation` 122 → 16 k (elle vivait de la même application PME).
- **C. Vitesse de décision** (`Segment.Adoption`) : PME 0,7, makers 1,3. Effet : `plusieurs_petits` 132 → 52 k, `ia` (analystes en
  PME) 116 → 58 k à 18 mois ; à 36 mois, `ia` reste à 303 k.
- **D2. Matériel** (`Industry.cs`, `ProductCatalog.cs`, `RealEstate.cs`, bot) : carte achetée 28 → 20 CHF, processeur 90 → 160,
  mémoire 25 → 45, GPU 220 → 300, boîtier 30 → 50 (les PC se vendaient presque au coût des matières d'une carte) ; auto-hébergement
  900 → 1 300 CHF ; atelier 3 400 → 2 600 CHF/mois ; le bot suit la chaîne de recherches (cartes → PC → serveurs ; cartes →
  série → GPU), fixe un prix d'au moins 1,4 × les matières et un plafond d'achats qui suit le coût unitaire. Effet : PC à 36 mois
  0 (4f) → 204 k.
- **D3. Réserves** (bot) : les réserves suivent la demande (deux semaines de ventes), une étagère de plus si la place manque.
- **E. XR** (`ProductCatalog.cs`, `Market.HasLaunchHype`) : une application XR vendue à l'unité se lance comme un jeu (liste de
  souhaits, pic de sortie) ; essai 0,05 → 0,07, bouche-à-oreille 1,8 → 2,2, joueurs XR 25 → 35 CHF. **Cause du plafond de
  300 utilisateurs** : une application à l'achat sans lancement attendu ne vendait qu'aux curieux du jour. Effet : `studio_xr`
  à 36 mois 81 → 109 k.
- **F. Salle serveur** (bot) : une baie de plus quand une offre cloud est saturée, serveur GPU pour `ia_salle_serveur`. Effet :
  revenus mensuels plus hauts (30 k contre 25 k à 36 mois), trésorerie plus basse (309 k contre 360 k) : l'argent est investi.
  `studio_xr` gagne aussi (109 → 158 k) sans cause isolée dans ce groupe (non prouvée).
- **G. Quantique** (`Furniture.cs`, `Infrastructure.cs`, `RealEstate.cs`, bot) : cryostat fermé 380 000 → 150 000 CHF, ouvert
  120 000 → 80 000, baie de contrôle 35 000 → 25 000, support 60 000 → 40 000, 20 → 30 unités vendables, laboratoire 9 500 →
  7 000 CHF/mois ; le bot entre au laboratoire dès 230 000 CHF et finance le cryostat par un prêt bancaire s'il comble l'écart.
  **Cause de l'inaccessibilité** : avec 95 000 CHF de supports et 380 000 de cryostat, le loyer du laboratoire (114 000 CHF par an)
  vidait la trésorerie avant d'y arriver. Effet : en service 3/5 à 60 mois.
- **H. Matériel renouvelé, cartes en panneau, prix en rupture** (`Market.cs`, `ProductCatalog.cs`, `Industry.cs`, bot) : les
  possesseurs rachètent (cartes 240 jours dans ce groupe, PC 1 100…), une carte se monte par panneau de deux, le bot monte son prix
  par paliers de 5 % quand il perd plus d'un quart de ses ventes. **Cause des faillites de la carte industrie** : sans rachat, un
  public de 25 000 s'épuisait en un an (ventes 106 → 65 par mois). Effet : `materiel_industrie` 4 k (4f) → 96 k.
- **I. Métier d'opérateur** (`Recruitment.cs`, bot) : un opérateur est payé 2 900 + 40 × niveau (≈ 4 900 CHF), pas comme un
  développeur (≈ 7 300) ; le bot embauche un opérateur pour la ligne au lieu d'y mettre son développeur. Cartes makers : rachat
  tous les 120 jours (écoles et makers rachètent à chaque projet). **Cause du « loyer non couvert »** : la carte à 65 CHF rapportait
  27 CHF par unité ; la ligne prenait le seul développeur (9 000 CHF/mois), le logiciel déclinait et le salaire mangeait la marge.
  Effet : PC 271 → 371 k, industrie 96 → 326 k, cartes makers 5f → 3f. Le quantique à 60 mois passe de 390 à 172 k mais 5/5 en
  service au lieu de 3/5 : le laboratoire est atteint plus tôt (jours 840–1 054 au lieu de 982–1 148), donc payé plus longtemps.
- **J. Entrée des cartes makers à 50 000 CHF** (stratégie `materiel`) : la ligne devient bénéficiaire vers son 5ᵉ mois, mais à
  35 000 CHF le bot y arrivait à découvert (graine 23 : trésorerie à zéro au 16ᵉ mois, au moment où la ligne gagnait 2 000 à
  2 500 CHF par mois). Effet : 3f → 1f à 36 mois. Essayé dans le même groupe : une seconde embauche pour la zone XR → `studio_xr`
  158 k → 7 k (1f) : un salaire de plus et un déménagement pour une application XR à 4 500 CHF par mois au mieux. Abandonné en K.
- **K. Formation plutôt qu'embauche** (bot) : la compétence qui manque au studio ou à la zone XR (Design / Tests 45) s'obtient par
  une formation (900 CHF, deux jours ; le fondateur polyvalent passe de 42 à 46 en tests). Zone XR complète 1/5 → 5/5, `studio_xr`
  158 → 175 k à 36 mois.

## Points demandés

- **Studio/XR** : voie viable (0 faillite sur 20 graines à 18 mois, 0/5 à 36 mois, studio et zone XR complets 5/5), moins
  rentable que la base (−56 % à 36 mois). Contrepartie identifiée : l'application XR (achat unique, 60 000 joueurs équipés) culmine
  vers 4 500 CHF par mois puis décroît, et son développement retarde l'application PME qui fait l'essentiel des revenus (à 24 mois,
  450 utilisateurs au lieu de 1 000 avec les mêmes composants : un public étroit consommé par une version faible ne revient pas).
  Le studio sert surtout les jeux et le marketing (+30 % de contenu, +15 % de portée au plus).
- **Quantique** : progression tardive atteignable, jamais par accident : en service dans les 5 graines entre le 36ᵉ et le 55ᵉ mois,
  jamais avant le 30ᵉ ; le laboratoire coûte cher tant que le cryostat fermé n'est pas en service (creux visibles dans le tableau
  graine par graine du bilan).
- **`marketing_excessif`** : puni par la bonne mécanique. 6 000 CHF par mois de marketing pour une application à 12 CHF dont
  le public (80 000 indépendants, largeur 0,89) ne fournit pas assez de curieux : environ 7 800 CHF de revenus mensuels contre
  6 000 de marketing et 2 160 de charges fixes. 18/20 faillites ; la même erreur corrigée à temps (`exces_puis_correction`) : 0/20.
  Le passage de 2/5 à 4/5 au lot 6 venait d'un décalage du hasard (`enquete_marketing_excessif.md`), corrigé ; le passage à 5/5
  vient du groupe B (largeur du public), une règle voulue.
- **Matériel** : chaque famille a maintenant une économie identifiable (table ci-dessous). PC, serveurs et GPU sont couverts par
  des bots (`materiel_pc`, `materiel_serveur`, `materiel_gpu`).
- **Salle serveur** : réévaluée avec les besoins de calcul. Elle paie par les **offres cloud** (3 400 à 6 600 CHF par mois dans
  la sonde, pour 1 800 de loyer et 1 100 à 1 650 d'électricité) et l'hébergement des projets ; pas par l'IA d'un petit modèle :
  entraîner le petit modèle en location coûte environ 300 CHF une fois, et l'inférence louée 100 à 170 CHF par mois, alors qu'un
  serveur GPU coûte 14 000 CHF (amorti en plus de 80 mois). Elle devient utile à l'IA avec les grands modèles (9 000 points
  d'entraînement : 60 jours et 4 500 CHF en location, moins de 5 nuits sur un serveur GPU) et une forte inférence. La recherche
  n'utilise pas la salle serveur (le calcul avancé du laboratoire est une armoire à part). Constat : à 36 mois, la salle serveur
  coûte 22 % de trésorerie mais rapporte 21 % de revenus mensuels de plus que la base ; `ia_salle_serveur` montre qu'un serveur
  GPU acheté pour un petit modèle est un mauvais investissement (le coût de location est affiché dans le jeu).
- **`plusieurs_petits` et `jeu_simple`** : la domination venait du segment PME (groupes B et C) ; `plusieurs_petits` reste la base
  la plus sûre à 36 mois, sans écraser les autres à 18 mois. `jeu_simple` est viable : 25 k en 18 mois, 0/20 faillites.
- **`delegation`** : viable (0/20), la moins rentable des stratégies sans erreur (18 k) ; c'est la contrepartie de jouer sans
  taper (43 décisions et 8 000 mots contre 60 et 11 500) : deux salaires et le fondateur qui ne développe plus.

## Utilité économique de chaque activité

| Activité | Ce qu'elle rapporte | Contraintes | Rythme mesuré |
|---|---|---|---|
| Applications et jeux | Abonnements, ventes, publicité | Public (taille, prix, vitesse de décision), bugs, support | 1re sortie au jour 22, bénéficiaire vers le 4ᵉ mois |
| Délégation | Moins d'effort au terminal | Salaires, équipe moins rapide que le fondateur | 18 k à 18 mois |
| Salle serveur | Offres cloud, hébergement interne | Loyer, électricité, froid, réseau ; payée en 2 à 3 ans | −22 % de trésorerie, +21 % de revenus à 36 mois |
| IA | Produit d'IA (analystes, créatifs, API) | Qualité des données, taille du modèle, inférence | publiée vers le jour 312 |
| Cartes makers | Première série, chaîne de recherche | Marge de 30 CHF : il faut ≈ 180 ventes par mois pour l'atelier et un opérateur | bénéficiaire vers le 5ᵉ mois de ligne |
| Cartes industrie | Marge de 130 CHF, public de 25 000 qui rachète | Industrialisation | 326 k à 36 mois |
| PC, serveurs, GPU | Grosses marges unitaires | Matières chères, chaîne de recherches, creux de trésorerie | 233 à 371 k à 36 mois |
| Studio | +30 % de contenu, +15 % de portée | Designer (Design 45) sur le site | posé vers le jour 220 |
| Zone XR | −60 % de défauts XR | Testeur·se (Tests 45) sur le site | complète deux jours après sa pose (formation) |
| Quantique | 30 unités de calcul vendables | Laboratoire, cryostat 150 000 CHF, froid et énergie | en service entre 36 et 55 mois |

## Limites

- 5 graines pour les tableaux à 36 et 60 mois, 20 pour la dispersion à 18 mois ; une faillite isolée (cartes makers, graine 59)
  ne fait pas un taux.
- Les bots appliquent des règles simples (une amélioration à la fois, seuils de trésorerie fixes) ; un joueur fera mieux ou moins
  bien. Le plaisir de jeu n'est pas mesuré.
