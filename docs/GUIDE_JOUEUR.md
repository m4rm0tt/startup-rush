# Startup Rush : guide du joueur (source texte à jour)

Cette page remplace, comme **source de contenu à jour**, `docs/Startup_Rush_Guide.pdf` et `docs/Startup_Rush_Dossier.pdf`, qui décrivent
encore la version 2.1 (voir `docs/DISTRIBUTION.md`). Les PDF n'ont pas pu être régénérés sur le VPS : voir « Régénérer les PDF » en bas.

## Ce qu'est le jeu

Tu fondes une entreprise de tech à Prilly, dans ta chambre. Tu crées des produits (sites, applications, jeux, cartes électroniques, PC,
modèles d'IA…), tu y travailles toi-même au terminal, tu embauches, tu aménages tes locaux (atelier, bureaux, salle serveur, laboratoire),
et tu fais croître la **valorisation** de l'entreprise jusqu'à l'**objectif final** (affiché dans Finances › Objectifs).
La partie se sauvegarde toute seule ; une faillite met fin à la partie, une réussite te laisse continuer.

## Premiers pas (le tutoriel le guide en 11 étapes)

1. **Projets → Nouveau projet** : un type (application, jeu…), un public, c'est tout.
2. **Travailler** (Entrée) ouvre le terminal : tape les mots affichés ; une série de mots justes accélère le travail.
3. Quand le projet est prêt : **Lancer**. Ensuite l'équipe le fait évoluer ; lis les **Avis** pour savoir quoi améliorer.
4. Les jalons donnent des **points de développement** (menu Développement) : nouveaux types de projets, serveurs, recrutement…
5. Surveille l'argent (en haut à droite) : charges, revenus, et le délai avant impayé sont expliqués dans Finances (bouton « ? »).

## Menus et touches

| Menu / geste | Touche | Pour |
|---|---|---|
| Projets | P | Portefeuille, composants, avis, prix, marketing, serveurs |
| Équipe | T | Qui fait quoi, congés, formation, recrutement |
| Aménagement | B | Meubles, couleurs, postes, cartons de déménagement |
| Développement | D | L'arbre de développement, la recherche, l'IA, le laboratoire |
| Finances | F | Argent, tendances, objectifs, bilan |
| Journal | N | Problèmes à traiter, bonnes nouvelles |
| Pause / vitesses | Espace / 1 à 4 | Vitesse 4 : la plus rapide |
| Plein écran | F11 | Aussi dans Réglages |
| Échap | | Ferme ce qui est ouvert, sinon ouvre le menu pause |

## Réglages utiles

Plein écran, **texte agrandi** (le texte courant passe de 18 à 24 px), langue (français / English), volumes de la musique et des
bruitages, « Ouvrir le dossier des journaux » (assistance en cas de plantage).

## Sauvegardes

`%AppData%\StartupRush\campaigns\<partie>\` : sauvegardes nommées, 3 automatiques tournantes, sauvegarde de sortie. Une sauvegarde
abîmée est remplacée par la dernière lisible de la même partie. « Supprimer la partie » demande confirmation et déplace le dossier
dans `StartupRush\deleted\` (rien n'est effacé). Détails : `SAVES.md`.

## Régénérer les PDF (à faire sous Windows)

- `tools/build_dossier.py` fabrique `Startup_Rush_Dossier.pdf` : il demande Python, `reportlab`, `Pillow`, les polices Windows
  (Segoe UI) et les captures de `docs/captures/` (à refaire avec `UI.exe --capture docs/captures`, ou la catégorie Shots des tests).
  Son texte décrit la version 2.1 : il faut d'abord le réécrire d'après ce guide, `README.md`, `DESIGN_SIMULATION.md` et `docs/V1_ETAT.md`.
- Le **guide illustré** n'a **aucun script dans le dépôt** (le PDF a été produit lors d'une ancienne session, script non conservé) :
  à refaire, par exemple en exportant cette page en PDF.
