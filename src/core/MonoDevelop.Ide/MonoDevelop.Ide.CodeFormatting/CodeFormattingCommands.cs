// 
// CodeFormattingCommands.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;


namespace MonoDevelop.Ide.CodeFormatting
{
	public enum CodeFormattingCommands {
		FormatBuffer
	}
	
	public class FormatBufferHandler : CommandHandler
	{
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsFile) {
				string mt = DesktopService.GetMimeTypeForUri (IdeApp.Workbench.ActiveDocument.FileName);
				IPrettyPrinter printer = TextFileService.GetPrettyPrinter (mt);
				if (printer != null)
					return;
			}
			info.Enabled = false;
		}
		
		protected override void Run (object tool)
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			string mt = DesktopService.GetMimeTypeForUri (doc.FileName);
			IPrettyPrinter printer = TextFileService.GetPrettyPrinter (mt);
			if (printer == null)
				return;
			doc.TextEditor.BeginAtomicUndo ();
			int line = doc.TextEditor.CursorLine;
			int column = doc.TextEditor.CursorColumn;
			doc.TextEditor.Select (0, doc.TextEditor.TextLength);
			doc.TextEditor.SelectedText = printer.FormatText (doc.Project, mt, doc.TextEditor.Text);
			doc.TextEditor.CursorLine = line ;
			doc.TextEditor.CursorColumn = column;
			doc.TextEditor.EndAtomicUndo ();
		}
	}
}
