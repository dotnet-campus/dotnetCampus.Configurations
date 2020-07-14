# COIN Configuration

COIN = Configuration\n , means `configuration + Line break`, that is the reason for its naming. COIN has designed a high-performance application configuration file and a .NET library for high-performance reading and writing of this configuration file

|Build|NuGet|
|--|--|
|![](https://github.com/dotnet-campus/dotnetCampus.Configurations/workflows/.NET%20Core/badge.svg)|[![](https://img.shields.io/nuget/v/dotnetCampus.Configurations.svg)](https://www.nuget.org/packages/dotnetCampus.Configurations)|

## Configuration file format

The configuration file is in units of lines, and the line with the character of `>` at the beginning of the line is used as a comment, and the content after `>` will be ignored. The line beginning with the first non- `>` character is used as the `Key` value, and the content between the lines below this line until the end of the file or the beginning of the next `>` character is used as the `Value` value

```
> COIN Configuration
> Version 1.0
State.BuildLogFile
xxxxx
> Comment content
Foo
This is the first line
This is the second line
>
> End Configuration
```

## Install NuGet

```
dotnet add package dotnetCampus.Configurations
```

## Usage

Initialization:

```csharp
var configs = DefaultConfiguration.FromFile(@"C:\Users\lvyi\Desktop\walterlv.fkv");
```

GetValue:

```csharp
string value0 = configs["Foo"];

string value1 = configs["Foo"] ?? "anonymous";
```

SetValue:

```csharp
configs["Foo"] = "lvyi";

configs["Foo"] = null;

configs["Foo"] = "";
```
