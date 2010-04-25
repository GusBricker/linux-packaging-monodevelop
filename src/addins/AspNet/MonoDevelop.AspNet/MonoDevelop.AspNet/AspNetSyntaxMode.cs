// 
// AspNetSyntaxMode.cs
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
using System.Linq;
using Mono.TextEditor.Highlighting;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Xml;
using System.Text;

namespace MonoDevelop.AspNet
{
	public class AspNetSyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode
	{
		SyntaxMode charpMode;
		
		public AspNetSyntaxMode ()
		{
			ResourceXmlProvider provider = new ResourceXmlProvider (typeof(IXmlProvider).Assembly, "AspNetSyntaxMode.xml");
			using (XmlReader reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				this.rules = new List<Rule> (baseMode.Rules);
				this.keywords = new List<Keywords> (baseMode.Keywords);
				this.spans = baseMode.Spans;
				this.matches = baseMode.Matches;
				this.prevMarker = baseMode.PrevMarker;
				this.SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				this.table = baseMode.Table;
			}
		}
		
		public override SpanParser CreateSpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack)
		{
			return new ASPNetSpanParser (doc, mode, line, spanStack);
		}

		protected class ASPNetSpanParser : SpanParser
		{
			public ASPNetSpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack) : base (doc, mode, line, spanStack)
			{}
			
			class CSharpSpan : Span
			{
				public CSharpSpan ()
				{
					Rule = "mode:text/x-csharp";
					Begin = new Regex ("<%");
					End = new Regex ("<%");
					Color = "";
				}
			}
			class CodeDeclarationSpan : Span
			{
				public CodeDeclarationSpan (string language)
				{
					Rule = "mode:" + language;
					Begin = new Regex ("<%");
					End = new Regex ("<%");
					Color = "";
				}
			}
			
			protected override void ScanSpan (ref int i)
			{
				if (i + 3 < doc.Length && doc.GetTextAt (i, 2) == "<%" && doc.GetCharAt (i + 2) != '@') {
					AspNetSyntaxMode.ASPNetSpanParser.CSharpSpan span = new CSharpSpan ();
					spanStack.Push (span);
					ruleStack.Push (GetRule (span));
					OnFoundSpanBegin (span, i, 0);
					return;
				}


				
				base.ScanSpan (ref i);
			}
			
			static string GetMimeForLanguage (string language)
			{
				switch (language) {
				case "C#":
					return "text/x-csharp";
				case "VB":
					return "text/x-vb";
				case "JScript.NET":
					return "application/javascript";
				}
				return null;
			}
			
			protected override bool ScanSpanEnd (Mono.TextEditor.Highlighting.Span cur, int i)
			{
				if (doc.GetCharAt (i) == '>') {
					int j = i;
					while (j > 0 && doc.GetCharAt (j) != '<')
						j--;

					if (j + 7 < doc.Length && doc.GetTextAt (j, 7) == "<script") {
						StringBuilder langBuilder = new StringBuilder ();
						j += 7;
						while (j < doc.Length && doc.GetCharAt (j) != '>') {
							if (j + 8 < doc.Length && doc.GetTextAt (j, 8) == "language") {
								j += 8;
								bool inString = false;
								while (j < doc.Length) {
									char ch = doc.GetCharAt (j);
									if (ch == '"' || ch == '\'') {
										if (inString)
											break;
										inString = true;
										j++;
										continue;
									}
									if (inString)
										langBuilder.Append (ch);
									j++;
								}
								break;
							}
							j++;
						}
						string mime = GetMimeForLanguage (langBuilder.ToString ());
						if (mime != null) {
							bool result = base.ScanSpanEnd (cur, i);
							CodeDeclarationSpan span = new CodeDeclarationSpan (mime);
							spanStack.Push (span);
							ruleStack.Push (GetRule (span));
							OnFoundSpanBegin (span, i + 1, 0);
							return result;
						}
					}
				}
				
				if (spanStack.Any (s => s is CodeDeclarationSpan) && i + 9 < doc.Length && doc.GetTextAt (i, 9) == "</script>") {
					while (!(spanStack.Peek () is CodeDeclarationSpan)) {
						OnFoundSpanEnd (spanStack.Peek (), i, 0);
						spanStack.Pop ();
						if (ruleStack.Count > 1)
							ruleStack.Pop ();
					}
					cur = spanStack.Peek ();
					OnFoundSpanEnd (cur, i, 0);
					spanStack.Pop ();
					if (ruleStack.Count > 1)
						ruleStack.Pop ();
				}
				
				if (spanStack.Any (s => s is CSharpSpan) && i + 2 < doc.Length && doc.GetTextAt (i, 2) == "%>") {
					while (!(spanStack.Peek () is CSharpSpan)) {
						OnFoundSpanEnd (spanStack.Peek (), i, 0);
						spanStack.Pop ();
						if (ruleStack.Count > 1)
							ruleStack.Pop ();
					}
					cur = spanStack.Peek ();
					OnFoundSpanEnd (cur, i, 0);
					spanStack.Pop ();
					if (ruleStack.Count > 1)
						ruleStack.Pop ();
					return true;
				}
				return base.ScanSpanEnd (cur, i);
			}
		}
	}
}
