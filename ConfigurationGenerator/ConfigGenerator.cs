using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace ConfigurationGenerator;

[Generator]
public class ConfigGenerator : IIncrementalGenerator
{
    private const string Namespace                   = "BepInEx.Configuration.Generators";
    private const string GenerateConfigAttributeName = "GenerateConfigAttribute";
    private const string EntryAttributeName          = "EntryAttribute";

    private const string ConfigAttributeSourceCode = $$"""
                                                       // <auto-generated/>

                                                       namespace {{Namespace}}
                                                       {
                                                           [System.AttributeUsage(System.AttributeTargets.Class)]
                                                           public class {{GenerateConfigAttributeName}} : System.Attribute
                                                           {
                                                               
                                                           }
                                                       }
                                                       """;

    private const string EntryAttributeSourceCode = $$"""
                                                      // <auto-generated/>

                                                      namespace {{Namespace}}
                                                      {
                                                          [System.AttributeUsage(System.AttributeTargets.Field)]
                                                          public class {{EntryAttributeName}} : System.Attribute
                                                          {
                                                              public {{EntryAttributeName}}(string section, string key, string description)
                                                              {
                                                              }
                                                          }
                                                      }
                                                      """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute to the compilation.
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource($"{GenerateConfigAttributeName}.g.cs", SourceText.From(ConfigAttributeSourceCode, Encoding.UTF8));
            ctx.AddSource($"{EntryAttributeName}.g.cs",          SourceText.From(EntryAttributeSourceCode,  Encoding.UTF8));
        });

        // Filter classes annotated with the [Config] attribute. Only filtered Syntax Nodes can trigger code generation.
        var provider = context.SyntaxProvider
                              .CreateSyntaxProvider(
                                  static (s,   _) => s is ClassDeclarationSyntax,
                                  static (ctx, _) => GetClassDeclarationForSourceGen(ctx))
                              .Where(t => t.reportAttributeFound)
                              .Select((t, _) => t.Item1);

        // Generate the source code.
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
                                     ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
    }

    /// <summary>
    /// Checks whether the Node is annotated with the [Report] attribute and maps syntax context to the specific node type (ClassDeclarationSyntax).
    /// </summary>
    /// <param name="context">Syntax context, based on CreateSyntaxProvider predicate</param>
    /// <returns>The specific cast and whether the attribute was found.</returns>
    private static (ClassDeclarationSyntax, bool reportAttributeFound) GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // Go through all attributes of the class.
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        foreach (var attributeSyntax in attributeListSyntax.Attributes)
        {
            if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol
                attributeSymbol)
                continue; // if we can't get the symbol, ignore it

            var attributeName = attributeSymbol.ContainingType.ToDisplayString();

            // Check the full name of the [GenerateConfig] attribute.
            if (attributeName == $"{Namespace}.{GenerateConfigAttributeName}")
                return (classDeclarationSyntax, true);
        }

        return (classDeclarationSyntax, false);
    }

    /// <summary>
    /// Generate code action.
    /// It will be executed on specific nodes (ClassDeclarationSyntax annotated with the [Report] attribute) changed by the user.
    /// </summary>
    /// <param name="context">Source generation context used to add source files.</param>
    /// <param name="compilation">Compilation used to provide access to the Semantic Model.</param>
    /// <param name="classDeclarations">Nodes annotated with the [Report] attribute that trigger the generate action.</param>
    private static void GenerateCode(SourceProductionContext                context, Compilation compilation,
                                     ImmutableArray<ClassDeclarationSyntax> classDeclarations)
    {
        // Go through all filtered class declarations.
        foreach (var classDeclarationSyntax in classDeclarations)
        {
            // We need to get semantic model of the class to retrieve metadata.
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            // Symbols allow us to get the compile-time information.
            if (ModelExtensions.GetDeclaredSymbol(semanticModel, classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                continue;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // 'Identifier' means the token of the node. Get class name from the syntax node.
            var className = classDeclarationSyntax.Identifier.Text;

            // Find all properties annotated with any attribute in this namespace.
            var fields = classSymbol.GetMembers()
                                    .OfType<IFieldSymbol>()
                                    .Where(p => p.IsReadOnly)
                                    .Select(p => (symbol: p,
                                                  attribute: p.GetAttributes()
                                                              .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() ==
                                                                                   $"{Namespace}.{EntryAttributeName}")))
                                    .Where(a => a is { attribute: not null })
                                    .ToList();

            // Implement these partial properties, and backing fields for each property.
            var memberDefinitions = new List<string>(fields.Count);
            var memberBindings    = new List<string>(fields.Count);

            foreach (var (symbol, attribute) in fields)
            {
                // Get the fields inside the entry attribute.
                var section     = attribute!.ConstructorArguments[0];
                var key         = attribute.ConstructorArguments[1];
                var description = attribute.ConstructorArguments[2];

                memberDefinitions.Add($"""
                                           /// <summary>
                                           /// {description.Value}
                                           /// </summary>
                                           /// <remarks>
                                           /// Uses default value specified by <see cref="{symbol.Name}"/>
                                           /// </remarks>
                                           public static BepInEx.Configuration.ConfigEntry<{symbol.Type.ToDisplayString()}> {symbol.Name}Config;
                                       """);

                memberBindings.Add(
                    $"""        {symbol.Name}Config = configFile.Bind<{symbol.Type.ToDisplayString()}>("{section.Value}", "{key.Value}", {symbol.Name}, "{description.Value}");"""
                );
            }


            // Build up the source code
            var code = $$"""
                         // <auto-generated/>

                         using System;
                         using System.Collections.Generic;

                         namespace {{namespaceName}};

                         partial class {{className}}
                         {
                         {{string.Join("\n\n", memberDefinitions)}}
                             
                             public static void Register(BepInEx.Configuration.ConfigFile configFile)
                             {
                                 // Prevent saving the config on bind
                                 configFile.SaveOnConfigSet = false;
                                 
                         {{string.Join("\n", memberBindings)}}

                                 var orphansProperty = HarmonyLib.AccessTools.Property(typeof(BepInEx.Configuration.ConfigFile), "OrphanedEntries");
                                 var orphans = (Dictionary<BepInEx.Configuration.ConfigDefinition, string>)orphansProperty.GetValue(configFile);
                                 orphans.Clear();
                                 
                                 configFile.Save();
                                 configFile.SaveOnConfigSet = true;
                             }
                         }

                         """;

            // Add the source code to the compilation.
            context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}
