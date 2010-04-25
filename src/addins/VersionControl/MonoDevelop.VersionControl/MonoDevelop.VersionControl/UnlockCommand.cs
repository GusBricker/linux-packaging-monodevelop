// UnlockCommand.cs
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
using MonoDevelop.Core;
using System.IO;

namespace MonoDevelop.VersionControl
{
	
	
	public class UnlockCommand
	{
		public static bool Unlock (VersionControlItemList items, bool test)
		{
			foreach (VersionControlItem it in items)
				if (!it.Repository.CanUnlock (it.Path))
					return false;
			if (test)
				return true;
			
			new UnlockWorker (items).Start();
			return true;
		}

		private class UnlockWorker : Task 
		{
			VersionControlItemList items;
						
			public UnlockWorker (VersionControlItemList items) {
				this.items = items;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Unlocking...");
			}
			
			protected override void Run ()
			{
				IProgressMonitor monitor = GetProgressMonitor ();
				
				foreach (VersionControlItemList list in items.SplitByRepository ())
					list[0].Repository.Unlock (monitor, list.Paths);
				
				Gtk.Application.Invoke (delegate {
					foreach (VersionControlItem item in items)
						VersionControlService.NotifyFileStatusChanged (item.Repository, item.Path, item.IsDirectory);
				});
			}
		}
	}
}
