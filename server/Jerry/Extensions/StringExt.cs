using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jerry.Extensions;

internal static class StringExt
{
    public static string Truncate(this string value, int maxChars)
    {
        return value.Length <= maxChars ? value : string.Concat(value.AsSpan(0, maxChars), "...");
    }
}

