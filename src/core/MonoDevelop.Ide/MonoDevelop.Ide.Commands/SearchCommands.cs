// 
// SearchCommands.cs
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

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
{
	public enum SearchCommands
	{
		Find,
		FindNext,
		FindPrevious,
		EmacsFindNext,
		EmacsFindPrevious,
		Replace,
		FindInFiles,
		FindNextSelection,
		FindPreviousSelection,
		FindBox,
		ReplaceInFiles,
		
		GotoType,
		GotoFile,
		GotoLineNumber,
		
		ToggleBookmark,
		PrevBookmark,
		NextBookmark,
		ClearBookmarks,
	}
	
	class GotoTypeHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.TextEditor != null)
				GoToDialog.Run (false, IdeApp.Workbench.ActiveDocument.TextEditor.SelectedText);
			else
				GoToDialog.Run (false);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workspace.IsOpen || IdeApp.Workbench.Documents.Count != 0;
		}
	}
	
	class GotoFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			GoToDialog.Run (true);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workspace.IsOpen || IdeApp.Workbench.Documents.Count != 0;
		}
	}
}
