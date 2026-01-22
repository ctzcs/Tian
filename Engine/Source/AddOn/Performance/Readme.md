# Tracy
1. 来源https://github.com/clibequilibrium/Tracy-CSharp 0.11.1,需要server和client对应
2. 这里使用的是Tracy工具库，用于进行性能分析
3. 具体使用方法可以参考官方文档




```csharp


    //Log
    Profiler.AppInfo("...");
    
    //
    public void Render()
    {
        //Code Block
        using (Profiler.BeginZone("Render"))
        {
            target.Clear(Color.White);
            modules.AfterUpdate(in deltaTime);
            res.batcher.Render(target);
            res.batcher.Clear();
        }
        //Mark End Each Frame
        Profiler.EmitFrameMark();
    }
```