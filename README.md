# prototype_1 — Urban UAV & Wind Simulation

A Unity prototype simulating a **drone flying through a city** under **aerodynamic wind forces**: base wind, street-canyon (venturi) effect, wake turbulence behind buildings, and altitude-based wind shear.

> Project for learning Unity/C# — code is written by hand, not generated.

## Requirements

- Unity Editor **6000.6.8f1**
- Universal RP 17.6.0
- Input System 1.20.1

## Run

1. Open `Assets/1.Scenes/Prototype 1.unity`.
2. Press **Play**.

| Key | Action |
|---|---|
| `W` / `S` | Forward / backward (pitch tilt) |
| `A` / `D` | Strafe left / right (roll tilt) |
| `Space` | Ascend |
| `Ctrl` | Descend |

The drone tilts its body to move and auto-compensates lift to hold altitude, so tilting causes it to drift.

## Layout

```
Assets/
├── 1.Scenes/       Main scene: ground, 12 buildings, drone, wind controller
├── 2.Script/       Gameplay logic
│   ├── SimulateObject.cs           Base class: input, physics params
│   ├── SimulateObjectDatabase.cs   Loads the JSON parameter table
│   ├── SimulateMain.cs             Wind field: drag force, wind occlusion
│   └── Drone.cs                    Flight control, tilt, lift compensation
├── 3.Material/     floor.mat, wall.mat
├── Resources/SimulateObjectDatabase.json   Physics parameters
├── Settings/       URP pipeline + volume profiles
├── Plugins/        IngameDebugConsole (third party)
└── vFavorites/     Editor bookmark tool (third party)
```

## Parameters

`Assets/Resources/SimulateObjectDatabase.json` is the single source of truth for `Cd`, thrust, move force, cross-section area and mass.

Editing those values on the Inspector has **no effect** while `_autoLoadFromDatabase` is on — the JSON overwrites them in `Awake()`.

## Wind model (current)

```
v_rel  = v_drone − v_wind
F_drag = 0.5 · ρ · |v_rel|² · Cd · A
F_wind = −v_rel_dir · F_drag
```

Wind is currently zeroed when the drone is occluded from the wind source. Planned improvements include venturi, wake turbulence, wind shear, PID attitude hold, and downwash.

## Contributing

- Commit scope: only commit changes inside `Assets/` (do not commit `Packages/`, `AGENTS.md`, or `plan.md`).
- Commit messages: one line, concise.
- Prefer pushing to a branch other than `main`.
- No AI-written or AI-edited script files — review and explanations only.

## Third-party

- [Ingame Debug Console](https://github.com/yasirkula/UnityIngameDebugConsole)
- vFavorites
