// 
// AspNetEditorExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.AspNet;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Html;
using MonoDevelop.DesignerSupport;

//I initially aliased this as SE, which (unintentionally) looked a little odd with the XDOM types :-)
using S = MonoDevelop.Xml.StateEngine; 
using MonoDevelop.AspNet.StateEngine;
using System.Text;
using System.Text.RegularExpressions;

namespace MonoDevelop.AspNet.Gui
{
	public class AspNetEditorExtension : BaseHtmlEditorExtension
	{
		AspNetParsedDocument aspDoc;
		AspNetAppProject project;
		DocumentReferenceManager refman = new DocumentReferenceManager ();
		
		bool HasDoc { get { return aspDoc != null; } }
		bool HasProject { get { return project != null; } }
		 
		Regex DocTypeRegex = new Regex (@"(?:PUBLIC|public)\s+""(?<fpi>[^""]*)""\s+""(?<uri>[^""]*)""");
		
		#region Setup and teardown
		
		protected override S.RootState CreateRootState ()
		{
			return new AspNetFreeState ();
		}
		
		#endregion
		
		protected override void OnParsedDocumentUpdated ()
		{
			base.OnParsedDocumentUpdated ();
			aspDoc = CU as AspNetParsedDocument;
			project = base.Document.Project as AspNetAppProject;
			if (HasProject)
				refman.Project = project;
			if (HasDoc)
				refman.Doc = aspDoc;
		}
		
		/// <summary>
		/// This wraps a project dom and adds the compilation information from the ASP.NET page to the DOM to lookup members
		/// on the page.
		/// </summary>
		class DomWrapper : ProjectDomDecorator
		{
			ParsedDocument doc, localDoc;
			
			public DomWrapper (ProjectDom decorated, ParsedDocument doc, ParsedDocument localDoc) : base (decorated)
			{
				this.doc = doc;
				this.localDoc = localDoc;
			}
			IType constructedType = null;
			MonoDevelop.Projects.Dom.IType CheckType (MonoDevelop.Projects.Dom.IType type)
			{
				if (type == null)
					return null;
				if (type.IsPartial && doc.CompilationUnit.Types[0].FullName == type.FullName) {
					if (constructedType == null) 
						constructedType = CompoundType.Merge (CompoundType.Merge (doc.CompilationUnit.Types[0], type), localDoc.CompilationUnit.Types[0]);
					constructedType.SourceProjectDom = this;
					return constructedType;
				}
				return type;
			}
			
			public override IType ResolveType (IType type)
			{
				if (type == constructedType)
					return type;
				return CheckType (base.ResolveType (type));
			}
			
			public override MonoDevelop.Projects.Dom.IType GetType (IReturnType returnType)
			{
				return CheckType (base.GetType (returnType));
			}

			public override MonoDevelop.Projects.Dom.IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
			{
				return CheckType (base.GetType (typeName, genericArguments, deepSearchReferences, caseSensitive));
			}

			public override MonoDevelop.Projects.Dom.IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive)
			{
				return CheckType (base.GetType (typeName, genericArgumentsCount, deepSearchReferences, caseSensitive));
			}
			
			public override System.Collections.Generic.IEnumerable<MonoDevelop.Projects.Dom.IType> GetInheritanceTree (IType type)
			{
				foreach (IType t in base.GetInheritanceTree (type)) {
					yield return CheckType (t);
				}
			}
		}
		
		ILanguageCompletionBuilder documentBuilder;
		MonoDevelop.Ide.Gui.Document hiddenDocument;
		LocalDocumentInfo localDocumentInfo;
		
		protected override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext,
		                                                            bool forced, ref int triggerWordLength)
		{
			ITextBuffer buf = this.Buffer;
			
			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			char currentChar = completionContext.TriggerOffset < 1? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 1);
			//char previousChar = completionContext.TriggerOffset < 2? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 2);
			
			//directive names
			if (Tracker.Engine.CurrentState is AspNetDirectiveState) {
				AspNetDirective directive = Tracker.Engine.Nodes.Peek () as AspNetDirective;
				if (HasDoc && directive != null && directive.Region.Start.Line == completionContext.TriggerLine &&
				    directive.Region.Start.Column + 4 == completionContext.TriggerLineOffset)
				{
					return DirectiveCompletion.GetDirectives (aspDoc.Type);
				}
				return null;
			} else if (Tracker.Engine.CurrentState is S.XmlNameState && Tracker.Engine.CurrentState.Parent is AspNetDirectiveState) {
				AspNetDirective directive = Tracker.Engine.Nodes.Peek () as AspNetDirective;
				if (HasDoc && directive != null && directive.Region.Start.Line == completionContext.TriggerLine &&
				    directive.Region.Start.Column + 5 == completionContext.TriggerLineOffset && char.IsLetter (currentChar))
				{
					triggerWordLength = 1;
					return DirectiveCompletion.GetDirectives (aspDoc.Type);
				}
				return null;
			}

			//non-xml tag completion
			if (currentChar == '<' && !(Tracker.Engine.CurrentState is S.XmlFreeState)) {
				var list = new CompletionDataList ();
				AddAspBeginExpressions (list);
				return list;
			}

			if (!HasDoc || aspDoc.Info.DocType == null) {
				//FIXME: get doctype from master page
				DocType = null;
			} else {
				DocType = new MonoDevelop.Xml.StateEngine.XDocType (DomLocation.Empty);
				var matches = DocTypeRegex.Match (aspDoc.Info.DocType);
				DocType.PublicFpi = matches.Groups["fpi"].Value;
				DocType.Uri = matches.Groups["uri"].Value;
			}
			
			//simple completion for ASP.NET expressions
			documentBuilder = HasDoc? LanguageCompletionBuilderService.GetBuilder (aspDoc.Info.Language) : null;
			
			// TODO: Detect <script> state here !!!
			if (documentBuilder != null && Tracker.Engine.CurrentState is AspNetExpressionState) {
				int start = Document.TextEditor.CursorPosition - Tracker.Engine.CurrentStateLength;
				if (Document.TextEditor.GetCharAt (start) == '=') {
					start++;
				}
				
				string sourceText = Document.TextEditor.GetText (start, Document.TextEditor.CursorPosition);

				MonoDevelop.AspNet.Parser.Internal.Location loc = new MonoDevelop.AspNet.Parser.Internal.Location ();
				int line, col;
 				Document.TextEditor.GetLineColumnFromPosition (start, out line, out col);
				loc.EndLine = loc.BeginLine = line;
				loc.EndColumn = loc.BeginColumn = col;
				
				var documentInfo = documentBuilder.BuildDocument (aspDoc, TextEditorData);
				
				localDocumentInfo = documentBuilder.BuildLocalDocument (documentInfo, TextEditorData, sourceText, true);
				
				MonoDevelop.Ide.Gui.HiddenTextEditorViewContent viewContent = new MonoDevelop.Ide.Gui.HiddenTextEditorViewContent ();
				viewContent.Project = Document.Project;
				viewContent.ContentName = localDocumentInfo.ParsedLocalDocument.FileName;
				
				viewContent.Text = localDocumentInfo.LocalDocument;
				viewContent.GetTextEditorData ().Caret.Offset = localDocumentInfo.CaretPosition;
				MonoDevelop.Ide.Gui.HiddenWorkbenchWindow workbenchWindow = new MonoDevelop.Ide.Gui.HiddenWorkbenchWindow ();
				workbenchWindow.ViewContent = viewContent;
				hiddenDocument = new MonoDevelop.Ide.Gui.Document (workbenchWindow);
				
				hiddenDocument.ParsedDocument = localDocumentInfo.ParsedLocalDocument;
				return documentBuilder.HandleCompletion (hiddenDocument, localDocumentInfo, new DomWrapper (ProjectDomService.GetProjectDom (Document.Project), documentInfo.ParsedDocument, localDocumentInfo.ParsedLocalDocument), currentChar, ref triggerWordLength);
			}
			
			return base.HandleCodeCompletion (completionContext, forced, ref triggerWordLength);
		}
		
		public override IParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
			if (Tracker.Engine.CurrentState is AspNetExpressionState && documentBuilder != null && localDocumentInfo != null && hiddenDocument != null)
				return documentBuilder.HandleParameterCompletion (hiddenDocument, localDocumentInfo, new DomWrapper (ProjectDomService.GetProjectDom (Document.Project), hiddenDocument.ParsedDocument, localDocumentInfo.ParsedLocalDocument), completionChar);
			
			return base.HandleParameterCompletion (completionContext, completionChar);
		}
		
		protected override void GetElementCompletions (CompletionDataList list)
		{
			S.XName parentName = GetParentElementName (0);
			
			//fallback
			if (!HasDoc) {
				AddAspBeginExpressions (list);
				string aspPrefix = "asp:";
				foreach (IType cls in WebTypeManager.ListSystemControlClasses (new DomType ("System.Web.UI.Control"), project))
					list.Add (new AspTagCompletionData (aspPrefix, cls));
				
				base.GetElementCompletions (list);
				return;
			}
			
			IType controlClass = null;
			
			if (parentName.HasPrefix) {
				controlClass = refman.GetControlType (parentName.Prefix, parentName.Name);
			} else {
				S.XName grandparentName = GetParentElementName (1);
				if (grandparentName.IsValid && grandparentName.HasPrefix)
					controlClass = refman.GetControlType (grandparentName.Prefix, grandparentName.Name);
			}
			
			//we're just in HTML
			if (controlClass == null) {
				//root element?
				if (!parentName.IsValid) {
					if (aspDoc.Info.Subtype == WebSubtype.WebControl) {
						AddHtmlTagCompletionData (list, Schema, new S.XName ("div"));
						AddAspBeginExpressions (list);
						list.AddRange (refman.GetControlCompletionData ());
						AddMiscBeginTags (list);
					} else if (!string.IsNullOrEmpty (aspDoc.Info.MasterPageFile)) {
						//FIXME: add the actual region names
						list.Add (new CompletionData ("asp:Content"));
					}
				}
				else {
					AddAspBeginExpressions (list);
					list.AddRange (refman.GetControlCompletionData ());
					base.GetElementCompletions (list);
				}
				return;
			}
			
			string defaultProp;
			bool childrenAsProperties = AreChildrenAsProperties (controlClass, out defaultProp);
			if (defaultProp != null && defaultProp.Length == 0)
				defaultProp = null;
			
			//parent permits child controls directly
			if (!childrenAsProperties) {
				AddAspBeginExpressions (list);
				list.AddRange (refman.GetControlCompletionData ());
				AddMiscBeginTags (list);
				//TODO: get correct parent for Content tags
				AddHtmlTagCompletionData (list, Schema, new S.XName ("body"));
				return;
			}
			
			//children of properties
			if (childrenAsProperties && (!parentName.HasPrefix || defaultProp != null)) {
				if (controlClass.SourceProjectDom == null) {
					LoggingService.LogWarning ("IType {0} does not have a SourceProjectDom", controlClass);
					return;
				}
				
				string propName = defaultProp ?? parentName.Name;
				IProperty property =
					GetAllProperties (controlClass.SourceProjectDom, controlClass)
						.Where (x => string.Compare (propName, x.Name, StringComparison.OrdinalIgnoreCase) == 0)
						.FirstOrDefault ();
				
				if (property == null)
					return;
				
				//sanity checks on attributes
				switch (GetPersistenceMode (property)) {
				case System.Web.UI.PersistenceMode.Attribute:
				case System.Web.UI.PersistenceMode.EncodedInnerDefaultProperty:
					return;
					
				case System.Web.UI.PersistenceMode.InnerDefaultProperty:
					if (!parentName.HasPrefix)
						return;
					break;
					
				case System.Web.UI.PersistenceMode.InnerProperty:
					if (parentName.HasPrefix)
						return;
					break;
				}
				
				//check if allows freeform ASP/HTML content
				if (property.ReturnType.FullName == "System.Web.UI.ITemplate") {
					AddAspBeginExpressions (list);
					AddMiscBeginTags (list);
					AddHtmlTagCompletionData (list, Schema, new S.XName ("body"));
					list.AddRange (refman.GetControlCompletionData ());
					return;
				}
				
				//FIXME:unfortunately ASP.NET doesn't seem to have enough type information / attributes
				//to be able to resolve the correct child types here
				//so we assume it's a list and have a quick hack to find arguments of strongly typed ILists
				
				IType collectionType = controlClass.SourceProjectDom.GetType (property.ReturnType);
				if (collectionType == null) {
					list.AddRange (refman.GetControlCompletionData ());
					return;
				}
				
				string addStr = "Add";
				IMethod meth = GetAllMethods (controlClass.SourceProjectDom, collectionType)
					.Where (m => m.Parameters.Count == 1 && m.Name == addStr).FirstOrDefault ();
				
				if (meth != null) {
					IType argType = controlClass.SourceProjectDom.GetType (meth.Parameters[0].ReturnType);
					if (argType != null && argType.IsBaseType (new DomReturnType ("System.Web.UI.Control"))) {
						list.AddRange (refman.GetControlCompletionData (argType));
						return;
					}
				}
				
				list.AddRange (refman.GetControlCompletionData ());
				return;
			}
			
			//properties as children of controls
			if (parentName.HasPrefix && childrenAsProperties)
			{
				if (controlClass.SourceProjectDom == null) {
					LoggingService.LogWarning ("IType {0} does not have a SourceProjectDom", controlClass);
				}
				
				foreach (IProperty prop in GetUniqueMembers<IProperty> (GetAllProperties (controlClass.SourceProjectDom, controlClass)))
					if (GetPersistenceMode (prop) != System.Web.UI.PersistenceMode.Attribute)
						list.Add (prop.Name, prop.StockIcon, prop.Documentation);
				return;
			}
		}
		
		protected override CompletionDataList GetAttributeCompletions (S.IAttributedXObject attributedOb,
		                                                 Dictionary<string, string> existingAtts)
		{
			var list = base.GetAttributeCompletions (attributedOb, existingAtts) ?? new CompletionDataList ();
			if (attributedOb is S.XElement) {
				
				if (!existingAtts.ContainsKey ("runat"))
					list.Add ("runat=\"server\"", "md-literal",
						GettextCatalog.GetString ("Required for ASP.NET controls.\n") +
						GettextCatalog.GetString (
							"Indicates that this tag should be able to be\n" +
							"manipulated programmatically on the web server."));
				
				if (!existingAtts.ContainsKey ("id"))
					list.Add ("id", "md-literal",
						GettextCatalog.GetString ("Unique identifier.\n") +
						GettextCatalog.GetString (
							"An identifier that is unique within the document.\n" + 
							"If the tag is a server control, this will be used \n" +
							"for the corresponding variable name in the CodeBehind."));
				
				existingAtts["ID"] = "";
				if (attributedOb.Name.HasPrefix) {
					AddAspAttributeCompletionData (list, attributedOb.Name, existingAtts);
				}
				
			} else if (attributedOb is AspNetDirective) {
				return DirectiveCompletion.GetAttributes (project, attributedOb.Name.FullName, existingAtts);
			}
			return list.Count > 0? list : null;
		}
		
		protected override CompletionDataList GetAttributeValueCompletions (S.IAttributedXObject ob, S.XAttribute att)
		{
			var list = base.GetAttributeValueCompletions (ob, att) ?? new CompletionDataList ();
			if (ob is S.XElement) {
				if (ob.Name.HasPrefix) {
					S.XAttribute idAtt = ob.Attributes[new S.XName ("id")];
					string id = idAtt == null? null : idAtt.Value;
					if (string.IsNullOrEmpty (id) || string.IsNullOrEmpty (id.Trim ()))
						id = null;
					AddAspAttributeValueCompletionData (list, ob.Name, att.Name, id);
				}
			} else if (ob is AspNetDirective) {
				return DirectiveCompletion.GetAttributeValues (project, Document.FileName, ob.Name.FullName, att.Name.FullName);
			}
			return list.Count > 0? list : null;
		}
		
		ClrVersion ProjClrVersion {
			get { return HasProject? project.TargetFramework.ClrVersion : ClrVersion.Net_2_0; }
		}
		
		CompletionDataList HandleExpressionCompletion (AspNetExpression expr)
		{
			if (!(expr is AspNetDataBindingExpression || expr is AspNetRenderExpression))
				return null;
			IType codeBehindClass;
			ProjectDom projectDatabase;
			GetCodeBehind (out codeBehindClass, out projectDatabase);
			
			if (codeBehindClass == null)
				return null;
			
			//list just the class's properties, not properties on base types
			CompletionDataList list = new CompletionDataList ();
			list.AddRange (from p in codeBehindClass.Properties
				where p.IsProtected || p.IsPublic
				select new CompletionData (p.Name, "md-property"));
			list.AddRange (from p in codeBehindClass.Fields
				where p.IsProtected || p.IsPublic
				select new CompletionData (p.Name, "md-property"));
			
			return list.Count > 0? list : null;
		}
		
		void GetCodeBehind (out IType codeBehindClass, out ProjectDom projectDatabase)
		{
			codeBehindClass = null;
			projectDatabase = null;
			
			if (HasDoc && HasProject && !string.IsNullOrEmpty (aspDoc.Info.InheritedClass)) {
				projectDatabase = ProjectDomService.GetProjectDom (project);
				if (projectDatabase != null)
					codeBehindClass = projectDatabase.GetType (aspDoc.Info.InheritedClass, false, false);
			}
		}
		
		#region ASP.NET data
		
		void AddAspBeginExpressions (CompletionDataList list)
		{
			list.Add ("%",  "md-literal", GettextCatalog.GetString ("ASP.NET render block"));
			list.Add ("%=", "md-literal", GettextCatalog.GetString ("ASP.NET render expression"));
			list.Add ("%@", "md-literal", GettextCatalog.GetString ("ASP.NET directive"));
			list.Add ("%#", "md-literal", GettextCatalog.GetString ("ASP.NET databinding expression"));
			list.Add ("%--", "md-literal", GettextCatalog.GetString ("ASP.NET server-side comment"));
			
			//valid on 2.0+ runtime only
			if (ProjClrVersion != ClrVersion.Net_1_1)
				list.Add ("%$", "md-literal", GettextCatalog.GetString ("ASP.NET resource expression"));
		}
		
		void AddAspAttributeCompletionData (CompletionDataList list, S.XName name, Dictionary<string, string> existingAtts)
		{
			Debug.Assert (name.IsValid);
			Debug.Assert (name.HasPrefix);
			
			//get a parser database
			var database = HasProject? ProjectDomService.GetProjectDom (project) : WebTypeManager.GetSystemWebDom (null);
			
			if (database == null) {
				LoggingService.LogWarning ("Could not obtain project DOM in AddAspAttributeCompletionData");
				return;
			}
			
			IType controlClass = refman.GetControlType (name.Prefix, name.Name);
			if (controlClass == null) {
				controlClass = database.GetType ("System.Web.UI.WebControls.WebControl");
				if (controlClass == null) {
					LoggingService.LogWarning ("Could not obtain IType for System.Web.UI.WebControls.WebControl");
					return;
				}
			}
			
			AddControlMembers (list, database, controlClass, existingAtts);
		}
		
		void AddControlMembers (CompletionDataList list, ProjectDom database, IType controlClass, 
		                        Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (var prop in GetUniqueMembers<IProperty> (GetAllProperties (database, controlClass)))
				if (prop.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (prop.Name)))
					if (GetPersistenceMode (prop) == System.Web.UI.PersistenceMode.Attribute)
						list.Add (prop.Name, prop.StockIcon, prop.Documentation);
			
			//similarly add events
			foreach (var eve in GetUniqueMembers<IEvent> (GetAllEvents (database, controlClass))) {
				string eveName = "On" + eve.Name;
				if (eve.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (eveName)))
					list.Add (eveName, eve.StockIcon, eve.Documentation);
			}
		}
		
		void AddAspAttributeValueCompletionData (CompletionDataList list, S.XName tagName, S.XName attName, string id)
		{
			Debug.Assert (tagName.IsValid && tagName.HasPrefix);
			Debug.Assert (attName.IsValid && !attName.HasPrefix);
			
			IType controlClass = HasDoc? refman.GetControlType (tagName.Prefix, tagName.Name) : null;
			
			if (controlClass == null) {
				LoggingService.LogWarning ("Could not obtain IType for {0}", tagName.FullName);
				
				var database = WebTypeManager.GetSystemWebDom (project);
				controlClass = database.GetType ("System.Web.UI.WebControls.WebControl", true, false);

				if (controlClass == null) {
					LoggingService.LogWarning ("Could not obtain IType for System.Web.UI.WebControls.WebControl");
					return;
				}
			}
			
			//find the codebehind class
			IType codeBehindClass;
			ProjectDom projectDatabase;
			GetCodeBehind (out codeBehindClass, out projectDatabase);
			
			//if it's an event, suggest compatible methods 
			if (codeBehindClass != null && attName.Name.StartsWith ("On")) {
				string eventName = attName.Name.Substring (2);
				
				foreach (IEvent ev in GetAllEvents (projectDatabase, controlClass)) {
					if (ev.Name == eventName) {
						var domMethod = BindingService.MDDomToCodeDomMethod (projectDatabase, ev);
						if (domMethod == null)
							return;
						
						foreach (IMethod meth 
						    in BindingService.GetCompatibleMethodsInClass (projectDatabase, codeBehindClass, ev))
						{
							list.Add (meth.Name, "md-method",
							    GettextCatalog.GetString ("A compatible method in the CodeBehind class"));
						}
						
						string suggestedIdentifier = ev.Name;
						if (id != null) {
							suggestedIdentifier = id + "_" + suggestedIdentifier;
						} else {
							suggestedIdentifier = tagName.Name + "_" + suggestedIdentifier;
						}
							
						domMethod.Name = BindingService.GenerateIdentifierUniqueInClass
							(projectDatabase, codeBehindClass, suggestedIdentifier);
						domMethod.Attributes = (domMethod.Attributes & ~System.CodeDom.MemberAttributes.AccessMask)
							| System.CodeDom.MemberAttributes.Family;
						
						list.Add (
						    new SuggestedHandlerCompletionData (project, domMethod, codeBehindClass,
						        MonoDevelop.DesignerSupport.CodeBehind.GetNonDesignerClass (codeBehindClass))
						    );
						return;
					}
				}
			}
			
			if (projectDatabase == null) {
				projectDatabase = WebTypeManager.GetSystemWebDom (project);
				
				if (projectDatabase == null) {
					LoggingService.LogWarning ("Could not obtain type database in AddAspAttributeCompletionData");
					return;
				}
			}
			
			//if it's a property and is an enum or bool, suggest valid values
			foreach (IProperty prop in GetAllProperties (projectDatabase, controlClass)) {
				if (prop.Name != attName.Name)
					continue;
				
				//boolean completion
				if (prop.ReturnType.FullName == "System.Boolean") {
					AddBooleanCompletionData (list);
					return;
				}
				
				//color completion
				if (prop.ReturnType.FullName == "System.Drawing.Color") {
					System.Drawing.ColorConverter conv = new System.Drawing.ColorConverter ();
					foreach (System.Drawing.Color c in conv.GetStandardValues (null)) {
						if (c.IsSystemColor)
							continue;
						string hexcol = string.Format ("#{0:x2}{1:x2}{2:x2}", c.R, c.G, c.B);
						list.Add (c.Name, hexcol);
					}
					return;
				}
				
				//enum completion
				IType retCls = projectDatabase.GetType (prop.ReturnType);
				if (retCls != null && retCls.ClassType == ClassType.Enum) {
					foreach (IField enumVal in retCls.Fields)
						if (enumVal.IsPublic && enumVal.IsStatic)
							list.Add (enumVal.Name, "md-literal", enumVal.Documentation);
					return;
				}
			}
		}
		
		static IEnumerable<T> GetUniqueMembers<T> (IEnumerable<T> members) where T : IMember
		{
			Dictionary <string, bool> existingItems = new Dictionary<string,bool> ();
			foreach (T item in members) {
				if (existingItems.ContainsKey (item.Name))
					continue;
				existingItems[item.Name] = true;
				yield return item;
			}
		}
		
		static IEnumerable<IProperty> GetAllProperties (
		    ProjectDom projectDatabase,
		    IType cls)
		{
			foreach (IType type in projectDatabase.GetInheritanceTree (cls))
				foreach (IProperty prop in type.Properties)
					yield return prop;
		}
		
		static IEnumerable<IEvent> GetAllEvents (
		    ProjectDom projectDatabase,
		    IType cls)
		{
			foreach (IType type in projectDatabase.GetInheritanceTree (cls))
				foreach (IEvent ev in type.Events)
					yield return ev;
		}
		
		static IEnumerable<IMethod> GetAllMethods (
		    ProjectDom projectDatabase,
		    IType cls)
		{
			foreach (IType type in projectDatabase.GetInheritanceTree (cls))
				foreach (IMethod meth in type.Methods)
					yield return meth;
		}
		
		static void AddBooleanCompletionData (CompletionDataList list)
		{
			list.Add ("true", "md-literal");
			list.Add ("false", "md-literal");
		}
		
		#endregion
		
		#region Querying types' attributes
		
		static System.Web.UI.PersistenceMode GetPersistenceMode (IProperty prop)
		{
			foreach (IAttribute att in prop.Attributes) {
				if (att.Name == "System.Web.UI.PersistenceModeAttribute") {
					System.CodeDom.CodePrimitiveExpression expr = att.PositionalArguments[0] as System.CodeDom.CodePrimitiveExpression;
					if (expr == null) {
						LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", att.PositionalArguments[0]);
						return System.Web.UI.PersistenceMode.Attribute;
					}
					
					return (System.Web.UI.PersistenceMode) expr.Value;
				}
				else if (att.Name == "System.Web.UI.TemplateContainerAttribute")
				{
					return System.Web.UI.PersistenceMode.InnerProperty;
				}
			}
			return System.Web.UI.PersistenceMode.Attribute;
		}
		
		static bool AreChildrenAsProperties (IType type, out string defaultProperty)
		{
			bool childrenAsProperties = false;
			defaultProperty = "";
			
			IAttribute att = GetAttributes (type, "System.Web.UI.ParseChildrenAttribute").FirstOrDefault ();
			if (att == null || att.PositionalArguments.Count == 0)
				return childrenAsProperties;
			
			if (att.PositionalArguments.Count > 0) {
				System.CodeDom.CodePrimitiveExpression expr = att.PositionalArguments[0] as System.CodeDom.CodePrimitiveExpression;
				if (expr == null) {
					LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", att.PositionalArguments[0]);
					return false;
				}
				
				if (expr.Value is bool) {
					childrenAsProperties = (bool) expr.Value;
				} else {
					//TODO: implement this
					LoggingService.LogWarning ("ASP.NET completion does not yet handle ParseChildrenAttribute (Type)");
					return false;
				}
			}
			
			if (att.PositionalArguments.Count > 1) {
				System.CodeDom.CodePrimitiveExpression expr = att.PositionalArguments[1] as System.CodeDom.CodePrimitiveExpression;
				if (expr == null || !(expr.Value is string)) {
					LoggingService.LogWarning ("Unknown expression '{0}' in IAttribute parameter", att.PositionalArguments[1]);
					return false;
				}
				defaultProperty = (string) expr.Value;
			}
			
			if (att.NamedArguments.Count > 0) {
				if (att.NamedArguments.ContainsKey ("ChildrenAsProperties")) {
					System.CodeDom.CodePrimitiveExpression expr = att.NamedArguments["ChildrenAsProperties"]
						as System.CodeDom.CodePrimitiveExpression;
					if (expr == null) {
						LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", att.PositionalArguments[0]);
						return false;
					}
					childrenAsProperties = (bool) expr.Value;
				}
				if (att.NamedArguments.ContainsKey ("DefaultProperty")) {
					System.CodeDom.CodePrimitiveExpression expr = att.NamedArguments["DefaultProperty"]
						as System.CodeDom.CodePrimitiveExpression;
					if (expr == null) {
						LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", att.PositionalArguments[0]);
						return false;
					}
					defaultProperty = (string) expr.Value;
				}
				if (att.NamedArguments.ContainsKey ("ChildControlType")) {
					//TODO: implement this
					LoggingService.LogWarning ("ASP.NET completion does not yet handle ParseChildrenAttribute (Type)");
					return false;
				}
			}
			
			return childrenAsProperties;
		}
		
		static IEnumerable<IAttribute> GetAttributes (IType type, string attName)
		{
			foreach (IAttribute att in type.Attributes) {
				if (att.Name == attName)
					yield return att;
			}
			
			if (type.SourceProjectDom == null) {
				LoggingService.LogWarning ("IType {0} has null SourceProjectDom", type);
				yield break;
			}
			
			foreach (IType t2 in type.SourceProjectDom.GetInheritanceTree (type)) {
				foreach (IAttribute att in t2.Attributes)
					if (att.Name == attName)
						yield return att;
			}
		}
		
		#endregion
		
		#region Document outline
		
		protected override void RefillOutlineStore (ParsedDocument doc, Gtk.TreeStore store)
		{
			ParentNode p = ((AspNetParsedDocument)doc).RootNode;
//			Gtk.TreeIter iter = outlineTreeStore.AppendValues (System.IO.Path.GetFileName (CU.Document.FilePath), p);
			BuildTreeChildren (store, Gtk.TreeIter.Zero, p);
		}
		
		protected override void InitializeOutlineColumns (MonoDevelop.Ide.Gui.Components.PadTreeView outlineTree)
		{
			outlineTree.TextRenderer.Xpad = 0;
			outlineTree.TextRenderer.Ypad = 0;
			outlineTree.AppendColumn ("Node", outlineTree.TextRenderer, new Gtk.TreeCellDataFunc (outlineTreeDataFunc));
		}
		
		protected override void OutlineSelectionChanged (object selection)
		{
			SelectNode ((Node)selection);
		}
		
		static void BuildTreeChildren (Gtk.TreeStore store, Gtk.TreeIter parent, ParentNode p)
		{
			foreach (Node n in p) {
				if ( !(n is TagNode || n is DirectiveNode || n is ExpressionNode))
					continue;
				Gtk.TreeIter childIter;
				if (!parent.Equals (Gtk.TreeIter.Zero))
					childIter = store.AppendValues (parent, n);
				else
					childIter = store.AppendValues (n);
				ParentNode pChild = n as ParentNode;
				if (pChild != null)
					BuildTreeChildren (store, childIter, pChild);
			}
		}
		
		void outlineTreeDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText txtRenderer = (Gtk.CellRendererText) cell;
			Node n = (Node) model.GetValue (iter, 0);
			string name = null;
			if (n is TagNode) {
				TagNode tn = (TagNode) n;
				name = tn.TagName;
				string att = tn.Attributes["id"] as string;
				if (att != null)
					name = "<" + name + "#" + att + ">";
				else
					name = "<" + name + ">";
			} else if (n is DirectiveNode) {
				DirectiveNode dn = (DirectiveNode) n;
				name = "<%@ " + dn.Name + " %>";
			} else if (n is ExpressionNode) {
				ExpressionNode en = (ExpressionNode) n;
				string expr = en.Expression;
				if (string.IsNullOrEmpty (expr)) {
					name = "<% %>";
				} else {
					if (expr.Length > 10)
						expr = expr.Substring (0, 10) + "...";
					name = "<% " + expr + "%>";
				}
			}
			if (name != null)
				txtRenderer.Text = name;
		}
		
		void SelectNode (Node n)
		{
			ILocation start = n.Location, end;
			TagNode tn = n as TagNode;
			if (tn != null && tn.EndLocation != null)
				end = tn.EndLocation;
			else
				end = start;
			
			//FIXME: why is this offset necessary?
			int offset = n is TagNode? 1 : 0;
			EditorSelect (new DomRegion (start.BeginLine, start.BeginColumn + offset, end.EndLine, end.EndColumn + offset));
		}
		#endregion
	}
	
	
		
}
