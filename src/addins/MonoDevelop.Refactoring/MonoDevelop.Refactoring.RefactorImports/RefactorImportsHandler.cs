// 
// ImportsHandler.cs
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
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Refactoring;

namespace MonoDevelop.Refactoring.RefactorImports
{
	public class RemoveUnusedImportsHandler: AbstractRefactoringCommandHandler
	{
		protected override void Run (RefactoringOptions options)
		{
			RemoveUnusedImportsRefactoring removeUnusedImportsRefactoring = new RemoveUnusedImportsRefactoring ();
			if (removeUnusedImportsRefactoring.IsValid (options))
				removeUnusedImportsRefactoring.Run (options);
		}
	}
	
	public class SortImportsHandler: AbstractRefactoringCommandHandler
	{
		protected override void Run (RefactoringOptions options)
		{
			SortImportsRefactoring sortImportsRefactoring = new SortImportsRefactoring ();
			if (sortImportsRefactoring.IsValid (options))
				sortImportsRefactoring.Run (options);
		}
	}
	
	public class RemoveSortImportsHandler: AbstractRefactoringCommandHandler
	{
		protected override void Run (RefactoringOptions options)
		{
			RemoveUnusedImportsRefactoring removeUnusedImportsRefactoring = new RemoveUnusedImportsRefactoring ();
			SortImportsRefactoring sortImportsRefactoring = new SortImportsRefactoring ();
			if (removeUnusedImportsRefactoring.IsValid (options) && sortImportsRefactoring.IsValid (options)) {
				sortImportsRefactoring.Run (options);
				removeUnusedImportsRefactoring.Run (options);
			}
		}
	}
}
