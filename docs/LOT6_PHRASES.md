# Traduction des phrases du jeu (panneaux, terminal, tutoriel, messages du moteur)

## Principe

Les menus et les barres utilisent des **clés** (`UI/Lang/fr.json`, `en.json`, `Loc.T("clé")`). Le reste du jeu (une dizaine de
milliers de lignes de panneaux, catalogues et messages du moteur) écrit ses phrases en français avec des nombres et des noms
insérés au milieu. Au lieu de remplacer chaque phrase par une clé, **la phrase française telle qu'elle s'affiche est la clé** :
`UI/Lang/phrases/<langue>/<zone>.json` associe « phrase française » → « phrase traduite ».

- Phrase exacte : `"Aucun projet": "No project"`.
- Phrase à trous : `{0}`, `{1}`… remplacent les morceaux variables (nombres, noms). Le même numéro peut être déplacé ou répété
  dans la traduction : `"{0} a terminé {1}": "{1} was finished by {0}"`. Un trou est traduit à son tour s'il est connu
  (`"Site vitrine"` seul dans le fichier traduit aussi le trou).
- Plusieurs lignes ou plusieurs phrases (séparées par `.`, `!`, `?` suivis d'une espace, ou `•`) : chaque ligne et chaque
  phrase est cherchée à part si le texte entier n'est pas connu.
- Le gabarit le plus précis (le plus de texte fixe) gagne. Un texte inconnu reste en français.
- La traduction est faite **à l'affichage** (`PixelText` : toute zone de texte, infobulle, texte dessiné). Les notifications et
  le journal déjà enregistrés dans une sauvegarde sont donc traduits eux aussi, et changer de langue ne modifie aucune
  sauvegarde. Les noms que le joueur a choisis ne sont jamais traduits.
- Code : `Loc.Translate`, `Loc.LooksFrench`, `Loc.RecordMisses` / `Loc.Misses` (relevé des textes d'apparence française non traduits).

## Vérifier la couverture

`UiTests/EnglishCoverage*.cs` : chaque zone liste ses catalogues (noms, descriptions) et ses messages, les passe par
`Loc.Translate` en anglais et échoue si un texte reste d'apparence française. `Loc.RecordMisses = true` + panneaux ouverts
donne la liste des textes oubliés.
