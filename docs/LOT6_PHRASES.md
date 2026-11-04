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

## Consignes pour traduire une zone

Contexte : Jeu « Startup Rush » (C#/.NET 9, Avalonia 12), dépôt /root/startup-rush, branche new-assets. Tu travailles dans ton worktree (copie isolée) ; toutes les commandes dotnet commencent par :
  export PATH=$PATH:/root/.dotnet DOTNET_ROOT=/root/.dotnet DOTNET_NOLOGO=1
Mission : traduction ANGLAISE complète d'une zone du jeu (le français reste la langue de référence et ne doit pas changer). Lis d'abord docs/LOT6_PHRASES.md (principe) et UI/Loc.cs (moteur Loc.Translate). La phrase française TELLE QU'ELLE S'AFFICHE est la clé ; tu écris des fichiers JSON dans UI/Lang/phrases/en/<ta zone>_*.json (plusieurs fichiers possibles, uniquement avec le préfixe de ta zone, pour ne jamais te chevaucher avec les autres agents). Format : {"phrase française": "English text"}, avec {0},{1}… pour les morceaux variables (nombres, noms, produits) : le gabarit est la phrase française rendue, où chaque morceau variable est remplacé par un trou numéroté dans l'ordre d'apparition. Exemple de code français `$"{p.Name} a terminé {n} tâches"` → clé "{0} a terminé {1} tâches" → "{0} finished {1} tasks". Une valeur interpolée qui vient d'un catalogue traduisible (nom de composant, de public, d'objet…) est traduite à son tour si ce nom est lui-même une entrée du JSON (ajoute-le).
Règles :
- Traduis TOUT texte destiné au joueur dans tes fichiers : libellés, titres, descriptions de catalogue, infobulles, messages d'erreur et de succès (CommandResult), notifications, états vides, confirmations, étapes de tutoriel, textes conditionnels (chaque branche d'un ternaire = une entrée), fragments assemblés. Ne traduis PAS : identifiants internes, clés de sauvegarde, noms de fichiers, noms propres choisis par le joueur, logs de développement, code des bots (Core/bots) qui n'est pas affiché.
- Anglais naturel de jeu de gestion (ton clair et concis, « CHF » et nombres inchangés, pas de mot-à-mot). Garde la ponctuation de mise en forme (retours à la ligne, •, :). Les trous {n} doivent être les mêmes (mêmes numéros) dans la phrase française et dans la traduction.
- Si une phrase est assemblée de morceaux qui ne peuvent pas être retrouvés (ex. « A, B et C » construits dynamiquement, ou une première lettre mise en majuscule par le code), fais une MODIFICATION MINIMALE du code pour que chaque morceau soit une phrase entière traduisible (ou appelle Loc.Translate sur le morceau). N'ajoute pas de fonctionnalité, ne refais pas de mise en page (la taille du texte passe à 18 px : la mise en page sera traitée par ailleurs).
- Les tests existants, qui attendent le français, doivent continuer de passer : ne change aucun texte français sauf nécessité absolue (alors mets les tests à jour).
- Ajoute pour ta zone un test NUnit dans UiTests/EnglishCoverage_<zone>Test.cs (fichier à toi seul) qui, avec Loc.Language="en" (remettre "fr" après), liste par code les textes de ta zone et vérifie que chacun est traduit (Loc.Translate(x) != x) ou que Loc.LooksFrench(traduction) est faux ; pour les messages dynamiques, déclenche-les (commandes de la campagne, ouverture des panneaux via UiTests/PanelsTest.Advanced(), Loc.RecordMisses = true puis Loc.Misses vide). Pour les catalogues du moteur, itère les catalogues réels (pas une copie de la liste). Utilise de vrais scénarios plutôt que des listes recopiées : c'est ce qui prouve la couverture.
- Moyen de relever les oublis dynamiques : Loc.RecordMisses=true, ouvrir les écrans/panneaux/commandes, lire Loc.Misses.
- Teste avec : dotnet test UiTests/UiTests.csproj --filter "FullyQualifiedName~<ton test>" (et Tests/Tests.csproj pour le moteur si tu touches Core). N'exécute JAMAIS le rapport d'équilibrage ni la suite complète. N'utilise JAMAIS pkill/killall. Un autre processus lourd tourne sur la machine : sois patient.
- Fais des commits cohérents dans ton worktree (git add -A ; git commit), message en français, terminé par les deux lignes :
  Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01AUc5BVHdaAMjMzh5qqakxT
  Ne pousse pas. Ne touche pas aux fichiers hors de ta zone, hors fichiers JSON/tests prévus (sauf les retouches minimales de code décrites).
- Termine par un rapport court : nombre d'entrées, fichiers modifiés, ce qui reste non traduit et pourquoi, résultats des tests.
