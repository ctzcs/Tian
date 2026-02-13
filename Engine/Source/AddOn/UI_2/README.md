UI_2 布局与容器使用说明
=====================

本文档简要说明 UI_2 布局系统里常用容器（`RowGroup`、`ColumnGroup` 等）和几个关键概念，重点回答：

- 什么时候自己写 `.WithSize(w, h)`，什么时候写 `0` 让它自适应？
- 如何让“容器根据子元素自动适应”？
- `Ui2ReorderableColumn` 这类高级控件怎么嵌在普通容器里？

术语说明
--------

- **UIElement**：所有 UI 控件的基类，包含 `Layout`、`Children` 等。
- **LayoutStyle**：布局数据结构，字段包括 `Width`、`Height`、`Grow`、`Shrink`、`AlignX` / `AlignY` 等。
- **RowGroup**：横向布局容器（类似水平 StackPanel）。
- **ColumnGroup**：纵向布局容器（类似垂直 StackPanel）。
- **GridGroup**：网格容器，按行列摆放元素，可固定或自动单元格大小。
- **SizeMode = Pixel**：宽高按像素计算（默认模式）。
- **SizeMode = ViewportRatio**：宽高按视口比例计算（用 `WithViewportRatio` 设置）。

宽高的基本规则
--------------

1. **默认行为（未设置 Width / Height）**

   - 如果 `Layout.Width == 0`，就认为“宽度由父容器给多少就用多少”。
   - 如果 `Layout.Height == 0`，就认为“高度由父容器给多少就用多少”。

   换句话说：**0 不代表“自适应子元素”，而是“占满父容器的可用空间”**（除非容器内部做了特殊测量）。

2. **显式设置宽高**

   使用 `WithSize(width, height)` 会直接写入 `Layout.Width` 和 `Layout.Height`：

   ```csharp
   element.WithSize(200f, 100f); // 宽高都固定
   element.WithSize(0f, 100f);   // 宽度由父容器决定，高度固定 100
   element.WithSize(200f, 0f);   // 高度由父容器决定，宽度固定 200
   ```

3. **最小 / 最大尺寸约束**

   - `MinWidth / MinHeight`：最小尺寸约束。
   - `MaxWidth / MaxHeight`：最大尺寸约束。

   一般不需要手动设置，做复杂弹性布局时可以使用。

RowGroup / ColumnGroup / GridGroup 的测量逻辑
--------------------------------------------

### ColumnGroup（纵向）

在 `ColumnGroup.Measure` 中：

- 会遍历所有可见的子元素：
  - 调用 `child.Measure(...)` 得到每个子元素的高度。
  - 累加高度 + 上下 margin + 容器的 `Gap`。
- 得到一个“内容高度”后，再加上 padding，作为 `ColumnGroup` 的内容高度。
- 如果自身 `Layout.Height > 0`，则直接使用该高度覆盖测量结果。

**重点：**

- 如果你希望 `ColumnGroup` 高度**刚好包住所有子元素**：
  - 不要给它显式设置 `Height`（`WithSize(任意宽, 0f)`）。
  - 子元素本身需要有明确的高度（比如文本行设置 `WithSize(0f, 行高)`）。

### RowGroup（横向）

类似 `ColumnGroup`，只是方向换成横向：

- 累加子元素的宽度 + 左右 margin + `Gap`。
- 得到内容宽度后，加上 padding 得到测量宽度。
- 如果自身 `Layout.Width > 0`，则用该宽度覆盖。

### GridGroup（网格）

`GridGroup` 用来做规则的网格布局，比如图标宫格、工具按钮面板等。核心字段：

- `Columns`：列数（默认为 1）。
- `Gap`：单元格之间的间距（像素）。
- `CellWidth` / `CellHeight`：单元格宽高（可固定，也可让它自动测量）。

测量规则：

1. 过滤出所有可见子元素。
2. 根据元素数量和 `Columns` 计算需要多少行。
3. 计算单元格大小：
   - 如果 `CellWidth` 和 `CellHeight` 都 **> 0**，直接使用这两个值；
   - 否则遍历所有子元素，调用 `child.Measure(...)`，取“最大宽度 / 最大高度 + margin”作为单元格大小。
4. 网格总宽高为：
   - `gridW = 列数 * cellW + (列数 - 1) * Gap`
   - `gridH = 行数 * cellH + (行数 - 1) * Gap`
5. 最终容器宽高 = `gridW / gridH + padding`，如果自身 `Layout.Width / Height > 0` 则直接覆盖。

排列规则（Arrange）：

- 先计算网格在容器内部的起点 `originX / originY`：
  - 使用 `Layout.AlignX` / `Layout.AlignY` 决定网格整体在内部区域是 Start / Center / End / Stretch。
- 再对每个子元素：
  - 根据索引算出所在行列 `(row, col)`；
  - 得到该单元格的左上角坐标；
  - 在单元格内部再根据 `AlignX / AlignY` 决定子元素的对齐方式（Start / Center / End / Stretch）。

直观理解：

- `AlignX / AlignY` 既影响“整块网格在容器里的位置”，也影响“子元素在单元格里的位置”；
- 不设置 `CellWidth / CellHeight` 时，会根据内容自动算一个“刚好够用”的单元格大小。

常见使用模式
------------

### 1. 顶层根容器占满屏幕

```csharp
var root = new ColumnGroup
{
    Gap = 8f
}
.WithPadding(24f, 24f, 24f, 24f)
.WithAlign(HorizontalAlignment.Start, VerticalAlignment.Start)
.WithViewportRatio(new Rect(0f, 0f, 1f, 1f)); // 宽高跟随视口

Canvas.Root.WithChild(root);
```

- 使用 `WithViewportRatio`，`root` 会覆盖整个视口区域。
- 里面再用 `RowGroup` / `ColumnGroup` 做正常布局。

### 2. 固定宽度 + 高度根据子元素自动适应

需求：一个竖直面板，宽度固定 200，高度由标题 + 列表内容决定。

```csharp
var panel = new ColumnGroup
{
    Gap = 4f
}
.WithSize(200f, 0f) // 宽 200，高度由内容测量
.WithBackgroundColor(new Color(0.12f, 0.12f, 0.16f, 1f));

var title = new UIText()
    .WithText("Directories")
    .WithTextColor(Color.White)
    .WithTextSize(14f)
    .WithTextAlign(new Vector2(0f, 0.5f))
    .WithSize(0f, 20f); // 明确给标题一个高度，便于 ColumnGroup 计算
```

这里的关键是：

- `panel` 的高度没有写死（`Height = 0`）。
- `title` 有固定行高。
- 后面的列表控件也要能正确测量自己的高度（见下一节）。

### 3. 列表容器 Ui2ReorderableColumn

`Ui2ReorderableColumn` 继承自 `ColumnGroup`，内部会为每一行创建一个 `RowGroup`：

```csharp
var list = new Ui2ReorderableColumn()
    .WithSize(200f, 0f); // 宽 200，高度由行数决定

string[] names = { "Assets", "Scenes", "Scripts" };
for (int i = 0; i < names.Length; i++)
    list.AddItem(names[i]);

panel.WithChildren(title, list);
```

在 `AddItem` 里，每一行是这样创建的（简化版）：

```csharp
var row = new RowGroup
{
    Gap = 4f
}
.WithSize(0f, 20f); // 每行 20 高
```

所以：

- 列表高度 = 行高 × 行数 + 间距。
- 面板高度 = 标题高度 + 列表高度 + `Gap` / margin / padding。

整体组合后，面板会根据子项自动适应高度，而不需要手动算像素。

### 4. 使用 GridGroup 做宫格布局

#### 4.1 自动单元格大小（根据子元素自适应）

```csharp
var grid = new GridGroup
{
    Gap = 8f,
    Columns = 3
}
.WithSize(0f, 0f) // 交给父容器决定整体区域
.WithAlign(HorizontalAlignment.Center, VerticalAlignment.Start);

for (int i = 0; i < 9; i++)
{
    var icon = new UIImage()
        .WithSize(48f, 48f); // 每个图标自己有固定宽高

    grid.WithChild(icon);
}
```

- 因为没有设置 `CellWidth / CellHeight`，`GridGroup` 会遍历所有图标，用它们的尺寸 + margin 计算出单元格大小。
- 网格宽高由图标大小 + 行列数决定。

#### 4.2 固定单元格大小（强制所有格子一样大）

```csharp
var grid = new GridGroup
{
    Gap = 4f,
    Columns = 4,
    CellWidth = 64f,
    CellHeight = 64f
}
.WithSize(0f, 0f)
.WithAlign(HorizontalAlignment.Center, VerticalAlignment.Center);
```

- 单元格大小完全由 `CellWidth / CellHeight` 决定，子元素只会在这个格子里面对齐 / 拉伸。
- 适合棋盘、物品栏这类严格固定大小的布局。

### 5. 容器填满父容器

如果你希望某个容器在父容器内填满可用空间：

```csharp
var fillPanel = new ColumnGroup
{
    Gap = 4f
}
.WithSize(0f, 0f); // 宽高都由父容器决定
```

这种情况下：

- 父容器在 `Arrange` 时会把自己的可用区域传给 `fillPanel`。
- `fillPanel` 的 `Measure` 会直接使用父容器给的尺寸。

### 6. 使用 Grow / Shrink 做弹性布局

`RowGroup` / `ColumnGroup` 中，对子元素：

- `Grow > 0`：有多余空间时，会按比例分配给这些元素。
- `Shrink > 0`：空间不足时，会按比例压缩这些元素。

示例：在一行中，左边按钮固定宽度，右边面板填充剩余空间。

```csharp
var row = new RowGroup { Gap = 8f }.WithSize(0f, 40f);

var leftButton = new Ui2Button()
    .WithSize(100f, 32f);

var rightPanel = new ColumnGroup()
    .WithGrow(1f); // Grow>0 => 吃掉剩余空间

row.WithChildren(leftButton, rightPanel);
```

何时用 0，何时用固定值？
------------------------

可以简单按以下经验来选：

1. **想让容器高度跟着子元素自动变化**  
   - 对容器：`WithSize(固定宽度, 0f)`，或者全部 0（交给父布局控制宽度）。
   - 对子元素：每个子元素需要有可预期的高度：
     - 纯容器：继续用 `ColumnGroup` / `RowGroup` 组合。
     - 文本：用 `WithSize(0f, 行高)` 给个行高。
     - 自定义控件：在内部 `AddChild` 的时候给明确的高度。

2. **想要一个固定大小的面板**  
   - 直接 `WithSize(固定宽度, 固定高度)`。

3. **想要“铺满父容器”**  
   - 用 `WithSize(0f, 0f)`，并注意父容器的 `AlignX` / `AlignY`：
     - 对 `ColumnGroup`，高度主要看内部测量结果；
     - 对 `RowGroup`，宽度主要看内部测量结果；
     - 但 Align 为 `Stretch` 时，会强制拉伸对应方向。

4. **想要占据视口百分比**  
   - 使用 `WithViewportRatio`：

   ```csharp
   element.WithViewportRatio(new Rect(xRatio, yRatio, wRatio, hRatio));
   ```

   例如 `new Rect(0f, 0f, 1f, 1f)` 表示覆盖整个视口。

调试布局的小技巧
----------------

- 给容器加背景颜色，方便看出它到底有多大：

  ```csharp
  panel.WithBackgroundColor(new Color(0.12f, 0.12f, 0.16f, 1f));
  ```

- 给每一行 / 每个块不同颜色，方便判断是 **容器高度不对** 还是 **子元素高度不对**。
- 注意：如果某个元素没有设置高度，又没有自定义 Measure，它很可能直接拿到一整个父容器的高度，导致“面板无限拉高”的现象。

UI 导航与手柄适配
----------------

目前 UI_2 主要是指针驱动（鼠标），为了适配手柄 / 键盘，可以使用一个简单的方向式导航辅助类 `Ui2Navigator`。

### 基本思路

- 给所有可交互控件设置 `Interactable = true`。
- 使用控件在屏幕上的矩形（`GetWorldRect()`）决定它们在空间中的相对位置。
- 当手柄方向键 / 摇杆触发时，根据方向在可交互控件中选择“下一个”元素作为焦点。
- 当手柄确认键触发时，对当前元素触发一次点击。

### Ui2Navigator 用法

`Ui2Navigator` 定义在 [UIElement.cs](file:///d:/MySpace/Github/Tian/Engine/Source/AddOn/UI_2/UIElement.cs)，核心接口如下：

- `new Ui2Navigator(UIElement root)`：传入某个根节点（通常是 `canvas.Root` 或某个面板）。
- `SetFocus(UIElement element)`：手动指定当前焦点。
- `MoveUp / MoveDown / MoveLeft / MoveRight()`：根据方向移动焦点。
- `ClickCurrent()`：对当前焦点元素触发一次 `OnClick`。

示例：在某个系统里接入手柄

```csharp
Ui2Navigator navigator;

void Init(UICanvas canvas)
{
    navigator = new Ui2Navigator(canvas.Root);

    var firstButton = canvas.Root.Hit(new Vector2(100f, 100f));
    if (firstButton != null)
        navigator.SetFocus(firstButton);
}

void UpdateFromGamepad(Input input)
{
    if (input.Gamepad.LeftPressed)
        navigator.MoveLeft();
    if (input.Gamepad.RightPressed)
        navigator.MoveRight();
    if (input.Gamepad.UpPressed)
        navigator.MoveUp();
    if (input.Gamepad.DownPressed)
        navigator.MoveDown();

    if (input.Gamepad.AcceptPressed)
        navigator.ClickCurrent();
}
```

导航规则：

- Navigation 会遍历 `root` 下所有子元素，收集满足以下条件的元素作为候选：
  - `Visible && Display == true`
  - `Interactable == true`
  - `GetWorldRect()` 的宽高都大于 0
- 对于当前焦点元素 `Current` 和某个方向向量（例如向下 `(0, 1)`）：
  - 只考虑在该方向上的元素（方向夹角不超过约 80°）；
  - 在这些元素中选择“方向最接近且距离最近”的一个作为下一个焦点。

这样：

- 对 `GridGroup` 或横竖排列的 `RowGroup`/`ColumnGroup`，方向键导航会比较自然；
- 不需要在每个控件上手动配置上下左右邻居；
- 需要更精细控制时，可以在后续扩展 `Ui2Navigator`（例如加显式邻接表）。

参考示例：DragDirectoryDemo
--------------------------

`Test/Content/Source/Test_UI2/DragDirectoryDemo.cs` 是一个综合示例，演示了：

- 顶层 `ColumnGroup` 充满视口；
- 左侧固定宽度列 + 右侧自适应高度的 Directories 面板；
- `Ui2ReorderableColumn` 的使用方式（可拖拽排序列表）。

可以从这里拷贝布局代码，在自己的 UI 里按需要做修改。
