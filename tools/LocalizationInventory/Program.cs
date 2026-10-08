using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

Console.OutputEncoding = new System.Text.UTF8Encoding(false);
if (args.Length != 2)
    throw new ArgumentException("Usage: LocalizationInventory <root> <C# path manifest.json>");
string root = Path.GetFullPath(args[0]);
var paths = JsonSerializer.Deserialize<string[]>(File.ReadAllText(args[1]))!;
var jsonOptions = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
foreach (string path in paths)
{
    var source = SourceText.From(File.ReadAllText(Path.Combine(root, path)));
    var records = new Dictionary<(int, int, string), Dictionary<string, object>>();
    foreach (string api in new[] { "1.3", "1.4" })
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview,
            preprocessorSymbols: api == "1.4" ? new[] { "BANNERLORD_1_4_OR_GREATER" } : Array.Empty<string>());
        var syntax = CSharpSyntaxTree.ParseText(source, options).GetRoot();
        void Add(TextSpan span, string kind, string text, string raw, SyntaxNode? node)
        {
            if (text.Length == 0) return;
            var key = (span.Start, span.Length, kind);
            if (records.TryGetValue(key, out var previous))
            {
                ((List<string>)previous["api_variants"]).Add(api);
                return;
            }
            var location = source.Lines.GetLinePositionSpan(span);
            var member = node?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            string symbol = member switch
            {
                MethodDeclarationSyntax method => method.Identifier.ValueText,
                ConstructorDeclarationSyntax ctor => ctor.Identifier.ValueText,
                PropertyDeclarationSyntax property => property.Identifier.ValueText,
                FieldDeclarationSyntax field => string.Join(",", field.Declaration.Variables.Select(v => v.Identifier.ValueText)),
                TypeDeclarationSyntax type => type.Identifier.ValueText,
                _ => ""
            };
            var call = node?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            var creation = node?.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
            string context = call?.Expression.ToString() ?? creation?.Type.ToString() ?? "";
            records[key] = new Dictionary<string, object>
            {
                ["path"] = path, ["line"] = location.Start.Line + 1,
                ["column"] = location.Start.Character + 1, ["end_line"] = location.End.Line + 1,
                ["offset"] = span.Start, ["length"] = span.Length,
                ["offset_unit"] = "utf16_code_units",
                ["kind"] = kind, ["text"] = text, ["raw"] = raw,
                ["symbol"] = symbol, ["context"] = context,
                ["api_variants"] = new List<string> { api }
            };
        }
        foreach (var token in syntax.DescendantTokens())
        {
            string name = token.Kind().ToString();
            if (name.EndsWith("StringLiteralToken", StringComparison.Ordinal))
                Add(token.Span, "csharp.string", token.ValueText, token.Text, token.Parent);
            else if (token.IsKind(SyntaxKind.CharacterLiteralToken))
                Add(token.Span, "csharp.char", token.ValueText, token.Text, token.Parent);
        }
        foreach (var interpolation in syntax.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
        {
            string text = string.Concat(interpolation.Contents.Select(part => part is InterpolatedStringTextSyntax literal
                ? literal.TextToken.ValueText : part.ToString()));
            Add(interpolation.Span, "csharp.interpolated", text, interpolation.ToString(), interpolation);
        }
        foreach (var trivia in syntax.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.DisabledTextTrivia))
                Add(trivia.Span, "csharp.disabled", trivia.ToString(), trivia.ToString(), null);
            else if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                Add(trivia.Span, "csharp.comment", trivia.ToString(), trivia.ToString(), null);
        }
    }
    foreach (var record in records.Values.OrderBy(r => (int)r["offset"]).ThenBy(r => (string)r["kind"]))
        Console.WriteLine(JsonSerializer.Serialize(record, jsonOptions));
}
