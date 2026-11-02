# Validation manuelle après les corrections des tests

À faire **à la souris, dans le vrai jeu** (les tests automatiques ne pilotent pas une vraie souris Windows). Cocher, noter ce qui
cloche avec une capture d'écran et la sauvegarde (`%AppData%\StartupRush\campaigns\`). Jouer à la vitesse 2 ou 3 : c'est
quand le jeu tourne que les anciens bugs (menus reconstruits chaque seconde) se voyaient.

## Création d'une partie
- [ ] Nouvelle partie, nom d'entreprise, premier projet : tout s'enchaîne sans clic perdu.
- [ ] Le terminal s'ouvre (Entrée), les mots avancent, Échap le ferme et la vitesse d'avant revient.

## Menus : ouverture, fermeture, clics
- [ ] P, T, B, D, F ouvrent et ferment leur menu ; Échap ferme d'abord le menu, puis ouvre la pause.
- [ ] Chaque bouton répond **au premier clic**, jeu en marche, y compris en cliquant vite.
- [ ] Le curseur ne clignote pas en restant immobile sur un bouton, un menu, la barre du haut ou la scène.
- [ ] Une infobulle reste ouverte tant que la souris reste dessus (catalogue, objets de la scène, vitesses).

## Défilement dans les catalogues
- [ ] Aménagement : faire défiler tout en bas, choisir un meuble, le poser : la liste **reste où elle était**.
- [ ] La barre de défilement de droite se tire à la souris sans lâcher ; la molette marche partout.
- [ ] Les flèches haut et bas de la barre marchent.
- [ ] Changer de rubrique avec la liste déroulante : elle reste ouverte jusqu'au choix et ne se referme pas seule.

## Placement et sélection des meubles
- [ ] L'objet en pose s'affiche au-dessus de la liste (bandeau « Poser : … ») et le reste en faisant défiler.
- [ ] Double-cliquer sur un objet de la scène le sélectionne **sans** le déplacer (la fiche s'ouvre).
- [ ] Fiche d'un objet acheté : Déplacer, Tourner, Ranger et Revendre sont entiers, rien n'est coupé à droite.
- [ ] Survoler une armoire ou un objet haut : il reste entier et opaque.
- [ ] Poser une baie haute densité et un chemin de câbles sur la même case, dans un ordre puis dans l'autre : le chemin de
      câbles passe toujours par-dessus.

## Serveurs
- [ ] Survoler un serveur du catalogue : sa description reste affichée.
- [ ] Une baie sans switch : le problème est signalé (notifications et section Infrastructure).

## Projets
- [ ] Onglets d'un projet : les onglets inactifs se lisent ; noter si la barre orange gêne toujours.
- [ ] « Préparer une mise à jour » et les autres boutons se lisent sans lettres empâtées.
- [ ] IA : publier un modèle sans produit qui l'utilise, puis avec un produit d'IA : le message dit ce qui change.

## Déménagement et cartons
- [ ] Déménager : la notification « … dans les cartons » ouvre l'aménagement **sur les cartons**.
- [ ] Cliquer les cartons dans la scène, puis « Ouvrir les cartons » : même chose.
- [ ] Avant d'installer les postes, une alerte s'affiche devant la porte. La cliquer : la fiche dit ce qui manque.
- [ ] Installer un poste complet, **même en pause** : l'alerte disparaît aussitôt.

## Contrôles du temps
- [ ] Laisser passer la nuit avec « nuits accélérées » : l'étiquette apparaît sans déplacer les boutons de vitesse.
- [ ] Ouvrir un grand menu avec la pause automatique : l'étiquette « PAUSE » ne déplace rien non plus.
- [ ] Pause (Espace), vitesses 1 à 4 (touches et boutons).

## Sauvegarde et reprise
- [ ] Charger une sauvegarde faite **avant** ces corrections : elle s'ouvre et la partie continue normalement.
- [ ] Sauvegarder, quitter, Continuer : on retrouve la même partie.
