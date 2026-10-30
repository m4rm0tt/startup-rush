# Enquête : `marketing_excessif` passe de 2 à 4 faillites sur 5 après le lot 5

**Conclusion prouvée : ce n'est pas une règle du jeu qui a changé, c'est le tirage aléatoire.** Le lot 6 (commit `e9eb639`)
a ajouté six publics non matériels (`ai_assist`, `ai_creative`, `ai_analytics`, `ai_api`, `xr_gamers`, `xr_pros`). Leurs
concurrents étaient tirés à la création de chaque partie (`Campaign.SeedCompetitors`), ce qui consomme des tirages de
`SimRandom` et décale tout le hasard qui suit, même dans une partie qui ne touche jamais à l'IA ni à la XR.

## Protocole

Code extrait par `git archive` dans un dossier temporaire (le dépôt n'est pas touché), même sonde aux trois états, mêmes
graines (11, 23, 37, 41, 59), 18 mois, `BalanceReportTest.Play`.

| État du code | `marketing_excessif` faillites | Argent final (graines 11, 23, 37, 41, 59) | `plusieurs_petits` (graine 11) |
|---|---|---|---|
| `fa42c76` (fin du lot 5) | **2/5** | 39 168 · faillite · 39 632 · faillite · 35 088 | 191 938 |
| `e9eb639` (lot 6) | **4/5** | faillite · faillite · 42 758 · faillite · faillite | 169 047 |
| `e9eb639` sans tirer les concurrents des 6 nouveaux publics à la création | **2/5** | identique au lot 5, au centime | 191 938, identique au lot 5 |

## Ce que ça veut dire

- Les règles du lot 6 (studio, IA, XR) n'ont aucun effet sur ces stratégies : sans le décalage du hasard, les résultats sont
  identiques au centime.
- `marketing_excessif` est **à la limite** : selon le tirage, 2 ou 4 parties sur 5 font faillite. Le marketing excessif est puni
  par la bonne mécanique (6 000 CHF par mois pour une petite application, sans correction), mais 5 graines ne suffisent pas à
  mesurer un taux de faillite : le bilan final utilisera davantage de graines et donnera la dispersion.
- Correction de robustesse retenue : les concurrents d'un public sont tirés la première fois qu'un projet le vise (comme pour le
  matériel depuis le lot 5). Ajouter un public au catalogue ne décale plus les parties existantes ni les nouvelles.
