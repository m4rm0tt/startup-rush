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

### Lancement et délégation

Le lancement est la grosse action du projet. Un mini-jeu facultatif (application : remettre les étapes du déploiement dans
l'ordre ; jeu : repérer les tests en anomalie) donne jusqu'à 50 % de bugs en moins à la sortie. Sans jouer, le lancement se
fait au résultat de base. Les mises à jour ne demandent jamais de mini-jeu.

Branche « Outils du boss » de l'arbre ⚙️ : Raccourcis clavier (1 pt, +15 % par mot), Clavier confortable (1 pt, −40 %
d'énergie par mot), Tests assistés (2 pts, mots de tests +30 %), Macros (2 pts, sessions de 32 mots), Délégation des
lancements (2 pts : un employé présent lance à ta place ; mode « déléguer par défaut »), Procédures de publication (2 pts :
un lancement délégué obtient la moitié du meilleur bonus), Supervision à distance (2 pts : le terminal s'ouvre sans poste
complet ou pendant une formation, au rendement d'un vieux portable ×0,85), Automatisation avancée (3 pts : chaque lundi,
l'équipe met en file la petite amélioration que demandent les avis d'un projet en ligne ; il faut au moins un employé).

✅ Les grosses actions ne s'imposent pas : un projet prêt est **proposé** par une seule annonce regroupée (« 2 projets prêts à
sortir »), qui ouvre le lancement ; « Ne pas me déranger » la laisse au journal ; rien n'est proposé quand un employé lancera
le projet le lendemain ; aucune proposition n'interrompt la saisie au terminal.

✅ Saisie simplifiée (accessibilité, désactivée par défaut) : le mot s'écrit tout seul (⚙️ 3 lettres par seconde), Entrée le
valide ; chaque mot rapporte comme une frappe lente (×0,85), quel que soit le rythme.

## 4 bis. Confort du local

✅ Chaque meuble sert, sans corvée : il agit sur **un** indicateur, plafonné, et souvent pour un nombre de personnes donné.
Rien n'est obligatoire : un manque prive seulement d'un bonus. La synthèse est dans Aménagement (« Confort du local »).

| Indicateur | Objets | Effet ⚙️ (`Core/space/Layout.cs`) |
|---|---|---|
| Ambiance | plantes, lampes, luminaire, tapis, affiches, photo, horloge, stores, présentoir, trophées | moral commun, 6 au plus |
| Détente | canapé (+2), fauteuil (+1) | moral commun, 4 au plus |
| Confort de base | rangements et propreté (chacun pour 1 à 8 personnes) ; WC pour 10, lavabo, cabine | moral commun, 3 au plus : 1,5 × part de l'équipe rangée + 1,5 × sanitaires (WC 70 %, +20 % lavabo, +10 % cabine ; ta chambre compte comme équipée) |
| Pause | café (+9, 8 pers.), distributeurs (+6 / +3, 15 pers.), fontaine (+4, 10), frigo, micro-ondes, bol de snacks, bouteille ; canapé (+5, 3 pers.), fauteuil (+2, 1) ; évier et table basse complètent (+1) | énergie par pause de 3 à 15 : chacun prend la meilleure boisson ou collation et la meilleure place encore libres |
| Concentration | cloisons acoustiques (+2 %), basses et vitrées (+1 %), 4 personnes chacune | production, 4 % au plus |
| Coordination | tableau blanc, planning (+1), petite table de réunion avec 2 chaises (+1), grande avec 4 (+2), table collective (+1), écran de présentation (+0,5), notes, croquis, carnets, papiers (+0,25, 1 au plus) | personnes de plus par équipe sans perte, 3 au plus |
| Formation | bibliothèques (+15 %, 2 utiles), livres, schéma (+5 %), écran de présentation (+10 %), tables de réunion (+5 %) | efficacité des formations, 40 % au plus |
| Accueil | banque d'accueil | 1 candidat de plus chaque semaine |

Les objets muraux se posent sur les murs du fond (un mur plein ; les stores sur une fenêtre ; jamais sur la porte), le
luminaire au plafond ; ni l'un ni l'autre ne prend de case au sol. Le trophée (premier lancement) et la plaque (10 000 CHF par
mois) arrivent dans les cartons et ne se revendent pas.

**Finitions** (lot 7, `Core/space/Finishes.cs`, Aménagement › Finitions) : un local porte les sols et les motifs muraux de son
plan. On peut refaire **tout le sol** (prix ⚙️ par case : béton ciré 20, vinyle 25, moquette 30, dalles de moquette 35,
carrelage 45, lames 50, parquet 60, faux plancher 120, faux plancher perforé 160) ou **habiller les deux murs du fond** (prix ⚙️
par segment : peinture 40, papier peint 60, deux tons 70, bandes 70, béton banché 80, brique 110, lambris 140, panneaux
acoustiques 160). Les travaux restent au local ; un déménagement repart du plan du nouveau local. Effets, plafonnés :

| Finition | Effet ⚙️ |
|---|---|
| Faux plancher (nœud « Salle serveur ») | froid du site +5 % (perforé : +10 %), à proportion de la surface couverte ; il ne crée pas de froid (0 reste 0) |
| Lambris | +0,5 d'ambiance par mur, dans le plafond d'ambiance de 6 |
| Les autres | décoratives : l'identité du local |

La salle serveur et le datacenter sont livrés avec un faux plancher perforé (+10 % de froid), le laboratoire avancé avec un quart
de faux plancher (+1,25 %), le siège avec un mur en lambris (+0,5 d'ambiance).

## 4 ter. Sites, infrastructure et cloud

✅ Une seule entreprise, plusieurs sites. Le premier est le siège. On ouvre un site de plus (nœud « Plusieurs sites »), on
déménage le site affiché, on ferme un site sans rien perdre (cartons, garantie rendue, personnes concernées nommées). Regarder un
site ne change rien : chacun travaille là où est son poste.

| Local ⚙️ | Loyer / mois | Adresses | Électricité, froid, réseau de base | Déblocage |
|---|---|---|---|---|
| Ta chambre | compris | Prilly | 3 kW, 0,6 kW, 50 | — |
| Petit bureau | 2 300 | Flon, Ouchy, Morges | 8, 1,5, 50 | — |
| Bureau d'équipe | 5 400 | Malley, Ecublens | 15, 2, 100 | — |
| Grand open space | 12 500 | Renens, Nyon | 30, 3, 200 | — |
| Siège (60 postes) | 26 000 | Lausanne-Gare | 60, 5, 400 | Plusieurs sites |
| Salle serveur | 1 800 | Crissier, Bussigny | 12 (+30 armoire), 0 (+24 climatiseur), 300 (+1 000 fibre) | Salle serveur |
| Datacenter | 16 000 | Gland, Yverdon | 40 (+60), 0 (+48 et rangées), 2 000 | Datacenter |

Infrastructure ⚙️ (`Core/space/Furniture.cs`, `Core/space/Infrastructure.cs`) :
- Une baie (8U ou 20U) reçoit des modules en U ; il lui faut un **switch**. Serveur 1U : 16 unités d'hébergement ; calcul 2U :
  40 de calcul et 8 d'hébergement ; nœuds de calcul : 64 ; GPU 4U : 10 ; stockage 2U : 48 To ; tiroir de disques : +96 To avec
  un stockage 2U dans la même baie. Serveur tour : 8 unités sans baie.
- Ce qu'un site fournit est limité par l'**électricité** (puissance distribuable), le **froid** (chaleur évacuée ; sinon au
  plus 30 %), le **réseau** (2 de débit par unité d'hébergement) et le **câblage** (−10 % sans brassage ni passe-câbles :
  2 points par baie). Chaque limite est affichée avec sa raison.
- **Incidents** : 3 % par jour pour un site équipé, réduits par les onduleurs (jusqu'à −90 %) et la supervision (÷2) ; un
  incident coupe la moitié de la capacité du site pour la journée.
- **Électricité** : 0,25 CHF/kWh, facturée avec le mois (catégorie « Électricité et réseau »), plus l'abonnement fibre.

✅ Ordre d'usage : tes projets hébergés en interne d'abord (ordre du portefeuille, 25 % de marge), une réserve réglable, puis les
offres cloud. Une même unité n'est jamais promise deux fois. Un projet en interne déborde chez l'hébergeur pour ce qui ne tient
pas, sans dépasser son plafond.

Offres cloud ⚙️ (`Core/campaign/Campaign.Cloud.cs`) : hébergement (14 CHF par unité et par mois), stockage (9 CHF/To),
calcul (9), GPU (120). La demande va vers une cible : taille du marché × renommée × prix (élasticité 1,6) × fiabilité (−6 % par
jour de coupure sur 30 jours) × sécurité (+25 % avec une cage complète) ; une offre saturée perd des clients. Chaque offre
explique ce qui la fait monter ou baisser (« trop cher », « plus de place », « des coupures », « fiable »).

## 4 quater. Recherche, matériel et production

✅ La recherche est une file de projets choisis. Les personnes affectées à la recherche (fiche : « Travaille sur », Recherche)
avancent la première recherche dont l'équipement est sur leur site. Durée (points) et budget par jour ⚙️ sont annoncés ; le
budget n'est payé que les jours où quelqu'un y a travaillé ; suspendre ne perd rien. Pas d'échec aléatoire.

| Recherche ⚙️ | Débloque | Équipement (un de chaque groupe) |
|---|---|---|
| Conception de cartes (60 pts, 60 CHF/j) | famille « Cartes et kits » | établi électronique ; oscilloscope ou station de soudure |
| Validation de PC (90) | « PC assemblés » | banc de test PC |
| Imprimantes 3D (100) | « Imprimantes 3D » | imprimante 3D ; établi de calibration ou fraiseuse |
| Équipements réseau (80) | « Équipements réseau » | établi ; oscilloscope |
| Portables (140), Serveurs et NAS (140) | familles | bancs de test, chariot d'instruments |
| Contrôle qualité (80) | −30 % de défauts | instruments ou oscilloscope ; microscope |
| Gabarits d'assemblage (80) | postes manuels +25 % | découpeuse laser, fraiseuse ou imprimante 3D |
| Cartes en série (120), Cobotique (100), Cartes graphiques (200), Microcomposants (260) | recettes et méthodes | microscope, instruments, cobot, paillasse |

✅ Le matériel se conçoit comme un logiciel (composants, file, équipe), puis se **fabrique** : chaque unité consomme sa
nomenclature (celle de la famille, plus une fois les matières de chaque composant présent). On ne vend que des unités entières
**en stock**, par des canaux : vente directe (visibilité 0,5, 4 par jour), boutique en ligne (1,2, 4 % et 150 CHF/mois, 60 par
jour), distributeurs (2,0, 25 %, 500 par jour). Les ventes perdues (rupture, canaux pleins) sont comptées. Les unités qui
reviennent en panne coûtent leur réparation (garanties) ; le taux de pannes dépend de la conception, du contrôle qualité, du
rodage (banc : −40 % sur 30 unités par jour) et du **mini-jeu de calibration** de la première série (jusqu'à −50 % sur 100 unités).

✅ Une ligne de production relie des stations par des convoyeurs, port à port, dans le sens des flèches du pack, jusqu'à une fin
de ligne. Rien n'est téléporté : une unité avance d'un nœud à la fois ; un convoyeur tient 2 unités, une station 1 ; une sortie
bloquée arrête l'étape d'avant. Une station dont l'étape n'est plus au programme laisse passer ; une station d'une étape à venir
bloque (« mauvaise entrée »). Il faut des opérateurs (affectation Production) : un par poste manuel au travail, un pour 4 automates
(ou une armoire de contrôle). Diagnostics : matière manquante, sortie bloquée, mauvaise entrée, opérateur absent, stock plein,
salle propre incomplète, électricité ou froid insuffisants.

| Recette | Étapes | Matières → produit |
|---|---|---|
| Produit fini | assemblage → logiciel → emballage | nomenclature + 1 emballage → 1 unité |
| Cartes électroniques | placement → refusion → inspection | 1 circuit + 2 composants → 1 carte |
| Cartes graphiques | assemblage → inspection | 1 puce + 2 mémoires + 1 carte → 1 GPU |
| Processeurs | traitement (salle propre, outil chargé à la main) | 1 plaquette → 20 puces (10 sans station de précision) |

Stocks et achats ⚙️ : stock par site (40 + rack 150, palette 40, bac 10, étagère 20). Un achat est payé à la commande et livré
après le délai du fournisseur. Les réserves automatiques recommandent jusqu'à leur niveau (stock + en route), sous un plafond par
mois civil ; la matière la plus en manque d'abord, chacune limitée à sa part de la place.

## 4 quinquies. IA, studio, XR et quantique

Détail et limites : `docs/V1_ETAT.md`, « Lot 6 ». En bref :

- **Modèle d'IA** : objectif + taille + jeu de données → qualité = plafond de la taille × (0,4 + 0,6 × qualité des données) × adéquation à
  l'objectif (un petit modèle spécialisé bat un gros modèle mal nourri). Entraînement : 600 / 2 400 / 9 000 points ; un GPU en fournit 20
  par nuit, la location 150 par jour à 0,50 CHF. Évaluation : jusqu'à 0,08 de qualité retrouvée. La version en service reste jusqu'à la
  publication de la suivante.
- **Inférence** : un produit d'IA charge le GPU selon la taille du modèle (0,5 / 1,5 / 5 pour 1 000 utilisateurs) ; l'audience servie est
  plafonnée par la capacité (interne d'abord, puis location sous plafond). Ordre de la nuit : hébergement, inférence, entraînement, offres cloud.
- **Studio** : +30 % de qualité du contenu et +15 % de portée marketing au plus ; **zone XR** : jusqu'à −60 % de défauts d'un produit XR.
- **Quantique** : un cryostat fermé avec sa baie de contrôle et son support cryogénique, 14 jours de mise en route si le froid et l'énergie
  suivent, 20 unités vendables ; +25 % seulement sur les recherches de calcul intensif.

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
| Lot 4 | Plusieurs sites (Organisation, 1), Salle serveur (Infrastructure, 1), Haute densité (2), Datacenter (3), Offres cloud (Commercial, 2), Calcul à la demande (2) |
| Lot 5 | Matériel (Création, 2), Industrialisation (Innovation, 3), Salle propre (3), Boutique en ligne (Commercial, 1), Distributeurs (2) ; recherches : +1 point chacune |

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
