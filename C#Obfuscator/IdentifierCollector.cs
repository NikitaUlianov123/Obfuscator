using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace C_Obfuscation
{
    public class IdentifierCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel model;
        private readonly Dictionary<ISymbol, string> map;

        public IdentifierCollector(SemanticModel model, Dictionary<ISymbol, string> map) : base(SyntaxWalkerDepth.Trivia)
        {
            this.model = model;
            this.map = map;
        }

       

        // attempt to add a symbol to the rename map
        private void TryAddSymbol(ISymbol? sym)
        {
            if (sym is null) return;
            if (!CanRenameSymbol(sym)) return;

            var canonical = sym.OriginalDefinition ?? sym;

            if (!map.ContainsKey(canonical))
                map[canonical] = Obfuscator.NewName(sym.Name);
        }

        // determine if a symbol is eligible for renaming
        private bool CanRenameSymbol(ISymbol sym)
        {
            if (sym == null) return false;

            // keep public/protected/internal API names intact
            switch (sym.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.Internal:
                    return false;
            }

            // don't rename overrides, implicit decls, or the entrypoint Main
            if (sym.IsOverride || sym.IsImplicitlyDeclared) return false;
            if (sym.IsStatic && sym.Name == "Main") return false;

            // don't rename types
            if (sym.Kind == SymbolKind.NamedType) return false;

            // avoid renaming if attributes suggest reflection usage
            foreach (var attr in sym.GetAttributes())
            {
                var n = attr.AttributeClass?.Name ?? "";
                if (n.Contains("Json") || n.Contains("Serializable"))
                    return false;
            }

            return true;
        }

        // visit variable declarators
        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);

            base.VisitVariableDeclarator(node);
        }

        // visit parameter declarations
        public override void VisitParameter(ParameterSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);

            base.VisitParameter(node);
        }

        // visit method declarations
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);

            base.VisitMethodDeclaration(node);
        }

        // visit property declarations
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);

            base.VisitPropertyDeclaration(node);
        }

        // visit field declarations
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var v in node.Declaration.Variables)
            {
                var sym = model.GetDeclaredSymbol(v);
                TryAddSymbol(sym);
            }

            base.VisitFieldDeclaration(node);
        }

        // visit identifier names (captures references that may need renaming)
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var sym = model.GetSymbolInfo(node).Symbol ?? model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);

            base.VisitIdentifierName(node);
        }
    }
}
