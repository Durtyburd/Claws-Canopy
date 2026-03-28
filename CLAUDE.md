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
│   ├── EnemyAI.cs              # Generic dino AI — aggressive/passive, wander, chase, flee, attack, frighten
│   ├── RaptorAI.cs             # Pack raptor — stalk, circle, strike, retreat, dodge
│   ├── RaptorPack.cs           # Pack coordinator — flanking, distraction, synchronized strikes, pack calls
│   ├── TRexAI.cs               # T-Rex — territorial patrol, roar, charge, stomp AoE, enrage, vibration sense
│   ├── IDamageable.cs          # Interface: MaxHealth, CurrentHealth, TakeDamage()
│   ├── DetectionUtils.cs       # Static utility: visibility-aware LOS detection for all AI types
│   └── DinoSpawner.cs          # Spawns dino prefabs on NavMesh — auto-detects packs vs solo
├── Environment/
│   ├── GlowbugSpawner.cs       # Spawns glowing fireflies (single manager Update, no per-bug scripts)
│   ├── StarGenerator.cs        # Generates star sky (single combined mesh, 1 draw call)
│   ├── ScreenShake.cs          # Static singleton — ScreenShake.Shake(duration, magnitude)
│   └── TallGrassZone.cs        # Trigger collider — sets PlayerVisibility.InTallGrass on enter/exit
├── Items/
│   ├── Collectible.cs          # Press E to eat — heals player, used on mushrooms
│   ├── Harvestable.cs          # Press E to harvest — gives money, auto-added to dead dinos
│   └── MushroomSpawner.cs      # Spawns mushroom prefabs via terrain raycast
├── Player/
│   ├── PlayerHealth.cs         # Health, damage, healing, respawn, invulnerability timer
│   ├── PlayerWallet.cs         # Money tracking
│   ├── PlayerVisibility.cs     # Calculates visibility score (0–1.5) from crouch/sprint/grass/movement
│   └── PlayerLOSTarget.cs      # Provides eye-level and body-center raycast targets for AI
├── UI/
│   ├── StaminaBar.cs           # Reads FirstPersonController.CurrentStamina
│   ├── HealthBar.cs            # Reads PlayerHealth.CurrentHealth
│   ├── EnemyHealthBar.cs       # World-space bar, works with any IDamageable
│   ├── MoneyDisplay.cs         # Shows PlayerWallet.Money as text
│   ├── Hotbar.cs               # 5-slot hotbar — scroll wheel/number keys, glow on selected
│   └── PauseMenu.cs            # ESC/gamepad Start to pause/resume, quit button
├── FireArm.cs                  # Raycast shooting, URP decals, damage via IDamageable interface
└── Steamworks.NET/
    └── SteamManager.cs         # Steam API init
```

```
Assets/
├── StarterAssets/                                # FPS controller + input
│   ├── FirstPersonController/Scripts/
│   │   └── FirstPersonController.cs             # Movement, sprint, jump, crouch, stamina, knockback
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
- **FirstPersonController** — WASD movement, sprint (LShift, drains stamina), jump (Space), crouch (C, toggle), knockback support via `ApplyKnockback(Vector3)`
- **PlayerHealth** — 100 HP, TakeDamage/Heal, respawn at start position on death (resets stamina), 2s invulnerability after respawn
- **PlayerWallet** — Money counter, AddMoney()
- **PlayerVisibility** — Calculates visibility score (0–1.5) used by all AI detection. Factors: crouching (0.5x), standing still (0.7x), sprinting (1.4x), tall grass+crouch (0.15x), tall grass+standing (0.6x)
- **PlayerLOSTarget** — Provides eye-level (CinemachineCameraTarget) and body-center raycast points for AI line-of-sight checks
- **FireArm** — On weapon child. Raycast shooting, deals damage via `IDamageable` interface, spawns URP DecalProjector bullet holes

### Damage System (IDamageable)
All damageable entities implement `IDamageable` (MaxHealth, CurrentHealth, TakeDamage). FireArm and EnemyHealthBar use this interface — any new enemy type just implements it.

### Detection System (DetectionUtils)
All AI uses `DetectionUtils.CanDetectPlayer()` — a shared static utility:
1. Distance check first (cheap) using `baseRange * visibility * (1/multiplier)`
2. LOS raycast only when in range (`QueryTriggerInteraction.Ignore` so grass triggers don't block)
3. Fallback ray to body center if head is behind cover
- Trees/rocks/terrain with colliders block line-of-sight
- Each AI type has a `visibilityMultiplier` for tuning (lower = keener sight)

### Dinosaur AI (EnemyAI)
- **Modes:** Aggressive (chase + attack) or Passive (flee)
- **Detection:** Uses DetectionUtils with standard visibility (1.0x multiplier)
- **Wander:** Dinos roam near spawn when player is undetected. Walk animation while wandering, run when chasing.
- **Frighten:** `Frighten(Vector3 source, float duration)` forces flee behavior (used by T-Rex roar)
- **Animator params are configurable per dino** — defaults: isWalking, isRunning, isAttacking, isDead, isEating
- **Health:** Takes damage from FireArm raycast. On death: plays death anim, adds Harvestable component, despawns after 30s
- **Harvest:** Dead dinos show "Press E to Harvest" — gives money to PlayerWallet
- **Sounds:** 3D spatialized movement sound, plays when moving, stops on idle/death

### Raptor Pack AI (RaptorPack + RaptorAI)
- **Pack spawning:** RaptorPack spawns 2-3 raptors together. DinoSpawner auto-creates packs when it picks a raptor prefab.
- **Detection:** Keen-eyed (0.8x visibility multiplier). Uses closest member position for LOS origin.
- **Flanking:** Orbit positions biased toward player's flanks and rear based on camera direction
- **Distractor:** One raptor assigned to taunt from the front, does feint darts toward player
- **Synchronized strikes:** 25% chance two raptors attack from opposite sides simultaneously
- **Dodge:** Raptors detect when player aims at them (5° angle check) and sprint sideways
- **Pack calls:** When one pack spots player, alerts all packs within 60m
- **Aggression ramp:** Strike cooldown shrinks as pack members die (0.7x per death)
- **States:** Wandering → Stalking → Circling → Striking → Retreating

### T-Rex AI (TRexAI)
- **Territorial:** Patrols a 40m radius territory
- **Dual detection:** Poor eyesight (1.3x visibility multiplier) + vibration sense (detects movement within 25m regardless of cover/visibility)
- **Encounter flow:** Patrol → Alert (2s stare) → Roar (screen shake, scatters nearby dinos) → Charge → Attack
- **Attack:** Bite (35 dmg) + knockback + stomp AoE (15 dmg, 6m radius)
- **Enrage:** Below 50% HP — 1.4x speed, 1.5x damage, 0.5x attack cooldown
- **Footsteps:** Audible up to 50m, cause distance-faded screen shake
- **500 HP, 200 money harvest, 60s despawn**

### Dino Animator Parameters (polyperfect pack)
All use bools: `isWalking`, `isRunning`, `isAttacking`, `isDead`, `isEating`, `isRoaring`

### Spawners
- **DinoSpawner** — Uses `DinoSpawnEntry[]` (prefab + count per entry). Auto-detects RaptorAI prefabs and creates RaptorPack groups. Solo dinos (EnemyAI, TRexAI) spawn individually. Validates NavMesh before spawning. Enforces min distance between packs.
- **MushroomSpawner** — Spawns prefabs via terrain raycast. Auto-adds Collectible + SphereCollider.
- **GlowbugSpawner** — Single manager, moves all bugs in one Update loop. No per-bug scripts or lights.
- **StarGenerator** — Builds combined mesh on Start. 1 draw call, 0 Update calls.

### Stealth / Visibility
- **PlayerVisibility** on PlayerCapsule calculates a 0–1.5 score each frame
- **Crouching** halves detection range. **Sprinting** increases it by 1.4x. **Standing still** reduces by 0.7x.
- **Tall grass** nearly eliminates visibility when crouching (0.15x) or reduces when standing (0.6x)
- **TallGrassZone** — trigger collider component placed over grass patches. Event-driven (OnTriggerEnter/Exit), no Update.
- **Line-of-sight** — AI raycasts are blocked by any collider (trees, rocks, terrain). Trigger colliders are ignored.

### UI
- **Hotbar** — 5 slots at bottom-center. Scroll wheel or keys 1-5 to select. Selected slot pulses gold. `SetSlotIcon(index, sprite)` API for future items.
- **EnemyHealthBar** — Works with any IDamageable via interface. Auto-calculates height from renderer bounds.
- **PauseMenu** — ESC key + gamepad Start button support.

## Important Notes

- **Input System:** Project uses new Input System exclusively. Old `Input.GetKeyDown` does NOT work. Use `Keyboard.current.xKey.wasPressedThisFrame` or wire through StarterAssetsInputs.
- **URP Materials:** Imported asset packs may use built-in Standard shader (pink materials). Fix via Render Pipeline Converter or manually set shader to URP/Lit.
- **NavMesh:** Must re-bake NavMeshSurface after terrain changes. DinoSpawner, NavMeshAgent, and all dino AI depend on it.
- **Colliders:** Dinos need a collider (Box Collider preferred) for gun raycast hits AND line-of-sight blocking. Mesh Colliders require a mesh assigned + Convex checked.
- **Tree/Rock colliders:** Trees and rocks MUST have colliders for the stealth/LOS system to work. Without colliders, AI can see through them.
- **Player layer:** PlayerCapsule should be on a "Player" layer excluded from gun's Shootable Layers to avoid self-hits. The "Player" layer is also excluded from AI LOS raycasts.
- **Mirror:** Imported but gameplay currently runs locally via FirstPersonController (MonoBehaviour).
- **DinoSpawner entries:** The old `dinoPrefabs[]` array was replaced with `DinoSpawnEntry[]` (prefab + count). Prefabs must be re-assigned in the inspector.

## TODO: Scene Setup Required

- **Tall grass zones:** Place box colliders (Is Trigger = true) over tall grass geometry patches in the scene, then add the `TallGrassZone` component to each. The visual grass meshes are separate from these trigger volumes. Without these, the grass stealth mechanic won't function.
- **PlayerCapsule components:** Ensure `PlayerVisibility` and `PlayerLOSTarget` are added to the PlayerCapsule GameObject alongside the existing components.

## Conventions

- Scripts organized by domain: `AI/`, `Player/`, `UI/`, `Items/`, `Environment/`
- C# naming: PascalCase for public fields, _camelCase for private fields
- Use new Input System for all input (Keyboard.current, Mouse.current)
- Spawners use raycast or NavMesh.SamplePosition to place objects on terrain
- UI bars use Image.fillAmount with Filled/Horizontal Image type
- All damageable enemies implement `IDamageable` — FireArm and EnemyHealthBar use this interface
- AI detection goes through `DetectionUtils.CanDetectPlayer()` — never raw distance checks
- Screen shake via `ScreenShake.Shake(duration, magnitude)` — auto-attaches to main camera
