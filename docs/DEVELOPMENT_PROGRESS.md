# Suivi du développement : demandes des testeurs (novembre 2026)

Mission : terminer ce qui était en suspens après les corrections de bugs (`BUGFIX_TRACKER.md`), développer les évolutions
demandées par les deux testeurs (`BACKLOG_TESTEURS.md`, `test.md`) et améliorer l'expérience de jeu. Branche `new-assets`.

Statuts : **terminé** (comportement vérifié par un test automatique ou une capture regardée), **partiel**, **bloqué**,
**non commencé**. Un « terminé » ne veut pas dire « validé à la souris par un humain » : voir `BUGFIX_VALIDATION.md`.

## Environnement de cette session

- VPS Linux sans écran (Ubuntu 24.04), SDK .NET 9 dans `/root/.dotnet` (pas dans le PATH : `export PATH=$PATH:/root/.dotnet`).
- Le jeu vise Windows : ici, pas de vraie fenêtre ni de souris. Vérifications possibles : tests du moteur, tests d'interface
  Avalonia.Headless (vrai rendu Skia), et captures PNG de l'interface rendue sans fenêtre (`UiTests/Shots.cs`, catégorie
  `Shots`, images écrites dans le dossier donné par `STARTUPRUSH_SHOTS`).
- `git push` impossible depuis ce VPS (pas d'identifiants GitHub) : les commits sont locaux, à pousser depuis un poste authentifié.

## Plan (ordre des lots)

1. Création de partie, tutoriel et contrôles : A1 nom obligatoire, A4 pause, A3 lit, A2 tutoriel.
2. Aménagement et personnalisation : B5 capacité, B4 retirer les objets livrés, B3 couleurs, B1/B2 aperçu et choix des sols et
   murs, B6 déchets, E4 indicateurs sur les équipements.
3. Projets : C2 ordre des composants, C4 vraies mises à jour (versions préparées puis publiées), C3 approches des composants et
   effets lisibles, C5 progression d'un produit dans la durée, C1 Production et Stock dans Projets.
4. Équilibrage : D1 début de partie, D2 grandes équipes, D3 bilan des bots.
5. Expérience : E2 mini-jeux de terminal variés, E3 IA expliquée, E1 moins de texte.
6. Bugs ouverts (BUG-009, 010, 012, 013), validation générale, documentation.

## État des tâches

| Tâche | Statut | Détail |
|---|---|---|
| A1 Nom d'entreprise obligatoire | terminé | `Campaign.CompanyNameError` (vide, espaces, < 2 ou > 40 caractères refusés ; nom nettoyé), message sous le champ, test de sauvegarde (`TesterRequestsTest`), capture. |
| A2 Tutoriel interactif | terminé | 11 étapes validées par les vraies actions (`Campaign.Tutorial.cs`), carte de consigne non bloquante, menu concerné marqué d'une étoile, anneau sur le fondateur, « Passer », +1 point à la fin ; sauvegardé. Tests moteur et interface. |
| A3 Dormir dans son lit | terminé | Le fondateur reste dans sa chambre : assis à son bureau le soir (`Activity.Home`), dans son lit de 23 h à 7 h (`Activity.Sleeping`, bulle « zZ » cliquable). Pas de pose couchée dans le pack : bulle au-dessus du lit. Sans lit (bureaux), il rentre chez lui. |
| A4 Pause (Espace, menus) | terminé | Plus de pause dans les grands menus par défaut (réglages déjà écrits mis à jour une fois, `Settings.Version` 2). Espace ou une vitesse relancent le temps arrêté par un grand menu. Pauses gardées : menu pause, confirmations, mini-jeux, saisie (réglage). |
| B1 Aperçu des sols et murs | non commencé | |
| B2 Choisir un mur ou une zone de sol | non commencé | |
| B3 Couleurs des meubles | non commencé | |
| B4 Retirer les objets livrés | non commencé | |
| B5 Capacité des espaces de travail | non commencé | |
| B6 Déchets | non commencé | |
| C1 Production et Stock dans Projets | non commencé | |
| C2 Ordre des composants | non commencé | |
| C3 Profondeur des composants | non commencé | |
| C4 Vraies mises à jour | non commencé | |
| C5 Progression d'un produit | non commencé | |
| D1 Début de partie | non commencé | |
| D2 Grandes équipes | non commencé | |
| D3 Équilibrage général | non commencé | |
| E1 Moins de texte | non commencé | |
| E2 Mini-jeux | non commencé | |
| E3 IA expliquée | non commencé | |
| E4 Indicateurs sur les équipements | non commencé | |
