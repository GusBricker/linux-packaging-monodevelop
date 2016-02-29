using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using System.Linq;

namespace RefactoringEssentials.CSharp.Diagnostics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [NotPortedYet]
    public class ExpressionIsNeverOfProvidedTypeAnalyzer : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "ExpressionIsNeverOfProvidedTypeAnalyzer";
        const string Description = "CS0184:Given expression is never of the provided type";
        const string MessageFormat = "";
        const string Category = DiagnosticAnalyzerCategories.CompilerWarnings;

        static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "CS0184:Given expression is never of the provided type");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            //context.RegisterSyntaxNodeAction(
            //	(nodeContext) => {
            //		Diagnostic diagnostic;
            //		if (TryGetDiagnostic (nodeContext, out diagnostic)) {
            //			nodeContext.ReportDiagnostic(diagnostic);
            //		}
            //	}, 
            //	new SyntaxKind[] { SyntaxKind.None }
            //);
        }

        static bool TryGetDiagnostic(SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
        {
            diagnostic = default(Diagnostic);
            if (nodeContext.IsFromGeneratedCode())
                return false;
            //var node = nodeContext.Node as ;
            //diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
            //return true;
            return false;
        }

        //		class GatherVisitor : GatherVisitorBase<ExpressionIsNeverOfProvidedTypeAnalyzer>
        //		{
        //			//readonly CSharpConversions conversions;

        //			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        //				: base(semanticModel, addDiagnostic, cancellationToken)
        //			{
        //				//conversions = CSharpConversions.Get(ctx.Compilation);
        //			}

        ////			public override void VisitIsExpression(IsExpression isExpression)
        ////			{
        ////				base.VisitIsExpression(isExpression);
        ////
        //////				var conversions = CSharpConversions.Get(ctx.Compilation);
        ////				var exprType = ctx.Resolve(isExpression.Expression).Type;
        ////				var providedType = ctx.ResolveType(isExpression.Type);
        ////
        ////				if (exprType.Kind == TypeKind.Unknown || providedType.Kind == TypeKind.Unknown)
        ////					return;
        ////				if (IsValidReferenceOrBoxingConversion(exprType, providedType))
        ////					return;
        ////				
        ////				var exprTP = exprType as ITypeParameter;
        ////				var providedTP = providedType as ITypeParameter;
        ////				if (exprTP != null) {
        ////					if (IsValidReferenceOrBoxingConversion(exprTP.EffectiveBaseClass, providedType)
        ////					    && exprTP.EffectiveInterfaceSet.All(i => IsValidReferenceOrBoxingConversion(i, providedType)))
        ////						return;
        ////				}
        ////				if (providedTP != null) {
        ////					if (IsValidReferenceOrBoxingConversion(exprType, providedTP.EffectiveBaseClass))
        ////						return;
        ////				}
        ////				
        ////				AddDiagnosticAnalyzer(new CodeIssue(isExpression, ctx.TranslateString("Given expression is never of the provided type")));
        ////			}
        ////
        ////			bool IsValidReferenceOrBoxingConversion(IType fromType, IType toType)
        ////			{
        ////				Conversion c = conversions.ExplicitConversion(fromType, toType);
        ////				return c.IsValid && (c.IsIdentityConversion || c.IsReferenceConversion || c.IsBoxingConversion || c.IsUnboxingConversion);
        ////			}
        //		}
    }

    [ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
    [NotPortedYet]
    public class ExpressionIsNeverOfProvidedTypeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(ExpressionIsNeverOfProvidedTypeAnalyzer.DiagnosticId);
            }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var diagnostic = diagnostics.First();
            var node = root.FindNode(context.Span);
            //if (!node.IsKind(SyntaxKind.BaseList))
            //	continue;
            var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
            context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
        }
    }
}