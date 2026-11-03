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
| C1 Production et Stock dans Projets | terminé | Projets a trois catégories quand le matériel existe : Développement (portefeuille), Production (lignes, ordres), Stock (stock, achats, transferts). Plus de menu séparé : l'aménagement, la fiche d'un produit matériel et les notifications ouvrent Projets › Production ou Stock. Test d'interface. |
| C2 Ordre des composants | terminé | Flèches ▲ ▼ dans « En cours » (`Campaign.MoveQueued`) : l'équipe et le terminal suivent l'ordre de la file ; une évolution ne passe pas devant ce dont elle dépend (message clair) ; sauvegardé. Test. |
| C3 Profondeur des composants | terminé | Façon de faire à chaque ajout ou amélioration : Rapide (−35 % de travail, qualité −12, peu testé), Équilibré, Soigné (+35 %, qualité +10, bien testé). Chaque carte dit ce qu'elle apporte vraiment (« +3,6 pts d'attrait »), deux « Recommandé », les composants qui n'apportent presque rien à ce public rangés dans « Autres composants ». Intégration : +4 % de travail par composant au-delà de trois (40 % au plus). Tests. |
| C4 Vraies mises à jour | terminé | Ce que l'équipe termine sur un produit en ligne attend la prochaine version (`Product.Staged`) : section « Version N en préparation » (prêt, en file, attrait avant → après), publication avec un mini-jeu facultatif (jusqu'à −50 % des bugs de la version), historique des versions et bilan une semaine après. Réglage « Publier chaque évolution dès qu'elle est prête ». La délégation et l'automatisation publient à ta place. Tests moteur et interface. |
| C5 Progression d'un produit | terminé | Communauté (0 à 100 %) construite par chaque version (plus forte si la version apporte beaucoup) : jusqu'à −30 % de départs et +50 % de bouche-à-oreille ; elle s'effrite après 45 jours sans version. Une version qui apporte plus fait plus parler d'elle. +1 point de développement à 3 mises à jour d'un produit, +2 à 8. Tests. |
| D1 Début de partie | non commencé | |
| D2 Grandes équipes | non commencé | |
| D3 Équilibrage général | non commencé | |
| E1 Moins de texte | non commencé | |
| E2 Mini-jeux | terminé | Quatre nouveaux mini-jeux de terminal, chacun avec un geste différent : Journal (cocher les lignes [ERREUR], avec des pièges), Pare-feu (passer ou bloquer selon deux règles), Fusion (garder A ou B selon la règle), Commande (taper la commande de mise en ligne). Ils alternent d'une version à l'autre (jamais deux fois le même de suite), et la difficulté monte avec les sorties déjà faites. Le tout premier lancement reste le déploiement. Tests et captures. |
| E3 IA expliquée | non commencé | |
| E4 Indicateurs sur les équipements | terminé | « ! » au-dessus de l'objet en cause (baie sans switch, tiroir de disques sans stockage 2U, cryostat incomplet, site qui manque d'électricité, de froid ou de débit), cliquable ; la fiche liste « Ce qui manque » avec un bouton d'achat ; disparaît dès que c'est réglé (test). Notifications gardées. |
