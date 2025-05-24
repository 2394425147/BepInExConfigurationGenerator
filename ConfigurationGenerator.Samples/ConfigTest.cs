using BepInEx.Configuration.Generators;

namespace ConfigurationGenerator.Samples;

[GenerateConfig]
public static partial class ConfigTest
{
    [Entry("Test", "Test1", 0, "Description")]
    public static partial int Test { get; }

    [Entry("Test", "Test 2", (short)1321, "Another test")]
    public static partial short Test2 { get; }
}
