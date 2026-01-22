UI的数据层如果直接来自World，岂不是很爽。

## 如何使用
- 简单来说
  - 顶层 / 独立 Panel ：
    - 要固定像素 → 写 Rect 。
    - 要随屏幕变化 → SizeMode = ViewportRatio + NormalizedRect 。
  - Group 里的孩子 ：
      - 宽高 → 写 Rect 的 Size、Ratio、Grow、Min/Max。
      - 位置 → 交给 Group，别自己管。
  - 普通父子绝对布局 ：
      - 用 Rect 写绝对坐标；
      - 想随父缩放/移动 → 再加 (X/Y/Width/Height) Ratio 。

    
1. 顶层面板 / HUD 面板（相对整个逻辑屏幕）

   - 目标：类似「左上角 10% 宽、30% 高的 panel」。
   - 建议：
       - 想用 绝对像素 ：
           - `SizeMode = Pixel`
           - 直接设 Rect = new Rect(x, y, w, h) （逻辑坐标）
       - 想用 相对屏幕 （类似 Unity anchor / 你之前说的 viewport）：
           - SizeMode = ViewportRatio
           - 配置 NormalizedRect = new Rect(nx, ny, nw, nh)
               - nx, ny ：基于逻辑屏幕的起点比例
               - nw, nh ：宽高比例（如果你希望固定宽高，可以让这两个为 0，然后通过 Rect 或 Min/Max 控制）
           - 不要直接改 TargetRect，Viewport 布局会在 ApplyViewportLayout 里算好。
2. LayoutGroup 的子元素（Row / Column 布局里）

   - 目标：一行/一列里面的按钮、卡牌等。
   - 布局规则已经是：Group 负责 位置 ，子元素负责 自己的宽高 。
   - 推荐写法：
       - 子元素的 Rect：
           - 常见： new Rect(0, 0, w, h)
               - X/Y 对 Group 来说意义不大，最后位置由 Group 决定。
               - 宽高作为“基础尺寸”，再叠加 Grow/Ratio 等。
       - 想要随父一起伸缩：
           - 用 WidthRatioToParent/HeightRatioToParent/.WithWidthRatioToParent(0.3f)
       - 想按权重吃掉多余空间（类似 flex-grow）：
         - child.GrowX = 1f; （Row） 或 child.GrowY = 1f; （Column）
     - Group 会在 innerW/innerH 剩余空间里按 Grow 比例分配。
     - 想限制子元素的尺寸：
       - 用 MinWidth/MaxWidth/MinHeight/MaxHeight ：child.MinWidth = 200; child.MaxWidth = 400;

3. 普通 UIElement 直接挂在某个父 Panel 下面（非 Group 布局）

   - 目标：比如一个 Panel 上面的角标图标、角上的关闭按钮。
   - 你可以用两种方式：

   - 绝对局部坐标 ：

       - Rect = new Rect(x, y, w, h) （相对于父元素左上）
   - 相对父尺寸的 Anchor 风格 ：
       - XRatioToParent / YRatioToParent 控制起点位置：`child.XRatioToParent = 1.0f;/child.YRatioToParent = 0.0f;`
