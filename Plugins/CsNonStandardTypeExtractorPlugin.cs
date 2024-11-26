using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SemanticKernel;
using Spectre.Console;

[Description("Plugin to extract non-standard types from C# code.")]
internal sealed class CsNonStandardTypeExtractorPlugin
{
    [KernelFunction("extract_non_standard_types")]
    [Description("Extracts non-standard types from the provided C# code string.")]
    [return: Description("List of non-standard type names used in the provided C# code.")]
    public List<string> ExtractNonStandardTypes(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            AnsiConsole.MarkupLine("[red]Error: Input code cannot be null or empty.[/]");
            return [];
        }

        // TODO skip errors
        return AnsiConsole.Status().Start("Searching non standard types...", ctx =>
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            var typeCollector = new TypeCollector();
            typeCollector.Visit(root);

            return new List<string>(typeCollector.NonStandardTypes);
        });
    }

    private class TypeCollector : CSharpSyntaxWalker
    {
        private readonly HashSet<string> standardTypes = new HashSet<string>
        {
            "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint",
            "long", "ulong", "object", "short", "ushort", "string", "void"
        };

        public HashSet<string> NonStandardTypes { get; } = new HashSet<string>();

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            string typeName = node.Identifier.Text;
            if (!standardTypes.Contains(typeName) && char.IsUpper(typeName[0]))
            {
                NonStandardTypes.Add(typeName);
            }
            base.VisitIdentifierName(node);
        }
    }
}
