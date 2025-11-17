using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C_Obfuscation
{
    // Collects all symbols that should be renamed.
    internal class IdentifierCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel model;

        // Now the map is string->string (symbolKey => newName)
        private readonly Dictionary<string, string> map;

        public IdentifierCollector(SemanticModel model, Dictionary<string, string> map)
        {
            this.model = model;
            this.map = map;
        }

        // Convert any symbol to a stable identity string
        private static string SymbolKey(ISymbol sym)
        {
            return sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);
            base.VisitVariableDeclarator(node);
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);
            base.VisitParameter(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var v in node.Declaration.Variables)
            {
                var sym = model.GetDeclaredSymbol(v);
                TryAddSymbol(sym);
            }

            base.VisitFieldDeclaration(node);
        }

        // Helper to check if a symbol should be renamed, then add it to the map
        private void TryAddSymbol(ISymbol sym)
        {
            if (CanRenameSymbol(sym))
            {
                string key = SymbolKey(sym);

                if (!map.ContainsKey(key))
                {
                    map[key] = Obfuscator.NewName();
                }
            }
        }

        private bool CanRenameSymbol(ISymbol sym)
        {
            if (sym == null)
                return false;

            // don't rename public API
            switch (sym.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Internal:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return false;
            }

            if (sym.IsOverride)
                return false;

            if (sym.IsImplicitlyDeclared)
                return false;

            // don't rename Main
            if (sym.IsStatic && sym.Name == "Main")
                return false;

            // Don't rename types (class, struct, interface)
            if (sym.Kind == SymbolKind.NamedType)
                return false;

            // Skip symbols used by serializers or reflection (rough heuristic)
            foreach (var attr in sym.GetAttributes())
            {
                string n = attr.AttributeClass?.Name ?? "";
                if (n.Contains("Json") || n.Contains("Serializable"))
                    return false;
            }

            return true;
        }
    }
}
