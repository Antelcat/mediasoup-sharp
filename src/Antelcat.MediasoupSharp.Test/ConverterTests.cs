﻿using Antelcat.MediasoupSharp.Internals.Converters;

namespace Antelcat.MediasoupSharp.Test;

public class ConverterTests
{
    [Test]
    public void OutputConverters()
    {
        var str =
        string.Join("\n",
            typeof(IEnumStringConverter)
                .Assembly
                .GetTypes()
                .Where(x=>x.Name.EndsWith("Converter"))
                .Where(x => x.GetInterfaces().Any(x=>x == typeof(IEnumStringConverter)))
                .Select(x => x.Name));
    }
}