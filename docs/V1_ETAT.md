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
| Mini-jeux de grosses actions et délégation | **fait (lot 3)** pour le logiciel | `Core/campaign/Campaign.Launch.cs`, `UI/Hud/LaunchView.cs` | Mini-jeux matériel, IA, quantique (lots 5 et 6) |
| Avis causaux avec lien vers le levier | partiel | `Core/products/Reviews.cs` ; offres cloud : `Campaign.CloudTrends` (prix, saturation, coupures) | Causes matériel (rupture, défauts, délai), IA |
| Prix, marketing, souhaits, entretien, hébergement plafonné | **fait (lot 4)** | `Campaign.Products.cs` (`AdjustHosting` : externe, ou interne avec débordement sous le plafond), `Core/products/Market.cs` (`Product.Capacity`) | — |
| Équipe : recrutement, horaires, congés, formation, augmentation | fait | `Core/campaign/Campaign.Team.cs`, `Core/team/` | Métiers nouveaux (opérateur, chercheur), apparences du pack |
| Arbre de développement sur vrais jalons | partiel | `Core/progress/DevTree.cs` (25 nœuds, 6 branches dont « Outils du boss »), `Campaign.Daily.cs` (`AwardUsers`, `CheckMilestones`) | Sous-branches matériel, industrie, IA, cloud, recherche |
| Finances et faillite progressive | fait | `Core/finance/Ledger.cs` (catégories `Cloud`, `Energy`), `Core/finance/Solvency.cs`, `Campaign.Daily.cs` | Matières (lot 5) ; détail du résultat par site |
| Sauvegardes nommées, automatiques, atomiques, migration | **format 4 (lot 4)** | `SaveStore.cs` (format 4), `Migration.cs` (1 → 2), `MigrationV2.cs` (2 → 3), `MigrationV3.cs` (3 → 4), copie `.v<format>.json` | Nouvelles activités des lots 5 et 6 |
| Mobilier utile et plafonné | **fait (lot 3)** pour le bureau | `Core/space/Furniture.cs` (89 IDs du pack jouables), `Core/space/Layout.cs` (`Comfort` : 8 indicateurs plafonnés avec couverture), synthèse dans `UI/Hud/Panels/BuildPanel.cs` | `moving_box` ; mobilier des ateliers, labos et salles techniques (lots 4 à 6) ; synthèse multisite (lot 4) |
| Plusieurs sites visitables, vue commune | **fait (lot 4)** | `Campaign.Sites`, `Campaign.SiteId` (vue), `Core/campaign/Campaign.Sites.cs`, `Core/space/RealEstate.cs` (parc d'offres), `UI/Hud/Panels/SitesPanel.cs` | Ateliers, usine, laboratoire (lots 5 et 6) ; transferts entre sites avec délai (lot 5, pour les stocks) |
| Rendu isométrique, ancres, tri topologique, ombres, couches | **fait (lots 2 et 3)** | `UI/Iso/PackImages.cs`, `UI/Iso/RoomRenderer.cs` (portage du compositeur ; objets muraux et plafond depuis le local réel), `UI/Scene/SceneCanvas.cs` | Estompage des objets hauts |
| HUD Papier, polices du pack, curseurs | partiel (lot 2) | `UI/Pixel/PixelPanel.cs` (9-slice du pack), `UI/Pixel/Palette.cs`, `UI/App.axaml`, `UI/Pixel/Icons.cs` | Curseurs, onglets, cartes, listes, cases à cocher du pack ; police à régler (taille non multiple de 12) |
| Personnages en couches, coiffures, peaux | **fait (lot 2)** | `Core/team/Person.cs` (`Appearance`), `UI/Iso/Portraits.cs`, choix à la nouvelle campagne | Recoloration des tenues |
| Matériel, usine, stocks, approvisionnement, canaux de vente | absent | — | Lot 5 |
| Infrastructure interne, racks, énergie, refroidissement, offres cloud | **fait (lot 4)** | `Core/space/Infrastructure.cs`, `Core/campaign/Campaign.Infra.cs`, `Core/campaign/Campaign.Cloud.cs`, `UI/Hud/Panels/CloudPanel.cs` | Usage du calcul et des GPU par l'IA et la recherche (lots 5 et 6) ; quantique (lot 6) |
| IA simulée | absent | — | Lot 6 |
| Recherche, laboratoires, studio, XR, quantique | absent | — | Lots 5 et 6 |
| Bots d'équilibrage et rapport | partiel | `Core/bots/`, `Tests/balance/` (terminal, délégation, mots tapés mesurés) | Nouvelles branches (lots 4 à 6) |
| Captures du vrai jeu | fait | `UI/Capture.cs` (`--capture`) | Refaire après chaque lot visuel |

Le pack fournit un compositeur C# de référence (`assets/StartupRushOriginalAssets/tools/csharp/StartupRushReference/Program.cs`, 612 lignes, sans dépendance). Il n'a pas encore été lu en détail ni exécuté ici ; c'est la base prévue pour le rendu isométrique du lot 2.

## Lots

| Lot | Contenu | État |
|---|---|---|
| 1 | Audit, protection des sauvegardes, registre de couverture | **fait** : pack et spécification versionnés, registre de 586 lignes (570 IDs, 10 plans, 6 modules), contrôle statique, cette matrice. Les sauvegardes actuelles sont déjà atomiques avec copie `.bak` ; la migration vers le format 3 arrive avec le lot 2. |
| 2 | Chambre avec les nouveaux assets, HUD, création de projet, terminal du boss, premier lancement, avis, sauvegarde et reprise | **fait**, voir ci-dessous |
| 3 | Employés automatiques, délégation, mini-jeux sans spam, arbre et confort utiles | **fait**, voir « Lot 3 » (mini-jeux matériel, IA et quantique avec les lots 5 et 6) |
| 4 | Multisite, réseau, racks, hébergement interne et externe, offres cloud | **fait**, voir « Lot 4 » |
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

Fait ensuite (testé par `Tests/space/ComfortTest.cs` et `Tests/campaign/LaunchAndBossTest.cs`, vu sur les captures 07, 15, 18) :
- **Grosses actions** : « Me les proposer » (par défaut), « Ne pas me déranger » ou « Déléguer par défaut », dans l'aperçu d'un
  projet. Plusieurs projets prêts font **une seule** annonce (clé `ready`), mise à jour au lieu de s'empiler, qui ouvre le
  lancement ; rien n'est proposé quand un employé lancera demain matin ; une proposition attend la fin de la saisie au terminal.
- **Outils du boss** : Supervision à distance (terminal sans poste complet ou pendant une formation, rendement ×0,85) et
  Automatisation avancée (chaque lundi, l'équipe met en file la petite amélioration demandée par les avis d'un projet en ligne ;
  il faut un employé ; réglable « Préparées par l'équipe / Je les choisis »).
- **Saisie simplifiée** (Réglages, accessibilité, désactivée par défaut) : le mot s'écrit tout seul à 3 lettres/s, Entrée le
  valide, rendement de la frappe lente (×0,85) quel que soit le rythme.
- **Confort** : `Layout.Comfort` regroupe le mobilier en 8 indicateurs plafonnés : ambiance (6), détente (4), confort de base
  (3 : moitié rangement, moitié sanitaires, selon les personnes couvertes), pause (3 à 15 d'énergie, chacun prend la meilleure
  boisson ou collation et la meilleure place assise encore libres), concentration (+4 % au plus, cloisons), coordination
  (+3 personnes au plus ; tables de réunion avec leurs chaises ; petits supports 1 au plus), formation (+40 % au plus), accueil
  (+1 candidat par semaine). Un manque prive seulement d'un bonus. Gardé en cache tant que le local et l'effectif ne changent pas.
- **Objets muraux** posés par le joueur sur les murs du fond (plein ; stores sur une fenêtre ; jamais sur la porte ; planning
  sur deux segments) ; le décor mural des plans devient de vrais objets livrés avec le local, ajoutés une fois aux sauvegardes
  plus anciennes (`Campaign.ImportWallItems`, marqueur `WallItemsImported`, toujours le format 3). Luminaire au plafond.
- **Récompenses** : trophée au premier lancement, plaque à 10 000 CHF par mois, livrés dans les cartons, sans revente.
- **Bots** : stratégie « delegation » (le fondateur arrête de taper à la première embauche) : 0 faillite sur 5, 6 166 mots
  tapés contre 11 503 pour « plusieurs_petits » ; colonne « Mots tapés » dans `BALANCE_REPORT.md`.

Reste (reporté aux lots suivants, avec leur contexte) :
- Mini-jeux matériel (calibration), IA (échantillons) et quantique (signaux) : lots 5 et 6.
- `moving_box` : montrer les cartons dans la scène et ouvrir leur contenu (décor sans fonction pour l'instant).
- Mobilier des ateliers, laboratoires et salles techniques, synthèse multisite : lots 4 à 6.

## Lot 4 : ce qui a été fait, ce qui reste

Testé par `Tests/space/SitesTest.cs` et `Tests/space/InfrastructureTest.cs` ; vu sur les captures 26 à 28 du vrai jeu.

- **Sites** : `Campaign.Sites` (le premier est le siège) et `Campaign.SiteId` (le site affiché, un choix de vue). Louer en plus
  (nœud « Plusieurs sites »), déménager le site affiché, fermer un site (garantie rendue, matériel en cartons, personnes sans
  poste nommées ; le dernier site ne se ferme pas). Le parc de locations est défini par données : 7 types de locaux, plusieurs
  adresses chacun ; une offre louée disparaît des offres. Salle serveur et datacenter : sites techniques (climatisation,
  armoires électriques, fibre, chemins de câbles et cage livrés ; baies et serveurs du plan = aperçu) ; siège jusqu'à 60 postes.
- **Simulation par site** : chaque personne travaille sur le site de son poste (confort, pauses, coordination de ce site) ; le
  site affiché ne change rien (test : deux campagnes identiques regardant deux sites différents donnent le même état).
- **Format 4** : `MigrationV3` fait du local le premier site, reprend l'adresse depuis le nom et marque les loyers déjà facturés
  pour ce site ; fixture réelle `Tests/fixtures/campaign_v3.json` (une sauvegarde écrite par le jeu au format 3).
- **Infrastructure** (`Infrastructure.Of`) : modules en U dans les baies (fiche `fits` et emplacements du pack), switch
  obligatoire, tiroir de disques avec contrôleur ; capacités hébergement, calcul, GPU, stockage ; électricité (base du local +
  armoires), froid (base + climatiseurs ; baie haute densité −20 %, obturateurs −3 % chacun, 9 % au plus), réseau (2 par unité
  d'hébergement), câblage (−10 % sans organisation), onduleurs et supervision contre les incidents (3 % par jour au plus), cage
  (confiance des offres). Les climatiseurs consomment selon la chaleur. Électricité à 0,25 CHF/kWh, facturée avec le mois.
- **Hébergement interne** : chaque nuit, les projets « sur tes serveurs » reçoivent leur part dans l'ordre du portefeuille
  (avec 25 % de marge), une réserve réglable reste libre, puis les offres cloud. Ce qui ne tient pas déborde chez l'hébergeur,
  sous le plafond du projet (0 : jamais).
- **Offres cloud** : hébergement, stockage (nœud « Offres cloud »), calcul et GPU (« Calcul à la demande ») ; prix et réserve
  réglables ; demande agrégée (taille du marché, renommée, prix, coupures des 30 derniers jours, sécurité) ; une réservation qui
  ne tient plus est réduite avec un avertissement ; causes lisibles avec leur levier ; +2 points à 1 000 CHF sur 30 jours.
- **Bots** : « hebergement_maison » (0 faillite sur 5 ; 77 865 CHF médian contre 191 938 sans salle serveur sur 18 mois).

Limites et suite :
- À petite échelle, la salle serveur se rembourse lentement (bilan ci-dessus) : à réévaluer au lot 7 avec l'IA et la recherche,
  qui consommeront le calcul et les GPU (aujourd'hui seulement vendus).
- Pas de câblage fil à fil ni de zones dans une pièce : l'organisation et la sécurité sont des scores plafonnés par site.
- Le datacenter et le siège n'ont pas de test dédié (même code que la salle serveur et les bureaux).
- `moving_box` toujours sans fonction ; mini-jeux matériel/IA/quantique avec les lots 5 et 6.

## Couverture du registre

586 lignes : **77 validées**, 142 partielles (branchées sans test d'effet, ou vérifiées seulement en capture), 367 non
implémentées (dont les décors des plans, affichés mais sans fonction pour l'instant). Lot 3 : `tools/registry_lot3.py` ;
lot 4 : `tools/registry_lot4.py`. Une ligne passe à « validé » seulement quand elle nomme un consommateur réel dans le jeu, son fichier de définition et un scénario de test qui vérifie une conséquence (avant/après pour un objet de gameplay, action et état pour un contrôle d'interface). Le contrôle `AssetRegistryTest` est statique : il vérifie les IDs et les fichiers cités, pas les effets.
