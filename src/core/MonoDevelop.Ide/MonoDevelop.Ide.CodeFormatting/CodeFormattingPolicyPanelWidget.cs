// 
// CodeFormattingPolicyPanel.cs
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;



namespace MonoDevelop.Ide.CodeFormatting
{
	public class TypedCodeFormattingPolicyPanelWidget<T> : CodeFormattingPolicyPanelWidget where T : class, IEquatable<T>, new () 
	{
		T settings;
		CodeFormatDescription description;
		
		Gtk.TreeStore store;
		TreeModel model;
		TreeIter iter;
		CodeFormatOption option;
		
		CellRendererPixbuf pixbufCellRenderer;
		
		public TypedCodeFormattingPolicyPanelWidget ()
		{
			store = new Gtk.TreeStore (typeof (string), typeof (string), typeof (string), typeof (object), typeof (string), typeof(bool), typeof(bool), typeof(bool));
			
			TreeViewColumn column = new TreeViewColumn ();
			
			// pixbuf column
			pixbufCellRenderer = new CellRendererPixbuf ();
			column.PackStart (pixbufCellRenderer, false);
			column.SetCellDataFunc (pixbufCellRenderer, new Gtk.TreeCellDataFunc (RenderIcon));
			
			// text column
			CellRendererText cellRendererText = new CellRendererText ();
			cellRendererText.Ypad = 1;
			column.PackStart (cellRendererText, true);
			column.SetAttributes (cellRendererText, "text", keyColumn);
			
			TreeviewCategories.AppendColumn (column);
			
			column = new TreeViewColumn ();
			CellRendererCombo cellRendererCombo = new CellRendererCombo ();
			cellRendererCombo.Ypad = 1;
			cellRendererCombo.Mode = CellRendererMode.Editable;
			cellRendererCombo.TextColumn = 1;
			cellRendererCombo.Model = comboBoxStore;
			cellRendererCombo.HasEntry = false;
			cellRendererCombo.Editable = true;
			column.PackStart (cellRendererCombo, false);
			column.SetAttributes (cellRendererCombo, "markup", valueDisplayTextColumn, "visible", comboVisibleColumn);
			
			CellRendererToggle cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Ypad = 1;
			cellRendererToggle.Toggled += CellRendererToggleToggled;
			column.PackStart (cellRendererToggle, false);
			column.SetAttributes (cellRendererToggle, "active", toggleColumn, "visible", toggleVisibleColumn);
			
			TreeviewCategories.AppendColumn (column);
			
			cellRendererCombo.EditingStarted += delegate(object o, EditingStartedArgs args) {
				CodeFormatType type = description.GetCodeFormatType (settings, option);
				comboBoxStore.Clear ();
				foreach (KeyValuePair<string, string> v in type.Values) {
					comboBoxStore.AppendValues (v.Key, GettextCatalog.GetString (v.Value));
				}
			};
			
			cellRendererCombo.Edited += delegate(object o, EditedArgs args) {
				CodeFormatType type = description.GetCodeFormatType (settings, option);
				foreach (KeyValuePair<string, string> v in type.Values) {
					if (args.NewText == GettextCatalog.GetString (v.Value)) {
						description.SetValue (settings, option, v.Key);
						TreeIter iter;
						if (store.GetIterFromString (out iter, args.Path)) {
							store.SetValue (iter, valueColumn, v.Key);
							store.SetValue (iter, valueDisplayTextColumn, args.NewText);
						}
						break;
					}
				}
				UpdateExample ();
			};
			
			TreeviewCategories.HeadersVisible = false;
			
			TreeviewCategories.Selection.Changed += TreeSelectionChanged;
			TreeviewCategories.Model = store;
		}

		void CellRendererToggleToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				CodeFormatOption option = (CodeFormatOption) store.GetValue (iter, objectColumn);
				bool value = !(bool) store.GetValue (iter, toggleColumn);
				description.SetValue (settings, option, value ? "True" : "False");
				store.SetValue (iter, toggleColumn, value);
				UpdateExample ();
			}
			
		}
		
		void RenderIcon (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) 
		{
			object o = store.GetValue (iter, objectColumn);
			if (o is CodeFormatCategory) {
				pixbufCellRenderer.Pixbuf = ImageService.GetPixbuf (TreeviewCategories.GetRowExpanded (store.GetPath (iter)) ? MonoDevelop.Core.Gui.Stock.OpenFolder : MonoDevelop.Core.Gui.Stock.ClosedFolder, IconSize.Menu);
			} else {
				pixbufCellRenderer.Pixbuf = ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.Property, IconSize.Menu);
			}
		}
		
		void UpdateExample ()
		{
			IPrettyPrinter printer = TextFileService.GetPrettyPrinter (description.MimeType);
			if (printer == null)
				return;
			DotNetAssemblyProject parent = new DotNetAssemblyProject ();
			parent.Policies.Set<T> (settings, description.MimeType);
			texteditor1.Document.Text  = printer.FormatText (parent, description.MimeType, texteditor1.Document.Text);
		}
		
		protected override void HandleChanged (object sender, EditedArgs e)
		{
		}
		
		public void SetFormat (CodeFormatDescription description, T settings)
		{
			this.description = description;
			this.settings    = settings;
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = false;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			options.Zoom = 0.8;
			texteditor1.Options = options;
			texteditor1.Document.ReadOnly = true;
			texteditor1.Document.MimeType = description.MimeType;
			store.Clear ();
			
			if (description != null) {
				foreach (CodeFormatCategory category in description.SubCategories) {
					AppendCategory (store, TreeIter.Zero, category);
				}
			}
			TreeviewCategories.ShowAll ();
		}
		
		const int keyColumn   = 0;
		const int valueColumn = 1;
		const int valueDisplayTextColumn = 2;
		const int objectColumn = 3;
		const int iconColumn = 4;
		const int toggleColumn = 5;
		const int toggleVisibleColumn = 6;
		const int comboVisibleColumn = 7;

		public new T Settings {
			get {
				return settings;
			}
		}
		
		void AppendCategory (Gtk.TreeStore store, TreeIter iter, CodeFormatCategory category)
		{
			TreeIter categoryIter = iter.Equals (TreeIter.Zero) 
				? store.AppendValues (GettextCatalog.GetString (category.DisplayName), null, null, category)
				: store.AppendValues (iter, GettextCatalog.GetString (category.DisplayName), null, null, category);
			foreach (CodeFormatOption option in category.Options) {
				CodeFormatType type = description.GetCodeFormatType (settings, option);
				KeyValuePair<string, string> val = description.GetValue (settings, option);
				bool isBool = type.Name == "Bool";
				bool boolVal = isBool && val.Value == "True";
				store.AppendValues (categoryIter, 
				                    GettextCatalog.GetString (option.DisplayName), 
				                    val.Key,
				                    GettextCatalog.GetString (val.Value), 
				                    option,
				                    null,
				                    boolVal,
				                    isBool,
				                    !isBool);
			}
			foreach (CodeFormatCategory s in category.SubCategories) {
				AppendCategory (store, categoryIter, s);
			}
		}
		
		/*
		void AddCategoryPage (CodeFormatCategory category)
		{
			Gtk.Label label = new Gtk.Label (GettextCatalog.GetString (category.DisplayName));
			
			foreach (CodeFormatCategory cat in category.SubCategories) {
				AppendCategory (store, TreeIter.Zero, cat);
			}
			Gtk.TreeView tree = new Gtk.TreeView (store);
			tree.AppendColumn (GettextCatalog.GetString ("Key"), new CellRendererText (), "text", keyColumn);
			tree.AppendColumn (GettextCatalog.GetString ("Value"), new CellRendererText (), "text", valueDisplayTextColumn);
			
			ScrolledWindow sw = new ScrolledWindow ();
			sw.Child = tree;
			NotebookCategories.AppendPage (sw, label);
		}*/
		
		void TreeSelectionChanged (object sender, EventArgs e)
		{
			Gtk.TreeSelection treeSelection = (Gtk.TreeSelection)sender;
			if (treeSelection.GetSelected (out model, out iter)) {
				option =  model.GetValue (iter, objectColumn) as CodeFormatOption;
				this.store = model as TreeStore;
				if (option == null) {
					texteditor1.Document.Text = "";
					return;
				}
				CodeFormatType type = description.GetCodeFormatType (settings, option);
				texteditor1.Document.Text = option.Example;
				
				comboBoxStore.Clear ();
				foreach (KeyValuePair<string, string> v in type.Values) {
				 	comboBoxStore.AppendValues (v.Key, GettextCatalog.GetString (v.Value));
				}
				UpdateExample ();
			}
		}
	}
		
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CodeFormattingPolicyPanelWidget : Gtk.Bin
	{
		protected ListStore comboBoxStore;
		protected Mono.TextEditor.TextEditor texteditor1 = new Mono.TextEditor.TextEditor ();
		protected Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions ();
		
		protected Gtk.TreeView TreeviewCategories {
			get {
				return treeviewCategories;
			}
		}
		
		public CodeFormattingPolicyPanelWidget ()
		{
			this.Build();
			checkbuttonWhiteSpaces.Toggled += CheckbuttonWhiteSpacesToggled;
			comboBoxStore = new ListStore (typeof (string), typeof (string));
		/*	comboboxValue.Clear ();
			Gtk.CellRendererText ctx = new Gtk.CellRendererText ();
			comboboxValue.PackStart (ctx, true);
			comboboxValue.AddAttribute (ctx, "text", 1);
			
			
			comboboxValue.Model = comboBoxStore;
			
			comboboxValue.Changed += HandleChanged;*/
			scrolledwindow2.Child = texteditor1;
			ShowAll ();
		}
		
		protected virtual void HandleChanged (object sender, EditedArgs e)
		{
		}
		
		void CheckbuttonWhiteSpacesToggled (object sender, EventArgs e)
		{
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = checkbuttonWhiteSpaces.Active;
			this.texteditor1.QueueDraw ();
		}
		
		
	}
}
