---
name: th10-unity-stg-dev
description: 高效开发 TH10 Unity STG 弹幕项目。用于在本仓库中编辑玩法、ECS 系统、帧同步联机、Addressables 资源、ScriptableObject 配置、对象池、战斗流程、弹幕、敌人、UI 或 UTP 网络相关内容。
---

# TH10 Unity STG 开发

## 使用场景

当这个 Unity 项目的改动涉及以下内容时，使用此 Skill：

- STG/弹幕战斗逻辑、玩家/敌人行为、碰撞、发射器或关卡时间轴。
- `Assets/Scripts/ECS` 下的自制轻量 ECS + OOP 桥接架构。
- 2-4 人帧同步、输入收集、房间流程或 UTP 消息。
- Addressables、`GameResourceManifest`、`GameResDB`、ScriptableObject 配置资产或对象池。
- UI 面板、战斗准备流程或场景启动。

除非用户明确要求其他语言，否则始终使用简体中文回复。

## 项目地图

- `Assets/Scripts/ECS`：自制 ECS 核心。`World` 持有 `EntityManager`、`EntityFactory`、`GameObjectBridge`、`LogicFrameTimer` 和已注册的 `BaseSystem` 实例。
- `Assets/Scripts/BattlePart`：战斗入口、逻辑帧推进、战斗区域工具、玩家生成流程。
- `Assets/Scripts/SO`：基于 `GameConfig` 的 ScriptableObject 配置类。
- `Assets/Scripts/Resource`：Addressables 封装、清单加载、运行时索引化资源数据库。
- `Assets/Scripts/Pool`：`GameObjectPoolManager` 与 `IPoolable` 预制体复用。
- `Assets/Scripts/Network`：基于 UTP 的底层网络管理器和可序列化消息结构体。
- `Assets/Scripts/DanmakuWorld`、`Assets/Scripts/Enemy`、`Assets/Scripts/UI`：Editor/Viewer MonoBehaviour 辅助、敌人、UI 面板/条目。
- `Assets/Configs`：手工配置的 `*.asset` 数据，包括弹幕、发射器、关卡、池、角色、武器、敌人和 Manifest 配置。
- `Assets/Prefabs`：由 Manifest id 和对象池引用的预制体资产。

## 优先打开的文件

修改行为前，先阅读最接近的入口文件：

- 启动/资源初始化：`Assets/Scripts/GameLauncher.cs`、`Assets/Scripts/Resource/ResManager.cs`、`Assets/Scripts/Resource/GameResDB.cs`。
- 战斗循环/帧同步：`Assets/Scripts/BattlePart/BattleManager.cs`、`Assets/Scripts/BattlePart/LogicTickDriver.cs`。
- ECS 契约：`Assets/Scripts/ECS/World.cs`、`Assets/Scripts/ECS/Component/Components.cs`、`Assets/Scripts/ECS/System/`、`Assets/Scripts/ECS/Entity/EntityFactory.cs`。
- 表现桥接：`Assets/Scripts/ECS/Bridge/GameObjectBridge.cs`、`Assets/Scripts/ECS/Bridge/Updater/`。
- 弹幕：`Assets/Scripts/ECS/System/DanmakuSystem.cs`、`Assets/Scripts/ECS/System/DanmakuEmitSystem.cs`、`Assets/Scripts/SO/Danmaku/`。
- 关卡时间轴：`Assets/Scripts/ECS/System/StageTimelineSystem.cs`、`Assets/Scripts/SO/Stage/StageTimelineConfig.cs`、`Assets/Scripts/SO/Stage/EnemyWaveConfig.cs`、`Assets/Scripts/SO/Stage/BossPhaseConfig.cs`。
- 网络：`Assets/Scripts/Network/NetworkManager.cs`、`Assets/Scripts/Network/NetworkMessages.cs`、`Assets/Scripts/InputManager.cs`、`Assets/Scripts/RoomManager.cs`。
- 对象池：`Assets/Scripts/Pool/GameObjectPoolManager.cs`、`Assets/Scripts/SO/Pool/GlobalPoolConfig.cs`、`Assets/Scripts/SO/Pool/StagePoolConfig.cs`。
- UI 流程：`Assets/Scripts/UI/UIManager.cs`、`Assets/Scripts/UI/UI_Panel/`。

## 架构规则

- 将此项目视为自制 ECS，而不是 Unity DOTS。`EntityManager`、`Entity`、`IComponent` 和 `BaseSystem` 都是项目内类型。
- 将确定性的战斗行为放在 `BaseSystem.OnLogicTick(uint currentFrame)` 中。
- 尽量不要把 Unity `Transform`、预制体激活、UI 和视觉同步放入确定性逻辑。表现层使用 `OnUpdate`、`OnLateUpdate`、`GameObjectBridge` 和 `IGameObjectUpdater`。
- ECS 数据应作为实现 `IComponent` 的 `struct` 组件添加。遵循已有命名，例如 `CPosition`、`CVelocity`、`CDanmaku`、`CDanmakuEmitter`、`CPlayer`、`CEnemy`、`CCollider`。
- 使用 `CPoolGetTag`、`CPoolRecycleTag` 等标签组件，通过现有系统请求预制体创建/回收。
- 新系统按 `BattleManager.PerpareBattleWorld()` 所在位置和顺序模式注册。注意：系统顺序会影响碰撞、输入、关卡时间轴、子弹发射和表现。
- 对于帧同步，避免在 `OnLogicTick` 中使用非确定性逻辑：不要使用 `Time.deltaTime`、墙钟时间、Unity 物理回调、无序字典迭代来做玩法决策，也不要使用未同步的随机源。
- 如果战斗逻辑需要随机性，应绑定到战斗流程中已有的共享种子/帧/实体状态，而不是本地运行时状态。

## 资源与配置规则

- Addressable key 必须通过 `ResHelper.GetAddressableKey(E_ResourceCategory, string)` 生成。key 使用小写，并带有 `cfg_`、`prefab_`、`se_`、`tex_`、`atlas_`、`shader_` 等前缀。
- 运行时代码在初始化后应优先使用 `GameResDB` 索引访问，而不是在战斗中反复加载 Addressables。
- 新配置类型应继承 `GameConfig`。如果包含引用其他资源的字符串 id，应实现 `IReferenceResolver`，并在 `GameResDB.ResolveReferences()` 中将 id 解析为运行时索引。
- 在 `Assets/Configs` 或 `Assets/Prefabs` 下添加手工资产时，保持 id 与 `GameResourceManifest` 以及 `DM_`、`DME_`、`Character_`、`Weapon_`、`Minion_` 等已有命名约定一致。
- 关卡、波次、Boss 阶段、弹幕和发射器配置应优先通过 SO 数据表达，不要把可调数值硬编码进系统逻辑。
- 对象池预热由 `GlobalPoolConfig`、`StagePoolConfig` 和 prefab id 数据驱动。如果某个池化预制体会在战斗中出现，确保它存在于 Manifest 和池配置中，并有足够的预热数量。
- 避免添加直接的 `Resources.Load` 路径。本项目使用 Addressables 加 `GameResDB`。

## 多人联机与 UTP 规则

- 网络消息是在 `NetworkMessages.cs` 中实现 `INetworkMessage` 的结构体；每条消息都必须有 `MessageId`、`Serialize` 和 `Deserialize`。
- 保持序列化载荷紧凑且确定性。沿用现有代码风格，优先使用基础字段、byte、uint 帧 id、打包输入和固定字符串。
- 战斗帧推进由 `BattleManager.Update()` 控制：记录本地输入，在多人模式广播，等待所有活跃玩家输入，然后调用 `World.LogicTick(frameToProcess)`。
- 修改同步行为时，应同时检查 `InputManager`、`RoomManager`、`BattleManager` 和 `NetworkManager`。
- 除非用户明确要求，否则不要引入 Unity Netcode for GameObjects 模式；这里的网络基于 UTP 构建。

## 实现工作流

1. 识别子领域：战斗/ECS、关卡时间轴、资源/配置、网络、对象池、UI 或编辑器工具。
2. 编辑前先阅读“优先打开的文件”中最接近的入口文件。
3. 保留现有命名、单例、异步 `.Forget()`、日志和配置索引模式，除非改动需要有意识地迁移。
4. 保持改动范围收敛。不要重写无关 Unity 资产、`.meta` 文件、生成的项目文件或用户已有的未提交改动。
5. 代码编辑后，检查已触碰 C# 文件的 lints。如果仓库已有可用命令，再运行聚焦且 Unity 安全的编译/测试命令。
6. 如果手动编辑序列化 Unity 资产，要格外谨慎：优先修改代码/配置类，并在资产序列化可能需要 Unity Editor 验证时提醒用户。

## 常见任务模式

### 添加弹幕或发射器功能

- 阅读 `DanmakuConfig`、`DanmakuEmitterConfig`、`CDanmakuEmitter`、`DanmakuEmitSystem` 和 `DanmakuSystem`。
- 将编辑器配置字段添加到 SO 配置中，再把运行时友好的值烘焙到 ECS 组件里。
- 在可行时，让逐帧发射计算保持无分配。
- 如果该功能需要预制体，更新 Manifest/池配置预期，并检查 `DanmakuUpdater`。

### 添加关卡时间轴或敌人波次

- 阅读 `StageTimelineSystem`、`StageTimelineConfig`、`EnemyWaveConfig`、`BossPhaseConfig` 和 `EntityFactory`。
- 用 SO 配置描述时间、波次、Boss 阶段和敌人参数。
- 在 `OnLogicTick` 中按逻辑帧推进，不要依赖 `Time.time` 或场景对象状态作为战斗决策来源。
- 生成敌人、Boss 或弹幕实体时，通过 `EntityFactory` 创建，并添加必要的池化表现标签。

### 添加战斗实体

- 添加或复用 `GameConfig` 数据。
- 通过 `EntityFactory` 创建实体。
- 添加相关 ECS 组件和用于池化表现的 `CPoolGetTag`。
- 在 `BaseSystem` 中实现行为，并在战斗世界设置中注册。
- 只有当新预制体需要自定义视觉同步时，才添加 updater。

### 添加网络消息

- 添加一个 `MessageId`。
- 添加一个实现 `INetworkMessage` 的结构体。
- 按完全相同的顺序序列化和反序列化字段。
- 通过 `NetworkManager` 和相关玩法管理器接入处理逻辑。
- 对于会影响战斗的数据，按需包含帧/玩家身份。

### 添加 UI 面板

- 遵循 `Assets/Scripts/UI/UI_Panel` 中已有的 `UIPanel` 子类。
- 使用 `UIManager.ShowPanelAsync<T>()` 进行异步面板显示。
- 保持预制体命名和 Addressable Manifest id 与现有 UI 预制体约定一致。

## 最终回复清单

向用户汇报工作时，包含：

- 行为层面发生了什么变化。
- 触碰了哪些核心文件。
- 是否运行了 lints/测试/Unity 验证。
- 是否需要 Unity Editor 后续操作，例如刷新 Addressables、检查序列化资产或验证预制体/配置引用。
