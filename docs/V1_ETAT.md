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
| Mini-jeux de grosses actions et délégation | **fait** (logiciel au lot 3, calibration du matériel au lot 5) | `Core/campaign/Campaign.Launch.cs`, `UI/Hud/LaunchView.cs` | Mini-jeux IA et quantique (lot 6) |
| Avis causaux avec lien vers le levier | partiel | `Core/products/Reviews.cs` ; offres cloud : `Campaign.CloudTrends` (prix, saturation, coupures) | Causes matériel (rupture, défauts, délai), IA |
| Prix, marketing, souhaits, entretien, hébergement plafonné | **fait (lot 4)** | `Campaign.Products.cs` (`AdjustHosting` : externe, ou interne avec débordement sous le plafond), `Core/products/Market.cs` (`Product.Capacity`) | — |
| Équipe : recrutement, horaires, congés, formation, augmentation | fait | `Core/campaign/Campaign.Team.cs`, `Core/team/` | Métiers nouveaux (opérateur, chercheur), apparences du pack |
| Arbre de développement sur vrais jalons | partiel | `Core/progress/DevTree.cs` (36 nœuds, 6 branches), `Campaign.Daily.cs` (`AwardUsers`, `CheckMilestones`), recherches (`Campaign.Research.cs`) | IA, quantique (lot 6) |
| Finances et faillite progressive | fait | `Core/finance/Ledger.cs` (catégories `Cloud`, `Energy`), `Core/finance/Solvency.cs`, `Campaign.Daily.cs` | Matières (lot 5) ; détail du résultat par site |
| Sauvegardes nommées, automatiques, atomiques, migration | **format 5 (lot 5)** | `SaveStore.cs` (format 5 ; le format 4 se relit tel quel), `Migration.cs` (1 → 2), `MigrationV2.cs` (2 → 3), `MigrationV3.cs` (3 → 4), copie `.v<format>.json` | IA et studio (lot 6) |
| Mobilier utile et plafonné | **fait (lot 3)** pour le bureau | `Core/space/Furniture.cs` (89 IDs du pack jouables), `Core/space/Layout.cs` (`Comfort` : 8 indicateurs plafonnés avec couverture), synthèse dans `UI/Hud/Panels/BuildPanel.cs` | `moving_box` ; mobilier des ateliers, labos et salles techniques (lots 4 à 6) ; synthèse multisite (lot 4) |
| Plusieurs sites visitables, vue commune | **fait (lot 4)** | `Campaign.Sites`, `Campaign.SiteId` (vue), `Core/campaign/Campaign.Sites.cs`, `Core/space/RealEstate.cs` (parc d'offres), `UI/Hud/Panels/SitesPanel.cs` | Ateliers, usine, laboratoire (lots 5 et 6) ; transferts entre sites avec délai (lot 5, pour les stocks) |
| Rendu isométrique, ancres, tri topologique, ombres, couches | **fait (lots 2 et 3)** | `UI/Iso/PackImages.cs`, `UI/Iso/RoomRenderer.cs` (portage du compositeur ; objets muraux et plafond depuis le local réel), `UI/Scene/SceneCanvas.cs` | Estompage des objets hauts |
| HUD Papier, polices du pack, curseurs | partiel (lot 2) | `UI/Pixel/PixelPanel.cs` (9-slice du pack), `UI/Pixel/Palette.cs`, `UI/App.axaml`, `UI/Pixel/Icons.cs` | Curseurs, onglets, cartes, listes, cases à cocher du pack ; police à régler (taille non multiple de 12) |
| Personnages en couches, coiffures, peaux | **fait (lot 2)** | `Core/team/Person.cs` (`Appearance`), `UI/Iso/Portraits.cs`, choix à la nouvelle campagne | Recoloration des tenues |
| Matériel, usine, stocks, approvisionnement, canaux de vente | **fait (lot 5)** | `Core/industry/Industry.cs`, `Core/campaign/Campaign.Industry.cs`, familles matérielles de `ProductCatalog.cs`, `UI/Hud/Panels/IndustryPanel.cs` | Kits XR (lot 6) ; transferts de stock entre sites ; rentabilité (lot 7) |
| Infrastructure interne, racks, énergie, refroidissement, offres cloud | **fait (lot 4)** | `Core/space/Infrastructure.cs`, `Core/campaign/Campaign.Infra.cs`, `Core/campaign/Campaign.Cloud.cs`, `UI/Hud/Panels/CloudPanel.cs` | Usage du calcul et des GPU par l'IA et la recherche (lots 5 et 6) ; quantique (lot 6) |
| IA simulée | **fait (lot 6)** | `Core/ai/AiCatalog.cs`, `Core/campaign/Campaign.Ai.cs`, `UI/Hud/Panels/AiPanel.cs`, `UI/Hud/ChallengeView.cs` | Équilibrage de la rentabilité (lot 7) |
| Recherche, laboratoires, studio, XR, quantique | **fait (lots 5 et 6)** | `Core/campaign/Campaign.Research.cs`, `Core/campaign/Campaign.Lab.cs`, `UI/Hud/Panels/LabPanel.cs` | Équilibrage, captures du lot 6 (lot 7) |
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
| 5 | Recherche, prototypage, gamme matérielle, flux d'usine, stocks, approvisionnement, canaux de vente | **fait**, voir « Lot 5 » |
| 6 | IA simulées, studio, XR, calcul avancé et quantique | **fait**, voir « Lot 6 » (vu en captures, mesuré par bots ; réglage au lot 7) |
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

## Lot 5 : ce qui a été fait, ce qui reste

Testé par `Tests/industry/IndustryTest.cs` (10 tests) ; vu sur les captures 29 à 33 du vrai jeu.

- **Recherche** (`Campaign.Research.cs`) : 12 projets (familles, recettes, méthodes), une file ; des personnes affectées à la
  recherche avancent la première recherche équipée sur leur site (un équipement de chaque groupe : établi, oscilloscope, bancs,
  instruments...) ; durée et budget par jour annoncés, payés seulement si quelqu'un y a travaillé ; suspendre ne perd rien.
  Établis et bancs de laboratoire servent de postes. Une recherche terminée débloque comme un nœud (`IsUnlocked`) et rapporte 1 point.
- **Matériel** : 7 familles, 14 publics, 15 composants matériels conçus par l'équipe comme un logiciel ; chaque composant ajoute
  des matières à la nomenclature (une fois). Coût des matières, marge, stock, ventes et pannes sur la fiche du projet.
- **Production** (`Core/industry/Industry.cs`, `Campaign.Industry.cs`) : graphe des ports orientés du pack ; toutes les 10 minutes de
  travail, les unités avancent de station en convoyeur (2 unités par convoyeur, 1 par station), une saturation bloque l'amont,
  une station dont l'étape n'est plus au programme laisse passer, une station d'une étape à venir bloque (« mauvaise entrée ») ;
  matières prises au stock à la première station ; fin de ligne → stock du site. Opérateurs (affectation Production) pour les
  postes manuels et la supervision (1 pour 4 automates, armoires de contrôle) ; vitesse réduite par l'électricité/le froid.
  Puces : outil chargé à la main en salle propre complète, la moitié rebutée sans station de précision. Dispositions conseillées
  (assemblage, cartes, GPU) et ligne du plan d'usine, posées d'un clic avec coût annoncé.
- **Stocks et achats** : stock par site borné (40 + racks, palettes, bacs) ; achats livrés après le délai ; réserves automatiques
  sous plafond mensuel, transit compté, argent engagé à la commande, la matière la plus en manque d'abord et chacune à sa part de
  place ; alerte quand un ordre manque d'une matière sans réserve ; bouton « Réserves pour cet ordre ».
- **Ventes** : unités entières en stock seulement, par canaux (direct ; boutique en ligne ; distributeurs) avec visibilité,
  commission et capacité ; ventes perdues comptées ; garanties payées selon le taux de pannes (qualité de conception, contrôle
  qualité, rodage, calibration de la première série).
- **Format 5** : rien à convertir (données ajoutées), copie `.v4.json` gardée.
- **Bots** : « materiel » (0 faillite sur 5 ; ~60 cartes par mois sans rupture ; 10 356 CHF médian contre 191 938 sans matériel).

Limites et suite :
- Rentabilité : une carte à bas prix ne paie pas le loyer d'un atelier en 18 mois (bilan) ; PC, serveurs et GPU, plus chers,
  ne sont pas encore mesurés par un bot. À reprendre au lot 7.
- Pas de transferts de stock entre sites (chaque ligne prend au stock de son site ; les ventes puisent dans tous les sites).
- Les opérateurs ne sont pas assis aux postes des stations : une bulle les montre près d'une station de leur ligne.
- Le fondateur ne prépare pas la recherche au terminal (prévu par la spécification, pas fait).
- Les objets studio, XR, quantique et le calcul avancé du plan de laboratoire restent pour le lot 6.

## Lot 6 : ce qui a été fait, ce qui reste

Testé par `Tests/lab/Lot6Test.cs` (34 tests). Vu dans le vrai jeu : le parcours `--capture` entraîne, évalue et publie un modèle, loue
un laboratoire avancé et y pose studio, zone XR et installation quantique (captures 34 à 40 dans `docs/captures/`, relues : panneaux
IA et Studio/XR/quantique, mini-jeux d'évaluation et de signaux, scène du labo). Mesuré par trois bots (`ia`, `studio_xr`, `quantique`).

Revue du code du lot 6 (session suivante) : deux défauts corrigés et testés. (1) Créer un produit d'IA sans modèle publié faisait
disparaître en silence son composant `ai_core` : le projet est maintenant refusé avec sa raison. (2) Une inférence saturée fait
chuter la performance dans `Market` ; l'avis « C'est lent à charger » renvoyait alors à l'hébergement, qui n'y pouvait rien : il
renvoie au menu IA quand les serveurs suivent.

- **IA** (`Core/ai/AiCatalog.cs`, `Core/campaign/Campaign.Ai.cs`) : un modèle = objectif (assistant, génération, analyse, API) + taille
  (petit, moyen, grand ; les deux derniers demandent les recherches « Apprentissage automatique » et « Grands modèles »). On
  l'entraîne avec un jeu de données (public, acheté, curé) sur tes GPU (part réglable, `TrainingShare`) ou en calcul loué
  (0,50 CHF le point, 150 points par jour au plus). Version entraînée → évaluation (mini-jeu « associer échantillons et résultats »,
  `EvaluationChallenge`, jusqu'à 0,08 de qualité retrouvée, ou délégation) → publication. **La version en service continue de
  servir tant que la suivante n'est pas publiée.** Qualité = plafond de la taille × données × adéquation à l'objectif : un petit
  modèle spécialisé sur de bonnes données bat un gros modèle sur des données médiocres (testé).
- **Produits d'IA** : nouveau type `ai` (4 publics) dont le composant `ai_core` prend la qualité du modèle publié ; composant
  `ai_feature` pour les autres applications (une part de la charge). Le modèle se branche (`SetProductAiModel`, automatique au premier
  composant d'IA). **Inférence** : chaque nuit (`AllocateAi`), après l'hébergement et avant les offres cloud, les produits prennent du
  GPU (avec 25 % de marge), puis l'entraînement ; ce qui manque se loue sous un plafond mensuel (60 CHF l'unité). La capacité plafonne
  l'audience servie (`Market.Step`, `AiService`), les avis blâment la lenteur (cause `ai_capacity`, lien `ai`). Aucune vente interne
  n'est comptée.
- **Studio** (`Campaign.Lab.cs`, `StudioOf`) : caméra, fond vert, 2 éclairages, micro, enceintes, séparation acoustique ; score vidéo/audio,
  moitié si personne ne travaille sur le site. Effet plafonné : +30 % de qualité des composants graphics/audio/content/immersion
  (`Design.StudioBoosted`) et +15 % de portée marketing. Le meilleur studio compte, pas la somme.
- **Zone XR** (`XrOf`) : casque seul = 40 % (prototype), + 2 balises + tapis + quelqu'un sur place = 100 %. Retire jusqu'à 60 % des
  défauts à chaque amélioration d'une application XR, et 40 % des pannes de série d'un kit XR. Nouveaux types `xr_app` (public XR) et
  `xr_kit` (matériel, recherche « Suivi immersif »).
- **Laboratoire avancé et quantique** : local `advanced_lab` (nœud « Recherches avancées »), calculateur avancé (120 unités de calcul),
  cryostat ouvert (équipement de recherche) et fermé (installation stabilisée, après la recherche « Calibration cryogénique »), baie de
  contrôle et support cryogénique (un de chaque par cryostat), terminal de supervision. Un cryostat fermé pris en charge démarre
  après 14 jours de mise en route, seulement si le froid et l'énergie du site suivent (`RunQuantum`) ; le mini-jeu « signaux » la
  raccourcit de moitié au mieux, une fois (`QuantumChallenge`, `StabilizeQuantum`). Fournit 20 unités quantiques, vendues par l'offre
  `CloudKind.Quantum` (nœud « Service quantique »). Le quantique n'accélère que les recherches de calcul intensif (+25 %, `ResearchDef.Quantum`).
  Type `quantum` (matériel de fin de progression, recherche « Systèmes quantiques »).
- **Format 6** : rien à convertir (données ajoutées), copie `.v5.json` gardée.
- **Arbre** : `studio`, `xr`, `ai_models`, `advanced_lab`, `quantum_offers`. Recherches : `res_ml`, `res_data`, `res_ml_large`, `res_xr`,
  `res_cryo`, `res_qsystems`.

Limites et suite (à faire au lot 7) :
- **Équilibrage mesuré, pas encore réglé** (`BALANCE_REPORT.md`, 18 mois, 5 graines ; référence `plusieurs_petits` : 198 000 CHF) :
  - `ia` : 160 000 CHF, IA publiée vers le jour 277, le produit d'IA rapporte ~6 000 CHF/mois en fin de partie. Viable, pas dominant.
  - `studio_xr` : 38 000 CHF. Studio complet (jour 233) et zone XR complète (jour 287) dans 5 parties sur 5, mais le chemin coûte
    7 points d'arbre pris à la croissance, et l'application XR plafonne vers 300 utilisateurs et ~2 000 CHF/mois. **Trop faible.**
  - `quantique` : jamais en service, même sur 36 mois (tableau « Horizon long ») : labo loué vers le jour 700, calibration finie
    vers le jour 760, puis le cryostat fermé (380 000 CHF) reste hors de portée. Voulu « fin de progression », mais à décider :
    l'atteindre suppose ~600 000 CHF d'équipement plus 9 500 CHF/mois de loyer.
  - Les stratégies plus anciennes ont bougé depuis le bilan du lot 5 (ex. `marketing_excessif` : 4 faillites sur 5 au lieu de 2) ;
    vérifié que la correction du bot de cette session n'y est pour rien sur 18 mois. Cause non isolée (code du lot 6 ou autres
    changements depuis le bilan précédent).
- Le fondateur ne prépare toujours pas la recherche au terminal. Pas de transferts de stock entre sites. `moving_box` sans fonction.
- Le studio ne demande pas d'employé précis (« quelqu'un sur le site » suffit) ; les motifs muraux (`pattern_acoustic`) ne sont pas posables.
- Les icônes `icon_gpu`, `icon_server`, `icon_training` sont mappées (`Icons.cs`) mais pas encore utilisées.

## Couverture du registre

586 lignes : **110 validées**, 175 partielles (branchées sans test d'effet, ou vérifiées seulement en capture), 301 non
implémentées (dont les décors des plans, affichés mais sans fonction pour l'instant). Lot 3 : `tools/registry_lot3.py` ;
lot 4 : `tools/registry_lot4.py` ; lot 5 : `tools/registry_lot5.py`. Une ligne passe à « validé » seulement quand elle nomme un consommateur réel dans le jeu, son fichier de définition et un scénario de test qui vérifie une conséquence (avant/après pour un objet de gameplay, action et état pour un contrôle d'interface). Le contrôle `AssetRegistryTest` est statique : il vérifie les IDs et les fichiers cités, pas les effets.
