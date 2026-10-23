# sokoban-master

Unity 推箱子项目。开发方向与改造约定见[项目规范](.kiro/steering/api-spec.md)，具体实现和运行状态以工程与验证结果为准。

## 打开项目

1. 编辑器版本以 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 为准，当前记录为 **2022.3.9f1c1**。
2. 在 Unity Hub 中添加仓库根目录，即同时包含 `Assets/`、`Packages/` 和 `ProjectSettings/` 的目录。
3. 打开项目，等待 Package Manager 解析 [manifest.json](Packages/manifest.json) 中的依赖。
4. 打开 [MainMenuScene.unity](Assets/Scenes/MainMenuScene.unity)，从该场景开始检查播放流程；如有导入或编译错误，先保存 Console 日志再排查。

## 场景与代码入口

[构建场景配置](ProjectSettings/EditorBuildSettings.asset) 当前启用了以下场景，顺序如下：

| 顺序 | 场景 |
| --- | --- |
| 0 | [MainMenuScene](Assets/Scenes/MainMenuScene.unity) |
| 1 | [LevelScene](Assets/Scenes/LevelScene.unity) |
| 2 | [InnerLevel_Test](Assets/Scenes/InnerLevel_Test.unity) |

- [Assets/Scripts/](Assets/Scripts/)：基础脚本；可从 [MainMenuController.cs](Assets/Scripts/UI/MainMenuController.cs) 的开始游戏回调和 [PlayerMovementController.cs](Assets/Scripts/PlayerMovementController.cs) 的移动输入回调阅读。
- [Assets/Scripts_Echoes/](Assets/Scripts_Echoes/)：扩展脚本目录。

## 基础键盘操作

以下按键来自 [sokoban.inputactions](Assets/Resources/Player/sokoban.inputactions) 的 `Player` 动作映射，已与对应输入回调核对：

| 操作 | 按键 | 动作名 |
| --- | --- | --- |
| 向上移动 | W / ↑ | `MoveUp` |
| 向下移动 | S / ↓ | `MoveDown` |
| 向左移动 | A / ← | `MoveLeft` |
| 向右移动 | D / → | `MoveRight` |
| 撤销 | U | `Undo` |
| 重做 | R | `Redo` |
| 暂停 / 继续 | Esc | `Pause` |

移动回调见 [PlayerMovementController.cs](Assets/Scripts/PlayerMovementController.cs)，撤销与重做回调见 [GameController.cs](Assets/Scripts/GameController.cs)，暂停与继续回调见 [PauseMenuController.cs](Assets/Scripts/UI/PauseMenuController.cs)。

以上仅确认输入配置与源码中的对应关系；场景中的事件接线和实际按键效果仍需在 Unity 中验证。

## 依赖与验证状态

当前依赖清单包含 URP **14.0.8**、Input System **1.7.0** 和通过 Git 引用的 Unity MCP 包；完整配置见 [manifest.json](Packages/manifest.json)。

本文依据仓库中的版本文件、场景配置、依赖清单与源码整理。编辑器导入、编译、播放和构建均尚待验证，开发规范中的目标不等同于已完成的功能。
