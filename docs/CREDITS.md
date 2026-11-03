# Crédits et licences du jeu livré

Ne liste que ce que l'exécutable contient ou charge réellement (vérifié le 3 novembre 2026 dans `UI/UI.csproj`, `UI/Program.cs`
et par comparaison d'empreintes MD5 des fichiers embarqués avec les packs d'origine). Détail historique et packs en réserve :
`THIRD_PARTY_NOTICES.md` (copié à côté de l'exécutable) et `ASSET_USAGE.md`.

## Images, interface et polices du jeu

| Ressource | Auteur | Licence | Source | Vérification |
|---|---|---|---|---|
| Pack `assets/StartupRushOriginalAssets` v1.0.0 : pièces, mobilier, matériel, personnages, HUD « Papier », icônes, terminal, curseurs, plans | Généré pour ce projet par Claude (Anthropic), le 27 octobre 2026, par du code (pas d'éditeur d'images, pas de générateur d'images, aucune ressource téléchargée) | Aucune licence tierce (contenu propre au projet) | `assets/StartupRushOriginalAssets/PROVENANCE.md` | Embarqué (`EmbeddedResource pack/...`) |
| Polices StartupRush Pixel et StartupRush Mono | Même pack | Même statut | `assets/StartupRushOriginalAssets/assets/fonts/` | Embarquées (`AvaloniaResource`) |
| Icône de vitesse 4 | Composée au lancement à partir du chevron de `icon_speed_3` du pack | Même statut | `UI/Iso/PackImages.cs` (`SpeedFour`) | Code |

Selon la plateforme, une mention « contenu créé avec l'aide d'une IA » peut être exigée (questionnaire Steam sur le contenu
généré par IA) : à renseigner pour le pack ci-dessus.

## Sons et musiques (CC0 1.0, domaine public)

| Fichiers du jeu | Œuvre | Auteur | Licence | Source | Vérification |
|---|---|---|---|---|---|
| `UI/Assets/Audio/Sfx/*.wav` (16) | The Essential Retro Video Game Sound Effects Collection (512 sounds) | Juhani Junkala (SubspaceAudio) | CC0 1.0 | https://opengameart.org/content/512-sound-effects-8-bit-style | Les 16 fichiers sont identiques (MD5) à des fichiers du pack `assets/opengameart/512-sound-effects-8-bit-style` |
| `UI/Assets/Audio/Music/menu.ogg`, `work1.ogg`, `work2.ogg`, `crisis.ogg` | 4 Chiptunes (Adventure) : « Stage Select », « Stage 1 », « Stage 2 », « Boss Fight » | Juhani Junkala (SubspaceAudio) | CC0 1.0 | https://opengameart.org/content/4-chiptunes-adventure | Identiques (MD5) aux fichiers .ogg du pack `assets/opengameart/4-chiptunes-adventure` |

CC0 n'impose aucune mention : elle est faite par courtoisie.

## Bibliothèques livrées avec l'exécutable

| Bibliothèque | Licence | Usage |
|---|---|---|
| Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent 12.1.3 | MIT | Interface |
| Avalonia.Fonts.Inter 12.1.3 (police Inter, Rasmus Andersson) | Paquet MIT ; police Inter sous SIL Open Font License 1.1 | Police de repli chargée par `WithInterFont()` (`UI/Program.cs`) ; le jeu affiche ses propres polices pixel |
| SkiaSharp et HarfBuzzSharp (dépendances d'Avalonia) | MIT (Skia : BSD-3 ; HarfBuzz : Old MIT) | Rendu et mise en forme du texte |
| NAudio.WinMM 2.2.1 (Mark Heath) | MIT | Sortie audio |
| NAudio.Vorbis 1.5.0, NVorbis (Andrew Ward) | MIT | Lecture des musiques OGG |
| .NET 9 (runtime, si publié en autonome) | MIT | Exécution |

Provenance non vérifiée ici : les notices exactes des dépendances transitives (SkiaSharp, HarfBuzzSharp) sont à joindre au
moment de la publication (`dotnet list package --include-transitive`), avec le texte de chaque licence.

## Ce qui n'est pas livré

Les packs Kenney, 2dPig Pixel Office, opengameart (icônes, objets de bureau, portraits, ui-sounds-0, 5 Chiptunes Action) et
les polices Press Start 2P, Pixelify Sans, VT323 et Silkscreen sont dans `assets/` mais **ne sont pas embarqués** : ils ne
demandent donc aucune mention dans le jeu. Ne pas les ajouter à l'exécutable sans compléter ce fichier.
