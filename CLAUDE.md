# Claw & Canopy

FPS survival game built in Unity (6000.3.11f1). Player explores a forest, fights/harvests dinosaurs, collects mushrooms for health, earns money.

## Performance Target

Target low-mid spec PCs (similar to R.E.P.O. / PEAK). Prioritize performance in all features:
- Avoid per-object Update() calls — use manager patterns
- Minimize draw calls — batch/combine meshes, use GPU instancing
- Avoid runtime Instantiate/Destroy in hot paths — use object pooling
- Limit real-time point lights — prefer unlit emissive materials
- Always evaluate performance impact when adding features

## Tech Stack

- **Engine:** Unity 6000.3.11f1
- **Rendering:** URP 17.3.0
- **Input:** Unity Input System 1.19.0 (new Input System — do NOT use old `Input.GetKeyDown`, use `Keyboard.current`)
- **Camera:** Cinemachine 3.1.6
- **Navigation:** AI Navigation 2.0.11 (NavMeshSurface for baking, NavMeshAgent for dinos)
- **Networking:** Mirror (imported but not actively used — game runs locally)
- **Platform:** Steamworks.NET (test app ID 480)

## Project Structure

```
Assets/Scripts/
├── AI/
│   ├── EnemyAI.cs              # Dino AI — aggressive/passive modes, wander, chase, flee, attack, health, harvest, sounds
│   └── DinoSpawner.cs          # Spawns dino prefabs on NavMesh at runtime
├── Environment/
│   ├── GlowbugSpawner.cs       # Spawns glowing fireflies (single manager Update, no per-bug scripts)
│   └── StarGenerator.cs        # Generates star sky (single combined mesh, 1 draw call)
├── Items/
│   ├── Collectible.cs          # Press E to eat — heals player, used on mushrooms
│   ├── Harvestable.cs          # Press E to harvest — gives money, auto-added to dead dinos
│   └── MushroomSpawner.cs      # Spawns mushroom prefabs via terrain raycast
├── Player/
│   ├── PlayerHealth.cs         # Health, damage, healing, respawn (resets stamina too)
│   └── PlayerWallet.cs         # Money tracking
├── UI/
│   ├── StaminaBar.cs           # Reads FirstPersonController.CurrentStamina
│   ├── HealthBar.cs            # Reads PlayerHealth.CurrentHealth
│   ├── EnemyHealthBar.cs       # World-space bar, auto-calculates height from renderers
│   ├── MoneyDisplay.cs         # Shows PlayerWallet.Money as text
│   └── PauseMenu.cs            # ESC to pause/resume, quit button
├── FireArm.cs                  # Raycast shooting, URP decals, damage to EnemyAI
└── Steamworks.NET/
    └── SteamManager.cs         # Steam API init
```

```
Assets/
├── StarterAssets/                                # FPS controller + input
│   ├── FirstPersonController/Scripts/
│   │   └── FirstPersonController.cs             # Player movement, sprint, jump, crouch, stamina
│   └── InputSystem/
│       ├── StarterAssets.inputactions            # Input bindings (WASD, Sprint=LShift, Jump=Space, Crouch=C)
│       └── StarterAssetsInputs.cs               # Input value storage (move, look, jump, sprint, crouch)
├── Scenes/
│   ├── SampleScene.unity                        # Main playable scene
│   ├── OnlineScene.unity                        # Multiplayer scene (unused currently)
│   └── DecalScene.unity                         # Test scene
├── polyperfect/Low Poly Animated Dinosaurs/     # Dino pack — prefabs, animators, sounds, meshes
├── Low Poly Mushrooms Pack/                     # Mushroom prefabs
├── Blink/                                       # Bear models & animations
├── BountyHunter_RIO/                            # Player character model
├── Free-Low Poly Stylized Weapons/              # Weapon models
├── Polytope Studio/                             # Environment art
├── Mirror/                                      # Networking framework
└── Settings/                                    # URP render pipeline settings
```

## Key Systems

### Player (on PlayerCapsule)
- **FirstPersonController** — WASD movement, sprint (LShift, drains stamina), jump (Space), crouch (C, toggle)
- **PlayerHealth** — 100 HP, TakeDamage/Heal, respawn at start position on death (resets stamina)
- **PlayerWallet** — Money counter, AddMoney()
- **FireArm** — On weapon child. Raycast shooting, deals damage to EnemyAI, spawns URP DecalProjector bullet holes on environment

### Dinosaur AI (EnemyAI)
- **Modes:** Aggressive (chase + attack) or Passive (flee)
- **Wander:** Dinos roam near spawn when player is out of detection range. Walk animation while wandering, run when chasing.
- **Animator params are configurable per dino** — defaults: isWalking, isRunning, isAttacking, isDead
- **Health:** Takes damage from FireArm raycast. On death: plays death anim, adds Harvestable component, despawns after 30s
- **Harvest:** Dead dinos show "Press E to Harvest" — gives money to PlayerWallet
- **Sounds:** 3D spatialized movement sound, plays when moving, stops on idle/death

### Dino Animator Parameters (polyperfect pack)
All use bools: `isWalking`, `isRunning`, `isAttacking`, `isDead`, `isEating`, `isRoaring`

### Spawners
- **DinoSpawner** — Spawns prefabs on NavMesh. Needs baked NavMeshSurface.
- **MushroomSpawner** — Spawns prefabs via terrain raycast. Auto-adds Collectible + SphereCollider.
- **GlowbugSpawner** — Single manager, moves all bugs in one Update loop. No per-bug scripts or lights.
- **StarGenerator** — Builds combined mesh on Start. 1 draw call, 0 Update calls.

## Important Notes

- **Input System:** Project uses new Input System exclusively. Old `Input.GetKeyDown` does NOT work. Use `Keyboard.current.xKey.wasPressedThisFrame` or wire through StarterAssetsInputs.
- **URP Materials:** Imported asset packs may use built-in Standard shader (pink materials). Fix via Render Pipeline Converter or manually set shader to URP/Lit.
- **NavMesh:** Must re-bake NavMeshSurface after terrain changes. Dino spawner and NavMeshAgent depend on it.
- **Colliders:** Dinos need a collider (Box Collider preferred) for gun raycast hits and player collision. Mesh Colliders require a mesh assigned + Convex checked.
- **Player layer:** PlayerCapsule should be on a "Player" layer excluded from gun's Shootable Layers to avoid self-hits.
- **Mirror:** Imported but gameplay currently runs locally via FirstPersonController (MonoBehaviour), not OnlineFirstPersonController (NetworkBehaviour, deleted).

## Conventions

- Scripts organized by domain: `AI/`, `Player/`, `UI/`, `Items/`, `Environment/`
- C# naming: PascalCase for public fields, _camelCase for private fields
- Use new Input System for all input (Keyboard.current, Mouse.current)
- Spawners use raycast or NavMesh.SamplePosition to place objects on terrain
- UI bars use Image.fillAmount with Filled/Horizontal Image type
