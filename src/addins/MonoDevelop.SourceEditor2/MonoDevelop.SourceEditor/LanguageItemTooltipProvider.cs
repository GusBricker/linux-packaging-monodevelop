// LanguageItemTooltipProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
//

using System;
using Mono.TextEditor;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.SourceEditor
{
	public class LanguageItemTooltipProvider: ITooltipProvider
	{
		public LanguageItemTooltipProvider()
		{
		}

		#region ITooltipProvider implementation 
		
		public TooltipItem GetItem (Mono.TextEditor.TextEditor editor, int offset)
		{
			ExtensibleTextEditor ed = (ExtensibleTextEditor) editor;
			
			ResolveResult resolveResult = ed.GetLanguageItem (offset);
			if (resolveResult == null || resolveResult.ResolvedExpression == null)
				return null;
			int startOffset = editor.Document.LocationToOffset (resolveResult.ResolvedExpression.Region.Start.Line,
			                                                    resolveResult.ResolvedExpression.Region.Start.Column);
			int endOffset = editor.Document.LocationToOffset (resolveResult.ResolvedExpression.Region.End.Line, 
			                                                    resolveResult.ResolvedExpression.Region.End.Column);
			return new TooltipItem (resolveResult, startOffset, endOffset - startOffset);
		}
		
		ResolveResult lastResult = null;
		LanguageItemWindow lastWindow = null;
		
		public Gtk.Window CreateTooltipWindow (Mono.TextEditor.TextEditor editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			ExtensibleTextEditor ed = (ExtensibleTextEditor) editor;
			ParsedDocument doc = ProjectDomService.GetParsedDocument (null, ed.Document.FileName);
			
			ResolveResult resolveResult = (ResolveResult)item.Item;
			if (lastResult != null && lastResult.ResolvedExpression != null && lastWindow.IsRealized && 
			    resolveResult != null && resolveResult.ResolvedExpression != null &&  lastResult.ResolvedExpression.Expression == resolveResult.ResolvedExpression.Expression)
				return lastWindow;
			LanguageItemWindow result = new LanguageItemWindow (ed, modifierState, resolveResult, null, doc != null ? doc.CompilationUnit : null);
			lastWindow = result;
			lastResult = resolveResult;
			if (result.IsEmpty)
				return null;
			return result;
		}
		
		public void GetRequiredPosition (Mono.TextEditor.TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			LanguageItemWindow win = (LanguageItemWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}
		
		public bool IsInteractive (Mono.TextEditor.TextEditor editor, Gtk.Window tipWindow)
		{
			return false;
		}
		
		#endregion 
		
	}
}
