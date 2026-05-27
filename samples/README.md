# Sample projects

Three small, self-contained scripts written while learning the listed engines / frameworks. They're intentionally short — each is something a hobbyist would build in a weekend to practice one or two concepts deeply.

| File | Engine / framework | Concepts practiced |
|---|---|---|
| `unity_BlockStacker.cs` | Unity (C#) | two-player local multiplayer, grid mechanics, wall-kick rotation, garbage rows |
| `unity_TrainRunner.cs` | Unity (C#) | cross-platform input (keyboard + iPad touch/swipe), lane-based movement, collision |
| `pygame_orbital_dodge.py` | PyGame | polar-coordinate movement, simple spawner, fixed-step game loop |
| `unity_FirstPersonController.cs` | Unity (C#) | CharacterController, mouse-look with pitch clamp, head-bob easing |
| `panda3d_voxel_garden.py` | Panda 3D | procedural geometry via GeomVertexWriter, orbit camera math, task-based input |

## Run

```bash
# PyGame demo
pip install pygame
python pygame_orbital_dodge.py

# Panda3D demo
pip install panda3d
python panda3d_voxel_garden.py
```

The Unity script drops onto a GameObject that has a `CharacterController` plus a child `Camera`.
