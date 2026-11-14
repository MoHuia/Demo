# Unity 第三人称动作战斗 Demo

个人学习与求职演示。玩法参考《绝区零》，使用 Unity C# 实现角色移动、普攻连段、闪避、受击恢复和敌人攻击。部分实现与调试使用了 AI 辅助。角色、动作、场景、界面和音频等游戏素材来源于《绝区零》，相关权利归原权利人。

## 打开项目

1. 安装 Git LFS 和 Unity **2022.3.62f3**。
2. 克隆仓库并下载大型动画。
   ```sh
   git lfs install
   git clone https://github.com/MoHuia/Demo.git
   cd Demo
   git lfs pull
   ```
3. 在 Unity Hub 中添加克隆得到的 `Demo` 目录。首次打开时等待 Unity 导入资源和安装 Package Manager 依赖。
4. 打开 `Assets/Scenes/TrainingGround.unity`，点 Play 进入训练场。`Assets/Scenes/SixthStreet.unity` 可单独打开体验场景。

键盘鼠标操作为 WASD 移动、鼠标调整视角、左 Shift 闪避、鼠标左键攻击。
