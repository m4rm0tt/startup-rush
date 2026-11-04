# Réserve d'idées : 100 améliorations (rédigée le 3 novembre 2026)

**Réserve d'idées, pas une commande.** Liste écrite *avant* les six lots de la phase Steam : plusieurs points sont depuis faits
(format des nombres, texte agrandi, résolutions, plein écran, journaux d'erreur, suppression confirmée, objectif final, bilan,
début d'anglais). Voir `docs/CHECKLIST_STEAM_V1.md` pour l'état réel. Beaucoup d'idées sont **hors périmètre** de la consigne
(manette, mods, concurrents, météo, etc.) : ne pas les développer sans nouvelle demande.

J'ai lu le code et les docs du jeu et regardé toutes les captures. Voici 100 améliorations, groupées par thème. Les deux premiers groupes sont ce qui me paraît le plus important pour une sortie sur Steam.

Ce que je n'ai pas vérifié en détail, donc à confirmer avant d'y toucher :
- l'événement aléatoire (n° 39) et les succès Steam (n° 81) : je n'ai pas cherché à fond s'il en existe déjà une forme ;
- les petites tailles de fenêtre (n° 7) et la manette (n° 8) : simplement jamais testées.

### Indispensable pour Steam (finition et plateforme)
- **Nombres au format du jeu, pas de la machine** : l'affichage de l'argent suit la langue du système (87,760 sur un PC en anglais) ; forcer le format suisse ou français partout.
- **Plusieurs emplacements de sauvegarde** : nommer, dupliquer et supprimer des parties depuis le menu, avec la date et une miniature.
- **Sauvegarde automatique tournante** : 3 sauvegardes automatiques en rotation, pour revenir en arrière après une erreur ou un fichier abîmé.
- **Succès Steam** : une quarantaine (premier lancement, 1 000 utilisateurs, première levée de fonds, fabriquer un GPU, etc.).
- **Sauvegarde dans le cloud Steam** : synchroniser les parties entre plusieurs PC.
- **Taille du texte réglable** (BUG-010) : option « Texte plus grand », en 24 px net.
- **Interface adaptée à toutes les résolutions** : vérifier 1280 × 720, 1366 × 768, l'ultra-large et la 4K ; les menus ne doivent jamais être coupés.
- **Prise en charge de la manette et du Steam Deck** : navigation à la croix et saisie au terminal avec le clavier virtuel.
- **Touches modifiables** : changer les raccourcis (vitesses, menus, caméra) dans les réglages.
- **Traduction anglaise** : sortir tous les textes dans des fichiers de langue ; l'anglais est indispensable pour vendre sur Steam.
- **Écran de chargement et démarrage rapide** : barre de chargement, et préchargement des images pour éviter l'écran figé au lancement.
- **Rapport de plantage** : en cas d'erreur, écrire un journal et proposer d'ouvrir le dossier, avec une sauvegarde de secours automatique.
- **Performance des grandes parties** : rendu des grandes salles (le test actuel d'image en moins d'une demi-seconde est limite), et calcul de nuit plus rapide.
- **Réglages graphiques** : plein écran, fenêtré, fenêtré sans bordure, synchronisation verticale, limite d'images par seconde.
- **Réglages audio détaillés** : volumes séparés pour la musique, les effets, l'ambiance et l'interface, et coupure du son quand la fenêtre n'est pas active.
- **Accessibilité** : mode daltonien (barres et alertes pas seulement en couleur), option de réduction des animations, sous-titres des sons importants.
- **Page de crédits et mentions légales** : licences des polices, de la musique et du pack d'assets.

### Bugs et finitions repérés
- **4e icône de vitesse** (BUG-013) : une icône dédiée à la vitesse 4, au lieu du seul chiffre.
- **Test de vitesse d'affichage** : à mesurer sur un vrai PC Windows et à corriger si besoin.
- **Validation à la souris** : passer toute la liste `BUGFIX_VALIDATION.md` sous Windows.
- **Personnages assis manquants** : moins de personnes assises que de postes sur certaines captures (déjà noté dans la doc, jamais résolu).
- **Opérateurs debout** : les opérateurs de production ne sont pas assis à leur station, seulement une bulle à côté.
- **Carton de déménagement sans fonction** : l'objet `moving_box` n'a toujours aucun usage.
- **Dossier PDF obsolète** : le dossier et le guide décrivent encore la version 2.1 ; à refaire ou à retirer.
- **Équilibrage « trop de marketing »** : une partie sur cinq survit ; décider si c'est voulu.
- **Matériel peu rentable** : une carte bon marché ne rembourse pas un atelier en 18 mois ; revoir le prix ou le loyer.
- **Pas de transfert de stock automatique entre sites** : proposer le réapprovisionnement d'un site vers un autre.

### Début de partie et prise en main
- **Choix de difficulté** : Détendu (plus d'argent, marché plus tolérant), Normal, Requin (concurrents agressifs, banques strictes).
- **Mode bac à sable** : argent illimité et tout débloqué pour construire librement, sans les succès.
- **Scénarios** : départs imposés (« Reprendre une boîte en faillite », « Studio de jeux en 2 ans », « Fabriquer son GPU ») avec des objectifs.
- **Choix du parcours du fondateur** : plus de profils de départ (designer, commercial, chercheur) avec des bonus différents.
- **Choix de la ville de départ** : Lausanne, Genève, Zurich, avec des loyers, des salaires et un public différents.
- **Encyclopédie en jeu** : un « Wiki » consultable (composants, objets, formules simplifiées) au lieu de tout chercher dans les bulles.
- **Conseiller contextuel** : un mentor qui propose une action utile quand le joueur stagne depuis plusieurs jours.
- **Objectifs à court terme** : 3 objectifs du moment toujours visibles (« Atteins 100 utilisateurs », « Embauche ton premier dev »), avec une petite récompense.

### Marché, concurrence et produits
- **Vrais concurrents** : des entreprises rivales avec un nom, un logo, des sorties, des baisses de prix et des rachats, au lieu d'un simple chiffre d'attrait.
- **Fil d'actualité du secteur** : annonces des concurrents, tendances, nouveaux publics.
- **Tendances du marché** : publics qui montent et descendent (la VR à la mode, puis passée), pour qu'il faille choisir où aller.
- **Événements aléatoires** : panne du cloud, fuite de données, buzz viral, mention dans la presse, avec des choix à faire.
- **Prix et promotions** : soldes temporaires, version gratuite avec achats en jeu, prix par région.
- **Critiques de presse** : notes chiffrées à la sortie (style Metacritic) qui pèsent sur les ventes.
- **Avis des utilisateurs lisibles** : quelques vrais commentaires générés (« Trop de bugs à la connexion ») qui renvoient au composant en cause.
- **Feuille de route publique** : annoncer une fonctionnalité monte la communauté, mais la décevoir si elle est en retard.
- **Bêta et accès anticipé** : sortir un produit en bêta pour gagner de l'argent tôt, avec plus de tolérance aux bugs.
- **Fin de vie d'un produit** : arrêter, vendre ou laisser en maintenance un vieux produit, avec ce que ça change pour la communauté.
- **Suites et licences** : un produit à succès peut avoir une suite qui hérite d'une partie de sa communauté.
- **Partenariats et intégrations** : brancher deux de ses produits ensemble, ou s'associer à une entreprise rivale.
- **Contrats clients** : missions de commande (sous-traitance) pour financer le début de partie.
- **Plateformes de distribution** : choisir où publier (magasin d'applications, site web, console), avec leurs commissions et leurs règles.
- **Ventes du matériel en magasin** : choisir des distributeurs, les stocks en magasin et les retours.

### Équipe et personnages
- **Traits de caractère** : perfectionniste, rapide, râleur, mentor, noctambule… qui changent le travail et l'ambiance.
- **Épuisement professionnel** : trop d'heures supplémentaires mènent à un arrêt maladie, avec des signes à surveiller.
- **Entretiens annuels et augmentations** : demandes de salaire, menace de démission, contre-offres.
- **Débauchage par les concurrents** : un concurrent tente de recruter tes meilleurs employés.
- **Relations entre collègues** : affinités et tensions qui pèsent sur une équipe.
- **Postes de direction** : directeur technique, directeur marketing, DRH, qui automatisent une partie de la gestion.
- **Stagiaires et alternants** : peu chers et lents, mais formables ; ils peuvent être embauchés à la fin.
- **Télétravail** : une partie de l'équipe travaille à distance (moins de loyer, coordination plus difficile).
- **Événements d'équipe** : afterwork, séminaire, hackathon (moral, idées, fatigue).
- **Progression visible des employés** : courbe d'expérience, spécialités débloquées, titres (junior → senior → expert).
- **Fiches de personnage enrichies** : courte biographie, centres d'intérêt, historique dans l'entreprise.
- **Recrutement actif** : passer par un chasseur de têtes, publier une annonce ciblée, aller dans des salons.

### Argent et entreprise
- **Levées de fonds par étapes** : seed, série A, B, C, avec des investisseurs qui ont leurs exigences (croissance, sièges au conseil).
- **Entrée en bourse** : objectif de fin de partie, puis cours de l'action et actionnaires à satisfaire.
- **Rachat d'entreprises** : acheter un petit concurrent pour ses produits ou son équipe.
- **Se faire racheter** : offres de rachat à accepter ou refuser, ce qui peut être une fin de partie.
- **Subventions et concours** : aides publiques, concours de start-up, incubateur.
- **Impôts et TVA** : déclaration simplifiée à la fin de l'année, qui donne des décisions à prendre.
- **Graphiques financiers sur la durée** : trésorerie, revenus et utilisateurs sur 12 ou 36 mois, avec des courbes par produit.
- **Prévisions de trésorerie** : « Au rythme actuel, tu tiens jusqu'au… », et ce que changerait une embauche.
- **Rapport trimestriel** : un écran de bilan tous les 3 mois avec les faits marquants, comme un vrai conseil d'administration.

### Bureaux et construction
- **Décoration à thèmes** : lots d'objets (scandinave, industriel, gamer) avec un bonus d'ambiance quand le lot est complet.
- **Plus d'objets** : cafetière haut de gamme, baby-foot, plantes en mur, panneaux acoustiques, avec de vrais effets.
- **Pièces et cloisons** : construire des murs pour créer une salle de réunion, une cuisine ou un open space.
- **Éclairage jour et nuit** : lumière qui change avec l'heure, lampes qui s'allument le soir.
- **Météo et saisons** : neige à la fenêtre l'hiver, chaleur l'été (climatisation utile), décorations de Noël.
- **Copier-coller d'aménagement** : sélectionner plusieurs objets, les dupliquer, enregistrer un plan d'aménagement.
- **Annuler et rétablir** dans l'aménagement : Ctrl+Z et Ctrl+Y.
- **Vue en coupe et rotation** : tourner la vue de 90° pour voir derrière les meubles.
- **Mode photo** : masquer l'interface, cadrer et exporter une capture pour la partager.
- **Visite des bureaux** : animation au premier déménagement dans un nouveau local, avec les personnages qui découvrent les lieux.

### Vie dans la scène
- **Animations plus riches** : pauses café, discussions, réunions debout, personnages qui fêtent une sortie.
- **Bulles d'humeur** : petites icônes au-dessus des têtes (fatigue, idée, frustration, joie).
- **Visiteurs** : investisseurs, journalistes, clients qui passent au bureau, avec un effet.
- **Animaux de bureau** : chien ou chat de l'entreprise (moral) ; populaire auprès des joueurs de jeux de gestion.
- **Ambiance sonore** : bruits de clavier, machine à café, conversations, serveurs, selon le local.
- **Plus de musiques** : seulement 4 morceaux aujourd'hui ; il en faut une dizaine (calme, crise, succès, nuit).
- **Effets de fête** : confettis et son pour les grands jalons (premier million, 10 000 utilisateurs).

### Terminal et mini-jeux
- **Mots du terminal liés au composant** : des mots du vocabulaire réel du composant en cours (« oauth », « jwt » pour la connexion).
- **Défis de frappe chronométrés** : sprint de 30 secondes facultatif pour un gros bonus, avec un record personnel.
- **Mini-jeu de débogage** : trouver la ligne fautive dans un court extrait de code pour corriger un bug critique.
- **Mini-jeu d'entretien d'embauche** : poser 3 questions pour révéler les vrais traits d'un candidat.
- **Mini-jeu de négociation** : négocier un salaire, un loyer ou une levée de fonds avec un curseur et des arguments.
- **Mini-jeu de pitch** : choisir l'ordre des arguments devant des investisseurs.
- **Mini-jeux matériel et IA** : calibrer une ligne de production, régler l'entraînement d'un modèle (prévus dans la spécification, à étendre).
- **Statistiques du terminal** : mots par minute, précision et meilleure série dans le temps, pour voir ses progrès.

### Fin de partie et rejouabilité
- **Conditions de victoire claires** : entrée en bourse, licorne (1 milliard de valorisation), rachat géant, ou objectif choisi au départ.
- **Écran de fin de partie** : chronologie de l'entreprise, chiffres clés, meilleurs produits, et score.
- **Héritage entre parties** : petits bonus débloqués pour les parties suivantes (style « rogue-lite léger »), comme le prévoyait l'ancien concept.
- **Défis hebdomadaires** : même graine pour tous, avec un classement Steam.
- **Graine de partie partageable** : rejouer la même partie qu'un ami.
- **Arbre de développement à branches exclusives** : des choix qui ferment d'autres voies, pour que chaque partie soit différente.

### Interface et confort de jeu
- **Recherche dans les menus** : une barre de recherche (Ctrl+K) pour trouver un objet, une personne ou un projet.
- **Tableau de bord personnalisable** : épingler les chiffres qu'on veut suivre en haut de l'écran.
- **Infobulles comparatives** : survoler un objet à acheter montre ce qu'il change par rapport à l'actuel (+3 moral).
- **Historique des notifications** : un journal filtrable avec un bouton pour aller à l'action concernée (déjà partiel, à compléter).
- **Raccourcis et file d'actions** : actions en lot (former 3 personnes, acheter 5 bureaux).
- **Mode file de nuit** : programmer des actions à exécuter le lendemain matin.
- **Glisser-déposer d'employés** : déplacer un personnage sur un projet pour l'y affecter.
- **Tutoriels avancés** : courtes leçons facultatives pour la production, l'IA et le quantique quand on les débloque.

### Communauté et contenu
- **Prise en charge des mods** : objets, publics et composants ajoutés par fichiers, puis Steam Workshop.
- **Éditeur de scénarios** : créer et partager ses propres défis.
- **Démo Steam** : une version limitée aux 3 premiers mois de jeu, pour le Steam Next Fest.
- **Page Steam et bande-annonce** : captures, GIF du terminal et vidéo, grâce au mode photo et aux captures automatiques.
- **Retours depuis le jeu** : un bouton « Envoyer un retour » qui joint la sauvegarde et le journal, utile pendant l'accès anticipé.

Pour une sortie sur Steam, je ferais en priorité :
1. les sauvegardes multiples et automatiques ;
2. la traduction anglaise ;
3. les vrais concurrents avec les événements ;
4. les traits des employés avec l'épuisement ;
5. les conditions de victoire avec l'écran de fin ;
6. les succès Steam.

Ce sont les choses que les joueurs de jeux de gestion remarquent en premier. Dis-moi lesquelles tu veux lancer.