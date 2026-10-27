# Prompt Claude — StartupRush : améliorer le jeu existant, intégrer les assets, équilibrer

Tu es mon développeur C#/Avalonia, game designer et directeur artistique pixel art. Ta mission est d'améliorer **mon jeu StartupRush existant**, pas de le recréer de zéro. Travaille dans le cloud, intègre réellement les assets que je fournis dans le dossier `assets`, corrige les défauts, améliore l'équilibrage et livre une version PC compilée et jouable.

## 1. Sources et périmètre

Lis d'abord :
1. Le PDF **StartupRush - Concept du jeu.pdf**, y compris les captures des huit pages, pas seulement le texte.
2. Les instructions du dépôt, sa solution, ses projets, son README, les éventuels AGENTS.md, global.json et les tests existants.
3. Le catalogue `CATALOGUE_ASSETS_STARTUPRUSH.md`, `ASSET_PACKS.json`, `ASSET_INVENTORY.csv`, `README_ASSETS.md` et les fichiers `SOURCE.json`/licences des packs dans `assets/`.

Je confirme : conserver le jeu **PC en C#/Avalonia** et améliorer l'existant. Pas de port Android, pas d'Unity/Godot, pas de site, pas de PWA et pas de réécriture injustifiée. Le PDF décrit .NET 9 ; vérifie le SDK réellement ciblé par le dépôt avant toute décision. Si une migration est indispensable, explique-la et conserve un chemin de construction reproductible.

Le code décrit ce qui fonctionne effectivement ; le PDF décrit la vision de référence. Signale les écarts importants et les régressions sans inventer ce que tu n'as pas vu. Ne supprime pas une fonctionnalité valide parce qu'elle n'apparaît pas dans le PDF.

Le dossier fourni contient des bibliothèques de ressources, pas un habillage final prêt à poser partout. Il faut choisir, découper, harmoniser et intégrer les éléments pertinents. Les fichiers de prévisualisation ne sont pas des scènes de jeu à coller telles quelles.

## 2. Développement cloud et GitHub en mon nom

- Toutes les opérations de développement, installation du SDK, compilation et vérification doivent se dérouler dans ton environnement cloud. Ne me demande pas d'installer un environnement de développement local.
- Vérifie immédiatement l'accès réel au projet et à l'exécution de commandes. Si tu ne peux pas exécuter de code ou accéder au dépôt, annonce ce blocage précisément ; ne prétends jamais avoir compilé ou testé.
- Utilise le dépôt GitHub du jeu que j'ai connecté/autorisé. Demande son URL si elle manque ; ne devine ni le nom ni le propriétaire et ne crée pas un projet concurrent sans accord.
- Crée une branche de travail adaptée à l'état du dépôt. Garde mes changements existants et mes sauvegardes. Pas de reset destructeur, de suppression massive ou de force-push.
- Fais des commits utiles et pousse-les régulièrement. **Attribue les commits à mon identité Git vérifiée**, pas à Claude : réutilise la configuration correcte du dépôt ou demande mon nom d'auteur et mon adresse GitHub/noreply. Ne fabrique pas l'adresse depuis mon pseudo. Configuration locale au dépôt, jamais globale.
- Utilise la connexion et les secrets disponibles sans me demander un mot de passe/token dans le chat. Pas de secret dans les logs ou Git.
- Si nécessaire, utilise une CI cloud/GitHub Actions pour compiler le projet et produire l'archive PC, dans les limites gratuites disponibles, sans activer de facturation.
- Le système d'exploitation/architecture de la version PC doit être confirmé à partir du projet ou d'une question courte si c'est ambigu. Publie un exécutable adapté ; si l'environnement cloud est Linux, ne présente pas son lancement comme un test Windows.

## 3. Vision du jeu à préserver

StartupRush est un roguelike de gestion en pixel art. On part développeur fauché avec **150 CHF et un vieux portable**, dans une chambre miteuse. On travaille, accepte des contrats, paie les factures, améliore son matériel, déménage, recrute, fonde des entreprises et investit jusqu'à devenir Roi du monde.

Référence des règles décrites dans le PDF :
- Une run vise environ **30 à 40 minutes**.
- Un mois dure **60 secondes de temps de jeu actif**.
- Deux factures impayées consécutives provoquent la faillite/permadeath de la run.
- Les contrats proposés arrivent à gauche et expirent. Les contrats acceptés progressent à droite selon la vitesse de travail ; certains ont une échéance.
- Clic sur la scène ou Espace : travail supplémentaire et petit gain, avec critiques selon les règles existantes.
- Fin de mois : loyer, abonnements, salaires puis impôt sur la fortune selon la progression.
- Boutique sous la scène : Matériel, Maison, Équipe, Empire, Bourse.
- Chaque achat pertinent doit se voir dans la pièce, pas seulement dans une statistique.

Statuts : **Fauché → Freelance (3 000 CHF) → Fondateur (création d'entreprise) → Patron (100 000 CHF et employés) → Investisseur (5 millions, bourse) → Magnat (100 millions) → Roi du monde (1 milliard et holding)**.

Ne confonds pas argent disponible, patrimoine et revenus. Examine les définitions du code ; ne modifie pas silencieusement le sens des seuils. La holding est une condition de victoire réelle.

Couche roguelike :
- Choisir un atout parmi trois à chaque trimestre et nouveau statut ; raretés commune, rare, épique, légendaire, et malédictions.
- Relance payante et passage contre de l'argent.
- Crises annoncées, tous les trois mois à partir du mois 6, durant un mois.
- Événements avec choix risqué et dernière option sûre.
- Quatre fondateurs : Alex le dev fauché, Inès la hackeuse, Hugo l'école de commerce, Charles-Henri le fils à papa ; bonus/malus distincts.
- Pression de 0 à 3 et méta-progression par lingots/Héritage, avec personnages déblocables.

## 4. Audit initial bref, puis action

Compile la version actuelle, lance les tests disponibles et inspecte la structure. Identifie : séparation simulation/UI, source des valeurs d'équilibrage, sauvegardes, chargement d'images, rendu pixel, musique, contrôle du temps, déterminisme et prise en charge des résolutions.

Rends un état initial court avec les défauts confirmés et un plan concret. Une fois les accès essentiels obtenus, exécute le travail autorisé sans me redemander une validation pour chaque correction courante.

Ne bloque pas tout sur un détail de design. Pour les choix encore ouverts, présente-les comme propositions et pose uniquement les questions qui changent réellement la direction du travail. N'invente pas mes réponses ou des objectifs chiffrés d'équilibrage que je n'ai pas choisis.

## 5. Direction artistique précise

La DA vient du PDF : **vrai pixel art basse résolution, Endesga 32, tramage rétro, interface sombre et accents chauds**, personnages et objets lisibles. Ce n'est pas la DA noire/violette/liquide d'un autre projet : ne mélange pas les cahiers des charges.

La scène centrale des captures montre le héros **de dos devant son bureau**, avec une vue frontale de la pièce. Préserve cette mise en scène et sa progression visible. Ne transforme pas le jeu en RPG top-down simplement parce que certains packs sont top-down.

- Choisis une grille logique cohérente après inspection du rendu existant ; documente-la.
- Redimensionnement au plus proche voisin, coordonnées visuelles alignées sur la grille et absence de lissage flou des sprites. Respecte les facteurs d'échelle utiles aux résolutions cibles.
- Harmonise les ressources à Endesga 32 au besoin, avec retouches contrôlées. Ne remappe pas aveuglément les ombres au point de rendre les objets illisibles.
- Garde une densité de pixels, une lumière, des contours et une perspective cohérents. Évite un bureau 16 px à côté d'un personnage beaucoup plus détaillé sans adaptation.
- Distingue la scène pixel et le texte du HUD : ne rends pas les petits textes illisibles pour obtenir artificiellement un effet rétro.
- Palette d'interface, dimensions, hiérarchie, bordures, marges et raretés doivent rester cohérentes sur tous les écrans.
- Les objets génériques doivent être adaptés au ton startup contemporain : aucune épée, potion, armure ou coiffure fantasy ajoutée sans raison.
- N'intègre pas les logos d'éditeurs, watermarks, captures de démonstration ni slogans promotionnels des packs dans la scène. Les marques ne sont pas automatiquement autorisées par la licence d'un dessin.

## 6. Intégration obligatoire du dossier assets

**Je vais déposer les ressources dans `assets` à la racine du dépôt. Tu dois réellement les utiliser dans le jeu.** N'écris pas un guide qui me laisse faire l'import ou les raccordements moi-même.

Procédure :
1. Inspecte récursivement les fichiers existants, leur format, les dimensions, la transparence, les sources Aseprite, les polices, sons, licences et métadonnées de grille.
2. Préserve les originaux et leurs licences. Travaille sur des dérivés dans un sous-dossier séparé, ou utilise les atlas originaux avec des rectangles source correctement définis.
3. Établis un manifeste d'intégration : identifiant métier → fichier/atlas → rectangle source → ancrage → éventuelles frames/durée → rôle dans le jeu → provenance/licence.
4. Déduis les tailles de tuiles, espacements et marges depuis les fichiers réels et `tilesheetInfo.txt`/équivalents ; ne suppose pas que tout utilise une grille 16×16 sans marge. Une planche peut contenir plusieurs centaines de sprites sans autant de PNG séparés.
5. Configure correctement les ressources Avalonia, URI et chemins, avec attention à la casse sur le cloud Linux et aux différences `assets`/`Assets`. Aucun chemin absolu de ton environnement dans le jeu livré.
6. Charge et mets en cache les images, fonts et sons. N'ouvre pas un fichier et ne recrée pas un bitmap à chaque frame/clic.
7. Remplace effectivement les placeholders quand une ressource appropriée est disponible. Les éléments non adaptés restent en réserve, avec explication ; ne force pas tous les packs sur un même écran.
8. Produit `ASSET_USAGE.md` avec les éléments utilisés, modifiés, non utilisés et manquants, et un `THIRD_PARTY_NOTICES.md` fidèle aux ressources effectivement redistribuées.

Packs prioritaires fournis :
- **2dPig Pixel Office** : mobilier, ordinateurs, employés, petits accessoires et animaux, avec fichiers Aseprite. C'est une base thématique ; la perspective et la palette doivent être adaptées à la scène du jeu.
- **Kenney Pixel UI Pack** : panneaux, boutons, états et éléments 9-slice ; privilégie une même famille de bordures pour tout le HUD.
- **Kenney Roguelike Indoors** : bibliothèque de sols, murs et accessoires ; puise les éléments compatibles ou redessine leur projection, pas de changement de caméra automatique.
- **Kenney Roguelike Modern City / Tiny City / Tiny Town** : matériaux pour les vues extérieures et progression des logements ; réserve les vues de dessus à un usage cohérent, ne les colle pas telles quelles dans une fenêtre frontale.
- **Office objects / Office Stuff** : compléments bureau, tableaux et affiches.
- **Old Computer Hardware** : composants et icônes de matériel rétro, sans logos réels inutiles.
- **Input Prompts Pixel / 1-Bit** : raccourcis clavier/souris dans un style pixel cohérent. Le pack générique est une alternative, pas la priorité artistique.
- **Kenney Game Icons et extension / Document Icons** : répertoire pour contrats, factures et actions ; sélectionner et adapter, ne pas mélanger les variantes vectorielles/grandes avec les sprites basse résolution.
- **512 Sound Effects + UI Audio / Digital Audio / Interface Sounds / Casino Audio** : banque de bruitages à choisir par usage.
- **5 Chiptunes Action + 4 Chiptunes Adventure** : neuf compositions en réserve, avec formats/variantes parfois doublés ; auditionne pour choisir un ton adapté au travail, aux crises et aux fins de run.
- **Polices** : Press Start 2P pour titres courts, VT323 ou une autre police lisible fournie pour corps/chiffres ; Silkscreen, Pixelify Sans et Kenney Fonts sont des alternatives. Vérifie les accents français, le symbole monétaire utilisé et le rendu réel sous Avalonia.

Les quatre fondateurs exacts, le héros vu de dos, les animations de frappe, le mur d'écrans, l'ordinateur quantique, la holding et la vue d'île privée ne sont **pas garantis comme sprites prêts à l'emploi** dans les packs. Inventorie ces manques. Crée ou adapte des sprites originaux cohérents à partir de ressources dont la licence le permet, avec sources éditables ou génération déterministe documentée. Ne prétends pas avoir trouvé ces éléments sous un faux nom de fichier. Ne récupère pas d'images d'un jeu commercial.

## 7. Licences et diffusion commerciale

Les packs fournis sont déclarés CC0 sur leurs pages sources, à l'exception des polices Google Fonts sous OFL-1.1. Consulte les licences réellement présentes, y compris celles de fichiers inclus, avant distribution.

- Gratuit à télécharger ne signifie pas automatiquement libre : pas de pack NC, usage personnel uniquement, démo restrictive, licence absente ou ambiguë.
- Tu peux utiliser/modifier les éléments CC0 dans le jeu commercial. Conserve la provenance pour pouvoir retracer les droits et les versions.
- Pour les polices OFL, garde les avis de copyright et l'OFL avec la distribution. Ne vends pas une police isolément ; respecte les noms réservés et conditions de modification le cas échéant.
- Ne remplace jamais une licence OFL par CC0 et ne dis pas que tout le jeu doit devenir open source parce qu'il contient une police OFL ou des images CC0.
- Si tu ajoutes une ressource externe, justifie sa nécessité, conserve son URL, auteur, licence, obligations et fichier de licence. Priorité aux ressources déjà fournies et CC0. En cas de doute, ne l'intègre pas à la version distribuée.
- Les notices de l'archive sont un inventaire de recherche ; ton rapport final doit correspondre au contenu réel du build.

## 8. Mise en scène et transformations visibles

Construis un décor en couches : extérieur, fenêtre/murs, sol, bureau, matériel, héros, employés/accessoires, éclairage et effets. Lie les variations visuelles aux données existantes de la run.

Prévois une couverture explicite des achats : ordinateur, nombre et qualité des écrans, chaise, café, Internet, logements, locaux, employés et bonus particuliers comme le chat. Les montées en gamme doivent être perceptibles à taille normale sans lire la boutique.

Le héros travaille et réagit : frappe, critique, contrat terminé, paiement et inquiétude lors d'une crise. Animation légère, sans déplacer toute l'UI à chaque clic. Les employés au bureau reflètent l'équipe réellement recrutée ; ne montre pas des employés inexistants et conserve une lecture claire si les effectifs dépassent les places dessinables.

Menu titre : chambre de nuit sous la pluie avec animation discrète. Les changements de logement modifient ambiance/vue et coût, pas uniquement le texte du bouton. Les crises utilisent un bandeau, un effet visuel bref et des indications lisibles ; les valeurs impactées restent compréhensibles.

## 9. Écrans et ergonomie

Conserve les écrans du PDF : titre, choix du fondateur, Héritage, intro avec graine/pression, tirage d'atout, HUD de run, pause, boutique, crises, événements et fin de run.

HUD : portrait/statut/progression, argent et patrimoine, mois, prochaine facture/montant/compte à rebours. Contrats proposés à gauche, scène centrale, boutique dessous, en cours/atouts/effets/journal à droite. Améliore le contraste, les tailles et la hiérarchie ; évite les grands panneaux quasi vides ou les textes minuscules.

Clavier : Espace travailler ; 1–4 pour les choix réellement disponibles ; R relance ; Entrée continue ; Échap pause/retour ; F11 plein écran. Tout doit aussi fonctionner à la souris.

Les fenêtres de choix mettent la simulation en pause comme décrit. Les boutons masqués/désactivés ne doivent pas intercepter des actions involontaires. Ne laisse pas Espace déclencher à la fois le travail et un bouton ayant le focus. Définis le comportement d'appui répété/maintien pour qu'il ne dépende pas du taux de répétition du clavier.

Teste la lisibilité à une petite résolution PC raisonnable, en 1080p et sous mise à l'échelle système. Ne coupe ni les cartes ni les actions essentielles. Une fenêtre redimensionnée conserve une scène nette et des contrôles accessibles.

## 10. Audio et sensations

Intègre des sons distincts et dosés : travail, critique, contrat reçu/accepté/terminé, achat, choix/relance d'atout, facture, statut, crise, victoire et faillite. Les chiffres d'argent qui jaillissent, particules et secousses doivent renforcer l'action sans masquer les informations.

Auditionne les fichiers réels. N'utilise pas tous les bruitages au hasard ni une longue musique d'action sans vérifier qu'elle supporte une run de 30–40 minutes. Limite la polyphonie, évite l'écrêtage et les clics audio, vérifie les boucles et transitions.

Conserve les réglages musique/sons existants et ajoute des volumes séparés si utile, sans transformer la pause en usine à gaz. Les réglages persistent. Prévois de réduire les secousses/flashs et n'utilise pas de clignotements agressifs.

## 11. Équilibrage : corriger la facilité sans tricher

Le PDF indique qu'un bot gagne toutes ses runs, même à la pression maximale. Vérifie cette observation sur le code disponible avant de conclure ; c'est une faiblesse à examiner, pas une preuve déjà reproduite dans ton environnement.

- Établis d'abord une référence reproductible : graines fixes, personnage, pression, état de l'Héritage, stratégie et règles d'entrée connues.
- Compare plusieurs politiques : prudente, matériel, équipe, empire/passif, clic et mixte, plutôt qu'un seul bot réputé optimal. Simule de vraies contraintes de clic/input ; pas de ressources gratuites accordées au bot.
- Mesure taux de victoire, durée médiane, causes de faillite, mois des seuils, revenus/dépenses, effet des crises et dispersion des résultats. Sépare débutant sans Héritage et joueur avancé.
- Identifie les sources de facilité : intérêts exponentiels, empilements d'atouts, coût de passage/reroll exploitable, inflation des revenus passifs, employés toujours rentables, dépenses trop faibles, seuils sans contraintes ou bugs de simulation.
- Ajuste de manière ciblée. La pression 0 doit rester accueillante ; la pression 3 doit demander des choix et tolérer l'échec. Ne fixe pas arbitrairement un taux de victoire « idéal » que je n'ai pas validé et ne rends pas les pertes inévitables par un tirage caché.
- Les choix de build doivent rester intéressants et les critiques/chances influentes sans décider seules de la run. Les revenus passifs doivent faire sentir la montée en puissance sans supprimer la gestion.
- La méta-progression ne doit pas devenir obligatoire pour atteindre une première victoire ; vérifie ce point plutôt que de le supposer.
- Publie un tableau avant/après et explique les changements de valeurs. Sépare clairement modifications visuelles et règles d'équilibrage dans les commits et le journal.

## 12. Fiabilité de la simulation et sauvegardes

Vérifie les risques concrets :
- Argent/transactions cohérents, arrondis explicites, valeur finie et pas de NaN/overflow à grande progression. Utilise les types adaptés après inspection ; une correction ne doit pas changer silencieusement toutes les sauvegardes.
- Une facture appliquée une seule fois par mois ; remise à zéro correcte du compteur d'impayés après règlement selon les règles établies.
- Temps de simulation indépendant du framerate, pause fiable, gestion de plusieurs mois si une mise à jour doit les traverser, et pas de gain involontaire en arrière-plan ou à la réouverture.
- Contrats expirés, deadlines, multiplications de récompense et tâches parallèles correctement traités ; pas de paiement du même contrat deux fois.
- Atouts et crises appliqués/retirés sans effet permanent accidentel. Tirage reproductible avec la graine, y compris après reprise si prise en charge.
- Victoire : patrimoine/seuil pertinent ET holding selon la définition validée ; défaite et récompenses de lingots accordées une seule fois.
- Sauvegardes persistantes de méta-progression et réglages conservées, migrations et écriture atomique. Ne supprime pas les sauvegardes du joueur pour simplifier le développement.
- Règles de pause/reprise à documenter si le projet les laisse ambiguës ; ne rajoute pas un système idle hors ligne non demandé.

## 13. Validation réelle

Exécute les tests adaptés aux changements : calcul de factures/impayés, transitions de statut, conditions de fin, atouts/effets temporaires, dépenses/revenus, sauvegardes et déterminisme. Vérifie la non-régression des fonctionnalités existantes.

Lance l'application dans un affichage cloud/virtuel si disponible et inspecte les écrans. Montre des captures réellement produites : début de run, stade avancé, choix d'atout et crise/fin de partie. Aucune image marketing générée ne remplace un contrôle du rendu du jeu.

Vérifie le chargement de chaque ressource utilisée depuis l'archive publiée et pas seulement depuis le dossier de travail. Contrôle les rectangles d'atlas, le fond transparent, les caractères français, les clics et les volumes. Teste le build extrait dans un dossier propre sans SDK quand il est présenté comme autonome.

Si tu n'as pas pu écouter l'audio, lancer l'UI ou tester l'OS cible, indique exactement la limite. Des tests de logique seuls ne prouvent pas que les sons fonctionnent ou que le rendu est correct.

## 14. Ordre de réalisation et livrables

1. Audit, baseline et état du dépôt.
2. Inventaire/licences et manifeste de correspondance des assets.
3. Une scène de début de run réellement habillée, jouable et cohérente.
4. Progression visuelle, HUD/cartes/menus, feedback et sons.
5. Corrections de simulation et campagne d'équilibrage mesurée.
6. Contrôle visuel, tests et publication de la version PC.

Ne t'arrête pas à l'étape 2 en me donnant des instructions à exécuter. Le travail attendu est l'intégration et l'amélioration réalisées dans le code.

Livre :
- Code modifié, commits et push sur mon dépôt avec mon identité Git vérifiée.
- Archive PC exécutable pour la cible confirmée, accompagnée de sa version et de son SHA-256. Si .NET doit être installé par le joueur, indique-le ; privilégie un build autonome quand le projet et la cible le permettent.
- Catalogue d'utilisation des ressources, licences/credits inclus dans la distribution et liste des compléments originaux créés.
- Rapport avant/après de l'équilibrage avec protocole, nombre de runs, graines et résultats réellement obtenus.
- README français : lancer le jeu, contrôles, sauvegardes, volumes, construction cloud et limites restantes.
- Résumé final bref : changements concrets, validations, lien réel de l'archive, dépôt/branche/dernier SHA, limitations.

Commence par lire les sources et vérifier l'environnement cloud/GitHub, puis mène le projet jusqu'à une version améliorée réellement compilée. Si une dépendance ou un accès est bloquant, nomme ce blocage, conserve le travail effectué et ne fabrique jamais une réussite.
