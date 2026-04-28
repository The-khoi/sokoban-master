# 《幻匣迷踪：心域回响》美术资产规范说明书

> **适用对象**：美术同学 / 动画同学  
> **最后更新**：2026-05-05  
> **对应 Unity 版本**：2022.3.9 LTS

---

## 目录

1. [Visual 预制体层级结构规范](#1-visual-预制体层级结构规范)
2. [动画参数命名标准](#2-动画参数命名标准)
3. [快速更新美术资产流程](#3-快速更新美术资产流程)
4. [文件命名与目录规范](#4-文件命名与目录规范)
5. [常见问题 FAQ](#5-常见问题-faq)

---

## 1. Visual 预制体层级结构规范

### 1.1 核心设计原则

游戏采用**视觉层与逻辑层分离**的架构。角色的物理碰撞、移动逻辑全部在 `Player`（根对象）上，美术资产只存在于 `Visual` 子对象中。

**切换角色时，只有 Visual 子对象会被销毁和重建，Player 根对象永远不会被销毁。**

这意味着：美术同学只需要关心 `Visual_XXX.prefab` 这一层，不需要接触任何逻辑脚本。

---

### 1.2 运行时层级结构

```
Player (根对象，逻辑层 — 美术不需要修改)
├── [Collider2D]
├── [Movable 脚本]
├── [Player 脚本]
│
└── VisualAnchor (空 Transform，挂载点)
    └── Visual  ← ⭐ 这就是你的 Visual_XXX.prefab 实例化后的位置
        ├── [SpriteRenderer]   ← 挂在根节点
        └── [Animator]         ← 挂在根节点
```

> **重要**：`SpriteRenderer` 和 `Animator` 必须挂在 Visual 预制体的**根节点**上，不能放在子对象里。程序会通过 `GetComponent<Animator>()` 直接从根节点获取，放在子节点会导致动画系统失效。

---

### 1.3 Visual 预制体内部结构

每个角色对应一个 `Visual_[角色名].prefab`，存放在：

```
Assets/Resources/Characters/Visuals/
├── Visual_TimeKeeper.prefab    ← 时光守望者
├── Visual_IceFreeze.prefab     ← 凛冬静
└── Visual_FireDancer.prefab    ← 焰舞红
```

**预制体根节点必须包含的组件：**

| 组件 | 必须 | 说明 |
|------|------|------|
| `SpriteRenderer` | ✅ 必须 | 挂载角色精灵图，Sorting Order 设为 **2** |
| `Animator` | ✅ 必须 | 关联对应角色的 AnimatorController，无动画时留空也可 |

**SpriteRenderer 配置要求：**

| 属性 | 要求值 | 说明 |
|------|--------|------|
| Sorting Order | `2` | 确保角色渲染在地面（Order 0）和箱子（Order 1）之上 |
| Sprite | 关联对应 PNG | 见第 3 节 |
| Color | 各角色有默认色调 | 见下表，可在 Inspector 中覆盖 |

**各角色默认色调（程序自动生成时使用）：**

| 角色 | CharacterId | 默认颜色 | RGBA 值 |
|------|-------------|----------|---------|
| 时光守望者 TimeKeeper | 1 | 蓝色 | `(0.6, 0.8, 1.0, 1.0)` |
| 凛冬静 IceFreeze | 2 | 冰蓝半透明 | `(0.7, 0.9, 1.0, 0.9)` |
| 焰舞红 FireDancer | 3 | 橙红色 | `(1.0, 0.6, 0.4, 1.0)` |

> 如果你提供了完整的角色精灵图，可以将 Color 改回 `(1, 1, 1, 1)` 纯白，让精灵图本身的颜色直接显示。

---

### 1.4 特效子对象（可选）

如果角色有技能特效，可以在 Visual 预制体下添加子对象，但**不影响根节点的 SpriteRenderer 和 Animator**：

```
Visual_IceFreeze.prefab
├── [SpriteRenderer]          ← 根节点，必须
├── [Animator]                ← 根节点，必须
└── FreezeParticles (子对象)  ← 可选，特效粒子系统
```

---

## 2. 动画参数命名标准

### 2.1 参数命名总表

以下参数名已硬编码在程序中，**大小写必须完全一致**，不可修改。

#### 移动类参数（由程序自动驱动，美术只需在 AnimatorController 中创建同名参数）

| 参数名 | 类型 | 取值范围 | 触发时机 | 用途 |
|--------|------|----------|----------|------|
| `MoveX` | `Float` | `-1.0` / `0.0` / `1.0` | 每次移动时 | 水平方向（-1 左，1 右） |
| `MoveY` | `Float` | `-1.0` / `0.0` / `1.0` | 每次移动时 | 垂直方向（-1 下，1 上） |
| `IsMoving` | `Bool` | `true` / `false` | 移动开始/结束 | 控制行走动画开关 |

> **程序行为说明**：每次玩家移动时，程序先将 `IsMoving` 设为 `false`，再更新 `MoveX`/`MoveY`，最后将 `IsMoving` 设为 `true`。动画师可以用 `MoveX`/`MoveY` 做四方向混合树（Blend Tree）。

---

#### 技能类参数（由技能脚本驱动）

| 参数名 | 类型 | 对应技能 | 触发时机 |
|--------|------|----------|----------|
| `UseIceFreeze` | `Bool` | 冰封固化（凛冬静专属） | 技能执行时设为 `true` |
| `UseBurstPush` | `Bool` | 爆裂推进（焰舞红专属） | 技能执行时设为 `true` |

> **注意**：技能 Bool 参数由程序设为 `true` 后，需要动画师在 AnimatorController 中自行处理"播放完毕后归位"的逻辑（例如在技能动画的 Exit 状态中用 Behaviour 脚本将其设回 `false`，或使用 Trigger 类型替代）。

> **时间回溯技能（时光守望者）**：该技能目前无专属动画参数，如需添加，参数名约定为 `UseTimeRewind`，类型 `Bool`。

---

### 2.2 AnimatorController 推荐结构

```
[Any State]
    │
    ├─ IsMoving = true  ──→  [Walk Blend Tree]
    │                            ├─ MoveX=-1, MoveY=0  → 向左走
    │                            ├─ MoveX=1,  MoveY=0  → 向右走
    │                            ├─ MoveX=0,  MoveY=1  → 向上走
    │                            └─ MoveX=0,  MoveY=-1 → 向下走
    │
    ├─ IsMoving = false ──→  [Idle]
    │
    ├─ UseIceFreeze = true ──→  [IceFreeze Skill Anim]  ──→  [Idle]
    │
    └─ UseBurstPush = true ──→  [BurstPush Skill Anim]  ──→  [Idle]
```

---

### 2.3 无动画时的处理

如果某个角色暂时没有动画资产，**不需要做任何特殊处理**。程序在获取不到 AnimatorController 时会静默跳过，不会报错。Visual 预制体上的 `Animator` 组件留空即可。

---

## 3. 快速更新美术资产流程

### 3.1 整体流程图

```
① 替换 PNG 文件
   Assets/Art/char_[角色名]_[动作].png
        ↓
② 在 Unity 中设置 Sprite 导入参数
        ↓
③ 将 Sprite 赋值给 Visual 预制体的 SpriteRenderer
        ↓
④ 运行 Sync 工具（菜单 Echoes → Pipeline → Sync Character Assets）
        ↓
⑤ 验证游戏内表现
```

---

### 3.2 第一步：替换 PNG 文件

将新的角色精灵图放入以下目录：

```
Assets/Art/
├── char_timekeeper_idle.png      ← 时光守望者（已有）
├── char_timekeeper_walk_right.png
├── char_icefreeze_idle.png       ← 凛冬静（待补充）
└── char_firedancer_idle.png      ← 焰舞红（待补充）
```

**文件命名约定**：`char_[角色英文名小写]_[动作名].png`

> 直接覆盖同名文件即可，Unity 会自动检测文件变化并重新导入。

---

### 3.3 第二步：设置 Sprite 导入参数

在 Unity Project 面板中选中 PNG 文件，在 Inspector 中确认以下设置：

| 属性 | 推荐值 | 说明 |
|------|--------|------|
| Texture Type | `Sprite (2D and UI)` | 必须 |
| Sprite Mode | `Single`（单帧）或 `Multiple`（序列帧） | 根据实际情况 |
| Pixels Per Unit | `100` | 与关卡网格对齐（1 格 = 1 Unity 单位 = 100px） |
| Filter Mode | `Point (no filter)` | 像素风格推荐，防止模糊 |
| Compression | `None` 或 `Low Quality` | 开发阶段推荐 None |

设置完成后点击 **Apply**。

---

### 3.4 第三步：将 Sprite 赋值给 Visual 预制体

1. 在 Project 面板中找到对应的 Visual 预制体：  
   `Assets/Resources/Characters/Visuals/Visual_[角色名].prefab`

2. 双击打开预制体（进入 Prefab Mode）

3. 选中根节点，在 Inspector 中找到 `SpriteRenderer` 组件

4. 将 PNG 文件（或切割后的 Sprite）拖入 `Sprite` 槽位

5. 点击左上角 **Save** 保存预制体

---

### 3.5 第四步：运行 Sync 工具

在 Unity 菜单栏执行：

```
Echoes → Pipeline → Sync Character Assets
```

**工具会自动完成以下操作：**

- ✅ 检查所有 `CharacterData.asset` 是否有对应的 Visual 预制体，没有则自动创建
- ✅ 检查所有技能配置是否有对应的 Skill 预制体，没有则自动创建
- ✅ 将生成的预制体引用自动写回 `CharacterData`，无需手动拖拽
- ✅ 保存所有修改

**工具执行完毕后会弹出对话框**，显示新建了多少个资产。Console 窗口也会有详细日志。

> **幂等性保证**：重复运行 Sync 工具是安全的，已存在的资产不会被覆盖。

---

### 3.6 第五步：验证

进入 Play Mode，按 `Tab` 键切换角色，确认：

- [ ] 角色精灵图正确显示
- [ ] 角色切换时 UI 头像更新
- [ ] 移动时动画正常播放（如有 AnimatorController）
- [ ] 技能动画正常触发（如有配置）

---

### 3.7 仅更新 AnimatorController（不换图）

如果只是更新动画，不需要换图：

1. 在 Project 面板中创建或修改 `AnimatorController`（推荐存放在 `Assets/Resources/Characters/Visuals/` 同级目录）
2. 打开对应的 Visual 预制体，将 `Animator` 组件的 `Controller` 槽位关联新的 AnimatorController
3. 保存预制体
4. **不需要**重新运行 Sync 工具

---

## 4. 文件命名与目录规范

### 4.1 目录结构总览

```
Assets/
├── Art/                                    ← 美术源文件（PNG 原图）
│   └── char_[角色名]_[动作].png
│
└── Resources/
    └── Characters/
        ├── Character_001_TimeKeeper.asset  ← 角色数据（程序配置，美术不需要修改）
        ├── Character_002_IceFreeze.asset
        ├── Character_003_FireDancer.asset
        │
        ├── Visuals/                        ← ⭐ 美术主要工作目录
        │   ├── Visual_TimeKeeper.prefab
        │   ├── Visual_IceFreeze.prefab
        │   └── Visual_FireDancer.prefab
        │
        └── Skills/                         ← 技能预制体（程序自动生成，美术不需要修改）
            ├── Skill_TimeRewind.prefab
            ├── Skill_IceFreeze.prefab
            └── Skill_BurstPush.prefab
```

### 4.2 命名规则速查

| 资产类型 | 命名格式 | 示例 |
|----------|----------|------|
| 角色精灵图（源文件） | `char_[英文名小写]_[动作].png` | `char_timekeeper_idle.png` |
| Visual 预制体 | `Visual_[角色名PascalCase].prefab` | `Visual_TimeKeeper.prefab` |
| AnimatorController | `Anim_[角色名PascalCase].controller` | `Anim_TimeKeeper.controller` |
| 技能特效预制体 | `FX_[技能名].prefab` | `FX_IceFreeze.prefab` |

---

## 5. 常见问题 FAQ

**Q：我替换了 PNG，但游戏里角色还是旧图？**  
A：检查 Visual 预制体的 SpriteRenderer 的 Sprite 槽位是否已更新。直接覆盖 PNG 文件后，Unity 会重新导入，但 Sprite 引用不会自动更新到预制体，需要手动拖拽赋值（见第 3.4 步）。

---

**Q：运行 Sync 工具后，CharacterData 的 Visual 引用还是空的？**  
A：确认 Visual 预制体的文件名格式为 `Visual_[CharacterName].prefab`，其中 `CharacterName` 必须与 `CharacterData` 中的 `characterName` 字段完全一致（大小写敏感）。

---

**Q：动画参数设置了，但技能动画不触发？**  
A：检查两点：① 参数名是否与第 2.1 节的表格完全一致（区分大小写）；② 参数类型是否正确（`Bool` 不能用 `Trigger`）。可以在 Play Mode 中打开 Animator 窗口实时观察参数值变化。

---

**Q：我想给角色添加受击动画，参数名应该叫什么？**  
A：目前程序中没有受击参数，可以与程序同学协商后添加。约定命名格式为 `OnHit`（Bool 类型）。添加后需要在 `Skill.cs` 的 `TriggerAnimation()` 方法中调用。

---

**Q：Visual 预制体可以有多个 SpriteRenderer 吗（比如身体和武器分层）？**  
A：可以，但主 SpriteRenderer（用于朝向判断）必须在**根节点**上。子对象可以有额外的 SpriteRenderer 用于武器、特效等分层渲染。

---

**Q：Sorting Order 设为 2 会不会和其他物体冲突？**  
A：当前层级约定：地面 = 0，箱子 = 1，角色 = 2，UI 特效 = 3+。如有冲突请与程序同学确认。

---

*文档由 Kiro 自动生成，如有疑问请联系程序同学。*
