---
name: th10-stage-timeline-editor
description: Fast-entry TH10 Unity workflow for modifying StageTimeline configs and their editor tooling. Use when Codex needs to adjust StageTimelineConfig, EnemyWaveConfig, MidBoss/MainBoss/BossPhase timeline data, StageTimelineConfigViewer/Editor inspector sections, runtime preview controls, background preview, Scene path handles, path snapping, embedded config editing, or any StageTimeline editor-preview workflow in this repository.
---

# TH10 Stage Timeline Editor

Use this skill as the narrow entry point for StageTimeline config and editor-tool changes in `/Users/mlnikko/Projects/Unity/TH10`. Reply in Simplified Chinese unless the user asks otherwise.

## First Files

- Runtime system: `Assets/Scripts/ECS/System/StageTimelineSystem.cs`
- Core config: `Assets/Scripts/SO/Stage/StageTimelineConfig.cs`
- Wave config: `Assets/Scripts/SO/Stage/EnemyWaveConfig.cs`
- Boss configs: `Assets/Scripts/SO/Stage/MidBossEncounterConfig.cs`, `Assets/Scripts/SO/Stage/MainBossEncounterConfig.cs`, `Assets/Scripts/SO/Stage/BossPhaseConfig.cs`
- Viewer/runtime preview: `Assets/Scripts/SO/Stage/StageTimelineConfigViewer.cs`, `Assets/Scripts/SO/Stage/StageTimelinePreviewRuntime.cs`
- Inspector shell: `Assets/Scripts/SO/Stage/Editor/StageTimelineConfigEditor.cs`
- Visual timeline: `Assets/Scripts/SO/Stage/StageTimelineVisualSchedule.cs`, `Assets/Scripts/SO/Stage/Editor/StageTimelineVisualTimelineEditor.cs`
- Scene path editing: `Assets/Scripts/SO/Stage/Editor/StageTimelinePathRouteSceneHandles.cs`, `StageTimelinePathNodeMultiEdit.cs`, `StageTimelineBezierHandleEdit.cs`, `StageTimelinePathEditSnap.cs`
- Nested inspectors: `Assets/Scripts/SO/Stage/Editor/StageTimelineEmbeddedConfigEditor.cs`, `StageTimelineBossSpellPhaseEdit.cs`

## Workflow

1. Identify whether the request is data/runtime behavior, Inspector layout, runtime preview, background preview, visual timeline, or Scene path editing.
2. Read `StageTimelineConfigViewer.cs` plus the nearest editor helper before editing. Do not assume `StageTimelineConfigEditor.cs` owns all behavior.
3. Keep `StageTimelineConfigEditor` as a routing shell. Move focused UI into small helpers when logic grows.
4. For embedded wave/boss/spell edits, call `viewer.OnEmbeddedConfigChanged()` so Scene gizmos repaint and active previews can restart cleanly.
5. For Scene path edits, preserve `StageTimelinePathEditScope` and shared gizmo context through `StageTimelineConfigViewer`.
6. For preview changes, go through `StageTimelinePreviewRuntime.PrepareForPreviewAsync`; preview must be Play-mode only and must not disturb an active battle.

## Common Change Map

- Inspector section order or buttons: start in `StageTimelineConfigEditor.cs`.
- Runtime preview start/stop, scoped preview duration, GameResDB preload: start in `StageTimelineConfigViewer.cs` and `StageTimelinePreviewRuntime.cs`.
- Background preview: keep formal battles on `BattleStageBackgroundPresenter`; editor preview should create a transient child runtime from `StageTimelineConfigViewer`.
- Timeline bars or duration math: use `StageTimelineVisualSchedule.cs` before changing editor drawing.
- Wave spawn paths and queue-entry path editing: use `EnemyWaveConfig.cs` plus `StageTimelinePathRouteSceneHandles.cs`.
- MidBoss/MainBoss path phases: use `StageTimelineBossPathEdit` call sites and the relevant encounter config.
- Boss spell phase nested editing: use `StageTimelineBossSpellPhaseEdit.cs` and `StageTimelineEmbeddedConfigEditor.cs`.

## Rules

- Reserve `waveIndex` for actual `midStageWaves`; use `previewIndex` or scope-specific names for generic preview targets.
- Keep runtime battle code reading ScriptableObject configs, not ConfigViewer prefab state.
- Keep preview APIs editor-only behind `#if UNITY_EDITOR`.
- Do not add `Time.deltaTime` or wall-clock decisions to deterministic runtime systems; editor preview clocks may use existing `LogicFramePreviewClock`.
- Do not mount `BattleStageBackgroundRuntime` as a scene-maintained component.
- Keep runtime preview controls near the top of the Inspector, before heavier timeline/background/path editing sections.
- Avoid caching `SerializedProperty` fields in `OnEnable` unless they are used across calls; local `FindProperty` is fine for one-off section drawing.

## Validation

- Run `git diff --check` for changed files.
- Search for stale names after renames, especially `waveIndex`, preview field names, and serialized field names in `.unity` scenes.
- If Unity compilation cannot be run from shell, say so and ask the user to let Unity recompile.
- For editor-only changes, inspect the relevant Inspector/Scene behavior by code path: selected viewer state, dirty target, undo target, and repaint/restart path.
