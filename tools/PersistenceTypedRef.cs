using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.Json;
using Microsoft.CodeAnalysis;using Microsoft.CodeAnalysis.CSharp;using Microsoft.CodeAnalysis.CSharp.Syntax;
class Program {
 static void Main(string[] args) {
 var inputs=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[0]));
 var trees=inputs.Select(x=>CSharpSyntaxTree.ParseText(x.Value,path:x.Key)).ToArray();
 var refs=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(x=>MetadataReference.CreateFromFile(x));
 var compilation=CSharpCompilation.Create("TypedRefAuditExperiment",trees,refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
 var result=new List<object>();
 foreach(var tree in trees){var model=compilation.GetSemanticModel(tree);foreach(var call in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()){
 var name=call.Expression is MemberAccessExpressionSyntax m?m.Name.Identifier.ValueText:call.Expression is IdentifierNameSyntax i?i.Identifier.ValueText:"";
 if(name!="SyncData"||call.ArgumentList.Arguments.Count<2)continue;
 var key=call.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
 var arg=call.ArgumentList.Arguments[1];if(key==null||!key.IsKind(SyntaxKind.StringLiteralExpression)||!arg.RefKindKeyword.IsKind(SyntaxKind.RefKeyword))continue;
 var symbol=model.GetSymbolInfo(arg.Expression).Symbol;ITypeSymbol type=null;
 if(symbol is ILocalSymbol local)type=local.Type;
 if(symbol is IParameterSymbol parameter)type=parameter.Type;
 if(symbol is IFieldSymbol field&&!field.IsReadOnly)type=field.Type;
 string syntaxType=null;
 if(symbol!=null)foreach(var reference in symbol.DeclaringSyntaxReferences){var declaration=reference.GetSyntax();
  if(declaration is ParameterSyntax p)syntaxType=p.Type?.ToString();
  if(declaration is VariableDeclaratorSyntax v&&v.Parent is VariableDeclarationSyntax d)syntaxType=d.Type.ToString();
 }
 var resolved=type!=null&&type.TypeKind!=TypeKind.Error&&syntaxType!=null&&syntaxType!="var";
 result.Add(new{path=tree.FilePath,line=tree.GetLineSpan(call.Span).StartLinePosition.Line+1,key=key.Token.ValueText,expression=arg.Expression.ToString(),symbolKind=symbol?.Kind.ToString(),type=resolved?syntaxType:"UNRESOLVED",semanticType=type?.ToDisplayString(),reason=resolved?"declared symbol type":"unbound/error/unsupported non-variable or inferred type"});
 }}File.WriteAllText(args[1],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("typed-ref rows="+result.Count);
 }
}