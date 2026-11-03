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
| B1 Aperçu des sols et murs | terminé | Choisir une finition la montre dans la scène sans payer (copie du local, `Premises.PreviewCopy`) ; bandeau « Aperçu » avec le prix et « Faire les travaux » / « Annuler » ; en mode zone, case ou mur visé, l'aperçu suit la souris et le clic pose. Test `APreviewCopyNeverTouchesThePremises`, captures. |
| B2 Choisir un mur ou une zone de sol | terminé | Sol : tout le local, une zone (celle sous la case cliquée) ou une seule case ; murs : les deux, ou un mur visé dans la scène. Couleur au choix (canaux « floor » et « wall » du pack). Coût = cases ou segments couverts. Sauvegardé (`LeftPattern`, `RightPattern`, couleurs des murs, zones de sol). |
| B3 Couleurs des meubles | terminé | Un seul système : les canaux recolorables du masque de chaque objet du pack (tissu, bois, couleur principale, métal, détails, feuillage) ; teintes du pack ; fiche de l'objet, « Couleurs d'origine ». Gratuit, sauvegardé, suit l'objet dans les cartons. |
| B4 Retirer les objets livrés | terminé | Les objets livrés se retirent (cartons) ou se jettent, un par un ou « Retirer le décor » ; restent en place : sanitaires, cuisine équipée, installations techniques (`FurnitureDef.Structural`) et le lit de la chambre. Confirmation avant de retirer. |
| B5 Capacité des espaces de travail | terminé | « Postes : 4 / 7 » : postes complets sur le plafond réel (bail de la chambre, sinon postes installés + bureaux compacts qui tiennent encore selon les règles de pose). Tuiles postes / équipe / bureaux en plus. |
| B6 Déchets | terminé | Chaque jour de travail, qui n'a pas de corbeille (corbeilles et bacs de tri couvrent 3 et 8 personnes) laisse parfois un déchet par terre (hasard séparé, 24 au plus) ; −0,75 de moral par déchet (5 au plus) ; clic pour ramasser, « Tout ramasser » ; avec assez de corbeilles l'équipe en ramasse un par jour. Section Propreté. |
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
| E4 Indicateurs sur les équipements | terminé | « ! » au-dessus de l'objet en cause (baie sans switch, tiroir de disques sans stockage 2U, cryostat incomplet, site qui manque d'électricité, de froid ou de débit), cliquable ; la fiche liste « Ce qui manque » avec un bouton d'achat ; disparaît dès que c'est réglé (test). Notifications gardées. |
