## AssetManager

- 目标: 
  - PrefabEntity
  - Clone 如果有层级问题怎么办，其实克隆的时候，如果里面有资源，应该引用Guid,如果是实体，则应该将Child再克隆



- 渲染的时候要将坐标轴y反向,引入单元格像素



## Architecture

//From MurderEngine

- Core : 核心层
  - 数学
  - 渲染上下文: Camera2D, RenderContext
  - 场景框架 : Scene, GameScene, SceneLoader
  - 输入
  - 音频
- Data : 数据和资源层
  - 资源/存档/打包: GameDataManager, Packer/Save/Sound
  - 文件鱼序列化 : FileManager,PackedGameData
  - 游戏配置 : 运行参数与起始世界GameProfile
- Assets : 资产模型层
  - 世界/图形/输入/本地化等等资产定义
  - 由 Data 层加载、由 ECS/渲染层消费（例如 WorldAsset 引用系统与贴图集）。
- ECS层
- 游戏编排层（Runtime Orchestrator）
  - 主循环 Game ：输入注册、内容加载、窗口/帧率、场景装载与切换。
  - 起始场景：由 InitialScene 使用 GameProfile.StartingScene 决定。
- 诊断与工具层（Diagnostics/Utilities）
  - 日志/控制台命令 Diagnostics 、性能跟踪。
  - 工具方法集（颜色/矩阵/缓动/随机/序列化等）位于 Utilities* 源文件。
- 扩展点与桥接
  - 游戏接口 IMurderGame ：提供 Name、Options、ComponentsLookup 及生命周期回调。
  - 着色器提供者 IShaderProvider 。
  - 可覆盖的渲染上下文创建 Game.CreateRenderContext 。
    依赖方向（自下而上）

- 平台层 → Core（图形设备/窗口）
- Core ↔ Utilities（只向下使用 Utilities）
- Assets → Data（载入/保存）
- Data → Core/Utilities（渲染资源、文件 IO）
- ECS（World/Systems/Components）→ Core 与 Data（取资源、渲染/几何/输入）
- Runtime(Game) → Core + Data + ECS（编排与调度）
- Diagnostics/Tools 可被 Runtime/ECS/Core 引用，但应避免反向依赖