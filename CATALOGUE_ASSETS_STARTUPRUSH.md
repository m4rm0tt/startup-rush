# StartupRush — catalogue des assets

Recherche et téléchargement : **26 octobre 2026**. Cible confirmée : amélioration du jeu PC C#/Avalonia existant.

**31 packs/familles téléchargés : 27 sous CC0 et 4 familles de polices sous OFL-1.1.** Les fichiers graphiques/audio CC0 sont libres d’utilisation, adaptation et redistribution commerciale. Les polices restent sous leurs licences OFL et leurs avis doivent accompagner la distribution.

Cette collection est une bibliothèque de production, pas un habillage final complet. L’aspect du PDF — héros de dos devant le bureau, pièce frontale, palette Endesga 32 et HUD sombre — reste la référence. Certains packs sont top-down, vectoriels ou non pixel ; ils sont classés en complément/réserve pour éviter un assemblage incohérent. « Prioritaire » signifie recommandé pour l’intégration, pas obligation d’utiliser chaque fichier.

## Utilisation immédiate

1. Décompresser `STARTUPRUSH_ASSETS_ET_PROMPT.zip`.
2. Copier/fusionner son dossier `assets` à la racine du dépôt du jeu. Ne pas créer `assets/assets` et ne pas supprimer les ressources déjà présentes.
3. Donner `PROMPT_CLAUDE_STARTUPRUSH.md`, ce catalogue et le PDF à Claude dans l’environnement cloud connecté au dépôt.
4. Claude doit analyser les fichiers, sélectionner/découper les sprites et intégrer les ressources au code. Les noms de fichiers et licences sont conservés.

## Bibliothèque fournie

Les liens ci-dessous sont les pages sources officielles de chaque pack, et non des listes de résultats. Les fichiers sont déjà dans l’archive.

| Pack / auteur | Licence | Priorité | Usage et limite |
|---|---|---|---|
| [Pixel Office Asset Pack](https://2dpig.itch.io/pixel-office) — 2dPig | CC0-1.0 | Prioritaire | Sprites de bureau, personnages, petits animaux et sources Aseprite. Découper les objets et harmoniser perspective/palette. Ne pas coller la scène exemple comme fond final. |
| [Pixelify Sans](https://github.com/google/fonts/tree/main/ofl/pixelifysans) — Stefie Justprince | OFL-1.1 | Alternative | Pixelify Sans variable ; accents français vérifiés. Vérifier la prise en charge du fichier variable dans le projet. Conserver OFL.txt. |
| [Press Start 2P](https://github.com/google/fonts/tree/main/ofl/pressstart2p) — CodeMan38 | OFL-1.1 | Prioritaire | Press Start 2P ; accents français vérifiés dans le TTF fourni. Titres courts ; police large qui devient fatigante en petits paragraphes. Conserver OFL.txt. |
| [Silkscreen](https://github.com/google/fonts/tree/main/ofl/silkscreen) — Jason Kottke | OFL-1.1 | Alternative | Silkscreen Regular ; accents français vérifiés. Alternative pour libellés et petits titres. Conserver OFL.txt. |
| [VT323](https://github.com/google/fonts/tree/main/ofl/vt323) — Peter Hull | OFL-1.1 | Prioritaire | VT323 ; accents français vérifiés. Texte et chiffres à taille suffisante ; tester le rendu Avalonia. Conserver OFL.txt. |
| [Casino Audio](https://kenney.nl/assets/casino-audio) — Kenney | CC0-1.0 | Complément | Cartes, jetons et interactions de casino. Tirages et événements ; sons organiques à doser pour rester cohérent avec le chiptune. |
| [Digital Audio](https://kenney.nl/assets/digital-audio) — Kenney | CC0-1.0 | Complément | Bips et effets synthétiques numériques. Erreurs, alertes, bug en production ; sélectionner après écoute. |
| [Game Icons](https://kenney.nl/assets/game-icons) — Kenney | CC0-1.0 | Réserve | Icônes génériques en plusieurs formats et variantes. À rasteriser/adapter si besoin ; éviter le mélange vectoriel lisse et pixel art. |
| [Game Icons Expansion](https://kenney.nl/assets/game-icons-expansion) — Kenney | CC0-1.0 | Réserve | Compléments de symboles et commandes. Même avertissement : cohérence graphique avant quantité. |
| [Input Prompts](https://kenney.nl/assets/input-prompts) — Kenney | CC0-1.0 | Réserve | Grande bibliothèque de touches/manettes et variantes. Préférer les deux packs pixel. Les milliers de fichiers comprennent des variantes/formats, pas autant de concepts uniques. |
| [Input Prompts Pixel](https://kenney.nl/assets/input-prompts-pixel) — Kenney | CC0-1.0 | Prioritaire | Glyphes clavier/souris et contrôles en 16 px. Espace, Entrée, R, Échap, F11 et choix 1-4 ; garder les images nettes. |
| [Input Prompts Pixel 1 Bit](https://kenney.nl/assets/input-prompts-pixel-1-bit) — Kenney | CC0-1.0 | Alternative | Version monochrome des glyphes en 16 px. Alternative teintable ; ne pas compter ses variantes comme de nouvelles fonctionnalités. |
| [Interface Sounds](https://kenney.nl/assets/interface-sounds) — Kenney | CC0-1.0 | Complément | Confirmations, clics, ouverture/fermeture et transitions. Alternative à UI Audio, pas une obligation de tout mélanger. |
| [Kenney Fonts](https://kenney.nl/assets/kenney-fonts) — Kenney | CC0-1.0 | Alternative | Polices distribuées par Kenney sous CC0. Choisir selon lisibilité réelle ; le jeu ne doit pas utiliser toutes les polices. |
| [Particle Pack](https://kenney.nl/assets/particle-pack) — Kenney | CC0-1.0 | Réserve | Textures de particules, étincelles, fumée et lumières. Ce pack n’est pas du pixel art strict : utiliser seulement après conversion contrôlée ou créer les particules pixel originales. |
| [Pixel Ui Pack](https://kenney.nl/assets/pixel-ui-pack) — Kenney | CC0-1.0 | Prioritaire | Panneaux, boutons, bordures et variantes 9-slice. Choisir une famille de panneaux ; adapter au HUD sombre et garder les états survol/appui. |
| [Roguelike Characters](https://kenney.nl/assets/roguelike-characters) — Kenney | CC0-1.0 | Réserve | Bibliothèque de petits personnages dans des atlas. À adapter à des employés contemporains ; aucune garantie de portraits des quatre fondateurs. |
| [Roguelike Indoors](https://kenney.nl/assets/roguelike-indoors) — Kenney | CC0-1.0 | Complément | Sols, murs et mobilier dans des atlas pixel. Perspective surtout top-down/3-4 : compatible par sélection ou redessin, pas prêt pour la chambre frontale. |
| [Roguelike Modern City](https://kenney.nl/assets/roguelike-modern-city) — Kenney | CC0-1.0 | Complément | Ville moderne, façades, véhicules et accessoires urbains. Réserve pour vues extérieures composées ; ne pas placer une carte top-down derrière la fenêtre. |
| [Tiny City](https://kenney.nl/assets/tiny-city) — Kenney | CC0-1.0 | Complément | Tuiles de ville moderne 16×16. Intérêt pour progression urbaine et bâtiments de l’Empire ; ne pas changer la caméra du jeu. |
| [Tiny Town](https://kenney.nl/assets/tiny-town) — Kenney | CC0-1.0 | Réserve | Petite bibliothèque de bâtiments et environnement. Plus village que startup ; retouches nécessaires et perspective à respecter. |
| [Ui Audio](https://kenney.nl/assets/ui-audio) — Kenney | CC0-1.0 | Prioritaire | Clics, survols, interrupteurs et interactions. Choisir une seule famille sonore et limiter le nombre de sons par clic. |
| [4 Chiptunes Adventure](https://opengameart.org/content/4-chiptunes-adventure) — SubspaceAudio / Juhani Junkala | CC0-1.0 | Prioritaire | Quatre compositions chiptune, avec versions OGG et WAV. Les deux formats sont des doublons musicaux : neuf compositions au total avec le pack Action, pas treize. |
| [5 Chiptunes Action](https://opengameart.org/content/5-chiptunes-action) — SubspaceAudio / Juhani Junkala | CC0-1.0 | Prioritaire | Cinq compositions chiptune en boucle. Plus énergiques ; sélectionner celles qui ne fatiguent pas sur une longue run. |
| [512 Sound Effects](https://opengameart.org/content/512-sound-effects-8-bit-style) — SubspaceAudio / Juhani Junkala | CC0-1.0 | Prioritaire | Banque de 512 effets synthétiques rétro. Achats, critiques, argent, alertes, cartes et fins de run. Écoute et sélection indispensables. |
| [CC0 Document Icons](https://opengameart.org/content/cc0-document-icons) — AntumDeluge; upstream credits included | CC0-1.0 | Complément | Icônes de papiers, documents et calendrier, plusieurs familles. Choisir les documents modernes ; conserver les notices amont déjà dans les archives. |
| [Office objects](https://opengameart.org/content/office-objects) — malek elsady | CC0-1.0 | Complément | Ordinateur, horloge, tableau et accessoires sur une planche PNG. Petits sprites épars ; découpe et retouches nécessaires. |
| [Office Stuff](https://opengameart.org/content/office-stuff) — MSavioti | CC0-1.0 | Complément | Six petits PNG : tableaux, graphiques et scènes de réunion. Affiches murales ou icônes ; pas un tileset complet de bureau. |
| [Old Computer Hardware](https://opengameart.org/content/old-computer-hardware) — edomin | CC0-1.0 | Complément | Composants, cartes et périphériques, séparés et en atlas. Utile pour les premiers upgrades ; références historiques/marques à neutraliser pour les objets génériques. |
| [RPG portraits](https://opengameart.org/content/rpg-portraits) — Buch | CC0-1.0 | Réserve | Neuf portraits 32×32 en trois variantes de couleur. Fantasy : seulement comme base modifiable pour les employés. Les fondateurs doivent rester reconnaissables et contemporains. |
| [UI Sounds](https://opengameart.org/content/ui-sounds-0) — HaelDB | CC0-1.0 | Réserve | Sons d’erreur, acceptation, notification et attention. Alternative de notifications, pas à empiler avec toutes les autres banques. |

## Couverture des besoins du jeu

| Besoin | Ressources à utiliser | État réel |
|---|---|---|
| Bureau, chaises, écrans, plantes, café et petits accessoires | Pixel Office, Office objects, Indoors | Base disponible ; palette, découpe et perspective à harmoniser. |
| Pièce frontale avec héros de dos | Scène existante du jeu + dérivés des objets fournis | Aucun pack ne reproduit exactement cette composition ; préserver la caméra et compléter les sprites. |
| Logements et vues par la fenêtre | Modern City, Tiny City/Town, éléments existants | Bibliothèques de ville surtout top-down ; vues frontales riches/île privée à composer ou dessiner. |
| Employés et quatre fondateurs nommés | Personnages Pixel Office + portraits existants | Les fondateurs exacts et leurs animations ne sont pas livrés clés en main. Adapter/créer sans changer leur identité. |
| Chat de bureau | Animaux de Pixel Office | Dessins disponibles ; état animé à vérifier/créer. |
| Ordinateur de départ et composants | Pixel Office + Old Computer Hardware | Convient aux débuts ; équipements avancés/quantique à compléter. |
| Boutique, menus, cartes d’atouts | Pixel UI Pack + icônes | Panneaux et états fournis ; illustrations spécifiques de chaque atout à mapper ou créer. |
| Contrats et factures | Document Icons, UI Pack, polices | Icônes et conteneurs disponibles ; texte et règles restent rendus par le jeu. |
| Bourse et courbe de patrimoine | Rendu programmatique pixel du jeu | Les courbes doivent refléter les données : pas d’image statique d’un graphique à coller. |
| Entreprises et holding | Bibliothèques de ville + icônes, originaux du jeu | Pas une série complète de logos pour chaque entreprise ; créer des emblèmes fictifs cohérents. |
| Pièces/lingots, critiques, statuts et crises | Ressources existantes + icônes/particules adaptées | Choisir ou créer les sprites exacts ; particules géométriques pixel possibles sans image externe. |
| Pluie de l’écran titre | Animation procédurale pixel + décors | À réaliser dans le code, pas à remplacer par une photo ou vidéo. |
| Sons de clic, contrat, achat, erreur et récompense | 512 Retro SFX, UI Audio, Digital Audio, Interface Sounds | Banque abondante ; audition, choix, niveaux et déclenchement restent à faire. |
| Musique de menu/run/crise/victoire | 5 Action + 4 Adventure | Neuf compositions ; boucle et adéquation musicale à écouter dans le jeu. |
| Titres, corps et chiffres français | 4 polices OFL + Kenney Fonts CC0 | Présence des glyphes `éèêàâùûçÉÀœ€` vérifiée pour les quatre TTF Google Fonts ; rendu Avalonia non testé. |
| Raccourcis clavier/souris | Input Prompts Pixel ou 1-Bit | Glyphes fournis ; ne signifie pas prise en charge manette dans le code. |

## Cohérence artistique : choix recommandé

Prendre Pixel Office comme banque thématique, Pixel UI Pack pour une seule famille de bordures, la scène existante comme référence de perspective, et les SFX rétro comme famille sonore principale. Les autres packs servent à combler les trous. Les captures du PDF ont une scène frontale ; plusieurs bibliothèques trouvées sont vues du dessus. Les licences permettent de les adapter, mais cela ne fait pas disparaître le travail de dessin et d’intégration.

Les visuels de présentation Kenney peuvent inclure un logo. Ils servent uniquement à consulter le pack. Claude doit utiliser les sprites/atlas de production et ne pas inclure les logos, fichiers `.url` ou scènes promotionnelles dans le build.

## Licences et sources modifiables

- CC0 : page de l’auteur vérifiée ; licences originales conservées quand fournies. Le texte CC0 général est dans `LICENSES/CC0-1.0.txt`. Les notices de provenance ne réattribuent pas les droits et ne constituent pas une garantie générale sur d’éventuelles marques représentées.
- OFL : `OFL.txt` et `METADATA.pb` accompagnent chaque famille. Conserver copyright/licence, respecter les noms réservés si une police est modifiée, ne pas vendre une police isolément. Cela n’impose pas de publier le code du jeu sous OFL.
- Les PNG et atlas sont modifiables, mais ne sont pas nécessairement des sources multicouches. Pixel Office comprend effectivement des fichiers `.aseprite`. Aucun fichier source en couches absent n’est promis.
- Les fichiers d’origine n’ont pas été recolorés ou intégrés dans cette livraison. Le futur travail de Claude doit garder originaux et dérivés séparés.
- Tous les packs inclus sont téléchargeables gratuitement et placés sous une licence libre identifiée. Les annonces « free commercial » sans licence libre claire n’ont pas été incluses.

## Quantités : ne pas confondre fichiers et assets uniques

Les nombres annoncés sur les pages éditeurs peuvent compter des sprites au sein d’un seul atlas, des variantes de couleur ou des formats. L’inventaire compte les fichiers physiques et leurs SHA-256, sans promettre autant d’objets différents. Les variantes OGG/WAV des morceaux ne sont pas des compositions supplémentaires. Plusieurs banques UI peuvent se recouvrir.

## Ce qui a été vérifié et ce qui reste à faire

Téléchargements depuis les pages auteurs, validité CRC des ZIP téléchargés, extraction contrôlée, présence des fichiers, licences annoncées, notices incluses, aperçu visuel de plusieurs packs et couverture des caractères des quatre polices. Aucune compilation ni intégration dans StartupRush n’a été réalisée ici : le code du jeu n’a pas été fourni dans cette demande. L’audio n’a pas été auditionné ; la sélection finale et les boucles doivent être testées par Claude. Tous les sprites de toutes les planches n’ont pas été inspectés un à un.

Le catalogue n’affirme pas couvrir chaque sprite unique d’un jeu commercial final. Les éléments spécifiques encore à produire sont identifiés dans la matrice ci-dessus et dans le prompt.

## Traçabilité dans l’archive

- `ASSET_PACKS.json` : source, auteur, licence, rôle, priorité et chemin de chaque pack.
- `ASSET_INVENTORY.csv` : chemin, taille, extension et SHA-256 de chaque fichier du dossier assets.
- `SOURCE.json` dans chaque pack : provenance et usage recommandé.
- `VERIFY_ASSETS.py` : vérification locale facultative des fichiers de l’inventaire, sans réseau.
- `APERCU_ASSETS_STARTUPRUSH.png` : planche de ressources réelles, pas une maquette finale du jeu.
