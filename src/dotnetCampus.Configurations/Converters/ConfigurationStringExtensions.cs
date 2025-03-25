using System;
using System.Globalization;

namespace dotnetCampus.Configurations.Converters;

/// <summary>
/// 对 <see cref="ConfigurationString"/> 的扩展方法
/// </summary>
public static class ConfigurationStringExtensions
{
    /// <summary>
    /// 将 <see cref="ConfigurationString"/> 转换为 <see cref="bool"/>? 类型。如传入 <see cref="ConfigurationString"/> 为非法 <see cref="bool"/> 或空，则都返回 null 值
    /// </summary>
    /// <param name="configurationString"></param>
    /// <returns></returns>
    public static bool? AsBoolean(this ConfigurationString? configurationString)
    {
        if (configurationString is null)
        {
            return null;
        }
        var value = configurationString.Value.InternalGetValue();
        return bool.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// 将 <see cref="ConfigurationString"/> 转换为 <see cref="decimal"/>? 类型。如传入 <see cref="ConfigurationString"/> 为非法 <see cref="decimal"/> 或空，则都返回 null 值
    /// </summary>
    /// <param name="configurationString"></param>
    /// <returns></returns>
    public static decimal? AsDecimal(this ConfigurationString? configurationString)
    {
        if (configurationString is null)
        {
            return null;
        }

        var value = configurationString.Value.InternalGetValue();
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <summary>
    /// 将 <see cref="ConfigurationString"/> 转换为 <see cref="double"/>? 类型。如传入 <see cref="ConfigurationString"/> 为非法 <see cref="double"/> 或空，则都返回 null 值
    /// </summary>
    /// <param name="configurationString"></param>
    /// <returns></returns>
    public static double? AsDouble(this ConfigurationString? configurationString)
    {
        if (configurationString is null)
        {
            return null;
        }
        var value = configurationString.Value.InternalGetValue();
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <summary>
    /// 将 <see cref="ConfigurationString"/> 转换为 <see cref="float"/>? 类型。如传入 <see cref="ConfigurationString"/> 为非法 <see cref="float"/> 或空，则都返回 null 值
    /// </summary>
    /// <param name="configurationString"></param>
    /// <returns></returns>
    public static float? AsSingle(this ConfigurationString? configurationString)
    {
        if (configurationString is null)
        {
            return null;
        }
        var value = configurationString.Value.InternalGetValue();
        return float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <inheritdoc cref="AsSingle"/>
    [Obsolete("此方法的作用只是用来告诉你，应该调用的是 AsSingle 方法")]
    public static float? AsFloat(this ConfigurationString? configurationString)
    {
        return configurationString.AsSingle();
    }

    /// <summary>
    /// 将 <see cref="ConfigurationString"/> 转换为 <see cref="int"/>? 类型。如传入 <see cref="ConfigurationString"/> 为非法 <see cref="int"/> 或空，则都返回 null 值
    /// </summary>
    /// <param name="configurationString"></param>
    /// <returns></returns>
    public static int? AsInt32(this ConfigurationString? configurationString)
    {
        if (configurationString is null)
        {
            return null;
        }
        var value = configurationString.Value.InternalGetValue();
        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <summary>
    /// 将 <see cref="ConfigurationString"/> 转换为 <see cref="long"/>? 类型。如传入 <see cref="ConfigurationString"/> 为非法 <see cref="long"/> 或空，则都返回 null 值
    /// </summary>
    /// <param name="configurationString"></param>
    /// <returns></returns>
    public static long? AsInt64(this ConfigurationString? configurationString)
    {
        if (configurationString is null)
        {
            return null;
        }
        var value = configurationString.Value.InternalGetValue();
        return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <summary>
    /// 将 <see cref="ConfigurationString"/> 转换为 <see cref="Enum"/>? 类型。如传入 <see cref="ConfigurationString"/> 为非法 <see cref="Enum"/> 或空，则都返回 null 值
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="configurationString"></param>
    /// <returns></returns>
    public static T? AsEnum<T>(this ConfigurationString? configurationString) where T : struct, IConvertible
    {
        if (configurationString is null)
        {
            return null;
        }
        var value = configurationString.Value.InternalGetValue();

        return Enum.TryParse<T>(value, out var result) ? result : null;
    }
}