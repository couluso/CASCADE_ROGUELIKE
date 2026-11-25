# CASCADE — prototype roguelike plinko (Auteur : Coulu)

## Installation (2 minutes)
1. Unity 2022.3 LTS ou Unity 6, projet **3D** (Built-in ou URP, les deux marchent). Ton projet actuel convient.
2. **Supprime l'ancien dossier `Assets/Cascade`**, puis copie le nouveau → `Scripts`, `Resources` (polices + shader), `Licenses`.
3. Menu **Tools > Cascade > Créer la scène CASCADE**, puis **Play**.
   (Ou : scène vide + GameObject vide + composant `CascadeBootstrap`.)

La borne 3D, la salle, les lumières, les sons et l'interface sont générés par code.
Pas besoin de TextMeshPro. Compatible ancien Input Manager et nouveau Input System.

### Rendu
- Borne d'arcade 3D : planche en feutre, pics chromés, rails en laiton, cadre laqué, fronton à ampoules qui défilent,
  bornes voisines et enseignes dans une salle sombre. Ombres douces + sonde de reflets pour le chrome.
- **Filtre moniteur d'arcade** plein écran (touche **V** pour l'activer / le couper) : courbure, lueur des éléments lumineux,
  masque RGB, lignes de balayage, très légère bavure VHS. Réglages : valeurs par défaut dans `Resources/Shaders/CascadeCRT.shader`.
- **HUD physique** : le score, les reliques et les charmes sont affichés sur deux tours d'affichage 3D de part et d'autre
  de la borne (compteurs LED 7/14 segments, voyants qui clignotent, ampoules). Choix des seuils et boutique : vitre devant la planche.
- Pics reconnaissables par **forme et couleur** : dôme rose (mult), pièce jaune (doré), anneau vert (bumper),
  losange cyan (glace), étoile orange (épineux), tête de clou chromée (standard).
- Polices : **Bungee** (enseigne), **VT323** (textes), **DSEG7/DSEG14** (afficheurs LED). Licence OFL, voir `Licenses/`.
- URP : pour les ombres du spot, active *Additional Lights > Cast Shadows* dans ton asset URP.
- Build : si les néons/ampoules ne brillent plus dans un build, ajoute le shader Standard (ou URP/Lit) à
  *Project Settings > Graphics > Always Included Shaders* (l'émission est activée par code).

## Contrôles
- Souris : viser (zone de lâcher en haut) · Clic gauche (ou Entrée/S) : lâcher
- A / D (ou flèches) : coup de pouce, 1 par bille
- Espace : secousse de table — 3 secousses rapprochées = **TILT** (bille perdue)
- V : filtre d'arcade on/off
- R / clic droit : relance (reprendre la bille avant qu'elle touche le 1er pic), 2 par manche
- F : frein (avec la relique Le Frein)

## Nouveautés HUD
- **Sac en 3D** : bocal en verre rempli des vraies billes. Glisser = tourner (les billes roulent), survol = effet, clic en boutique = retirer.
- **Bille de départ** : 9 standard + 1 spéciale au hasard (jamais la jumelle).
- **Boutique casino** : tapis de feutre ; reliques = cartes à jouer (valet ♣ commune, dame ♦ peu commune, roi ♥ rare, as ♠ légendaire),
  billes = jetons de casino, charmes = tickets de tombola, pack = carte à gratter.
- **Infobulles complètes** : un charme affiche aussi l'effet du pic qu'il crée ; survoler un pic de la planche affiche son effet.
- **Décompte façon Balatro** : jetons x mult, puis x case, puis bonus, puis total, avant d'entrer dans le score de la manche.
  Si la bille fait franchir la cible, les compteurs prennent feu (et s'embrasent si elle dépasse la cible à elle seule).

## Boucle de jeu
3 actes × (petit seuil, seuil moyen, boss) puis endless. 5 billes par manche, tirées du sac (10 billes au départ).
Score d'une bille = jetons (pics) × mult × valeur de la case × multiplicateurs finaux.
Tickets : 3/4/5 par seuil + 1 par bille non utilisée + intérêts (1 par 5 tickets, max 5).
Passer un petit/moyen seuil = étiquette bonus (Or, Charme, Boutique, Planche) mais pas de récompense.

## Contenu
- 7 types de pics : standard, multiplicateur, doré, bumper, glace, épineux (+ butoirs de mur)
- 8 billes : standard, lourde, légère, rebondissante, jumelle, feu, cristal, dorée · 3 éditions : Foil, Holo, Polychrome
- 20 reliques (économie, score, contrôle, familles Feu/Glace, 3 légendaires dont Le Jumeau)
- 10 charmes (pinceaux, marteau, sablier, clone, cadeau, pari, ralenti)
- 7 boss + boss final combiné (acte 3)
- Boutique : 2 reliques, 2 billes/charmes, pack mystère (1 relique parmi 3), reroll croissant, vente à moitié prix, retrait de billes, réordonner les reliques (l'ordre compte : Le Jumeau copie celle de droite)
- Juice : chaque pic joue une note (gamme pentatonique qui monte avec le mult), particules, tremblement, ralenti dramatique sur le dernier pic, paliers WOW / INCROYABLE / BROKEN

## Où équilibrer
- `Core/BoardLayout.cs` : géométrie de la planche, rebond, gravité, zone de lâcher, valeurs des cases
- `Core/Catalog.cs` : effets/prix des pics, billes, éditions, charmes, boss
- `Core/RunState.cs` : cibles de score par acte (`ActBase`), récompenses, intérêts, billes par manche
- `Items/Relics.cs` : chaque relique (ajoute une classe + une ligne dans `factories`)
- `Items/Shop.cs` : composition de la boutique, chances d'édition et de rareté

## Pas encore fait (pistes de la fiche)
Sceaux, éléments de planche (rails, portails, ventilateurs…), événements entre seuils, méta-progression et difficultés,
sauvegarde, Daily Drop / replays (la graine fixe tirages et boutiques, mais la physique Unity n'est pas garantie déterministe).

## Crédits
Voir `Licenses/CREDITS.md` : filtre VHS adapté du shader CC0 de *Vacation* (godotshaders.com), polices Bungee et VT323 (OFL).
