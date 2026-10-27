# StartupRush — V1 complète : tous les assets utiles, empire tech et boss actif

## À joindre et ordre de priorité

Joindre le dépôt actuel, `Startup_Rush_Guide(1).pdf` **version 2.1** et `StartupRushOriginalAssets.zip`, ou le pack extrait sans modifier ses chemins internes. Ce fichier est un **nouveau prompt complet**, annexe comprise. Il remplace le précédent `PROMPT_CLAUDE_STARTUPRUSH_V1_STEAM.md`, qui limitait à tort les usines et technologies avancées à de futures versions.

Priorité : ces décisions récentes → état réel du dépôt → guide 2.1 pour comprendre la base → documentation technique du pack. Les mentions `usage: future`, les écrans de démonstration « contrats » ou les anciens documents n’annulent pas ce nouveau périmètre.

Tu dois réellement modifier le jeu existant dans ton environnement cloud, conserver C#/Avalonia et pousser le travail sur mon dépôt GitHub avec mon identité vérifiée. Pas de nouveau prototype indépendant, pas de simple plan à la place du développement. Avance par versions jouables, sans réécriture globale du moteur.

## 1. Les décisions du créateur, à respecter

- **Chaque asset du pack doit avoir une fonction réelle**. Une plante contribue au bien-être ; un serveur héberge ; une machine fabrique ; une carte UI présente une information ou permet une action. Les variantes techniques servent au rendu. Charger le fichier ou l’afficher dans une galerie ne suffit pas.
- Une seule entreprise du début à la fin, plusieurs métiers et sites. Pas de filiales obligatoires ni de comptabilités indépendantes. La croissance peut continuer tant que l’offre de bâtiments à louer le permet.
- Le maximum de produits et activités tech cohérents que permettent les assets présents, y compris matériel, production industrielle, création de sa propre IA, services de calcul et quantique.
- Créer du matériel reste simple : choisir le type/public et quelques composants, développement puis fabrication. Pas de conception de circuit au composant près.
- Usine de complexité intermédiaire : machines, étapes, connexions et goulets visibles, sans devenir un autre Factorio.
- Approvisionnement automatique sous plafond réglable ; quantités et réserves contrôlables. Canaux de vente à débloquer : boutique en ligne et distributeurs, sans simulation de magasins physiques.
- Recherche par projets choisis, avec chercheurs, matériel, budget et durée. Les laboratoires servent réellement.
- Plusieurs sites gérables depuis une vue commune ; chacun peut être visité. Pas de déplacement obligatoire pour agir.
- Personnages statiques à leur poste ou machine, progression visible ; aucune animation de marche exigée. Les employés continuent automatiquement.
- **Le fondateur représente désormais le joueur : il ne développe plus tout seul.** Son travail productif passe par un terminal de saisie mot par mot. Il répond encore automatiquement aux avis lorsqu’il a du temps.
- D’autres mini-jeux courts accompagnent les grosses actions, avec des variantes adaptées au type de projet. Ils doivent devenir moins fréquents grâce à la délégation et l’automatisation, et ne jamais envahir toute la partie.
- L’arbre inclut terminal, efficacité du boss, délégation, automatisation, matériel, industrie, IA, cloud et recherche avancée.
- Ambiance détendue, progression satisfaisante, texte réduit ; difficulté surtout financière. Pas de contrats clients, roguelike ou disparition de sauvegarde.

**Interprétation de rythme choisie pour cette réalisation :** viser environ 10–15 heures de jeu réel pour découvrir les grandes branches, sans fin obligatoire ni attente artificielle jusqu’à la dixième heure pour commencer l’industrie. C’est une cible à ajuster après essais, pas une durée garantie. Ne présente pas cette interprétation comme une mesure du jeu actuel.

## 2. Audit de la base : conserver ce qui existe

Le guide 2.1 décrit déjà :

| Présent dans le guide | Traitement attendu |
| --- | --- |
| Départ appartement, 18 000 CHF, ~2 160 CHF de frais mensuels | Conserver comme référence ; rééquilibrer pour le travail actif du boss |
| Applications par abonnement, jeux vendus ; communauté/marketplace à débloquer | Préserver, différencier et étendre |
| Public, nom, composants initiaux, prix proposés | Garder une création rapide |
| Design → développement → tests automatiques | Maintenir pour les employés ; remplacer la contribution automatique du fondateur |
| Files, ordre de priorité, affectation auto ou projet précis | Étendre aux nouvelles tâches et métiers |
| Composants à niveaux, dépendances, coût de suivi | Préserver et enrichir sans éditeur de pages |
| Avis causaux et actions de résolution | Préserver, étendre au matériel, cloud et IA |
| Prix, marketing, souhaits des jeux, entretien, hébergement plafonné | Relier à l’économie étendue |
| Recrutement, horaires, congés, formation, augmentation | Préserver la gestion simple et automatique des formalités |
| Cinq branches de développement et points sur vrais jalons | Étendre le graphe ; pas d’achat automatique de tout dans un ordre imposé |
| Bureaux, cartons, déménagement, trois postes au départ | Transformer en patrimoine multisite et préserver les objets |
| Finances et faillite progressive | Conserver avec agrégation de toutes les activités |
| Projets/Équipe/Aménagement/Développement/Finances, journal | Conserver cette lisibilité ; utiliser sous-vues contextuelles pour les nouvelles activités |
| Continuer, charger, sauvegardes nommées et réglages | Conserver, fiabiliser et migrer |

Lis le code pour distinguer implémenté, partiel et seulement décrit. Produis une matrice d’écarts avec les vrais chemins de code. Le guide et les captures ne prouvent pas qu’un système fonctionne. Ne reconstruis pas un système existant qui satisfait déjà le besoin.

## 3. Boucle de jeu et progression

**Imaginer → développer → publier ou fabriquer → vendre et exploiter → lire les retours → améliorer → déléguer → ouvrir une nouvelle activité.**

Au départ, on crée depuis une chambre de passionné d’informatique, avec un équipement modeste. Le joueur travaille dans son terminal et prend quelques décisions simples. Les premières recettes permettent d’améliorer ses outils, puis d’engager une personne. La délégation libère du temps pour les lancements, l’aménagement et les nouveaux projets.

Ensuite on choisit ses axes : studio de jeux, applications, IA, hébergement, produits matériels, production, recherche. Les branches se renforcent : logiciels installés sur son matériel, GPU utilisés pour entraîner une IA, services hébergés dans son datacenter, recherche améliorant ses produits. Ces synergies donnent des options ; elles ne rendent pas obligatoire de posséder toute la chaîne.

Rester une petite équipe spécialisée doit être viable. Les capacités externes et les composants achetés permettent d’explorer sans construire immédiatement une usine ou un datacenter. Les activités avancées se débloquent par des jalons de réussite, recherche et investissement ; pas par des heures d’attente réelles.

Jalons de rythme proposés, à mesurer et ajuster : première session de travail dès les premières minutes ; premier lancement dans les 10–20 premières minutes ; début de délégation dans la première heure ; premières branches nouvelles dans les 1–3 h ; atelier/production et infrastructure progressivement dans les 3–6 h ; recherche et expansion dans les 5–10 h ; technologies avancées autour de 8–15 h. Ce sont des cibles de conception souples, qui ne doivent pas remplacer des tests ni bloquer une stratégie spécialisée.

Les performances financières et la complexité de portefeuille, pas une quantité imposée de mots tapés, pilotent la progression. Pas de plafond d’argent qui déclenche une fin forcée.

## 4. Le boss actif et son terminal

### 4.1 Rôle exact

Supprime la contribution de développement automatique du fondateur. Hors travail actif et décisions ponctuelles, il peut gérer automatiquement une part du support/réponses aux avis ; il ne fabrique, ne recherche et ne termine pas des composants en secret. Les employés gardent leur fonctionnement automatique.

Le boss reste représenté à son poste, avec état « terminal », « support » ou « disponible ». Aucun déplacement animé requis pour lancer une action à distance. Les contraintes d’horaires/énergie restent compréhensibles et ne forcent pas à rester devant un personnage qui dort sans option utile.

### 4.2 Saisie mot par mot

Ouvrir « Travailler » sur une tâche éligible affiche le terminal du pack. Un mot est montré ; le joueur le tape puis le valide ; le suivant apparaît. Affiche mot courant, saisie, erreurs locales, progression utile et rendement. Listes de mots originales, courtes, lisibles et adaptées au thème, sans connaissance de programmation nécessaire.

Ce terminal est **entièrement simulé**. Aucune commande n’est exécutée sur l’ordinateur, aucun shell, appel externe, fichier arbitraire ou vrai entraînement d’IA. Les scripts de build du développeur sont distincts de ce jeu de saisie.

Chaque validation attribue un montant borné de travail au stade actif d’une vraie tâche. Pas de CHF directement par touche. Le résultat est visible dans le même projet que celui des employés. Les mots ne doivent pas faire progresser un composant déjà fini ni attribuer deux fois le travail.

Vise des sessions de 20–45 secondes, arrêtables à tout instant avec progression acquise conservée. Le jeu n’exige pas de retaper une liste entière après une interruption. Une faute se corrige ; pas de perte de projet ou d’argent, combo spectaculaire ou punition disproportionnée pour une faute de frappe. La vitesse augmente le rendement, mais taper lentement reste viable.

Le boss peut contribuer au design, code, tests, prototypage et préparation de recherche via des contextes adaptés, sans devoir jouer cinq fois le même mini-jeu pour livrer une petite modification. C’est un budget de travail distribué aux phases autorisées, pas une multiplication de barrières manuelles.

### 4.3 Temps et cohérence

Définis explicitement un seul comportement temporel : pendant une session active, la simulation d’entreprise continue à vitesse normale, avec son statut visible ; aucune facture ne s’accélère parce que le joueur tapait à vitesse ×4. À la fermeture, restaure exactement pause/vitesse antérieures. La vraie pause interrompt aussi le mini-jeu. Un réglage d’accessibilité peut mettre l’entreprise en pause pendant la session sans modifier le rendement par mot ni créer une boucle de monnaie gratuite.

Ne multiplie pas le travail de saisie par l’accélération du temps. Empêche les doubles sessions, récompenses répétées, travail hors horaires non prévu et progression après fermeture. Les raccourcis globaux P/T/B/D/F/N/Espace/1–4 ne s’exécutent pas pendant la saisie. Gère clavier français, focus, retour arrière et événements IME ; la validation ne doit pas pouvoir être réémise en boucle par le maintien d’Entrée.

Si un employé travaille sur la même tâche, réserve une contribution distincte et borne le total ; le boss ne remplace pas son avancement et ne reçoit pas une seconde fois les points d’achèvement.

### 4.4 Amélioration du terminal et délégation

L’arbre débloque : rendement du travail actif, confort de saisie, outils de test, macros simulées limitées, scripts de préparation, délégation d’étapes, procédures de publication, supervision à distance et automatisation avancée. Le matériel du poste agit avec un plafond clair. Une amélioration doit modifier une vraie capacité et son indicateur.

Les macros ne sont pas une méthode cachée de remettre le fondateur en production passive infinie : elles augmentent l’effet des interactions ou transfèrent des tâches à une ressource d’entreprise payée. Les employés et les procédures débloquées sont la voie principale d’automatisation.

Prévois une option d’accessibilité d’entrée simplifiée, à effort réel réduit, qui n’exige pas une frappe rapide pour réussir la campagne. Son fonctionnement doit être honnêtement annoncé, sans être activé par défaut à la place du terminal demandé.

## 5. Mini-jeux de grosses actions, puis délégation

Les mini-jeux s’ouvrent à la demande ou à une étape majeure, jamais à chaque fin de tâche ou petite mise à jour. Vise 15–40 secondes, une règle comprise en une phrase, progrès conservé ou nouvelle tentative non punitive. Pas d’arcade réflexe exigeante.

Implémente quelques familles réutilisables avec variantes contextualisées :

| Contexte | Mini-jeu proposé | Conséquence limitée |
| --- | --- | --- |
| Publication d’application/plateforme | Relier quelques modules de déploiement dans un ordre logique visible | Réduire le délai de mise en ligne ou le surcoût initial |
| Lancement d’un jeu | Identifier une courte série d’incohérences de test à partir d’indices simples | Bonus plafonné de stabilité au lancement |
| Prototype ou première série matérielle | Calibration : aligner quelques réglages dans une zone cible | Ajuster rendement/qualité de la première série |
| Première version d’IA | Associer échantillons de test et résultats attendus selon une règle claire | Meilleure validation initiale, jamais puissance infinie |
| Recherche avancée/quantique | Stabiliser un petit schéma de signaux, sans connaissance scientifique exigée | Réduire une partie de la calibration ou du temps de démarrage |

Le terminal est le moyen régulier de travailler au départ ; ces jeux marquent des moments importants. Ils ne remplacent pas toutes les décisions par des épreuves obligatoires. Une publication standard automatique reste possible, plus lente ou au résultat de base annoncé, via l’employé compétent ou les procédures débloquées. Ne bloque pas un lancement éternellement si le joueur ne veut plus jouer au mini-jeu.

Au début, le joueur fait davantage par lui-même ; ensuite un lancement peut être délégué et les petites mises à jour ne demandent aucune intervention. Ajoute une préférence « me proposer les grosses actions » et un mode « déléguer par défaut » une fois la capacité débloquée. Évite plusieurs sollicitations rapprochées en regroupant les actions. Un terminal de travail actif ne peut pas être interrompu par un second mini-jeu.

## 6. Catalogue de projets et de produits

### 6.1 Création commune

Un parcours court : famille → public/usage → composants de départ proposés → nom/prix et résumé. Ensuite, atelier de composants, version, file de travail, avis et réglages. Les caractéristiques sont agrégées ; aucun éditeur complet de logiciel, site, jeu, schéma électronique ou ligne de code.

Réutilise les assets existants pour représenter les variantes et les icônes de catalogue. Un produit vendu et un équipement placé sont deux objets métier distincts pouvant partager une illustration ; vendre un PC ne doit pas enlever par erreur celui du salarié. Les prototypes ne deviennent pas automatiquement du stock commercial.

### 6.2 Familles numériques

- Applications par abonnement : utilité récurrente, fidélité, résiliation, support.
- Jeux vendus : souhaits, ventes au lancement puis catalogue, qualité et mises à jour. Composants jeu : moteur, menu, progression, sauvegarde, inventaire, classement, multijoueur simulé.
- Applications communautaires : activité, rétention, modération et monétisation annoncée.
- Places de marché : deux populations agrégées, confiance, catalogue, recherche, paiements et commission.
- Outils métier/automatisation et services API : niches d’usage, intégrations, fiabilité et abonnement ou consommation.
- Applications et jeux XR : usage du matériel de test et d’un espace XR ; pas de prise en charge réelle de la VR du joueur.
- Produits d’IA : assistants, outils de génération, analyse/prédiction et API de modèles ; créés dans la simulation décrite plus bas.

### 6.3 Matériel permis par les assets

À rendre réellement concevable et commercialisable : PC assemblés, PC compacts et stations spécialisées, ordinateurs portables ; serveurs et stockage/NAS ; cartes électroniques et kits embarqués ; CPU, GPU et RAM ; boîtiers et sous-ensembles ; équipements réseau ; dispositifs de test et kits XR ; équipements de calcul avancés, dont système quantique en fin de progression.

Les machines de laboratoire présentes permettent aussi des lignes de produits spécialisées comme imprimantes 3D ou petits équipements de prototypage. Utilise leur modèle existant comme représentation du produit, avec une recette abstraite crédible ; n’exige pas un asset de carton unique pour chaque référence.

Le maximum signifie des familles avec un intérêt de marché et une chaîne réalisable, pas un catalogue de centaines de copies. Un même squelette de produit paramétré peut couvrir plusieurs gammes. Aucun produit n’est annoncé comme vendable si sa recette, capacité de fabrication, prix, débouché, stock et retour utilisateur sont absents.

Les boîtiers, puces et cartes sont des entrées achetables au début et fabricables plus tard. L’achat externe ne doit pas demander de contrat client. On peut fabriquer et vendre un sous-ensemble sans produire tous ses intrants. Les matières fondamentales restent agrégées : pas de mine de silicium à construire.

## 7. Usine, chaîne intermédiaire et approvisionnement

Toutes les machines de `industry_future` deviennent des équipements jouables dans cette V1, malgré le nom historique de la catégorie.

### 7.1 Deux niveaux utiles

Commence par un atelier d’assemblage : composants achetés → assemblage → installation logicielle → test → emballage → stock commercial. Les salariés assurent automatiquement les postes, après création d’un modèle et ordre de production.

Débloque ensuite la fabrication de cartes et puces :

- Carte : PCB/support acheté → placement de composants → refusion → inspection/test → carte validée.
- Microcomposant : wafer acheté → traitement microélectronique abstrait → contrôle de précision → CPU/mémoire/puce destinée à une carte.
- Carte GPU : puce/calcul + mémoire + PCB validé + composants agrégés → assemblage/test → GPU.
- PC/serveur/équipement : sous-ensembles → assemblage → flash/configuration → stress-test → emballage.

Ces recettes sont des abstractions de jeu, pas des procédés industriels réels. Une même machine peut prendre des recettes compatibles ; chaque recette consomme des stocks, du temps, une capacité et de l’énergie définis.

### 7.2 Placement et flux

Le joueur place et relie les machines dans les pièces prévues, avec convoyeurs droits, courbes, jonctions, table de transfert et fins de ligne du pack. Les ports et orientations viennent du manifeste. Aperçu de flux et diagnostic : « mauvaise entrée », « sortie bloquée », « matière manquante », « opérateur absent », « capacité de refroidissement insuffisante ».

Chaque liaison a un sens, des matières compatibles et un débit. Les buffers sont limités. Pas de téléportation entre deux machines simplement parce qu’elles sont dans le même bâtiment. Les transferts entre sites sont en revanche agrégés, avec délai/coût visibles ; pas de camion à conduire ni d’itinéraire routier.

Une jonction n’invente pas de stock ; une boucle ne génère pas de produit ; une saturation arrête proprement l’étape précédente. Les lots, contrôles qualité et rejets sont comptabilisés. Les convoyeurs et produits peuvent rester des sprites statiques avec indicateur de flux, sans animations nouvelles obligatoires.

Prévois des dispositions conseillées réutilisables et un diagnostic simple, mais ne remplace pas tout par une usine opaque qui tourne sans tenir compte des connexions.

### 7.3 Commandes et stocks

Choix : quantité fixe, stock cible ou production continue sous limites. Achats automatiques selon réserve cible, délai fournisseur et plafond **de dépenses par période clairement définie**, avec récapitulatif avant validation. L’argent est engagé une seule fois ; les matières arrivent après le délai annoncé. Pas de rachat infini parce que la livraison en cours n’a pas été comptée.

Les racks, palettes et bacs définissent des capacités réelles. Un produit non emballé n’est pas expédiable. Affiche stock disponible, réservé, en production et en transit. Gestion unifiée, sans remplir un formulaire par palette.

Les besoins de filtration, salle propre, froid et puissance électrique sont des capacités simples par site/zone, pas de réseau de tuyaux à concevoir. Le sas et les cloisons rendent une zone compatible ; les machines concernées vérifient cette compatibilité. Pas de nouvelles urgences d’entretien manuelles permanentes.

## 8. Vente, canaux et avis

La boutique en ligne et les distributeurs se débloquent et s’activent par produit. Ils ont une visibilité, commission/frais et capacité de vente distinctes ; le joueur compare marge et volume. Le commerce est automatique, pas une série de contrats à accepter.

Les produits physiques ont prix, demande, disponibilité, qualité et coût complet de fabrication. Les invendus ne deviennent pas des revenus. Les retours clients et garanties sont une charge agrégée proportionnée à la qualité ; pas de tickets individuels.

Les avis expliquent des causes calculées : prix trop élevé, rupture de stock, performances insuffisantes, défauts, délai ; pour les services : lenteur, manque de fonctions, support ; pour l’IA : qualité des réponses et délai/coût d’usage. Les retours positifs comptent également. Un bouton ouvre le bon levier.

Concurrence simple, publics limités, marketing à rendement décroissant et produits qui vieillissent donnent un renouvellement sans sabotages arbitraires. Les gains d’un nouveau produit ne sont pas garantis simplement parce qu’il est plus avancé.

## 9. Infrastructure, datacenter et services de calcul

Unifie les ressources : CPU/calcul général, GPU/calcul spécialisé, RAM, stockage, réseau, énergie et refroidissement. Agrège-les pour la lecture, garde des allocations explicables en détails. N’impose pas au joueur un système d’exploitation ou une configuration IP.

Serveurs tours/NAS → racks compacts → baies standard/haute densité → datacenters. Les modules consomment leurs vraies unités de rack et apportent des capacités distinctes. Les obturateurs aident le flux d’air, les passe-câbles/brassage réduisent la surcharge d’organisation, les onduleurs apportent une marge de continuité, le refroidissement permet de soutenir la charge. Rien n’est juste un gros objet qui donne le même +5 % que tous les autres.

Chaque site a une limite électrique/réseau. Fibre, routeurs, switches, brassage et distribution servent cette capacité, avec simplicité et plafonds. Les éléments de câblage organisent les liaisons par zones et améliorent un score d’organisation plafonné ; pas de parcours obligatoire de chaque fil.

Priorités d’usage : exploitation de ses projets, entraînement IA, recherche, capacité vendue à l’extérieur. Le joueur peut ajuster et réserver une marge. Une même unité GPU ou stockage ne doit pas être promise à trois activités à la fois.

Services commerciaux : hébergement web/app, stockage, calcul CPU/GPU et, tardivement, service de calcul quantique pour usages spécialisés. Choisir une offre, prix et capacité allouée ; la demande est agrégée, sans contrats individuels. Les revenus viennent de capacité réellement disponible et demandée.

Hébergement externe et location de calcul restent disponibles sous plafond. Une entreprise de jeux peut prospérer sans posséder un datacenter. Si on réduit une infrastructure, afficher l’impact et proposer migration externe ou réduction d’allocation au lieu de couper silencieusement tous les produits.

## 10. Créer sa propre IA dans le jeu

Une vraie famille de projets de simulation, pas un bouton décoratif dans l’arbre. Tout fonctionne hors ligne ; aucun modèle réel à télécharger ou API payante.

Parcours : objectif/public → base de modèle et composants → données agrégées → calcul d’entraînement → évaluation → version publiable → hébergement et usage → revenus/avis → amélioration.

Variables lisibles : aptitude à l’usage choisi, qualité des données, taille/coût du modèle, calcul nécessaire, latence, consommation par requête. Quelques profils clairs plutôt qu’une console de paramètres techniques. Acheter des ensembles de données simulés ou investir dans leur qualité ; pas d’import de données personnelles réelles.

Une petite IA spécialisée peut réussir face à un gros modèle coûteux. Les GPU et stations existantes accélèrent certaines étapes, la location externe permet de commencer. Les nouvelles versions gardent l’ancienne en service tant que le remplacement n’est pas prêt. L’inférence consomme une part de capacité et plafonne l’audience servie.

L’IA peut être un produit propre ou un composant de ses applications. Facture une seule fois les ressources partagées, et ne compte pas un transfert interne comme une vente externe. Les modèles d’IA restent dans la campagne sauvegardée ; rien n’est entraîné sur le PC du joueur.

## 11. Recherche, laboratoires, studio et XR

La recherche est une file de projets : choix de l’avancée, chercheurs affectés, compétences/équipements requis, coût estimé et durée. Les recherches débloquent une technologie, recette, composant, méthode ou capacité, avec résultat lisible. Pas de recherche qui ne sert qu’à donner accès à une autre recherche sans usage.

La recherche doit être prévisible : pas de disparition aléatoire de tout le budget. Les performances peuvent influer sur durée et qualité, avec borne basse et progression conservée. Un projet commencé peut être suspendu sans effacer tout l’acquis.

Laboratoire électronique : établi, alimentation, soudure, oscilloscope, instruments, microscope ; prototypes et diagnostic agrégés. Prototypage mécanique : imprimante 3D, CNC, laser, calibration et cobot ; amélioration de la mise au point et des procédés. Bancs PC/embarqué valident les familles correspondantes.

Les assets audiovisuels deviennent un module de studio utilisable pour jeux, applications XR et campagnes : caméra, fond vert, éclairage, micro et enceintes produisent une capacité de contenu/qualité de présentation. Aucune obligation d’écrire un scénario ou un morceau de musique ; pas de DAW à intégrer.

Zone XR : casque + balises + tapis + personnel compatible = tests immersifs simulés, qui réduisent les défauts spécifiques et débloquent la gamme. Un casque seul peut servir au prototype, l’ensemble offre une capacité complète.

Quantique : le cryostat ouvert sert à une configuration expérimentale/calibration à rendement de recherche ; le fermé à une installation stabilisée pour les calculs spécialisés/commercialisation. Ce sont deux installations/catalogues compatibles avec les modèles fournis, pas un état « cassé/réparé ». Contrôle, support cryogénique, froid et énergie sont requis. Ne simule pas un vrai cryostat ouvert opérationnel : la représentation ouverte illustre la phase de laboratoire. Le quantique n’accélère pas magiquement toute application ou tout entraînement IA.

## 12. Une entreprise, plusieurs sites visitables

Utilise les **10 plans de pièces** et **6 modules** du pack. Chaque plan a sa place : chambre, petit bureau, bureau moyen, open space, siège, salle serveur compacte, datacenter, atelier matériel, usine et laboratoire avancé. Les modules kitchenette, détente, réunion, quatre postes, accueil et sanitaires doivent être utilisables.

Une vue Sites accessible depuis l’entreprise/aménagement permet de voir activité, effectifs, capacité, loyer, alertes et stocks. Ouvrir un site affiche son intérieur, mais recrutement, affectation, production et urgences restent gérables sans le visiter.

Un parc de locations est défini par données avec plusieurs instances de plans et offres renouvelables à mesure de la progression. Ne limite pas le jeu à dix sites simplement parce qu’il existe dix plans. Affiche les limites réelles de l’offre ; pas de promesse d’infinité technique. La simulation hors vue donne les mêmes résultats et ne charge pas les scènes graphiques de tous les bâtiments.

Louer un autre lieu ajoute un site ; déménager remplace celui choisi. Contrôle prix, caution éventuelle, loyer, disponibilité, capacité et coûts cumulés avant validation. Lors d’une fermeture, reloger/vendre/mettre en cartons sans perte silencieuse, et réassigner ou signaler clairement les ressources touchées.

Les plans et modules peuvent contenir du mobilier de démonstration : distingue ce qui est livré avec la location, acheté en lot ou seulement proposé comme prévisualisation. Ne transforme pas le contenu d’un aperçu en machines gratuites revendables, et ne facture pas deux fois un équipement inclus dans un lot.

## 13. Chaque meuble et décoration sert, sans corvées

Le mobilier influence quelques indicateurs regroupés : confort/bien-être, récupération, concentration, formation/coordination, rangement/capacité, qualité de test et aptitude d’une zone. Affiche surtout une synthèse par site et les contributions sur demande.

Le lit de la chambre aide la récupération du fondateur hors session ; canapés, fauteuils, coin détente, boissons et nourriture soutiennent l’énergie et le moral de façon agrégée. Sanitaires et rangement contribuent au confort de base. Plantes et objets personnels améliorent l’ambiance ; trophées matérialisent des jalons et la fierté. Tableaux, livres et salles de réunion soutiennent formation/design/coordination.

Ces effets sont plafonnés, avec rendements décroissants et règles de couverture/occupation : cent plantes ne doublent pas la vitesse de code, cent tasses ne remplacent pas des congés. Pas de différence de performance liée au genre, à la couleur de peau ou à la coiffure. Leurs assets servent à la représentation et à la personnalisation.

Les objets domestiques ne doivent pas devenir obligatoires dans chaque usine, et une absence de poubelle ne doit pas provoquer une catastrophe. Les lots de mobilier ou modules peuvent satisfaire des besoins communs. Le joueur aménage pour progresser et personnaliser, sans commander du savon ou remplir des distributeurs un par un. Les consommables ont au plus un coût agrégé annoncé.

Les murs, sols, motifs et cloisons servent au plan et à l’identification des zones, avec effets raisonnables quand ils ont un sens : acoustique, salle propre, isolation visuelle, refroidissement de salle technique. Toutes les textures ne doivent pas donner un bonus unique inventé ; partager une fonction de revêtement/personnalisation réellement utilisée est une utilité valide.

## 14. Arbre de développement cohérent

Conserve les acquis utiles des cinq branches historiques et étends-les en sous-branches lisibles :

- Création : familles numériques, qualité, matériel, IA et composants.
- Organisation : formation, confort, coordination, multisite et délégation.
- Infrastructure : réseau, hébergement, baies, calcul GPU, puissance et refroidissement.
- Commercial : publics, marketing, boutique, distributeurs, offres cloud et marges.
- Innovation : prototypage, procédés industriels, laboratoires et quantique.
- Outils du boss, visibles comme une sous-branche identifiable : terminal, rendement, tests, préparation de lancement et automatisation.

L’arbre n’est pas un paiement de points à la place de tout le gameplay. Un nœud peut débloquer une capacité dont l’usage demande ensuite équipement, équipe et budget. Chaque verrou indique les conditions manquantes, avec un lien vers la bonne vue.

Points sur accomplissements réels, une fois chacun : public atteint, qualité stabilisée, rentabilité, première série vendue, capacité cloud exploitée, première IA utile, recherche terminée. Pas de farming en lançant puis supprimant des projets vides. Débloquer tout le terminal ne doit pas rendre les employés inutiles.

## 15. Graphismes et intégration technique du pack

Le manifeste de référence contient **570 entrées** : HUD 224, mobilier 55, composants de projets 51, ordinateurs 32, industrie 32, laboratoires 27, serveurs 26, décoration 25, terminal 21, personnages 18, effets 16, sols 13, curseurs 12, architecture 8, motifs muraux 8, polices 2. Ce ne sont pas 570 machines indépendantes. Vérifie le compte si le pack a changé.

Lis `INTEGRATION_GUIDE.md`, `STYLE_GUIDE.md`, `COLOR_SYSTEM.md`, `QA_REPORT.md`, `PROVENANCE.md`, `manifest.json`, les inventaires, les plans et les exemples sources. Le pack de référence a déjà subi des contrôles d’intégrité des images et références ; cela ne prouve pas son fonctionnement dans Avalonia. Teste le vrai rendu du jeu.

Scène en trois quarts 2:1, cases natives 64×32 : `X=(U−V)*32`, `Y=(U+V)*16`, plus origine. Caméra déplaçable/zoomable, sans rotation. Les objets peuvent prendre les orientations du manifeste. Respecte ancre, emprise, collisions, accès, attaches, occupation, calques et masques. Le tri des emprises multicases doit éviter les erreurs de profondeur des objets longs.

Recoloration par canaux/rampes, pas de duplications massives de PNG. Ombres composées au maximum des alphas puis opacité commune de 30 %, selon le guide. Racks en unités de hauteur avec compatibilités, couches de verre et objets attachés dans le bon ordre. Le fauteuil, le bureau et le personnage ne flottent pas entre eux.

Personnages 40×72 avec ancre de siège `(20,50)` et couches cheveux arrière → corps → tête → cheveux avant. Les travailleurs aux machines n’exigent pas de nouveau sprite debout : utilise un poste opérateur assis adjacent/attaché et l’icône d’activité ; ne déforme pas un sprite assis pour prétendre qu’il marche. Apparences sauvegardées, variations aléatoires seulement à la création.

HUD **Papier** crème/encre avec les tokens turquoise/corail/jaune du pack, pas l’ancien HUD sombre ni le noir/violet de mon autre application. Utilise les vrais nine-slices, états et marges. `nine_slice=[gauche,haut,droite,bas]` ; les états peuvent être des objets et non des noms de fichiers simples.

Police StartupRush Pixel pour l’interface et Mono pour terminal/données ; vérifie accents, symboles monétaires et lisibilité. Sprites nets, échelles adaptées, contrôles utilisables aux DPI Windows 100/150/200 %. Les composants restent de vrais contrôles reliés aux données, jamais une capture aplatie en guise de menu.

Tous les curseurs/états/effets ont un usage contextuel. Les orientations, masques, atlases, ombres et variantes ne deviennent pas des produits séparés. Les anciens pictogrammes « client » ou « contrat » sont réemployés pour clientèle ou conditions fournisseurs/canaux, sans retour des prestations clients.

## 16. Lisibilité avec davantage de systèmes

La scène reste au centre. Barre haute : date/temps, argent, résultat mensuel et « À traiter ». Navigation principale compacte, basée sur Projets, Équipe, Aménagement/Sites, Développement et Finances. Projets regroupe les créations ; une fiche matérielle expose sa production ; une fiche cloud expose sa capacité. Ne colle pas un sixième, septième et huitième tableau en permanence juste parce que le jeu s’agrandit.

Un bouton contextuel clairement nommé ouvre le terminal du boss. Les nouvelles rubriques apparaissent avec leur déblocage ; l’écran de départ ne montre pas les jauges du quantique ou dix stocks à zéro.

Chaque vue répond à : « Que se passe-t-il ? », « Pourquoi ? », « Que puis-je faire ? ». Une action principale, peu d’indicateurs, détails dépliables. Les alertes récurrentes se regroupent et disparaissent après résolution. Les bonnes nouvelles sont également visibles.

Recherche/filtres dans catalogues et portefeuille, tri par état/site/type et favoris possibles. Échap remonte/ferme/pause sans quitter. Pas de raccourcis de menus pendant une saisie. Frais ponctuels, mensuels et engagements futurs sont distingués. Les légendes et les pictogrammes empêchent de dépendre uniquement des couleurs.

Le début guidé est court, contextuel, désactivable : créer, ouvrir le terminal, voir le travail produit, lancer, lire un avis, améliorer, recruter puis déléguer. Aucun manuel interminable à lire avant le premier mot.

## 17. Finances, simulation et sauvegardes

Un seul bilan pour l’entreprise, détails par site, activité et projet. Charges : personnel, locations, énergie, hébergement/calcul externe, matières, marketing et exploitation. Ventes et paiements ne sont comptés qu’une fois. Les composants consommés, stocks réservés et transferts internes ne créent pas de recettes.

Les simulations à vitesses différentes donnent un résultat équivalent sur une durée de jeu identique, hors interactions manuelles explicitement dépendantes du temps réel. L’entreprise hors champ continue. Les jours de paie, congés, formation, livraisons et réservations ne sautent pas à vitesse élevée.

Garde l’échec financier progressif du guide : impayés, mise en demeure après 20 jours, puis faillite 30 jours plus tard si non résolu ; rends les dates de départ/fin explicites. Préviens et propose des leviers concrets. Ni faute de terminal, ni mini-jeu raté, ni décoration manquante ne déclenche une faillite immédiate.

Migration des sauvegardes 2.1 avec copie de sécurité et rapport de conversion. Les fondateurs existants perdent leur production passive selon cette nouvelle règle, sans perdre compétences, équipements ni progression. Les anciens locaux sont convertis en premier site. Si certaines données manquent, valeurs par défaut prudentes et expliquées, pas de remise à zéro de la campagne.

Sauvegarde aussi : fondateur/support, files et contributions, apparences, projets/versions, arbre et récompenses déjà reçues, sites/objets/teintes, racks/modules/allocations, recherches, IA simulées, recettes, lignes/ports/buffers, stocks/transit, ordres et achats engagés, canaux et réglages de délégation. Une session active peut être interrompue proprement avec son travail déjà validé conservé, sans persister des timers incohérents.

Sauvegardes manuelles multiples, autosaves tournants, écritures atomiques et gestion disque plein/refus d’écriture. Ne supprime pas une partie après faillite, corruption ou incompatibilité. Dossiers utilisateur séparés des fichiers d’installation, réglages machine séparés des campagnes.

## 18. Exigence particulière : preuve d’usage de tous les assets

L’annexe ci-dessous attribue un usage prévu à chacune des 570 entrées et aux pièces/modules. C’est une spécification, **pas une preuve que ces fonctions sont déjà codées**. Tu dois la transformer en un registre dans le dépôt, par exemple `docs/asset_usage.csv`, relié aux vrais systèmes.

Pour chaque ID, renseigne : catégorie, rôle, écran/recette/objet consommateur, fichier de définition, condition de déblocage, indicateur influencé ou interaction d’interface, état de validation et scénario de test. Pas de lignes fictives « utilisé = oui » ni d’enregistrement de tous les assets à vide pour passer le contrôle.

Une fonction partagée est valide : plusieurs plantes soutiennent un bien-être plafonné, plusieurs coiffures personnalisent les avatars, les angles de lien dessinent un vrai graphe. Un simple chargement, fichier copié, sprite hors écran ou menu de debug n’est pas une utilité dans le jeu livré.

Les variantes d’état, curseurs de redimensionnement, ports orientés et composants rares doivent être atteignables via des interactions normales ou une configuration utilisateur documentée. Tous ne sont pas affichés simultanément. Les utilitaires techniques sont justifiés par leur consommateur réel (rendu, masques, métriques de police, atlas) plutôt que par un bonus de gameplay artificiel.

Ajoute un contrôle des références, des IDs et de la couverture du manifeste, puis des scénarios d’exécution couvrant les catégories. Ce contrôle statique doit être présenté comme statique : il ne prouve pas qu’un objet apporte réellement son effet. Pour les objets de gameplay, vérifie au moins une conséquence avant/après et les limites de cumul ; pour les contrôles UI, vérifie l’action et l’état concernés.

Si un conflit ou une impossibilité réelle apparaît, documente précisément l’ID, le problème et une solution cohérente. Ne reporte pas toute une famille à une roadmap et ne marque pas la V1 complète avec des assets sans fonction.

## 19. Réalisation, validation et sortie Windows/Steam

Travaille par lots reliés, chacun jouable :

1. Audit du dépôt/guide/pack, protection des sauvegardes, registre de couverture.
2. Chambre avec nouveaux assets, HUD, création de projet, terminal du boss, premier lancement/avis et sauvegarde/reprise.
3. Employés automatiques, délégation et mini-jeux de grosses actions sans spam ; arbre et confort utiles.
4. Multisite, réseau, racks, hébergement interne/externe et offres cloud.
5. Recherche/prototypage, gamme matérielle, flux d’usine, stocks, approvisionnement et canaux de vente.
6. IA simulées, studio/XR, calcul avancé et quantique, reliés à la même économie.
7. Couverture de tous les IDs, campagnes d’équilibrage, performance, accessibilité, migrations et candidat de sortie.

Les lots sont un ordre de travail, pas l’autorisation de terminer après le lot 2. Tiens un état d’avancement dans le dépôt pour reprendre une longue tâche sans oubli. N’ajoute pas de mécanismes techniques génériques dont aucun système actuel n’a besoin.

Tests indispensables :

- Fondateur sans session : aucun progrès de développement ; support automatique seulement si disponible. Session ouverte/fermée : travail attribué une fois à la bonne tâche, raccourcis neutralisés, temps restauré.
- Employés et délégation : campagnes viables sans jeu de saisie constant après montée en équipe ; procédures capables de livrer les actions déléguées.
- Flux industriels : sens des ports, stocks conservés, intrants consommés, buffers/stockage bornés, absence de duplication dans boucles/jonctions et reprise après blocage.
- Achats/ventes : plafond respecté, transit compté, marge et stock cohérents, pas de vente d’invendus ni revenu compté deux fois.
- Infrastructure/IA/recherche : capacités réservées une fois, énergie/froid respectés, services exploitables, version précédente toujours active pendant une amélioration.
- Multisite : résultats identiques site visible ou non, locations/fermetures/déménagements sans pertes silencieuses.
- Sauvegarde : aller-retour fidèle, migration 2.1, paiement/récompense non répétés, fichier corrompu/écriture refusée correctement traités.
- Chaque grande branche possède une boucle complète de création/exploitation/revenus ou bénéfice, pas seulement un écran ou déblocage.
- Rendu : ancres, ombres, masques, chaises, opérateurs, objets multicases, variantes HUD, accents et DPI réellement vérifiés.
- Campagnes répétables avec stratégie raisonnable et mauvaise gestion ; pas de travail de frappe artificiellement gonflé pour durer 15 heures.

Préserve les versions utiles de .NET/Avalonia du dépôt ; les exemples C# du pack n’imposent pas leur SDK. Build Release Windows 10/11 x64 reproductible, ressources incluses, pas besoin de SDK sur le PC joueur. Tester un lancement hors dossier de développement et sous utilisateur standard. Si l’environnement cloud ne permet pas une vérification visuelle Windows, le dire et fournir une procédure/CI réellement exécutable ; un build compilé ne vaut pas un test UI.

Prévois réglages d’affichage/échelle, volumes si audio présent, commandes, accessibilité terminal, réduction des effets et notifications. Le jeu est solo et fonctionne hors ligne, sans service externe, compte StartupRush ou connexion à une vraie IA. Préserve l’audio existant si son usage commercial est justifié ; n’invente pas de licence.

Pour Steam : prépare archive, guide à jour, changelog, crédits/provenance, liste des dépendances, vraies captures du jeu final et dossier de boutique factuel. Vérifie les règles officielles actuelles avant de préparer les fichiers de publication. Ne promets ni validation Valve, ni performance, ni compatibilité Steam Deck non testée. Steam Cloud/succès peuvent être préparés mais ne sont pas déclarés opérationnels sans AppID/configuration et essais réels. Aucun AppID, accès ou résultat ne doit être inventé.

Pas de publication Steam ou de modification d’une branche publique dans cette tâche. Les informations de compte, prix, frais, déclarations et soumission finale sont des actions du propriétaire, distinctes du développement à achever. Prépare les renseignements de provenance et contenu généré par IA requis, sans garantie juridique fabriquée.

## 20. GitHub et compte rendu

Branche dédiée sur mon dépôt, commits cohérents **avec mon nom/mail Git vérifiés**, puis push. Le fait d’utiliser le compte Claude d’un ami ne change pas le propriétaire du projet ni l’auteur demandé. Vérifie les remotes et la configuration ; n’invente pas d’adresse. Si identité ou accès manque, demande l’information précise et continue le reste. Pas de force push, suppression de travail, fusion automatique ou secrets dans Git.

Livrer : code et liens de commits, build Windows identifié par commit, registre exhaustif d’usage des assets, documentation des règles et réglages, guide court actualisé, tests/mesures/captures réellement obtenus, problèmes restants et étapes Steam manuelles. Signaler distinctement ce qui est testé, partiel ou non exécuté. Ne pas déclarer « tous les assets utiles » si la seule preuve est un compteur de fichiers chargés.

**Résultat attendu : une seule entreprise persistante, jouable et extensible, où tous les assets trouvent un usage, où le boss commence par travailler activement puis délègue, et où les nouvelles activités apportent du plaisir sans submerger l’interface.**

---

# Annexe — Affectation exhaustive des assets du pack de référence

Les fonctions ci-dessous sont des choix de conception pour cette V1. Elles ne décrivent pas des fonctions déjà vérifiées dans le code. Les contributions de confort, organisation, qualité et efficacité sont plafonnées. Les seuils/coûts chiffrés sont à équilibrer ; ne transforme pas chaque objet décoratif en bonus obligatoire.


<!-- GENERATED_ASSET_MAPPING_START -->

Couverture de conception : **570/570 IDs**, **10 pièces**, **6 modules**. Ce chiffre vérifie seulement l’exhaustivité de cette affectation, pas sa réalisation dans le jeu.


## architecture — 8 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `wall_corner_inner` | Angle interieur (coin du fond) | Raccorder sans trou les murs du fond des pièces louées. |
| `wall_corner_outer` | Angle saillant (retour) | Fermer correctement les retours saillants de cloisons/pièces. |
| `wall_door` | Mur avec porte | Point d'accès à la pièce ; contrôle d'accessibilité logique des postes sans animation de marche. |
| `wall_end` | Terminaison de mur (chant visible) | Terminer les murs visibles et éviter les raccords ouverts dans le rendu des plans. |
| `wall_opening` | Mur avec ouverture (passage) | Passage ouvert entre zones compatibles ; continuité d'accès logique. |
| `wall_plain` | Mur plein | Fermer les pièces prédéfinies et porter couleurs, motifs et objets muraux ; structure de la zone. |
| `wall_window` | Mur avec fenetre | Fenêtre d'une case : lisibilité de la pièce et contribution plafonnée à l'ambiance lumineuse. |
| `wall_window_wide` | Mur avec grande fenetre (2 cases) | Grande ouverture sur deux cases : même fonction de lumière/ambiance avec une emprise différente. |

## characters — 18 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `employee_f_01` | Employe (presentation feminine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_f_02` | Employe (presentation feminine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_f_03` | Employe (presentation feminine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_f_04` | Employe (presentation feminine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_f_05` | Employe (presentation feminine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_m_01` | Employe (presentation masculine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_m_02` | Employe (presentation masculine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_m_03` | Employe (presentation masculine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_m_04` | Employe (presentation masculine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `employee_m_05` | Employe (presentation masculine) | Base visuelle des employés, métiers et portraits persistants ; présentation sans bonus/malus statistique. |
| `hair_bob` | Coiffure bob | Couche de coiffure pour personnaliser les personnages compatibles, couleur/choix sauvegardés. |
| `hair_bun` | Coiffure bun | Couche de coiffure pour personnaliser les personnages compatibles, couleur/choix sauvegardés. |
| `hair_curly_top` | Coiffure curly top | Couche de coiffure pour personnaliser les personnages compatibles, couleur/choix sauvegardés. |
| `hair_long_straight` | Coiffure long straight | Couche de coiffure pour personnaliser les personnages compatibles, couleur/choix sauvegardés. |
| `hair_short_crop` | Coiffure short crop | Couche de coiffure pour personnaliser les personnages compatibles, couleur/choix sauvegardés. |
| `hair_side_part` | Coiffure side part | Couche de coiffure pour personnaliser les personnages compatibles, couleur/choix sauvegardés. |
| `player_f` | Avatar joueur (presentation feminine) | Représentation du fondateur choisi, apparence personnalisable persistante sans différence statistique liée à la présentation. |
| `player_m` | Avatar joueur (presentation masculine) | Représentation du fondateur choisi, apparence personnalisable persistante sans différence statistique liée à la présentation. |

## computers — 32 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `access_point` | Point d'acces Wi-Fi (mural) | Couverture réseau des équipements sans fil d'une zone. |
| `capture_device` | Boitier de capture | Capacité de capture de démonstrations, jeux et campagne vidéo. |
| `console_test_kit` | Kit de test console generique | Tests de compatibilité d'une gamme de jeux console simulée, qualité/accès à ce canal de jeu. |
| `device_test_rack` | Poste de test mobile | Tests d'applications mobiles sur plusieurs profils simulés, réduction de défauts de compatibilité. |
| `docking_station` | Station d'accueil | Étendre la compatibilité des périphériques d'un portable ; sockets réellement utilisables. |
| `fiber_box` | Boitier fibre (mural) | Accès réseau plus performant du site, débit externe amélioré avec coût annoncé. |
| `graphics_tablet` | Tablette graphique | Capacité spécialisée de design/contenu graphique. |
| `headset_stand` | Casque sur support | Confort de concentration/audio-test du poste, contribution plafonnée. |
| `keyboard_mouse` | Clavier et souris | Entrée de travail d'un poste desktop ; doit être comptée par l'ensemble et non rachetée involontairement. |
| `laptop_gaming` | Portable puissant | Portable puissant adapté jeu/test, consommation/coût supérieurs ; gamme vendable. |
| `laptop_old` | Portable ancien | Machine initiale limitée, terminal et travail de base ; produit d'entrée de gamme si pertinent. |
| `laptop_pro` | Portable professionnel | Poste mobile professionnel, efficacité dev/bureautique et gamme matérielle correspondante. |
| `laptop_standard` | Portable standard | Machine généraliste portable à rendement intermédiaire et faible emprise. |
| `mini_pc` | Mini-PC | Poste compact ou petite offre matérielle, faible consommation et emprise. |
| `monitor_arm_second` | Module de deuxieme ecran (bras) | Attache de second écran, amélioration limitée du poste sans créer de CPU. |
| `monitor_basic` | Ecran simple | Affichage de base du poste, condition d'un poste desktop utilisable. |
| `monitor_large` | Grand ecran | Amélioration plafonnée du confort/productivité d'affichage, contre coût et emprise. |
| `monitor_ultrawide` | Ecran ultralarge incurve | Affichage spécialisé travail multivue, bonus plafonné et compatibilité de support. |
| `network_cabinet` | Baie de rangement technique | Rangement/organisation d'équipements réseau et points d'attache compatibles. |
| `network_switch_desk` | Switch de bureau | Extension de capacité réseau locale pour postes et petites installations. |
| `printer_mfp` | Imprimante multifonction | Service de prototypage papier/documentation et organisation, abstrait sans impression réelle. |
| `router` | Routeur | Passerelle réseau du site et plafond de connectivité des services. |
| `speakerphone` | Pieuvre de conference | Équipement de réunion/coordination intersites sans déplacement obligatoire. |
| `tower_office` | Tour bureautique | Machine de bureau standard pour tâches légères/support, coût d'exploitation modéré. |
| `tower_used` | Tour d'occasion | Tour économique d'occasion pour premier salarié, rapport coût/puissance distinct. |
| `videoconf_bar` | Barre de visioconference | Capacité de réunion multisite plus importante, associée à une salle équipée. |
| `webcam` | Webcam | Capacité de collaboration à distance et création de contenu simple. |
| `workstation_compute` | Poste de calcul avance | Poste de calcul avancé pour recherche/prototypage, consommation et capacités distinctes. |
| `workstation_creative` | Station creative | Station spécialisée design/contenu/studio, rendement ciblé. |
| `workstation_dev` | Station de developpement | Station spécialisée développement, rendement ciblé et coût explicite. |
| `workstation_gpu` | Station GPU / IA | Poste de prototypage/entraînement IA et tâches GPU, capacité GPU réservée réellement. |
| `xr_headset_stand` | Casque XR sur support | Prototypage/test XR et représentation de la gamme de kits XR. |

## cursors — 12 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `cursor_action` | Clic / action | Survol d'une action cliquable disponible. |
| `cursor_busy` | Occupé (statique) | Opération locale en cours, sans prétendre qu'une action indisponible fonctionne. |
| `cursor_move` | Déplacement | Déplacement de caméra, panneau ou objet selon le contexte. |
| `cursor_normal` | Normal | Pointeur standard des zones neutres. |
| `cursor_place_invalid` | Placement invalide | Aperçu refusé avec raison lisible. |
| `cursor_place_valid` | Placement valide | Aperçu de placement accepté avec contraintes vérifiées. |
| `cursor_resize_d1` | Redimensionner diagonale \ | Coins de panneaux redimensionnables avec contraintes de taille/écran. |
| `cursor_resize_d2` | Redimensionner diagonale / | Coins de panneaux redimensionnables avec contraintes de taille/écran. |
| `cursor_resize_h` | Redimensionner (horizontal) | Redimensionnement horizontal réel d'un panneau ou séparateur prévu. |
| `cursor_resize_v` | Redimensionner (vertical) | Redimensionnement vertical réel d'un panneau ou séparateur prévu. |
| `cursor_select` | Sélection (main) | Sélection d'objet, carte ou personnage. |
| `cursor_text` | Saisie de texte | Saisie dans noms, recherches et terminal. |

## decoration — 25 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `award_plaque` | Plaque de reussite | Récompense visuelle d'un jalon obtenu, contribue à la fierté sans réattribuer les points du jalon. |
| `books_stack` | Pile de livres | Ressource de lecture au poste ; contribution plafonnée au groupe formation/ambiance. |
| `bottle` | Bouteille d'eau | Confort personnel du poste ; appartient au même groupe plafonné que les autres accessoires de pause. |
| `cable_organizer` | Rangement de cables | Organisation du poste, réduit une part plafonnée de la surcharge d'équipements. |
| `clock_wall` | Horloge murale | Repère horaire affichant le temps de jeu et contribution visuelle d'organisation. |
| `figurine_robot` | Petite figurine robot | Personnalisation et ambiance au poste ; petit effet de bien-être plafonné. |
| `idea_sketch` | Croquis d'idee punaise | Idée affichée, contribution plafonnée à la collaboration/design. |
| `moving_box` | Cartons de demenagement | Représentation de l'inventaire emballé lors de déménagement ; accès à son contenu réel. |
| `mug` | Tasse | Confort personnel du poste, personnalisation ; pas de bonus infini avec plusieurs tasses. |
| `notebook` | Carnet et stylo | Support d'organisation/design personnel ; contribution au groupe coordination. |
| `paper_stack` | Pile de papiers | Support d'administration/planification visuelle ; contribution partagée d'organisation. |
| `photo_abstract` | Photo abstraite encadree | Ambiance murale et personnalisation du site. |
| `plant_floor_medium` | Plante en pot (moyenne) | Ambiance/bien-être d'une zone, couverture moyenne. |
| `plant_floor_tall` | Grande plante (ficus lyre) | Ambiance/bien-être d'une zone, emprise/couverture différente de la plante moyenne. |
| `plant_succulent` | Succulente de bureau | Petit bonus plafonné d'ambiance au poste ; nécessite un support compatible. |
| `plant_trailing` | Plante retombante (etagere) | Ambiance via une étagère/support compatible, personnalisation sans occuper une case supplémentaire au sol. |
| `poster_project` | Affiche de projet fictif (fusee) | Affichage de l'identité d'un projet, fierté/attachement de l'équipe plafonnés. |
| `poster_schematic` | Schema technique (plan bleu) | Ambiance technique et ressource de conception, contribution au groupe design/formation. |
| `retro_message` | Petit ecran retro (invite) | Écran de message du poste avec état réel/prochaine action ; personnalisation d'ambiance. |
| `rubber_duck_debug` | Canard de debogage | Accessoire de concentration/débogage, petit effet plafonné sur le suivi des bugs du poste. |
| `side_project` | Mini-projet personnel (platine) | Prototype personnel exposé, ambiance créative et inspiration plafonnées. |
| `snack_bowl` | Bol de snacks | Confort de pause local, service agrégé limité par les employés couverts. |
| `sticky_notes` | Notes adhesives (mur) | Visualisation d'idées et organisation du poste, contribution au groupe coordination. |
| `storage_boxes` | Boites de rangement | Rangement de petits équipements et organisation, avec capacité définie. |
| `trophy_project` | Trophee de projet | Trophée de projet réussi, souvenir persistant et fierté plafonnée. |

## floors — 13 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `floor_carpet` | Moquette | Revêtement textile personnalisable ; contribution au traitement acoustique plafonné du bureau. |
| `floor_carpet_tiles` | Dalles de moquette | Revêtement textile modulaire d'open space ; même famille acoustique, rendu distinct. |
| `floor_concrete` | Beton cire | Revêtement de zone industrielle compatible avec les charges des machines. |
| `floor_edge` | Bord avant de dalle (coupe) | Bord visible de la dalle des locaux, assemblé selon la limite du plan. |
| `floor_edge_corner` | Coin avant de dalle | Raccord d'angle des bords de dalle sans chevauchement. |
| `floor_marking` | Marquage au sol (atelier) | Marquage des zones de production, accès machines et emplacements logistiques. |
| `floor_parquet` | Parquet a batons (vannerie) | Revêtement de bureau/domestique contribuant à l'ambiance de la zone. |
| `floor_raised` | Faux plancher technique | Zone de plancher technique permettant une organisation câbles/refroidissement en salle serveur. |
| `floor_raised_vent` | Faux plancher perfore (soufflage) | Élément ventilé du plancher technique contribuant à la distribution du froid de la zone, sans créer de froid. |
| `floor_threshold` | Seuil de transition (barre laiton) | Seuil de passage entre types de sol/zones ; lecture de l'accès. |
| `floor_tile_ceramic` | Carrelage | Revêtement des sanitaires et kitchenette ; identification des zones de service. |
| `floor_vinyl` | Vinyle mouchete | Revêtement polyvalent d'atelier ou bureau, entretien agrégé standard. |
| `floor_wood_planks` | Plancher bois (lames) | Revêtement de chambre et détente ; personnalisation d'ambiance. |

## fonts — 2 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `font_startuprush_mono` | StartupRush Mono | Saisie mot par mot, journal technique simulé et chiffres à alignement fixe. |
| `font_startuprush_pixel` | StartupRush Pixel | Texte courant du HUD, menus et libellés ; métriques/accents réellement utilisés et testés. |

## furniture — 55 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `armchair` | Fauteuil | Place de détente contribuant à la récupération agrégée des employés. |
| `bed_single` | Lit simple | Récupération du fondateur dans la chambre hors session de travail ; usage sans animation. |
| `bookcase` | Bibliotheque | Ressources de formation : aide plafonnée à l'efficacité des formations du site. |
| `chair_budget` | Chaise economique | Assise économique valide pour un poste, confort de base. |
| `chair_ergonomic` | Chaise ergonomique | Assise longue durée, amélioration plafonnée du confort et de la fatigue. |
| `chair_meeting` | Chaise de reunion | Place valide de réunion/formation ; ne crée pas à elle seule un poste de développement. |
| `chair_office` | Chaise de bureau | Assise de travail standard, confort supérieur à la chaise économique. |
| `coat_rack` | Porte-manteau | Accueil des effets personnels, confort de base du site. |
| `coffee_machine` | Machine a cafe | Service de pause café : récupération/moral agrégés et coût de fonctionnement simple. |
| `desk_compact` | Bureau compact | Poste économique de la chambre : petite emprise et attaches limitées. |
| `desk_corner` | Bureau d'angle | Poste en L offrant davantage d'attaches/accessoires contre une plus grande emprise. |
| `desk_long` | Bureau long (bench) | Bench à configurer selon les sockets réels ; accessoires/écrans supplémentaires, sans inventer de sièges non prévus. |
| `desk_simple` | Bureau simple | Poste de travail standard : bureau, ordinateur et siège compatibles. |
| `desk_tech` | Bureau technique renforce | Poste opérateur renforcé compatible avec atelier/labo et accessoires techniques. |
| `display_case` | Presentoir vitre | Présentation de prototypes/trophées ; fierté d'équipe et support d'objet. |
| `dresser` | Commode | Rangement domestique, contribution plafonnée au confort de la chambre. |
| `filing_cabinet` | Classeur a tiroirs | Organisation administrative agrégée ; facilite coordination sans documents à remplir. |
| `fridge` | Refrigerateur | Stockage/service de repas du coin cuisine ; complète la capacité de pause. |
| `kitchen_counter` | Element de cuisine | Support utile de kitchenette pour appareils et capacité de repas. |
| `kitchen_sink` | Evier | Service d'hygiène de la kitchenette, complète son niveau de confort. |
| `kitchen_wall_cabinet` | Placard haut de cuisine | Capacité de rangement de la kitchenette et support mural prévu. |
| `lamp_ceiling` | Luminaire suspendu | Éclairage général d'une zone ; ne bloque pas la circulation au sol. |
| `lamp_desk` | Lampe de bureau | Éclairage local du poste, contribution plafonnée au confort visuel. |
| `lamp_floor` | Lampadaire | Éclairage de la zone détente/bureau ; couverture de confort visuel. |
| `locker` | Casiers | Rangement personnel d'atelier/usine, confort proportionné aux employés couverts. |
| `microwave` | Micro-ondes | Équipement de repas contribuant à la qualité du coin pause si installé avec un support compatible. |
| `nightstand` | Table de chevet | Support compatible de lampe/objets personnels dans la chambre ; complète le confort domestique. |
| `partition_acoustic` | Separation acoustique | Réduire la surcharge de bruit d'open space/studio avec couverture limitée. |
| `partition_glass` | Vitrage interieur | Séparer visuellement les zones, contribuer à leur organisation et conserver transparence. |
| `partition_glass_door` | Vitrage interieur avec porte | Passage logique dans une séparation de verre ; accès de la zone préservé. |
| `partition_low` | Cloison basse | Délimiter les zones et améliorer modérément la concentration, sans isoler tout le local. |
| `pedestal_drawer` | Caisson a roulettes | Organisation d'un poste ou support de rangement selon les sockets, effet plafonné. |
| `planning_board` | Tableau de planning mural | Support visuel des priorités réelles et contribution plafonnée à la coordination d'équipe. |
| `presentation_screen` | Ecran de presentation sur pied | Support de formation, démonstration et réunions ; contenu dynamique cohérent avec le contexte. |
| `reception_desk` | Banque d'accueil | Capacité d'accueil/organisation du site ; améliore l'efficacité des canaux professionnels de manière plafonnée. |
| `recycling_station` | Bacs de tri | Service de tri de site, organisation/réduction plafonnée d'une part des coûts de déchets. |
| `rug_large` | Grand tapis | Traitement de sol décoratif/acoustique de plus grande couverture, non cumulable sans limite. |
| `rug_small` | Tapis | Traitement de sol décoratif/acoustique local, sous les ombres selon le manifeste. |
| `shelf_metal` | Etagere metallique | Capacité de stockage de petits équipements/cartons compatible avec les attaches prévues. |
| `sideboard_low` | Meuble bas | Support pour imprimante, café et décoration ; capacité réelle d'accueil des objets compatibles. |
| `sofa` | Canape | Places de détente supplémentaires, récupération/moral plafonnés par l'occupation. |
| `table_coffee` | Table basse | Compléter un coin détente avec places compatibles ; pas un bureau productif. |
| `table_collab` | Table de travail collective | Postes de collaboration selon attaches réelles ; coordination de design/prototypage. |
| `table_meeting_large` | Grande table de reunion | Capacité de coordination/formation d'une équipe plus importante ; effet limité par les places. |
| `table_meeting_small` | Petite table de reunion (ronde) | Capacité de coordination/formation d'une petite équipe, si sièges compatibles. |
| `toilet` | WC | Capacité sanitaire de base du site, besoin agrégé sans déplacements ni mini-jeu. |
| `toilet_stall` | Cabine sanitaire | Intimité/organisation du sanitaire, à associer à un WC et un accès valide. |
| `trash_bin` | Poubelle | Couverture simple de propreté/confort, sans corvée de vidage. |
| `vending_drinks` | Distributeur de boissons | Capacité automatique de boissons pour sites plus fréquentés, coût agrégé annoncé. |
| `vending_snacks` | Distributeur de snacks | Capacité automatique de collation ; récupération sans gestion de chaque paquet. |
| `wardrobe_office` | Armoire | Rangement de site : contribution à l'organisation et au confort. |
| `washbasin` | Lavabo | Hygiène du module sanitaire, contribue à la qualité du service. |
| `water_cooler` | Fontaine a eau | Service d'eau pour les employés couverts ; confort sans recharge manuelle. |
| `whiteboard_mobile` | Tableau blanc mobile | Capacité de collaboration/design et formation, selon personnel présent. |
| `window_blinds` | Stores de fenetre | Contrôle visuel/lumineux d'une fenêtre compatible ; confort et réglage d'ambiance. |

## hud — 224 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `hud_badge_accent` | Badge accent | Mise en avant d'une action conseillée ou d'un point de progression. |
| `hud_badge_count` | Pastille compteur | Nombre réel de tâches/alertes/éléments filtrés. |
| `hud_badge_danger` | Badge danger | Impayé, échéance critique ou arrêt menaçant, avec texte/symbole. |
| `hud_badge_info` | Badge info | Information sans urgence ni action obligatoire. |
| `hud_badge_neutral` | Badge neutral | État neutre : brouillon, disponible ou en stock. |
| `hud_badge_success` | Badge success | État valide/résolu/publication réussie. |
| `hud_badge_warning` | Badge warning | Capacité proche du plafond ou problème récupérable. |
| `hud_banner` | Bandeau | Résumé prioritaire d'un problème majeur ou jalon dans la vue concernée. |
| `hud_button_destructive` | Bouton destructif | Arrêt/vente/suppression avec conséquence et confirmation si nécessaire. |
| `hud_button_icon` | Bouton icone | Commande compacte avec nom accessible et info-bulle. |
| `hud_button_icon_small` | Petit bouton icone (barre de titre) | Petite commande de ligne, par exemple réordonner une tâche ou ouvrir un détail. |
| `hud_button_primary` | Bouton principal | Action principale contextuelle, états survol/pressé/focus/désactivé fonctionnels. |
| `hud_button_secondary` | Bouton secondaire | Action secondaire non destructive, par exemple voir les détails. |
| `hud_button_text` | Bouton texte | Lien d'action discret vers les explications ou un réglage. |
| `hud_card` | Carte produit / projet | Carte de projet/site/produit avec données et interaction. |
| `hud_chart_axis_h` | Axe horizontal | Axe temporel des courbes, libellés correspondant à la période affichée. |
| `hud_chart_bar` | Barre de graphique (teinte par multiplication) | Histogramme de revenus, coûts, capacité ou stocks. |
| `hud_chart_frame` | Cadre de graphique | Cadre de courbe de finances, ventes ou qualité, données réelles. |
| `hud_chart_grid_h` | Ligne de grille horizontale (pointillee) | Repères horizontaux/verticaux de graphique à échelle réelle. |
| `hud_chart_grid_v` | Ligne de grille verticale (pointillee) | Repères horizontaux/verticaux de graphique à échelle réelle. |
| `hud_chart_legend` | Pastille de legende (teinte par multiplication) | Légende permettant d'identifier/activer les séries du graphique. |
| `hud_chart_point` | Point de courbe (teinte par multiplication) | Point d'une courbe de tendances, sélection avec valeur datée. |
| `hud_checkbox` | Case à cocher | Option cumulable : automatisation, notifications ou filtre de catalogue. |
| `hud_chip_dark` | Puce sombre (date, statut) | Étiquette compacte de type, site, métier ou version. |
| `hud_confirm_dialog` | Dialogue de confirmation (composition) | Confirmation contextualisée d'arrêt, vente, suppression ou écrasement. |
| `hud_drag_handle` | Poignée de déplacement | Réordonner la file de travail ou déplacer un panneau prévu. |
| `hud_dropdown` | Liste déroulante (champ) | Sélection de site, métier, famille ou unité de période. |
| `hud_dropdown_list` | Liste déroulante (volet) | Liste des options réellement sélectionnables du menu déroulant. |
| `hud_field_multiline` | Zone multiligne | Description/note personnalisée d'un projet ou site, sauvegardée. |
| `hud_field_numeric` | Champ numérique (+/-) | Prix, quantité cible ou plafond de budget avec validation. |
| `hud_field_search` | Champ de recherche | Recherche réelle dans catalogue, équipe et portefeuille. |
| `hud_field_text` | Champ texte | Nom de projet/entreprise et saisies simples. |
| `hud_gauge_base` | Jauge (base vide) | Jauge réelle de capacité, moral ou énergie avec valeurs bornées. |
| `hud_gauge_fill` | Jauge (segments pleins) | Jauge réelle de capacité, moral ou énergie avec valeurs bornées. |
| `hud_gauge_segment_map` | Jauge : carte d'index (R = segment 1..10) | Masque de la jauge segmentée, appliqué au remplissage réellement rendu. |
| `hud_list_row` | Ligne de liste / tableau | Ligne sélectionnable d'employé, stock, sauvegarde ou tâche. |
| `hud_menu_item` | Élément de menu | Commande réelle du menu contextuel/pause avec focus clavier. |
| `hud_menu_panel` | Menu contextuel (panneau) | Menu de pause ou menu contextuel d'objet. |
| `hud_menu_separator` | Séparateur de menu | Séparer commandes ordinaires et actions destructives. |
| `hud_modal` | Fenêtre modale | Surface modale d'une décision ponctuelle avec fermeture et gestion de focus. |
| `hud_money_chip` | Puce trésorerie | Trésorerie ou coût en CHF avec fréquence explicite. |
| `hud_notification` | Notification | Ligne de journal liée à une vraie cause et action de résolution. |
| `hud_panel_inset` | Zone en creux (liste, graphique) | Bloc intérieur d'indicateurs ou prévisualisation de tâche. |
| `hud_panel_main` | Panneau principal | Surface des panneaux principaux Projets, Équipe, Aménagement, arbre et Finances. |
| `hud_panel_secondary` | Panneau secondaire | Détails secondaires de projet/site, hiérarchie visuelle. |
| `hud_pattern_cell` | Pastille de motif | Cellule répétée de fond des zones/grilles de prévisualisation. |
| `hud_popover` | Popover | Détails temporaires d'une jauge, allocation ou ressource au clic. |
| `hud_portrait_frame` | Cadre de portrait | Portrait cliquable du boss/employé avec apparence réelle persistante. |
| `hud_progress_fill_danger` | Remplissage danger | Charge critique ou échéance financière, distincte d'une tâche productive. |
| `hud_progress_fill_default` | Remplissage default | Progression normale d'une tâche en cours. |
| `hud_progress_fill_striped` | Remplissage en cours (hachures, a faire defiler par code) | Attente/activité indéterminée seulement quand la durée n'est pas connue, pas faux progrès. |
| `hud_progress_fill_success` | Remplissage success | Progression/livraison achevée et validée. |
| `hud_progress_fill_warning` | Remplissage warning | Progression avec contrainte non bloquante, attente annoncée. |
| `hud_progress_track` | Barre de progression (piste) | Fond d'une progression de travail, recherche ou série. |
| `hud_radio` | Bouton radio | Choix exclusif d'un mode de production, public ou type de lancement. |
| `hud_scroll_arrow_down` | Flèche de defilement down | Commandes de défilement pas à pas des vues concernées, atteignables normalement. |
| `hud_scroll_arrow_left` | Flèche de defilement left | Commandes de défilement pas à pas des vues concernées, atteignables normalement. |
| `hud_scroll_arrow_right` | Flèche de defilement right | Commandes de défilement pas à pas des vues concernées, atteignables normalement. |
| `hud_scroll_arrow_up` | Flèche de defilement up | Commandes de défilement pas à pas des vues concernées, atteignables normalement. |
| `hud_scroll_grip_h` | Prise du curseur de defilement (h) | Défilement horizontal des graphes/catalogues dépassant la largeur disponible. |
| `hud_scroll_grip_v` | Prise du curseur de defilement (v) | Défilement vertical des listes longues, journal et catalogue. |
| `hud_scroll_thumb_h` | Barre de défilement (h) curseur | Défilement horizontal des graphes/catalogues dépassant la largeur disponible. |
| `hud_scroll_thumb_v` | Barre de défilement (v) curseur | Défilement vertical des listes longues, journal et catalogue. |
| `hud_scroll_track_h` | Barre de défilement (h) piste | Défilement horizontal des graphes/catalogues dépassant la largeur disponible. |
| `hud_scroll_track_v` | Barre de défilement (v) piste | Défilement vertical des listes longues, journal et catalogue. |
| `hud_section_header` | En-tête de section | Titre d'un groupe repliable permettant de réduire la quantité de texte visible. |
| `hud_segmented_left` | Contrôle segmenté (left) | Sélecteur à plusieurs niveaux, par exemple entretien 10/25/45 ou filtres d'état. |
| `hud_segmented_mid` | Contrôle segmenté (mid) | Sélecteur à plusieurs niveaux, par exemple entretien 10/25/45 ou filtres d'état. |
| `hud_segmented_right` | Contrôle segmenté (right) | Sélecteur à plusieurs niveaux, par exemple entretien 10/25/45 ou filtres d'état. |
| `hud_separator_h` | Séparateur horizontal | Séparation des groupes de commandes/données selon l'orientation. |
| `hud_separator_v` | Séparateur vertical | Séparation des groupes de commandes/données selon l'orientation. |
| `hud_slider_fill` | Glissière (remplissage) | Curseur réel de volume, plafond ou allocation, avec valeur numérique disponible. |
| `hud_slider_knob` | Glissière (bouton) | Curseur réel de volume, plafond ou allocation, avec valeur numérique disponible. |
| `hud_slider_track` | Glissière (piste) | Curseur réel de volume, plafond ou allocation, avec valeur numérique disponible. |
| `hud_stepper_down` | Flèche de champ numerique (down) | Ajustement borné d'une quantité, d'un prix ou d'une allocation. |
| `hud_stepper_up` | Flèche de champ numerique (up) | Ajustement borné d'une quantité, d'un prix ou d'une allocation. |
| `hud_swatch_cell` | Pastille de couleur | Choix de rampe/couleur du mobilier, murs et apparences. |
| `hud_switch` | Interrupteur | Activation simple d'une délégation ou option automatique débloquée. |
| `hud_tabs` | Onglet | Onglets Aperçu, Composants, Avis, Réglages et sous-vues adaptées. |
| `hud_theme_papier` | Theme Papier (jetons de couleur) | Source des tokens de thème utilisés par les contrôles de production. |
| `hud_titlebar` | Barre de titre | En-tête d'une fenêtre avec titre et commandes réelles. |
| `hud_toast` | Toast | Bonne nouvelle ou nouveau problème significatif non bloquant. |
| `hud_tooltip` | Infobulle | Aide courte, motif d'indisponibilité et explication des coûts. |
| `hud_topbar_bg` | Barre supérieure (fond) | Fond de la barre persistante temps/trésorerie/alertes. |
| `hud_topbar_divider` | Barre supérieure (séparateur) | Séparer les groupes de données de la barre haute. |
| `hud_topbar_segment` | Barre supérieure (groupe) | Section de la barre haute regroupant des indicateurs cohérents. |
| `hud_window` | Fenêtre (cadre + barre de titre) | Fenêtre de gestion redimensionnable intégrée au jeu. |
| `icon_access` | Accès | Commande/indicateur : accès logique et compatibilité de zone/poste. |
| `icon_ai` | IA | Commande/indicateur : familles de projets IA et ressources de modèle. |
| `icon_assembly` | Assemblage | Commande/indicateur : phase et poste d’assemblage matériel. |
| `icon_assignment` | Affectation | Commande/indicateur : affectation d’un employé ou du travail actif. |
| `icon_back` | Retour | Commande/indicateur : retour au niveau précédent de navigation. |
| `icon_bell` | Notifications | Commande/indicateur : journal des problèmes et bonnes nouvelles regroupés. |
| `icon_budget` | Budget | Commande/indicateur : plafond d’approvisionnement/hébergement/marketing. |
| `icon_bug` | Bogue | Commande/indicateur : défauts et suivi correctif réellement calculés. |
| `icon_calendar` | Calendrier | Commande/indicateur : date et horaires/congés/formation. |
| `icon_cancel` | Annuler | Commande/indicateur : annuler une saisie ou action réversible. |
| `icon_caret_down` | Déroulant | Commande/indicateur : ouvrir une liste et ses options. |
| `icon_cash` | Trésorerie | Commande/indicateur : trésorerie disponible de l’entreprise. |
| `icon_chart_line` | Courbe | Commande/indicateur : tendance de ventes, audience ou résultat. |
| `icon_chevron_down` | Chevron bas | Commande/indicateur : déplier les détails d’une section. |
| `icon_chevron_left` | Chevron gauche | Commande/indicateur : navigation précédente dans un catalogue/graphe. |
| `icon_chevron_right` | Chevron droit | Commande/indicateur : navigation suivante dans un catalogue/graphe. |
| `icon_chevron_up` | Chevron haut | Commande/indicateur : replier les détails d’une section. |
| `icon_client` | Client | Commande/indicateur : public/clientèle agrégée des produits, sans mission client. |
| `icon_close` | Fermer | Commande/indicateur : fermer un panneau en restaurant le focus. |
| `icon_cloud` | Nuage | Commande/indicateur : offres hébergées et location de ressources externes. |
| `icon_code` | Code | Commande/indicateur : travail du terminal et phase de développement. |
| `icon_comp_admin` | Administration | Pictogramme compact de comp_admin dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_api` | API | Pictogramme compact de comp_api dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_auth` | Compte / connexion | Pictogramme compact de comp_auth dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_automation` | Automatisation | Pictogramme compact de comp_automation dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_catalog` | Catalogue | Pictogramme compact de comp_catalog dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_engine` | Moteur | Pictogramme compact de comp_engine dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_feed` | Fil de contenu | Pictogramme compact de comp_feed dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_game_menu` | Menu de jeu | Pictogramme compact de comp_game_menu dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_infrastructure` | Infrastructure | Pictogramme compact de comp_infrastructure dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_integration` | Intégration | Pictogramme compact de comp_integration dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_inventory` | Inventaire | Pictogramme compact de comp_inventory dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_leaderboard` | Classement | Pictogramme compact de comp_leaderboard dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_messaging` | Messagerie | Pictogramme compact de comp_messaging dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_netplay` | Réseau (jeu) | Pictogramme compact de comp_netplay dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_payment` | Paiement simulé | Pictogramme compact de comp_payment dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_processing` | Traitement | Pictogramme compact de comp_processing dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_profile` | Profil | Pictogramme compact de comp_profile dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_progression` | Progression | Pictogramme compact de comp_progression dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_comp_reviews` | Avis | Pictogramme compact de comp_reviews dans listes, dépendances et résumés ; même fonction réelle que la carte correspondante. |
| `icon_company` | Entreprise | Commande/indicateur : vue commune de l’unique entreprise. |
| `icon_competition` | Concurrence | Commande/indicateur : repère de concurrence et positionnement du projet. |
| `icon_components` | Composants | Commande/indicateur : atelier de composants numériques ou matériels. |
| `icon_confirm` | Valider | Commande/indicateur : valider une décision après vérification. |
| `icon_contract` | Contrat | Commande/indicateur : conditions/coûts récurrents d’un fournisseur ou canal ; aucun contrat de prestation à accepter. |
| `icon_conversion` | Conversion | Commande/indicateur : part des personnes exposées devenant clientes. |
| `icon_conveyor` | Convoyeur | Commande/indicateur : vue de flux et liaisons de ligne industrielle. |
| `icon_cooling` | Refroidissement | Commande/indicateur : capacité de froid et marge thermique. |
| `icon_cpu` | CPU | Commande/indicateur : capacité CPU et recettes de processeurs. |
| `icon_debt` | Dette | Commande/indicateur : dette et échéances de remboursement. |
| `icon_delete` | Supprimer | Commande/indicateur : suppression/arrêt avec confirmation contextualisée. |
| `icon_dependency` | Dépendance | Commande/indicateur : prérequis de technologie/composant. |
| `icon_deploy` | Déployer | Commande/indicateur : mise en service et mini-jeu de déploiement. |
| `icon_design` | Design | Commande/indicateur : phase de conception et capacité de design. |
| `icon_drag_grip` | Poignee | Commande/indicateur : réordonner les priorités de file. |
| `icon_edit` | Modifier | Commande/indicateur : modifier un nom, réglage ou configuration. |
| `icon_energy` | Énergie | Commande/indicateur : énergie des employés ou capacité électrique avec libellé explicite. |
| `icon_error` | Erreur | Commande/indicateur : erreur d’action/saisie et motif compréhensible. |
| `icon_expense` | Dépense | Commande/indicateur : charges mensuelles ou coût de lot. |
| `icon_external` | Ouvrir à part | Commande/indicateur : ressource externe ou lien de documentation explicite, jamais ouverture cachée. |
| `icon_eye` | Afficher | Commande/indicateur : visiter un site/prévisualiser un objet. |
| `icon_filter` | Filtres | Commande/indicateur : filtrer le portefeuille, les stocks ou le catalogue. |
| `icon_furnish` | Aménagement | Commande/indicateur : ouvrir le catalogue d’aménagement. |
| `icon_gpu` | GPU | Commande/indicateur : calcul GPU et cartes graphiques de produit. |
| `icon_help` | Aide | Commande/indicateur : aide contextuelle et commandes consultables. |
| `icon_home` | Logement | Commande/indicateur : retour à la scène principale/site courant. |
| `icon_idea` | Idée | Commande/indicateur : nouvelle création et projet de recherche. |
| `icon_income` | Recette | Commande/indicateur : recettes réellement encaissées. |
| `icon_info` | Information | Commande/indicateur : explication facultative sans urgence. |
| `icon_invoice` | Facture | Commande/indicateur : facture réelle avec échéance et état de paiement. |
| `icon_link` | Liaison | Commande/indicateur : connexion de graphe, allocation ou machine compatible. |
| `icon_load` | Charger | Commande/indicateur : charger une campagne existante. |
| `icon_loan` | Prêt | Commande/indicateur : détails d’un prêt existant et coût annoncé. |
| `icon_lock` | Verrouiller | Commande/indicateur : élément verrouillé avec raison et conditions. |
| `icon_mail` | Messages | Commande/indicateur : support/réponses aux avis, notamment tâche automatique du boss libre. |
| `icon_maintenance` | Maintenance | Commande/indicateur : part d’entretien et capacité de suivi des projets. |
| `icon_manufacturing` | Fabrication | Commande/indicateur : ordre de fabrication et lignes industrielles. |
| `icon_marketing` | Marketing | Commande/indicateur : budget/canal de visibilité du produit. |
| `icon_memory` | Mémoire | Commande/indicateur : capacité RAM et modules mémoire. |
| `icon_minus` | Retirer | Commande/indicateur : réduire une valeur ou retirer un élément réversible. |
| `icon_monitor` | Écran | Commande/indicateur : affichage et état d’un poste de travail. |
| `icon_more` | Plus d’actions | Commande/indicateur : actions secondaires repliées d’une ligne/carte. |
| `icon_move` | Déplacement | Commande/indicateur : déplacer un objet dans une pièce ou réorganiser un site. |
| `icon_nav_menu` | Menu | Commande/indicateur : navigation compacte des panneaux secondaires. |
| `icon_network` | Réseau | Commande/indicateur : débit et capacité réseau du site. |
| `icon_occupancy` | Occupation | Commande/indicateur : postes/capacité d’espace occupés. |
| `icon_office` | Bureau | Commande/indicateur : catalogue et fiche d’un bureau loué. |
| `icon_packaging` | Emballage | Commande/indicateur : emballage et disponibilité à l’expédition. |
| `icon_pause` | Pause | Commande/indicateur : pause du temps et suspension d’une tâche. |
| `icon_pc` | PC | Commande/indicateur : gammes PC et configuration de poste. |
| `icon_place` | Placement | Commande/indicateur : placer/valider un équipement dans la pièce. |
| `icon_plus` | Ajouter | Commande/indicateur : ajouter un composant/ordre ou créer un projet. |
| `icon_price` | Prix | Commande/indicateur : prix de vente et comparaison au public. |
| `icon_product` | Produit | Commande/indicateur : produit matériel commercialisé et ses versions. |
| `icon_profit` | Bénéfice | Commande/indicateur : résultat réel et marge de produit. |
| `icon_project` | Projet | Commande/indicateur : portefeuille et identité d’une création. |
| `icon_publish` | Publier | Commande/indicateur : lancer une version prête sur son marché. |
| `icon_quality` | Qualité | Commande/indicateur : qualité mesurée d’un projet/lot/prototype. |
| `icon_quantum` | Quantique | Commande/indicateur : recherche, capacité et services quantiques spécialisés. |
| `icon_rack` | Baie | Commande/indicateur : racks, emplacements en U et compatibilité de modules. |
| `icon_receivable` | Créance | Commande/indicateur : recettes attendues des canaux avec versement différé, distinctes de la trésorerie. |
| `icon_recruit` | Recruter | Commande/indicateur : recrutement avec impact financier. |
| `icon_refresh` | Actualiser | Commande/indicateur : nouvelle estimation/rafraîchissement d’une liste autorisée. |
| `icon_retention` | Rétention | Commande/indicateur : rétention mesurée du public. |
| `icon_risk` | Risque | Commande/indicateur : marge de risque financier/capacité expliquée. |
| `icon_rotate` | Rotation | Commande/indicateur : rotation d’objet vers une orientation du manifeste, jamais rotation de caméra. |
| `icon_salary` | Salaire | Commande/indicateur : salaire récurrent et augmentation simple. |
| `icon_save` | Sauvegarder | Commande/indicateur : sauvegarder une campagne avec confirmation de résultat. |
| `icon_search` | Recherche | Commande/indicateur : recherche dans les listes et catalogues. |
| `icon_security` | Sécurité | Commande/indicateur : niveau de confiance/sécurité du produit ou site. |
| `icon_sell` | Revente | Commande/indicateur : revendre un équipement ou ouvrir un canal de vente. |
| `icon_server` | Serveur | Commande/indicateur : ressource serveur et hébergement de produits. |
| `icon_settings` | Réglages | Commande/indicateur : réglages du jeu et préférences du terminal. |
| `icon_sort` | Trier | Commande/indicateur : tri d’une liste et ordre des priorités. |
| `icon_speed_1` | Vitesse 1 | Commande/indicateur : premier niveau de vitesse de simulation. |
| `icon_speed_2` | Vitesse 2 | Commande/indicateur : niveau intermédiaire de vitesse. |
| `icon_speed_3` | Vitesse 3 | Commande/indicateur : niveau rapide de vitesse. |
| `icon_star` | Favori | Commande/indicateur : favori de catalogue/projet ou note utilisateur selon le libellé. |
| `icon_statistics` | Statistiques | Commande/indicateur : détails chiffrés facultatifs de l’activité. |
| `icon_stock` | Stock | Commande/indicateur : stock disponible, réservé, en production et transit. |
| `icon_storage` | Stockage | Commande/indicateur : capacité de stockage informatique ou physique clairement distinguée. |
| `icon_subscription` | Abonnement | Commande/indicateur : revenu récurrent des applications compatibles. |
| `icon_success` | Succès | Commande/indicateur : action ou jalon réellement réussi. |
| `icon_support` | Support | Commande/indicateur : charge et résolution du suivi utilisateur. |
| `icon_team` | Équipe | Commande/indicateur : équipe et capacités disponibles. |
| `icon_terminal` | Terminal | Commande/indicateur : ouvrir le travail actif du boss. |
| `icon_test` | Test | Commande/indicateur : phase de test/validation d’un projet. |
| `icon_test_station` | Contrôle | Commande/indicateur : banc de test matériel/XR/mobile. |
| `icon_time` | Temps | Commande/indicateur : durée et estimation de travail avec hypothèses. |
| `icon_training` | Formation | Commande/indicateur : formation avec coût et absence annoncés. |
| `icon_trophy` | Trophée | Commande/indicateur : jalons obtenus et récompenses visuelles persistantes. |
| `icon_unavailable` | Indisponible | Commande/indicateur : indisponibilité avec cause/action possible. |
| `icon_unlock` | Déverrouiller | Commande/indicateur : déblocage d’une capacité par l’arbre. |
| `icon_user` | Utilisateur | Commande/indicateur : fiche d’une personne ou individu agrégé du public. |
| `icon_users` | Utilisateurs | Commande/indicateur : audience/effectif selon le libellé de la vue. |
| `icon_version` | Version | Commande/indicateur : version publiée et mise à jour en préparation. |
| `icon_warning` | Avertissement | Commande/indicateur : problème récupérable nécessitant une décision. |

## industry_future — 32 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `aoi_inspection` | Inspection optique automatique | Contrôle optique d'une série, réduit les défauts qui atteignent l'étape suivante. |
| `assembly_cell_robotic` | Cellule d'assemblage robotisee | Assemblage automatisé plus rapide sur recettes compatibles ; exige capacité et supervision. |
| `assembly_station_manual` | Poste d'assemblage manuel | Assemblage par opérateur de sous-ensembles puis produits matériels. |
| `chip_fab_tool` | Outil de traitement microelectronique (ferme) | Traitement abstrait de wafers en microcomposants avec zone propre et ressources compatibles. |
| `cleanroom_airlock` | Sas de salle propre (douche a air) | Accès logique filtré de la zone propre, à associer aux cloisons et filtration requises. |
| `cleanroom_partition` | Cloison de salle propre | Délimitation de zone propre pour la microélectronique ; couverture compatible avec le plan. |
| `control_cabinet` | Armoire de controle | Contrôle de ligne et plafond de machines automatisées supervisables. |
| `conveyor_curve` | Convoyeur courbe (90 degres) | Changement de direction de flux entre ports compatibles. |
| `conveyor_end` | Convoyeur fin de ligne (butee) | Terminaison avec tampon borné et point de collecte vers stock/opérateur compatible. |
| `conveyor_junction_t` | Convoyeur jonction en T (insertion) | Fusion ou distribution de flux selon les ports réellement décrits ; jamais duplication d'unités. |
| `conveyor_straight` | Convoyeur droit | Transport orienté d'unités entre ports, débit/buffer définis. |
| `conveyor_transfer_table` | Table de transfert a angle droit | Transfert entre lignes/orientations compatibles, règle de priorité visible. |
| `dust_extraction` | Centrale d'aspiration / filtration | Capacité de filtration pour procédés propres/laser ; compatibilité de la zone, sans entretien manuel. |
| `flash_station` | Station d'installation / flash logiciel | Installation du firmware/logiciel sur une série, consomme capacité et valide une étape de recette. |
| `industrial_chiller` | Groupe de refroidissement (chiller) | Capacité de refroidissement pour industrie/labo, avec coût énergétique. |
| `packing_station` | Poste d'emballage et d'etiquetage | Transforme une unité validée en produit commercial emballé et expédiable. |
| `pallet` | Palette de manutention | Unité de manutention/stockage et support de charge réellement réservée. |
| `pcb_tester` | Testeur de cartes (sondes mobiles) | Contrôle fonctionnel des cartes, validation des lots et détection de défauts. |
| `pick_and_place_machine` | Machine de placement de composants | Étape de placement des composants pour la recette de carte électronique. |
| `precision_station` | Station de precision avancee | Contrôle/finition de précision de puces et composants avancés. |
| `product_box` | Carton de produit | Représentation et état logistique d'un produit emballé ; ne crée pas une unité supplémentaire. |
| `product_case` | Boitier vide | Boîtier d'assemblage de machines ; sous-ensemble achetable/fabricable/vendable. |
| `product_cpu` | Processeur (microcomposant) | Microcomposant consommé dans assemblages et commercialisable comme produit de gamme. |
| `product_gpu` | Carte graphique | Carte graphique consommable/équipable ou vendable, lien réel aux capacités GPU. |
| `product_pc_assembled` | Ordinateur assemble | Unité physique de PC terminée, stock/vente ou équipement interne explicitement transféré. |
| `product_pcb` | Carte electronique | Type d'intrant et produit vendable : carte électronique ; utilisé dans recettes et stocks. |
| `product_ram` | Module memoire | Mémoire consommable/équipable et commercialisable ; capacité RAM de configurations. |
| `product_wafer` | Plaquette de silicium | Intrant de microélectronique acheté ou vendu comme matière, support des traitements abstraits. |
| `reflow_oven` | Four de refusion (tunnel) | Étape de soudure/refusion agrégée après placement, débit/énergie définis. |
| `storage_rack_industrial` | Rack de stockage a palettes | Capacité physique de stockage des intrants/produits avec unités de capacité définies. |
| `stress_test_rack` | Banc de stress-test (rodage) | Tests de stabilité de produits assemblés avant emballage, amélioration de fiabilité. |
| `tote_bin` | Bac de manutention | Petit buffer de pièces en atelier/ligne ; capacité consommée par son contenu. |

## laboratories — 27 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `calibration_bench` | Etabli de calibration (marbre) | Précision des prototypes et premières séries ; préparation du mini-jeu de calibration. |
| `camera_tripod` | Camera sur trepied | Capture de contenu pour jeux, démonstrations et marketing de produits. |
| `cnc_mill_desktop` | Fraiseuse CNC de table | Prototypage de pièces rigides/précises ; capacité et recette spécifiques, gamme possible. |
| `cobot_cell` | Cellule robotisee (cobot) | Assistance automatisée à l'assemblage/prototypage avancé ; capacité supervisée. |
| `compute_cabinet` | Calculateur avance (armoire) | Calcul scientifique avancé, ressource réservée de recherche ou service spécialisé. |
| `cryo_support_unit` | Unite de support cryogenique | Support cryogénique agrégé dimensionnant les installations quantiques. |
| `greenscreen_backdrop` | Fond vert sur portique | Qualité de production du studio vidéo compatible avec caméra/éclairage. |
| `instrument_rack` | Chariot d'instruments de mesure | Capacité de mesures agrégées avancées sur bancs de test. |
| `lab_power_supply` | Alimentation de laboratoire | Alimentation réglable des essais électroniques, équipement requis de banc compatible. |
| `laser_cutter` | Decoupeuse laser | Prototypage de pièces plates/boîtiers avec filtration compatible ; gamme possible. |
| `mic_stand` | Pied de micro avec perche | Capacité de capture audio du studio pour jeux/contenus/présentation. |
| `microscope` | Microscope binoculaire | Inspection fine de prototypes/microcomposants et soutien des recherches matérielles. |
| `oscilloscope` | Oscilloscope | Diagnostic de signaux pour validation/prototypage des cartes, capacité de qualité. |
| `printer_3d` | Imprimante 3D fermee | Capacité de prototypage de pièces/boîtiers ; modèle réutilisable pour une gamme d'équipement vendable. |
| `quantum_control_rack` | Baie de controle micro-ondes | Capacité de contrôle requise pour installations quantiques compatibles. |
| `quantum_cryostat_closed` | Ordinateur quantique (cryostat ferme) | Installation quantique stabilisée pour calcul spécialisé et gamme commerciale de fin de progression. |
| `quantum_cryostat_open` | Ordinateur quantique (cryostat ouvert) | Installation expérimentale représentée en phase de laboratoire, recherches/calibrations quantiques, pas machine réelle ouverte en production. |
| `research_station` | Poste de recherche (paillasse) | Poste de chercheur, capacité de recherche selon compétence/projet. |
| `softbox_light` | Eclairage softbox | Éclairage de studio, amélioration plafonnée de qualité des contenus produits. |
| `soldering_station` | Station de soudure | Outillage de prototype/réparation de conception, capacité de validation électronique. |
| `studio_monitors` | Enceintes de studio (paire) | Capacité de contrôle audio, qualité de production de contenu et de jeux. |
| `supervision_terminal` | Terminal de supervision (pupitre debout) | Poste opérateur/supervision de labo ou ligne, accès aux vraies files et diagnostics. |
| `test_bench_embedded` | Banc de test mobile / embarque | Validation de cartes, dispositifs mobiles et projets embarqués. |
| `test_bench_pc` | Banc de test PC | Validation de gammes PC/serveurs et compatibilité des configurations. |
| `workbench_electronics` | Etabli electronique | Poste de prototypage électronique et assemblage expérimental. |
| `xr_base_station` | Balise de suivi XR sur pied | Capacité de suivi de la zone de test XR compatible avec casque et tapis. |
| `xr_play_mat` | Tapis de zone de jeu XR | Emprise réservée et compatible d'une zone de test XR. |

## project_components — 51 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `comp_admin` | Administration | Administration du produit, réduction plafonnée de charge d'exploitation. |
| `comp_analytics` | Analytics | Analyse de l'usage du produit, meilleure lecture du public et amélioration de conversion ciblée. |
| `comp_api` | API | Accès API du produit, public professionnel et consommation de capacité. |
| `comp_auth` | Compte / authentification | Composant comptes/authentification, prérequis cohérent de fonctions personnalisées. |
| `comp_automation` | Automatisation | Automatisation de tâches du produit, valeur métier contre complexité et charge. |
| `comp_card` | Carte de composant | Vrai contrôle de carte réutilisé pour composants et étapes de création ; états et sélection fonctionnels. |
| `comp_catalog` | Catalogue | Catalogue d'offres/produits, utilité de boutique ou marketplace. |
| `comp_engine` | Moteur | Moteur de jeu, prérequis et capacité de la famille de jeux choisie. |
| `comp_feed` | Fil de contenu | Fil communautaire, activité et rétention avec coût de modération. |
| `comp_game_menu` | Menu (jeu) | Menus du jeu simulé, accessibilité/onboarding de son public. |
| `comp_home` | Accueil | Composant accueil/navigation du projet ; onboarding/utilité du public. |
| `comp_infrastructure` | Infrastructure | Socle technique du produit, capacité/performance avec coûts affichés. |
| `comp_integration` | Intégration | Intégrations avec des outils simulés, utilité métier et coût d'entretien. |
| `comp_inventory` | Inventaire | Inventaire de jeu ou gestion de stock intégrée à un outil, selon le projet simulé. |
| `comp_leaderboard` | Classement | Classements du produit, intérêt compétitif et charge serveur. |
| `comp_messaging` | Messagerie | Messagerie entre utilisateurs du produit, utilité/rétention et coût de modération. |
| `comp_netplay` | Réseau (multijoueur) | Multijoueur dans le jeu créé, audience/entretien/hébergement ; ne rend pas StartupRush multijoueur. |
| `comp_notifications` | Notifications | Notifications du produit, rétention plafonnée et coût de suivi. |
| `comp_payment` | Paiement simulé | Paiements simulés du produit, conversion/transactions et prérequis de revenus compatibles. |
| `comp_processing` | Traitement | Traitement de données/calcul métier ou IA selon contexte, consommation de calcul. |
| `comp_profile` | Profil | Profils de membres, personnalisation/rétention du public compatible. |
| `comp_progression` | Progression | Système de progression du jeu simulé, rétention de publics compatibles. |
| `comp_reviews` | Avis | Avis utilisateurs intégrés au produit, confiance et retour d'information. |
| `comp_savegame` | Sauvegarde | Sauvegarde dans le jeu simulé, satisfaction/continuité du public. |
| `comp_search` | Recherche | Recherche dans le produit simulé, facilité d'accès à son contenu/catalogue. |
| `comp_security` | Sécurité | Sécurité/fiabilité du produit, confiance et coût d'implémentation/maintenance. |
| `comp_storage` | Stockage | Stockage de contenu/données du produit, utilité et charge réelle d'infrastructure. |
| `comp_subscription` | Abonnement | Abonnement du produit, monétisation récurrente selon la famille. |
| `comp_support` | Support | Aide/support intégré, réduction de demandes simples et meilleure satisfaction. |
| `link_arrow_down` | Pointe de lien (down) | Sens de progression/flux des graphes et mini-jeux de connexion selon leur orientation. |
| `link_arrow_left` | Pointe de lien (left) | Sens de progression/flux des graphes et mini-jeux de connexion selon leur orientation. |
| `link_arrow_right` | Pointe de lien (right) | Sens de progression/flux des graphes et mini-jeux de connexion selon leur orientation. |
| `link_arrow_up` | Pointe de lien (up) | Sens de progression/flux des graphes et mini-jeux de connexion selon leur orientation. |
| `link_corner_ne` | Angle de lien (ne) | Angles de raccordement des liens selon leur chemin réel dans le graphe. |
| `link_corner_nw` | Angle de lien (nw) | Angles de raccordement des liens selon leur chemin réel dans le graphe. |
| `link_corner_se` | Angle de lien (se) | Angles de raccordement des liens selon leur chemin réel dans le graphe. |
| `link_corner_sw` | Angle de lien (sw) | Angles de raccordement des liens selon leur chemin réel dans le graphe. |
| `link_dashed_h` | Lien optionnel (tirets) horizontal | Dépendances optionnelles/synergies distinctes des prérequis obligatoires. |
| `link_dashed_v` | Lien optionnel (tirets) vertical | Dépendances optionnelles/synergies distinctes des prérequis obligatoires. |
| `link_h` | Lien horizontal (teinte par multiplication) | Segments de vraies dépendances d'arbre/composants et schémas de déploiement. |
| `link_tee_down` | Jonction en T (vers le bas) | Bifurcations de dépendances multiples de l'arbre/composants, réellement raccordées. |
| `link_tee_up` | Jonction en T (vers le haut) | Bifurcations de dépendances multiples de l'arbre/composants, réellement raccordées. |
| `link_v` | Lien vertical | Segments de vraies dépendances d'arbre/composants et schémas de déploiement. |
| `port_in_bottom` | Port in (bottom) | Entrée de module UI dans un graphe ou mini-jeu ; orientation et compatibilité validées. |
| `port_in_left` | Port in (left) | Entrée de module UI dans un graphe ou mini-jeu ; orientation et compatibilité validées. |
| `port_in_right` | Port in (right) | Entrée de module UI dans un graphe ou mini-jeu ; orientation et compatibilité validées. |
| `port_in_top` | Port in (top) | Entrée de module UI dans un graphe ou mini-jeu ; orientation et compatibilité validées. |
| `port_out_bottom` | Port out (bottom) | Sortie de module UI dans un graphe ou mini-jeu ; connexion orientée réellement utilisée. |
| `port_out_left` | Port out (left) | Sortie de module UI dans un graphe ou mini-jeu ; connexion orientée réellement utilisée. |
| `port_out_right` | Port out (right) | Sortie de module UI dans un graphe ou mini-jeu ; connexion orientée réellement utilisée. |
| `port_out_top` | Port out (top) | Sortie de module UI dans un graphe ou mini-jeu ; connexion orientée réellement utilisée. |

## servers — 26 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `cable_conduit` | Passe-cable au sol | Passage protégé de liaison au sol ; organisation d'une zone sans bloquer les accès. |
| `cable_tray_corner` | Chemin de cables suspendu (angle) | Angle du même chemin de câbles, continuité logique et visuelle. |
| `cable_tray_straight` | Chemin de cables suspendu (droit) | Tronçon de chemin de câbles en hauteur, connectivité/organisation des zones. |
| `crac_unit` | Climatiseur de salle informatique | Production de froid de salle, énergie consommée et plafond de charge soutenable. |
| `module_blank_1u` | Obturateur 1U | Canalisation d'air des emplacements vides, amélioration plafonnée de l'efficacité du froid disponible. |
| `module_cable_manager_1u` | Passe-cables 1U | Réduire la surcharge de câblage/organisation du rack, effet plafonné. |
| `module_compute_2u` | Serveur de calcul 2U | Calcul CPU pour recherche, batch et offres de calcul. |
| `module_compute_node_2u` | Noeuds de calcul 2U (2 demi-largeurs) | Nœuds de calcul parallèles, avantage sur travaux parallélisables et non sur toute tâche. |
| `module_gpu_4u` | Serveur GPU 4U | Calcul GPU réservé pour entraînement/inférence IA et offres spécialisées. |
| `module_network_switch_1u` | Switch reseau 1U | Capacité de réseau interne des baies et de leurs modules. |
| `module_patch_panel_1u` | Panneau de brassage 1U | Organisation du brassage et capacité de raccordement, sans créer de débit externe. |
| `module_server_1u` | Serveur 1U | Capacité d'hébergement général par module 1U. |
| `module_storage_2u` | Stockage / NAS 2U (12 baies) | Stockage géré pour hébergement/données de modèles. |
| `module_storage_jbod_4u` | Tiroir de disques 4U (JBOD) | Extension brute de stockage rattachée à une capacité de contrôle compatible ; pas serveur autonome. |
| `module_ups_2u` | Onduleur de baie 2U | Marge de continuité électrique du rack et qualité de service agrégée. |
| `nas_desktop` | NAS de bureau (4 baies) | Stockage partagé initial de projets/données et petite gamme matérielle. |
| `power_distribution_cabinet` | Armoire de distribution electrique | Capacité de distribution électrique du site ; limite de puissance explicite. |
| `rack_compact` | Baie compacte (8U, roulettes) | Support de modules 8U : petite capacité de salle serveur. |
| `rack_cooler_inrow` | Refroidissement de rangee (in-row) | Refroidissement local d'une rangée, alimenté par la capacité énergétique correspondante. |
| `rack_high_density` | Baie haute densite (20U, porte arriere refroidie) | Support haute densité avec meilleure dissipation prévue, coûts/puissance adaptés. |
| `rack_standard` | Baie standard (20U) | Support de modules 20U standard, capacité et contraintes physiques du rack. |
| `security_cage_door` | Porte de cage grillagee | Accès valide d'une zone protégée ; complète la cage sans condamner les postes. |
| `security_cage_panel` | Grille de securite (cage) | Délimiter une zone technique protégée et contribuer à son niveau de service/sécurité. |
| `server_tower_classic` | Serveur tour (pied) | Hébergement local d'entrée de gamme et serveur vendable. |
| `supervision_console` | Console de supervision (NOC) | Poste de supervision d'infrastructure, contribution à l'automatisation et à la coordination du site. |
| `ups_floor` | Onduleur au sol | Capacité de continuité électrique d'une zone, dimensionnée à sa charge. |

## terminal — 21 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `term_cursor_bar` | Curseur barre (saisie) | Curseur de saisie normal dans le terminal du boss. |
| `term_cursor_block` | Curseur bloc (cellule 6x12) | Curseur en mode cellule/navigation de l'historique ou préférence d'affichage documentée. |
| `term_cursor_underline` | Curseur souligne | Alternative de curseur sélectionnable dans les préférences du terminal. |
| `term_frame` | Terminal : cadre + barre de titre | Cadre du terminal complet du boss, contenant une vraie saisie de jeu. |
| `term_input` | Champ de commande | Champ de saisie du mot courant, focus et validation fonctionnels. |
| `term_mark_error` | Marqueur de ligne : Erreur | Faute de saisie locale ou action invalide, sans exécution de commande réelle. |
| `term_mark_info` | Marqueur de ligne : Information | Information de contexte, rendement et consignes courtes. |
| `term_mark_ok` | Marqueur de ligne : Succès | Validation réussie d'un mot ou d'une étape réellement effectuée. |
| `term_mark_prompt` | Marqueur de ligne : Invite | Ligne du mot attendu / invite de travail simulée. |
| `term_mark_running` | Marqueur de ligne : En cours | Tâche ou phase de session active et encore en cours. |
| `term_mark_warning` | Marqueur de ligne : Avertissement | Contrainte de tâche/capacité ou changement de contexte nécessitant attention. |
| `term_progress_cap_left` | Segment de progression (crochet gauche) | Extrémités de la barre segmentée de travail/calibration. |
| `term_progress_cap_right` | Segment de progression (crochet droit) | Extrémités de la barre segmentée de travail/calibration. |
| `term_progress_empty` | Segment de progression (vide) | Cellule vide de la barre de progression de travail actif. |
| `term_progress_full` | Segment de progression (plein) | Cellule pleine de progression, bornée au total de la tâche. |
| `term_progress_half` | Segment de progression (moitie) | Cellule partiellement remplie représentant une fraction réelle de progression. |
| `term_scroll_thumb` | Défilement sombre (curseur) | Poignée de défilement de l'historique du terminal. |
| `term_scroll_track` | Défilement sombre (piste) | Piste de l'historique de session/supervision, qui devient défilant quand nécessaire. |
| `term_selection` | Surlignage de ligne (cellule) | Sélection réelle d'une ligne/historique ou surlignage du mot actif selon mode. |
| `term_surface` | Terminal : surface de texte | Fond de la zone de texte du terminal, distinct du cadre pour panneaux intégrés. |
| `term_titlebar` | Terminal : barre de titre seule | Titre d'une sous-vue de terminal ou de supervision intégrée. |

## vfx — 16 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `vfx_alert` | Alerte | Apparition d'un nouveau problème significatif, une seule fois par changement d'état. |
| `vfx_code` | Code (bulle) | Petit effet de travail actif/dev sur écran, borné et désactivable. |
| `vfx_coin` | Pièce (gain) | Retour visuel discret d'un jalon de vente/recette, pas une particule par transaction. |
| `vfx_heart_morale` | Moral (cœur) | Retour visuel d'amélioration de moral ou récupération. |
| `vfx_hover_ring` | Anneau de survol / selection | Objet ou personnage actuellement survolé. |
| `vfx_idea` | Idée (ampoule) | Nouvelle idée, recherche ou déblocage pertinent. |
| `vfx_select_ring` | Anneau de selection (large) | Objet ou personnage réellement sélectionné. |
| `vfx_sparkle_large` | Étincelle (grande) | Grand jalon de campagne, effet rare et désactivable. |
| `vfx_sparkle_small` | Étincelle (petite) | Réussite ordinaire d'une étape/amélioration. |
| `vfx_success` | Succès | Confirmation de lancement, recherche ou lot validé. |
| `vfx_tile_footprint` | Emprise (pointilles) | Emprise réelle d'un objet pendant le placement. |
| `vfx_tile_hover` | Case survolee | Case de sol actuellement survolée en aménagement. |
| `vfx_tile_invalid` | Placement invalide | Placement/connexion refusé et motif affiché. |
| `vfx_tile_selected` | Case selectionnee | Case/zone sélectionnée dans aménagement ou flux industriel. |
| `vfx_tile_valid` | Placement valide | Placement/connexion valide, accompagné d'une indication non uniquement colorée. |
| `vfx_work` | Travail en cours (engrenage) | Indicateur discret de travail d'un poste/machine actif. |

## wall_patterns — 8 entrées

| ID exact | Nom du pack | Utilité à implémenter |
| --- | --- | --- |
| `pattern_acoustic` | Panneaux acoustiques | Traitement acoustique mural ; contribution plafonnée à la concentration/qualité du studio. |
| `pattern_brick` | Brique interieure | Habillage d'atelier/startup ; identité visuelle et ambiance partagée, sans multiplicateur unique. |
| `pattern_concrete` | Beton banche | Habillage industriel/technique ; lecture des zones et personnalisation. |
| `pattern_paint` | Peinture unie | Habillage mural recolorable du local ; personnalisation de son identité. |
| `pattern_stripes` | Bandes verticales | Habillage signalétique de zone ; identification des espaces et ambiance. |
| `pattern_two_tone` | Deux tons + cimaise | Habillage à deux tons ; personnalisation des locaux et repères de zone. |
| `pattern_wallpaper` | Papier peint discret | Habillage domestique de la chambre et des espaces détente ; ambiance. |
| `pattern_wood_panels` | Lambris bois | Habillage chaleureux de réunion/détente ; contribution plafonnée à l'ambiance. |

## Plans et modules — 16 définitions

| ID exact | Fonction de campagne |
| --- | --- |
| `room_advanced_lab` | Recherches avancées et installation quantique, contraintes froid/calcul/énergie. |
| `room_bedroom_start` | Départ et identité domestique du fondateur, premiers projets/terminal et petite équipe. |
| `room_datacenter` | Capacités d’hébergement, GPU/stockage et services commerciaux à grande échelle. |
| `room_factory_hall` | Lignes connectées de fabrication, buffers, emballage et stockage. |
| `room_hq_floor` | Siège de grande équipe et coordination des sites, sans créer une seconde société. |
| `room_hw_workshop` | Prototypes, validation et assemblage de petites séries matérielles. |
| `room_office_medium` | Bureau d’équipe intermédiaire avec fonctions de confort/coordination. |
| `room_open_space` | Plateau d’équipe important, arbitrage densité/confort et spécialisation. |
| `room_server_compact` | Première installation de serveur/rack et supervision locale. |
| `room_startup_small` | Premier local supplémentaire de petite équipe/atelier léger, coût accessible. |
| `module_kitchenette` | Ensemble de services de repas/hydratation avec capacité de confort définie. |
| `module_lounge` | Capacité de détente/récupération et ambiance. |
| `module_meeting_room` | Capacité de formation/coordination, dont intersites si équipée. |
| `module_pod_4` | Quatre postes compatibles à placer en ensemble, achat et capacité réellement comptés. |
| `module_reception` | Accueil et organisation du site avec équipements du pack. |
| `module_restroom` | Service sanitaire complet, capacité/accès valides sans corvées individuelles. |

## Dernier contrôle avant de conclure

Pour chaque ligne, retrouve un consommateur réel dans le jeu livré. Les effets partagés sont autorisés et doivent rester plafonnés. Les sprites/ports/états rares doivent être réellement atteignables. Les lignes de cette annexe ne remplacent ni le code, ni les données de gameplay, ni les essais. Si le pack diffère, mets à jour l’affectation par comparaison d’IDs et explique chaque ajout/retrait.
