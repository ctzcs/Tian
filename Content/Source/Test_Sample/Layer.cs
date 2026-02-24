namespace Content.Test;

//最多32层
public enum ELayer
{
    Lowest = 0,
    Line = 1 << 1,
    Building = 1 << 2,
    Frog = 1 << 3,
}

public static class ELayerExt
{
    public static int GetId(this ELayer layer)=> (int) layer;
}