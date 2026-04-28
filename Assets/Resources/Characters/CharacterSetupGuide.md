# 角色数据配置指南

## 角色与技能关联

由于预制体 GUID 需要在 Unity 导入后自动生成，请在 Unity 编辑器中按以下步骤配置：

### 步骤 1：打开角色数据

在 Project 窗口中找到以下文件并双击打开：
- `Assets/Resources/Characters/Character_001_TimeKeeper.asset`
- `Assets/Resources/Characters/Character_002_IceFreeze.asset`
- `Assets/Resources/Characters/Character_003_FireDancer.asset`

### 步骤 2：关联技能预制体

在 Inspector 中找到 `Skills` 数组，展开每个元素：

**Character_001_TimeKeeper (星野未来)**
```
Skill Type: TimeRewind (1)
Skill Prefab: 拖入 Assets/Resources/Characters/Skills/Skill_TimeRewind.prefab
Unlocked By Default: ✓
Custom Energy Cost: 1
```

**Character_002_IceFreeze (凛冬静)**
```
Skill Type: IceFreeze (2)
Skill Prefab: 拖入 Assets/Resources/Characters/Skills/Skill_IceFreeze.prefab
Unlocked By Default: ✓
Custom Energy Cost: 2
```

**Character_003_FireDancer (焰舞红)**
```
Skill Type: BurstPush (10)
Skill Prefab: 拖入 Assets/Resources/Characters/Skills/Skill_BurstPush.prefab
Unlocked By Default: ✓
Custom Energy Cost: 2
```

### 步骤 3：关联视觉预制体（可选）

如果已有视觉资源，在 `Visual Prefab` 字段中拖入：
- `Assets/Resources/Characters/Visuals/Visual_TimeKeeper.prefab`
- `Assets/Resources/Characters/Visuals/Visual_PhaseWalker.prefab`
- `Assets/Resources/Characters/Visuals/Visual_BurstPusher.prefab`

### 步骤 4：保存并测试

1. 保存场景
2. 运行游戏
3. 按 Tab 键切换角色
4. 按 Q 键使用技能

---

## 技能说明

| 角色 | 技能 | 按键 | 能量消耗 | 效果 |
|------|------|------|----------|------|
| 星野未来 | 时间回溯 | Q | 1格 | 撤销最近3步操作 |
| 凛冬静 | 冰封固化 | Q | 2格 | 将动态箱子转为静态地形 |
| 焰舞红 | 爆裂推进 | Q | 2格 | 箱子直线滑行直至撞墙 |

---

## 故障排除

如果技能无法使用，请检查：
1. EnergyManager 是否有能量（最大3格）
2. 技能预制体是否正确挂载到 Player 下
3. 控制台是否有错误信息