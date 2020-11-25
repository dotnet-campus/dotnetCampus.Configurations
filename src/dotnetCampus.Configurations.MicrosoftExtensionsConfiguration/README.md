# dotnetCampus.Configurations.MicrosoftExtensionsConfiguration

兼容 Microsoft.Extensions.Configuration 的逻辑，可以将 Microsoft.Extensions.Configuration 的配置加入到 dotnetCampus.Configurations 里面

## 使用方法

可以对 IConfigurationBuilder 和 IConfiguration 对象调用 ToAppConfigurator 方法进行接入

```csharp
public void Foo(IConfigurationBuilder builder)
{
     IAppConfigurator appConfigurator = builder.ToAppConfigurator();
     // 完成接入
}

public void Foo(IConfiguration configuration)
{
     IAppConfigurator appConfigurator = configuration.ToAppConfigurator();
     // 完成接入
}
```

在获取到 IAppConfigurator 对象之后，就可以使用 dotnetCampus.Configurations 的方式读写配置