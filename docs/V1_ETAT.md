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
| Le fondateur ne produit plus en passif | absent | `DoWork` traite le fondateur comme les autres | Exclure le fondateur de la production ; lui laisser le support automatique |
| Terminal de saisie mot par mot | absent | — | Lot 2 : session, budget de travail borné, temps cohérent, raccourcis neutralisés |
| Mini-jeux de grosses actions et délégation | absent | — | Lot 3 |
| Avis causaux avec lien vers le levier | partiel | `Core/products/Reviews.cs` | Causes matériel (rupture, défauts, délai), cloud, IA |
| Prix, marketing, souhaits, entretien, hébergement plafonné | fait | `Campaign.Products.cs` (`SetPrice`, `SetMarketing`, `AdjustHosting`), `Core/products/Market.cs` | Relier à l'infrastructure interne |
| Équipe : recrutement, horaires, congés, formation, augmentation | fait | `Core/campaign/Campaign.Team.cs`, `Core/team/` | Métiers nouveaux (opérateur, chercheur), apparences du pack |
| Arbre de développement sur vrais jalons | partiel | `Core/progress/DevTree.cs` (17 nœuds, 5 branches), `Campaign.Daily.cs` (`AwardUsers`, `CheckMilestones`) | Sous-branches, Outils du boss, matériel, industrie, IA, cloud, recherche |
| Finances et faillite progressive | fait | `Core/finance/Ledger.cs`, `Core/finance/Solvency.cs`, `Campaign.Daily.cs` (`UpdateSolvency`) | Charges énergie, matières, calcul externe ; détail par site |
| Sauvegardes nommées, automatiques, atomiques, migration | partiel | `Core/persistence/SaveStore.cs` (format 2), `Core/persistence/Migration.cs` (1 → 2) | Format 3 : sites, fondateur actif, apparences, nouvelles activités ; migration 2 → 3 |
| Mobilier utile et plafonné | partiel | `Core/space/Furniture.cs` (19 meubles), `Core/space/Layout.cs` | Catalogue du pack (55 meubles, 25 décorations), indicateurs regroupés par site |
| Plusieurs sites visitables, vue commune | absent | Un seul `Premises` dans `Campaign` | Lot 4 |
| Rendu isométrique, ancres, tri topologique, ombres, couches | absent | `UI/Scene/SceneCanvas.cs` (grille de dessus) | Lot 2 : porter le compositeur de référence du pack |
| HUD Papier, polices du pack, curseurs | absent | `UI/Pixel/`, `UI/Hud/Ui.cs` | Lot 2 |
| Personnages en couches, coiffures, peaux | absent | `Person.Look` (5 personnages Pixel Office) | Lot 2 : apparence sauvegardée, tirée à la création |
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
| 2 | Chambre avec les nouveaux assets, HUD, création de projet, terminal du boss, premier lancement, avis, sauvegarde et reprise | à faire |
| 3 | Employés automatiques, délégation, mini-jeux sans spam, arbre et confort utiles | à faire |
| 4 | Multisite, réseau, racks, hébergement interne et externe, offres cloud | à faire |
| 5 | Recherche, prototypage, gamme matérielle, flux d'usine, stocks, approvisionnement, canaux de vente | à faire |
| 6 | IA simulées, studio, XR, calcul avancé et quantique | à faire |
| 7 | Couverture de tous les IDs, équilibrage, performance, accessibilité, migrations, candidat de sortie | à faire |

## Couverture du registre

586 lignes, **0 validée**. Une ligne passe à « validé » seulement quand elle nomme un consommateur réel dans le jeu, son fichier de définition et un scénario de test qui vérifie une conséquence (avant/après pour un objet de gameplay, action et état pour un contrôle d'interface). Le contrôle `AssetRegistryTest` est statique : il vérifie les IDs et les fichiers cités, pas les effets.
