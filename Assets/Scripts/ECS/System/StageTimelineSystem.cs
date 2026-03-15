//using System.Collections.Generic;
//using UnityEngine;

//public class StageTimelineSystem : BaseSystem
//{
//    StageTimelineConfig _config;
//    CStageState _stageState;

//    // 事件指针，避免每帧遍历列表
//    int _nextMidWaveIndex = 0;
//    bool _midBossSpawned = false;
//    bool _mainBossSpawned = false;
//    bool _stageCleared = false;

//    protected override void OnCreate()
//    {
//        // 1. 加载配置 (实际项目中通过 ResourceManager 或 Addressables 加载)
//        // 假设这里已经通过某种方式注入了 Config
//        _config = ResourceLoader.LoadStageConfig("Stage1_Normal");

//        // 2. 预处理：确保波次按帧数排序 (防止策划配错顺序)
//        _config.midStageWaves?.Sort((a, b) => a.startFrameOffset.CompareTo(b.startFrameOffset));

//        // 3. 初始化关卡状态
//        _stageState = new CStageState
//        {
//            currentState = E_StageState.MidStage,
//            stateEnterFrame = 0,
//            currentBossPhaseIndex = -1,
//            bossEntity = Entity.Null
//        };

//        // 将状态写入 ECS (如果是单例状态，也可以存在 System 内部，但写入 ECS 方便其他系统读取)
//        // 假设有一个全局单例实体 Entity.GlobalGame
//        EntityManager.SetComponent(EntityManager.GlobalGameEntity, _stageState);

//        _nextMidWaveIndex = 0;
//        _midBossSpawned = false;
//        _mainBossSpawned = false;
//    }

//    public override void OnLogicTick(uint currentFrame)
//    {
//        // 读取最新状态 (以防其他系统修改了状态，虽然通常只有本系统改)
//        _stageState = EntityManager.GetComponent<CStageState>(EntityManager.GlobalGameEntity);

//        // === 1. 检查结束条件 ===
//        if (!_stageCleared && currentFrame >= _config.maxStageFrames)
//        {
//            TriggerStageClear(currentFrame);
//            return;
//        }
//        if (_stageState.currentState == E_StageState.StageClear) return;

//        // === 2. 道中波次生成 (Mid-Stage Waves) ===
//        if (_stageState.currentState == E_StageState.MidStage)
//        {
//            ProcessMidStageWaves(currentFrame);

//            // 检查是否该刷中BOSS
//            if (!_midBossSpawned && _config.hasMidBoss && currentFrame >= _config.midBossSpawnFrame)
//            {
//                SpawnMidBoss(currentFrame);
//            }
//        }

//        // === 3. 主 BOSS 战逻辑 ===
//        if (_config.hasMainBoss && !_mainBossSpawned && currentFrame >= _config.mainBossSpawnFrame)
//        {
//            SpawnMainBoss(currentFrame);
//            return; // 本帧优先处理BOSS生成，后续逻辑下一帧再跑
//        }

//        if (_stageState.currentState == E_StageState.BossIntro || _stageState.currentState == E_StageState.BossFight)
//        {
//            ProcessBossLogic(currentFrame);
//        }
//    }

//    #region 波次处理逻辑

//    private void ProcessMidStageWaves(uint currentFrame)
//    {
//        if (_config.midStageWaves == null) return;

//        // 只遍历未触发的波次，一旦遇到未到的帧数就停止 (因为列表已排序)
//        while (_nextMidWaveIndex < _config.midStageWaves.Count)
//        {
//            var wave = _config.midStageWaves[_nextMidWaveIndex];

//            // 注意：startFrameOffset 是相对于关卡开始的绝对帧数
//            if (currentFrame >= wave.startFrameOffset)
//            {
//                ExecuteWave(wave);
//                _nextMidWaveIndex++;
//            }
//            else
//            {
//                break; // 后面的波次时间更晚，直接跳出
//            }
//        }
//    }

//    private void ExecuteWave(EnemyWaveConfig wave)
//    {
//        // 根据 wave.spawnPattern 生成敌人
//        // 这里调用一个工厂方法或发送命令
//        switch (wave.spawnPattern)
//        {
//            case SpawnPattern.Line:
//                SpawnEnemiesInLine(wave);
//                break;
//            case SpawnPattern.Grid:
//                SpawnEnemiesInGrid(wave);
//                break;
//                // ... 其他模式
//        }

//        Debug.Log($"[Timeline] Wave {_nextMidWaveIndex} spawned at frame {wave.startFrameOffset}");
//    }

//    #endregion

//    #region BOSS 逻辑

//    private void SpawnMidBoss(uint currentFrame)
//    {
//        _midBossSpawned = true;
//        // 1. 创建实体
//        Entity boss = EntityFactory.CreateBoss(_config.midBossPrefabId);

//        // 2. 设置登场运动轨迹
//        var moveComp = EntityManager.GetComponent<CMovement>(boss);
//        if (_config.midBossIntroMove != null)
//        {
//            ApplyMovementPattern(ref moveComp, _config.midBossIntroMove);
//        }

//        // 3. 标记状态 (可选：中BOSS期间暂停道中刷怪)
//        // _stageState.currentState = E_StageState.MidBossFight; 
//    }

//    private void SpawnMainBoss(uint currentFrame)
//    {
//        _mainBossSpawned = true;

//        // 1. 清空屏幕子弹 (典型 STG 逻辑)
//        EntityManager.DestroyAllWithComponent<CDanmaku>();

//        // 2. 创建 BOSS
//        Entity boss = EntityFactory.CreateBoss(_config.mainBossPrefabId);
//        _stageState.bossEntity = boss;
//        _stageState.currentState = E_StageState.BossIntro;
//        _stageState.stateEnterFrame = currentFrame;

//        // 3. 设置无敌和登场动画
//        var bossData = new CBossData { isInvincible = true, currentPhase = -1 };
//        EntityManager.AddComponent(boss, bossData);

//        var moveComp = EntityManager.GetComponent<CMovement>(boss);
//        // 应用登场轨迹 (如果没有配置，默认从上方飞入)
//        // ApplyMovementPattern(...)

//        EntityManager.SetComponent(EntityManager.GlobalGameEntity, _stageState);
//    }

//    private void ProcessBossLogic(uint currentFrame)
//    {
//        Entity bossEntity = _stageState.bossEntity;
//        if (!EntityManager.Exists(bossEntity))
//        {
//            // BOSS 意外死亡或消失
//            TriggerStageClear(currentFrame);
//            return;
//        }

//        // 1. 检查登场无敌时间结束
//        if (_stageState.currentState == E_StageState.BossIntro)
//        {
//            if (currentFrame - _stageState.stateEnterFrame >= _config.bossIntroDurationFrames)
//            {
//                _stageState.currentState = E_StageState.BossFight;
//                _stageState.stateEnterFrame = currentFrame;

//                // 移除无敌
//                ref var bossData = ref EntityManager.GetComponent<CBossData>(bossEntity);
//                bossData.isInvincible = false;
//                bossData.currentPhase = 0; // 进入第一阶段

//                EntityManager.SetComponent(EntityManager.GlobalGameEntity, _stageState);
//                Debug.Log("[Timeline] Boss Fight Start!");
//            }
//            return;
//        }

//        // 2. 管理 BOSS 阶段 (Phases)
//        if (_stageState.currentState == E_StageState.BossFight)
//        {
//            ref var bossData = ref EntityManager.GetComponent<CBossData>(bossEntity);
//            var hpComp = EntityManager.GetComponent<CHealth>(bossEntity); // 假设有血条组件

//            // 简单逻辑：根据血量切换阶段
//            // 实际项目中可能需要更复杂的条件 (时间、特定攻击结束等)
//            int targetPhase = GetPhaseByHp(hpComp.currentHp, _config.bossPhases);

//            if (targetPhase > bossData.currentPhase)
//            {
//                bossData.currentPhase = targetPhase;
//                // 触发阶段转换事件 (例如：全屏炸弹、回血、改变弹幕模式)
//                OnBossPhaseChange(bossEntity, targetPhase);
//            }

//            // 检查 BOSS 死亡
//            if (hpComp.currentHp <= 0)
//            {
//                OnBossDefeated(currentFrame);
//            }
//        }
//    }

//    private int GetPhaseByHp(float currentHp, List<BossPhaseConfig> phases)
//    {
//        if (phases == null || phases.Count == 0) return 0;

//        float maxHp = phases[0].maxHp; // 假设第一个配置包含总血量或单独定义
//        float hpPercent = currentHp / maxHp;

//        // 倒序查找，找到第一个满足血量条件的阶段
//        for (int i = phases.Count - 1; i >= 0; i--)
//        {
//            if (hpPercent <= phases[i].triggerHpPercent)
//                return i;
//        }
//        return 0;
//    }

//    private void OnBossPhaseChange(Entity boss, int phaseIndex)
//    {
//        Debug.Log($"[Timeline] Boss entered Phase {phaseIndex}");
//        // 这里可以发送事件，让 BossAISystem 切换到新的攻击模式
//        // 或者直接在 Boss 实体上替换 CDanmakuEmitter 的配置
//    }

//    private void OnBossDefeated(uint currentFrame)
//    {
//        Debug.Log("[Timeline] Boss Defeated!");
//        // 播放爆炸特效
//        // 延迟几帧后进入结算
//        TriggerStageClear(currentFrame);
//    }

//    #endregion

//    #region 工具方法

//    void TriggerStageClear(uint currentFrame)
//    {
//        _stageCleared = true;
//        _stageState.currentState = E_StageState.StageClear;
//        EntityManager.SetComponent(EntityManager.GlobalGameEntity, _stageState);

//        // 清理剩余敌人
//        // EntityManager.DestroyAllWithComponent<CEnemy>();

//        // 触发 UI 显示结算
//        // EventSystem.Raise(new StageClearEvent());
//    }

//    void ApplyMovementPattern(ref CMovement move, MovementPatternData data)
//    {
//        // 将配置数据转换为组件数据
//        move.speed = data.speed;
//        move.patternType = (MovementPatternType)data.type; // 需要对应枚举
//        // ... 复制其他字段
//    }

//    #endregion
//}