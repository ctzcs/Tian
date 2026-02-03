using System.Numerics;
using Engine.Asset;
using Engine.Components;
using Engine.Core;
using Foster.Framework;
using Cursor = Engine.Core.Input.Cursor;

namespace Engine.UI;

public class UIRoot
{
	private float time = 0f;
    private Input input;
    private Vector2 logicScreen;
    private UIElement root;
    
    private UIElement? lastOver;
    
    private UiFrame lastFrame;
    private UiFrame currentFrame;
    
    
    List<UIElement> _primaryInputFocusListeners = new();
    List<UIElement> _secondaryInputFocusListeners = new();
    private readonly List<UIDrawCommand> _drawCommands = new();

    public UIElement Root => root;

    public UIElement? DebugLastOver => lastOver;

    public bool IsOpen
    {
        get;
        set
        {
            root.Visible = value;
            root.Selectable = value;
            root.Interactable = value;
            field = value;
        }
    }

    public UIRoot(App app,Vector2Int logicScreen)
    {
	    this.input = app.Input;
	    this.logicScreen = logicScreen;
	    root = new UIElement(new Rect(0, 0, 0, 0));
	    lastFrame = new UiFrame();
	    currentFrame = new UiFrame();
    }
    
    public void Update(float deltaTime)
    {
		time += deltaTime;
        root.Update(time);
        ApplyViewportLayout();
        UpdateInputMouse();

        if (lastOver is IInputListener listener)
            listener.OnPointerHover(currentFrame);
    }

    public void Render(Batcher batcher)
    {
        if (!IsOpen) return;
        _drawCommands.Clear();
        root.CollectDrawCommandsAsRoot(_drawCommands);
        UIDrawCommandRenderer.Render(_drawCommands, batcher);
    }

    void ApplyViewportLayout()
    {
        var viewport = new Rect(0, 0, logicScreen.X, logicScreen.Y);
        foreach (var child in root.Children)
            ApplyViewportLayoutRecursive(child, viewport);
    }

    void ApplyViewportLayoutRecursive(UIElement element, Rect parentRect)
    {
        if (element.SizeMode == UISizeMode.ViewportRatio)
        {
            var nr = element.NormalizedRect;
            var layoutRect = element.TargetRect;

            var x = parentRect.X + parentRect.Width * nr.X;
            var y = parentRect.Y + parentRect.Height * nr.Y;

            float w = layoutRect.Width;
            float h = layoutRect.Height;

            bool autoWidth = false;
            bool autoHeight = false;

            if (element is UILayoutGroup group)
            {
                var cfg = group.Layout;
                autoWidth = cfg.AutoWidth;
                autoHeight = cfg.AutoHeight;
            }

            if (!autoWidth && nr.Width > 0f)
                w = parentRect.Width * nr.Width;
            if (!autoHeight && nr.Height > 0f)
                h = parentRect.Height * nr.Height;

            element.SetTargetRect(new Rect(x, y, w, h));
        }

        var childParentRect = element.TargetRect;
        foreach (var child in element.Children)
            ApplyViewportLayoutRecursive(child, childParentRect);
    }


    void UpdateInputMouse()
    {
	    /*var lastState = input.LastState;
	    var currentState = input.State;*/
	    
	    lastFrame.CopyFrom(currentFrame);
	    currentFrame.inputState = input.State;
	    
	    //TODO 这里的坐标转换，需要改成窗口无关

	    var screenPos = currentFrame.Mouse.Position;
	    var viewport = Engine.Core.Input.Cursor.ViewportPosition; //CameraUtils.ScreenToViewport(screenPos, window);
	    var pos = 
		    CameraUtils.ViewportToLogicScreen(
			    viewport,logicScreen);
        currentFrame.targetPosition = pos;
	    UpdateInputPoint(lastFrame,currentFrame,ref lastOver);
    }
    
    void UpdateInputPoint(UiFrame lastState,UiFrame curState,
        ref UIElement lastOver)
    {
	    var over = root.Hit(curState.targetPosition);
        //阻挡鼠标影响GameWorld位置
        Cursor.IsOnGameUi = over != null;
	    var inputPress = curState.Mouse.LeftPressed;
	    var inputRelease = curState.Mouse.LeftReleased;
	    var inputMoved = curState.targetPosition != lastState.targetPosition;
	    var secondaryInputPress = curState.Mouse.RightPressed;
	    var secondaryInputRelease = curState.Mouse.RightReleased;
		var overChanged = over != lastOver;
	    //鼠标进入
	    //鼠标离开
	    //鼠标点击
	    //鼠标释放
	    //鼠标移动
	    //鼠标滚轮
	    //鼠标悬停
	    if (over != null) HandleMouseWheel(over,curState);
	    if (inputPress) UpdatePrimaryInputDown(curState, over);
	    if (secondaryInputPress) UpdateSecondaryInputDown(curState, over);
		// 除了指针移动，如果当前指向的元素变化原应该能触发Pointer事件
	    if (inputMoved || overChanged) UpdateInputMoved(curState, over, ref lastOver, inputMoved); 
	    if (inputRelease) UpdatePrimaryInputReleased(curState);
	    if (secondaryInputRelease) UpdateSecondaryInputReleased(curState);
	    lastOver = over;
    }
    
    /// <summary>
	/// Mouse or touch is down this frame.
	/// </summary>
	/// <param name="inputPos">location of cursor</param>
	/// <param name="over">element under cursor</param>
	void UpdatePrimaryInputDown(UiFrame state,UIElement over)
	{
		// lose keyboard focus if we click outside of the keyboardFocusElement
		/*if (_keyboardFocusElement != null && over != _keyboardFocusElement)
			SetKeyboardFocus(null);*/

		// if we are over an element and the left button was pressed we notify our listener
		if (over is IInputListener listener)
		{
			//var elementLocal = over.StageToLocalCoordinates(inputPos);
		
			// add the listener to be notified for all onMouseDown and onMouseUp events
			if (listener.OnPointerDown(state) && !_primaryInputFocusListeners.Contains(over))
				_primaryInputFocusListeners.Add(over);
		}
	}


	/// <summary>
	/// Mouse or touch is down this frame.
	/// </summary>
	/// <param name="inputPos">location of cursor</param>
	/// <param name="over">element under cursor</param>
	void UpdateSecondaryInputDown(UiFrame state,UIElement over)
	{
		// lose keyboard focus if we click outside of the keyboardFocusElement
		/*if (_keyboardFocusElement != null && over != _keyboardFocusElement)
			SetKeyboardFocus(null);*/

		// if we are over an element and the left button was pressed we notify our listener
		if (over is IInputListener listener)
		{
			if (listener.OnRightPointerDown(state) && !_secondaryInputFocusListeners.Contains(over))
				_secondaryInputFocusListeners.Add(over);
		}
	}


	/// <summary>
	/// Mouse or touch is being moved.
	/// </summary>
	/// <param name="inputPos">location of cursor</param>
	/// <param name="over">element under cursor</param>
	/// <param name="lastOver">element that was previously under the cursor</param>
	void UpdateInputMoved(UiFrame state,UIElement over,ref UIElement lastOver, bool inputMoved)
	{
		if (inputMoved)
		{
			for (var i = _primaryInputFocusListeners.Count - 1; i >= 0; i--)
				((IInputListener)_primaryInputFocusListeners[i]).OnPointerMoved(state);
			for (var i = _secondaryInputFocusListeners.Count - 1; i >= 0; i--)
				((IInputListener)_secondaryInputFocusListeners[i]).OnPointerMoved(state);
		}

		if (over != lastOver)
		{
			(over as IInputListener)?.OnPointerEnter(state);
			(lastOver as IInputListener)?.OnPointerExit(state);
		}
	}


	/// <summary>
	/// Mouse or touch is being released this frame.
	/// </summary>
	/// <param name="state"></param>
	void UpdatePrimaryInputReleased(UiFrame state)
	{
		for (var i = _primaryInputFocusListeners.Count - 1; i >= 0; i--)
			((IInputListener)_primaryInputFocusListeners[i]).OnPointerUp(state);
		_primaryInputFocusListeners.Clear();
	}

	/// <summary>
	/// Right mouse click or touch is being released this frame.
	/// </summary>
	/// <param name="inputPos">location under cursor</param>
	void UpdateSecondaryInputReleased(UiFrame state)
	{
		for (var i = _secondaryInputFocusListeners.Count - 1; i >= 0; i--)
			((IInputListener)_secondaryInputFocusListeners[i]).OnRightPointerUp(state);
		_secondaryInputFocusListeners.Clear();
	}


	/// <summary>
	/// bubbles the onMouseScrolled event from mouseOverElement to all parents until one of them handles it
	/// </summary>
	/// <returns>The mouse wheel.</returns>
	/// <param name="mouseOverElement">Mouse over element.</param>
	void HandleMouseWheel(UIElement mouseOverElement,UiFrame curstate)
	{
		// bail out if we have no mouse wheel motion
		if (curstate.Mouse.Wheel.Y == 0)
			return;

		// check the deepest Element first then check all of its parents that are IInputListeners
		var listener = mouseOverElement as IInputListener;
		if (listener != null && listener.OnMouseScrolled(curstate))
			return;

		while (mouseOverElement.Parent != null)
		{
			mouseOverElement = mouseOverElement.Parent;
			listener = mouseOverElement as IInputListener;
			if (listener != null && listener.OnMouseScrolled(curstate))
				return;
		}
	}
	
	
	
}