# COIN 硬币配置文件

COIN = Configuration\n，即“配置+换行符”，因默认使用“\n”作为换行符而得名。COIN 设计了一个高性能的应用程序配置文件，以及实现高性能读写这个配置文件的 .NET 库。

原名为：dotnetCampus.Configurations，这也是此库中命名空间的前缀。

|Build|NuGet|
|--|--|
|![](https://github.com/dotnet-campus/dotnetCampus.Configurations/workflows/.NET%20Core/badge.svg)|[![](https://img.shields.io/nuget/v/dotnetCampus.Configurations.svg)](https://www.nuget.org/packages/dotnetCampus.Configurations)|

## 配置文件存储格式

配置文件以行为单位，将行首是 `>` 字符的行作为注释，在 `>` 后面的内容将会被忽略。在第一个非 `>` 字符开头的行作为 `Key` 值，在此行以下直到文件结束或下一个 `>` 字符开始的行之间内容作为 `Value` 值

```
> 配置文件
> 版本 1.0
State.BuildLogFile
xxxxx
> 注释内容
Foo
这是第一行
这是第二行
>
> 配置文件结束
```

## NuGet 安装

```csharp
dotnet add package dotnetCampus.Configurations
```

## 快速使用

初始化：

```csharp
// 使用一个文件路径创建默认配置的实例。文件可以存在也可以不存在，甚至其所在的文件夹也可以不需要提前存在。
// 这里的配置文件后缀名 coin 是 Configuration\n，即 “配置+换行符” 的简称。你也可以使用其他扩展名，因为它实际上只是 UTF-8 编码的纯文本而已。
var configs = DefaultConfiguration.FromFile(@"C:\Users\lvyi\Desktop\walterlv.coin");
```

获取值：

```csharp
// 获取配置 Foo 的字符串值。
// 这里的 value 一定不会为 null，如果文件不存在或者没有对应的配置项，那么为空字符串。
string value0 = configs["Foo"];

// 获取字符串值的时候，如果文件不存在或者没有对应的配置项，那么会使用默认值（空传递运算符 ?? 可以用来指定默认值）。
string value1 = configs["Foo"] ?? "anonymous";
```

设置值：

```csharp
// 设置配置 Foo 的字符串值。
configs["Foo"] = "lvyi";

// 可以设置为 null，但你下次再次获取值的时候却依然保证不会返回 null 字符串。
configs["Foo"] = null;

// 可以设置为空字符串，效果与设置为 null 是等同的。
configs["Foo"] = "";
```

## 在大型项目中使用

大型项目的模块数量众多，其配置的数量也是十分庞大的。为了保证配置在业务之间独立，也为了防止类型转换辅助代码在大型项目中重复编写，你需要使用更高级的初始化和使用方法。

初始化：

```csharp
// 这里是大型项目配置初始化处的代码。
// 此类型中包含底层的配置读写方法，而且所有读写全部是异步的，防止影响启动性能。
var configFileName = @"C:\Users\lvyi\Desktop\walterlv.coin";
var config = ConfigurationFactory.FromFile(configFileName);

// 如果你需要对整个应用程序公开配置，那么可以公开 CreateAppConfigurator 方法返回的新实例。
// 这个实例的所有配置读写全部是同步方法，这是为了方便其他模块使用。
Container.Set<IAppConfigurator>(config.CreateAppConfigurator());
```

在业务模块中定义类型安全的配置类：

```csharp
internal class StateConfiguration : Configuration
{
    /// <summary>
    /// 获取或设置整型。
    /// </summary>
    internal int? Count
    {
        get => GetInt32();
        set => SetValue(value);
    }

    /// <summary>
    /// 获取或设置带默认值的整型。
    /// </summary>
    internal int Length
    {
        get => GetInt32() ?? 2;
        set => SetValue(Equals(value, 2) ? null : value);
    }

    /// <summary>
    /// 获取或设置布尔值。
    /// </summary>
    internal bool? State
    {
        get => GetBoolean();
        set => SetValue(value);
    }

    /// <summary>
    /// 获取或设置字符串。
    /// </summary>
    internal string Value
    {
        get => GetString();
        set => SetValue(value);
    }

    /// <summary>
    /// 获取或设置带默认值的字符串。
    /// </summary>
    internal string Host
    {
        get => GetString() ?? "https://localhost:17134";
        set => SetValue(Equals(value, "https://localhost:17134") ? null : value);
    }

    /// <summary>
    /// 获取或设置非基元值类型。
    /// </summary>
    internal Rect? Screen
    {
        get => this.GetValue<Rect>();
        set => this.SetValue<Rect>(value);
    }
}
```

在业务模块中使用：

```csharp
private readonly IAppConfiguration _config = Container.Get<IAppConfigurator>();

// 读取配置。
private void Restore()
{
    var config = _config.Of<StateConfiguration>();
    var bounds = config.Screen;
    if (bounds != null)
    {
        // 恢复窗口位置和尺寸。
    }
}

// 写入配置。
public void Update()
{
    var config = _config.Of<StateConfiguration>();
    config.Screen = new Rect(0, 0, 3840, 2160);
}
```

## 特性

1. 高性能读写
    - 在初始化阶段使用全异步处理，避免阻塞主流程。
    - 使用特别为高性能读写而设计的配置文件格式。
    - 多线程和多进程安全高性能读写。
1. 无异常设计
    - 所有配置项的读写均为“无异常设计”，你完全不需要在业务代码中处理任何异常。
    - 为防止业务代码中出现意料之外的 `NullReferenceException`，所有配置项的返回值均不为实际意义的 `null`。
        - 值类型会返回其对应的 `Nullable<T>` 类型，这是一个结构体，虽然有 `null` 值，但不会产生空引用。
        - 引用类型仅提供字符串，返回 `Nullable<ConfigurationString>` 类型，这也是一个结构体，你可以判断 `null`，但实际上不可能为 `null`。
1. 全应用程序统一的 API
    - 在大型应用中开放 API 时记得使用 `CreateAppConfigurator()` 来开放，这会让整个应用程序使用统一的一套配置读写 API，且完全的 IO 无感知。
