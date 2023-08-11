using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

using Generator.Attributes;

namespace Generator.Generators;

[Generator]
internal class MakeInterfaceGenerator : IIncrementalGenerator
{
    private record class SemanticProvider(ClassDeclarationSyntax Syntax, INamedTypeSymbol Symbol);
    private record class CustomProvider(Compilation Compilation, ImmutableArray<SemanticProvider> Classes);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider.CreateSyntaxProvider(SyntacticPredicate, SemanticTransform);

        var provider = context.CompilationProvider.Combine(classes.Collect()).Select((x, _) => new CustomProvider(x.Left, x.Right));

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } candidate
            && !candidate.Modifiers.Any(SyntaxKind.StaticKeyword);
    }

    private static SemanticProvider SemanticTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var candidate = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(candidate, cancellationToken) ?? throw new Exception("Candidate is not a symbol");
        return new(candidate, symbol);
    }

    private class ValidItem
    {
        public ValidItem(SemanticProvider provider, AttributeData attribute, string interfaceName)
        {
            Provider = provider;
            Attribute = attribute;
            IName = interfaceName;
        }
        public SemanticProvider Provider { get; }
        public AttributeData Attribute { get; }
        public string IName { get; }

    }

    private static void Execute(SourceProductionContext context, CustomProvider provider)
    {
        var markerAttrSymbol = provider.Compilation.GetSymbolByName(typeof(MakeInterfaceAttribute).FullName);//"Generator.Attributes.MakeInterfaceAttribute");

        // Get only the classes with the attribute code
        var valid = new HashSet<ValidItem>();
        foreach (var classItem in provider.Classes)
        {
            foreach (var attr in classItem.Symbol.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, markerAttrSymbol) || attr.AttributeConstructor == null)
                    continue;
                var interfaceName = UtilString.ReplaceLastOccurrence(classItem.Symbol.ToGlobalName(), ".", ".I");
                valid.Add(new(classItem, attr, interfaceName));
            }
        }

        var sb = new StringBuilderSG();
        var interfaces = new List<string>();
        foreach (var item in valid)
        {
            var classSyntax = item.Provider.Syntax;
            var classSymbol = item.Provider.Symbol;
            var attr = item.Attribute;

            // Get Attribute Informations
            bool useProps = false, useMethods = false, useEvents = false;
            bool inheritInterfaces = false, inheritGeneratedInterfaces = false;

            var parameters = attr.AttributeConstructor!.Parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var value = attr.ConstructorArguments[i].Value as bool?;

                if (value == null)
                    continue;

                switch (param.Name)
                {
                    case nameof(MakeInterfaceAttribute.useProps): useProps = (bool)value; break;
                    case nameof(MakeInterfaceAttribute.useMethods): useMethods = (bool)value; break;
                    case nameof(MakeInterfaceAttribute.useEvents): useEvents = (bool)value; break;
                    case nameof(MakeInterfaceAttribute.inheritInterfaces): inheritInterfaces = (bool)value; break;
                    case nameof(MakeInterfaceAttribute.inheritGeneratedInterfaces): inheritGeneratedInterfaces = (bool)value; break;
                }
            }

            // Get Class Informations
            var ns = classSymbol.ContainingNamespace.Name;
            var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var acessibility = classSymbol.DeclaredAccessibility.AccessibilityToString();

            // ------ Open
            sb.AddNullable(true);
            sb.AppendLine();

            Action? closeNamespace = null;
            if (!string.IsNullOrWhiteSpace(ns))
                closeNamespace = sb.CreateNamespace(ns);

            // Create the Interface from Class
            var interfaceName = 'I' + className;
            var interfaceDeclaration = $"{acessibility} interface {interfaceName}";

            interfaces.Clear();
            if (inheritGeneratedInterfaces)
            {
                foreach (var item2 in valid)
                    if (item != item2 && classSymbol.IsOfBaseType(item2.Provider.Symbol))
                        interfaces.Add(item2.IName);
            }
            if (inheritInterfaces)
            {
                interfaces.AddRange(classSymbol.AllInterfaces.Select(x => x.ToGlobalName()));
            }
            if (interfaces.Count > 0)
                interfaceDeclaration += " : " + string.Join(", ", interfaces);

            var closeType = sb.CreateBracketDeclaration(interfaceDeclaration);

            var model = provider.Compilation.GetSemanticModel(classSyntax.SyntaxTree);
            var acessorText = new StringBuilder();

            foreach (var member in classSyntax.Members)
            {
                if (member.Modifiers.Count > 0 && !member.Modifiers.Any(SyntaxKind.PublicKeyword))
                    continue;

                if (useProps && member is PropertyDeclarationSyntax propSyntax)
                {
                    var typeName = propSyntax.Type.ToFullyQualifiedName(model);
                    var propName = propSyntax.Identifier.ToString();

                    if (propSyntax.AccessorList != null)
                    {
                        foreach (var acessor in propSyntax.AccessorList.Accessors)
                        {
                            var mods = acessor.Modifiers;
                            // cant be public - so only internal, private or protected keywords are valid
                            if (mods.Count > 0)
                                continue;
                            acessorText.Append(acessor.ToString() + " ");
                        }
                        sb.AppendLine($"{typeName} {propName} {{ {acessorText}}}");
                        acessorText.Clear();
                    }
                    else
                        sb.AppendLine($"{typeName} {propName}");
                }
                else if (useMethods && member is MethodDeclarationSyntax methodSyntax)
                {
                    var returnTypeName = methodSyntax.ReturnType.ToFullyQualifiedName(model);
                    var methodName = methodSyntax.Identifier.ToString();

                    var pl = methodSyntax.ParameterList.Parameters;
                    const string SPACING = ", ";
                    var methodParams = new StringBuilder(pl.Count * (SPACING.Length + 4));
                    foreach (var param in pl)
                    {
                        string paramName = param.Identifier.ToString();
                        string paramType = param.Type != null ? param.Type.ToFullyQualifiedName(model) : "object";
                        methodParams.Append($"{paramType} {paramName}{SPACING}");
                    }
                    methodParams.Remove(methodParams.Length - SPACING.Length, SPACING.Length);
                    sb.AppendLineC($"{returnTypeName} {methodName}({methodParams})");
                }
                else if (useEvents && member is EventDeclarationSyntax eventSyntax)
                {
                    var typeName = eventSyntax.Type.ToFullyQualifiedName(model);
                    var eventName = eventSyntax.Identifier.ToString();
                    sb.AppendLineC($"event {typeName} {eventName}");
                }
                else if (useEvents && member is EventFieldDeclarationSyntax eventFieldSyntax)
                {
                    var declaration = eventFieldSyntax.Declaration;
                    var typeName = declaration.Type.ToFullyQualifiedName(model);
                    var eventName = declaration.Variables.ToString();
                    sb.AppendLineC($"event {typeName} {eventName}");
                }
            }
            closeType?.Invoke();

            // Add interface to the Class
            if (classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                sb.AppendLine();
                sb.AppendLine($"partial class {className} : {interfaceName} {{}}");
            }
            // ------ Close
            closeNamespace?.Invoke();
            context.AddSource($"{className}.{interfaceName}.g.cs", sb.ToString());
            sb.Clear();
        }
    }
}
