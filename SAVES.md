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

- **Versionné** : `Format` vaut 4 (plusieurs sites, lot 4). Un fichier d'un format plus récent est refusé avec un message, sans être touché. Les formats 1, 2 et 3 sont convertis au chargement, l'original étant copié en `.v<format>.json` (voir plus bas).
- **Format 3 → 4** (`Core/persistence/MigrationV3.cs`) : le local devient le premier site (`Sites`, `SiteId`), son adresse est reprise de son nom, les loyers déjà facturés restent marqués pour ce site (aucun loyer payé deux fois). Vérifié sur une vraie sauvegarde du jeu (`Tests/fixtures/campaign_v3.json`, test `AFormat3SaveBecomesTheFirstSiteWithoutPayingRentTwice`). Nouveaux champs du format 4 : sites, offres cloud, hébergement interne des projets, incidents des 30 derniers jours, réserve d'hébergement.
- **Résumé** lisible sans charger la campagne (liste des sauvegardes : nom, date de jeu, entreprise, situation, trésorerie, dernière écriture).
- **Campagne complète** : minute de jeu, état du générateur aléatoire (`Random.State`), compteur d'identifiants (`NextId`), local et meubles (positions, attributions), cartons, personnes (compétences, énergie, moral, affectation, horaire, congés, formation, augmentations, préavis), candidats, projets (type, public, prix, couleur, ordre, composants et leur qualité, file d'améliorations, version, entretien, serveurs automatiques et plafond, marketing, liste de souhaits, marché, avis, historique de 400 jours), tâches avec leur avancement, concurrents, comptes (trésorerie, mouvements, factures, dettes, prêts, totaux mensuels, coûts courus), situation financière, notifications (regroupements et résolutions), événements déjà traités, jalons, points et nœuds de l'arbre, accomplissements déjà récompensés.
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

## Sauvegardes de la version à prestations (format 1)

Au chargement d'un fichier au format 1, le jeu :

1. **copie l'original** en `<nom>.v1.json` à côté (jamais écrasé) ;
2. **arrête les prestations en cours sans pénalité** : les demandes disparaissent, aucun retard ne sera plus compté ;
3. **garde l'argent tel quel** : trésorerie, factures clients à encaisser (y compris les acomptes et soldes déjà facturés), dettes, prêts. Rien n'est ajouté ni retiré (testé) ;
4. **convertit les produits** : chaque module publié devient un composant de niveau 1 avec sa qualité mesurée (la présentation devient « Interface soignée »), la version publiée devient la version en ligne, l'accent de la page d'accueil devient la couleur du projet ;
5. **remet en file** les modules d'une version en cours de fabrication, **avec leur avancement** ;
6. **reprend les affectations** : produit, support ou maintenance d'un produit → ce projet ; prestation → automatique ;
7. **débloque les types** déjà utilisés (communauté, place de marché) pour ne rien retirer au joueur ;
8. affiche un message résumant la conversion.

L'ancien éditeur de pages n'existe plus. Les titres, textes, dispositions et pages ne sont pas repris : seule la couleur est gardée. Vérifié sur une vraie sauvegarde au format 1 (`Tests/fixtures/campaign_v1.json`, test `AnOldSaveWithContractsIsMigratedWithoutLosingMoney`).

## Ancien roguelike

`profile.json` contenait des lingots, l'Héritage, des records et des réglages. Ces notions n'existent pas dans la simulation.

- **Repris** une seule fois, au premier lancement : volume de la musique, volume des bruitages, effets réduits (copiés dans `settings.json`).
- **Pas repris** : lingots, Héritage, personnages débloqués, records. Ils ne sont convertis ni en argent ni en bonus.
- Les runs du roguelike n'étaient jamais sauvegardées en cours de partie : il n'y a rien d'autre à migrer.
- Le fichier reste en place, intact (testé). Le code de l'ancien jeu est archivé dans git sous le tag `archive/roguelike-v1`.

## Limites

- Chaque format a son passage : 1 → 2 (`Migration.cs`), 2 → 3 (`MigrationV2.cs`), 3 → 4 (`MigrationV3.cs`). Chaque nouveau format devra ajouter le sien.
- Les sauvegardes ne sont pas chiffrées ni signées : on peut les modifier à la main.
