# Startup Rush : règles de la simulation

Ce document décrit les règles **réellement implémentées** dans `Core/`. Il sépare deux choses :

- **Décisions du cahier des charges** (tes choix) : marquées ✅. Elles ne se discutent pas ici.
- **Valeurs de réglage proposées** : marquées ⚙️. Elles sont toutes dans `Core/campaign/Tuning.cs` (et dans les catalogues cités), et restent à calibrer avec de vraies parties. `BALANCE_REPORT.md` montre ce qu'elles donnent avec des bots.

## 1. Cadre

- ✅ Jeu PC en C#/Avalonia. Simulation d'entreprise tech **persistante**, sans runs, sans tirages d'atouts, sans lingots, sans victoire automatique.
- ✅ On commence dans **son appartement**, visible et aménageable, puis on peut louer de vrais locaux.
- ✅ Activités exclusivement tech. Monnaie : CHF. Ton romand.
- ✅ Une mauvaise gestion grave fait **perdre la partie** (liquidation) ; la sauvegarde n'est jamais effacée.
- ✅ Interface : scène dominante, barre d'état discrète, barre d'outils, menus ouvrables (inspiration d'ergonomie, pas de copie).

## 2. Temps

| Élément | Règle |
|---|---|
| Horloge | Une seule horloge : la minute de jeu depuis le lundi 4 janvier 2027, 0 h. Tout est déterministe à partir d'elle et de la graine. |
| Journée | ⚙️ Arrivées vers 8 h 25-8 h 55, travail de 9 h à 17 h, pause de midi de 12 h à 13 h, une pause café le matin et l'après-midi s'il y a un coin pause. Horaire étendu jusqu'à 20 h au choix. Pas de travail le week-end. |
| Vitesses | ⚙️ Pause, puis 5, 20, 60 et 180 minutes de jeu par seconde réelle (touches 1 à 4, Espace pour la pause). Le temps suit le temps réel mesuré, jamais le nombre d'images ; un écart de plus de 0,1 s est plafonné (pas de rattrapage après une fenêtre gelée). |
| Nuits | ⚙️ Quand personne n'est au bureau, le temps passe ×8 (réglage « Nuits accélérées », désactivable). |
| Menus | ⚙️ La pause, les confirmations et (par défaut) les grands menus Produits et Finances arrêtent le temps ; les autres menus le laissent couler. La barre du haut affiche « PAUSE : … » avec la raison. Réglable. |
| Hors ligne | Rien ne se passe quand le jeu est fermé. |
| Comptabilité | Chaque nuit à minuit : marché, encaissements, échéances, équipe, situation financière. Le 1er du mois : loyer, charges, hébergement et marketing du mois écoulé, remboursements. Le 25 : salaires et rémunération du fondateur. Le lundi : nouveaux candidats et sauvegarde automatique. |

Chaque événement périodique a une clé (« rent:2027-02 », « salary:12:2027-02 »...) gardée dans la sauvegarde : il ne peut pas se produire deux fois, même après un rechargement.

## 3. Local et aménagement

- Grille vue de dessus, **projection 3/4 unique** (voir `FURNITURE_MANIFEST.md`). Porte dans le mur du bas ; la case d'entrée reste libre.
- **Appartement** (✅ départ) : 14 × 9 cases, partie privée (6 colonnes : lit, cuisine, frigo, canapé, table) où l'on ne pose pas de matériel de travail (plantes et décor admis). ⚙️ 3 postes au plus. Pas de loyer séparé : il est compris dans la rémunération du fondateur.
- **Postes** : un bureau, une chaise **sur la case juste derrière**, un ordinateur posé **dessus**, et un accès libre depuis la porte. Une chaise seule ne fait rien. On attribue un poste à une personne en cliquant le bureau.
- **Placement** : prévisualisation verte ou rouge, refus avec la raison : hors du local, case prise, entrée bloquée, partie privée, place de la chaise d'un autre bureau, façade d'un équipement, ou passage coupé vers un poste existant. Déplacer, ranger dans les cartons, revendre (⚙️ 50 % du prix, avec confirmation).
- **Matériel** ⚙️ (`Core/space/Furniture.cs`) : ordinateurs ×0,85 / ×1,00 / ×1,10 (le 2e saut rapporte moins que le 1er), qualité +0,04 pour la station ; chaise ergonomique : fatigue −20 % ; plantes +1 de moral chacune, 6 au plus ; machine à café : pauses +12 d'énergie, 1 machine pour 8 personnes ; tableau blanc : +1 personne coordonnée sans perte (2 au plus) ; bibliothèque : formations +15 % (2 au plus) ; tapis : décor sans effet.
- **Déménagement** ⚙️ : petit bureau 2 300 CHF/mois, plateau 5 400, étage 12 500 ; garantie de 3 mois de loyer (rendue au départ), forfait de déménageurs, 35 CHF par meuble. **Tout le mobilier part dans les cartons** : rien n'est perdu ni à racheter, on le réinstalle.
- **Déplacements** : recherche de chemin en largeur (4 directions), une case par minute de jeu, l'interface fait glisser les personnages entre deux cases. Personne ne traverse un meuble ou un mur (testé).

## 4. Équipe et production

- **Compétences** : design, développement, tests, support (0 à 100). Il faut 15 pour travailler dans une discipline.
- **Une seule affectation par personne** : un contrat, le développement d'un produit, sa maintenance ou son support. Rien n'est compté deux fois (testé).
- **Production** ⚙️ : 1 point = 1 heure d'une personne de niveau 50 sur un poste standard. Points par minute = (0,4 + 0,012 × niveau) × ordinateur × énergie × moral × heures sup. × coordination / 60. Sans poste complet : zéro.
- **Qualité** ⚙️ : 0,3 + 0,006 × niveau + bonus de l'ordinateur ± moral, −0,1 si fatigué, −0,05 en heures supplémentaires.
- **Coordination** ⚙️ : au-delà de 3 personnes sur une même cible (6 avec un responsable, +1 par tableau blanc), chaque personne de trop coûte 12 % d'efficacité à toute l'équipe.
- **Dépendances** : design → développement → tests, par module ou par contrat. Les tâches font 10 points au plus. Le goulot (discipline dont le travail prêt attend le plus) est affiché.
- **Énergie et moral** ⚙️ : −1,6 d'énergie par heure (−3,2 en heures sup.), +38 la nuit, +30 le week-end. Le moral tend chaque jour vers une cible : 58 + mobilier commun + bureau + écart de salaire au marché − impayés − absence de poste − horaire étendu − fatigue. Moral < 25 pendant 10 jours : démission annoncée 10 jours ouvrés à l'avance, annulée si le moral remonte au-dessus de 45. Salaire impayé 14 jours : départ immédiat.
- **Recrutement** ⚙️ : 3 à 6 candidats par semaine ; salaire ≈ 3 800 + 70 × niveau CHF ; préavis 0 à 10 jours ouvrés ; la réputation attire de meilleurs profils. Licenciement : un mois d'indemnité plus les jours dus, moral de l'équipe −6. Formation : 900 CHF, 2 jours ouvrés hors du bureau, gain dégressif.
- **Délégation** : responsables d'équipe, correctifs automatiques au-delà d'un seuil de bugs (la règle crée la tâche ; il faut quelqu'un pour la faire), budgets de marketing mensuels plafonnés.

## 5. Prestations

- Demandes ⚙️ : chance quotidienne 0,35 + réputation / 150 (6 en attente au plus), trois tailles (≈ 22-48, 60-130, 160-340 points) qui se débloquent à 20 et 55 de réputation ; 95 à 120 CHF par point selon le type, +15 % ou +30 % pour les gros projets.
- **Négociation du délai** : sûr (+40 % de jours, −10 % de prix), standard, rapide (−25 % de jours, +15 % de prix).
- **Argent** : acompte de 30 % facturé à la signature (payable sous 10 jours), solde facturé à la livraison (30 jours). Une facture est une **créance** ; elle est encaissée à une date qui dépend de la fiabilité du client (0,6 à 1), parfois jamais (créance irrécouvrable après 60 jours).
- **Qualité** : sous l'attendu de plus de 0,08, le client demande une fois des corrections (sans repousser l'échéance) ; sous l'attendu, réduction de prix (1,5 × l'écart, 30 % au plus).
- **Retard** : 2 % par jour ouvré (30 % au plus) ; avertissement 3 jours avant la rupture ; à 12 jours ouvrés de retard, le client rompt, le solde n'est jamais payé, réputation −10. Abandon volontaire : réputation −6, acompte du travail non fait remboursé.
- **Code réutilisable** : capitaliser coûte +15 % de développement et donne −10 % sur les projets suivants du même type (niveau 3 au plus), et sur les modules de produit liés (boutique → catalogue, paiement, recherche, etc.).

## 6. Produits

### Archétypes livrés et à venir

| Archétype | État | Contraintes propres |
|---|---|---|
| Outil SaaS (indépendants, PME, associations) | ✅ jouable | Abonnement ou freemium ; peu d'utilisateurs, abandon lent (×0,45), beaucoup de tickets (×1,8) ; module Abonnement indispensable pour encaisser |
| App de contenu / communauté (étudiants, créateurs) | ✅ jouable | Publicité ou freemium ; bouche-à-oreille fort (×6), abandon rapide (×1,2), forte charge serveur ; Fil de contenu indispensable ; la publicité est versée par une régie avec 30 jours de délai |
| Place de marché (artisans, occasion) | ✅ jouable | Commission ; deux publics : les acheteurs repartent sans assez de vendeurs (liquidité), part du marketing réservée aux vendeurs ; Annonces et Paiement indispensables |
| Studio de jeux, services d'infrastructure | ❌ pas développés | Pas présents dans le jeu (aucun bouton factice) |
| Prestations (agence web/mobile) | ✅ jouable | Voir §5 |

### Pages et modules

- Un produit a des **pages** (nom, titre, texte court, disposition, thème, couleur d'accent, visibilité, liens de menu) et chaque page des **modules** (16 types : présentation, connexion, profil, catalogue, recherche, fil, abonnement, paiement, avis, messagerie, aide, tableau de bord, exports, notifications, annonces, statistiques), avec des réglages (`Core/products/ProductCatalog.cs`).
- **Apparence ≠ valeur** : disposition, thème, couleurs, colonnes du catalogue et illustration personnalisent l'aperçu mais **n'entrent pas dans la simulation** (testé). La valeur vient des modules utiles au public, de leur qualité et de leur cohérence.
- Chaque module a : besoins couverts (découverte, engagement, confiance, conversion, productivité, liquidité), effort (design, développement, tests), complexité (bugs), maintenance par semaine, charge serveur et tickets par 1 000 utilisateurs. Ses réglages modifient ces valeurs (filtres avancés : +60 % d'effort, +35 % de valeur, +50 % de charge...).
- **Couverture** : chaque module comble une part de ce qui reste d'un besoin (rendement décroissant). L'attrait est la couverture pondérée par les besoins de l'archétype, multipliée par l'ergonomie : −5 % par module au-delà de 5 sur une page, −3 % par module sans intérêt pour ce public, ×0,6 si un module indispensable manque. Un module sur une page qu'aucun menu n'atteint, ou masqué, ne compte pas. Un module qui demande la Connexion ne vaut rien sans elle.
- **Versions** : le brouillon est la prochaine version. « Lancer le développement » compare le brouillon à la version en ligne et crée seulement le travail nécessaire (nouveau module : tout ; réglages modifiés : 35 % ; retouches visuelles : 1 point de design ; retrait : 2 points). Le brouillon est figé pendant la fabrication ; on peut annuler (travail perdu). États affichés par module : brouillon, en développement, en test, prêt, publié, modifié.
- **Avant de lancer** : liste du travail, durée estimée avec l'équipe affectée, coût en salaires, et une **prévision à 90 jours** calculée avec le même modèle de marché (qualité probable de l'équipe ±15 %, fourchette d'utilisateurs, de revenus et de bugs). Après publication, la prévision est comparée à la mesure.
- **Bugs livrés** ⚙️ : 0,12 × code × complexité × (1,2 − qualité), réduits par les tests (85 % au plus), avec un aléa de 0,6 à 1,5.

### Marché (un pas par jour, `Core/products/Market.cs`)

1. Attrait = conception × (1 − bugs × 1,2 %, 45 % au plus) × performances.
2. Part face aux concurrents du segment : attrait³ / (attrait³ + Σ concurrents³).
3. Nouveaux curieux = organique (25 × (0,4 + 2 × attrait)) + marketing + bouche-à-oreille, réduits à mesure que le public est déjà au courant.
4. Marketing : budget/jour × portée du canal / (1 + budget/jour ÷ saturation) : chaque franc rapporte moins que le précédent (testé).
5. Inscriptions = curieux × taux d'essai de l'archétype × (0,4 + 2 × attrait) × (0,5 + part) × √(prix) pour un abonnement.
6. Support : tickets = utilisateurs × modules × archétype × (1 + 2 % par bug) ; 1 point de support = 6 tickets ; l'attente fait baisser la satisfaction.
7. Abandons = utilisateurs × 0,6 % × archétype × (1,8 − 2 × satisfaction), plus 1 % par 24 h de panne.
8. Revenus selon le modèle (abonnement, freemium, publicité, commission sur un panier de 55 CHF), moins 3 % de frais de paiement.
9. Hébergement : 5 offres de 15 à 3 900 CHF/mois. Au-delà de 80 % de charge, les pages ralentissent ; au-delà de 100 %, des pannes graduées. Trois optimisations possibles (−15 % de charge chacune, tâche de développement).
10. Dette de maintenance : sans maintenance, elle s'accumule et fait apparaître des bugs.

Le menu Résultats compare la dernière semaine à la précédente, facteur par facteur, et donne les causes principales (notoriété, attrait, concurrence, bugs, performances, support, abandons, liquidité, prix, module manquant).

### Concurrents

1 à 2 par segment, attrait de 18 à 34 %, +0,04 % par jour. Environ tous les 4 mois, l'un d'eux **annonce** une nouvelle version 10 à 20 jours à l'avance (+4 à +10 % d'attrait). Ils ne lisent jamais les chiffres du joueur.

## 7. Finances

- **Séparés** : trésorerie (argent réel), créances (factures clients non payées), dettes (factures à payer), prêts, résultat mensuel, valeur estimée de l'entreprise.
- **Départ** ⚙️ : 7 000 CHF, découvert autorisé 2 000 CHF. Charges fixes au départ : rémunération du fondateur 2 200 CHF (logement compris) + 160 CHF de services = **2 360 CHF/mois**, soit environ 3 mois d'autonomie sans revenu. Un petit contrat (≈ 2 500-4 500 CHF) prend 1 à 2 semaines seul. Ce départ est viable sans argent infini : il faut signer un contrat dans les premières semaines.
- **Paiement** : les dettes échues sont payées automatiquement, salaires d'abord, puis rémunération du fondateur, loyer, prêts, le reste, tant que le découvert le permet.
- **Autonomie** : (trésorerie + découvert − dettes ouvertes + 85 % des factures attendues sous 30 jours) ÷ (charges fixes − revenus récurrents des produits). Les prestations futures ne sont pas comptées. Les hypothèses sont affichées.
- **Valeur estimée** : actif net (trésorerie, créances à 90 %, matériel à la revente, garantie, moins dettes et prêts) + 36 mois du bénéfice récurrent des produits s'il est positif.

### Leviers de redressement

Encaisser plus (prestations), revendre du matériel, baisser marketing et hébergement, licencier (avec indemnité), déménager plus petit, et :

- **Prêt bancaire** ⚙️ : sans impayés, après 60 jours, jusqu'à 3 mois de revenus moyens (2 000 CHF au moins), 12 mois à 7 %. Un seul à la fois.
- **Investisseur** ⚙️ : une seule levée, proposée si un produit a 1 500 utilisateurs ou 100 payants et a grandi de 20 % en 30 jours ; 50 000 à 400 000 CHF contre 10 à 30 % du capital, offre valable 21 jours.
- **Plan de redressement** ⚙️ : en impayés ou mise en demeure, si les revenus récurrents couvrent au moins 50 % des charges fixes ; dettes échues étalées sur 6 mois avec +10 % ; une fois tous les deux ans.

Pas de prêt infini, pas de bouton de sauvetage gratuit.

## 8. Défaite : procédure de liquidation

| État | Critère mesurable | Conséquence |
|---|---|---|
| Saine | Aucune dette échue, autonomie ≥ 30 jours, solde ≥ 0 | — |
| Tendue | Aucune dette échue, mais autonomie < 30 jours ou solde négatif (dans le découvert) | Avertissement |
| Impayés | Au moins une obligation échue non payée (salaire, rémunération, loyer, services, hébergement, marketing, remboursement, indemnité) | Notification critique par impayé ; salariés impayés : moral en baisse, départ après 14 jours |
| Mise en demeure | ⚙️ La plus ancienne obligation impayée a ≥ 20 jours de retard et le total impayé ≥ 500 CHF | Délai fixe de ⚙️ 30 jours, compte à rebours à 10 et 3 jours ; tout payer lève la mise en demeure |
| **Liquidée** | Fin du délai avec un impayé restant | **Partie perdue** : bilan affiché (impayés, trésorerie, créances, prêts, charges, équipe) ; choix : charger une sauvegarde, nouvelle partie, menu principal |

Un solde légèrement négatif ne termine rien (testé). Les créances et le matériel invendu ne comptent pas comme de la trésorerie. La sauvegarde n'est jamais supprimée ; aucun mode ironman.

## 9. Jalons

Premier client, première mise en ligne, première embauche, de vrais locaux, 100 clients payants, 10 000 CHF récurrents, 10 personnes, 10 000 utilisateurs. Ce sont des objectifs : la campagne continue.

## 10. Architecture

| Dossier | Rôle |
|---|---|
| `Core/campaign` | État de la campagne, temps, commandes du joueur, réglages (`Tuning.cs`) |
| `Core/space` | Local, catalogue de meubles, règles de placement, pathfinding, annonces |
| `Core/team`, `Core/work` | Personnes, recrutement, tâches, contrats |
| `Core/products` | Catalogue de modules, pages et versions, modèle de marché, concurrents |
| `Core/finance` | Comptes, factures, dettes, prêts, solvabilité |
| `Core/persistence` | Sauvegardes, réglages, ancien profil |
| `Core/bots` | Stratégies jouées pour l'équilibrage et les captures |
| `UI` | Scène, menus, sons ; aucune règle de jeu |

Le moteur tourne sans interface (tests, bots). Les menus lisent et modifient les mêmes objets que la simulation.
