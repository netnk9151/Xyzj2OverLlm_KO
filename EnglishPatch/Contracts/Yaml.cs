using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EnglishPatch.Contracts;

public class Yaml
{
    public static ISerializer CreateSerializer()
    {
        return new SerializerBuilder()
           .WithNamingConvention(CamelCaseNamingConvention.Instance)
           .Build();
    }

    public static IDeserializer CreateDeserializer()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }
}
