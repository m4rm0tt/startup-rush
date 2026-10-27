# Startup Rush : règles du jeu

Les règles **réellement implémentées** dans `Core/`. Deux types d'éléments :
- ✅ : une décision du cahier des charges, à appliquer telle quelle.
- ⚙️ : une valeur de réglage proposée, à tester. Ces valeurs sont dans `Core/campaign/Tuning.cs` et les catalogues (`Core/products/ProductCatalog.cs`, `Core/progress/DevTree.cs`).

## 1. Ce que fait le joueur

✅ Tu crées tes propres projets (applications, jeux…) et tu les fais grandir. Il n'y a **aucune prestation client** : pas de contrats, pas de devis.
✅ Le jeu tourne tout seul ; tu décides de la direction.
✅ Campagne persistante, sauvegardes, appartement puis locaux aménageables, pixel art.

Boucle : **créer une idée → choisir ses composants → l'équipe les développe seule → lancer → utilisateurs et revenus → lire les avis → améliorer ou créer autre chose → agrandir l'entreprise.**

## 2. Départ

| Élément | Valeur | Pourquoi |
|---|---|---|
| Argent ⚙️ | 18 000 CHF, découvert de 2 000 | Sans prestations, il faut de quoi sortir un premier projet et voir ses résultats. |
| Dépenses fixes ⚙️ | 2 160 CHF/mois (ta rémunération de 2 000, logement compris, plus 160 de services) | Environ 8 mois d'autonomie sans aucun revenu. |
| Lieu | Ta chambre (plan `room_bedroom_start` du pack) : lit, rangements, bureau compact, vieille tour, écran simple | 3 bureaux au plus. Le lit donne +10 d'énergie la nuit au fondateur. |
| Premier projet | 3 composants de départ, environ 380 à 450 mots au terminal pour le fondateur seul | Mesuré : 445 mots (4 jours de jeu) dans le parcours de capture, sortie au jour 22 (médiane) dans le bilan. |
| Points de développement | 1 | Un premier choix dans l'arbre tout de suite. |

## 3. Projets

### Types

| Type | Disponible | Revenus | Particularités |
|---|---|---|---|
| Application | dès le départ | abonnement ou gratuit + option | Abandon lent, beaucoup de questions au support. |
| Jeu | dès le départ | vente à l'unité | Liste de souhaits remplie avant la sortie, convertie le jour du lancement ; ensuite chaque nouveau joueur achète une fois. Une mise à jour fait revenir du monde. |
| App communautaire | arbre (2 pts) | publicité ou gratuit + option | Bouche-à-oreille fort, abandon rapide, beaucoup de charge serveur. |
| Place de marché | arbre (3 pts) | commission | Il faut des vendeurs et des acheteurs : sans assez de vendeurs, les acheteurs repartent. |

Chaque type propose 2 ou 3 publics (taille, prix habituel, sensibilité au prix). Créer un projet, c'est choisir **un type et un public** ; le nom, le prix et les composants de départ ont des valeurs par défaut viables.

### Composants (l'atelier)

- ⚙️ 26 composants, chacun proposé aux types concernés : comptes, tableau de bord, abonnements, recherche, messagerie, exports, statistiques, gameplay, contenu, graphismes, musique, classements, succès, multijoueur, fil, profils, modération, annonces, catalogue, paiement, avis, interface, performances, aide intégrée, notifications, sauvegarde en ligne.
- Chaque composant a un rôle en une phrase, les besoins qu'il couvre, un coût en travail et des dépendances (« Messagerie demande Comptes »).
- **3 niveaux**. ⚙️ Valeur ×1 / ×1,5 / ×1,85 (rendement décroissant) ; travail ×1 / ×1,4 / ×2 (×2,2 sur l'effort de base). Le niveau 3 se débloque dans l'arbre.
- Ajouter ou améliorer **met le travail en file**. Les tâches design → développement → tests s'enchaînent seules.
- La couleur et l'icône d'un projet sont cosmétiques : elles **ne changent pas la simulation** (testé).
- Ajouter tout ce qui existe n'est pas la bonne stratégie : chaque composant ajoute de la charge serveur, du support et de l'entretien, et ne répond qu'à certains besoins. Il n'y a pas de pénalité brutale pour une combinaison imparfaite.

### Sortie et vie du projet

- On peut lancer dès que **2 composants** sont terminés. Le jeu signale « Prêt à sortir » quand la file est vide.
- Après la sortie, chaque amélioration terminée est une **mise à jour** : la version augmente, des curieux reviennent, et une notification regroupée est envoyée.
- Fermer un projet arrête ses coûts et ses revenus.

## 4. Le fondateur et son terminal

✅ Le fondateur représente le joueur : **il ne développe plus tout seul**. Présent à son poste, il répond seulement au support
des projets en ligne. Son travail productif passe par le terminal (bouton **Travailler**, touche Entrée).

| Règle | Valeur ⚙️ (`Tuning.Terminal`) |
|---|---|
| Un mot validé | 0,25 point × compétence × poste × vitesse × énergie (borné à ce qui reste de la tâche) |
| Vitesse de frappe | ×0,85 en dessous de 1,5 caractère/s, ×1,2 au-dessus de 6,5 ; entre les deux, linéaire |
| Session | 24 mots (20 à 45 secondes), on en enchaîne une autre aussitôt ; arrêt possible à tout instant, rien de validé ne se perd |
| Faute | Rien ne se perd ni ne se gagne ; le mot reste affiché |
| Énergie | −0,35 par mot ; sous 40, le rendement baisse |
| Horaires | De 7 h à 23 h, ni en congé ni en formation, avec un poste complet |
| File vide | Sur un projet en ligne, les mots corrigent les bugs (borné à ce qui reste à corriger) |

Les mots sont courts, sans accent, tirés d'un hasard séparé de la simulation (le jeu reste reproductible avec sa graine).
Le terminal est entièrement simulé : rien n'est exécuté sur l'ordinateur du joueur.

Pendant la saisie, l'entreprise tourne à vitesse normale ; à la fermeture, la vitesse et la pause d'avant reviennent. Un
réglage d'accessibilité met l'entreprise en pause pendant la saisie, sans changer le rendement d'un mot. Aucun raccourci
n'agit pendant la frappe ; Espace ou Entrée valident, sans répétition si on maintient la touche.

## 5. Automatismes

| Ce qui est automatique | Règle | Ce que le joueur règle |
|---|---|---|
| Qui travaille sur quoi | En « Auto » (par défaut), chaque **employé** travaille sur le **premier projet du portefeuille qui a du travail en file**, sinon sur l'entretien du premier projet en ligne. Le fondateur, lui, passe par le terminal. | L'ordre du portefeuille (◀ ▶), ou fixer une personne sur un projet. |
| Enchaînement des tâches | Design, puis développement, puis tests, par composant, dans l'ordre de la file, de préférence dans la spécialité de chacun. Tout le monde peut tout faire (plus lentement hors de sa spécialité) : un projet n'est jamais bloqué faute d'une compétence. | Rien. |
| Support et maintenance | Sur un projet en ligne, une part fixe du temps de son équipe y va : ⚙️ Peu 10 %, Normal 25 %, Prioritaire 45 %. Moitié support, moitié corrections quand les deux sont nécessaires. Quand il n'y a rien à développer, tout le temps y passe. | Le niveau d'entretien. |
| Serveurs | Par défaut, la plus petite offre débloquée qui tient la charge (75 % au plus), **sans dépasser un plafond mensuel visible** (⚙️ 400 CHF par défaut). Une alerte prévient si le plafond empêche de suivre. | Le plafond, ou le mode manuel. |
| Paiements, salaires, formalités | Payés automatiquement à l'échéance (salaires d'abord) s'il y a l'argent. Salaires de référence ⚙️ ≈ 3 800 + 70 × niveau CHF. | Rien. |

L'automatisation **ne recrute pas, ne loue pas, n'emprunte pas et n'augmente aucun budget** : ces décisions restent au joueur.

## 6. Avis des utilisateurs

Les avis viennent des **facteurs réels de la simulation**, calculés sur la dernière semaine :

| Cause | Quand | Lien |
|---|---|---|
| Lenteurs, pannes | Serveurs chargés au-delà de 80 %, ou pannes | Réglages → Serveurs |
| « Ça plante » | Bugs qui pèsent sur l'attrait | Réglages → Entretien |
| « Personne ne répond » | Questions qui s'accumulent | Réglages → Entretien |
| « Trop cher » | Prix nettement au-dessus de l'habitude du public | Réglages → Prix |
| « Il manque… » | Le besoin le plus mal couvert (en dessous de 35 %) parmi ceux qui comptent | Le composant qui y répond le mieux (« Demandé dans les avis ») |
| « Pas assez d'offres » | Place de marché sans assez de vendeurs | Annonces |
| Positifs | Mise à jour récente, meilleur composant | — |

Au plus 3 tendances négatives et 2 positives, avec la part des avis concernés. Quelques avis datés s'ajoutent chaque semaine (24 gardés au plus). Ce ne sont pas des commandes : on ne doit rien à personne. La note sur 5 vient de la satisfaction de la dernière semaine.

## 7. Arbre de développement

Une seule monnaie : les **points de développement**. Ils ne servent qu'à la campagne en cours et ne se transmettent pas d'une campagne à l'autre.

**Comment on les gagne** (une seule fois chacun) ⚙️ :
- un projet atteint 50 utilisateurs actifs : +2 ;
- un projet passe 500 utilisateurs : +1, puis 5 000 : +2, puis 50 000 : +3 ;
- un jeu se vend à 1 000 exemplaires : +1 ;
- jalons : première embauche +1, vrais locaux +1, 1 000 utilisateurs +1, 10 000 CHF par mois +2, 3 projets ayant trouvé leur public +1, 10 personnes +2, 50 000 utilisateurs au total +2 ;
- premier mois bénéficiaire : +2.

**Anti-abus** : lancer des projets vides ne rapporte rien (il faut 50 vrais utilisateurs, testé). Les mises à jour ne rapportent pas de points.

| Branche | Nœuds (coût) |
|---|---|
| Création | Composants avancés (1), Apps communautaires (2), Multijoueur (2, après Composants avancés), Places de marché (3, après Apps communautaires) |
| Organisation | Recrutement élargi (1), Responsables (1), Formation interne (2), Grandes équipes (2) |
| Infrastructure | Cloud M (1), Optimisation −15 % de charge (1), Cloud L et cluster (2) |
| Commercial | Moteurs de recherche (1), Lancements événementiels (1), Salons et presse (2), Fidélisation −15 % d'abandon (2) |
| Innovation | Laboratoire qualité +0,05 (2), Assistants de code +10 % de production (3) |

Chaque nœud a un effet réel, testé ou visible dans la simulation. Aucune branche n'en exclut une autre et rien n'est irréversible. Les outils de base (application, jeu, composants niveaux 1 et 2, réseaux sociaux, hébergement mutualisé et Cloud S) sont disponibles dès le départ.

## 8. Équipe

- **Recrutement** : quelques profils par semaine (spécialité, niveau, coût mensuel). La confirmation affiche le coût et l'effet sur le budget mensuel.
- **Affectation** : Auto ou un projet. Une personne ne travaille que sur un projet à la fois (testé).
- **Horaire** : 9 h–17 h ou 9 h–20 h. Les soirées produisent un peu plus mais fatiguent et pèsent sur le moral.
- **Congés** : 5 jours ouvrés, salaire maintenu, énergie à 100 et moral en hausse.
- **Formation** ⚙️ : 900 CHF (450 avec Formation interne) et 2 jours d'absence ; gain dégressif.
- **Augmentation** ⚙️ : +8 % de salaire. Le moral gagne 15 points, puis 7,5 pour la deuxième de l'année. Deux par an au plus. Le surcoût mensuel est affiché avant validation.
- **Moral** : conditions de travail, salaire par rapport à la référence, impayés. Un moral très bas pendant 10 jours mène à une démission annoncée 10 jours à l'avance (annulée si le moral remonte). Un salaire impayé pendant 14 jours entraîne un départ.
- **Coordination** ⚙️ : au-delà de 3 personnes sur un projet (6 avec un responsable, +2 avec Grandes équipes, +1 par tableau blanc), chaque personne de trop coûte 12 % d'efficacité.

## 9. Argent

- **Écran Finances** : argent disponible, solde du mois (revenus moins dépenses), principales dépenses et sources de revenus en barres, nombre de jours d'autonomie. Factures, tableaux mois par mois et financements sont dans les détails, repliés.
- Avant chaque décision qui coûte (recrutement, achat, déménagement, budget) : **coût immédiat et effet mensuel** affichés.
- **Leviers en cas de difficulté** : baisser le marketing, fermer un projet déficitaire, revendre ou ranger du matériel, réduire l'équipe, déménager plus petit. Aussi un prêt bancaire, limité et sous conditions, un investisseur (une seule levée) et un plan de redressement (un tous les deux ans).
- **Faillite** ✅ : un solde légèrement négatif ne fait rien. Une facture impayée ouvre la procédure : **impayés** → **mise en demeure** après ⚙️ 20 jours (et 500 CHF au moins) → **faillite** ⚙️ 30 jours plus tard si rien n'est payé. Un compte à rebours est affiché, les alertes sont regroupées (pas de déluge de messages) et la sauvegarde n'est jamais effacée.

## 10. Notifications

- Trois catégories : **à traiter**, **bonnes nouvelles**, **infos**.
- Un problème qui se répète est regroupé (« ×3 »). Une fois réglé, il passe en « réglé » et quitte la liste à traiter (testé).
- Seuls les bonnes nouvelles et les nouveaux problèmes apparaissent en bulles. Les infos restent dans le journal.

## 11. Découverte

- Pas de tutoriel. Une phrase d'aide apparaît à la première ouverture des Projets, de l'atelier, de l'équipe et de l'arbre, avec un bouton « Compris ».
- Un portefeuille vide affiche une grande carte « Créer ton premier projet ».
- Un nœud débloqué fait apparaître sa nouveauté à l'endroit naturel : type dans « Nouveau projet », composant dans l'atelier, canal dans Réglages, offre dans Serveurs.

## 12. Marché (inchangé dans son principe)

Le marché suit un modèle en un pas par jour :
- l'attrait (couverture des besoins × bugs × performances) détermine la part face aux concurrents ;
- les nouveaux curieux viennent de la présence naturelle, du marketing (rendement décroissant) et du bouche-à-oreille ;
- les inscriptions dépendent de l'attrait, de la concurrence et du prix ;
- le support absorbe les questions ;
- les départs dépendent de la satisfaction ;
- les revenus dépendent du modèle choisi.

Les concurrents progressent lentement et annoncent leurs nouvelles versions 10 à 20 jours à l'avance. La décomposition complète est dans les détails de l'Aperçu d'un projet.
