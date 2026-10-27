# Portage C# du compositeur de référence

`StartupRushReference/Program.cs` (un seul fichier, aucune dépendance NuGet) montre en C# tout l'assemblage décrit
dans `INTEGRATION_GUIDE.md` : lecture de `manifest.json`, décodage/encodage PNG (ZLibStream), projection,
tri topologique des emprises, couches `base`/`glass`/`front`, couche d'ombre commune (max puis 30 %), sockets,
postes de travail et personnages en couches, modules de baie, recoloration par masque et motifs muraux.

```bash
dotnet build -c Release -o bin
bin/StartupRushReference <racine_du_pack> room_office_medium sortie.png previews/rooms/room_office_medium.png
```

Le dernier argument (facultatif) compare le rendu C# au rendu Python : `tools/csharp_check.py` le fait pour les
10 pièces (résultat dans `build_cache/qa_csharp.json`). Ce n'est pas du code Avalonia : c'est une référence
vérifiée à transposer dans votre boucle de rendu (par exemple vers `WriteableBitmap`, en BGRA prémultiplié).
