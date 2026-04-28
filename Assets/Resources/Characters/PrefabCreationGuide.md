# 预制体创建指南

由于预制体 GUID 需要 Unity 导入后自动生成，请在 Unity 中按以下步骤创建：

---

## 技能预制体

### 方法：使用菜单生成器

1. 在 Unity 菜单栏点击：`Echoes` → `Generate Skill Prefabs`
2. 等待生成完成
3. 预制体将自动创建在 `Assets/Resources/Characters/Skills/`

### 手动创建（如果生成器不工作）

1. 在 Project 窗口右键 → Create → Empty GameObject
2. 命名为 `Skill_TimeRewind`
3. 在 Inspector 点击 `Add Component`
4. 搜索并添加 `TimeRewindSkill`
5. 将 GameObject 拖到 `Assets/Resources/Characters/Skills/` 目录

重复以上步骤创建：
- `Skill_IceFreeze` + `IceFreezeSkill` 组件
- `Skill_BurstPush` + `BurstPushSkill` 组件

---

## 视觉预制体

### 使用菜单生成器

1. 在 Unity 菜单栏点击：`Echoes` → `Generate Visual Prefabs`
2. 等待生成完成

### 手动创建

1. 创建 Empty GameObject，命名为 `Visual_TimeKeeper`
2. 添加 `SpriteRenderer` 组件
3. 添加 `Animator` 组件
4. 保存为 Prefab

重复创建：
- `Visual_IceFreeze`（冰蓝色）
- `Visual_FireDancer`（橙红色）

---

## 关联到角色数据

1. 双击打开角色数据资产（如 `Character_001_TimeKeeper.asset`）
2. 在 Inspector 中找到 `Visual Prefab` 字段
3. 将对应的预制体拖入
4. 展开 `Skills` 数组，将技能预制体拖入 `Skill Prefab` 字段
5. 保存

---

## 测试

1. 运行游戏
2. 按 Tab 切换角色
3. 按 Q 使用技能