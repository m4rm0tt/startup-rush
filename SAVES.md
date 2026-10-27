# Sauvegardes

## Où

`%AppData%\StartupRush\` :

| Fichier | Contenu |
|---|---|
| `campaigns\<identifiant de campagne>\manual-<nom>.json` | Sauvegardes nommées (menu pause). « Début » est écrite à la création. |
| `campaigns\<id>\auto-1.json` à `auto-3.json` | Sauvegardes automatiques tournantes, chaque lundi à minuit de jeu ; la plus ancienne est remplacée. |
| `campaigns\<id>\exit.json` | Sauvegarde de sortie : écrite en quittant la partie, en revenant au menu ou en fermant la fenêtre. « Continuer » reprend la plus récente. |
| `*.bak` | Version précédente de chaque fichier, gardée à chaque écriture. |
| `*.corrupt` | Copie d'un fichier illisible, conservée pour diagnostic. |
| `settings.json` | Réglages communs (volumes, taille de l'interface, pause des grands menus, nuits accélérées). |
| `profile.json` | Ancien profil du roguelike : **jamais modifié ni supprimé**. |

Chaque campagne a son dossier : une nouvelle partie n'écrase jamais une autre.

## Format

JSON, une seule ligne :

```json
{ "Format": 1, "Game": "StartupRush-simulation",
  "Summary": { "CampaignId", "CampaignName", "Company", "Minute", "Cash", "Situation", "People", "WrittenUtc" },
  "Campaign": { ...tout l'état... } }
```

- **Versionné** : `Format` vaut 1. Un fichier d'un format plus récent est refusé avec un message, sans être touché.
- **Résumé** lisible sans charger la campagne (liste des sauvegardes : nom, date de jeu, entreprise, situation, trésorerie, dernière écriture).
- **Campagne complète** : minute de jeu, état du générateur aléatoire (`Random.State`), compteur d'identifiants (`NextId`), entreprises, local et meubles (positions, attributions), cartons, personnes (compétences, énergie, moral, affectation, position et chemin en cours, formation, préavis), candidats, contrats (offres, en cours, historique), tâches avec leur avancement et leur qualité, produits (brouillon, version en fabrication, version en ligne, qualité des modules, marché, historique de 400 jours), concurrents, comptes (trésorerie, mouvements, factures, dettes, prêts, totaux mensuels, coûts courus), situation financière, notifications, événements déjà traités, jalons.
- Les propriétés calculées ne sont pas écrites.

Conséquences, vérifiées par `Tests/persistence/SaveTest.cs` :

- Recharger redonne **exactement** le même état, et le même futur : deux copies avancées de 20 jours restent identiques octet pour octet.
- Les identifiants sont stables : pas de personne dupliquée, pas de facture repayée, pas de récompense redonnée.

## Écriture sûre

1. Le contenu est écrit dans `<fichier>.tmp`.
2. `File.Replace` remplace le fichier en gardant l'ancien en `.bak` (première écriture : simple déplacement).

Une coupure pendant l'écriture laisse soit l'ancien fichier, soit le nouveau, jamais un fichier à moitié écrit. La sauvegarde se fait entre deux minutes de jeu, sur le fil de l'interface : aucun événement ne peut tomber au milieu.

## Fichier abîmé

- La liste « Charger » affiche les fichiers illisibles comme « fichier abîmé » au lieu de les cacher.
- Au chargement, si le fichier ne se lit pas : il est copié en `.corrupt`, puis le jeu essaie le `.bak`. Si la copie est bonne, elle est chargée avec un message ; sinon, un message propose de choisir une autre sauvegarde de la campagne.
- Rien n'est supprimé.

## Ancien roguelike

`profile.json` contenait des lingots, l'Héritage, des records et des réglages. Ces notions n'existent pas dans la simulation.

- **Repris** une seule fois, au premier lancement : volume de la musique, volume des bruitages, effets réduits (copiés dans `settings.json`).
- **Pas repris** : lingots, Héritage, personnages débloqués, records. Ils ne sont convertis ni en argent ni en bonus.
- Les runs du roguelike n'étaient jamais sauvegardées en cours de partie : il n'y a rien d'autre à migrer.
- Le fichier reste en place, intact (testé). Le code de l'ancien jeu est archivé dans git sous le tag `archive/roguelike-v1`.

## Limites

- Un seul format (1) existe : il n'y a pas encore de migration entre versions de sauvegarde. Le champ `Format` est là pour la prévoir.
- Les sauvegardes ne sont pas chiffrées ni signées : on peut les modifier à la main.
