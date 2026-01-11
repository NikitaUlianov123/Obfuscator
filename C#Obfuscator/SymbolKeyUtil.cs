using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// Public-API-only replacement for Roslyn's internal Microsoft.CodeAnalysis.SymbolKey.
/// Produces a deterministic string key for most symbol kinds (types/members/params, etc).
/// </summary>
internal static class SymbolKeyUtil
{
    /// <summary>
    /// Create a stable-ish key for an ISymbol. Suitable for dictionary keys/caching and
    /// cross-compilation matching when assemblies/names remain consistent.
    /// </summary>
    public static string Create(ISymbol? sym)
    {
        if (sym is null) return string.Empty;

        // Canonicalize similarly to Roslyn
        var canonical = sym.OriginalDefinition ?? sym;

        var sb = new StringBuilder(256);

        // Assembly name is key for disambiguation across references.
        var asm = canonical.ContainingAssembly?.Identity;
        sb.Append("asm:");
        sb.Append(asm?.Name ?? "<noasm>");
        sb.Append(';');

        sb.Append("kind:");
        sb.Append(canonical.Kind);
        sb.Append(';');

        sb.Append("sym:");
        sb.Append(BuildQualifiedSignature(canonical));
        sb.Append(';');

        return sb.ToString();
    }

    /// <summary>
    /// Convenience extension-style call without needing an extensions class.
    /// </summary>
    public static string ToStableKey(ISymbol? sym) => Create(sym);

    private static string BuildQualifiedSignature(ISymbol symbol)
    {
        // Build path from outermost container to the symbol.
        // Avoid locations and other compilation-variant data.
        var parts = new Stack<string>();

        ISymbol? current = symbol;
        while (current is not null &&
               current.Kind != SymbolKind.Assembly &&
               current.Kind != SymbolKind.NetModule)
        {
            parts.Push(SymbolPart(current));
            current = current.ContainingSymbol;
        }

        return string.Join("/", parts);
    }

    private static string SymbolPart(ISymbol s)
    {
        return s switch
        {
            INamespaceSymbol ns => "ns:" + (ns.IsGlobalNamespace ? "<global>" : ns.ToDisplayString()),

            INamedTypeSymbol nt => "type:" + TypeIdentity(nt),

            IMethodSymbol m => "m:" + MethodIdentity(m),

            IPropertySymbol p => "p:" + PropertyIdentity(p),

            IFieldSymbol f => "f:" + f.MetadataName,

            IEventSymbol e => "e:" + e.MetadataName,

            IParameterSymbol p => "param:" + p.MetadataName + ":" + TypeRef(p.Type),

            ITypeParameterSymbol tp => "tp:" + tp.Name + "#" + tp.Ordinal,

            // These are NOT stable across compilations; included only for best-effort uniqueness.
            ILocalSymbol l => "local:" + l.Name + ":" + TypeRef(l.Type),
            ILabelSymbol lab => "label:" + lab.Name,

            _ => s.Kind + ":" + (string.IsNullOrEmpty(s.MetadataName) ? s.Name : s.MetadataName)
        };
    }

    private static string TypeIdentity(INamedTypeSymbol t)
    {
        // MetadataName includes `1 etc for generic arity and is stable.
        // Containing types/namespaces are handled by BuildQualifiedSignature.
        var sb = new StringBuilder();
        sb.Append(t.MetadataName);

        // For constructed generic types, include arguments so List<int> != List<string>.
        // If t is an OriginalDefinition, TypeArguments will be type parameters and still fine,
        // but for canonical keys we generally pass OriginalDefinition for the main symbol.
        if (t.IsGenericType && t.TypeArguments.Length > 0 && !SymbolEqualityComparer.Default.Equals(t, t.OriginalDefinition))
        {
            sb.Append('<');
            sb.Append(string.Join(",", t.TypeArguments.Select(TypeRef)));
            sb.Append('>');
        }

        return sb.ToString();
    }

    private static string MethodIdentity(IMethodSymbol m)
    {
        // Include method kind + metadata name + arity + parameter types + return type.
        // Helps disambiguate overloads and operators/conversions.
        var sb = new StringBuilder();

        sb.Append(m.MethodKind);
        sb.Append(':');
        sb.Append(m.MetadataName);

        if (m.Arity > 0)
        {
            sb.Append('`');
            sb.Append(m.Arity);
        }

        sb.Append('(');
        sb.Append(string.Join(",", m.Parameters.Select(ParameterIdentity)));
        sb.Append(')');

        // Return type is especially useful for user-defined conversions/operators.
        sb.Append("->");
        sb.Append(TypeRef(m.ReturnType));

        return sb.ToString();
    }

    private static string ParameterIdentity(IParameterSymbol p)
    {
        var refPrefix = p.RefKind switch
        {
            RefKind.None => "",
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            RefKind.RefReadOnlyParameter => "refreadonly ",
            _ => p.RefKind.ToString().ToLowerInvariant() + " "
        };

        return refPrefix + TypeRef(p.Type);
    }

    private static string PropertyIdentity(IPropertySymbol p)
    {
        // Properties: name + (indexer params if any) + property type
        var sb = new StringBuilder();
        sb.Append(p.MetadataName);

        if (p.Parameters.Length > 0)
        {
            sb.Append('[');
            sb.Append(string.Join(",", p.Parameters.Select(ParameterIdentity)));
            sb.Append(']');
        }

        sb.Append("->");
        sb.Append(TypeRef(p.Type));
        return sb.ToString();
    }

    private static string TypeRef(ITypeSymbol t)
    {
        // Fully-qualified avoids ambiguity with using aliases.
        return t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}