# 🧩 Clumsy Crew – Core Systems Showcase

This repository showcases the core gameplay, UI, and utility systems developed for **Clumsy Crew**, a humorous handyman party game by **Igronaut Games**.  
It highlights my approach to writing modular, data-driven Unity systems built for flexibility and designer iteration.

🔍 **Note:** This repository does not include prefabs, textures, or third-party assets (LeanTween, Odin, Feedbacks).  
Only the original C# source code is provided to illustrate architecture.

⚠️ **This is not a standalone playable project.**  
It’s a code architecture sample meant to demonstrate the quality, modularity, and scalability of the systems used in the full game.

---

## 🎯 Purpose

This repo is designed to:

- Showcase clean, modular Unity C# architecture for professional or portfolio purposes.  
- Demonstrate how *Clumsy Crew* manages character systems, UI flow, gameplay feedback, and designer-driven definitions.  
- Serve as a technical reference for clean Unity project structuring and component orchestration.

---

## 🧠 Highlights

- **Data-Driven Design:** All core gameplay logic is built around ScriptableObject definitions for tools, feedbacks, and camera shakes.  
- **Modular Architecture:** Each system (UI, characters, minigames, definitions) is isolated for easy testing and future expansion.  
- **Polished Game Feel:** Dynamic feedback systems — camera shakes, HUD bursts, animated UI, crowd reactions — are built around LeanTween for responsiveness.  
- **Editor Tools Included:** Handy productivity utilities (scene switcher, fullscreen view, bone renamer) for smoother workflow.  
- **Readable, Reusable Code:** Fully commented and dependency-light, suitable as a teaching or portfolio sample.

---


## 📂 Project Structure
```
Assets/
└── Scripts/
    ├── 🎵 Audio/
    │   ├── AudioManager.cs                # Plays and adjusts all sounds
    │   ├── SoundDefinition.cs             # Defines certain sound with different adjustments
    ├── 🧍 Characters/
    │   ├── CarryingVisuals.cs             # Cartoony stacked-item visuals & animation
    │   ├── CharacterCamera.cs             # Cinemachine camera logic per character
    │   ├── CharacterRigAnchors.cs         # Rig anchor references for attaching tools/props
    │   ├── CharacterRigAnchorsEditor.cs   # Custom inspector for rig anchors
    │   ├── CharacterScript.cs             # Base character logic and initialization
    │   ├── DetectionController.cs         # Detects nearby interactables
    │   ├── Detector.cs                    # Collider-based hit detector
    │   ├── DynamicJoystick.cs             # Mobile joystick controller
    │   ├── MainCharacterScript.cs         # Extended player-controlled character
    │   ├── ModifierController.cs          # Handles attribute/stat modifications
    │   ├── Movement.cs                    # Core movement and animation blending
    │   └── StatModifier.cs                # Data struct for applying stat bonuses
    │
    ├── 🧠 Core/
    │   ├── CharactersManager.cs           # Handles all player & NPC character references
    │   ├── PlayerManager.cs               # Player slots, cameras, and HUD setup
    │   ├── DefinitionsManager.cs          # Loads and provides access to all ScriptableObject definitions
    │   ├── PopUpManager.cs                # Global popup spawning & stack control
    │   └── RTController.cs                # Runtime canvas & render-texture management
    │
    ├── 📜 Definitions/
    │   ├── CameraShakeDefinition.cs       # Designer-friendly camera shake tuning
    │   ├── HudFeedbackDef.cs              # Feedback animation parameters
    │   ├── ReactionDef.cs                 # Host/NPC reaction configurations
    │   └── ToolDefinition.cs              # Data for each tool (speed, effects, charge)
    │
    ├── 🎮 Minigames/
    │   ├── CrowdController.cs             # NPC crowd reactions & cheering logic
    │   ├── HittingController.cs           # Tool-based hit detection during jobs
    │
    ├── ⚙️ Systems/
    │   ├── BezierSwingPath.cs             # Helper for visualizing Bezier motion paths
    │   ├── MeshTrail.cs                   # Generates dynamic motion trail meshes
    │   ├── ProjectileUtil.cs              # Calculates ballistic projectile velocity
    │   └── SpatialAudioScript.cs          # Plays localized or global audio clips with falloff
    │
    ├── 🧰 Tools/
    │   ├── BoneRenamer.cs                 # Batch rename bones in animation clips
    │   ├── FullscreenGameView.cs          # Opens Game view in full screen during play mode
    │   ├── SceneSwitcherMenu.cs           # Quick scene shortcuts in Unity menu
    │   └── UIFormatters.cs                # Utility for formatting numbers, time, and FPS
    │
    ├── 🧩 UI/
    │   ├── GameHudFeedback.cs             # Animated HUD feedback elements
    │   ├── HudFeedbackController.cs       # Spawns and manages HUD feedback effects
    │   ├── PopUp.cs                       # Generic popup base class with open/close animation
    │   ├── SafeScreen.cs                  # Adjusts anchors for device safe areas
    │   ├── UIButtonJuicyTilt.cs           # Cartoony button tilt and color feedback
    │   ├── UIEffects.cs                   # Manages collectible animations and flying icons
    │   └── UIFeedbackSpawner.cs           # Helper for spawning UI effects dynamically
    │
    └── 🔧 Utilities/
        ├── CollectionExtensions.cs        # List and collection helper methods
        └── TransformExtensions.cs         # Transform movement and hierarchy helpers
```


---

## 🧭 Folder Summary

| Folder | Purpose |
|--------|----------|
| 🧠 **Core** | Global managers controlling the flow, players, and definitions |
| 🧍 **Characters** | Character logic, rig setup, animation, and movement |
| 🎮 **Minigames** | Task-specific logic (jobs, feedbacks, crowd reactions) |
| 🧩 **UI** | Popups, HUDs, feedback, and on-screen effects |
| ⚙️ **Systems** | Reusable gameplay systems like trails, audio, or projectiles |
| 📜 **Definitions** | ScriptableObjects for designer-editable gameplay data |
| 🧰 **Tools** | Editor utilities improving workflow |
| 🔧 **Utilities** | Static helpers and generic extensions |

---

## 🧾 License

© 2025 Igronaut Games.  
Shared for portfolio and educational reference purposes only.  
Redistribution or commercial use is not permitted without permission.
