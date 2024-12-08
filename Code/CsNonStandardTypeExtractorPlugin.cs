using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyAi.Code;

public sealed class CsNonStandardTypeExtractorPlugin
{
    public List<string> ExtractNonStandardTypes(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return [];
        }

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var typeCollector = new TypeCollector();
        typeCollector.Visit(root);

        return [.. typeCollector.NonStandardTypes];
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
