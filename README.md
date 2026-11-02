# 背包消除 Demo（架构设计线）

一个可操作、可重新开始的消除类 Demo 关卡，演示「交换 → 匹配消除 → 清除背包 → 下落连锁 → 胜利」的完整主流程。本项目为 **架构设计线** 二测交付，核心目标是展示清晰的模块边界与主流程，而非美术精度或完整商业化功能。

- 答题方向：架构设计线（二测沿用一测方向）
- 引擎：Unity 6000.6.7f1（Universal 2D 模板）
- 语言：C#
- 状态：规则层已验证；表现层多数结论待 Unity Play Mode 人工核验（见「当前状态」）

## 玩法概览

- 棋盘 **9 列 × 8 行**，坐标自左上角起，行向下、列向右。
- 交换相邻棋子形成同色匹配，匹配命中的**上下左右相邻背包**被清除；本关卡目标是**清空全部 33 个背包**。
- 特殊匹配生成三类道具：**炸弹**（5×5 范围）、**火箭**（整行/整列）、**螺旋桨**（局部 + 自选一个背包）。
- 消除后垂直下落、斜向补位、顶部补充，并可继续连锁。
- 背包归零即胜利，提供重新开始；Demo 不设步数/时间失败。

## 架构设计

规则与表现严格分离：

- **规则层**（`Assets/Scripts/Rules/`）—— 纯 C# 程序集，`noEngineReferences: true`，不依赖 UnityEngine、场景对象与动画。
  - `Board/`：棋盘数据、坐标、单元、对象编号、颜色与道具方向。
  - `Config/`：关卡配置、ASCII 解析、校验、`LevelCatalog`（第二关 + 推荐路线）。
  - `Legality/`：交换评估与合法交换枚举。
  - `Matching/`：直线/方块匹配识别。
  - `Simulation/`：整轮结算（`TurnSolver`、`PropEffects`、`GravitySolver`、`MatchGroups`）、类型化结算步骤（`SimulationStep`/`StepKind`）、会话/步骤隔离（`SessionGate`）、版本化回放（`ReplayLog`）。
- **表现层**（`Assets/Scripts/Presentation/`）—— 只提交请求、消费结算步骤，不直接修改棋盘和目标数量。
  - `BoardController`：输入、忙碌锁定、结算消费、重开、自动演示、回放。
  - `BoardView`（及 `.Playback`/`.Hud`/`.Scenery`）：渲染、拾取与动画。
  - `GameBootstrap`：启动装配。

**数据流**：输入请求 → 规则层 `TurnSolver.Resolve` 产出结算步骤 → `SessionGate` 校验会话/步骤 → 视图消费步骤做动画 → 控制器推进权威 `BoardState`。`BoardState` 是唯一权威棋盘状态。

## 目录结构

```
GameExam/
├─ 题面.md                       # 二测题目
├─ 背包消除Demo开发计划书_架构设计线_提交版.md   # 开发计划书（玩法/规则基线）
├─ CLAUDE.md                     # 项目开发与检查规范
├─ UnityDemo/                    # Unity 工程
│  ├─ Assets/Scripts/Rules/      # 规则层（纯 C#）
│  ├─ Assets/Scripts/Presentation/  # 表现层
│  ├─ Assets/Tests/EditMode/     # EditMode 固定局面测试
│  ├─ Assets/Editor/GameExamBuild.cs  # 构建入口
│  ├─ Tools/Build.ps1            # 构建 / EditMode 执行脚本
│  └─ Docs/                      # 开发状态、验收与代码审查记录
└─ README.md
```

## 构建与运行

### 环境与版本

- Unity **6000.6.7f1**（revision `b22767f04f04`），Universal 2D 模板。
- 关键依赖：Universal RP `17.6.0`、Input System `1.20.1`、UGUI `2.6.0`、Test Framework `1.8.0` 与 2D 全家桶（完整清单见 `UnityDemo/Packages/manifest.json`）。

### 程序集划分（`.asmdef`）

| 程序集 | 位置 | 说明 |
| --- | --- | --- |
| `BagElimination.Rules` | `Assets/Scripts/Rules/` | 纯 C# 规则层，`noEngineReferences: true`，零引用 |
| `BagElimination.Presentation` | `Assets/Scripts/Presentation/` | 表现层，引用 Rules + Input System + UGUI |
| `BagElimination.Tests.EditMode` | `Assets/Tests/EditMode/` | 仅 Editor，引用 Rules + 测试运行器，`nunit.framework.dll` |

### 打开与运行

用 Unity Hub 打开 `UnityDemo/`，进入 Play Mode 即可运行演示关卡。唯一启用的构建场景是 `Assets/Scenes/SampleScene.unity`（guid `8c9cfa26abfee488c85f1582747f6a02`），已在 Build Settings 勾选。

### 命令行构建（Windows）

构建入口为 [GameExamBuild.cs](UnityDemo/Assets/Editor/GameExamBuild.cs) 的 `GameExamBuild.Windows()`（菜单 `GameExam/Build Windows Release`），经 `Tools/Build.ps1` 以批处理方式调用：

```powershell
# 默认输出 Builds/Windows/GameExam.exe，随后自动打包 zip + sha256
.\UnityDemo\Tools\Build.ps1
```

- 目标平台固定为 `StandaloneWindows64`，使用 Build Settings 中所有 `enabled` 场景。
- 输出路径可用环境变量 `GAMEEXAM_BUILD_OUTPUT` 覆盖（默认 `Builds/Windows/GameExam.exe`）。
- 构建后校验 `GameExam.exe`、`GameExam_Data/`、`UnityPlayer.dll` 三件套是否齐全。
- 自动生成 `GameExam-Windows-x64-<时间戳>.zip` 与同名 `.sha256`，并写入 `请先阅读.txt` 说明免安装运行与操作；打包时排除 Unity 的 `_BackUpThisFolder_ButDontShipItWithYourGame` 与 `_BurstDebugInformation_DoNotShip` 调试目录。
- `Build.ps1` 的 `-UnityPath` 默认为 `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.com`，本机路径不同时用该参数覆盖。

### 命令行测试

```powershell
# 运行 EditMode 测试（batchmode + runTests，结果输出到 Builds/Logs/editmode.xml 与 editmode.log）
.\UnityDemo\Tools\Build.ps1 -TestOnly
```

### 运行验证参数

打包产物或直接在编辑器下运行时，可传命令行参数驱动验收（由 `GameBootstrap` / `BoardController` 读取）：

| 参数 | 作用 |
| --- | --- |
| `-verify-route` | 自动演示六步推荐路线 |
| `-no-animation` | 跳过动画，直接渲染每步快照（与 `-verify-route` 连用做逐步比对） |
| `-second-level` | 切换到第二关配置（`LevelCatalog.Second()`） |

> 注意：批处理构建 / 测试前需先关闭 Unity 编辑器，否则工程被占用会使 Unity 以非零退出码失败（`Build.ps1` 会报错）。构建入口已就绪，但历史上被 Unity 许可证 / 编辑器占用阻断，暂未产出可运行包（见 `Docs/开发状态.md`）。

## 测试

- 规则层固定局面测试位于 `Assets/Tests/EditMode/`，覆盖对象编号唯一、掉落/补位、特殊匹配与道具、死局恢复、会话隔离、回放一致等。
- 当前证据：独立 NUnit 运行器 **102 通过、0 失败**；运行时与构建入口独立编译 **0 错误**（该数字来自独立运行器/程序集引用编译，**非 Unity Test Runner / Play Mode 验收**）。

## 当前状态

任务 T01—T13、验收 A01—A13 的状态与执行证据详见 `Docs/开发状态.md`。要点：

- 规则层（纯 C#）已通过固定局面测试验证；表现层（动画/UI/胜利）多数结论**待 Unity Play Mode 人工核验**。
- 已知最大交付风险：Unity 正式 EditMode / Play Mode / 构建包验收因许可证与编辑器占用尚未执行。
- 最新代码审查见 `Docs/代码审查-2026-11-02.md`。

## 文档索引

- `Docs/开发状态.md` — 任务与验收（A01—A13）状态索引
- `Docs/代码审查-2026-11-02.md` — 最新代码审查
- `Docs/A06-A13修复与验收-2026-11-01.md` — 结算契约/回放/第二配置/构建的修复与验收
- `Docs/实现过程与验证记录-2026-10-31.md`、`Docs/动画优化记录-2026-10-31.md`、`Docs/视觉与动画优化记录-2026-11-01.md`、`Docs/UI修复记录-2026-11-01.md`、`Docs/背景与胜利优化记录-2026-11-01.md` — 实现与优化记录

## AI 参与说明

规则实现、固定局面测试、独立编译/运行器验证、代码审查与文档均为 AI 辅助完成；人工复核尚未进行。误报处理与复核方式见各代码审查记录的「较上一轮的判定」章节。