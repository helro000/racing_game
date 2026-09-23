# Road Relay

## Description

Road Relay is a Unity-based arcade racing game where players race against AI opponents across multiple tracks. The game includes different difficulty levels, race modes, checkpoints, boost strips, car selection, minimap and rival indicators, dynamic road hazards such as oil, mud, and rough road, and local saving for progress and best times.

The project focuses on smooth arcade-style driving, competitive AI racing, and enjoyable gameplay.

## Project Download

The full Unity project is larger than the free Git storage limit.

**Cloud Storage:**  
[Download Road Relay Project](PASTE_YOUR_GOOGLE_DRIVE_OR_CLOUD_LINK_HERE)

## Screenshots

### Main Menu
![Main Menu](Screenshots/main-menu.png)

### Race Setup
![Race Setup](Screenshots/race-setup.png)

### Gameplay
![Gameplay](Screenshots/gameplay.png)

### Road Hazards
![Road Hazards](Screenshots/road-hazards.png)

## Members

- Member 1: Htoo Pyae Sone Htun,6540159
- Member 2: Lin Myat Thu 6722057
- Member 3: MIN AUNG KYAW 6238126

## Technologies

- Unity
- C#
- Unity Physics
- EasyRoads3D

## License

This project is developed for educational purposes.

Copyright © 2026 Road Relay Team.  
All rights reserved.

A single-player racing game built in Unity 6000.5.3f1 with five car models and four selectable tracks: Valley Sprint, City Sprint, Mountain Pass, and Desert Run.

## Track and mode selection

In the garage, click the track, mode, or difficulty button to cycle its options, then click RACE. Race mode has two rivals and Easy, Normal, or Hard pace settings. Time Trial is a solo run. Best times are stored separately for each track, mode, race difficulty, and car. Selections are remembered between launches.

During driving, only race status, the minimap, speed, boost charges, and rival information are shown. Control instructions remain in the garage and pause menu. Press Escape to pause or see the controls.

## Play

Open `Builds/RoadRelay/RoadRelay.exe`. Keep the entire RoadRelay folder together: the executable needs the adjacent data folder and DLLs.

Choose a free car with the arrow buttons, then click RACE. NOVA and ECHO start ahead in separate grid slots. Follow the wide, barrier-lined road to the finish. Rival labels, distance indicators, and the live course map show where your opponents are. The result screen offers Race Again and Change Car.

The rebuilt course has a 12-meter racing surface, broad bends, shoulders, curbs, and a visible finish line. Steering reduces with speed; Space brakes, and holding S/down brakes before reversing. Ordered progress counts across the road's width rather than requiring you to touch invisible centerline points.

## Controls

- WASD or arrow keys: accelerate, reverse and steer.
- Space: brake.
- B: place a shared boost strip on the road ahead.
- R: return to your last section of road.
- Escape: pause or resume.

## The twist

You build your own boost opportunities, but opponents can use them too. Rivals place orange strips that you can steal. Cyan strips are yours. Each strip boosts each car once, lasts 12 seconds and cannot launch a car beyond its speed cap. You have two charges; each used charge takes eight seconds to refill. Build only during the race and near the road.

## Included

One valley sprint, five free cars, two opponents with bend-aware speed and passing lanes, a countdown, ordered route progress, live position and speed, recovery, pause, results, replay and a saved best time for each car. The old shop and unfinished map menus are not part of Road Relay's menu or Windows release. Their source scenes remain in the Unity project.

## Unity project

Open this folder in Unity Hub, then open `Assets/Scenes/awakeScene.unity` and press Play.

- `RelayGarage.cs`: car selection and start screen.
- `RelayRace.cs`: race states, ranking, finish, best times and HUD.
- `RelayRacer.cs`: ordered progress, recovery and AI pure-pursuit steering.
- `RoadBuilderPower.cs` / `RoadBoostPad.cs`: shared strips, recharge and one boost per car.
- `EasyRoadsMapBuilder.cs` / `RelayTrackGeometry.cs`: generate the valley race, sharing a centerline between pavement and navigation, with barriers, curbs, scenery and start/finish arches.
- `RelayMinimap.cs`: shows the course and all three racers.

Editor commands:

- `EasyRoadsMapBuilder.BuildAll`: rebuild all four tracks and register them in Build Settings. `Build` rebuilds the original valley scene only.
- `RelayValidation.Run` with `-relay-test`: run play-mode integration checks. Use graphics-enabled batch mode; this project's old rendering assets crash Unity's no-graphics play mode.
- `RelayValidation.BuildPlayer`: build the standalone Windows game.

`RelayPlaytestReport.txt` records the latest integration results. These exercise actual vehicle physics, all five cars, shared boosts, pause, ordered completion, recovery and scene transitions. Automated driving is a repeatable check; final visual and keyboard checks are also required.

`ExpansionReport.txt` records the expansion checks across four tracks and both modes, including road continuity, minimap geometry, opponent progress, clean HUD, pause, restart, and separate saved best times. The editor-only `RelayExpansionPipeline` runs these checks and builds the Windows game when explicitly requested with a `Library/RelayExpansion.request` marker.

The EasyRoads3D package and original vehicle assets remain subject to their respective licenses. No extra package download is required to play this local build.

## How to Play

1. Launch **Road Relay**.
2. Choose your car.
3. Select a track.
4. Choose a race mode and difficulty.
5. Start the race and compete against AI opponents.
6. Follow the route, pass checkpoints in order, and reach the finish line as quickly as possible.
7. Avoid road hazards such as oil, mud, and rough patches because they can slow the car or make it lose control.
8. Use boost strips when available to gain extra speed.

### Controls

| Key | Action |
|---|---|
| `W` / `Up Arrow` | Accelerate |
| `S` / `Down Arrow` | Brake / Reverse |
| `A` / `Left Arrow` | Steer Left |
| `D` / `Right Arrow` | Steer Right |
| `Space` | Brake |
| `B` | Place / use shared boost strip |
| `R` | Recover car if stuck |
| `Esc` | Pause the game |
