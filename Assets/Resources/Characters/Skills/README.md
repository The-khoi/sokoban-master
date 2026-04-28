# 技能预制体创建指南

由于预制体需要 Unity 导入后自动生成 GUID，请在 Unity 中按以下步骤创建：

## 方法 1：使用生成器（推荐）

1. 在 Unity 中打开菜单：`Echoes` → `Generate Skill Prefabs`
2. 等待生成完成
3. 预制体将自动创建在 `Assets/Resources/Characters/Skills/` 目录

## 方法 2：手动创建

### 1. 创建 Skill_TimeRewind 预制体

1. 在 Project 窗口中右键 → Create → Empty GameObject
2. 命名为 `Skill_TimeRewind`
3. 添加组件 → 搜索 `TimeRewindSkill`（如果找不到，先确保脚本已编译）
4. 将 GameObject 拖到 `Assets/Resources/Characters/Skills/` 目录
5. Unity 会自动生成 `.prefab` 文件

### 2. 创建 Skill_IceFreeze 预制体

1. 创建 Empty GameObject，命名为 `Skill_IceFreeze`
2. 添加组件 → 搜索 `IceFreezeSkill`
3. 配置属性：
   - Freeze Range: 1.5
4. 保存为 Prefab

### 3. 创建 Skill_BurstPush 预制体

1. 创建 Empty GameObject，命名为 `Skill_BurstPush`
2. 添加组件 → 搜索 `BurstPushSkill`
3. 配置属性：
   - Push Distance: 3
4. 保存为 Prefab

## 关联到角色数据

创建完预制体后，在角色数据资产的 Inspector 中：
1. 展开 `Skills` 数组
2. 将预制体拖到 `Skill Prefab` 字段

## 技能脚本位置

- `Assets/Scripts_Echoes/Skills/TimeRewindSkill.cs` - 时间回溯
- `Assets/Scripts_Echoes/Skills/IceFreezeSkill.cs` - 冰封固化
- `Assets/Scripts_Echoes/Skills/BurstPushSkill.cs` - 爆裂推进