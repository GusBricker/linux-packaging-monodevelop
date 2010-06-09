// 
// AspDocumentBuilder.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.AspNet.Gui;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharp.Completion
{
	public class AspLanguageBuilder : Visitor, ILanguageCompletionBuilder
	{
		StringBuilder document = new StringBuilder ();
		StringBuilder pageRenderer = new StringBuilder ();
		TextEditorData data;
		
		public bool SupportsLanguage (string language)
		{
			return language == "C#";
		}
		
		ParsedDocument Parse (string fileName, string text)
		{
			return new MonoDevelop.CSharp.Parser.NRefactoryParser ().Parse (null, fileName, text);
		}
		
		static string GetDefaultBaseClass (MonoDevelop.AspNet.WebSubtype type)
		{
			switch (type) {
			case MonoDevelop.AspNet.WebSubtype.WebForm:
				return "System.Web.UI.Page";
			case MonoDevelop.AspNet.WebSubtype.MasterPage:
				return "System.Web.UI.MasterPage";
			case MonoDevelop.AspNet.WebSubtype.WebControl:
				return "System.Web.UI.UserControl";
			}
			throw new InvalidOperationException (string.Format ("Unexpected filetype '{0}'", type));
		}
		
		static void WriteUsings (MonoDevelop.AspNet.Parser.AspNetParsedDocument doc, StringBuilder builder)
		{
			foreach (var u in doc.Info.Imports) {
				builder.Append ("using ");
				builder.Append (u);
				builder.AppendLine (";");
			}
		}
		
		static void WriteClassDeclaration (MonoDevelop.AspNet.Parser.AspNetParsedDocument doc, StringBuilder builder)
		{
			string typeName = string.IsNullOrEmpty (doc.Info.ClassName)? "Generated" : doc.Info.ClassName;
			
			string baseClass = string.IsNullOrEmpty (doc.Info.InheritedClass)?
				GetDefaultBaseClass (doc.Type) : doc.Info.InheritedClass;
			
			builder.Append ("partial class ");
			builder.Append (typeName);
			builder.Append (" : ");
			builder.AppendLine (baseClass);
		}
		
		public LocalDocumentInfo BuildLocalDocument (DocumentInfo documentInfo, TextEditorData data, string expressionText, bool isExpression)
		{
			this.data = data;
			var result = new StringBuilder ();
			
			WriteUsings (documentInfo.AspNetParsedDocument, result);
			WriteClassDeclaration (documentInfo.AspNetParsedDocument, result);
			result.AppendLine ("{");
		
			if (isExpression) {
				result.AppendLine ("void Generated ()");
				result.AppendLine ("{");
				//Console.WriteLine ("start:" + location.BeginLine  +"/" +location.BeginColumn);
				foreach (ExpressionNode expression in documentInfo.Expressions) {
					if (expression.Location.BeginLine > data.Caret.Line || expression.Location.BeginLine == data.Caret.Line && expression.Location.BeginColumn > data.Caret.Column - 5) 
						continue;
					//Console.WriteLine ("take xprt:" + expressions.Key.BeginLine  +"/" +expressions.Key.BeginColumn);
					if (expression.IsExpression)
						result.Append ("WriteLine (");
					result.Append (expression.Expression.Trim ('='));
					if (expression.IsExpression)
						result.Append (");");
				}
			}
			result.Append (expressionText);
			int caretPosition = result.Length;
			result.AppendLine ();
			result.AppendLine ("}");
			result.AppendLine ("}");
			return new LocalDocumentInfo () {
				LocalDocument = result.ToString (),
				CaretPosition = caretPosition,
				ParsedLocalDocument = Parse (documentInfo.AspNetParsedDocument.FileName, result.ToString ())
			};
		}
		
		
		public ICompletionDataList HandleCompletion (MonoDevelop.Ide.Gui.Document document, LocalDocumentInfo info, ProjectDom currentDom, char currentChar, ref int triggerWordLength)
		{
			CodeCompletionContext codeCompletionContext;
			using (CSharpTextEditorCompletion completion = CreateCompletion (document, info, currentDom, out codeCompletionContext)) {
				return completion.HandleCodeCompletion (codeCompletionContext, currentChar, ref triggerWordLength);
			}
		}
		
		public IParameterDataProvider HandleParameterCompletion (MonoDevelop.Ide.Gui.Document document, LocalDocumentInfo info, ProjectDom currentDom, char completionChar)
		{
			CodeCompletionContext codeCompletionContext;
			using (CSharpTextEditorCompletion completion = CreateCompletion (document, info, currentDom, out codeCompletionContext)) {
				return completion.HandleParameterCompletion (codeCompletionContext, completionChar);
			}
		}

		CSharpTextEditorCompletion CreateCompletion (MonoDevelop.Ide.Gui.Document document, LocalDocumentInfo info, ProjectDom currentDom, out CodeCompletionContext codeCompletionContext)
		{
			codeCompletionContext = new CodeCompletionContext ();
			
			codeCompletionContext.TriggerOffset = info.CaretPosition;
			
			Mono.TextEditor.Document doc = new Mono.TextEditor.Document ();
			doc.Text = info.LocalDocument;
			DocumentLocation documentLocation = doc.OffsetToLocation (info.CaretPosition);
			
			codeCompletionContext.TriggerLine       = documentLocation.Line + 1;
			codeCompletionContext.TriggerLineOffset = documentLocation.Column + 1;
			
			CSharpTextEditorCompletion completion = new CSharpTextEditorCompletion (document);
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new System.IO.StringReader (info.LocalDocument))) {
				parser.Parse ();
				completion.ParsedUnit = parser.CompilationUnit;
			}
			completion.Dom = currentDom;
			return completion;
		}
		
		DocumentInfo documentInfo;
		public DocumentInfo BuildDocument (MonoDevelop.AspNet.Parser.AspNetParsedDocument aspDocument, TextEditorData data)
		{
			documentInfo = new DocumentInfo ();
			this.data = data;
			
			WriteUsings (aspDocument, document);
			WriteClassDeclaration (aspDocument, document);
			
			aspDocument.RootNode.AcceptVisit (this);
			
			documentInfo.AspNetParsedDocument = aspDocument;
			documentInfo.ParsedDocument = Parse (aspDocument.FileName, document.ToString ());
			return documentInfo;
		}
		
		void Finish ()
		{
			document.Append ("void Render () {");
			document.Append (pageRenderer.ToString ());
			document.Append ("}");
			
			document.Append ("}");
			/*
			CursorPosition = buildCursorPosition;
			if (buildCursorIsExpression)
			    CursorPosition += expressionStart;
			
			int curLine = 1;
			int curColumn = 1;
			
			string text = document.ToString ();
			
			for (int i = 0; i < text.Length; i++) {
				switch (text[i]) {
				case '\n':
					if (i + 1 < text.Length && text[i + 1] == '\r')
						i++;
					goto case '\r';
				case '\r':
					curLine++;
					curColumn = 1;
					break;
				default:
					curColumn++;
					break;
				}
				if (i == CursorPosition) {
					CursorLine = curLine;
					CursorColumn = curColumn;
					break;
				}
			}*/
		}
		
		public override void Visit (TagNode node)
		{
			if (node.TagName == "script") {
				int start = data.Document.LocationToOffset (node.Location.EndLine - 1,  node.Location.EndColumn);
				int end = data.Document.LocationToOffset (node.EndLocation.BeginLine - 1,  node.EndLocation.BeginColumn);
				document.AppendLine (data.Document.GetTextBetween (start, end));
			}
		}
		
		public override void Visit (ExpressionNode node)
		{
			string text = node.Expression;
			documentInfo.Expressions.Add (node);
			pageRenderer.AppendLine (text);
		}
	}
}
