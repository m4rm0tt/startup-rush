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
| Référence bots, graines fixes | **fait** (`docs/equilibrage/reference_avant.md`, reproduite à l'identique) | bilan reproduit et archivé avant tout réglage | `Tests/balance/*`, `docs/equilibrage/` | même chiffres qu'au lot 7 | — |
| Studio/XR faible (38 k vs 198 k) | constaté au lot 6 | voie viable | `Campaign.Lab.cs`, `ProductCatalog.cs`, bots | bilan avant/après, 5 graines | — |
| Quantique jamais atteint (36 mois) | constaté au lot 6 | atteignable tard dans une campagne longue bien gérée | `Furniture.cs`, `Campaign.Lab.cs`, bot `quantique` | horizon 36 mois, jalon « en service » | — |
| `marketing_excessif` 4/5 faillites (2/5 au lot 5) | **fait** : décalage du hasard prouvé (`docs/equilibrage/enquete_marketing_excessif.md`), corrigé (`1dec18a`), 2/5 à nouveau | cause prouvée par l'historique ou dite non prouvée | historique git, `Market.cs` | rejouer le bot aux commits du lot 5 et du lot 6 | — |
| Carte à bas prix vs loyer d'atelier | constaté au lot 5 | matériel avec une utilité économique claire | `Industry.cs`, `ProductCatalog.cs` | bots matériel | — |
| PC, serveurs, GPU sans bot | constaté | scénarios bots | `Core/bots/*` | bilan | — |
| Salle serveur (≈ 78–86 k vs 192–198 k) | constaté au lot 4 | réévaluée avec les besoins de calcul de l'IA et de la recherche | `Campaign.Infra.cs`, `Campaign.Ai.cs` | bilan | — |
| `plusieurs_petits` domine, `jeu_simple` vivote | constaté | domination avec contrepartie, `jeu_simple` viable | `Market.cs`, `ProductCatalog.cs` | bilan, dispersion | — |

## 3. Fonctions prévues par la spécification

| Point | État constaté | Attendu | Fichiers | Validation | Blocage |
|---|---|---|---|---|---|
| Préparation de recherche au terminal | **fait** (`59eabc6`, FeaturesTest) | le fondateur contribue à une recherche par le terminal, budget distribué, borné | `Campaign.Terminal.cs`, `Campaign.Research.cs`, `TerminalView.cs` | tests ciblés | — |
| Transferts de stock entre sites | **fait** (`1dec18a`) + correctif stock fantôme d'un site fermé ou quitté | transfert explicite avec délai et coût, sans duplication ni perte ; plus de prélèvement involontaire sur un autre site | `Campaign.Industry.cs`, `IndustryPanel.cs` | invariants de stock | — |
| Studio : « quelqu'un sur le site » | **fait** : designer / testeur·se de niveau 45 sur le site (`1dec18a`) | une affectation de métier (personne affectée au studio, compétence design) | `Campaign.Lab.cs`, `Person.cs` | tests | — |
| Opérateurs assis aux postes | **fait** (`21f7918`, RendererTest, capture 30) | personnages assis aux chaises des stations, présence = activité réelle | `SceneCanvas.cs`, `RoomRenderer.cs`, `Campaign.Industry.cs` | captures | — |
| Graphe de dépendances, `comp_api`, `comp_storage`, `comp_game_menu` | **fait** (`59eabc6`, PanelsTest, capture 09b) ; flèches haut/bas/gauche non utilisées (graphe orienté gauche → droite) | graphe affiché dans l'atelier, trois composants testés | `ProductCatalog.cs`, `ProjectPanel.cs`, `ComponentArt.cs` | tests + captures | — |
| Composants de jeu de la spec (§6.2) | **fait** : moteur = gameplay, menu, progression = contenu, sauvegarde, inventaire, classement, multijoueur | liste « moteur, menu, progression, sauvegarde, inventaire, classement, multijoueur simulé » couverte, sans doublon | `ProductCatalog.cs` | tests | — |
| Métiers opérateur et chercheur | **fait** (`1dec18a`, format 8) | métiers à part entière (recrutement, compétences, effets) | `Person.cs`, `Recruitment.cs`, `Campaign.*` | tests + migration | — |
| Avis reliés aux causes (spec §8 : prix, rupture, performances, défauts, délai ; services : lenteur, manque de fonctions, support ; IA : qualité des réponses, délai/coût) | **fait** (`59eabc6`) ; le « délai » du matériel est dit par la rupture (pas de commandes en attente simulées) | causes calculées avec levier | `Reviews.cs` | tests | — |
| Résultat par site | **fait** (`1dec18a`, somme exacte testée, capture 33b) | détail par site réconcilié avec le total | `Ledger.cs`, `FinancePanel.cs` | invariant de somme | — |
| Saisie simplifiée du terminal | **déjà fait** (lot 3 : réglage, `TerminalView`) | vérifier qu'elle marche encore avec le terminal habillé du lot 7 | `TerminalView.cs` | test + capture | — |
| Recoloration des tenues | **fait** (`21f7918`) : canaux primary/secondary ; « accent » laissé à la base | tenue au choix à la création, sauvegardée | `Person.cs`, `RoomRenderer.cs`, `Portraits.cs` | capture + sauvegarde | — |
| Estompage des objets hauts | **fait** (`21f7918`, RendererTest) | objets hauts estompés quand ils cachent une personne ou le survol | `RoomRenderer.cs`, `SceneCanvas.cs` | capture | — |
| Câblage et sécurité | scores par site | **simplification assumée** (pas de fil à fil, pas de zones) : documentée | `DESIGN_SIMULATION.md` | — | — |

## 4. Couverture des assets

| Point | État constaté | Attendu | Validation |
|---|---|---|---|
| Registre | 141 validés, 349 partiels, 96 non implémentés (vérifié dans `docs/asset_usage.csv`) | chaque ligne justifiée : validé, partiel avec raison, ou non applicable | **fait** : 252 validés, 319 partiels, 15 non applicables, 0 non implémenté (`tools/registry_finition.py`, `AssetRegistryTest`) |
| Six modules de pièce | non implémentés | posables comme lots, prix et effets comptés | **fait** (`9e1cb36`, FeaturesTest, capture 41) |
| `moving_box` | non implémenté | les cartons du déménagement visibles et ouvrables | **fait** (`9e1cb36`, RendererTest) |
| `floor_marking`, `floor_threshold` | non implémentés | usage réel | **fait** (`9e1cb36`, RendererTest) |
| `wall_corner_outer`, `wall_opening` | non implémentés | usage réel ou non applicable justifié | **non applicable** : aucun plan du pack n'a d'angle saillant ni de passage sans porte ; seuls les deux murs du fond sont dessinés |
| HUD restants | non implémentés | branchés là où ils servent | **fait** (`UI/Hud/HudKit.cs`) : liste déroulante (catalogue, transferts), champ numérique (quantité), multiligne (notes de projet, sauvegardées), recherche (catalogue), menu contextuel (projets), jauges (sites), bandeau (factures en retard), puce sombre, séparateurs, panneau secondaire, carte (modules), barres (résultat par site), défilement horizontal (mois par mois), rayures (recherche du jour), barre de titre (inspecteur), grille verticale (courbes), thème Papier. Tests : `UiTests/HudKitTest.cs`, `PanelsTest.TheHudKitIsWiredIntoTheRealPanels` |
| Icônes sans usage | non implémentées ou mappées sans affichage | affichées là où elles servent | **fait** : `icon_gpu` (chiffre « GPU des sites »), `icon_server` (dépense « Serveurs »), `icon_training` (Former, Entraîner, Compétences) ; 15 icônes de titres de section, besoins, causes d'avis, priorité, verrou. Correspondances mortes retirées. `icon_drag_grip` : non applicable (pas de glisser-déposer) |
| `cursor_busy`, `cursor_resize_v/d1/d2` | non implémentés | usage réel ou non applicable | **fait** : `cursor_busy` au chargement et à la sauvegarde (test) ; redimensionnement vertical/diagonal non applicable (fenêtre Windows, panneau redimensionnable à l'horizontale seulement) |
| Objets du catalogue « partiels » | affichage vu en capture, effets non testés | affichage, clic et effet vérifiés | **fait** : `UiTests/CatalogCoverageTest.cs` pose les 185 objets achetables, vérifie dessin, clic et chaque effet déclaré. **Défaut corrigé** : les objets centrés sans `selection_poly` (lampadaire, fontaine, trépied, poubelle…) n'étaient pas cliquables, donc ni déplaçables ni rangeables. Restent partiels : ordinateurs et stations dont l'effet est un rendement de travail (`Output`/`Quality`), objets agissant par leur identifiant (convoyeurs, entrepôt) |
| Graphe de dépendances (`link_*`, `port_*`) | — | — | 13 pièces utilisées (partielles : capture 09b) ; 9 flèches et ports haut/bas/gauche non applicables (graphe orienté gauche → droite) |

## 5. Défauts et finitions

| Point | État constaté | Attendu |
|---|---|---|
| Police pixel floue | **fait** (`f5c1283`) : échelle continue en cause ; grille 12 px, Alias, échelle entière | tailles natives (multiples) et rendu net |
| Moins de personnages assis que de postes (campagne avancée) | **expliqué, non reproduit** : sonde déterministe (graine 37) : 4 bureaux, 4 postes complets, 4 personnes assises ; une personne de dos face au bureau compact donnait l'illusion d'un poste vide | — |
| Datacenter et siège sans test | constaté | tests dédiés |
| Compositeur de référence du pack | jamais lu ni exécuté | lu, comparé, utilisé si pertinent |
| Retour au sol d'origine du plan | **fait** (`a0f655f`) | commande « revenir au plan » |
| Panneaux acoustiques (motif mural) | **fait** (`a0f655f`) : spec « contribution plafonnée à la concentration/qualité du studio » | effet vérifié dans la spec et implémenté si confirmé |
| Vignettes du carrelage et du faux plancher | **fait** (`a0f655f`) : les dalles 2 × 2 montraient une croix au centre de la pastille | retrouver sur les captures et corriger |

## 6. Validation · 7. Livraison

Voir les sections du même nom en fin de document, remplies au fil du travail.
