using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace C_Obfuscation
{
    
    internal class IdentifierCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel model;
        
        private readonly Dictionary<string, string> map;

        public IdentifierCollector(SemanticModel model, Dictionary<string, string> map)
        {
            this.model = model;
            this.map = map;
        }
        // generate a unique key for a symbol
        private static string SymbolKey(ISymbol sym)
        {
            
            return sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        // attempt to add a symbol to the rename map
        private void TryAddSymbol(ISymbol sym)
        {
            if (sym == null) return;
            if (!CanRenameSymbol(sym)) return;

            string key = SymbolKey(sym);
            if (!map.ContainsKey(key))
            {
                map[key] = Obfuscator.NewName();
            }
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

            if (sym != null && CanRenameSymbol(sym))
            {
                string key = SymbolKey(sym);
                var anno = new SyntaxAnnotation("RenameKey", key);
                var newNode = node.WithAdditionalAnnotations(anno);
                
            }

            base.VisitVariableDeclarator(node);
        }
        // visit parameter declarations
        public override void VisitParameter(ParameterSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);

            if (sym != null && CanRenameSymbol(sym))
            {
                string key = SymbolKey(sym);
                var anno = new SyntaxAnnotation("RenameKey", key);
                var newNode = node.WithAdditionalAnnotations(anno);
                
            }

            base.VisitParameter(node);
        }
        // visit method declarations
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);

            if (sym != null && CanRenameSymbol(sym))
            {
                string key = SymbolKey(sym);
                var anno = new SyntaxAnnotation("RenameKey", key);
                var newNode = node.WithAdditionalAnnotations(anno);
                
            }

            base.VisitMethodDeclaration(node);
        }
        // visit property declarations
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var sym = model.GetDeclaredSymbol(node);
            TryAddSymbol(sym);

            if (sym != null && CanRenameSymbol(sym))
            {
                string key = SymbolKey(sym);
                var anno = new SyntaxAnnotation("RenameKey", key);
                var newNode = node.WithAdditionalAnnotations(anno);
               
            }

            base.VisitPropertyDeclaration(node);
        }
        // visit field declarations
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            
            base.VisitFieldDeclaration(node);
        }

        // visit identifier names
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            
            var sym = model.GetSymbolInfo(node).Symbol ?? model.GetDeclaredSymbol(node);

            if (sym != null && CanRenameSymbol(sym))
            {
                
                TryAddSymbol(sym);

                string key = SymbolKey(sym);
                var anno = new SyntaxAnnotation("RenameKey", key);
                var newNode = node.WithAdditionalAnnotations(anno);
                
            }

            base.VisitIdentifierName(node);
        }
    }
}
