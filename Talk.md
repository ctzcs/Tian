# 引擎讨论记录

本文件用于记录关于引擎的想法、讨论与决策，不涉及具体开发任务。

## 记录模板

请复制下方模板以新增一条记录：

```
日期：2026-01-16
主题：
背景：
讨论要点：
结论/决策：
跟进事项：
参考：
```

## 进行中的话题
- [ ] 占位：添加当前正在讨论的主题
 - [ ] UI 改进路线：样式/裁剪/组件库/资源管线

## 决策记录
- 2026-01-16 占位：记录已达成的决策
 - 2026-01-16 配置数据策略：阶段性不接入 Lua；采用 C# 类型化配置 + 外部数据文件；若需 Mod/脚本行为再评估
 - 2026-01-16 数据表策略：建议接入外部数据表（CSV/Excel→JSON→C# 模型），不引入运行时数据库；小项目可暂缓
 - 2026-01-16 UI策略：当前可做基础美观UI（九宫格、文字样式、布局），但“方便”程度有限；优先完善样式/主题系统、遮罩裁剪、基础组件库
 - 2026-01-16 第三方 UI 接入：当前“无必要”接入；满足触发条件再评估（见下）

## 问题收集 / 研究队列
- P0 路径与职责边界不清：Assets/Content/Build 语义混用，且 Runtime 不应写 meta/index
- P1 资产索引闭环不稳：Guid -> Path -> AssetsV1 规则未完全固化，路径一致性易出错
- P2 Content 分离未完全工程化：动态加载已建立，但构建链与产物保障仍需标准化

## 资产管线推进清单（P0 -> P1 -> P2）
- [x] P0 统一路径入口：Editor 管线只使用统一 Assets 路径服务，移除硬编码目录
- [x] P0 固化职责边界：Editor 生成 meta/index，Runtime 只读取
- [x] P1 固化 Index 规则：AssetIndex.Path 统一为相对资源根目录路径
- [x] P1 补齐运行时查询接口：LoadIndex/TryGetPath/TryGetRelativePath，并接入 AssetsV1 的 AssetId 读取
- [x] P2 固化构建链：Editor/Runner 构建前自动构建 Content 项目，保证 Content.dll 产出

## 术语与约定
- 术语：占位
- 约定：占位
 - 配置：纯数据，无副作用
 - 脚本：可执行逻辑与行为，需沙箱与权限控制
  - 数据表：以 CSV/Excel/Google Sheets 为源的结构化数据，离线导出供引擎使用
  - 主键/引用：每行唯一主键；字段可引用其他表主键，需校验

## 快速记录区
- 2026-01-16 占位：临时想法速记
- 2026-03-05 记录：自定义资源管理系统的分析与步骤
  - 背景：缺少独立的资源元数据/导入/打包流程，需建立可扩展的资源管理系统
  - 目标：支持源资源→导入→生成→打包→运行时加载的统一流程，编辑器与运行时一致
  - 要实现的核心模块
    - 资源注册与索引：资源唯一 ID、路径索引、类型映射、依赖关系图
    - 资源元数据：每种资源的导入配置、变更时间、版本、依赖与输出清单
    - 资源导入器：按类型/扩展名分发，执行导入、生成与验证
    - 资源打包器：将导入产物组织为运行时可直接加载的格式
    - 运行时加载器：按需加载、缓存策略、引用计数/生命周期
    - 热重载与增量：变更检测、最小化重建、编辑器通知与刷新
  - 实现步骤
    - 1) 定义资源基类与 AssetId 规范（GUID + 稳定路径），建立资源清单与索引结构
    - 2) 设计元数据模型（ImporterSettings、SourceHash、OutputFiles、Dependencies、Version）
    - 3) 实现导入器接口与注册机制（按扩展名/目录过滤，允许自定义导入器）
    - 4) 实现变更检测与增量导入（基于时间戳/Hash 的 ShouldRebuild）
    - 5) 设计输出目录结构与打包格式（按类型/模块分包，支持平台差异）
    - 6) 实现运行时资源加载 API（同步/异步、缓存与卸载）
    - 7) 资源验证与诊断（缺失/循环依赖/版本不匹配）
    - 8) 编辑器集成（导入进度、错误列表、强制重建、清理缓存）
  - 参考机制
    - Murder：ImporterSettingsAttribute + ResourceImporter + EditorDataManager 组织导入与保存
    - Unity：.meta 文件记录 GUID 与导入设置
    - Godot：导入器负责生成运行时资源
  - **Tian Engine 落地规划 (基于现有架构)**
    - **目录结构调整**
      - `Engine/Source/Asset/Pipeline/`: 存放管线核心接口
        - `AssetId.cs`: 封装 GUID
        - `AssetMeta.cs`: 序列化数据（Hash, ImporterID, Dependencies）
        - `IAssetImporter.cs`: 导入器接口
        - `AssetDatabase.cs`: 运行时/编辑器共享的索引数据库
      - `Editor/Source/Asset/Importers/`: 存放具体导入器（仅编辑器用）
        - `TextureImporter.cs`: 处理 png/jpg -> Texture
        - `AudioImporter.cs`: 处理 wav/ogg -> AudioBuffer
        - `ScriptImporter.cs`: 脚本处理（可选）
    - **关键类设计**
      - `AssetMeta` (JSON):
        ```csharp
        public class AssetMeta {
            public Guid Guid;
            public string ImporterId; // "TextureImporter"
            public long SourceHash;   // 变更检测
            public List<string> Dependencies;
            public Dictionary<string, object> ImporterSettings; // 导入参数
        }
        ```
      - `IAssetImporter` (Interface):
        ```csharp
        [ImporterAttribute(".png", ".jpg")]
        public abstract class AssetImporter {
            public abstract void Import(string sourceFile, string outputDir, AssetMeta meta);
            public virtual bool ShouldRebuild(string sourceFile, AssetMeta meta) { ... }
        }
        ```
      - `EditorAssetManager` (Editor Service):
        - 负责扫描 `Content/Assets`
        - 维护 `AssetDatabase` (Path <-> Guid)
        - 调度 `Import` 任务 (增量构建)
    - **数据流向**
      1. **Source**: `Content/Assets/image.png`
      2. **Meta**: `Content/Assets/image.png.meta` (生成/更新)
      3. **Import**: 读取 Source + Meta -> 处理 -> 输出到 `Library/Output/GUID.dat` (中间产物)
      4. **Pack**: 将 `Library/Output` 打包 -> `StreamingAssets/data.pak`
      5. **Runtime**: `ContentManager` 加载 `data.pak` -> 通过 GUID 索引资源

## TODO（UI改进）
- [ ] 样式/主题系统：状态样式与 Style Token、统一配色与间距
- [ ] 组件状态动画：Hover/Pressed/Disabled 的过渡与反馈
- [ ] 遮罩裁剪：层级 scissor/stencil，容器裁剪与子元素裁剪恢复
- [ ] 基础组件库：Image、Text、Panel/Window、ScrollView、InputField、Slider、Toggle、Dropdown
- [ ] 输入与焦点：IME 支持、键盘焦点、Tab 导航与可访问性
- [ ] 布局增强：Grid/Wrap 布局；现有 Auto/MIN/MAX/Grow 的规范化使用
- [ ] 渲染合批优化：按深度/组减少材质与纹理切换，提升批次利用
- [ ] UI 资源管线：专用 Atlas、九宫格规范、字体与本地化导出/热重载
- [ ] 调试可视化：样式/布局检查器、Overlay 信息面板扩展
- [ ] 文档与约定：样式命名规范、组件使用指南、素材切片标准

### 记录：是否有必要接入 Lua 用于配置数据
日期：2026-01-16
主题：是否有必要接入 Lua 用于配置数据
背景：当前配置主要通过 C# 代码/序列化数据完成，需评估是否引入脚本层
讨论要点：
- C# 优势：强类型、IDE 支持、易测试与重构；可用 JSON/CSV/YAML 作为外部数据
- C# 劣势：需编译发包；运行时热更新成本高；数据与行为易耦合
- Lua 优势：轻量、可热重载、适合策划/Mod；与引擎解耦
- Lua 劣势：需绑定层与桥接；类型安全薄弱；工具链与调试成本；维护两套语言
- 安全与确定性：脚本需沙箱与权限控制；配置应尽量无副作用
- 适用场景：热重载/Mod/策划编写行为/服务器热修复等强需求时
结论/决策：
- 现阶段不接入 Lua；采用 C# 类型化配置 + 外部数据文件（JSON/CSV）
- 配置严格限定为纯数据，行为在 C# 中实现
- 优先实现数据文件的热加载与校验，而非脚本层
- 若未来确有脚本需求，再评估 Lua 或 Roslyn C# Script，并明确沙箱与边界
跟进事项：
- 梳理配置域模型与 POCO、校验规则
- 选定数据格式与加载/校验/热重载机制
- 记录引入脚本的触发条件与安全边界
参考：待补充

### 记录：是否有必要接入数据表（CSV/Excel/Sheets）
日期：2026-01-16
主题：是否有必要接入数据表
背景：现有做法为 C# 类型化配置 + 外部数据文件；评估是否采用“表格为源”的数据管线
讨论要点：
- 定义：以 CSV/Excel/Sheets 为源，经导出为 JSON/二进制，反序列化到 C# POCO
- 优势：适合大量同构数据（物品、关卡、数值、文本）；策划易批量编辑；数据与行为解耦；便于校验与版本管理
- 成本：需建立导出/校验/引用解析工具链与规范（主键、枚举、引用、默认值）；纳入发布流程
- 风险：类型不一致导致运行时错误；表格公式/宏不可控；多源协作冲突
- 边界：仅存“纯数据”，不写执行逻辑；除非在线查询需求，不引入运行时数据库
结论/决策：
- 建议接入数据表作为外部数据来源；采用 CSV/Excel→JSON→C# 模型的路径
- 不引入运行时数据库（SQLite/SQL Server 等）；离线导出满足需求
- 若当前数据量小或迭代不频繁，可暂缓，仍遵循“配置只存数据”
跟进事项：
- 制定表结构规范（主键命名、引用、枚举、默认值）
- 选定导出格式与校验方案（JSON Schema/自定义校验）
- 实现数据热重载与引用完整性检查
- 规划本地化数据表与导出流程
参考：待补充

### 记录：游戏UI框架易用性与美观性
日期：2026-01-16
主题：游戏UI框架现在能否方便地做出好看的UI
背景：已有 UIElement/UILayoutGroup/UIDrawCommand/UIRoot/按钮等，需评估“易用性”和“美观产出”能力
现状能力：
- 布局：Row/Column、Padding/GAP、对齐、子元素 Grow、AutoWidth/AutoHeight、Viewport 比例定位
- 视觉：背景色/图片、原图/拉伸/适配/NineSlice、文字对齐/大小/颜色、换行/收缩适配/自动高度、布局动画 Tween、材质挂载
- 交互：Hit 测试、Button 事件（Enter/Exit/Hover/Click）、输入分发、鼠标滚轮冒泡、调试 Overlay
不足与风险：
- 样式系统缺失：未有主题/状态样式（Normal/Hover/Pressed/Disabled）与统一的 Style Token
- 裁剪未实现：maskable 未用于裁剪（缺少 scissor/stencil），复杂 UI 容器易溢出
- 组件不丰富：缺少常见控件（Image/Text 独立组件、ScrollView、InputField、Slider/Toggle/Dropdown、Panel/Window）
- 视觉细节：阴影、渐变、圆角、边框、描边等常用效果需材质或素材手动实现
- 资产与工具：UI 专用 Atlas/切图规范、九宫格素材约定、字体与本地化管线
结论/建议：
- 现阶段可做基础美观 UI，依赖九宫格素材与字体，但不算“方便”；复杂美术风格与大量控件将受限
- 优先路线：1）样式/主题系统（状态驱动+Style Token） 2）裁剪（scissor/stencil） 3）基础组件库 4）UI Atlas & 字体管线
参考实现线索：
- 样式：为 UIElement 增加状态机与 StyleSheet，Button 等控件按状态应用样式；集中定义颜色/间距/字体等 Token
- 裁剪：在渲染阶段按层级开启/恢复 scissor，或使用 stencil 区域遮罩，确保子元素不溢出
- 组件：拆分 Image/Text 组件；实现 ScrollView（内容容器+滚动条）；输入文本框需 IME/焦点管理
参考：Engine.UI 现有实现、Clay/Flex 布局参考、ImGui 状态主题启发

### 记录：与 Balatro UI 的对比
日期：2026-01-16
主题：相比 Balatro，当前 UI 的差距与方向
背景：Balatro 的 UI 以卡牌为核心，强调视觉识别度与“交互汁水（Juice）”
对比维度与现状：
- 视觉识别度：Balatro 使用统一色板、粗边/圆角、阴影/噪点纹理；当前有九宫格与材质挂载，但缺少统一主题与阴影/圆角等细节便利
- 微交互与反馈：Balatro 在 Hover/Click 有卡片抬起、缩放、弹性缓动与音效；当前仅布局 Tween 与基础事件，缺少属性级动画与 UI SFX
- 布局与容器：Balatro 常见列表、网格、滚动与弹出层；当前有 Row/Column 与 Auto 尺寸，但缺少 ScrollView/Panel/Window、裁剪
- 字体与数字表现：Balatro 的数字/得分突出，常用描边/阴影；当前文字溢出策略较全，但缺少描边/阴影与本地化字体管线
- 特效与动效：Balatro 有奖励粒子、光晕/高亮；当前可通过材质实现，但无现成组件与通用效果库
结论：
- 基础美观可达，想要“Balatro 级易用与质感”需补齐样式/裁剪/组件/动画与 SFX
改进路径（映射到 TODO）：
- 样式/主题系统与 Style Token（统一色板/圆角/边框/阴影）
- 裁剪（scissor/stencil）与容器控件（Panel/Window/ScrollView）
- 属性动画辅助（位置/尺寸/颜色/缩放/透明度），弹性缓动预设与事件绑定
- 文本描边/阴影（材质或双重绘制），本地化与字体 fallback
- UI SFX 与粒子效果：Hover/Click/奖励的声音与视觉反馈

### 记录：是否可以接入其他 C# UI 框架
日期：2026-01-16
主题：直接接入第三方 C# UI 的可行性
背景：希望复用成熟 UI 框架，提升易用性与视觉品质
选项与评估：
- 桌面 UI 叠加（WPF/WinForms/Avalonia）
  - 方式：单独开透明/无边窗口覆盖游戏窗口，或尝试嵌入句柄
  - 优势：控件丰富、工具成熟
  - 风险：窗口合成与输入同步、DPI/缩放、帧同步；跨平台受限（WPF/WinForms 仅 Windows）；与 SDL3/Foster 窗口生命周期耦合较重
- 游戏 UI 框架
  - NoesisGUI（XAML）：生产级、动画/样式强；需付费许可证与渲染桥接，接入成本中高
  - Myra UI（MonoGame/FNA）：开源轻量，可适配渲染器；需实现 Foster.Batcher 渲染适配与输入桥接，功能适中
  - ImGui（ImGui.NET）：适合工具，不擅长玩家 UI 的皮肤与动效；已用于 Editor，可用于 Debug/开发面板
- Web UI（WebView2/CEF → 纹理）
  - 优势：HTML/CSS 表达力强，跨团队易用
  - 风险：体量大、系统依赖、内存与集成复杂；不纯 C# 且跨平台受限
集成要点：
- 渲染桥：实现第三方 UI 绘制到 Foster 的 Batcher/Target，支持裁剪与九宫格/字体
- 输入桥：把 Foster.Input 事件映射到 UI 框架；管理焦点与 IME
- 生命周期：与 SDL3/Foster 的窗口/设备一致，避免消息泵冲突
结论/建议：
- 不建议“直接”嵌入桌面 UI 到游戏画面；可用于外部工具/编辑器或覆盖层原型
- 如需游戏内成熟 UI，优先评估 NoesisGUI（重但效果好）或 Myra（轻但需适配）
- 当前阶段更推荐完善自研 UI：样式、裁剪、组件与动效，可控且贴合引擎

触发条件（满足任意两项则进入评估）：
- 需求驱动：必须短期内上线 ScrollView/Window/输入框（IME/选择/光标）等复杂控件
- 视觉驱动：统一主题/阴影/圆角/描边与状态动画需求强烈，且素材/动效产线已就绪
- 效率驱动：自研 UI 在 2–4 周内无法完成上述能力或维护成本过高
- 跨平台限制：目标平台允许并需要外部 UI（如仅 Windows 用 WPF/Avalonia）
- 团队协作：策划/美术强依赖所见即所得工具（Noesis/XAML 或 Web）
