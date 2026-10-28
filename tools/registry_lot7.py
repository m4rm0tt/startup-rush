"""
Met à jour docs/asset_usage.csv avec les composants d'interface du pack branchés au lot 7 (voir docs/V1_ETAT.md).

« partiel » : affiché par le vrai jeu et vu dans les captures (UI.exe --capture), sans test automatisé d'interface. Aucune
ligne n'est marquée « validé » ici : la règle du registre demande un test qui vérifie l'action et l'état d'un contrôle.

    python tools/registry_lot7.py
"""
import csv
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY = os.path.join(ROOT, "docs", "asset_usage.csv")

SEEN = "vu dans les captures {} du vrai jeu ; pas de test automatisé d'interface"


def row(consumer, indicator, files, captures, unlock="toujours"):
    return dict(consommateur=consumer, fichier_definition=files, deblocage=unlock,
                indicateur_ou_interaction=indicator, etat="partiel", scenario_test=SEEN.format(captures))


UI_KIT = "UI/App.axaml;UI/Hud/Ui.cs;UI/Pixel/PixelPanel.cs"
SCREEN = "UI/Screens/GameScreen.cs;UI/Pixel/PixelPanel.cs"
SETTINGS = "UI/Screens/MenuScreens.cs;UI/Hud/Ui.cs"

updates = {}

# Boutons et choix
updates["hud_button_text"] = row("Boutons secondaires de tous les menus (−/+, Voir, Fiche…)", "action du bouton ; survol, appui, désactivé", UI_KIT, "09, 19, 34")
for i in ["hud_segmented_left", "hud_segmented_mid", "hud_segmented_right"]:
    updates[i] = row("Choix exclusifs (Ui.Choice) : objectif d'IA, horaires, rôle, public visé…", "valeur choisie (sarcelle) ; survol, désactivé", UI_KIT, "04, 19, 34")
updates["hud_button_icon"] = row("Barre du haut : pause, vitesses 1 à 4, journal, réglages", "vitesse active enfoncée (« toggled »)", SCREEN + ";UI/Hud/Ui.cs", "12, 19, 30")
updates["hud_button_icon_small"] = row("Barre de titre des fenêtres et modales : retour et fermer", "ferme ou remonte d'un panneau", SCREEN, "19, 22, 22c")

# Fenêtres, modales, notifications
updates["hud_window"] = row("Panneaux de jeu (projets, équipe, finances…) : titre dans la bande sarcelle", "titre et contexte du panneau", SCREEN, "13-19, 34-39")
updates["hud_modal"] = row("Modales titrées : menu de pause, confirmations", "titre, fermeture", SCREEN, "22, 22c")
updates["hud_confirm_dialog"] = row("Confirmations (licencier, fermer un site, embaucher…) : icône de gravité, Annuler puis l'action", "action confirmée ou annulée", SCREEN, "22c")
updates["hud_notification"] = row("Événements du journal en bas à gauche : bande et icône info, succès, attention, danger", "un clic ouvre la cause (Navigate)", SCREEN, "30, 37, 38")
updates["hud_toast"] = row("Retour d'une action réussie", "message bref, disparaît après 7 s", SCREEN, "05-11")
updates["hud_tooltip"] = row("Infobulles de toute l'interface (fond marine, texte clair)", "explication au survol", "UI/App.axaml", "survol non capturé ; style appliqué à toutes les infobulles")

# Barre du haut
updates["hud_topbar_bg"] = row("Barre du haut (fond marine)", "date, argent, vitesses, journal", SCREEN, "12, 30, 38")
updates["hud_topbar_segment"] = row("Barre du haut : groupes entreprise et date", "nom, site affiché, date et heure", SCREEN, "12, 30, 38")
updates["hud_topbar_divider"] = row("Barre du haut : séparateur entre les groupes", "lisibilité", SCREEN, "12, 30, 38")
updates["hud_money_chip"] = row("Barre du haut : trésorerie (puce avec la pièce)", "argent disponible, rouge si négatif", SCREEN, "12, 30, 38")
updates["hud_badge_count"] = row("Cloche « À traiter » : nombre de problèmes ouverts", "compte des avis non résolus", SCREEN, "30, 38")

# Contenu des panneaux
updates["hud_section_header"] = row("Titres de section de tous les panneaux", "structure du panneau", UI_KIT, "16, 19, 34, 39")
updates["hud_progress_track"] = row("Barres de progression (compétences, dépenses, revenus, studio, recherche)", "valeur affichée", UI_KIT, "16, 19, 39")
for state, where in [("default", "compétences, progression"), ("success", "revenus, confort"), ("warning", "studio, pause"), ("danger", "dépenses")]:
    updates[f"hud_progress_fill_{state}"] = row(f"Remplissage des barres ({where})", "couleur de sens doublée par le libellé et la valeur", UI_KIT, "16, 18c, 19, 39")
for state in ["neutral", "success", "warning", "danger", "info", "accent"]:
    updates[f"hud_badge_{state}"] = row("Étiquettes d'état (Ui.Tag) : type de projet, public, en service, pause…", "état lisible par le texte, la couleur le double", UI_KIT, "13, 34, 37")
updates["hud_field_text"] = row("Champs de saisie : nom du projet, nom de sauvegarde", "texte saisi ; survol, focus", UI_KIT, "04, 22")
for i in ["hud_scroll_track_v", "hud_scroll_thumb_v", "hud_scroll_grip_v", "hud_scroll_arrow_up", "hud_scroll_arrow_down"]:
    updates[i] = row("Barre de défilement verticale des panneaux longs", "défilement ; survol et appui", "UI/App.axaml;UI/Pixel/PackSkins.cs", "18c, 34, 37")

# Réglages
for i, what in [("hud_checkbox", "options d'accessibilité (l'entreprise attend, saisie simplifiée)"), ("hud_radio", "taille de l'interface"),
                ("hud_switch", "options de temps (grands menus, nuits accélérées)")]:
    updates[i] = row("Réglages : " + what, "réglage enregistré et appliqué", SETTINGS, "25b")
for i in ["hud_slider_track", "hud_slider_fill", "hud_slider_knob"]:
    updates[i] = row("Réglages : volumes de la musique et des bruitages", "volume appliqué pendant le glissement", SETTINGS + ";UI/App.axaml", "25b")
updates["hud_popover"] = row("Réglages : bulle de valeur à côté des glissières", "valeur en pour cent", SETTINGS, "25b")

# Onglets et graphiques
updates["hud_tabs"] = row("Onglets d'une fiche de projet (aperçu, composants, avis, réglages)", "onglet actif clair, les autres en retrait", UI_KIT + ";UI/Hud/Panels/ProjectPanel.cs", "09, 11, 11b")
for i in ["hud_chart_frame", "hud_chart_grid_h", "hud_chart_axis_h", "hud_chart_point", "hud_chart_legend"]:
    updates[i] = row("Courbes d'un projet (utilisateurs, revenus par jour) : cadre, grille, axe, légende, dernier point", "historique de 120 jours, maximum écrit",
                     "UI/Hud/LineChart.cs", "11b")
for card in ["comp_card"]:
    updates[card] = row("Atelier de composants : carte d'un composant, en-tête selon l'état (pas encore, en développement, en service, demandé, verrouillé)",
                        "état du composant écrit en toutes lettres", "UI/Hud/Panels/ProjectPanel.cs", "09")
for game, pack in {"ui": "comp_home", "performance": "comp_infrastructure", "support": "comp_support", "login": "comp_auth",
                   "notifications": "comp_notifications", "cloudsave": "comp_savegame", "dashboard": "comp_admin", "subscribe": "comp_subscription",
                   "search": "comp_search", "messaging": "comp_messaging", "export": "comp_integration", "stats": "comp_analytics",
                   "gameplay": "comp_engine", "content": "comp_progression", "leaderboard": "comp_leaderboard", "multiplayer": "comp_netplay",
                   "feed": "comp_feed", "profile": "comp_profile", "moderation": "comp_security", "listings": "comp_inventory",
                   "catalog": "comp_catalog", "checkout": "comp_payment", "reviews": "comp_reviews", "ai_core": "comp_processing",
                   "ai_feature": "comp_automation"}.items():
    updates[pack] = row(f"Atelier de composants : aperçu et pictogramme du composant « {game} » (mêmes effets que ce composant du jeu)",
                        "besoins couverts par le composant (Core/products/ProductCatalog.cs)", "UI/Hud/ComponentArt.cs;Core/products/ProductCatalog.cs", "09")
for icon in ["icon_comp_admin", "icon_comp_auth", "icon_comp_automation", "icon_comp_catalog", "icon_comp_engine", "icon_comp_feed", "icon_comp_infrastructure",
             "icon_comp_integration", "icon_comp_inventory", "icon_comp_leaderboard", "icon_comp_messaging", "icon_comp_netplay", "icon_comp_payment",
             "icon_comp_processing", "icon_comp_profile", "icon_comp_progression", "icon_comp_reviews"]:
    updates[icon] = row("Atelier de composants : pictogramme d'état de la carte du composant correspondant", "état du composant", "UI/Hud/ComponentArt.cs", "09")

# Listes, portraits, couleurs
updates["hud_list_row"] = row("Rangées de liste : journal (cliquables, ouvrent la cause) et sauvegardes à charger", "survol ; un clic ouvre l'élément",
                              UI_KIT + ";UI/Hud/Panels/NoticesPanel.cs;UI/Screens/MenuScreens.cs", "17b, 25")
updates["hud_portrait_frame"] = row("Portraits encadrés : équipe, candidats, fiche, inspecteur, nouvelle partie", "visage de la personne", UI_KIT + ";UI/Hud/Panels/TeamPanel.cs", "14, 24")
updates["hud_swatch_cell"] = row("Nouvelle partie : couleur des cheveux et de la peau (nuance de la rampe de la palette)", "apparence choisie (sans effet sur le jeu)",
                                 UI_KIT + ";UI/Screens/MenuScreens.cs", "24")
for icon in ["icon_success", "icon_warning", "icon_error"]:
    updates[icon] = row("Journal et notifications : sens d'un événement (succès, attention, problème)", "gravité lisible sans la couleur",
                        "UI/Hud/Panels/NoticesPanel.cs;" + SCREEN, "17b, 30")

# Icônes des boutons (règle commune de Ui.Button : le libellé donne l'icône) et des chiffres clés (Ui.Stat)
BUTTONS = "UI/Hud/Ui.cs (ButtonIcons)"
for icon, what in {"icon_save": "Sauvegarder", "icon_load": "Charger une partie", "icon_nav_menu": "Menu principal", "icon_cancel": "Annuler, Refuser",
                   "icon_confirm": "Valider", "icon_recruit": "Recruter", "icon_training": "Former, Entraîner un modèle", "icon_unlock": "Débloquer un nœud",
                   "icon_delete": "Licencier, Jeter une version", "icon_edit": "Renommer", "icon_loan": "Emprunter ; poste Remboursements",
                   "icon_debt": "Plan de redressement", "icon_publish": "Lancer un projet", "icon_rotate": "Tourner un objet", "icon_move": "Déplacer un objet",
                   "icon_packaging": "Ranger dans les cartons", "icon_place": "Poser", "icon_eye": "Fiche, Voir, Ouvrir la fiche",
                   "icon_manufacturing": "Production et stocks", "icon_stock": "Stock cible", "icon_storage": "Réserves d'un ordre",
                   "icon_refresh": "Essayer de récupérer une sauvegarde", "icon_team": "Déléguer", "icon_statistics": "Évaluer un modèle",
                   "icon_version": "Améliorer un composant", "icon_minus": "boutons −", "icon_chevron_left": "ordre des projets (plus prioritaire)",
                   "icon_chevron_up": "remonter"}.items():
    updates[icon] = row("Boutons : " + what, "action du bouton, même pictogramme partout", BUTTONS, "13, 16, 19, 22, 34 (même règle pour tous les boutons)")
for icon, what in {"icon_income": "Revenus ; revenus par projet", "icon_expense": "Dépenses", "icon_profit": "Résultat, Chaque mois", "icon_cpu": "Calcul (CPU)",
                   "icon_gpu": "GPU des sites", "icon_sell": "Ventes au total", "icon_salary": "poste Salaires", "icon_server": "poste Serveurs"}.items():
    updates[icon] = row("Chiffres clés et postes de dépense : " + what, "nature du chiffre", "UI/Hud/Ui.cs (StatIcons);UI/Hud/Panels/FinancePanel.cs", "16, 34")

# Terminal du fondateur
TERM = "UI/Hud/TerminalView.cs;UI/App.axaml"
for i, what in {"term_surface": "surface de texte", "term_titlebar": "titre (terminal · projet)", "term_input": "champ de saisie avec son chevron (normal, focus, désactivé)",
                "term_mark_ok": "mot juste dans le journal", "term_mark_error": "faute dans le journal (tapé ≠ attendu)",
                "term_mark_warning": "fin de session, pause du jeu", "term_mark_running": "tâche en cours (ligne d'état)",
                "term_progress_cap_left": "bout gauche de la barre de mots", "term_progress_cap_right": "bout droit de la barre de mots",
                "term_progress_half": "mot en cours de frappe dans la barre", "term_selection": "bande de la ligne de progression",
                "term_scroll_track": "défilement du journal", "term_scroll_thumb": "défilement du journal"}.items():
    updates[i] = row("Terminal du fondateur : " + what, "travail tapé, fautes, progression de la session", TERM, "05")
for i, label in {"term_cursor_block": "bloc (par défaut)", "term_cursor_bar": "barre", "term_cursor_underline": "souligné"}.items():
    updates[i] = row("Terminal : curseur au point atteint dans le mot, forme " + label + " (réglage Accessibilité, clignote sauf effets réduits)",
                     "position dans le mot à taper", TERM + ";UI/Screens/MenuScreens.cs;Core/persistence/LegacyProfile.cs", "05 (bloc), 25b (les trois au réglage)")

# Scène : cases, anneaux, particules, curseurs
SCENE = "UI/Scene/SceneEffects.cs;UI/Scene/SceneCanvas.cs"
for i, what, seen in [("vfx_tile_valid", "emprise d'un objet posable (pulse)", "18b"), ("vfx_tile_footprint", "cases d'accès à laisser libres", "18b"),
                      ("vfx_tile_invalid", "emprise d'un objet qui ne peut pas aller là", "dessiné par le même code que 18b, non capturé"),
                      ("vfx_tile_selected", "cases de l'objet sélectionné", "non capturé"), ("vfx_tile_hover", "case survolée hors aménagement (pulse)", "non capturé"),
                      ("vfx_select_ring", "anneau sous la personne sélectionnée", "12b"), ("vfx_hover_ring", "anneau sous la personne survolée (deux tailles)", "non capturé (survol souris)"),
                      ("vfx_work", "bouffée de travail au-dessus de qui travaille (toutes les 2 à 4 s)", "12b"), ("vfx_code", "bouffée quand la tâche est du code", "même code que 12b"),
                      ("vfx_idea", "idée au-dessus d'un chercheur", "même code que 12b"), ("vfx_heart_morale", "cœur quand le moral monte", "événement, non capturé"),
                      ("vfx_success", "coche quand une tâche se termine", "événement, non capturé"), ("vfx_coin", "pièces les jours avec des revenus", "événement, non capturé"),
                      ("vfx_sparkle_large", "étincelle quand un objet est posé", "événement, non capturé"), ("vfx_sparkle_small", "petites étincelles qui clignotent à la pose", "événement, non capturé"),
                      ("vfx_alert", "alerte qui pulse au-dessus de qui attend sans poste", "état, non capturé")]:
    updates[i] = dict(consommateur="Scène : " + what + " ; coupé ou figé par « réduire les effets »", fichier_definition=SCENE, deblocage="toujours",
                      indicateur_ou_interaction="retour visuel d'un état ou d'un événement réel (ne change rien à la simulation)", etat="partiel",
                      scenario_test=f"vu : {seen} ; pas de test automatisé d'interface")
for i, what in {"cursor_normal": "souris sur la scène", "cursor_select": "au-dessus d'une personne ou d'un objet cliquable",
                "cursor_place_valid": "objet posable sous la souris", "cursor_place_invalid": "objet impossible à poser ici",
                "cursor_move": "déplacement d'un objet"}.items():
    updates[i] = row("Curseur (export ×2 du pack) : " + what, "ce que ferait un clic", SCENE, "curseur système non visible dans les captures")

# Curseurs de l'interface et poignée du panneau
updates["cursor_action"] = row("Curseur au-dessus de tous les boutons (style global)", "ce qui se clique", "UI/App.axaml;UI/Pixel/PackSkins.cs", "curseur système non visible dans les captures")
updates["cursor_text"] = row("Curseur au-dessus des champs de saisie (style global)", "où l'on peut écrire", "UI/App.axaml;UI/Pixel/PackSkins.cs", "curseur système non visible dans les captures")
updates["cursor_resize_h"] = row("Curseur de la poignée du panneau latéral", "élargir ou rétrécir le panneau", SCREEN, "curseur système non visible dans les captures")
updates["hud_drag_handle"] = row("Poignée du panneau latéral : la tirer change sa largeur (retenue dans les réglages)", "largeur du panneau", SCREEN + ";Core/persistence/LegacyProfile.cs", "19")

# Objets informatiques du pack branchés au lot 7 : consommateur réel ET test d'effet → « validé »
OBJ_TEST = "Tests/space/Lot7ObjectsTest.cs : "
OBJ_FILES = "Core/space/Furniture.cs;Core/space/Layout.cs;Core/campaign/Campaign.Time.cs"
for i, (consumer, indicator, test) in {
    "laptop_gaming": ("Ordinateur d'un poste (portable)", "production ×1,05, qualité +0,03", "PeripheralsHelpTheirOwnPostUpToACapAndTheDockOnlyWithALaptop"),
    "mini_pc": ("Ordinateur d'un poste, posé sur le bureau", "production ×0,95, très sobre", "AMiniPcOnTheDeskIsThePostComputerAndTheBetterMachinesPayOff"),
    "workstation_creative": ("Ordinateur d'un poste (tour)", "production ×1,05, qualité +0,05", "AMiniPcOnTheDeskIsThePostComputerAndTheBetterMachinesPayOff (qualité comparée)"),
    "monitor_ultrawide": ("Écran d'un poste", "production +5 %, plafond des écrans 6 %", "TheUltrawideScreenHelpsWithinTheScreenCap"),
    "headset_stand": ("Périphérique d'un poste", "production +2 %, plafond 3 % par poste", "PeripheralsHelpTheirOwnPostUpToACapAndTheDockOnlyWithALaptop"),
    "docking_station": ("Périphérique d'un poste avec portable", "production +2 % seulement avec un portable", "PeripheralsHelpTheirOwnPostUpToACapAndTheDockOnlyWithALaptop"),
    "graphics_tablet": ("Périphérique d'un poste", "qualité du travail +0,02, plafond 0,03", "PeripheralsHelpTheirOwnPostUpToACapAndTheDockOnlyWithALaptop"),
    "webcam": ("Réunions à distance (sur un écran)", "coordination +0,25, petits supports plafonnés à 1", "CollaborationEquipmentAddsCoordinationWithinTheCaps"),
    "speakerphone": ("Réunions (sur une table de réunion)", "coordination +0,25, petits supports plafonnés à 1", "CollaborationEquipmentAddsCoordinationWithinTheCaps"),
    "printer_mfp": ("Documents et maquettes papier", "coordination +0,25, petits supports plafonnés à 1", "CollaborationEquipmentAddsCoordinationWithinTheCaps"),
    "videoconf_bar": ("Salle de visio (sur l'écran de présentation)", "coordination +0,5", "CollaborationEquipmentAddsCoordinationWithinTheCaps"),
    "console_test_kit": ("Banc de test des jeux", "15 % de défauts en moins à chaque amélioration d'un jeu", "TestBenchesRemoveDefectsOfTheirFamilyOnly"),
    "device_test_rack": ("Banc de test des applications", "15 % de défauts en moins à chaque amélioration d'une application", "TestBenchesRemoveDefectsOfTheirFamilyOnly"),
    "capture_device": ("Studio : fait office de caméra", "score vidéo du studio", "ACaptureBoxCountsAsTheStudioCamera"),
}.items():
    updates[i] = dict(consommateur=consumer, fichier_definition=OBJ_FILES, deblocage="arbre « Studio audiovisuel »" if i == "capture_device" else "toujours",
                      indicateur_ou_interaction=indicator, etat="validé", scenario_test=OBJ_TEST + test)

# Icônes de l'interface
for i, where in {"icon_pause": "bouton pause", "icon_speed_1": "vitesse 1", "icon_speed_2": "vitesse 2", "icon_speed_3": "vitesses 3 et 4 (avec le chiffre)",
                 "icon_bell": "journal / « À traiter »", "icon_settings": "menu et réglages", "icon_back": "retour d'un panneau", "icon_close": "fermer",
                 "icon_company": "groupe entreprise de la barre du haut", "icon_calendar": "groupe date de la barre du haut",
                 "icon_chevron_down": "section dépliée", "icon_chevron_right": "section repliée", "icon_info": "notification d'information",
                 "icon_help": "confirmation sans danger"}.items():
    updates[i] = row("Interface : " + where, "icône d'un contrôle ou d'un état", "UI/Pixel/Icons.cs;" + SCREEN, "12, 18c, 19, 22c")

with open(REGISTRY, encoding="utf-8", newline="") as f:
    reader = csv.DictReader(f)
    columns = reader.fieldnames
    rows = list(reader)
known = {r["id"] for r in rows}
missing = sorted(set(updates) - known)
assert not missing, f"IDs absents du registre : {missing}"
# Ne jamais rétrograder une ligne déjà validée
for r in rows:
    if r["id"] in updates and r["etat"] != "validé":
        r.update(updates[r["id"]])
with open(REGISTRY, "w", encoding="utf-8", newline="") as f:
    writer = csv.DictWriter(f, fieldnames=columns)
    writer.writeheader()
    writer.writerows(rows)
states = {}
for r in rows:
    states[r["etat"]] = states.get(r["etat"], 0) + 1
print(states)
