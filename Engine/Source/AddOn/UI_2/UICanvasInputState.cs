using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public sealed class UICanvasInputState
{
    private UIElement? lastHovered;
    private UIElement? lastPressed;
    private readonly List<UIElement> hitBuffer = new();
    private List<UIElement>? lastPressedChain;
    private UIElement? pressTarget;
    private Vector2 lastPointerPosition;
    private bool hasLastPointer;

    public UIElement? DebugHovered => lastHovered;
    public bool HasPointerCapture => lastPressedChain != null && pressTarget != null;

    public bool Update(UIElement root, Rect? clipRect, Vector2 pointerPosition, bool leftPressed, bool leftReleased)
    {
        bool hadPointerCapture = HasPointerCapture;
        hitBuffer.Clear();
        root.HitAll(pointerPosition, hitBuffer);

        var over = hitBuffer.Count > 0 ? hitBuffer[0] : null;
        var moved = !hasLastPointer || pointerPosition != lastPointerPosition;

        if (clipRect.HasValue && !clipRect.Value.Contains(pointerPosition))
        {
            hitBuffer.Clear();
            over = null;
        }

        if (over != lastHovered)
        {
            if (lastHovered != null)
            {
                var exitEvent = new Ui2PointerEvent
                {
                    Target = lastHovered,
                    Current = lastHovered,
                    Position = pointerPosition
                };
                lastHovered.OnPointerExit?.Invoke(exitEvent);
            }

            if (over != null)
            {
                var enterEvent = new Ui2PointerEvent
                {
                    Target = over,
                    Current = over,
                    Position = pointerPosition
                };
                over.OnPointerEnter?.Invoke(enterEvent);
            }

            lastHovered = over;
        }

        if (moved)
        {
            if (lastPressedChain != null && pressTarget != null)
            {
                for (int i = 0; i < lastPressedChain.Count; i++)
                {
                    var element = lastPressedChain[i];
                    var moveEvent = new Ui2PointerEvent
                    {
                        Target = pressTarget,
                        Current = element,
                        Position = pointerPosition
                    };

                    element.OnPointerMove?.Invoke(moveEvent);

                    if (!element.PointerPassThrough)
                        break;
                }
            }
            else if (over != null)
            {
                for (int i = 0; i < hitBuffer.Count; i++)
                {
                    var element = hitBuffer[i];
                    var moveEvent = new Ui2PointerEvent
                    {
                        Target = over,
                        Current = element,
                        Position = pointerPosition
                    };

                    element.OnPointerMove?.Invoke(moveEvent);

                    if (!element.PointerPassThrough)
                        break;
                }
            }
        }

        if (leftPressed && hitBuffer.Count > 0)
        {
            pressTarget = hitBuffer[0];
            lastPressedChain = new List<UIElement>(hitBuffer);

            for (int i = 0; i < hitBuffer.Count; i++)
            {
                var element = hitBuffer[i];
                var downEvent = new Ui2PointerEvent
                {
                    Target = pressTarget,
                    Current = element,
                    Position = pointerPosition
                };

                element.OnPointerDown?.Invoke(downEvent);
                lastPressed = element;

                if (!element.PointerPassThrough)
                    break;
            }
        }

        if (leftReleased && lastPressedChain != null)
        {
            var currentHits = new List<UIElement>(hitBuffer);

            for (int i = 0; i < lastPressedChain.Count; i++)
            {
                var element = lastPressedChain[i];
                var upEvent = new Ui2PointerEvent
                {
                    Target = pressTarget ?? element,
                    Current = element,
                    Position = pointerPosition
                };

                element.OnPointerUp?.Invoke(upEvent);

                if (currentHits.Contains(element))
                    element.OnClick?.Invoke(upEvent);

                if (!element.PointerPassThrough)
                    break;
            }

            lastPressedChain = null;
            lastPressed = null;
            pressTarget = null;
        }

        lastPointerPosition = pointerPosition;
        hasLastPointer = true;
        return hadPointerCapture || hitBuffer.Count > 0;
    }

    public void Block(Vector2 pointerPosition)
    {
        if (lastHovered != null)
        {
            var exitEvent = new Ui2PointerEvent
            {
                Target = lastHovered,
                Current = lastHovered,
                Position = pointerPosition
            };
            lastHovered.OnPointerExit?.Invoke(exitEvent);
            lastHovered = null;
        }

        lastPointerPosition = pointerPosition;
        hasLastPointer = true;
    }
}