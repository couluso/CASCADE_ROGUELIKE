# CASCADE — prototype roguelike plinko (Auteur : Coulu)

## Installation
1. Unity 6 (projet actuel : 6000.6, URP). Le projet utilise le nouvel Input System.
2. Menu **Tools > Cascade > Construire la scène CASCADE**, puis **Play**.

## Organisation de la scène
La scène `Assets/Cascade/Cascade.unity` contient tout le jeu, visible et modifiable dans la hiérarchie :
- **CameraRig / Main Camera** : la caméra (son cadrage suit la taille de la fenêtre, voir `Arcade.Fit`).
- **Arcade** : la borne, les deux tours d'affichage (et leurs écrans), la salle de casino, les lumières.
  `Room/Crowd` : rangées de machines, joueurs assis et passants qui flânent au hasard dans les allées (animés par `Crowd`).
- **Board** : murs, cases et pics (`Pegs/Peg_rangée_colonne`). Le type des pics vient de la run ; on peut déplacer un pic.
- **Launcher** : rail, chariot (va-et-vient automatique, vitesse `sweepSpeed` dans l'inspecteur), bille suivante.
- **UI** (canvas écran, `GameUI`) et **WorldCanvas** (étiquettes des cases, popups).
- **GameManager** (graine fixe dans l'inspecteur), **Juice** (sons, particules), **VhsFeed** (filtre), **BagView** (bocal du sac).
  Filtre VHS : réglage **Intensity** sur l'objet VhsFeed (0 = image nette, 1 = effet complet ; 0,45 par défaut).

Les objets sont reliés dans l'inspecteur. L'outil ne sert qu'à (re)créer la scène : relancé, il écrase les retouches.
Les matériaux sont des assets (`Resources/Materials`, retouchables), les matériaux physiques sont dans `Physics/`.
Encore construit par code au lancement : le contenu des panneaux d'interface (boutons, listes), les billes, les particules et les sons.

### Rendu
- Borne d'arcade 3D : planche en feutre, pics chromés, rails en laiton, cadre laqué, fronton à ampoules qui défilent,
  au milieu d'une salle de casino (machines à sous, bornes, joueurs, passants, enseignes néon, tapis, plafond lumineux). Ombres douces + sonde de reflets pour le chrome.
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
- Le lanceur va et vient tout seul · Clic gauche ou Espace (ou Entrée) : lâcher la bille (pas de trajectoire fantôme), même pendant qu'une autre tombe encore
- A / D (ou flèches) : coup de pouce, 1 par bille
- **R** : secousse de table — 3 secousses rapprochées = **TILT** (bille perdue).
  Quand une bille reste coincée, une alerte « BILLE COINCÉE ! Secoue avec [R] » s'affiche avec la jauge de TILT en dessous
  (rouge clignotant quand la prochaine secousse ferait TILT) jusqu'à ce que la bille se décoince ; au bout de 12 s elle est libérée d'office.
- V : filtre d'arcade on/off
- E / clic droit : relance (reprendre la bille avant qu'elle touche le 1er pic), 2 par manche
- F : frein (avec la relique Le Frein)

## Nouveautés HUD
- **Sac en 3D** : bocal en verre gris fumé (paroi bien visible, reflets, étiquette) rempli des vraies billes. Glisser = tourner (les billes roulent), survol = effet, clic en boutique = retirer.
- **Bille de départ** : 9 standard + 1 spéciale au hasard (jamais la jumelle).
- **Reliques = pendentifs** : chaque relique a sa forme, son émail et son symbole (`UI/RelicArt.cs`) ; le métal dit la rareté
  (bronze commune, argent peu commune, or rare, or rose légendaire). Accrochés à une tringle, ils se balancent ; une nouvelle relique
  tombe et rebondit avec des étincelles et un carillon, et chaque déclenchement fait flasher et osciller son pendentif.
- **Boutique** : laque sombre et laiton, deux rayons (Vitrine : reliques dans leur écrin et pack ; Comptoir : billes et charmes),
  étiquettes de prix en laiton. Les billes à vendre sont dessinées telles quelles (couleur, taille, édition).
- **Infobulles complètes** : un charme affiche aussi l'effet du pic qu'il crée ; survoler un pic de la planche affiche son effet.
- **Décompte façon Balatro** : jetons x mult, puis x case, puis bonus, puis total, avant d'entrer dans le score de la manche.
  Si une bille ÉCLATE la cible à elle seule (au moins 1,5x), les compteurs prennent feu (et s'embrasent à 3x).
- **Rebonds de plus en plus intenses** : plus le score du lancer monte, plus les notes montent, plus il y a de particules
  et plus la caméra tremble.

## Boucle de jeu
3 actes × (petit seuil, seuil moyen, boss) puis endless. 5 billes par manche, tirées du sac (10 billes au départ).
Score d'une bille = jetons (pics) × mult × valeur de la case × multiplicateurs finaux.
**La manche se joue jusqu'à la dernière bille** : atteindre la cible ne l'arrête plus, on continue pour faire du surplus.
Après la dernière bille : score ≥ cible = seuil passé, sinon fin de run.
Tickets : 3/4/5 par seuil + **surplus** (4 tickets par doublement de la cible : x1,2 = 1, x1,5 = 2, x2 = 4, x4 = 8, max 10)
+ intérêts (1 par 5 tickets, max 5).
Cibles (`RunState.TargetFor`) : 650 × 1,6 par seuil, boss × 1,2, accélération en endless, arrondies à 2 chiffres.
Acte 1 : 650 / 1 000 / 2 000 · acte 2 : 2 700 / 4 300 / 8 200 · acte 3 : 11 000 / 17 000 / 34 000.
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
- `Core/BoardLayout.cs` : géométrie de la planche (relancer l'outil pour reconstruire la scène), rebond, gravité, zone de lâcher, valeurs des cases
- `Core/Catalog.cs` : effets/prix des pics, billes, éditions, charmes, boss
- `Core/RunState.cs` : courbe des cibles (`TargetBase`, `TargetGrowth`, `BossFactor`, `EndlessAcceleration`), récompenses,
  tickets de surplus (`SurplusBonus`, `SurplusCap`), intérêts, billes par manche
- `Items/Relics.cs` : chaque relique (ajoute une classe + une ligne dans `factories`)
- `Items/Shop.cs` : composition de la boutique, chances d'édition et de rareté

## Code
- Assemblies : `Cascade` (jeu, `Scripts/Cascade.asmdef`), `Cascade.Editor` (outil de scène), `Cascade.Tests.EditMode` (tests).
- Les grosses classes sont découpées en fichiers partiels : `GameManager` (+ `.Shop`, `.Charms`, `.Falling`),
  `GameUI` (+ `.Panels`, `.Screens`, `.Shop`).
- **Tests** : `Tests/EditMode/CascadeLogicTests.cs` (cibles, surplus, graine, score, reliques, boutique).
  Lancer via *Window > General > Test Runner > EditMode > Run All*.
- **Réglages et records** (`Core/GameSettings.cs`, PlayerPrefs) : intensité et activation du filtre VHS, volume,
  meilleur nombre de seuils et meilleure bille (affichés en fin de partie). Base du futur menu d'options.
- Tous les tirages aléatoires passent par la graine de la run (`RunState`).
- Nettoyage : paquets Visual Scripting, AI Navigation et Collab retirés ; `TutorialInfo`, `Readme.asset` et l'ancien `Glass.mat` supprimés.

## Pas encore fait (pistes de la fiche)
Sceaux, éléments de planche (rails, portails, ventilateurs…), événements entre seuils, méta-progression et difficultés,
sauvegarde de run en cours, Daily Drop / replays (la graine fixe tirages et boutiques, mais la physique Unity n'est pas garantie déterministe).

## Crédits
Voir `Licenses/CREDITS.md` : filtre VHS adapté du shader CC0 de *Vacation* (godotshaders.com), polices Bungee et VT323 (OFL).
