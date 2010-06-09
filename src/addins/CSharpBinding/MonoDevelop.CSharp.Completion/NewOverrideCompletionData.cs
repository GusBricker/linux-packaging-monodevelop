// NewOverrideCompletionData.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;
using System.Collections.Generic;
using MonoDevelop.CSharp.Dom;
using MonoDevelop.Projects.CodeGeneration;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Completion
{
	public class NewOverrideCompletionData : CompletionData
	{
		TextEditorData editor;
		IMember member;
		static Ambience ambience = new CSharpAmbience ();
		string indent;
		int    initialOffset;
		int    declarationBegin;
		int    targetCaretPositon = -1;
		int    selectionEndPositon = -1;
		bool   insertPrivate;
		bool   insertSealed;
		IType  type;
		ICompilationUnit unit;
		IReturnType returnType;
		public bool GenerateBody {
			get;
			set;
		}
		
		public NewOverrideCompletionData (ProjectDom dom, TextEditorData editor, int declarationBegin, IType type, IMember member) : base (null)
		{
			this.editor = editor;
			this.type   = type;
			this.member = member;
			
			this.initialOffset = editor.Caret.Offset;
			this.declarationBegin = declarationBegin;
			this.unit = type.CompilationUnit;
			this.GenerateBody = true;
			string declarationText = editor.Document.GetTextBetween (declarationBegin, initialOffset);
			insertPrivate = declarationText.Contains ("private");
			insertSealed  = declarationText.Contains ("sealed");
			
			this.indent = editor.Document.GetLineIndent (editor.Caret.Line);
			this.Icon = member.StockIcon;
			this.DisplayText = ambience.GetString (member, OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics | OutputFlags.HideExtensionsParameter);
			this.CompletionText = member.Name;
			
			ResolveReturnTypes ();
		}

		void ResolveReturnTypes ()
		{
			returnType = member.ReturnType;
			foreach (IUsing u in unit.Usings) {
				foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
					if (alias.Key == member.ReturnType.FullName) {
						returnType = alias.Value;
						return;
					}
				}
			}
		}
		
		public override void InsertCompletionText (CompletionListWindow window)
		{
			string mod = GetModifiers (member);
			StringBuilder sb = new StringBuilder ();
			if (insertPrivate && String.IsNullOrEmpty (mod)) {
				sb.Append ("private ");
			} else {
				sb.Append (mod);
			}

			if (insertSealed)
				sb.Append ("sealed ");

			if (member.DeclaringType.ClassType != ClassType.Interface && (member.IsVirtual || member.IsAbstract))
				sb.Append ("override ");

			if (member is IMethod) {
				InsertMethod (sb, member as IMethod);
			} else if (member is IProperty) {
				InsertProperty (sb, member as IProperty);
			}
			editor.Replace (declarationBegin, editor.Caret.Offset - declarationBegin, sb.ToString ());
			if (selectionEndPositon >= 0) {
				editor.Caret.Offset = selectionEndPositon;
				editor.SetSelection (targetCaretPositon, selectionEndPositon);
			} else {
				editor.Caret.Offset = targetCaretPositon < 0 ? declarationBegin + sb.Length : targetCaretPositon;
			}
		}
		
		void GenerateMethodBody (StringBuilder sb, IMethod method)
		{
			sb.Append (this.indent);
			sb.Append (SingleIndent);
			if (method.Name == "ToString" && (method.Parameters == null || method.Parameters.Count == 0) && method.ReturnType != null && method.ReturnType.FullName == "System.String") {
				sb.Append ("return string.Format(");
				sb.Append ("\"[");
				sb.Append (type.Name);
				if (type.PropertyCount > 0) 
					sb.Append (": ");
				int i = 0;
				foreach (IProperty property in type.Properties) {
					if (property.IsStatic || !property.IsPublic)
						continue;
					if (i > 0)
						sb.Append (", ");
					sb.Append (property.Name);
					sb.Append ("={");
					sb.Append (i++);
					sb.Append ("}");
				}
				sb.Append ("]\"");
				foreach (IProperty property in type.Properties) {
					if (property.IsStatic || !property.IsPublic)
						continue;
					sb.Append (", ");
					sb.Append (property.Name);
				}
				sb.Append (");");
				sb.AppendLine ();
				return;
			}
			
			if (BaseRefactorer.IsMonoTouchModelMember (method)) {
				sb.Append ("// TODO: Implement - see: http://go-mono.com/docs/index.aspx?link=T%3aMonoTouch.Foundation.ModelAttribute");
			} else if (!method.IsAbstract && method.DeclaringType.ClassType != ClassType.Interface) {
				if (method.ReturnType != null && method.ReturnType.FullName != "System.Void")
					sb.Append ("return ");
				
				sb.Append ("base.");
				sb.Append (method.Name);
				sb.Append (" (");
				if (method.Parameters != null) {
					for (int i = 0; i < method.Parameters.Count; i++) {
						if (i > 0)
							sb.Append (", ");
							
						// add parameter modifier
						if (method.Parameters[i].IsOut) {
							sb.Append ("out ");
						} else if (method.Parameters[i].IsRef) {
							sb.Append ("ref ");
						}
						
						sb.Append (method.Parameters[i].Name);
					}
				}
				sb.Append (");");
			} else {
				targetCaretPositon = declarationBegin + sb.Length;
				sb.Append ("throw new System.NotImplementedException ();");
				selectionEndPositon = declarationBegin + sb.Length;
			} 
			sb.AppendLine ();
		}
		
		bool NamespaceImported (string namespaceName)
		{
			foreach (IUsing u in unit.Usings) {
				if (!u.IsFromNamespace || u.Region.Contains (editor.Caret.Line, editor.Caret.Column)) {
					foreach (string n in u.Namespaces) {
						if (n == namespaceName)
							return true;
					}
				}
			}
			return false;
		}
		
		void InsertMethod (StringBuilder sb, IMethod method)
		{
			if (returnType != null) {
				sb.Append (ambience.GetString (unit.ShortenTypeName (returnType, editor.Caret.Line, editor.Caret.Column), OutputFlags.ClassBrowserEntries | OutputFlags.UseFullName));
				sb.Append (" ");
			}
			sb.Append (method.Name);
			sb.Append (" (");
			OutputFlags flags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName | OutputFlags.IncludeModifiers | OutputFlags.IncludeKeywords;
			for (int i = 0; i < method.Parameters.Count; i++) {
				if (i > 0)
					sb.Append (", ");
				sb.Append (ambience.GetString (method.Parameters[i], NamespaceImported (method.Parameters[i].ReturnType.Namespace) ? flags : flags | OutputFlags.UseFullName));
			}
			sb.Append (")");
			sb.AppendLine ();
			sb.Append (this.indent);
			sb.AppendLine ("{");
			if (GenerateBody) {
				GenerateMethodBody (sb, method);
			} else {
				sb.Append (this.indent);
				sb.Append (SingleIndent);
				targetCaretPositon = declarationBegin + sb.Length;
				sb.AppendLine ();
			}
			sb.Append (this.indent);
			sb.Append ("}");
			sb.AppendLine ();
			sb.Append (indent);
		}
		
		string SingleIndent {
			get {
				if (TextEditorProperties.ConvertTabsToSpaces) 
					return new string (' ', TextEditorProperties.TabIndent);
				return "\t";
			}
		}
			
		void GeneratePropertyBody (StringBuilder sb, IProperty property)
		{
			if (property.HasGet) {
				sb.Append (this.indent);
				sb.Append (SingleIndent);
				sb.AppendLine ("get {");
				sb.Append (this.indent);
				sb.Append (SingleIndent);
				sb.Append (SingleIndent);

				if (BaseRefactorer.IsMonoTouchModelMember (property)) {
					sb.Append ("// TODO: Implement - see: http://go-mono.com/docs/index.aspx?link=T%3aMonoTouch.Foundation.ModelAttribute");
				} else if (!property.IsAbstract && property.DeclaringType.ClassType != ClassType.Interface) {
					sb.Append ("return base.");
					sb.Append (property.Name);
					sb.Append (";");
				} else {
					targetCaretPositon = declarationBegin + sb.Length;
					sb.Append ("throw new System.NotImplementedException ();");
					selectionEndPositon = declarationBegin + sb.Length;
				}
				sb.AppendLine ();
				sb.Append (this.indent);
				sb.Append (SingleIndent);
				sb.AppendLine ("}");
			}
			
			if (property.HasSet) {
				sb.Append (this.indent);
				sb.Append (SingleIndent);
				sb.AppendLine ("set {");
				sb.Append (this.indent);
				sb.Append (SingleIndent);
				sb.Append (SingleIndent);
				if (BaseRefactorer.IsMonoTouchModelMember (property)) {
					sb.Append ("// TODO: Implement - see: http://go-mono.com/docs/index.aspx?link=T%3aMonoTouch.Foundation.ModelAttribute");
				} else if (!property.IsAbstract && property.DeclaringType.ClassType != ClassType.Interface) {
					sb.Append ("base.");
					sb.Append (property.Name);
					sb.AppendLine (" = value;");
				} else {
					sb.AppendLine ("throw new System.NotImplementedException ();");
				}
				sb.Append (this.indent);
				sb.Append (SingleIndent);
				sb.AppendLine ("}");
			}
		}
		void InsertProperty (StringBuilder sb, IProperty property)
		{
			sb.Append (ambience.GetString (unit.ShortenTypeName (returnType, editor.Caret.Line, editor.Caret.Column), OutputFlags.ClassBrowserEntries | OutputFlags.UseFullName));
			sb.Append (" ");
			sb.Append (property.Name);
			sb.AppendLine (" {");
			if (GenerateBody)
				GeneratePropertyBody (sb, property);
			sb.Append (this.indent);
			sb.Append ("}"); 
			sb.AppendLine ();
			sb.Append (indent);
		}
		
		string GetModifiers (IMember member)
		{
			if (member.IsPublic || member.DeclaringType.ClassType == ClassType.Interface) 
				return "public ";
			if (member.IsPrivate) 
				return "";
				
			if (member.IsProtectedAndInternal) 
				return "protected internal ";
			if (member.IsProtectedOrInternal && type.SourceProjectDom == member.DeclaringType.SourceProjectDom) 
				return "internal protected ";
			
			if (member.IsProtected) 
				return "protected ";
			if (member.IsInternal) 
				return "internal ";
				
			return "";
		}
	}
}
