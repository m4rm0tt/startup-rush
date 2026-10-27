# Startup Rush — Plan d'implémentation P0

Oct 6, 2026 · @Neuille

La P0 se construit en 9 étapes : d'abord la logique du jeu dans une bibliothèque testée, puis les états du jeu, puis l'interface. Chaque étape se termine par un livrable vérifiable, un commit et une ligne dans le journal de travail.

## Décisions à prendre avant de coder

| Décision | Recommandation | Quand |
| --- | --- | --- |
| Framework graphique | WPF (XAML, liaison de données, rafraîchissement simple). Alternatives : WinForms si tu veux le plus simple, MonoGame si tu veux dessiner toi-même. | Avant l'étape 7 |
| Version de .NET | La version LTS en vigueur au démarrage du projet | Étape 0 |
| Tests unitaires | xUnit (ou NUnit si c'est ce que l'ETML utilise) | Étape 0 |
| Unité de temps | Secondes de jeu, `double`, passées en `deltaTime` à chaque mise à jour | Étape 1 |
| Argent | `decimal` | Étape 2 |
| Aléatoire | Un `Random` injecté (avec graine) pour rendre les tests reproductibles | Étape 3 |
| Valeurs d'équilibrage | Centralisées dans une classe `GameConfig` | Étape 2 |

## Architecture cible

Trois projets dans une seule solution. La règle qui compte : `Core` ne connaît ni l'interface ni le framework graphique ; l'interface lit l'état du jeu et lui envoie des commandes (accepter une demande, par exemple).

```text
StartupRush.sln
├─ StartupRush.Core          (logique du jeu, aucune dépendance UI)
│  ├─ Simulation/   GameClock, GameSession, GameConfig
│  ├─ Companies/    Company, Wallet
│  ├─ Requests/     ClientRequest, RequestType, RequestGenerator
│  ├─ Tasks/        WorkTask, WorkTaskStatus
│  └─ States/       IGameState, GameStateMachine, MenuState, PlayingState, GameOverState
├─ StartupRush.Tests         (xUnit, référence Core)
└─ StartupRush.UI            (WPF ou WinForms, référence Core)
```

## Vue d'ensemble

| Étape | Fonctionnalité P0 couverte | Livrable vérifiable |
| --- | --- | --- |
| 0. Mise en place | — | Solution, dépôt Git, un test qui passe |
| 1. Boucle de jeu | Boucle de jeu et temps réel | Le temps de jeu avance, testé |
| 2. Entreprise et économie | Création et état de l'entreprise, économie de base | L'argent évolue selon les charges, testé |
| 3. Clients et demandes | Clients et demandes | Des demandes apparaissent, on les accepte ou refuse |
| 4. Tâches | Tâches et exécution | Une demande acceptée progresse et rapporte de l'argent |
| 5. Boucle complète | Économie de base, boucle de jeu | Une partie complète se joue sans interface, avec une faillite possible |
| 6. États du jeu | États du jeu | Menu, partie et fin de partie enchaînés |
| 7. Interface | Interface de jeu | Partie jouable de bout en bout à l'écran |
| 8. Équilibrage | Toutes | P0 validée et versionnée |

## Étape 0 — Mise en place

**Objectif :** un projet propre et un filet de sécurité (Git + tests) avant la première ligne de logique.

- [x] Créer la solution `StartupRush` dans Rider avec les trois projets (`Core` en bibliothèque de classes, `Tests`, `UI`)
- [x] Ajouter les références : `Tests` → `Core`, `UI` → `Core`
- [x] Initialiser Git, ajouter un `.gitignore` .NET, faire le premier commit (messages selon les conventions de commit de l'ETML)
- [x] Écrire un test bidon, lancer `dotnet test` et vérifier qu'il passe
- [x] Créer le journal de travail (un fichier `JOURNAL.md` à la racine)

**Terminé quand :** `dotnet test` est vert et le premier commit est poussé.

## Étape 1 — Boucle de jeu et temps réel

**Objectif :** un temps de jeu qui avance de façon continue, indépendamment de l'affichage.

**À créer dans `Core` :** `GameClock` (temps total écoulé) et `GameSession` (point d'entrée qui expose `Update(double deltaTime)`).

- [x] `GameClock.Tick(deltaTime)` : ajoute le delta au temps total
- [x] Refuser un delta négatif et plafonner un delta trop grand (évite les sauts quand la fenêtre est figée)
- [x] `GameSession.Update(deltaTime)` appelle l'horloge, puis (plus tard) chaque système dans un ordre fixe
- [x] Tests : `Tick(1.5)` donne 1,5 s ; deux ticks s'additionnent ; delta négatif refusé ; delta énorme plafonné

**Principe à retenir :** toute logique dépend du `deltaTime`, jamais du nombre d'images par seconde. L'interface mesurera le vrai temps écoulé (avec `Stopwatch`) et le passera à `Update`.

**Terminé quand :** les tests passent et une boucle de test fait avancer une `GameSession` de 10 secondes simulées.

## Étape 2 — Entreprise et économie de base

**Objectif :** une entreprise avec un solde qui évolue en temps réel.

**À créer :** `GameConfig` (valeurs de départ et d'équilibrage), `Wallet` (argent), `Company` (nom, portefeuille, listes de demandes et de tâches, vides pour l'instant).

- [x] `GameConfig` : argent de départ, charges de fonctionnement par seconde
- [x] `Wallet` : `Balance`, `Earn(amount)`, `CanAfford(amount)`, `TrySpend(amount)` ; refuser les montants négatifs
- [x] Événement `MoneyChanged` pour que l'interface puisse réagir sans interroger en boucle
- [x] `Company.Update(deltaTime)` : retire les charges de fonctionnement proportionnellement au temps
- [x] Tests : dépense supérieure au solde refusée ; gain ajouté ; 10 s de charges retirent le bon montant

**Terminé quand :** une `GameSession` démarre avec l'argent de départ et le solde baisse selon les charges.

**Point d'étape conseillé :** affiche le solde dans une fenêtre minimale (un seul label) pour vérifier que la boucle tourne avant d'aller plus loin.

## Étape 3 — Clients et demandes

**Objectif :** des demandes de travail qui arrivent en continu et que le joueur accepte ou refuse.

**À créer :** `RequestType` (enum), `ClientRequest`, `RequestGenerator`.

- [ ] `ClientRequest` : identifiant, type, valeur (gain), difficulté, échéance optionnelle, durée de validité de l'offre
- [ ] `RequestGenerator` : reçoit un `Random` et `GameConfig`, produit une demande à intervalle régulier ou aléatoire
- [ ] `Company.PendingRequests` : liste des demandes en attente de décision
- [ ] Commandes `AcceptRequest(id)` et `RejectRequest(id)` ; une demande inconnue est refusée proprement
- [ ] Une offre non traitée expire au bout de sa durée de validité (c'est déjà de la pression temporelle)
- [ ] Tests : même graine donne les mêmes demandes ; refuser retire la demande ; accepter la retire aussi ; une offre expirée disparaît

**Terminé quand :** en laissant tourner la session, des demandes apparaissent, expirent, et les commandes d'acceptation et de refus fonctionnent (vérifié par tests).

## Étape 4 — Tâches et exécution

**Objectif :** une demande acceptée devient une tâche qui avance avec le temps, paie à la fin et sanctionne la mauvaise gestion.

**À créer :** `WorkTask`, `WorkTaskStatus` (en cours, terminée, échouée).

- [ ] `WorkTask` créée à partir d'une `ClientRequest` acceptée ; la durée de travail découle de la difficulté
- [ ] `Update(deltaTime)` : fait avancer la progression de 0 à 1 ; passe à « terminée » à 1
- [ ] Récompense : à la fin, la valeur de la demande est versée via `Wallet.Earn`
- [ ] Conséquence d'un retard : si l'échéance est dépassée, gain réduit (ex. moitié) ou tâche échouée sans gain, au choix, défini dans `GameConfig`
- [ ] Capacité de départ : le joueur ne peut mener qu'un nombre limité de tâches à la fois (ex. 2). Sans cette limite, accepter tout est toujours optimal. Les employés (P1) remplaceront cette règle
- [ ] Tests : progression proportionnelle au temps ; paiement à la fin ; gain réduit en cas de retard ; acceptation refusée quand la capacité est atteinte

**Terminé quand :** accepter une demande lance une tâche qui se termine, rapporte de l'argent, et dont le retard est sanctionné.

## Étape 5 — Boucle complète et fin de partie

**Objectif :** assembler les systèmes en une partie complète qui se joue sans interface, avec une condition de défaite.

- [ ] Fixer l'ordre de mise à jour dans `GameSession.Update` et le documenter en commentaire : temps, génération des demandes, expiration des offres, avancement des tâches, charges, vérification de fin
- [ ] Condition de défaite : faillite (solde négatif)
- [ ] Score de fin de partie : par exemple solde final et temps survécu
- [ ] `GameSession.IsOver` et `GameSession.Result`
- [ ] Test d'intégration : une partie sans aucune action finit par la faillite ; une partie avec acceptation automatique des bonnes demandes survit plus longtemps
- [ ] Petit simulateur de test (un « bot » qui accepte tout) pour repérer une économie trop facile ou impossible

**Terminé quand :** une partie complète se déroule en tests, du départ à la faillite, avec un score.

## Étape 6 — États du jeu

**Objectif :** séparer menu, partie et fin de partie pour ne pas mélanger la logique de chaque écran.

**À créer dans `Core/States` :** `IGameState` (`Enter`, `Update`, `Exit`), `GameStateMachine`, `MenuState`, `PlayingState`, `GameOverState`.

- [ ] `GameStateMachine.ChangeState(state)` appelle `Exit` sur l'ancien état puis `Enter` sur le nouveau
- [ ] `PlayingState` possède la `GameSession` et la fait avancer dans `Update`
- [ ] Transitions : menu vers partie (nouvelle partie), partie vers fin (faillite), fin vers menu ou nouvelle partie
- [ ] `GameOverState` conserve le résultat de la partie à afficher
- [ ] Tests : chaque transition appelle `Enter` et `Exit` dans le bon ordre ; la faillite fait passer de la partie à la fin ; une nouvelle partie repart de zéro

**Terminé quand :** la machine à états enchaîne menu, partie et fin de partie sans interface, en tests.

## Étape 7 — Interface de jeu

**Objectif :** une partie jouable à l'écran, lisible d'un coup d'œil.

**Règle :** aucune règle de jeu dans l'interface. Elle affiche l'état et appelle des commandes de `Core`.

- [ ] Boucle côté interface : un minuteur (environ 60 fois par seconde) mesure le temps réel écoulé avec `Stopwatch` et appelle `Update(deltaTime)`
- [ ] Écran de menu : nouvelle partie, quitter
- [ ] Écran de partie, bandeau du haut : argent (avec évolution visible), temps écoulé
- [ ] Écran de partie, demandes en attente : valeur, difficulté, échéance, temps restant avant expiration, boutons Accepter et Refuser
- [ ] Écran de partie, tâches actives : barre de progression, temps restant avant échéance, rouge quand l'échéance approche
- [ ] Journal des événements importants (tâche terminée +X, offre expirée, retard sanctionné)
- [ ] Écran de fin : score, bouton rejouer, retour au menu
- [ ] Le changement d'écran suit la machine à états de l'étape 6

**Terminé quand :** tu peux jouer du menu à la faillite uniquement à la souris, sans voir une seule ligne de console.

## Étape 8 — Équilibrage et validation

**Objectif :** une P0 qui se joue bien et qui est propre à passer en P1.

- [ ] Jouer au moins trois parties de 10 minutes et noter ce qui est trop facile, trop dur ou illisible dans le journal
- [ ] Ajuster uniquement `GameConfig` (argent de départ, charges, fréquence et valeur des demandes, pénalités, capacité)
- [ ] Relancer tous les tests après chaque réglage
- [ ] Nettoyer : noms clairs, commentaires utiles, code mort supprimé
- [ ] Mettre à jour le journal : décisions de conception, problèmes rencontrés, usage de l'IA
- [ ] Poser un tag Git de version (par exemple `v0.1.0`) selon les conventions de versioning de l'ETML

**Terminé quand :** la checklist ci-dessous est entièrement cochée.

## Critères de fin P0

- [ ] **Boucle de jeu et temps réel** : le temps ne s'arrête jamais en partie normale, la logique dépend du `deltaTime`
- [ ] **Création et état de l'entreprise** : une entreprise démarre avec peu d'argent et regroupe argent, demandes et tâches
- [ ] **Clients et demandes** : des demandes avec type, valeur, difficulté et échéance apparaissent ; le joueur accepte ou refuse
- [ ] **Tâches et exécution** : une demande acceptée avance dans le temps, rapporte à la fin, et un retard est sanctionné
- [ ] **Économie de base** : le solde évolue en temps réel (gains et charges) et mène à une faillite possible
- [ ] **Interface de jeu** : argent, demandes, tâches actives et messages importants sont lisibles d'un coup d'œil
- [ ] **États du jeu** : menu, partie et fin de partie sont séparés et s'enchaînent
- [ ] Les règles d'économie, de tâches et de transitions sont couvertes par des tests qui passent
- [ ] Le dépôt Git contient l'historique, le journal est à jour et la version est taguée

## Pièges à éviter

- **Logique dans l'interface :** si une règle se trouve dans le code-behind, elle ne se teste plus. Elle va dans `Core`.
- **Temps lié aux images par seconde :** le jeu irait deux fois plus vite sur un écran deux fois plus rapide. Toujours multiplier par `deltaTime`.
- **Aléatoire non injectable :** impossible d'écrire un test reproductible. Passer un `Random` au constructeur.
- **Valeurs codées en dur :** un nombre d'équilibrage dispersé dans dix classes se règle mal. Tout dans `GameConfig`.
- **Interface trop tôt :** sans logique testée, chaque bug d'affichage cache un bug de règle. L'interface vient à l'étape 7, avec un simple label dès l'étape 2 pour vérifier la boucle.
- **Débordement vers P1 :** pas d'employés, de matériel ni d'événements avant que la checklist P0 soit cochée. Prévoir seulement des classes assez simples pour qu'on les ajoute ensuite sans réécrire (c'est exactement ce que demande le critère de réussite du cahier des charges).
- **Gros commits :** un commit par sous-tâche cochée, avec le message conforme aux conventions ETML.
