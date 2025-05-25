using BepInEx.Configuration.Generators;
using BepInEx.Unity.Mono.Configuration;
using UnityEngine;

namespace ConfigurationGenerator.Samples;

[GenerateConfig]
public static partial class ConfigTest
{
    [Entry("General", "Test", "An integer config")]
    private static readonly int Test = 0;

    [Entry("General", "Keyboard shortcut", "A keyboard shortcut config (Custom type registered in TomlTypeConverter)")]
    private static readonly KeyboardShortcut TestKeyboardShortcut = new(KeyCode.H, KeyCode.LeftControl);
}
