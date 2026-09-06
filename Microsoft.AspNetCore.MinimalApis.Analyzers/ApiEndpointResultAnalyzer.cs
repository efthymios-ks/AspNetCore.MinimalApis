using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.MinimalApis.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApiEndpointResultAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "APIEP001";

    private static readonly DiagnosticDescriptor _rule = new(
        id: DiagnosticId,
        title: "Endpoint handler must return IResult",
        messageFormat: "Handler '{0}' must return IResult, Task<IResult> or ValueTask<IResult>",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Handlers mapped inside an ApiEndpoint must return IResult so the response contract is explicit.");

    private static readonly ImmutableHashSet<string> _mapMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapMethods"
    );

    private const string ApiEndpointMetadataName = "Microsoft.AspNetCore.MinimalApis.ApiEndpoints.ApiEndpoint";
    private const string IResultMetadataName = "Microsoft.AspNetCore.Http.IResult";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            var compilation = start.Compilation;

            var iResult = compilation.GetTypeByMetadataName(IResultMetadataName);
            var apiEndpoint = compilation.GetTypeByMetadataName(ApiEndpointMetadataName);
            if (iResult is null || apiEndpoint is null)
            {
                return;
            }

            var taskT = compilation.GetTypeByMetadataName(typeof(Task<>).FullName!);
            var valueTaskT = compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName!);

            var known = new KnownTypes(iResult, apiEndpoint, taskT, valueTaskT);
            start.RegisterSyntaxNodeAction(ctx => Analyze(ctx, known), SyntaxKind.InvocationExpression);
        });
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, KnownTypes known)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax member)
        {
            return;
        }

        if (!_mapMethodNames.Contains(member.Name.Identifier.Text))
        {
            return;
        }

        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0)
        {
            return;
        }

        var enclosingType = context.SemanticModel
            .GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken)?
            .ContainingType;
        if (!DerivesFrom(enclosingType, known.ApiEndpoint))
        {
            return;
        }

        var handler = args[args.Count - 1].Expression;

        var returnType = HandlerReturnType(context.SemanticModel, handler, context.CancellationToken);
        if (returnType is null)
        {
            return;
        }

        if (!ReturnsIResult(returnType, known))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(_rule, handler.GetLocation(), handler.ToString()));
        }
    }

    private static bool DerivesFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static ITypeSymbol? HandlerReturnType(
        SemanticModel model,
        ExpressionSyntax handler,
        CancellationToken cancellationToken
    )
    {
        var info = model.GetSymbolInfo(handler, cancellationToken);

        if (info.Symbol is IMethodSymbol method)
        {
            return method.ReturnType;
        }

        if (info.Symbol is null
            && info.CandidateSymbols.Length == 1
            && info.CandidateSymbols[0] is IMethodSymbol single
        )
        {
            return single.ReturnType;
        }

        if (model.GetTypeInfo(handler, cancellationToken).ConvertedType is INamedTypeSymbol del
            && del.DelegateInvokeMethod is { } invoke
        )
        {
            return invoke.ReturnType;
        }

        return null;
    }

    private static bool ReturnsIResult(ITypeSymbol returnType, KnownTypes known)
    {
        var unwrapped = returnType;

        if (returnType is INamedTypeSymbol { IsGenericType: true } named
            && (SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, known.TaskT)
                || SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, known.ValueTaskT)
            )
        )
        {
            unwrapped = named.TypeArguments[0];
        }

        return SymbolEqualityComparer.Default.Equals(unwrapped, known.IResult)
            || unwrapped.AllInterfaces.Any(@interface => SymbolEqualityComparer.Default.Equals(@interface, known.IResult));
    }

    private readonly struct KnownTypes(
        INamedTypeSymbol iResult,
        INamedTypeSymbol apiEndpoint,
        INamedTypeSymbol? taskT,
        INamedTypeSymbol? valueTaskT
    )
    {
        public INamedTypeSymbol IResult { get; } = iResult;
        public INamedTypeSymbol ApiEndpoint { get; } = apiEndpoint;
        public INamedTypeSymbol? TaskT { get; } = taskT;
        public INamedTypeSymbol? ValueTaskT { get; } = valueTaskT;
    }
}
