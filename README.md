# TH10

基于 Unity 的纵向卷轴射击游戏（STG）项目，玩法与关卡设计参考东方 Project 系列。项目采用 **自研轻量 ECS + OOP 混合架构**，战斗逻辑在固定逻辑帧下运行，支持单机与基于 Unity Transport 的联机锁步对战。

## 项目简介

TH10 是一款数据驱动的弹幕射击游戏。核心战斗循环包括：玩家操控、武器与弹幕发射、敌人波次与 Boss 战、确定性 2D 碰撞（受击 / 拾取 / 擦弹）、掉落物与关卡时间轴推进。表现层通过 GameObject 桥接与对象池渲染，逻辑层与表现层分离，便于联机时保持确定性。

主要特性：

- **自研 ECS**：非 Unity DOTS，使用 struct 组件与 `BaseSystem.OnLogicTick` 驱动战斗逻辑（默认 60 FPS）
- **数据驱动配置**：ScriptableObject + `GameResDB` 索引，资源经 Addressables 加载
- **关卡时间轴**：`StageTimeline` 编排敌波、道中 Boss、关底 Boss 阶段与路径移动
- **弹幕系统**：可配置发射器（直线 / 扇形 / 波弹 / 粒弹等）与多种弹幕 prefab 池化
- **联机锁步**：UTP 传输 + 帧输入收集，全员输入就绪后推进逻辑帧；联机模式下表现层插值平滑
- **编辑器工具**：ConfigViewer、StageTimeline 预览与路径编辑等，便于在 Unity 内调参

## 技术栈

| 类别 | 选型 |
|------|------|
| 引擎 | Unity 2022.3 LTS |
| 战斗逻辑 | 自研 ECS（`Assets/Scripts/ECS/`） |
| 资源 | Addressables、`GameResourceManifest`、`GameResDB` |
| 网络 | Unity Transport（UTP）、锁步输入 |
| UI | UGUI、TextMesh Pro |
| 其他 | DOTween、IngameDebugConsole |

## 场景与流程

| 场景 | 说明 |
|------|------|
| `BootScene` | 启动与资源初始化 |
| `TitleScene` | 标题、菜单、房间与联机入口 |
| `BattleScene` | 战斗主场景 |

典型流程：启动加载配置与资源 → 标题/菜单选择模式 → 战斗准备 → `BattleManager` 创建 ECS 世界并注册各 System → 逻辑帧驱动战斗 → 结算返回。

## 目录结构（概要）

```
Assets/
├── Scripts/          # 游戏代码（ECS、战斗、配置、网络、UI 等）
├── Configs/          # ScriptableObject 配置资产
├── Prefabs/          # 池化预制体（敌人、弹幕、掉落物、UI 等）
├── Scenes/           # 场景
├── Art/              # 美术资源
└── AddressableAssetsData/
Packages/             # Unity 包依赖
ProjectSettings/      # 项目设置
```

核心代码入口可参考：

- 启动：`Assets/Scripts/GameLauncher.cs`
- 战斗：`Assets/Scripts/BattlePart/BattleManager.cs`
- ECS 世界：`Assets/Scripts/ECS/World.cs`
- 资源索引：`Assets/Scripts/Resource/GameResDB.cs`
- 网络：`Assets/Scripts/Network/NetworkManager.cs`

## 架构概览

```
SO(Config) ──GameResDB──► ECS(LogicTick 确定性) ──Tag──► PresentationSystem(LateUpdate)
                              ▲
                    InputManager 锁步（联机等全员输入）
```

- **逻辑帧**：`World.LogicTick` 在 `GameManager.logicFPS` 下推进，玩法状态仅在 `OnLogicTick` 中修改
- **表现层**：`GameObjectBridge` + 对象池；单机直接对齐逻辑坐标，联机使用 `PresentationMotion` 插值
- **配置**：运行时只读 `GameConfig` 索引；编辑器通过 `*ConfigViewer` 写回 `.asset`

## 开发与运行

1. 使用 **Unity 2022.3.62f3**（或同 LTS 小版本）打开项目根目录
2. 首次打开等待脚本编译与 Addressables 相关资源就绪
3. 从 `BootScene` 或 `TitleScene` 进入 Play 模式进行测试

> 本项目为毕业设计工程，部分功能仍在迭代中（如完整回放、部分擦弹逻辑等）。

## 许可证

未单独声明开源许可证；美术与第三方插件遵循各自授权（如 DOTween、IngameDebugConsole 等）。
