using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace RefactoringEssentials.CSharp.Diagnostics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [NotPortedYet]
    public class LocalVariableHidesMemberAnalyzer : VariableHidesMemberAnalyzer
    {
        static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            CSharpDiagnosticIDs.LocalVariableHidesMemberAnalyzerID,
            GettextCatalog.GetString("Local variable has the same name as a member and hides it"),
            GettextCatalog.GetString("Local variable '{0}' hides {1} '{2}'"),
            DiagnosticAnalyzerCategories.CodeQualityIssues,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.CreateFor(CSharpDiagnosticIDs.LocalVariableHidesMemberAnalyzerID)
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

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

        //		class GatherVisitor : GatherVisitorBase<LocalVariableHidesMemberAnalyzer>
        //		{
        //			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        //				: base (semanticModel, addDiagnostic, cancellationToken)
        //			{
        //			}

        ////			public override void VisitVariableInitializer (VariableInitializer variableInitializer)
        ////			{
        ////				base.VisitVariableInitializer(variableInitializer);
        ////
        ////				if (!(ctx.Resolve (variableInitializer) is LocalResolveResult))
        ////					return;
        ////				var mre = variableInitializer.Initializer as MemberReferenceExpression;
        ////				if (mre != null && mre.MemberName == variableInitializer.Name && mre.Target is ThisReferenceExpression) {
        ////					// Special case: the variable is initialized from the member it is hiding
        ////					// In this case, the hiding is obviously intentional and we shouldn't show a warning.
        ////					return;
        ////				}
        ////				CheckLocal(variableInitializer, variableInitializer.Name, variableInitializer.NameToken);
        ////			}
        ////
        ////			void CheckLocal(AstNode node, string name, AstNode token)
        ////			{
        ////				IMember member;
        ////				if (HidesMember(ctx, node, name, out member)) {
        ////					string msg;
        ////					switch (member.SymbolKind) {
        ////						case SymbolKind.Field:
        ////							msg = ctx.TranslateString("Local variable '{0}' hides field '{1}'");
        ////							break;
        ////							case SymbolKind.Method:
        ////							msg = ctx.TranslateString("Local variable '{0}' hides method '{1}'");
        ////							break;
        ////							case SymbolKind.Property:
        ////							msg = ctx.TranslateString("Local variable '{0}' hides property '{1}'");
        ////							break;
        ////							case SymbolKind.Event:
        ////							msg = ctx.TranslateString("Local variable '{0}' hides event '{1}'");
        ////							break;
        ////							default:
        ////							msg = ctx.TranslateString("Local variable '{0}' hides member '{1}'");
        ////							break;
        ////					}
        ////					AddDiagnosticAnalyzer(new CodeIssue(token, string.Format(msg, name, member.FullName)));
        ////				}
        ////			}
        ////
        ////			public override void VisitForeachStatement (ForeachStatement foreachStatement)
        ////			{
        ////				base.VisitForeachStatement (foreachStatement);
        ////
        ////				CheckLocal(foreachStatement, foreachStatement.VariableName, foreachStatement.VariableNameToken);
        ////			}
        ////		}
        ////	}
        ////
        ////	public class MemberCollectionService
        ////	{
        ////		Dictionary<Tuple<IType, string>, IMember[]> memberCache = new Dictionary<Tuple<IType, string>, IMember[]> ();
        ////		Dictionary<IType, List<IMember>> allMembers = new Dictionary<IType, List<IMember>> ();
        ////
        ////		public IMember[] GetMembers(IType type, string variableName)
        ////		{
        ////			IMember[] members;
        ////			Tuple<IType, string> key = Tuple.Create(type, variableName);
        ////			if (!memberCache.TryGetValue (key, out members)) {
        ////				lock (memberCache) {
        ////					if (!memberCache.TryGetValue(key, out members)) {
        ////						List<IMember> am;
        ////						if (!allMembers.TryGetValue(type, out am)) {
        ////							lock (allMembers) {
        ////								if (!allMembers.TryGetValue(type, out am)) {
        ////									am = new List<IMember>(type.GetMembers());
        ////									allMembers [type] = am;
        ////								}
        ////							}
        ////						}
        ////						members = am.Where(m => m.Name == variableName).ToArray();
        ////						memberCache.Add(key, members);
        ////					}
        ////				}
        ////			}
        ////			return members;
        //		}
    }

    public abstract class VariableHidesMemberAnalyzer : DiagnosticAnalyzer
    {
        //		protected static bool HidesMember(BaseSemanticModel ctx, AstNode node, string variableName)
        //		{
        //			IMember member;
        //			return HidesMember(ctx, node, variableName, out member);
        //		}
        //
        //		protected static bool HidesMember(BaseSemanticModel ctx, AstNode node, string variableName, out IMember member)
        //		{
        //			MemberCollectionService mcs = (MemberCollectionService)ctx.GetService(typeof(MemberCollectionService));
        //			if (mcs == null) {
        //				lock (ctx) {
        //					if ((mcs = (MemberCollectionService)ctx.GetService(typeof(MemberCollectionService))) == null) {
        //						mcs = new MemberCollectionService();
        //						ctx.Services.AddService(typeof(MemberCollectionService), mcs);
        //					}
        //				}
        //			}
        //
        //			var typeDecl = node.GetParent<TypeDeclaration>();
        //			member = null;
        //			if (typeDecl == null)
        //				return false;
        //			var entityDecl = node.GetParent<EntityDeclaration>();
        //			var memberResolveResult = ctx.Resolve(entityDecl) as MemberResolveResult;
        //			if (memberResolveResult == null)
        //				return false;
        //			var typeResolveResult = ctx.Resolve(typeDecl) as TypeResolveResult;
        //			if (typeResolveResult == null)
        //				return false;
        //
        //			var sourceMember = memberResolveResult.Member;
        //
        //			member = mcs.GetMembers (typeResolveResult.Type, variableName).FirstOrDefault(m2 => IsAccessible(sourceMember, m2));
        //			return member != null;
        //		}
        //
        //		static bool IsAccessible(IMember sourceMember, IMember targetMember)
        //		{
        //			if (sourceMember.IsStatic != targetMember.IsStatic)
        //				return false;
        //
        //			var sourceType = sourceMember.DeclaringType;
        //			var targetType = targetMember.DeclaringType;
        //			switch (targetMember.Accessibility)
        //			{
        //				case Accessibility.None:
        //					return false;
        //					case Accessibility.Private:
        //					// check for members of outer classes (private members of outer classes can be accessed)
        //					var targetTypeDefinition = targetType.GetDefinition();
        //					for (var t = sourceType.GetDefinition(); t != null; t = t.DeclaringTypeDefinition)
        //					{
        //						if (t.Equals(targetTypeDefinition))
        //							return true;
        //					}
        //					return false;
        //					case Accessibility.Public:
        //					return true;
        //					case Accessibility.Protected:
        //					return IsProtectedAccessible(sourceType, targetType);
        //					case Accessibility.Internal:
        //					return IsInternalAccessible(sourceMember.ParentAssembly, targetMember.ParentAssembly);
        //					case Accessibility.ProtectedOrInternal:
        //					return IsInternalAccessible(sourceMember.ParentAssembly, targetMember.ParentAssembly) || IsProtectedAccessible(sourceType, targetType);
        //					case Accessibility.ProtectedAndInternal:
        //					return IsInternalAccessible(sourceMember.ParentAssembly, targetMember.ParentAssembly) && IsProtectedAccessible(sourceType, targetType);
        //					default:
        //					throw new Exception("Invalid value for Accessibility");
        //			}
        //		}
        //
        //		static bool IsProtectedAccessible(IType sourceType, IType targetType)
        //		{
        //			return sourceType.GetAllBaseTypes().Any(type => targetType.Equals(type));
        //		}
        //
        //		static bool IsInternalAccessible(IAssembly sourceAssembly, IAssembly targetAssembly)
        //		{
        //			return sourceAssembly.InternalsVisibleTo(targetAssembly);
        //		}
    }
}