namespace Engine.Core.Input;

public interface IInputAccept
{
    void SubmitPointerFrame(in PointerFrame p);
}