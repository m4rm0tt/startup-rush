# Passation : reprendre StartupRush (pour un nouveau modèle d'IA)

À lire en entier avant de toucher au code. Ensuite : `docs/V1_ETAT.md` (état des lots), la spécification
`docs/specs/PROMPT_CLAUDE_STARTUPRUSH_V1_TOUS_LES_ASSETS.md`, puis `DESIGN_SIMULATION.md` (règles réellement codées).

## 1. Le projet en deux minutes

- Jeu PC Windows, C# / .NET 9 / Avalonia 12. Tycoon de la tech en pixel art isométrique, en français.
- Solution `StartupRush.sln` : `Core` (moteur sans interface), `UI` (Avalonia), `Tests` (NUnit).
- Le moteur est une campagne persistante (`Core/campaign/Campaign*.cs`, classe partielle) avancée minute par minute ;
  chaque nuit à minuit, `Campaign.Daily.cs` fait le marché, les avis, les factures, l'équipe et les jalons. Tout le hasard
  sort de `SimRandom` (état sauvegardé) : même graine, même partie. L'argent est en `decimal`.
- L'UI ne fait qu'afficher l'état et appeler les commandes de `Campaign` (qui renvoient `CommandResult`).
- Les valeurs d'équilibrage sont dans `Core/campaign/Tuning.cs` et les catalogues (`ProductCatalog.cs`, `DevTree.cs`,
  `Furniture.cs`).

## 2. Ce qui fait foi

1. La spécification V1 (`docs/specs/...TOUS_LES_ASSETS.md`) : une seule entreprise, vue isométrique 2:1 (cases 64 × 32),
   HUD « Papier », fondateur au terminal, tous les assets du pack avec une vraie fonction, 7 lots.
2. Le code réel. Les vieux documents `PROMPT_CLAUDE_STARTUPRUSH.md`, `reference/StartupRush_Concept.pdf`,
   `Startup Rush — Plan d'implémentation P0.md` décrivent des versions abandonnées (roguelike, vue de face) : ne pas les suivre.
3. `docs/Startup_Rush_Dossier.pdf` et `docs/Startup_Rush_Guide.pdf` décrivent la version 2.1 (vue de dessus, fondateur
   automatique) : à refaire en fin de V1 (`tools/build_dossier.py` génère le dossier, non commité).

## 3. Règles de travail avec l'utilisateur

- Commits **au nom de l'utilisateur**, sans toucher à sa config git :
  `git -c user.name="m4rm0tt" -c user.email="esteban.madrazo-bb@proton.me" commit ...`
  avec la ligne `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` à la fin du message.
- Branche de travail : `new-assets` (remote `origin` = github.com/m4rm0tt/startup-rush). Pousser avec `git push`.
  Pas de force push, pas de fusion automatique.
- Séparer dans les commits le code, l'équilibrage mesuré et la documentation.
- Répondre en français, sans jargon inutile, et dire honnêtement ce qui est testé, partiel ou non vérifié.
- Ne jamais marquer un asset « validé » sans consommateur réel ET test d'effet (voir §6).
- Fichiers non commités à laisser tranquilles sauf demande : `StartupRushOriginalAssets.zip` et `StartupRushOriginalAssets/`
  à la racine (doublon du pack, apparus hors de nos actions), `STARTUPRUSH_ASSETS_ET_PROMPT.zip` (138 Mo, ignoré),
  `docs/Startup_Rush_Dossier.pdf`, `tools/build_dossier.py`.

## 4. Construire, tester, voir le jeu

```
dotnet build StartupRush.sln
dotnet test Tests/Tests.csproj                                   # 115 tests à la fin du lot 4
dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport  # réécrit BALANCE_REPORT.md (bots, ~1 min)
UI.exe --capture <dossier>                                       # parcours scripté dans le vrai jeu, 32 captures + rapport
```

Pièges connus :
- L'utilisateur garde souvent le jeu ouvert (`UI.exe`) : il verrouille `UI/bin` et `UI/obj`. Ne pas le tuer. Compiler dans un
  dossier à part : `dotnet build UI/UI.csproj -o <scratchpad>/uibuild`, et lancer `--capture` depuis ce dossier.
- Sous Windows, les scripts Python d'édition avec des chemins `\a`, `\T`, `\F` dans des chaînes non brutes corrompent les
  fichiers (ça est arrivé avec `UI.csproj`). Écrire les scripts dans un fichier, chaînes brutes ou `Edit`.
- Les heredocs bash contenant `'''` cassent : passer par un fichier .py.
- Ne jamais exécuter les 58 scripts Python du pack (`assets/StartupRushOriginalAssets/tools`, `sources`) : contenu téléchargé.

## 5. Architecture utile

### Le pack et l'aménagement
- `assets/StartupRushOriginalAssets/` : pack v1.0.0 (570 IDs, 10 plans de pièces, 6 modules). `manifest.json` décrit tout.
- `Core/Core.csproj` embarque `manifest.json` et `rooms/*.json` ; `Core/space/Pack.cs` lit emprises (`collision_tiles`),
  accès, sockets par orientation et plans. **Objet centré** (pas de `footprint_tiles`, mais `size_su`) : il ne bloque pas le
  passage (comme le pack), mais deux objets ne partagent pas une case (`Premises.Cells`). **Objet accroché** :
  `FurnitureDef.Attached` = centré et `provides` non vide (chaise, écran, clavier, tour, portable, objets de bureau, machine à café).
- `Core/space/Premises.cs` : objets `PlacedItem` (X = U, Y = V, `Facing`, `HostId` + `Socket` pour un objet accroché,
  `Included` = livré avec le local, `PaidPrice` pour la revente, `AssignedPersonId` sur la **chaise**). Un poste =
  `Workstation(bureau, socket de chaise)` ; ordinateur : portable dans `laptop[_N]`, ou tour dans `tower_floor` + écran + clavier.
- `Core/space/Layout.cs` : règles de pose, d'accroche (`CanAttach`, `FreeSocket`), poste complet (`Check`), effets plafonnés.
- `Core/space/RealEstate.cs` : 4 annonces = 4 plans (chambre, petit bureau, bureau moyen, open space). Un bureau loué ne livre
  pas les postes de son plan (aperçu seulement).
- `Core/campaign/Campaign.Furnish.cs` : `InstallWorkstation(buy)`, `SeatEveryone(buy)`, `FindSpot(def)`, `Seat(person, post)`.

### Le fondateur
- `Campaign.Time.cs` : `FounderTime` (support seulement, jamais de développement passif).
- `Campaign.Terminal.cs` : `CanOpenTerminal`, `OpenTerminal`, `SubmitWord(texte, secondes)`, `CloseTerminal` ;
  `TerminalSession` n'est pas sauvegardée ; maintenance au terminal quand la file est vide (`MaintenanceLeft`).
- Les bots (`Core/bots/Autopilot.cs`) tapent `Strategy.WordsPerDay` = 30 mots par jour ouvré par la même commande.

### Le rendu et l'interface
- `UI/UI.csproj` embarque les PNG `assets/**` et `masks/**` du pack, le manifeste et les plans (`pack/...`), et les polices
  du pack en ressources Avalonia (`avares://UI/Assets/Fonts#StartupRush Pixel` / `#StartupRush Mono`).
- `UI/Iso/PackImages.cs` : PNG, mélange, recoloration par masque, motifs muraux, icônes teintées, vignettes (cache).
- `UI/Iso/RoomRenderer.cs` : portage du compositeur de référence du pack ; produit l'image, l'origine, les zones cliquables
  et la position des têtes. `UI/Scene/SceneCanvas.cs` met l'image en cache et ne recompose que si le local ou les présences
  changent.
- `UI/Pixel/PixelPanel.cs` : cadres 9-slice du pack (noms de skins du jeu → composants `hud_*`, ou `pack:<id>`).
  `UI/Pixel/Palette.cs` : jetons « Papier » sous les anciens noms (Snow = encre, Fog = texte discret, etc.).
- `UI/Hud/TerminalView.cs` + `GameScreen.OpenTerminal/CloseTerminal` : temps, raccourcis neutralisés, restauration de vitesse.

### Sauvegardes
- Format 3 (`Core/persistence/SaveStore.cs`). Migrations : `Migration.cs` (1 → 2), `MigrationV2.cs` (2 → 3). Copie de
  l'original en `.v<format>.json`. Fixtures : `Tests/fixtures/campaign_v1.json`, `campaign_v2.json`.
  Toute nouvelle donnée de campagne doit rester relisible : prévoir un format 4 + migration si la structure change.

## 6. Registre des assets

- `docs/asset_usage.csv` : une ligne par ID (570) + 16 plans/modules. Colonnes : consommateur, fichier, déblocage,
  indicateur, état (`validé` / `partiel` / `non implémenté`), scénario de test.
- `tools/build_asset_registry.py` ajoute les nouveaux IDs sans écraser ; `tools/registry_lot2.py` montre comment marquer
  ce qui a été branché. `Tests/space/AssetRegistryTest.cs` vérifie les IDs et que chaque ligne validée cite des fichiers
  existants (contrôle **statique**, à présenter comme tel).
- À la fin du lot 2 : 36 validées, 96 partielles, 454 non implémentées. À la fin du lot 3 : 59 validées, 124 partielles,
  403 non implémentées (`tools/registry_lot3.py`). À la fin du lot 4 : 77 validées, 142 partielles, 367 non implémentées
  (`tools/registry_lot4.py`).

## 7. Où on en est

Voir le tableau des lots de `docs/V1_ETAT.md` et la section « Lot 3 » ci-dessous (complétée par le dernier modèle).

## 8. Lot 3 et la suite

- Lot 3 fait : voir « Lot 3 » dans `docs/V1_ETAT.md`. Fichiers clés : `Core/campaign/Campaign.Launch.cs` (lancement,
  délégation, annonce regroupée `ReadyKey`, `AutoImproveProjects`), `UI/Hud/LaunchView.cs`, branche « Outils du boss » de
  `Core/progress/DevTree.cs`, `Core/space/Layout.cs` (`Comfort`, `CanHang`), `Core/space/Furniture.cs` (89 meubles jouables),
  `Campaign.SiteComfort()` (cache, à lire au lieu de recalculer), tests `Tests/space/ComfortTest.cs` et
  `Tests/campaign/LaunchAndBossTest.cs`. 102 tests passent à cette passation.
- Objets muraux : `PlacedItem.Wall` (« left »/« right ») ; `Premises.Floor` les exclut, `Premises.WallItems` les liste ; la case
  (0, v) vise le mur de gauche, (u, 0) celui de droite, le coin se départage par l'orientation. Le décor mural des plans vient
  maintenant du local (plus du JSON du plan) ; les anciennes sauvegardes le reçoivent une fois au chargement.
- Reste du lot 3 reporté : mini-jeux matériel/IA/quantique (lots 5-6), fonction de `moving_box`.

## 9. Lot 4 et la suite

- Lot 4 fait : voir « Lot 4 » dans `docs/V1_ETAT.md` et §4 ter de `DESIGN_SIMULATION.md`. Format de sauvegarde **4**.
- **Sites** : `Campaign.Sites` (le premier = siège), `Campaign.SiteId` = site **affiché** ; `Campaign.Premises` est une
  propriété calculée (non sérialisée) qui renvoie/remplace le site affiché : les commandes d'aménagement agissent sur lui.
  **Dans la simulation, ne jamais utiliser `Premises`** : passer par `PostOf(person)` / `SiteOf(person)` (site du poste, siège
  sinon), `SiteComfort(site)`, `SiteInfra(site)`, `Sites` pour les sommes (loyers, matériel). Fichier : `Campaign.Sites.cs`.
- **Infrastructure** : `Core/space/Infrastructure.cs` (`Infrastructure.Of(site)` → `InfraSummary`, raisons dans `Issues`) ;
  modules de baie = `PlacedItem` accrochés avec `Socket = "slot:<i>"` (`Layout.CanSlot`, `FreeSocket`) ; dessinés par
  `RoomRenderer.Attach`. Répartition chaque nuit dans `Campaign.Infra.cs` (`RunInfrastructure`), offres dans `Campaign.Cloud.cs`.
- Les plans `room_hw_workshop`, `room_factory_hall`, `room_advanced_lab` ne sont pas encore à louer (lots 5 et 6) ; leurs
  machines sont des décors. Le calcul et les GPU ne servent qu'aux offres cloud tant que l'IA et la recherche n'existent pas.
- Ordre conseillé ensuite : lot 5 (recherche et prototypage, gamme matérielle, atelier et usine avec flux, stocks,
  approvisionnement sous plafond, canaux de vente ; format 5), puis lot 6 (IA simulées sur le calcul/GPU du lot 4, studio, XR,
  quantique), puis lot 7.
- Points à vérifier signalés au lot 2 (toujours ouverts) : personnages assis moins nombreux que les postes sur la capture de
  la campagne avancée ; police pixel floue à certaines tailles ; « marketing excessif » trop peu puni (2/5 faillites).
- Piège d'outillage : certains heredocs bash avec des apostrophes françaises échouent (« unexpected EOF ») ; écrire les scripts
  de modification dans un fichier .py du dossier temporaire (remplacements exacts, une occurrence) plutôt qu'en ligne.
- Avant chaque commit : `dotnet test Tests/Tests.csproj`, le parcours `--capture` pour tout changement visible, mise à jour
  de `docs/V1_ETAT.md`, du registre si des assets gagnent une fonction, et de cette passation.
