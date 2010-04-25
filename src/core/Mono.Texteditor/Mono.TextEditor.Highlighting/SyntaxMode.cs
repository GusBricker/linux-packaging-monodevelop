// SyntaxMode.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace Mono.TextEditor.Highlighting
{
	public class SyntaxMode : Rule
	{
		public static readonly SyntaxMode Default = new SyntaxMode ();
		protected List<Rule> rules = new List<Rule> ();
		
		public string MimeType {
			get;
			private set;
		}
		
		public IEnumerable<Rule> Rules {
			get {
				return rules;
			}
		}
		
		public SyntaxMode() : base (null)
		{
			DefaultColor = "text";
			Name = "<root>";
			this.Delimiter = "&()<>{}[]~!%^*-+=|\\#/:;\"' ,\t.?";
		}
		
		public bool Validate (Style style)
		{
			if (!GetIsValid (style)) {
				return false;
			}
			foreach (Rule rule in Rules) {
				if (!rule.GetIsValid (style)) {
					return false;
				}
			}
			return true;
		}
		
		public virtual Chunk GetChunks (Document doc, Style style, LineSegment line, int offset, int length)
		{
			SpanParser spanParser = CreateSpanParser (doc, this, line, null);
			ChunkParser chunkParser = CreateChunkParser (spanParser, doc, style, this, line);
			Chunk result = chunkParser.GetChunks (offset, length);
			if (SemanticRules != null) {
				foreach (SemanticRule sematicRule in SemanticRules) {
					sematicRule.Analyze (doc, line, result, offset, offset + length);
				}
			}
			return result;
		}
		
		public virtual string GetTextWithoutMarkup (Document doc, Style style, int offset, int length)
		{
			return doc.GetTextAt (offset, length);
		}
		
		public string GetMarkup (Document doc, ITextEditorOptions options, Style style, int offset, int length, bool removeIndent)
		{
			return GetMarkup (doc, options, style, offset, length, removeIndent, true, true);
		}
		
		public static string ColorToPangoMarkup (Gdk.Color color)
		{
			return string.Format ("#{0:X2}{1:X2}{2:X2}", color.Red >> 8, color.Green >> 8, color.Blue >> 8);
		}
		
		public string GetMarkup (Document doc, ITextEditorOptions options, Style style, int offset, int length, bool removeIndent, bool useColors, bool replaceTabs)
		{
			int curOffset = offset;
			int indentLength = int.MaxValue;
			while (curOffset < offset + length) {
				LineSegment line = doc.GetLineByOffset (curOffset);
				indentLength = System.Math.Min (indentLength, line.GetIndentation (doc).Length);
				curOffset = line.EndOffset + 1;
			}
			curOffset = offset;
			
			StringBuilder result = new StringBuilder ();
			while (curOffset < offset + length && curOffset < doc.Length) {
				LineSegment line = doc.GetLineByOffset (curOffset);
				int toOffset = System.Math.Min (line.Offset + line.EditableLength, offset + length);
				Stack<ChunkStyle> styleStack = new Stack<ChunkStyle> ();
				for (Chunk chunk = GetChunks (doc, style, line, curOffset, toOffset - curOffset); chunk != null; chunk = chunk.Next) {
					
					ChunkStyle chunkStyle = chunk.GetChunkStyle (style);
					bool setBold = chunkStyle.Bold && (styleStack.Count == 0 || !styleStack.Peek ().Bold) ||
						!chunkStyle.Bold && (styleStack.Count == 0 || styleStack.Peek ().Bold);
					bool setItalic = chunkStyle.Italic && (styleStack.Count == 0 || !styleStack.Peek ().Italic) ||
						!chunkStyle.Italic && (styleStack.Count == 0 || styleStack.Peek ().Italic);
					bool setUnderline = chunkStyle.Underline && (styleStack.Count == 0 || !styleStack.Peek ().Underline) || 
						!chunkStyle.Underline && (styleStack.Count == 0 || styleStack.Peek ().Underline);
					bool setColor = styleStack.Count == 0 || TextViewMargin.GetPixel (styleStack.Peek ().Color) != TextViewMargin.GetPixel (chunkStyle.Color);
					if (setColor || setBold || setItalic || setUnderline) {
						if (styleStack.Count > 0) {
							result.Append("</span>");
							styleStack.Pop ();
						}
						result.Append("<span");
						if (useColors) {
							result.Append(" foreground=\"");
							result.Append(ColorToPangoMarkup (chunkStyle.Color));
							result.Append("\"");
						}
						if (chunkStyle.Bold)
							result.Append(" weight=\"bold\"");
						if (chunkStyle.Italic)
							result.Append(" style=\"italic\"");
						if (chunkStyle.Underline)
							result.Append(" underline=\"single\"");
						result.Append(">");
						styleStack.Push (chunkStyle);
					}
					
					for (int i = 0; i < chunk.Length; i++) {
						char ch = chunk.GetCharAt (doc, chunk.Offset + i);
						switch (ch) {
						case '&':
							result.Append ("&amp;");
							break;
						case '<':
							result.Append ("&lt;");
							break;
						case '>':
							result.Append ("&gt;");
							break;
						case '\t':
							if (replaceTabs) {
								result.Append (new string (' ', options.TabSize));
							} else {
								result.Append ('\t');
							}
							break;
						default:
							result.Append (ch);
							break;
						}
					}
				}
				while (styleStack.Count > 0) {
					result.Append("</span>");
					styleStack.Pop ();
				}
				
				curOffset = line.EndOffset;
				if (removeIndent)
					curOffset += indentLength;
				if (result.Length > 0 && curOffset < offset + length)
					result.AppendLine ();
			}
			return result.ToString ();
		}
		
		public virtual SpanParser CreateSpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack)
		{
			return new SpanParser (doc, mode, line, spanStack);
		}
		
		public virtual ChunkParser CreateChunkParser (SpanParser spanParser, Document doc, Style style, SyntaxMode mode, LineSegment line)
		{
			return new ChunkParser (spanParser, doc, style, mode, line);
		}
		
		public class SpanParser
		{
			protected SyntaxMode mode;
			protected Stack<Span> spanStack;
			protected Stack<Rule> ruleStack;

			protected Document doc;
			protected LineSegment line;
			
			int maxEnd;
			
			public Rule CurRule {
				get {
					if (ruleStack.Count == 0)
						return mode;
					return ruleStack.Peek ();
				}
			}
			
			public Span CurSpan {
				get {
					if (spanStack.Count == 0)
						return null;
					return spanStack.Peek ();
				}
			}

			public Stack<Span> SpanStack {
				get {
					return spanStack;
				}
			}

			public Stack<Rule> RuleStack {
				get {
					return ruleStack;
				}
			}
			
			public SpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack)
			{
				if (doc == null)
					throw new ArgumentNullException ("doc");
				this.doc  = doc;
				this.mode = mode;
				this.line = line;
				this.spanStack = spanStack ?? new Stack<Span> (line != null && line.StartSpan != null ? line.StartSpan : new Span[0]);
				//this.ruleStack = ruleStack ?? new Stack<Span> (line.StartRule != null ? line.StartRule : new Rule[0]);
				
				ruleStack = new Stack<Rule> ();
				if (mode != null) 
					ruleStack.Push (mode);
				List<Rule> rules = new List<Rule> ();
				foreach (Span span in this.spanStack) {
					Rule rule = CurRule.GetRule (span.Rule);
					rules.Add (rule ?? CurRule);
				}
				for (int i = rules.Count - 1; i >= 0 ; i--) {
					ruleStack.Push (rules[i]);
				}
			}
			
			public Rule GetRule (Span span)
			{
				if (string.IsNullOrEmpty (span.Rule))
					return new Rule (mode);
				return CurRule.GetRule (span.Rule);
			}
			
			public delegate void SpanDelegate (Span span, int offset, int length);
			public delegate void CharParser (ref int i, char ch);
			
			public event SpanDelegate FoundSpanBegin;
			public virtual void OnFoundSpanBegin (Span span, int offset, int length)
			{
				if (FoundSpanBegin != null)
					FoundSpanBegin (span, offset, length);
			}
			
			public event SpanDelegate FoundSpanExit;
			public virtual void OnFoundSpanExit (Span span, int offset, int length)
			{
				if (FoundSpanExit != null)
					FoundSpanExit (span, offset, length);
			}
			
			public event SpanDelegate FoundSpanEnd;
			public virtual void OnFoundSpanEnd (Span span, int offset, int length)
			{
				if (FoundSpanEnd != null)
					FoundSpanEnd (span, offset, length);
			}
			
			public event CharParser ParseChar;
			public void OnParseChar (ref int i, char ch)
			{
				if (ParseChar != null)
					ParseChar (ref i, ch);
			}
			
			protected virtual void ScanSpan (ref int i)
			{
				for (int j = 0; j < CurRule.Spans.Length; j++) {
					Span span = CurRule.Spans[j];
					
					if (span.BeginFlags.Contains ("startsLine") && line != null && i != line.Offset)
						continue;
					
					RegexMatch match = span.Begin.TryMatch (doc, i);
					if (match.Success) {
						bool mismatch = false;
						if (span.BeginFlags.Contains ("firstNonWs") && line != null) {
							for (int k = line.Offset; k < i; k++) {
								if (!Char.IsWhiteSpace (doc.GetCharAt (k))) {
									mismatch = true;
									break;
								}
							}
						}
						if (mismatch)
							continue;
						OnFoundSpanBegin (span, i, match.Length);
						i += match.Length - 1;
						spanStack.Push (span);
						ruleStack.Push (GetRule (span));
						return;
					}
				}
			}
			
			protected virtual bool ScanSpanEnd (Span cur, int i)
			{
				if (cur.End != null) {
					RegexMatch match = cur.End.TryMatch (doc, i);
					if (match.Success) {
						OnFoundSpanEnd (cur, i, match.Length);
						spanStack.Pop ();
						if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
							ruleStack.Pop ();
						return true;
					}
				}
				
				if (cur.Exit != null) {
					RegexMatch match = cur.Exit.TryMatch (doc, i);
					if (match.Success) {
						spanStack.Pop ();
						if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
							ruleStack.Pop ();
						OnFoundSpanExit (cur, i, match.Length);
						return true;
					}
				}
				return false;
			}
			
			public void ParseSpans (int offset, int length)
			{
				maxEnd = System.Math.Min (doc.Length, System.Math.Min (offset + length, line != null ? System.Math.Min (line.Offset + line.EditableLength, doc.Length) : doc.Length));
				for (int i = offset; i < maxEnd; i++) {
					Span cur = CurSpan;
					if (cur != null) {
						if (cur.Escape != null) {
							bool mismatch = false;
							for (int j = 0; j < cur.Escape.Length; j++) {
								if (i + j >= doc.Length || doc.GetCharAt (i + j) != cur.Escape[j]) {
									mismatch = true;
									break;
								}
							}
							if (!mismatch) {
								i += cur.Escape.Length;
								if (cur.Escape.Length > 1)
									i--;
								continue;
							}
						}
						if (ScanSpanEnd (cur, i))
							continue;
					}
					
					ScanSpan (ref i);
					
					if (i < doc.Length)
						OnParseChar (ref i, doc.GetCharAt (i));
				}
			}
		}
		
		public class ChunkParser
		{
			readonly string defaultStyle = "text";
			SpanParser spanParser;
			Document doc;
			LineSegment line;
			SyntaxMode mode;
			
			public ChunkParser (SpanParser spanParser, Document doc, Style style, SyntaxMode mode, LineSegment line)
			{
				this.mode = mode;
				this.doc = doc;
				this.line = line;
				this.spanParser = spanParser;
				spanParser.FoundSpanBegin += FoundSpanBegin;
				spanParser.FoundSpanEnd += FoundSpanEnd;
				spanParser.FoundSpanExit += FoundSpanExit;
				spanParser.ParseChar += ParseChar;
				if (line == null)
					throw new ArgumentNullException ("line");
			}
			
			Chunk startChunk = null;
			Chunk endChunk;
			
			void AddRealChunk (Chunk chunk)
			{
				if (startChunk == null) {
					startChunk = endChunk = chunk;
					return;
				}
				if (endChunk.Style.Equals (chunk.Style)) {
					endChunk.Length += chunk.Length;
					return;
				}
				endChunk = endChunk.Next = chunk;
				/*
				const int MaxChunkLength = 80;
				int divisor = chunk.Length / MaxChunkLength;
				int reminder = chunk.Length % MaxChunkLength;
				for (int i = 0; i < divisor; i++) {
					Chunk newChunk = new Chunk (chunk.Offset + i * MaxChunkLength, MaxChunkLength, chunk.Style);
					if (startChunk == null)
						startChunk = endChunk = newChunk;
					else 
						endChunk = endChunk.Next = newChunk;
				}
				if (reminder > 0) {
					Chunk newChunk = new Chunk (chunk.Offset + divisor * MaxChunkLength, reminder, chunk.Style);
					if (startChunk == null)
						startChunk = endChunk = newChunk;
					else 
						endChunk = endChunk.Next = newChunk;
				}*/
			}
			
			void AddChunk (ref Chunk curChunk, int length, string style)
			{
				if (curChunk.Length > 0) {
					AddRealChunk (curChunk);
					curChunk = new Chunk (curChunk.EndOffset, 0, defaultStyle);
				}
				if (length > 0) {
					curChunk.Style  = style;
					curChunk.Length = length;
					AddRealChunk (curChunk);
				}
				curChunk = new Chunk (curChunk.EndOffset, 0, defaultStyle);
				curChunk.Style = GetSpanStyle ();
			}
			
			string GetChunkStyleColor (string topColor)
			{
				if (!String.IsNullOrEmpty (topColor)) 
					return topColor;
				if (String.IsNullOrEmpty (spanParser.SpanStack.Peek ().Color)) {
					Span span = spanParser.SpanStack.Pop ();
					string result = GetChunkStyleColor (topColor);
					spanParser.SpanStack.Push (span);
					return result;
				}
				return spanParser.SpanStack.Count > 0 ? spanParser.SpanStack.Peek ().Color : defaultStyle;
			}
			
			string GetSpanStyle ()
			{
				if (spanParser.SpanStack.Count == 0)
					return defaultStyle;
				if (String.IsNullOrEmpty (spanParser.SpanStack.Peek ().Color)) {
					Span span = spanParser.SpanStack.Pop ();
					string result = GetSpanStyle ();
					spanParser.SpanStack.Push (span);
					return result;
				}
				string rule = spanParser.SpanStack.Peek ().Rule;
				if (!string.IsNullOrEmpty (rule) && rule.StartsWith ("mode:"))
					return defaultStyle;
				return spanParser.SpanStack.Peek ().Color;
			}
			
			Chunk curChunk;
			
			string GetChunkStyle (Span span) 
			{
				if (span == null)
					return GetSpanStyle ();
				return span.TagColor ?? span.Color ?? GetSpanStyle ();
			}
			
			public void FoundSpanBegin (Span span, int offset, int length)
			{
				curChunk.Length = offset - curChunk.Offset;
				curChunk.Style  = GetStyle (curChunk) ?? GetSpanStyle ();
				AddChunk (ref curChunk, 0, curChunk.Style);
				
				curChunk.Offset = offset;
				curChunk.Length = length;
				curChunk.Style  = GetChunkStyle (span);
				AddChunk (ref curChunk, 0, curChunk.Style);
				foreach (SemanticRule semanticRule in spanParser.GetRule (span).SemanticRules) {
					semanticRule.Analyze (this.doc, line, curChunk, offset, line.EndOffset);
				}
			}
			
			public void FoundSpanExit (Span span, int offset, int length)
			{
				curChunk.Length = offset - curChunk.Offset;
				AddChunk (ref curChunk, 0, GetChunkStyle (span));
			}
			
			public void FoundSpanEnd (Span span, int offset, int length)
			{
				curChunk.Length = offset - curChunk.Offset;
				curChunk.Style  = GetStyle (curChunk) ?? GetChunkStyle (span);
				AddChunk (ref curChunk, 0, defaultStyle);
				
				curChunk.Offset = offset;
				curChunk.Length = length;
				curChunk.Style  = GetChunkStyle (span);
				AddChunk (ref curChunk, 0, defaultStyle);
			}
			
			bool inWord = false;
			public void ParseChar (ref int i, char ch)
			{
				int textOffset = i - line.Offset;
				if (textOffset >= str.Length)
					return;
				
				Rule cur = spanParser.CurRule;
				bool isWordPart = cur.Delimiter.IndexOf (ch) < 0;
				
				if (inWord && !isWordPart || !inWord && isWordPart) 
					AddChunk (ref curChunk, 0, curChunk.Style = GetStyle (curChunk) ?? GetSpanStyle ());
				
				inWord = isWordPart;
				
				if (cur.HasMatches && i - curChunk.Offset == 0) {
					Match foundMatch = null;
					int   foundMatchLength = 0;
					foreach (Match ruleMatch in cur.Matches) {
						int matchLength = ruleMatch.TryMatch (str, textOffset);
						if (foundMatchLength < matchLength) {
							foundMatch = ruleMatch;
							foundMatchLength = matchLength;
						}
					}
					if (foundMatch != null) {
						AddChunk (ref curChunk, foundMatchLength, GetChunkStyleColor (foundMatch.Color));
						i += foundMatchLength - 1;
						curChunk.Length = i - curChunk.Offset + 1; 
						return;
					}
				}
				
				curChunk.Length = i - curChunk.Offset + 1; 
			}
			
			string GetStyle (Chunk chunk)
			{
				if (chunk.Length > 0) {
/*					Console.WriteLine (spanParser.CurSpan  + " / " + spanParser.CurRule);
					Console.WriteLine ("----:Span:");
					foreach (var o in spanParser.SpanStack) {
						Console.WriteLine (o);
					}
					Console.WriteLine ("----:Rule:");
					foreach (var o in spanParser.RuleStack) {
						Console.WriteLine (o);
					}
					Console.WriteLine ("----");*/
					Keywords keyword = spanParser.CurRule.GetKeyword (doc, chunk.Offset, chunk.Length); 
					if (keyword != null)
						return keyword.Color;
				}
				return null;
			}
			
			string str;
			public virtual Chunk GetChunks (int offset, int length)
			{
				SyntaxModeService.ScanSpans (doc, mode, spanParser.CurRule, spanParser.SpanStack, line.Offset, offset);
				length = System.Math.Min (doc.Length - offset, length);
				str = length > 0 ? doc.GetTextAt (offset, length) : null;
				curChunk = new Chunk (offset, 0, GetSpanStyle ());
				spanParser.ParseSpans (offset, length);
				curChunk.Length = offset + length - curChunk.Offset;
				if (curChunk.Length > 0) {
					curChunk.Style = GetStyle (curChunk) ?? GetSpanStyle ();
					AddRealChunk (curChunk);
				}
				return startChunk;
			}
		}
		
		public override Rule GetRule (string name)
		{
			if (name == null || name == "<root>") {
				return this;
			}
			if (name.StartsWith ("mode:"))
				return SyntaxModeService.GetSyntaxMode (name.Substring ("mode:".Length));
			
			foreach (Rule rule in rules) {
				if (rule.Name == name)
					return rule;
			}
			return this;
		}
		
		void AddSemanticRule (Rule rule, SemanticRule semanticRule)
		{
			if (rule != null)
				rule.SemanticRules.Add (semanticRule);
		}
		
		public void AddSemanticRule (SemanticRule semanticRule)
		{
			AddSemanticRule (this, semanticRule);
		}
		
		public void AddSemanticRule (string addToRuleName, SemanticRule semanticRule)
		{
			AddSemanticRule (GetRule (addToRuleName), semanticRule);
		}
		
		void RemoveSemanticRule (Rule rule, Type type)
		{
			if (rule != null) {
				for (int i = 0; i < rule.SemanticRules.Count; i++) {
					if (rule.SemanticRules[i].GetType () == type) {
						rule.SemanticRules.RemoveAt (i);
						i--;
					}
				}
			}
		}
		public void RemoveSemanticRule (Type type)
		{
			RemoveSemanticRule (this, type);
		}
		public void RemoveSemanticRule (string removeFromRuleName, Type type)
		{
			RemoveSemanticRule (GetRule (removeFromRuleName), type);
		}
		
		public override string ToString ()
		{
			return String.Format ("[SyntaxMode: Name={0}, MimeType={1}]", Name, MimeType);
		}
				
		new const string Node = "SyntaxMode"; 
		
		public const string MimeTypesAttribute = "mimeTypes";
		
		public static SyntaxMode Read (XmlReader reader)
		{
			SyntaxMode result = new SyntaxMode ();
			List<Match> matches = new List<Match> ();
			List<Span> spanList = new List<Span> ();
			List<Marker> prevMarkerList = new List<Marker> ();
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case Node:
					string extends = reader.GetAttribute ("extends");
					if (!String.IsNullOrEmpty (extends)) {
						result = SyntaxModeService.GetSyntaxMode (extends);
					}
					result.Name       = reader.GetAttribute ("name");
					result.MimeType   = reader.GetAttribute (MimeTypesAttribute);
					if (!String.IsNullOrEmpty (reader.GetAttribute ("ignorecase")))
						result.IgnoreCase = Boolean.Parse (reader.GetAttribute ("ignorecase"));
					return true;
				case Rule.Node:
					result.rules.Add (Rule.Read (result, reader, result.IgnoreCase));
					return true;
				}
				return result.ReadNode (reader, matches, spanList, prevMarkerList);
			});
			result.spans   = spanList.ToArray ();
			result.prevMarker = prevMarkerList.ToArray ();
			result.matches = matches.ToArray ();
			return result;
		}
	}
}
