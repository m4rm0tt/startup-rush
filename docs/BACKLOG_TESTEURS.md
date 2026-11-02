# Backlog des testeurs : évolutions en attente

Les propositions des deux sessions de test (`docs/test.md` et les remarques du testeur 2). **Aucune n'est implémentée** :
ce sont des évolutions, pas des corrections. Les bugs sont suivis dans `BUGFIX_TRACKER.md`.

## Création et tutoriel
- Rendre obligatoire le nom de l'entreprise à sa création.
- Ajouter un tutoriel.
- Accélérer les premiers projets : le début est jugé « injouable » de lenteur. Pistes du testeur : un tutoriel qui renforce
  l'écriture, ou une activité plus ludique.
- Faire dormir le joueur dans son lit (c'est sa chambre) plutôt que le faire partir.
- Mettre en pause avec la barre d'espace. Note : **Espace met déjà en pause** (`GameScreen.HandleKey`). Si le testeur ne l'a pas
  vu, c'est un problème de découverte, ou Espace est pris par un autre élément (le terminal ouvert, par exemple) : à vérifier
  en jeu.
- Ne plus mettre le jeu en pause par défaut dans les menus. Note : c'est un réglage existant (`PauseInLargeMenus`), il s'agit
  de changer sa valeur par défaut.

## Aménagement et personnalisation
- Prévisualiser le sol et les murs avant de les poser.
- Choisir des carreaux ou des murs précis pour changer leur couleur.
- Changer la couleur des objets (canapés, cloisons, casiers, tables…).
- Supprimer les objets déjà présents dans un bâtiment lors d'un déménagement.
- Afficher la capacité utilisée d'un espace de travail (ex. 4/8 bureaux).
- Faire jeter les déchets au sol par les employés quand il n'y a pas de poubelle.

## Projets et production
- Déplacer les composants pour changer leur ordre de développement.
- Moins de composants, mais plus personnalisables, avec une vraie sensation d'amélioration.
- Revoir la progression des projets et l'équilibrage des employés (« trop facile » à partir d'un certain effectif).
- Permettre de s'attacher à un produit et de le faire évoluer longtemps, plutôt que d'enchaîner les projets.
- Préparer manuellement de vraies mises à jour au lieu de publier chaque composant tout seul.
- Déplacer « Production » et « Stock » dans « Projets », avec des catégories « Développement » et « Stock ».

## Gameplay et expérience
- Moins de texte, moins de temps passé dans les menus.
- Revoir les mini-jeux de publication et des actions importantes (jugés ennuyeux et peu beaux).
- Mini-jeux de terminal variés, qui rappellent la vraie informatique tout en restant accessibles.
- Mieux expliquer l'économie et les projets d'IA (à quoi sert de publier, d'où vient l'argent).
- Indicateurs sur place pour les problèmes de matériel : un point d'exclamation au-dessus d'un serveur qui a besoin d'un switch,
  avec ce qui manque, plutôt que dans les notifications.

## Décisions de direction artistique liées aux bugs
- **Taille du texte (BUG-010)** : la police du pack n'est nette qu'en multiples de 12 px, donc presque tout le texte courant fait
  12 px. Pour l'agrandir, il faut choisir : texte courant en 24 px (les menus devront être revus pour la place), ou une
  échelle non entière (police moins nette). À trancher avec l'auteur du jeu.
- **Onglet actif (BUG-012)** : la barre orange fait partie du sprite `hud_tabs:active` du pack.
- **Vitesses (BUG-013)** : le pack n'a que trois icônes de vitesse pour quatre vitesses.
