# V1 « tous les assets » : état d'avancement

Spécification : [`docs/specs/PROMPT_CLAUDE_STARTUPRUSH_V1_TOUS_LES_ASSETS.md`](specs/PROMPT_CLAUDE_STARTUPRUSH_V1_TOUS_LES_ASSETS.md).
Pack : `assets/StartupRushOriginalAssets/` (v1.0.0, copié sans modifier ses chemins internes).
Registre d'usage : [`docs/asset_usage.csv`](asset_usage.csv), tenu par `tools/build_asset_registry.py` et contrôlé par `Tests/space/AssetRegistryTest.cs`.

Ce fichier sert à reprendre le travail : il dit ce qui existe vraiment dans le code, ce qui manque, et où en est chaque lot.

## Décisions qui changent la base

| Sujet | Avant (version tycoon 2.1) | V1 |
|---|---|---|
| Projection | Grille de 16 px vue de dessus (`UI/Scene/SceneCanvas.cs`) | Isométrique 2:1 fixe, cases 64×32 : `X=(U−V)·32`, `Y=(U+V)·16` |
| HUD | Sombre, cadres Kenney recolorés (`UI/Pixel/PixelPanel.cs`, `UI/Assets/Ui/skins/`) | « Papier » crème et encre, nine-slices et états du pack |
| Fondateur | Produit tout seul comme un employé (`Campaign.Time.cs`, `DoWork`) | Ne produit que par le terminal de saisie ; support automatique seulement |
| Locaux | Un seul local, 4 annonces (`Core/space/RealEstate.cs`) | Plusieurs sites, 10 plans et 6 modules du pack |
| Concept de référence | `PROMPT_CLAUDE_STARTUPRUSH.md` et le PDF de concept (roguelike, pièce vue de face) | Remplacés par la spécification V1 : une entreprise persistante, pas de roguelike ni de contrats |

## Matrice d'écarts

État : **fait** (le code satisfait déjà le besoin), **partiel** (existe, à étendre ou à changer), **absent**.

| Besoin de la spécification | État | Code actuel | Ce qui reste |
|---|---|---|---|
| Création rapide d'un projet (type, public, composants, prix proposés) | fait | `Core/campaign/Campaign.Products.cs` (`CreateProject`), `UI/Hud/Panels/ProjectsPanel.cs` | Nouvelles familles (outils métier, API, XR, IA, matériel) |
| Composants à niveaux, dépendances, coût de suivi | fait | `Core/products/ProductCatalog.cs`, `Core/products/Design.cs` | Composants jeu demandés (moteur, menu, inventaire…), icônes `project_components` |
| Design → développement → tests automatiques par les employés | fait | `Core/campaign/Campaign.Time.cs` (`PickWork`, `DoWork`) | Étendre aux tâches de recherche, fabrication, IA |
| Le fondateur ne produit plus en passif | **fait (lot 2)** | `Campaign.Time.cs` (`FounderTime`) : support seulement, hors terminal | — |
| Terminal de saisie mot par mot | **fait (lot 2)** | `Core/campaign/Campaign.Terminal.cs`, `UI/Hud/TerminalView.cs`, `UI/Screens/GameScreen.cs` | Améliorations du terminal dans l'arbre (lot 3), saisie simplifiée d'accessibilité |
| Mini-jeux de grosses actions et délégation | absent | — | Lot 3 |
| Avis causaux avec lien vers le levier | partiel | `Core/products/Reviews.cs` | Causes matériel (rupture, défauts, délai), cloud, IA |
| Prix, marketing, souhaits, entretien, hébergement plafonné | fait | `Campaign.Products.cs` (`SetPrice`, `SetMarketing`, `AdjustHosting`), `Core/products/Market.cs` | Relier à l'infrastructure interne |
| Équipe : recrutement, horaires, congés, formation, augmentation | fait | `Core/campaign/Campaign.Team.cs`, `Core/team/` | Métiers nouveaux (opérateur, chercheur), apparences du pack |
| Arbre de développement sur vrais jalons | partiel | `Core/progress/DevTree.cs` (17 nœuds, 5 branches), `Campaign.Daily.cs` (`AwardUsers`, `CheckMilestones`) | Sous-branches, Outils du boss, matériel, industrie, IA, cloud, recherche |
| Finances et faillite progressive | fait | `Core/finance/Ledger.cs`, `Core/finance/Solvency.cs`, `Campaign.Daily.cs` (`UpdateSolvency`) | Charges énergie, matières, calcul externe ; détail par site |
| Sauvegardes nommées, automatiques, atomiques, migration | **format 3 (lot 2)** | `SaveStore.cs` (format 3), `Migration.cs` (1 → 2), `MigrationV2.cs` (2 → 3), copie `.v<format>.json` | Sites multiples et nouvelles activités (format 4, lot 4) |
| Mobilier utile et plafonné | partiel | `Core/space/Furniture.cs` (38 IDs du pack jouables), `Core/space/Layout.cs` (ambiance plafonnée, objets personnels, écrans, lit) | Le reste du mobilier et de la décoration, indicateurs regroupés par site |
| Plusieurs sites visitables, vue commune | absent | Un seul `Premises` dans `Campaign`, sur un plan du pack (chambre, petit bureau, bureau moyen, open space) | Lot 4 |
| Rendu isométrique, ancres, tri topologique, ombres, couches | **fait (lot 2)** | `UI/Iso/PackImages.cs`, `UI/Iso/RoomRenderer.cs` (portage du compositeur), `UI/Scene/SceneCanvas.cs` | Objets muraux posés par le joueur, estompage des objets hauts |
| HUD Papier, polices du pack, curseurs | partiel (lot 2) | `UI/Pixel/PixelPanel.cs` (9-slice du pack), `UI/Pixel/Palette.cs`, `UI/App.axaml`, `UI/Pixel/Icons.cs` | Curseurs, onglets, cartes, listes, cases à cocher du pack ; police à régler (taille non multiple de 12) |
| Personnages en couches, coiffures, peaux | **fait (lot 2)** | `Core/team/Person.cs` (`Appearance`), `UI/Iso/Portraits.cs`, choix à la nouvelle campagne | Recoloration des tenues |
| Matériel, usine, stocks, approvisionnement, canaux de vente | absent | — | Lot 5 |
| Infrastructure interne, racks, énergie, refroidissement, offres cloud | absent | — | Lot 4 |
| IA simulée | absent | — | Lot 6 |
| Recherche, laboratoires, studio, XR, quantique | absent | — | Lots 5 et 6 |
| Bots d'équilibrage et rapport | partiel | `Core/bots/`, `Tests/balance/` | Stratégies avec terminal et délégation, nouvelles branches |
| Captures du vrai jeu | fait | `UI/Capture.cs` (`--capture`) | Refaire après chaque lot visuel |

Le pack fournit un compositeur C# de référence (`assets/StartupRushOriginalAssets/tools/csharp/StartupRushReference/Program.cs`, 612 lignes, sans dépendance). Il n'a pas encore été lu en détail ni exécuté ici ; c'est la base prévue pour le rendu isométrique du lot 2.

## Lots

| Lot | Contenu | État |
|---|---|---|
| 1 | Audit, protection des sauvegardes, registre de couverture | **fait** : pack et spécification versionnés, registre de 586 lignes (570 IDs, 10 plans, 6 modules), contrôle statique, cette matrice. Les sauvegardes actuelles sont déjà atomiques avec copie `.bak` ; la migration vers le format 3 arrive avec le lot 2. |
| 2 | Chambre avec les nouveaux assets, HUD, création de projet, terminal du boss, premier lancement, avis, sauvegarde et reprise | **fait**, voir ci-dessous |
| 3 | Employés automatiques, délégation, mini-jeux sans spam, arbre et confort utiles | **en partie**, voir « Lot 3 » |
| 4 | Multisite, réseau, racks, hébergement interne et externe, offres cloud | à faire |
| 5 | Recherche, prototypage, gamme matérielle, flux d'usine, stocks, approvisionnement, canaux de vente | à faire |
| 6 | IA simulées, studio, XR, calcul avancé et quantique | à faire |
| 7 | Couverture de tous les IDs, équilibrage, performance, accessibilité, migrations, candidat de sortie | à faire |

## Lot 2 : ce qui a été fait

- **Moteur** : le pack est embarqué dans `Core` (`Core/space/Pack.cs` : emprises, accès et sockets par orientation, plans de
  pièces). Le local devient un plan du pack : objets orientés, enfants accrochés aux sockets (chaise, écran, clavier, tour,
  objets de bureau), postes définis par bureau et socket de chaise (un bench en a trois), objets livrés avec le local
  (`Included`), revente à la moitié du prix réellement payé. Rotation (touche R). `Campaign.Furnish.cs` installe un poste
  complet avec les cartons puis, si on l'accepte, des achats (bots, migration, déménagement).
- **Fondateur** : plus de production passive ; présent à son poste, il répond au support des projets en ligne. Il produit
  par le terminal : 24 mots par session, rendement borné (`Tuning.Terminal`), vitesse de frappe plafonnée (×0,85 à ×1,2),
  aucune pénalité pour une faute, énergie dépensée par mot, pas de travail la nuit ni en congé, une seule session à la
  fois, mots tirés d'un hasard séparé (la simulation reste reproductible). Quand rien n'est en file, le terminal sert à la
  maintenance d'un projet en ligne (borné à ce qu'il reste à corriger). Le lit de la chambre donne +10 d'énergie la nuit.
- **Temps** : pendant la saisie, l'entreprise tourne à vitesse normale ; à la fermeture, la vitesse et la pause d'avant
  reviennent. Un réglage d'accessibilité met l'entreprise en pause pendant la saisie. La vraie pause arrête la saisie.
  Aucun raccourci pendant la frappe ; Espace ou Entrée valident, sans répétition si on maintient la touche.
- **Rendu** : portage du compositeur de référence du pack (`UI/Iso`), image de la pièce recomposée seulement quand le local
  ou les présences changent ; bulles d'activité avec les icônes du pack ; mode aménagement avec fantôme orienté, cases
  d'accès en pointillés, accroche aux bureaux.
- **Interface** : cadres 9-slice et états du pack, jetons de couleur « Papier », polices StartupRush Pixel et Mono, icônes
  du pack, portraits en couches, apparence du fondateur au choix. Les anciens sprites, cadres et polices ne sont plus livrés.
- **Sauvegardes** : format 3, migration du format 2 (local → plan correspondant, meubles → équivalents du pack au prix payé,
  dans les cartons, puis un poste pour chacun sans aucun achat ; apparences ; copie `.v2.json` gardée).
- **Équilibrage** : effort des composants inchangé (×2,2) ; le premier projet demande environ 380 à 450 mots. Les bots tapent
  30 mots par jour ouvré. `BALANCE_REPORT.md` est régénéré.

Vérifié : 81 tests (dont terminal, fondateur sans production, sockets, rotation, lit, écrans, migration 2 → 3, apparences),
la campagne d'équilibrage, et le parcours `--capture` dans le vrai jeu sous Windows 11 (25 écrans, rechargement identique).
Captures : `docs/captures/`.

Limites connues du lot 2 :

- Taille de la police pixel : le jeu garde ses tailles de texte, qui ne sont pas toujours des multiples de 12 ; quelques textes
  sont un peu flous. À régler avec le reste du HUD.
- Sur la capture de la campagne avancée, on compte moins de personnages assis que de postes complets : à vérifier (personne
  en congé ou en formation, ou personnage masqué par un meuble).
- Équilibrage : « marketing excessif » ne fait plus faillite que 2 fois sur 5 (5 sur 5 avant) ; à reprendre au lot 7.
- Pas encore de mini-jeux, de délégation ni d'améliorations du terminal dans l'arbre (lot 3). Un seul site (lot 4).
- Le dossier `docs/Startup_Rush_Dossier.pdf` décrit encore la version 2.1 (vue de dessus, fondateur automatique).

## Lot 3 : ce qui a été fait, ce qui reste

Fait (testé par `Tests/campaign/LaunchAndBossTest.cs`, vu sur les captures `07b_lancement` et `15_arbre`) :
- Branche « Outils du boss » dans l'arbre (`Core/progress/DevTree.cs`) : Raccourcis clavier (+15 % par mot), Clavier
  confortable (−40 % d'énergie par mot), Tests assistés (mots de tests +30 %), Macros (sessions de 32 mots),
  Délégation des lancements, Procédures de publication.
- Mini-jeux du lancement (`Core/campaign/Campaign.Launch.cs`, `UI/Hud/LaunchView.cs`) : application → remettre 5 étapes
  du déploiement dans l'ordre ; jeu → repérer 3 tests en anomalie sur 8 (les fausses alertes coûtent). Bonus plafonné :
  jusqu'à −50 % de bugs à la sortie. « Lancer sans jouer » donne le résultat de base. Tirage séparé, reproductible.
- Délégation : un employé présent lance à ta place (sans bonus, ou la moitié du meilleur avec les procédures) ; mode
  « déléguer par défaut » (`Campaign.DelegateLaunches`) qui sort chaque matin les projets prêts.

Reste à faire :
- Mini-jeux matériel, IA et quantique (familles prévues par la spécification, avec les lots 5 et 6).
- Préférence « me proposer les grosses actions » et regroupement des sollicitations.
- Supervision à distance et automatisation avancée dans les Outils du boss ; saisie simplifiée d'accessibilité.
- Confort : les meubles de confort restants du pack (fauteuils, distributeurs, sanitaires, modules) et la synthèse par site.
- Les bots n'utilisent pas encore ces nœuds ni la délégation : à ajouter aux stratégies, puis refaire le bilan.

## Couverture du registre

586 lignes : **36 validées**, 96 partielles (branchées sans test d'effet, ou vérifiées seulement en capture), 454 non
implémentées (dont les décors des plans, affichés mais sans fonction pour l'instant). Une ligne passe à « validé » seulement quand elle nomme un consommateur réel dans le jeu, son fichier de définition et un scénario de test qui vérifie une conséquence (avant/après pour un objet de gameplay, action et état pour un contrôle d'interface). Le contrôle `AssetRegistryTest` est statique : il vérifie les IDs et les fichiers cités, pas les effets.
