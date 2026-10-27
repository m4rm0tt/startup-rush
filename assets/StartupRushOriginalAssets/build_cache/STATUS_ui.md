# Statut du lot UI (polices, icônes, curseurs, effets, HUD, terminal, composants de projet)

Manifeste partiel : `build_cache/custom_ui.json` (**326 entrées**, IDs préfixés `font_`, `icon_`, `cursor_`, `vfx_`,
`hud_`, `term_`, `comp_`, `port_`, `link_` ; aucune collision avec les autres lots, vérifié).
Tous les PNG sont RGBA 8 bits avec bloc sRGB ; 487 fichiers référencés, tous présents ; rectangles d'atlas vérifiés.

## Livré

| Famille | Contenu | Fichiers |
|---|---|---|
| Polices | `StartupRush Pixel` (proportionnelle, chiffres tabulaires) + `StartupRush Mono` (chasse 6 px), 152 caractères chacune (ASCII 32–126, accents français majuscules/minuscules, œ Œ æ Æ, € £ °, « » ‘ ’ “ ” …, – —, ×, ÷, ±, ≤, ≥, ≠, espaces insécables). Grille 12 px : ascendante 10, descendante 2, capitales 7, hauteur d'x 5. | `assets/fonts/*.ttf`, atlas bitmap + métriques JSON (repli pixel exact) |
| Icônes | 137 icônes 16×16 blanches (teinte par multiplication) : les 118 du vocabulaire de la section 9 du brief, toutes couvertes, plus les utilitaires (chevrons, menu déroulant, plus d'actions, actualiser, afficher, notifications, favori, idée, trophée, utilisateur, terminal, nuage, courbe, poignée, ouvrir à part, messages) et 19 icônes propres aux composants de projet | `assets/hud/icons/icon_*.png`, `assets/hud/icons_atlas.png/.json` |
| Curseurs | 12 curseurs : normal, sélection, clic/action, déplacement, placement valide, placement invalide, redimensionnement horizontal, vertical et deux diagonales, saisie de texte, occupé (statique). Hotspot donné dans le manifeste ; exports ×2 marqués « technique » | `assets/cursors/` (12 + 12 ×2) |
| Effets | 9 particules statiques (étincelles petite et grande, idée, travail en cours, code, alerte, succès, pièce, moral), 5 losanges de case 64×32 (survol, sélection, valide, invalide, emprise), 2 anneaux. Animation suggérée par le code dans `animation_hint` | `assets/vfx/` (16) |
| HUD « Papier » | 85 composants (200 images d'état) : panneaux principal et secondaire, zone en creux, fenêtre, barre de titre, modal + voile, bandeau + extrémités, popover + 4 pointes, infobulle + pointes, notification ×4, toast, carte ×4, ligne de liste ×4, séparateurs, poignée, en-tête de section ; boutons principal, secondaire et destructif (normal, survol, pressé, désactivé, focus), bouton texte, bouton icône (+ basculé), petit bouton icône ; onglets ; contrôle segmenté (3 pièces × 4 états) ; badges ×6 + compteur ; champs texte, numérique (+ flèches), recherche, multiligne (normal, survol, focus, désactivé, erreur) ; liste déroulante + volet ; menu contextuel (panneau, éléments ×4, séparateur) ; case à cocher (9 états), radio (6), interrupteur (6) ; glissière (piste, remplissage, bouton ×4) ; barres de défilement verticale et horizontale (piste, curseur ×3, prise, flèches ×4) ; progression (piste + 4 couleurs + hachures) ; jauge à 10 segments (base, plein, carte d'index) ; pastilles couleur et motif ×3 ; primitives de graphique (cadre, grilles, axe, barre, point, légende ; teinte par multiplication) ; cadre de portrait ×2 ; barre supérieure (fond, séparateur, groupe), puce trésorerie, puce sombre. Plus `hud_confirm_dialog` (composition décrite en données) et `hud_theme_papier` (jetons de couleur) | `assets/hud/components/`, `assets/hud/theme_tokens.json` |
| Terminal | 21 éléments : cadre, barre de titre, surface (cellule 6×12), curseurs bloc, souligné et barre, champ de commande ×3, 6 marqueurs de ligne, 5 segments de progression, défilement sombre, surlignage | `assets/terminal/` |
| Composants de projet | 28 composants (17 application, 6 jeu, 5 autres) : icône + aperçu abstrait 48×32. Carte 9-slice à 5 couleurs d'en-tête (prévu, en cours, terminé, bogue, verrouillé) avec emplacements de texte et cadre de sélection 9-slice ; ports entrée/sortie × 4 côtés × 3 états ; 14 primitives de lien (droites, tirets, angles, T, pointes) et couleurs d'état | `assets/project_components/` |
| Atlas UI | 1 page 1024 px, marge 2 px, extrusion 1 px, rectangles attachés à chaque entrée | `assets/hud/ui_atlas_0.png`, `ui_atlas.json` |

Dans le manifeste, chaque composant porte : `nine_slice [g,h,d,b]`, `min_size`, `content_padding`, `stretch` (étirer ou
répéter), `resize` (deux axes, horizontal, vertical ou aucun : les contrôles d'une ligne à glyphe intégré ne s'étirent qu'en
largeur), `states`, `tokens` (couleurs de texte), et selon le cas `pressed_content_offset`, `fill_rect`, `window_rect`,
`title_bar`, `slots`, `hotspot`, `anchor`. **Aucun texte ni nombre n'est cuit.**

## Vérifications effectuées

- **Polices** : rechargement fontTools, cmap complète (0 caractère manquant). Rendu Pillow/FreeType à 12 et 24 px
  comparé pixel à pixel au rendu de référence : **0 écart, 0 pixel gris**, sur 5 phrases couvrant accents, €, CHF et
  ponctuation, pour les deux polices.
  Deux corrections ont permis d'y arriver : les pixels en contact par un coin sont tracés comme un seul contour
  auto-tangent (avec des carrés séparés, l'auto-hinter de FreeType écartait le haut des accents), et une table `prep`
  minimale plus `gasp` ont été ajoutées.
- **9-slice** : `previews/ui_nine_slice_test.png`, 19 composants rendus à 3 tailles. Coins intacts, glyphes intégrés non
  déformés grâce à `resize`.
- **Revue visuelle** de chaque planche : `previews/ui_icons.png`, `ui_hud_components_a/b.png`, `ui_terminal_kit.png`,
  `ui_cursors_vfx.png`, `ui_project_components.png`, `ui_font_specimen.png`. Icônes redessinées après revue : budget,
  recruter, affectation, qualité, utilisateurs, quantique, infrastructure.
- **13 écrans de référence** (`previews/screens/`, 640×360 natif + ×2), composés **uniquement** à partir des PNG livrés,
  du manifeste et de la police (texte rendu à l'exécution), sur fonds de pièces rendus depuis `rooms/*.json` :
  1. menu principal (Continuer / Charger / Nouvelle partie)
  2. navigateur de campagnes
  3. HUD compact
  4. panneau d'aménagement
  5. fiche employé (portrait recomposé depuis les couches et masques de personnage livrés)
  6. projets et contrats
  7. produit et graphe de composants
  8. hébergement et serveurs
  9. finances (graphique tracé depuis des données)
  10. sélection de locaux
  11. panneau industriel futur
  12. terminal de déploiement fictif local
  13. paramètres, menu contextuel et dialogue de confirmation

## Manques et limites

- **Avalonia non testé** : le jeu n'a pas été lancé. Recommandations notées dans l'entrée police : tailles 12, 24 ou 36 px,
  `RenderOptions.TextRenderingMode="Alias"`, positions entières, sinon atlas bitmap.
- **Jauge** : remplissage par segments via la carte d'index, pas d'aiguille rotative (évite une rotation en pixel art).
- **Infobulle** : pointes haut et bas seulement ; le popover a les 4 directions.
- **Écrans de référence** : ce ne sont pas des assets. Leurs fonds dépendent de `rooms/*.json` et de
  `tools/srgen/rooms.py` (autres lots), avec repli sur `previews/rooms/*.png`.
- **Icônes chargées à 1×** : `design` (plume) et `comp_messaging` restent un peu denses ; correctes à ×2.

## Code partagé

Aucun fichier partagé modifié. J'avais constaté que `scene.Library` plantait sur les JSON de QA dans `build_cache` ;
c'était déjà corrigé (filtre `custom_`/`iso_`) au moment où je l'ai utilisé.

## Reconstruire

```
python StartupRushOriginalAssets/tools/build_all.py --modules defs_ui     # polices, icônes, HUD, atlas, planches (~1,5 s)
python StartupRushOriginalAssets/tools/ui_screens.py                      # 13 écrans de référence (~5 s)
```

Sources éditables :
- `sources/ui/font_glyphs.py`
- `sources/ui/icons_a.py`, `icons_b.py`, `icons_c.py`
- `sources/ui/cursors_vfx.py`
- `sources/ui/hud.py`, `terminal_kit.py`, `project_components.py`
- `sources/defs_ui.py`

Outils :
- `tools/srgen/lot_ui_core.py`, `lot_ui_font.py`, `lot_ui_icons.py`, `lot_ui_atlas.py`, `lot_ui_preview.py`
- `tools/ui_screens.py`
