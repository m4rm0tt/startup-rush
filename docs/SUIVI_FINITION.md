# Suivi de finition (après le lot 7)

Suivi unique de la demande « version complète, cohérente, testée et prête à distribuer ». Une ligne par point : état constaté au
départ (vérifié dans le code le 30.10), résultat attendu, fichiers, validation, blocage. Les états évoluent au fil des lots.

Légende : **déjà fait** (constaté avant ce travail) · **à faire** · **en cours** · **fait** (ce travail, avec sa preuve) ·
**non applicable** (justifié) · **à vérifier à la main** (impossible ici, protocole dans `VALIDATION.md`).

Référence de départ : commit `0dfda9a`, 173 tests verts, `BALANCE_REPORT.md` du lot 7 (14 stratégies × 5 graines, 18 mois)
conservé dans `docs/equilibrage/reference_avant.md`.

## 2. Équilibrage et économie

| Point | État constaté | Attendu | Fichiers | Validation | Blocage |
|---|---|---|---|---|---|
| Référence bots, graines fixes | à faire | bilan reproduit et archivé avant tout réglage | `Tests/balance/*`, `docs/equilibrage/` | même chiffres qu'au lot 7 | — |
| Studio/XR faible (38 k vs 198 k) | constaté au lot 6 | voie viable | `Campaign.Lab.cs`, `ProductCatalog.cs`, bots | bilan avant/après, 5 graines | — |
| Quantique jamais atteint (36 mois) | constaté au lot 6 | atteignable tard dans une campagne longue bien gérée | `Furniture.cs`, `Campaign.Lab.cs`, bot `quantique` | horizon 36 mois, jalon « en service » | — |
| `marketing_excessif` 4/5 faillites (2/5 au lot 5) | cause non isolée | cause prouvée par l'historique ou dite non prouvée | historique git, `Market.cs` | rejouer le bot aux commits du lot 5 et du lot 6 | — |
| Carte à bas prix vs loyer d'atelier | constaté au lot 5 | matériel avec une utilité économique claire | `Industry.cs`, `ProductCatalog.cs` | bots matériel | — |
| PC, serveurs, GPU sans bot | constaté | scénarios bots | `Core/bots/*` | bilan | — |
| Salle serveur (≈ 78–86 k vs 192–198 k) | constaté au lot 4 | réévaluée avec les besoins de calcul de l'IA et de la recherche | `Campaign.Infra.cs`, `Campaign.Ai.cs` | bilan | — |
| `plusieurs_petits` domine, `jeu_simple` vivote | constaté | domination avec contrepartie, `jeu_simple` viable | `Market.cs`, `ProductCatalog.cs` | bilan, dispersion | — |

## 3. Fonctions prévues par la spécification

| Point | État constaté | Attendu | Fichiers | Validation | Blocage |
|---|---|---|---|---|---|
| Préparation de recherche au terminal | absent (spec §4.2) | le fondateur contribue à une recherche par le terminal, budget distribué, borné | `Campaign.Terminal.cs`, `Campaign.Research.cs`, `TerminalView.cs` | tests ciblés | — |
| Transferts de stock entre sites | absent | transfert explicite avec délai et coût, sans duplication ni perte ; plus de prélèvement involontaire sur un autre site | `Campaign.Industry.cs`, `IndustryPanel.cs` | invariants de stock | — |
| Studio : « quelqu'un sur le site » | constaté (`Campaign.Lab.cs` `Staffed`) | une affectation de métier (personne affectée au studio, compétence design) | `Campaign.Lab.cs`, `Person.cs` | tests | — |
| Opérateurs assis aux postes | bulle près d'une station | personnages assis aux chaises des stations, présence = activité réelle | `SceneCanvas.cs`, `RoomRenderer.cs`, `Campaign.Industry.cs` | captures | — |
| Graphe de dépendances, `comp_api`, `comp_storage`, `comp_game_menu` | absent (96 non implémentés dont `link_*`, `port_*`) | graphe affiché dans l'atelier, trois composants testés | `ProductCatalog.cs`, `ProjectPanel.cs`, `ComponentArt.cs` | tests + captures | — |
| Composants de jeu de la spec (§6.2) | partiel | liste « moteur, menu, progression, sauvegarde, inventaire, classement, multijoueur simulé » couverte, sans doublon | `ProductCatalog.cs` | tests | — |
| Métiers opérateur et chercheur | absent (4 disciplines) | métiers à part entière (recrutement, compétences, effets) | `Person.cs`, `Recruitment.cs`, `Campaign.*` | tests + migration | — |
| Avis reliés aux causes (spec §8 : prix, rupture, performances, défauts, délai ; services : lenteur, manque de fonctions, support ; IA : qualité des réponses, délai/coût) | partiel (pas de rupture/défauts/qualité IA) | causes calculées avec levier | `Reviews.cs` | tests | — |
| Résultat par site | absent | détail par site réconcilié avec le total | `Ledger.cs`, `FinancePanel.cs` | invariant de somme | — |
| Saisie simplifiée du terminal | **déjà fait** (lot 3 : réglage, `TerminalView`) | vérifier qu'elle marche encore avec le terminal habillé du lot 7 | `TerminalView.cs` | test + capture | — |
| Recoloration des tenues | absent (canaux primary/secondary/accent dans le pack) | tenue au choix à la création, sauvegardée | `Person.cs`, `RoomRenderer.cs`, `Portraits.cs` | capture + sauvegarde | — |
| Estompage des objets hauts | absent (26 objets avec `occlusion` dans le manifeste) | objets hauts estompés quand ils cachent une personne ou le survol | `RoomRenderer.cs`, `SceneCanvas.cs` | capture | — |
| Câblage et sécurité | scores par site | **simplification assumée** (pas de fil à fil, pas de zones) : documentée | `DESIGN_SIMULATION.md` | — | — |

## 4. Couverture des assets

| Point | État constaté | Attendu | Validation |
|---|---|---|---|
| Registre | 141 validés, 349 partiels, 96 non implémentés (vérifié dans `docs/asset_usage.csv`) | chaque ligne justifiée : validé, partiel avec raison, ou non applicable | `AssetRegistryTest` + scénarios |
| Six modules de pièce | non implémentés | posables comme lots, prix et effets comptés | tests |
| `moving_box` | non implémenté | les cartons du déménagement visibles et ouvrables | capture + test |
| `wall_corner_outer`, `wall_opening`, `floor_marking`, `floor_threshold` | non implémentés | usage réel ou non applicable justifié | — |
| HUD restants (`hud_dropdown`, `hud_dropdown_list`, `hud_field_numeric`, `hud_stepper_up/down`, `hud_field_multiline`, `hud_field_search`, `hud_menu_panel/item/separator`, `hud_gauge_*`, `hud_banner`, `hud_chip_dark`, `hud_separator_h/v`, `hud_panel_secondary`, `hud_card`, `hud_chart_bar`, `hud_chart_grid_v`, défilement horizontal, `hud_progress_fill_striped`, `hud_titlebar`, `hud_theme_papier`) | non implémentés | branchés là où ils servent | captures |
| Icônes sans usage (dont `icon_gpu`, `icon_server`, `icon_training`, 27 autres) | non implémentées ou mappées sans affichage | affichées là où elles servent | captures |
| `cursor_busy`, `cursor_resize_v/d1/d2` | non implémentés | usage réel ou non applicable | captures |

## 5. Défauts et finitions

| Point | État constaté | Attendu |
|---|---|---|
| Police pixel floue | signalé depuis le lot 2 | tailles natives (multiples) et rendu net |
| Moins de personnages assis que de postes (campagne avancée) | non expliqué | cause trouvée, corrigée si défaut |
| Datacenter et siège sans test | constaté | tests dédiés |
| Compositeur de référence du pack | jamais lu ni exécuté | lu, comparé, utilisé si pertinent |
| Retour au sol d'origine du plan | absent | commande « revenir au plan » |
| Panneaux acoustiques (motif mural) | décoratifs | effet vérifié dans la spec et implémenté si confirmé |
| Vignettes du carrelage et du faux plancher | problème non écrit dans le dépôt | retrouver sur les captures et corriger |

## 6. Validation · 7. Livraison

Voir les sections du même nom en fin de document, remplies au fil du travail.
