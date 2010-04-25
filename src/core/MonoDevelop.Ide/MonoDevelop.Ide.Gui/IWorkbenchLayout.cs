//  IWorkbenchLayout.cs
//
// Author:
//   Mike Krüger
//   Lluis Sanchez Gual
//
//  This file was derived from a file from #Develop 2.0
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
//  Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// 
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
// 
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections.Generic;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// The IWorkbenchLayout object is responsible for the layout of 
	/// the workspace, it shows the contents, chooses the IWorkbenchWindow
	/// implementation etc. it could be attached/detached at the runtime
	/// to a workbench.
	/// </summary>
	internal interface IWorkbenchLayout: IMementoCapable
	{
		/// <summary>
		/// The active workbench window.
		/// </summary>

		Gtk.Widget LayoutWidget {
			get;
		}
		
		IWorkbenchWindow ActiveWorkbenchwindow {
			get;
		}

		/// <summary>
		/// The name of the active layout.
		/// </summary>
		string CurrentLayout {
			get;
			set;
		}

		/// <summary>
		/// A list of the currently available layouts for the current workbench context.
		/// </summary>
		string[] Layouts {
			get;
		}
		
		void DeleteLayout (string name);

		/// <summary>
		/// Attaches this layout manager to a workbench object.
		/// </summary>
		void Attach(IWorkbench workbench);
		
		/// <summary>
		/// Detaches this layout manager from the current workspace.
		/// </summary>
		void Detach();
		
		/// <summary>
		/// Shows a new <see cref="IPadContent"/>.
		/// </summary>
		void ShowPad (PadCodon content);
		void AddPad (PadCodon content);
		
		IPadWindow GetPadWindow (PadCodon content);
		
		/// <summary>
		/// Activates a pad (Show only makes it visible but Activate does
		/// bring it to foreground)
		/// </summary>
		void ActivatePad(PadCodon content, bool giveFocus);
		
		/// <summary>
		/// Hides a new <see cref="IPadContent"/>.
		/// </summary>
		void HidePad(PadCodon content);
		
		void RemovePad (PadCodon content);
		
		/// <summary>
		/// returns true, if padContent is visible;
		/// </summary>
		bool IsVisible(PadCodon padContent);
		bool IsContentVisible(PadCodon padContent);
		
		bool IsSticky (PadCodon padContent);

		void SetSticky (PadCodon padContent, bool sticky);
		
		/// <summary>
		/// Re-initializes all components of the layout manager.
		/// </summary>
		void RedrawAllComponents();
		
		/// <summary>
		/// Shows a new <see cref="IViewContent"/>.
		/// </summary>
		IWorkbenchWindow ShowView(IViewContent content);

		void RemoveTab (int pageNum);	

		/// <summary>
		/// Moves to the next tab.
		/// </summary>          
		void NextTab();
		
		/// <summary>
		/// Moves to the previous tab.
		/// </summary>          
		void PreviousTab();
		
		/// <summary>
		/// Is called, when the workbench window which the user has into
		/// the foreground (e.g. editable) changed to a new one.
		/// </summary>
		event EventHandler ActiveWorkbenchWindowChanged;

		/// <summary>
		/// A collection of all valid pads in the layout for the workbench context.
		/// </summary>
		List<PadCodon> PadContentCollection {
			get;
		}
		
		void ActiveMdiChanged(object sender, Gtk.SwitchPageArgs e);
		
		void ResetToolbars ();
		void SetWorkbenchTitle ();
	}
}
