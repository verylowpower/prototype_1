# prototype_1 — Wind & Aerodynamic Simulation Environment

Unity prototype focused on building a **general-purpose wind and aerodynamic simulation environment for arbitrary physical objects** (`SimulateObject`), based on each object's mass, drag coefficient (`Cd`), and cross-sectional area (`A`). Currently tested with a controllable drone in an urban environment.

> Built as a hands-on project for learning Unity and C#.

## Wind Model

```text
v_rel  = v_obj − v_wind
F_drag = 0.5 · ρ · |v_rel|² · Cd · A
F_wind = −v_rel_dir · F_drag
```

Wind is blocked when an obstacle occludes the object from the wind source, with planned extensions for urban wind shear, street-canyon (Venturi) acceleration, and wake turbulence.

## Requirements

- Unity **6000.6.8f1** (URP, Input System)

## Quick Start

Open `Assets/1.Scenes/Prototype 1.unity` and press **Play**.

- `W` / `A` / `S` / `D` — Tilt / move
- `Space` / `Ctrl` — Ascend / Descend
