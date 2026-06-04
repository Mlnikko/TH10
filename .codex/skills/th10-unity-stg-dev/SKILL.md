---
name: th10-unity-stg-dev
description: TH10 Unity STG project guide for Codex or Cursor. Use when working in this repository on Assets/Scripts, custom ECS battle logic, StageTimeline/EnemyWave/Boss configs, Danmaku systems, ConfigViewer/editor preview tools, Addressables/GameResDB resources, object pools, UTP lockstep networking, or UI battle flow.
---

# TH10 Unity STG Development

Use this skill as the local project map for TH10. Reply in Simplified Chinese unless the user asks otherwise.

## Project Shape

- Unity vertical STG inspired by Touhou; gameplay is data-driven and logic-frame based.
- Battle logic uses the project's own lightweight ECS, not Unity DOTS.
- MonoBehaviour/GameObject code handles rendering, animation, UI, audio, editor previews, and bridge-side presentation.
- Resources load through Addressables and are indexed by `GameResDB`; avoid `Resources.Load`.
- Networking uses Unity Transport with lockstep input collection; do not introduce Netcode for GameObjects unless requested.
- Runtime battle code should read ScriptableObject configs, not ConfigViewer prefabs.

## First Files To Open

- Startup/resources: `Assets/Scripts/GameLauncher.cs`, `Assets/Scripts/Resource/ResManager.cs`, `Assets/Scripts/Resource/GameResDB.cs`
- Battle flow: `Assets/Scripts/BattlePart/BattleManager.cs`, `Assets/Scripts/BattlePart/LogicFrameDriver.cs`
- ECS core: `Assets/Scripts/ECS/World.cs`, `Assets/Scripts/ECS/Component/Components.cs`, `Assets/Scripts/ECS/Entity/EntityFactory.cs`
- Systems: `Assets/Scripts/ECS/System/`
- Presentation bridge: `Assets/Scripts/ECS/Bridge/GameObjectBridge.cs`, `Assets/Scripts/ECS/Bridge/Updater/`
- Config base: `Assets/Scripts/SO/GameConfig.cs`, `Assets/Scripts/SO/ConfigViewer/GameConfigViewerBase.cs`, `Assets/Scripts/SO/Manifest/GameResourceManifest.cs`
- Stage timeline: `Assets/Scripts/ECS/System/StageTimelineSystem.cs`, `Assets/Scripts/SO/Stage/StageTimelineConfig.cs`, `Assets/Scripts/SO/Stage/EnemyWaveConfig.cs`, `Assets/Scripts/SO/Stage/BossPhaseConfig.cs`
- Danmaku: `Assets/Scripts/ECS/System/DanmakuSystem.cs`, `Assets/Scripts/ECS/System/DanmakuEmitSystem.cs`, `Assets/Scripts/SO/Danmaku/`
- Pooling: `Assets/Scripts/Pool/GameObjectPoolManager.cs`, `Assets/Scripts/SO/Pool/GlobalPoolConfig.cs`, `Assets/Scripts/SO/Pool/StagePoolConfig.cs`
- Networking: `Assets/Scripts/Network/NetworkManager.cs`, `Assets/Scripts/Network/NetworkMessages.cs`, `Assets/Scripts/InputManager.cs`, `Assets/Scripts/RoomManager.cs`
- UI: `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/UI_Panel/`

## Architecture Rules

- Put deterministic gameplay decisions in `BaseSystem.OnLogicTick(uint currentFrame)`.
- Do not use `Time.deltaTime`, wall-clock time, Unity physics callbacks, local-only randomness, or unordered iteration for synchronized battle decisions.
- Add ECS data as `struct` components implementing `IComponent`; follow existing names like `CPosition`, `CVelocity`, `CEnemy`, `CDanmaku`, `CCollider`.
- Request pooled presentation with tags such as `CPoolGetTag` and `CPoolRecycleTag`.
- Register new systems in `BattleManager.CreateBattleWorld()` with care; order affects timeline, movement, collision, firing, and presentation.
- Keep Unity `Transform`, prefab activation, UI, and visual interpolation in presentation/bridge code.
- Use `AsyncHelper.Forget` for fire-and-forget tasks; this project does not use UniTask.

## Config And Resource Rules

- New runtime data types should inherit `GameConfig`.
- Cross-config/resource references should implement `IReferenceResolver`.
- Timing/speed fields used by logic frames should implement `ILogicTimingBake`.
- Normalize logical ids with `StringHelper.NormalizeResourceId`; Addressable keys should be built with `ResHelper.GetAddressableKey`.
- Register new manually created configs or prefabs in `GameResourceManifest` and ensure `GameResDB.InitializeAsync` indexes them.
- ConfigViewer prefabs are editor tooling; save data back to `.asset` configs before relying on runtime behavior.
- BattleArea Scene edits, including the single-player center spawn handle, four independent multiplayer spawn handles, and drop-item collect-line handle, should update `BattleAreaConfigViewer` preview data first; the existing save button writes those values back to `BattleAreaConfig`.
- Keep drop-item collection responsibilities split: `BattleAreaConfig` configures only collect-line position and magnet speed, while each `DropItemConfig` configures its own pickup radius and previews it through `DropItemConfigViewer`.
- Pool ids must match Manifest ids and relevant `GlobalPoolConfig` / `StagePoolConfig` entries.

## Stage Timeline Notes

- Timeline runtime is driven by `StageTimelineSystem`.
- Stage editor/preview tooling lives under `Assets/Scripts/SO/Stage` and `Assets/Scripts/SO/Stage/Editor`.
- Preview tools should stay editor-only behind `#if UNITY_EDITOR`.
- Runtime preview must not disturb an active battle; use `StageTimelinePreviewRuntime.CanPreview` or validation helpers.
- Do not mount `BattleStageBackgroundRuntime` as a scene-maintained component. Formal battles use `BattleStageBackgroundPresenter`; editor background preview lets `StageTimelineConfigViewer` create a transient runtime child.
- StageTimeline preview resource setup should go through `StageTimelinePreviewRuntime.PrepareForPreviewAsync`; concurrent preload calls must await the same load task instead of bypassing readiness while `_loading` is true.
- In viewer preview APIs, use neutral names such as `previewIndex` for scope-dependent indices; reserve `waveIndex` for actual `midStageWaves`.
- Wave, mid boss, and main boss path editing share Scene gizmo context through `StageTimelineConfigViewer`.
- When modifying embedded timeline configs in Inspector, call `viewer.OnEmbeddedConfigChanged()` so Scene gizmos repaint and active previews can be restarted.
- Keep `StageTimelineConfigEditor` as a routing shell. Put focused UI into small helpers such as path section, spell phase section, duration section, and avoid caching `SerializedProperty` fields that are only assigned in `OnEnable`.
- Keep StageTimeline runtime preview controls near the top of the Inspector, before heavier timeline/background/path editing sections.
- For nested StageTimeline inspectors, use `StageTimelinePathEditScope` and `StageTimelineEmbeddedConfigEditor` so path fields, Scene handles, and restart prompts stay synchronized.

## Danmaku Notes

- Reuse `DanmakuEmitterSpawnMath` for emitter geometry.
- Keep player weapon emitter layout in `WeaponConfig` and `EntityFactory.CreatePlayerWeaponEmitters`.
- Avoid stacking multiple unrelated `CDanmakuEmitter` components on the player entity unless the existing weapon model requires it.
- New bullet or emitter prefabs must be registered in Manifest and pool config.

## Networking Notes

- Network payload structs live in `NetworkMessages.cs` and implement `INetworkMessage`.
- Every message needs a stable `MessageId`, `Serialize`, and `Deserialize`.
- Serialize and deserialize fields in exactly the same order.
- Gameplay-affecting network data usually needs frame/player identity.
- Check `InputManager`, `RoomManager`, `BattleManager`, and `NetworkManager` together for lockstep changes.

## Implementation Workflow

1. Identify the nearest subsystem: ECS, StageTimeline, config/resource, danmaku, pool, network, UI, or editor tool.
2. Read the first files above before changing behavior.
3. Preserve existing singleton, logging, id, bake, and editor-preview patterns.
4. Keep changes narrowly scoped; do not rewrite unrelated Unity assets or `.meta` files.
5. Be careful with serialized Unity assets. Prefer code/config class changes unless asset edits are required.
6. After editing C#, run available focused validation. If Unity compilation cannot be run from the shell, say so.

## Final Reply Checklist

- State the behavior-level change.
- List the core files touched.
- Mention validation run and any limits.
- Mention Unity Editor follow-up only when needed, such as recompiling, checking serialized assets, refreshing Addressables, or validating prefab references.
