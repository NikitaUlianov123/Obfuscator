using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C_Obfuscation
{

    // Collector: find symbols safe to rename
    class IdentifierCollector : CSharpSyntaxWalker
    {
        private SemanticModel _model;
        private Dictionary<ISymbol, string> _map;
        public IdentifierCollector(SemanticModel model, Dictionary<ISymbol, string> map)
        {
            _model = model;
            _map = map;
        }
        
        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var sym = _model.GetDeclaredSymbol(node);
            if (CanRenameSymbol(sym))
            {
                _map[sym] = Obfuscator.NewName();
            }
            base.VisitVariableDeclarator(node);
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            var sym = _model.GetDeclaredSymbol(node);
            if (CanRenameSymbol(sym))
            {
                _map[sym] = Obfuscator.NewName();
            }
            base.VisitParameter(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var sym = _model.GetDeclaredSymbol(node);
            if (CanRenameSymbol(sym))
            {
                _map[sym] = Obfuscator.NewName();
            }
            base.VisitMethodDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var sym = _model.GetDeclaredSymbol(node);
            if (CanRenameSymbol(sym))
            {
                _map[sym] = Obfuscator.NewName();
            }
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var v in node.Declaration.Variables)
            {
                var sym = _model.GetDeclaredSymbol(v);
                if (CanRenameSymbol(sym))
                {
                    _map[sym] = Obfuscator.NewName();
                }
            }
            base.VisitFieldDeclaration(node);
        }

        private bool CanRenameSymbol(ISymbol sym)
        {
            if (sym == null) return false;

            // keep public/protected/internal API names intact
            if (sym.DeclaredAccessibility == Accessibility.Public
                || sym.DeclaredAccessibility == Accessibility.Protected
                || sym.DeclaredAccessibility == Accessibility.ProtectedOrInternal
                || sym.DeclaredAccessibility == Accessibility.Internal)
                return false;

            // don't rename overrides, interface implementations, entry point, or extern
            if (sym.IsOverride || sym.IsImplicitlyDeclared || sym.IsStatic && sym.Name == "Main") return false;
            if (sym.Kind == SymbolKind.NamedType) return false; // keep type names for simplicity

            // don't rename if symbol is used via reflection attributes (basic heuristic)
            // (A production tool would inspect attributes and reflection usage more deeply.)
            foreach (var attr in sym.GetAttributes())
            {
                if (attr.AttributeClass.Name.Contains("Json") || attr.AttributeClass.Name.Contains("Serializable"))
                    return false;
            }

            return true;
        }
    }
}
