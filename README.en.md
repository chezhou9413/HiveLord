# LEVIATHANS: HIVE LORD

[简体中文](README.md)

A Hive Lord combat mod for RimWorld 1.6, featuring a Syndicate hunting contract, Spore Spewers and a learnable summoning ability. Includes desktop asset bundles for Windows, macOS and Linux. Requires Harmony and ChezhouLib.

## Layout

| Path | Responsibility |
| --- | --- |
| `HiveLord/About` | Mod metadata, cover and icon |
| `HiveLord/1.6/Defs` | Things, abilities, quests and asset registration |
| `HiveLord/1.6/Source/HiveLordLib` | Game runtime code |
| `HiveLord/1.6/Languages` | English and Simplified Chinese resources |
| `HiveLord/1.6/AssetBundles` | Windows, macOS and Linux model and shader bundles |
| `HiveLord/Docs` | Gameplay rules, interfaces and maintenance conventions |

The loadable mod root is the inner `HiveLord` directory. The Unity asset project is at `E:\mygame\HiveLordRimWorld`.

## Gameplay

The Hive Lord tracks enemies underground, surfaces to spray acid or slam the ground, then burrows away to reposition. Default health is 24,000, with a cap of 100 damage taken per hit. Lethal damage immediately disables combat and the hit proxy. The body rises from its current depth, transitions into a collapsing slam, and triggers dust and sound on ground impact. It holds for 90 ticks, then sinks over 600 ticks. Poses blend over 18 ticks; phase progress and the impact flag are saved and freeze while paused. Mod settings control damage dealt, damage taken, health, attack frequency and attack warnings.

Once total colony wealth reaches 500,000, the Syndicate offers a hunting contract when a valid site is available. Enter the nest and destroy the spewers with explosives before the Hive Lord surfaces three minutes later. Payment is 10,000 silver, 1,000 hyperweave and a Summon Hive Lord neurotrainer.

Site selection follows passable routes within 8–36 tiles of travel from a colony, preferring natural flat arid shrubland. If none exists, it selects reachable, unoccupied land in the same range. Acceptance rechecks the route, converts the selected tile to flat arid shrubland, and removes its landmark and terrain mutators. Converted tiles use a mean annual temperature of 26°C, rainfall of 800 mm and zero swampiness; climate, world rendering and path caches are refreshed. The terrain change is saved with the world, and retries reuse the same site. Only the absence of any valid reachable empty land defers the offer; missing natural plains do not block the contract.

The trainer permanently teaches the ability without requiring a psylink. The summon serves the user's faction and hunts enemies across the map. It has half the size and attack area, one-third of normal health (8,000 by default), a one-day duration and a 15-day ability cooldown.

Preparation and the Hive Lord fight count as active threats in vanilla checks against the player. Clearing the spewers, having no attackable target or the Hive Lord burrowing does not announce a safe site or enable safe caravan reformation early. Units can still withdraw through the map edge. After the contract ends, vanilla checks continue to account for remaining enemies. A living hostile Hive Lord with combat AI enabled also remains a threat underground on ordinary maps; allied summons do not count.

- [Contract, retries and summoning](HiveLord/Docs/SyndicateHunt.en.md)
- [Spore Spewers and map effects](HiveLord/Docs/SporeSpewer.en.md)
- [Localization conventions](HiveLord/Docs/Localization.en.md)
- [Challenge music](HiveLord/Docs/ChallengeMusic.en.md)

## Runtime architecture

`Things` owns entities, hit proxies and death. `Combat` and `Targeting` handle attack cycles, damage and positioning. `Rendering` captures isolated Unity models to transparent textures and composites them onto the map. A separate body mask excludes acid particles from silhouette detection. Each visible Hive Lord has its own capture stage, allowing summons and the contract target to coexist.

`SporeSpewer` owns the fixed building, status effect, map fog and collapse. Living bodies share a capture while particles run per instance. `Quests`, `World` and `Challenge` own contract generation, world sites and map progress respectively. `Summoning` handles training, placement validation and summon lifetime. `UI` measures text using the active language.

Combat parameters are stored in `HiveLordCombatExtension` in `Defs/ThingDefs/HiveLord_ThingDefs.xml`. Def names, asset keys and save fields are stable identifiers, independent of the display language.

## Build

Run from this directory:

```bat
compile_modSelf.bat Release
```

The runtime is a .NET Framework 4.8.1 library. MSBuild and dependency locations are configured in the build script and `HiveLordLib.csproj`; adjust local paths when moving to another machine. Output is `HiveLord/1.6/Assemblies/HiveLordLib.dll`. The deployment script calls the local deployment tool to copy the mod.

C#, Def and language changes do not require a bundle rebuild. After model or shader changes, update the capture prefabs in Unity and run the Windows, macOS and Linux AssetBundle builds under `RimWorldTools`. All platforms use the same asset labels and each produces three bundles: Hive Lord, Spore Spewer and shared shaders.

The builder is `Assets/Editor/BuildAssetBundle.cs` in the Unity project. Command-line entry points are `BuildAssetBundles.BuildAll`, `BuildAssetBundles.BuildMac` and `BuildAssetBundles.BuildLinux`. Use Unity 2022.3.35f1c1 with the corresponding platform modules. macOS shaders target Metal and OpenGL Core; Linux shaders target OpenGL Core and Vulkan. Builds are staged under `Library/HiveLordAssetBundles` and published to the mod's `1.6/AssetBundles` directory on success. Windows filenames have no platform suffix; macOS and Linux use `_mac.ab` and `_linux.ab`.

`Defs/ChezhouLib/HiveLord_UnityAssets.xml` declares platform paths for all three bundle types. ChezhouLib selects them using `Application.platform`, with no manual file switching required. Cross-platform verification covers Unity script, shader and bundle compilation; in-game rendering on macOS and Linux still requires confirmation on those devices.

Routine code verification stops at a Release build. Do not automatically launch RimWorld, enter Unity Play Mode or create test projects. Compilation does not verify in-game visuals or save behavior.

## Code interfaces

- `HiveLordQuestUtility.TryOfferQuest()`: validate wealth, site selection and uniqueness, then offer the contract.
- `HiveLordProjectionThing.SetCombatAiEnabled(bool)`: toggle automatic combat.
- `HiveLordProjectionThing.SetForcedCombatTarget(Thing)`: assign a target; pass `null` to clear it.
- `HiveLordSummonUtility.Spawn(Pawn, IntVec3)`: validate placement and create a timed summon.
- `SporeSpewerSpawnUtility.Spawn(Map, IntVec3)`: validate the footprint and spawn a Spore Spewer.
