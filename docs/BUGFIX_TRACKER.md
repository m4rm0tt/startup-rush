# Suivi des corrections : retours de deux sessions de test

Source : `docs/test.md` (testeur 1) et les remarques du testeur 2 (consigne du 2 novembre 2026).
Branche `new-assets`. Commits : `ebadf50`, `7896ec5`, `a2b0867`, `c3a23a8`, `a119f08`.

Vérification : `dotnet test Tests/Tests.csproj` (201 tests) et `dotnet test UiTests/UiTests.csproj` (420 tests) passent.
Le parcours `UI.exe --capture` tourne jusqu'au bout (59 captures), et j'ai regardé les captures des écrans touchés.
Les tests ne pilotent pas une vraie souris Windows : « corrigé » veut dire que la cause est supprimée et qu'un test automatique
le vérifie. Il manque encore une session de jeu humaine (voir `BUGFIX_VALIDATION.md`).

## Cause commune de BUG-001 à BUG-006

Chaque menu ouvert était **détruit et reconstruit en entier chaque seconde** (`GameScreen.RefreshPanel`). La barre du haut
l'était **4 fois par seconde**, et la fiche d'une personne sélectionnée aussi. Le contrôle sous la souris disparaissait donc en
permanence. Avalonia recalculait alors le curseur et le survol, fermait l'infobulle, oubliait le bouton appuyé et fermait la
liste déroulante, dont le bouton n'existait plus. La barre de défilement lâchée était recréée. La position de défilement était
« restaurée » avant la mise en page, ce qui la ramenait à 0.

Correction structurelle : `UI/Hud/UiPatch.cs`. Le nouvel arbre est fusionné dans celui qui est affiché : un contrôle du même
type au même endroit est gardé et ne reçoit que les valeurs qui ont changé. Pour que ce soit sûr :
- l'action d'un bouton est une valeur recopiée (`Ui.OnClick`), et le bouton gardé exécute celle de la dernière construction ;
- les survols sont déclaratifs (`PixelPanel.HoverSkin`, `FocusSkin`, `HoverImage`) et plus écrits par des gestionnaires ;
- les barres de progression sont des colonnes proportionnelles (plus de calcul dans `SizeChanged`) ;
- aucune mise à jour pendant un clic ni sous un menu ouvert (`HudKit.OpenFlyout`).

Les règles pour écrire un nouveau menu sont en tête de `UiPatch.cs`.

## Tableau

| ID | Description | Priorité | Cause identifiée | Fichiers | Correction | Test | Statut |
|---|---|---|---|---|---|---|---|
| BUG-001 | Curseur qui clignote | Critique | Contrôles détruits sous la souris (menus 1×/s, barre du haut 4×/s) ; flèche Windows sur les menus et flèche du pack sur la scène, qui alternaient ; recomposition de toute la pièce à chaque survol d'un objet haut | `UiPatch.cs`, `GameScreen.cs`, `SceneCanvas.cs` | Mise à jour sur place ; curseur du pack sur tout l'écran de jeu ; plus d'estompage au survol | `StabilityTest` : boutons de vitesse et du catalogue gardés à l'identique d'un rafraîchissement à l'autre | Corrigé (cause supprimée, à confirmer à la souris) |
| BUG-002 | Fenêtres, listes et infobulles qui se ferment ou clignotent (catégories du catalogue, description d'un serveur) | Critique | Même cause : la liste déroulante et l'infobulle appartenaient à un bouton détruit chaque seconde | `UiPatch.cs` (le Flyout est gardé, seul son contenu est mis à jour), `HudKit.cs` | Fusion + pas de mise à jour sous un menu ouvert | `APatchKeepsTheButtonAndRunsTheLatestAction`, `HudKitTest` | Corrigé (à confirmer à la souris) |
| BUG-003 | Boutons à cliquer plusieurs fois | Critique | Bouton remplacé entre l'appui et le relâchement : le clic ne se termine pas | `UiPatch.cs`, `GameScreen.cs` | Bouton gardé ; aucune mise à jour pendant un appui | `StabilityTest` | Corrigé (à confirmer à la souris) |
| BUG-004 | Un double-clic sur un objet le déplace | Critique | Le 1er clic sélectionne l'objet et ouvre sa fiche, dont « Déplacer » peut se trouver sous la souris ; le 2e clic l'actionne | `ClickGuard.cs`, `App.axaml.cs` | Un bouton apparu depuis moins de 350 ms ignore la souris (borne du double-clic) | `ADoubleClickDoesNotGoThroughToAButtonThatJustAppeared` | Corrigé |
| BUG-005 | Listes qui remontent en haut après une action | Haute | Reconstruction + restauration de défilement faite avant la mise en page ; l'objet en pose ajouté en haut décalait la liste | `GameScreen.cs`, `BuildPanel.cs` | Liste défilante gardée ; objet en pose dans un bandeau fixe (`GamePanel.BuildHeader`) hors du défilement ; rubrique retenue | `TheBuildPanelKeepsItsControlsScrollAndCatalogAcrossRefreshes`, `APatchKeepsTheScrollPosition` | Corrigé (tous les menus passent par le même code) |
| BUG-006 | Défilement difficile avec la barre de droite | Haute | Barre de défilement recréée chaque seconde : le glissement de la poignée était coupé | `GameScreen.cs` | Barre gardée ; pas de mise à jour pendant un appui | `StabilityTest` (même ScrollViewer) | Corrigé (glissement non testé à la souris) |
| BUG-007 | Fiche d'objet trop petite, boutons coupés | Haute | Quatre actions dans une rangée horizontale d'une fiche de 360 px (« Revendre 1 200 » sortait à droite) | `SelectionInspector.cs`, `GameScreen.cs` | Actions dans un WrapPanel (retour à la ligne), fiche portée à 380 px | `TheItemInspectorWrapsItsButtonsInsideItsFrame` | Corrigé |
| BUG-008 | Haut d'un objet tronqué et transparent au survol | Haute | Estompage voulu au survol : le haut d'un objet haut passait à 35 % d'opacité | `SceneCanvas.cs` | Plus d'estompage au survol ; il reste seulement devant une personne assise | Captures relues | Corrigé |
| BUG-009 | Alignements incohérents | Haute | Plusieurs causes locales (texte non centré dans une grille, texte dans une rangée horizontale qui ne revient pas à la ligne, bulles par-dessus la fiche) | `BuildPanel.cs`, `SelectionInspector.cs`, `GameScreen.cs` | Nom des objets des cartons centré ; novembre : texte d'effet de la fiche d'objet qui revient à la ligne, bulles posées au-dessus de la fiche de sélection, tuiles de capacité sans texte coupé, consigne du tutoriel dans une grille, barres de compétences d'une fiche alignées (colonnes fixes), graphique « Résultat par site » sans moitié vide sous les barres (montant affiché), bulles sous les grands menus au lieu de couvrir leur bas. Équipe, Finances, Développement, Sites, fiche d'une personne relus en capture. Menus encore non relus : Recherche, Laboratoire, Cloud, Production, Journal | Captures | Partiellement corrigé |
| BUG-010 | Police trop fine, textes peu lisibles | Haute | 1) Faux gras sur une police qui n'a qu'une graisse (lettres empâtées). 2) **Toute taille entre 13 et 17 est ramenée à 12 px** (`PixelText.Snap`, police nette seulement en multiples de 12) : presque tout le texte courant fait 12 px | `PixelText.cs` | 1) corrigé : graisse normale partout. 2) non changé : il faut une décision de direction artistique (texte courant en 24 px, ou accepter une police moins nette), voir le backlog | Captures relues (« mise à jour » lisible) | Partiellement corrigé |
| BUG-011 | Modules prêts à poser : pas de marge au-dessus de la description | Haute | Le titre ne remplissait pas la bande de 32 px de la carte du pack : la description touchait son bord | `BuildPanel.cs` | Titre à la hauteur de la bande + marge de 8 | Capture 41b relue | Corrigé |
| BUG-012 | Onglets de projets : barre orange, bouton Réglages à moitié caché, couleurs proches | Haute | Barre orange et fond beige : dessins du pack (`hud_tabs`) ; texte des onglets inactifs gris sur beige | `App.axaml`, `Ui.cs` (`Tabs`) | Novembre : les onglets utilisent le contrôle segmenté du pack (actif en sarcelle, texte clair ; inactifs à l'encre sur fond clair), plus grands ; plus de barre orange | Suite UiTests, captures | Corrigé |
| BUG-013 | Contrôles du temps : « Nuit accélérée » déplace la barre | Haute | Étiquette ajoutée dans la rangée centrée des vitesses, qui changeait de largeur | `GameScreen.cs` | Emplacement fixe de 200 px, compensé à gauche : les vitesses ne bougent plus. Règles du temps inchangées | `TheTopBarKeepsItsSpeedButtonsAndDoesNotMoveWithFastNights` | Corrigé (cohérence des 4 icônes de vitesse : non traitée) |
| BUG-014 | « Ouvrir les cartons » n'ouvre pas les cartons | Haute | Le bouton et les notifications de déménagement ouvraient l'aménagement en haut ; les cartons étaient tout en bas | `BuildPanel.cs`, `GameScreen.cs`, `SelectionInspector.cs`, `Campaign.Sites.cs` | Cartons en tête du menu ; `BuildPanel.AtCartons()` y fait défiler ; notifications `build:cartons` | `OpenTheCartonsGoesToTheCartonsSection` | Corrigé |
| BUG-015 | Icône d'alerte persistante devant la porte | Haute | Moteur : une personne « en attente à l'entrée » n'était replacée qu'au lendemain, même une fois son poste complet. Interface : l'alerte n'expliquait rien | `Campaign.Time.cs` (`SettlePlaces`), `GameSession.cs`, `SceneCanvas.cs`, `SelectionInspector.cs` | Places revues après tout changement d'aménagement (même en pause) ; l'alerte se clique et la fiche dit ce qui manque, avec un bouton vers les cartons ou l'aménagement | `SomeoneWaitingAtTheDoorSitsAsSoonAsTheirPostIsBackEvenPaused` | Corrigé |
| BUG-016 | Baie haute densité qui traverse les chemins de câbles | Haute | Même emprise : le tri les départageait selon l'ordre de pose. Le guide du pack demande de dessiner le chemin de câbles (et son coin) après | `RoomRenderer.cs` | Règle « overhead » dans le tri topologique (seuls 2 objets du pack l'ont) | `ACableTrayIsDrawnOverTheRackBelowWhateverTheOrderOfPlacement` (539 px différaient avant) | Corrigé |
| BUG-017 | « Publier » une IA : aucun résultat visible | Haute | La mécanique marche selon les règles : sans produit qui utilise le modèle, la publication ne rapporte rien. Mais le message ne le disait pas | `Campaign.Ai.cs`, `AiPanel.cs` | Message qui nomme les produits servis ou explique quoi faire ; liaison automatique aussi à la publication (filet de sécurité) | `PublishingSaysWhatItChangesAndLinksAnAiProductLeftWithoutModel` | Corrigé (manque d'explications : voir backlog) |

## Sauvegardes

Aucun champ sauvegardé n'a été ajouté (`_placesDirty` n'est pas sérialisé ; une cible de notification `build:cartons` reste
lisible par l'ancien code). Format inchangé (7). Les tests de migration (`Tests/persistence`) passent.

## Ouvert

- BUG-009 : menus encore non relus en capture : Recherche, Laboratoire, Cloud, Production, Journal.
- BUG-010 : taille du texte courant (décision de direction artistique, voir le backlog).
- BUG-013 : les quatre vitesses n'ont que trois icônes dans le pack (le chiffre départage).
- Rien n'a été validé à la souris par un humain : `BUGFIX_VALIDATION.md`.
