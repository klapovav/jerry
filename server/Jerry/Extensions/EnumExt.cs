using System;
using System.Linq;
using System.Reflection;

namespace Jerry.Extensions;
#nullable disable

public static class EnumExt
{
    public static bool Has<T>(this System.Enum type, T value)
    {
        try
        {
            return ((int)(object)type & (int)(object)value) == (int)(object)value;
        }
        catch
        {
            return false;
        }
    }

    public static T Add<T>(this System.Enum type, T value)
    {
        try
        {
            return (T)(object)((int)(object)type | (int)(object)value);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                string.Format(
                    "Could not append value from enumerated type '{0}'.",
                    typeof(T).Name
                    ), ex);
        }
    }

    public static T Remove<T>(this System.Enum type, T value)
    {
        try
        {
            return (T)(object)((int)(object)type & ~(int)(object)value);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                string.Format(
                    "Could not remove value from enumerated type '{0}'.",
                    typeof(T).Name
                    ), ex);
        }
    }
    /// <summary>
    ///     A generic extension method that aids in reflecting 
    ///     and retrieving any attribute that is applied to an `Enum`.
    /// </summary>
    public static TAttribute GetAttribute<TAttribute>(this Enum enumValue)
            where TAttribute : Attribute
    {
        return enumValue.GetType()
                        .GetMember(enumValue.ToString())
                        .First()
                        .GetCustomAttribute<TAttribute>();
    }
}