# Backlog des testeurs : état de chaque demande

Les propositions des deux sessions de test (`docs/test.md` et les remarques du testeur 2), développées pendant la mission de
novembre 2026 (branche `new-assets`). Détail technique, fichiers et tests : `docs/DEVELOPMENT_PROGRESS.md`. Les bugs sont suivis
dans `BUGFIX_TRACKER.md`.

Statuts : **terminé** (comportement vérifié par un test automatique ou une capture regardée), **partiel**, **non commencé**.
Rien n'a encore été rejoué à la souris par un humain sous Windows (voir `BUGFIX_VALIDATION.md`).

## Création et tutoriel
| Demande | Statut | Ce qui a été fait |
|---|---|---|
| Nom de l'entreprise obligatoire | terminé | Vide, espaces seuls, moins de 2 ou plus de 40 caractères refusés, message sous le champ. |
| Un tutoriel | terminé | 11 étapes validées par les vraies actions, carte de consigne non bloquante, menu concerné marqué, « Passer », +1 point à la fin. |
| Premiers projets moins lents | terminé | Le premier projet est un prototype (45 % du travail) et la série de mots justes multiplie jusqu'à +50 % : ~150 mots (application) ou ~190 (jeu) au lieu de ~380. Mesuré (`EarlyGameProbe`). |
| Dormir dans son lit | terminé | Le fondateur reste chez lui le soir et dort dans son lit de 23 h à 7 h (bulle « zZ »). |
| Pause avec Espace | terminé | Espace mettait déjà en pause, mais pas quand un grand menu avait arrêté le temps : Espace relance aussi ce temps-là. |
| Pas de pause par défaut dans les menus | terminé | Réglage désactivé par défaut, y compris pour les réglages déjà écrits (une seule fois). |

## Aménagement et personnalisation
| Demande | Statut | Ce qui a été fait |
|---|---|---|
| Prévisualiser sols et murs | terminé | Aperçu dans la scène avant de payer, avec le prix. |
| Choisir des carreaux ou des murs précis | terminé | Tout le sol, une zone ou une case ; les deux murs ou un mur visé ; couleur au choix. |
| Couleur des objets | terminé | Canaux recolorables du pack (tissu, bois, métal, couleur principale...), dans la fiche de l'objet. |
| Supprimer les objets d'un nouveau local | terminé | Retirer un par un ou tout le décor ; le bâtiment (sanitaires, cuisine, technique) et le lit restent. |
| Capacité d'un espace (ex. 4/8) | terminé | « Postes : 4 / 7 » d'après les vraies règles de pose. |
| Déchets sans poubelle | terminé | Déchets au sol, moral en moins, ramassage d'un clic, corbeilles qui couvrent l'équipe. |

## Projets et production
| Demande | Statut | Ce qui a été fait |
|---|---|---|
| Changer l'ordre des composants | terminé | ▲ ▼ dans la file, suivis par l'équipe et le terminal, dépendances respectées. |
| Moins de composants, plus personnalisables, vraie sensation d'amélioration | terminé | Façon de faire à chaque évolution (rapide / équilibrée / soignée), gain d'attrait chiffré sur chaque carte, recommandations, composants peu utiles rangés à part. Aucun composant supprimé (compatibilité des sauvegardes). |
| Progression et équilibrage des employés (« trop facile ») | terminé | Prise de poste progressive, coordination plus coûteuse au-delà de 3 (5 avec un responsable), gros produits plus longs à faire évoluer. Mesuré : 8 développeurs vont ~5 fois plus vite qu'un seul (8 fois avant). |
| S'attacher à un produit | terminé | Communauté construite par les versions (rétention, bouche-à-oreille), points pour un produit qui dure. |
| Préparer de vraies mises à jour | terminé | Les évolutions attendent la prochaine version, publiée quand le joueur veut, avec bilan une semaine après. |
| Production et Stock dans Projets | terminé | Catégories Développement / Production / Stock dans Projets. |

## Gameplay et expérience
| Demande | Statut | Ce qui a été fait |
|---|---|---|
| Moins de texte, moins de temps dans les menus | partiel | Explications rangées derrière des « ? » dans l'aménagement et les projets, cartes de composants plus parlantes, actions directes (publier depuis la notification). Le reste des menus n'a pas été repris un par un. |
| Mini-jeux de publication moins ennuyeux | terminé | Quatre nouveaux mini-jeux de terminal (journal, pare-feu, fusion, commande) qui alternent et se corsent. |
| Mini-jeux de terminal variés | terminé | Voir ci-dessus ; règle en une phrase, sans connaissance préalable. |
| Expliquer l'IA | terminé | Chemin en 5 étapes avec l'étape en cours, ce que chaque choix influence, qui utilise le modèle et ce qu'il rapporte. |
| Indicateurs sur place pour le matériel | terminé | « ! » au-dessus de l'équipement, fiche « Ce qui manque » avec bouton d'achat. |

## Décisions de direction artistique liées aux bugs
- **Taille du texte (BUG-010)** : voir `BUGFIX_TRACKER.md`.
- **Onglet actif (BUG-012)** : onglets refaits avec le contrôle segmenté du pack (plus de barre orange).
- **Vitesses (BUG-013)** : voir `BUGFIX_TRACKER.md`.
