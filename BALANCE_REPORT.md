# Rapport d'équilibrage

## Protocole

- **Outil** : `Tests/simulation/BalanceCampaignTest.cs`, un test `[Explicit]` à lancer à la main.

  ```
  dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~PlaystylesAcrossPressures"
  ```

  Le tableau sort dans la console et dans le fichier `BALANCE_OUT`, ou à défaut dans le dossier temporaire.
- **Runs** : personnage Alex, graines 0 à 19 (20 runs par ligne), 2 h de jeu au plus par run.
- **Héritage** : aucun pour le joueur débutant, tout acheté pour le joueur avancé (+1 000 CHF, réputation, carte en plus, 2 relances, une assurance, atouts légendaires).
- **Le bot** (`GameBot.cs`) n'a aucun avantage caché :
  - Il clique à un rythme humain fixe : 4 clics par seconde, 7 pour le style « clic ».
  - Il accepte les contrats qu'il peut finir à temps.
  - Il choisit l'atout le plus rare et prend toujours l'option sûre des événements.
- **Styles comparés**. Chacun garde une réserve de mois de factures avant d'acheter, puis donne la priorité à une famille d'achats :

| Style | Réserve | Priorité d'achat |
|---|---|---|
| prudent | 5 mois | le moins cher, loyer à part |
| matériel | 2 mois | matériel |
| équipe | 2 mois | embauches et locaux |
| empire | 2 mois | boîtes |
| clic | 2 mois | matériel, 7 clics/s |
| mixte | 2 mois | le moins cher, toutes familles |
| dépensier | 0,5 mois | le moins cher (joueur impatient) |

- **Avant / après** : la ligne « avant » rejoue le même bot sur le commit `6bc4f7c` (équilibrage d'origine), dans une copie isolée du dépôt (`git worktree`).

## Constat de départ

Le PDF disait vrai, et c'était même plus net : **tous les styles raisonnables gagnaient 20 fois sur 20, en pression 0 comme en pression 3**. Les durées allaient de 34 à 40 minutes. La pression 3 n'ajoutait que 3 à 5 minutes, et les styles se valaient presque tous.

Les causes mesurées (trace minute par minute de `TraceOneGame`) :

1. **Des dépenses trop faibles** : en milieu de partie, les factures ne pesaient que 10 à 20 % des revenus. Après les premières minutes, on ne risquait plus rien.
2. **L'inflation des revenus passifs** : une boîte se remboursait en 3 à 4,5 mois de jeu sans aucun coût de fonctionnement, et ses revenus étaient multipliés par 9 entre la 25e et la 35e minute.
3. **Une pression maximale presque sans effet** : factures +25 % et impôt ×4 ne changeaient presque rien face à des revenus exponentiels.

## Changements de règles

Tous les réglages sont dans `GameConfig`, `RunSetup.cs` (pressions) et `Crisis.cs`.

| Règle | Avant | Après | Pourquoi |
|---|---|---|---|
| Charges des boîtes (`BusinessUpkeepRate`) | aucune | 35 % du revenu nominal, chaque mois, sur la facture | Grandir vite crée des coûts fixes qui tombent même en récession |
| Inflation du coût de la vie (loyer, salaires, abonnements, charges) | aucune | +1 % par mois en pression 1, +2 % en pression 2, +3 % en pression 3, rien en pression 0 | Les pressions hautes serrent la trésorerie au fil de la run |
| Fréquence des crises en pression 3 | tous les 3 mois | tous les 2 mois | La pression maximale demande de garder de la réserve |
| Crise « Récession » | revenus des boîtes −40 % | −50 % | Avec les charges, c'est le vrai test des coûts fixes |
| Crise « Contrôle fiscal » | factures ×1,6 | factures ×2 | Même raison |
| Offre de contrat dont l'échéance est passée (correctif, commit séparé) | restait acceptable | expire | On ne peut plus prendre un contrat forcément raté |

La pression 0 n'a pas d'inflation et ne change que par les charges des boîtes : elle reste accueillante.

## Résultats (20 runs par ligne)

| Style | Pression | Héritage | Victoires avant | Victoires après | Durée médiane avant (min) | Durée médiane après (min) | Défaites après |
|---|---|---|---|---|---|---|---|
| prudent | 0 | aucun | 20/20 | 20/20 | 37 | 37 | – |
| prudent | 3 | aucun | 20/20 | 20/20 | 40 | 49 | – |
| matériel | 0 | aucun | 20/20 | 20/20 | 34 | 35 | – |
| matériel | 3 | aucun | 20/20 | 20/20 | 39 | 46 | – |
| équipe | 0 | aucun | 20/20 | 20/20 | 34 | 35 | – |
| équipe | 3 | aucun | 20/20 | 20/20 | 38 | 44 | – |
| empire | 0 | aucun | 20/20 | 20/20 | 34 | 35 | – |
| empire | 3 | aucun | 20/20 | 20/20 | 38 | 45 | – |
| clic | 0 | aucun | 20/20 | 20/20 | 34 | 35 | – |
| clic | 3 | aucun | 20/20 | 20/20 | 37 | 45 | – |
| mixte | 0 | aucun | 20/20 | 20/20 | 34 | 35 | – |
| mixte | 3 | aucun | 20/20 | 20/20 | 39 | 45 | – |
| dépensier | 0 | aucun | 19/20 | 15/20 | 33 | 35 | 4 faillites (deux impayés d'affilée), 1 achevé par une crise |
| dépensier | 3 | aucun | 5/20 | 0/20 | 36 | – | 17 faillites (deux impayés d'affilée), 3 achevés par une crise |
| mixte | 0 | complet | 20/20 | 20/20 | 33 | 32 | – |
| mixte | 3 | complet | 20/20 | 20/20 | 35 | 42 | – |
| contrats seuls (témoin, n'achète rien) | 0 | aucun | 0/20 | 0/20 | – | – | 20 bloqués à Freelance au bout de 2 h |

Mois des seuils, défaites détaillées et écarts min–max : voir la sortie complète du test.

## Lecture

- **Pression 0** : le jeu reste accueillant. Les styles raisonnables gagnent toujours, en environ 35 minutes comme avant. Le joueur impatient qui dépense tout perd maintenant 5 runs sur 20, contre 1 avant : garder une réserve devient une vraie leçon.
- **Pression 3** :
  - Les runs passent de 37 à 40 minutes à 44 à 49 minutes, et l'écart entre les styles se creuse : le prudent est le plus lent, le clic et l'équipe les plus rapides.
  - Dépenser sans réserve ne passe plus du tout (0/20). La moitié de ces runs meurent avant même le statut Freelance, comme avant les changements.
- **Méta-progression** : elle n'est pas nécessaire pour gagner une première fois, puisque tous les styles raisonnables gagnent sans Héritage. Avec l'Héritage complet, on gagne environ 3 minutes.
- **Hasard** : aucune défaite ne vient d'un tirage caché. Les crises sont annoncées à l'avance, et les défaites viennent toutes de la trésorerie.

## Limites

- **Le bot reste plus fort qu'un humain** : il accepte chaque contrat faisable à l'instant où il arrive et clique sans pause pendant 45 minutes. Un humain mettra plus longtemps et prendra plus de risques. Seules de vraies parties diront si la pression 3 est assez dure ou trop longue.
- **Aucun taux de victoire cible n'a été fixé**. Ce choix revient au concepteur du jeu.
- **Les styles raisonnables gagnent encore toujours en pression 3**. Pour qu'eux aussi perdent parfois, les pistes suivantes sont à essayer : des crises plus longues, un contrôle fiscal qui se cumule, ou une limite de temps pour la pression 3.
