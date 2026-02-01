# Tian


## Introduce

1. A simple ecs game engine
2. Tools


## TODO

- 引擎
  - 框架
    - FosterFramework
    - Arch
    
  - 功能
    - [x] 基于Transform的坐标系统
    
    - [x] 渲染系统
    
    - [x] 正交相机系统 
    
    - [x] 简单资源管理系统
      - [x] 图集
    - [ ] 支持存档配置的资源管理系统
    - [x] 性能分析工具

    - [ ] 打包系统
    - [ ] 数据结构
      - [ ] 空间分区结构
    
    - [ ] 动画系统
      - [ ] 状态机
      - [ ] 帧动画
      - [ ] Tween库
    
    - [ ] UI系统
      - [ ] UI布局系统 -> 参考Clay https://github.com/Orcolom/clay-cs?tab=readme-ov-file
        - [ ] 尝试直接接入Clay,并渲染
      - [ ] 基础UIElement
      - [ ] UI遮挡
    - [ ] 输入系统
      - [ ] 现在只是生成了Viewport的uv，应该有一些方便的函数帮助变换。更方便获取Game/Editor模式下，鼠标在屏幕上的位置。简化依赖
      - [ ] 鼠标
      - [ ] 键盘
      - [ ] 游戏pad
    - [ ] 粒子系统
    
    - [ ] 音频系统
    
    - [ ] 序列化系统
    - [ ] DebugUtils
    - [ ] Utils
      - [ ] 从窗口坐标转换到Rect UV坐标
      - [ ] 从UV坐标转换到Rect 的坐标
 - Friflo
    - [ ] Code Hot Reload
    - [ ] Eiditor
      - [ ] EditorUtility 简单的方法快速绘制常见编辑器
      - [ ] Build In Editor 
      - [ ] EditorWindow
     



## Archtecture

```powershell
|--Lib
|--Engine
|--Content
|--Runner
|--Editor
```


## Feature

### AssetManager
- Sprite
- Audio
- Txt
- 支持加载配置资源的系统


### AssetPackSystem
目前publish之后,由于启用了单文件发布，所以Assets/Sprites被包含进了文件中。导致找不到文件夹。这里可能需要一个资源打包系统，来替换掉这个特殊处理。
下面这段代码会在Build之后，将文件复制到文件夹中
如果你真的想把资源塞进 exe，不在磁盘上出现 ，那就得换加载方式：
- 用 <EmbeddedResource> 把文件变成程序集嵌入资源；
- 运行时用 Assembly.GetExecutingAssembly().GetManifestResourceStream("命名空间.路径.文件名") 读取流，再自己解析。 这就不能再用 Directory.EnumerateFiles 那套文件系统 API 了。
```csproj

<Target Name="CopySpritesAfterPublish" AfterTargets="Publish">
        <ItemGroup>
            <SpriteFiles Include="..\Content\Assets\Sprites\**\*.*" />
        </ItemGroup>
        <Copy SourceFiles="@(SpriteFiles)"
              DestinationFiles="@(SpriteFiles->'$(PublishDir)Assets\Sprites\%(RecursiveDir)%(Filename)%(Extension)')"
              SkipUnchangedFiles="true" />
</Target>
```

目前可能想将所有的资源都做成zip。
1. 研究zip，如何压缩，如何访问文件，目录

```cs
using System.IO.Compression;

// spritesPath 可以保留给以后用，但加载改为 zip
var zipPath = Path.Combine(AssetsPath, "Assets.zip");

if (File.Exists(zipPath))
{
    using var zip = ZipFile.OpenRead(zipPath);

    foreach (var entry in zip.Entries)
    {
        if (!entry.FullName.StartsWith("Sprites/", StringComparison.OrdinalIgnoreCase))
            continue;

        if (!entry.FullName.EndsWith(".ase", StringComparison.OrdinalIgnoreCase))
            continue;

        var relative = entry.FullName.Substring("Sprites/".Length);
        if (string.IsNullOrEmpty(relative))
            continue;

        var name = Path.ChangeExtension(relative, null)
            .Replace('/', Path.DirectorySeparatorChar);

        using var stream = entry.Open();
        var ase = new Aseprite(stream);

        if (ase.Frames.Length > 0)
            spriteFiles[name] = ase;
    }
}
else
{
    var spritesPath = Path.Combine(AssetsPath, "Sprites");
    if (Directory.Exists(spritesPath))
    {
        foreach (var file in Directory.EnumerateFiles(spritesPath, "*.ase", SearchOption.AllDirectories))
        {
            var name = Path.ChangeExtension(Path.GetRelativePath(spritesPath, file), null);
            var ase = new Aseprite(file);
            if (ase.Frames.Length > 0)
                spriteFiles[name] = ase;
        }
    }
}
```


### Editor

> How? 
> - Content 中有各种实体，只要Editor可以获取World就能获得实体上有什么。然后Editor根据Component绘制Inspector/改变数据

- Editor:游戏编辑器
    - 编辑器
        - Inspector 监视实体状态
        - 显示游戏运行的窗口
        - 写编辑器的接口
        - 编辑器文件的存档方法
    - 上面编辑器的几条总共可能都不需要完成，由于Arch架构
      - [ ] Editor Config Entity -> Convert System -> Convert to Runtime Entity
        - Editor Mode for Make Editor Config Entity
        - Runtime System Editor-Entity to Runtime-Entity
      - For example:
        - I want to make a Level Editor
        - Make World Entity/Level Entity in Editor Mode
        - Load Level Entity From File In Runtime -> Then make some request to convert level to more specific entity
        - [ ] 编辑器中的实体是定义实体，在Runtime的时候经过RuntimeConvert变成Runtime实体。如果有存档，从存档创建World,如果没有，从WorldAsset创建World
        - [ ] 1. PrefabAsset -> 有一个Asset表，里面着所有的Guid和对应的GameAsset(这里面放文件Name,文件路径等等),如果是Runtime直接从管理器中取对象然后Clone
        - [ ] 2. WorldAsset -> 世界资源，编辑器中编辑写入；Game中读取创建实际的实体.WorldAsset中已经自然包含一个实体注册表，可以将实体进行链接。
        - [ ] 3. WorldSave -> 那么存档保存的是什么呢？存档保存的是Game World中的世界->读取存档则是从GameWorld中读取世界
        - [ ] Entity Serialize Arch里面的 SingleEntitySerialize 可以作为参考 -> 感觉Entity可以单独作为一种序列化资源
      - [ ] ImGui Theme File
        - read/set theme config from file. 



     - 需要保存信息不应该是引用，而应该是可以存储的结构体或者string之类的，这样在还原后可以填充实体，什么时候填充呢？应该是在系统检查的时候吧，将世界上所有需要Init的实体标记InitRequest       

### Serializer
- 序列化库
    - 保存编辑器数据
    - 通过编辑器产生游戏数据

- 通常需要保存的数据取决于游戏本身，比如关卡数据，可以存放到每个json文件中，运行时加载这些关卡数据
- 对于引用支持，通常有两个阶段
  - 一阶段：反序列化所有的对象的规格数据，只包含原始ID和默认属性，不做引用解析
  - 注册映射：创建`Dictionary<guid,object>` 把每个对象的Guid写入
  - 二阶段：再次遍历DTO，将引用字段解析成运行时引用，并写回到对象的字段中
- 实践? 
  - 首先将所有的GameAssets填入，这里面包含了Guid->Game Asset->Path
  - 然后再反序列化需要的EntityPrefab的时候，由于保存了Guid，所以可以从Guid访问到EntityPath,如果没有创建，直接创建一个Entity,这个Entity,然后将引用放到这里
  - 读的时候直接读取Asset管线里面有没有该资源， 

- 创建一个给编辑器创建一整个预制体 Prefab,然后将所有的编辑器实体都序列化进去。
- 创建一个注册系统，创建世界的时候将所有的Prefab对应的string Name 和 Entity ID写入一个表
- Runtime的时候通过查询这个Name,找到EntityID

## Practice

### AddCharacter To Fonts
```csharp

// 初始化字体（可以用现有 res.font，或新建）
 // 已经注入到系统的字体
 for (int i = 0; i < 3; i++)
 {
     var icon = Assets.GetSubtexture($"special_num/{i}"); // 取你的图集子纹理
     // 选择一个私用区码点 U+E000（不与正常字符冲突）
     int codepoint = 0xE000 + i;
     // 以像素为单位设置水平进位宽度（基础字号 Size 下）
     float advance = icon.Source.Width;
     // 让图片高度约等于行高，顶边对齐到基线之上：offset.Y 取 -font.Height
     // 如需微调，可再加减几个像素
     var offset = new Vector2(0, -font.Height);
     font.AddCharacter(codepoint, advance, offset, icon);
 }
 
 var s = $"捉蛙 {(char)0xE000}  {(char)0xE001}  {(char)0xE002}    x3";
 batcher.Text(Assets.Font, s, new Vector2(200, 200), Color.White);

```

## 加速编译
--no-restore 不要进行NuGet检查

## Publish
- win-x64 `dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true`

## Changelog
### v0.2.1
- 加入人物按照路径移动

### v0.2.2
- 加入旋转大小依赖，按照示例青蛙变为两倍大小
- 解决shader编译到多平台的问题

### v0.2.3
- 加入相机移动，放缩，旋转
- 所有物体的旋转，放缩都相对于世界坐标原点
- 修复由物体震动引起的点击位置不对
- 出现一个新的问题，FindLineSystem如果物体速度过快，可能会超出目标，并在原地弹