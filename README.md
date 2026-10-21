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

## 依赖与验证状态

当前依赖清单包含 URP **14.0.8**、Input System **1.7.0** 和通过 Git 引用的 Unity MCP 包；完整配置见 [manifest.json](Packages/manifest.json)。

本文依据仓库中的版本文件、场景配置、依赖清单与源码整理。编辑器导入、编译、播放和构建均尚待验证，开发规范中的目标不等同于已完成的功能。
