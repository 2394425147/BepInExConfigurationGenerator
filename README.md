# BepInEx Configuration Generator

This is a configuration generation utility to help you iterate on mods quickly.

# Quick Start

Create a class called `MyConfig.cs`.

To allow the source generator to complete the class,
add the `[GenerateConfig]` attribute to the class, and mark the class as `static partial`.

```cs
using BepInEx.Configuration.Generators;

[GenerateConfig]
public static partial class MyConfig
{
    
}
```

To create a new configuration entry, add a new static readonly field with the `[Entry]` attribute to the property.

```cs
using BepInEx.Configuration.Generators;

[GenerateConfig]
public static partial class MyConfig
{
    [Entry("General", "Test", "An integer config")]
    private static readonly int Test = 0; // 0 is the default value.
    
    [Entry("General", "Keyboard shortcut", "A keyboard shortcut config (Custom type registered in TomlTypeConverter)")]
    private static readonly KeyboardShortcut TestKeyboardShortcut = new(KeyCode.H, KeyCode.LeftControl);
}
```

Register the configs to your plugin.

```cs
// ...

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
+       MyConfig.Register(Config);
        // ...
    }
}
```

You can then read from the entries. 

```cs
Debug.Log(ConfigTest.TestConfig); // The generated entries append the `Config` suffix.
```
