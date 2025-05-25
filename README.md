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

To create a new configuration entry, add a new read-only property.
Add the `[Entry]` attribute to the property, and mark the property as `static partial`.

```cs
using BepInEx.Configuration.Generators;

[GenerateConfig]
public static partial class MyConfig
{
   [Entry("Section", "Key", 0, "Description")]
   public static partial int Test { get; } 
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

You can then read from the entry as usual. 

```cs
Debug.Log(ConfigTest.Test);
```
