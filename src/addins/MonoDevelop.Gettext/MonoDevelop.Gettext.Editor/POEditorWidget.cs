//
// POEditorWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

using Gtk;
using Gdk;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Gettext.Editor;
using Mono.TextEditor;

namespace MonoDevelop.Gettext
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class POEditorWidget : Gtk.Bin, IUndoHandler
	{
		TranslationProject project;
		CatalogHeadersWidget headersEditor;
		ListStore store;
		ListStore foundInStore;
		Catalog catalog;
		string  poFileName;
		Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions ();
		Mono.TextEditor.TextEditor texteditorOriginal = new Mono.TextEditor.TextEditor ();
		Mono.TextEditor.TextEditor texteditorPlural = new Mono.TextEditor.TextEditor ();
		
		static List<POEditorWidget> widgets = new List<POEditorWidget> (); 
		
		public Catalog Catalog {
			get {
				return catalog;
			}
			set {
				catalog = value;
				headersEditor.CatalogHeaders = catalog;
				ClearTextview ();
				AddTextview (0);
				this.GetTextView (0).Document.TextReplaced += delegate {
					Gtk.TreeIter iter = SelectedIter;
					if (treeviewEntries.Selection.IterIsSelected (iter)) {
						store.SetValue (iter, (int)Columns.Stock, GetStockForEntry (SelectedEntry));
						store.SetValue (iter, (int)Columns.Translation, StringEscaping.ToGettextFormat (this.SelectedEntry.GetTranslation (0)));
						store.SetValue (iter, (int)Columns.RowColor, GetRowColorForEntry (SelectedEntry));
						store.SetValue (iter, (int)Columns.TypeSortIndicator, GetTypeSortIndicator (SelectedEntry));
					}
				};
				UpdateFromCatalog ();
				UpdateProgressBar ();
				UpdateTasks ();
			}
		}
		
		public string POFileName { // todo - move to Catalog class.
			get {
				return poFileName;
			}
			set {
				poFileName = value;
			}
		}
		
		internal static readonly Gdk.Color errorColor = new Gdk.Color (210, 32, 32);
		public POEditorWidget (TranslationProject project)
		{
			this.project = project;
			this.Build();
			this.headersEditor = new CatalogHeadersWidget ();
			this.notebookPages.AppendPage (headersEditor, new Gtk.Label ());
			
			AddButton (GettextCatalog.GetString ("Translation")).Active = true;
			AddButton (GettextCatalog.GetString ("Headers")).Active = false;
			
			// entries tree view 
			store = new ListStore (typeof (string), typeof (bool), typeof (string), typeof (string), typeof (CatalogEntry), typeof (Gdk.Color), typeof(int), typeof (Gdk.Color));
			this.treeviewEntries.Model = store;
			
			treeviewEntries.AppendColumn (String.Empty, new CellRendererPixbuf (), "stock_id", Columns.Stock, "cell-background-gdk", Columns.RowColor);
			
			CellRendererToggle cellRendFuzzy = new CellRendererToggle ();
			cellRendFuzzy.Toggled += new ToggledHandler (FuzzyToggled);
			cellRendFuzzy.Activatable = true;
			treeviewEntries.AppendColumn (GettextCatalog.GetString ("Fuzzy"), cellRendFuzzy, "active", Columns.Fuzzy, "cell-background-gdk", Columns.RowColor);
			 
			CellRendererText original = new CellRendererText ();
			original.Ellipsize = Pango.EllipsizeMode.End;
			treeviewEntries.AppendColumn (GettextCatalog.GetString ("Original string"), original, "text", Columns.String, "cell-background-gdk", Columns.RowColor, "foreground-gdk", Columns.ForeColor);
			
			CellRendererText translation = new CellRendererText ();
			translation.Ellipsize = Pango.EllipsizeMode.End;
			treeviewEntries.AppendColumn (GettextCatalog.GetString ("Translated string"), translation, "text", Columns.Translation, "cell-background-gdk", Columns.RowColor, "foreground-gdk", Columns.ForeColor);
			treeviewEntries.Selection.Changed += new EventHandler (OnEntrySelected);
			
			treeviewEntries.GetColumn (0).SortIndicator = true;
			treeviewEntries.GetColumn (0).SortColumnId = (int)Columns.TypeSortIndicator;
			
			treeviewEntries.GetColumn (1).SortIndicator = true;
			treeviewEntries.GetColumn (1).SortColumnId = (int)Columns.Fuzzy;
			
			treeviewEntries.GetColumn (2).SortIndicator = true;
			treeviewEntries.GetColumn (2).SortColumnId = (int)Columns.String;
			treeviewEntries.GetColumn (2).Resizable = true;
			treeviewEntries.GetColumn (2).Expand = true;
			
			treeviewEntries.GetColumn (3).SortIndicator = true;
			treeviewEntries.GetColumn (3).SortColumnId = (int)Columns.Translation;
			treeviewEntries.GetColumn (3).Resizable = true;
			treeviewEntries.GetColumn (3).Expand = true;
			// found in tree view
			foundInStore = new ListStore (typeof (string), typeof (string), typeof (string), typeof (Pixbuf));
			this.treeviewFoundIn.Model = foundInStore;
			
			TreeViewColumn fileColumn = new TreeViewColumn ();
			CellRendererPixbuf pixbufRenderer = new CellRendererPixbuf ();
			fileColumn.PackStart (pixbufRenderer, false);
			fileColumn.SetAttributes (pixbufRenderer, "pixbuf", FoundInColumns.Pixbuf);
			
			CellRendererText textRenderer = new CellRendererText ();
			fileColumn.PackStart (textRenderer, true);
			fileColumn.SetAttributes (textRenderer, "text", FoundInColumns.File);
			treeviewFoundIn.AppendColumn (fileColumn);
			
			treeviewFoundIn.AppendColumn ("", new CellRendererText (), "text", FoundInColumns.Line);
			treeviewFoundIn.HeadersVisible = false;
			treeviewFoundIn.GetColumn (1).FixedWidth = 100;
			
			treeviewFoundIn.RowActivated += delegate (object sender, RowActivatedArgs e) {
				Gtk.TreeIter iter;
				foundInStore.GetIter (out iter, e.Path);
				string line = foundInStore.GetValue (iter, (int)FoundInColumns.Line) as string;
				string file = foundInStore.GetValue (iter, (int)FoundInColumns.FullFileName) as string;
				int lineNr = 1;
				try {
					lineNr = 1 + int.Parse (line);
				} catch {}
				MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (file, lineNr, 1, true);
			};
			this.notebookTranslated.RemovePage (0);
			this.entryFilter.Text = "";
			entryFilter.Changed += delegate {
				UpdateFromCatalog ();
			};
			
			this.togglebuttonFuzzy.Active = PropertyService.Get ("Gettext.ShowFuzzy", true);
			this.togglebuttonFuzzy.TooltipText = GettextCatalog.GetString ("Show fuzzy translations");
			this.togglebuttonFuzzy.Toggled += delegate {
				MonoDevelop.Core.PropertyService.Set ("Gettext.ShowFuzzy", this.togglebuttonFuzzy.Active);
				UpdateFromCatalog ();
			};
			
			this.togglebuttonMissing.Active = PropertyService.Get ("Gettext.ShowMissing", true);
			this.togglebuttonMissing.TooltipText = GettextCatalog.GetString ("Show missing translations");
			this.togglebuttonMissing.Toggled += delegate {
				MonoDevelop.Core.PropertyService.Set ("Gettext.ShowMissing", this.togglebuttonMissing.Active);
				UpdateFromCatalog ();
			};
			
			this.togglebuttonOk.Active = PropertyService.Get ("Gettext.ShowTranslated", true);
			this.togglebuttonOk.TooltipText = GettextCatalog.GetString ("Show valid translations");
			this.togglebuttonOk.Toggled += delegate {
				MonoDevelop.Core.PropertyService.Set ("Gettext.ShowTranslated", this.togglebuttonOk.Active);
				UpdateFromCatalog ();
			};
			
			this.textviewComments.Buffer.Changed += delegate {
				if (this.isUpdating)
					return;
				if (this.currentEntry != null) {
					string[] lines = StringEscaping.FromGettextFormat (textviewComments.Buffer.Text).Split (new string[] { System.Environment.NewLine }, System.StringSplitOptions.None);
					for (int i = 0; i < lines.Length; i++) {
						if (!lines[i].StartsWith ("#"))
							lines[i] = "# " + lines[i];
					}
					this.currentEntry.Comment = string.Join (System.Environment.NewLine, lines);
				}
				UpdateProgressBar ();
			};
			this.treeviewEntries.PopupMenu += delegate {
				ShowPopup ();
			};
			
			this.treeviewEntries.ButtonReleaseEvent += delegate (object sender, Gtk.ButtonReleaseEventArgs e) {
				if (e.Event.Button == 3)
					ShowPopup ();
			};
			this.buttonOptions.Label = GettextCatalog.GetString ("Options");
			this.buttonOptions.StockImage = Gtk.Stock.Properties;
			this.buttonOptions.MenuCreator = CreateOptionsMenu;
			widgets.Add (this);
			UpdateTasks ();
//			this.vpaned2.AcceptPosition += delegate {
//				PropertyService.Set ("Gettext.SplitPosition", vpaned2.Position / (double)Allocation.Height);
//				inMove = false;
//			};
//			this.vpaned2.CancelPosition += delegate {
//				inMove = false;
//			};
//			this.vpaned2.MoveHandle += delegate {
//				inMove = true;
//			};
//			this.ResizeChecked += delegate {
//				if (inMove)
//					return;
//				int newPosition = (int)(Allocation.Height * PropertyService.Get ("Gettext.SplitPosition", 0.3d));
//				if (vpaned2.Position != newPosition)
//					vpaned2.Position = newPosition;
//			};
			checkbuttonWhiteSpaces.Toggled += CheckbuttonWhiteSpacesToggled;
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = false;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			
			this.scrolledwindowOriginal.Child = this.texteditorOriginal;
			this.scrolledwindowPlural.Child = this.texteditorPlural;
			this.texteditorOriginal.Show ();
			this.texteditorPlural.Show ();
			texteditorOriginal.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Insensitive));
			texteditorPlural.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Insensitive));
			this.texteditorOriginal.Options = options;
			this.texteditorPlural.Options = options;
			this.texteditorOriginal.Document.ReadOnly = true;
			this.texteditorPlural.Document.ReadOnly = true;
		}
		
		void CheckbuttonWhiteSpacesToggled (object sender, EventArgs e)
		{
			options.ShowSpaces = options.ShowTabs = checkbuttonWhiteSpaces.Active;
			texteditorOriginal.QueueDraw ();
			texteditorPlural.QueueDraw ();
			for (int i = this.notebookTranslated.NPages - 1; i >= 0; i--) {
				Mono.TextEditor.TextEditor view = GetTextView (i);
				if (view != null)
					view.QueueDraw ();
			}
		}
		
		#region Options
		enum SearchIn {
			Original,
			Translated,
			Both
		}
		
		static bool isCaseSensitive;
		static bool isWholeWordOnly;
		static bool regexSearch;
		static SearchIn searchIn;
		
		static POEditorWidget ()
		{
			isCaseSensitive = PropertyService.Get ("GettetAddin.Search.IsCaseSensitive", false);
			isWholeWordOnly = PropertyService.Get ("GettetAddin.Search.IsWholeWordOnly", false);
			regexSearch     = PropertyService.Get ("GettetAddin.Search.RegexSearch", false);
			searchIn        = PropertyService.Get ("GettetAddin.Search.SearchIn", SearchIn.Both);
		}
		
		static bool IsCaseSensitive {
			get {
				return isCaseSensitive;
			}
			set {
				PropertyService.Set ("GettetAddin.Search.IsCaseSensitive", value);
				isCaseSensitive = value;
			}
		}
		
		static bool IsWholeWordOnly {
			get {
				return isWholeWordOnly;
			}
			set {
				PropertyService.Set ("GettetAddin.Search.IsWholeWordOnly", value);
				isWholeWordOnly = value;
			}
		}
		
		static bool RegexSearch {
			get {
				return regexSearch;
			}
			set {
				PropertyService.Set ("GettetAddin.Search.RegexSearch", value);
				regexSearch = value;
			}
		}
		
		static SearchIn DoSearchIn {
			get {
				return searchIn;
			}
			set {
				PropertyService.Set ("GettetAddin.Search.SearchIn", value);
				searchIn = value;
			}
		}
		#endregion
		
		public Menu CreateOptionsMenu (MenuButton button)
		{
			Menu menu = new Menu ();
			
			MenuItem searchInMenu = new MenuItem (GettextCatalog.GetString ("_Search in"));
			Menu sub = new Menu ();
			searchInMenu.Submenu = sub;
			Gtk.RadioMenuItem  original = null, translated = null, both = null;
			GLib.SList group = new GLib.SList (IntPtr.Zero);
			original = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Original"));
			group = original.Group;
			original.ButtonPressEvent += delegate { original.Activate (); };
			sub.Append (original);
			
			translated = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Translated"));
			translated.ButtonPressEvent += delegate { translated.Activate (); };
			group = translated.Group;
			sub.Append (translated);
			
			both = new Gtk.RadioMenuItem (group, GettextCatalog.GetString ("_Both"));
			both.ButtonPressEvent += delegate { both.Activate (); };
			sub.Append (both);
			switch (DoSearchIn) {
			case SearchIn.Both:
				both.Activate ();
				break;
			case SearchIn.Original:
				original.Activate ();
				break;
			case SearchIn.Translated:
				translated.Activate ();
				break;
			}
			menu.Append (searchInMenu);
			both.Activated += delegate {
				if (DoSearchIn != SearchIn.Both) {
					DoSearchIn = SearchIn.Both;
					UpdateFromCatalog ();
					menu.Dispose ();
				}
			};
			original.Activated += delegate {
				if (DoSearchIn != SearchIn.Original) {
					DoSearchIn = SearchIn.Original;
					UpdateFromCatalog ();
					menu.Dispose ();
				}
			};
			translated.Activated += delegate {
				if (DoSearchIn != SearchIn.Translated) {
					DoSearchIn = SearchIn.Translated;
					UpdateFromCatalog ();
					menu.Dispose ();
				}
			};
			
			Gtk.CheckMenuItem regexSearch = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Regex search"));
			regexSearch.Active = RegexSearch;
			regexSearch.ButtonPressEvent += delegate { 
				RegexSearch = !RegexSearch;
				UpdateFromCatalog ();
			};
			menu.Append (regexSearch);
			
			Gtk.CheckMenuItem caseSensitive = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Case sensitive"));
			caseSensitive.Active = IsCaseSensitive;
			caseSensitive.ButtonPressEvent += delegate { 
				IsCaseSensitive = !IsCaseSensitive;
				UpdateFromCatalog ();
			};
			menu.Append (caseSensitive);
			
			Gtk.CheckMenuItem wholeWordsOnly = new Gtk.CheckMenuItem (MonoDevelop.Core.GettextCatalog.GetString ("_Whole words only"));
			wholeWordsOnly.Active = IsWholeWordOnly;
			wholeWordsOnly.Sensitive = !RegexSearch;
			wholeWordsOnly.ButtonPressEvent += delegate {
				IsWholeWordOnly = !IsWholeWordOnly;
				UpdateFromCatalog ();
			};
			menu.Append (wholeWordsOnly);
			menu.ShowAll ();
			return menu;
		}
		
		public static void ReloadWidgets ()
		{
			foreach (POEditorWidget widget in widgets) {
				widget.Reload ();
			}
		}
		
//		bool inMove = false;
//		protected override void OnSizeAllocated (Rectangle rect)
//		{
//			base.OnSizeAllocated (rect);
//		}
//		
		void Reload ()
		{
			Catalog newCatalog = new Catalog(project);
			newCatalog.Load (null, catalog.FileName);
			this.Catalog = newCatalog;
		}
		
		Mono.TextEditor.TextEditor GetTextView (int index)
		{
			ScrolledWindow window = this.notebookTranslated.GetNthPage (index) as ScrolledWindow;
			if (window != null)
				return window.Child as Mono.TextEditor.TextEditor;
			return null;
		}
		
		void ClearTextview ()
		{
			while (this.notebookTranslated.NPages > 0)
				this.notebookTranslated.RemovePage (0);
		}
		
		void AddTextview (int index)
		{
			ScrolledWindow window = new ScrolledWindow ();
			Mono.TextEditor.TextEditor textView = new Mono.TextEditor.TextEditor ();
			window.Child = textView;
			textView.Options = options;
			textView.Document.TextReplaced += delegate {
				if (this.isUpdating)
					return;
				try {
					if (this.currentEntry != null) {
						string escapedText = StringEscaping.FromGettextFormat (textView.Document.Text);
						string oldText     = this.currentEntry.GetTranslation (index);
						this.currentEntry.SetTranslation (escapedText, index);
						AddChange (this.currentEntry, oldText, escapedText, index);
					}
					MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.ShowReady ();
					textView.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
				} catch (System.Exception e) {
					MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.ShowError (e.Message);
					textView.ModifyBase (Gtk.StateType.Normal, errorColor);
				}
				UpdateProgressBar ();
				UpdateTasks ();
			};
			
			Label label = new Label ();
			label.Text = this.Catalog.PluralFormsDescriptions [index];
			window.ShowAll ();
			this.notebookTranslated.AppendPage (window, label);
		}
		
		void ShowPopup ()
		{
			Gtk.Menu contextMenu = CreateContextMenu ();
			if (contextMenu != null)
				contextMenu.Popup ();
		}
		
		Gtk.Menu CreateContextMenu ()
		{
			CatalogEntry entry = SelectedEntry;
			if (entry == null)
				return null;

			Gtk.Menu result = new Gtk.Menu ();
			
			Gtk.MenuItem item = new Gtk.MenuItem ("Delete");
			item.Sensitive = entry.References.Length == 0;
			item.Activated += delegate {
				RemoveEntry (entry);
			};
			item.Show();
			result.Append (item);
			
			return result;
		}
		
		void RemoveEntryByString (string msgstr)
		{
			CatalogEntry entry = this.catalog.FindItem (msgstr);
			if (entry != null) { 
				if (currentEntry.String == msgstr) 
					this.EditEntry (null);
				this.catalog.RemoveItem (entry);
				this.UpdateFromCatalog ();
			}
		}
		
		void RemoveEntry (CatalogEntry entry)
		{
			bool yes = MonoDevelop.Core.Gui.MessageService.AskQuestion (GettextCatalog.GetString ("Do you really want to remove the translation string {0} (It will be removed from all translations)?", entry.String),
			                                                            AlertButton.Cancel, AlertButton.Remove) == AlertButton.Remove;

			if (yes) {
				TranslationProject project = IdeApp.ProjectOperations.CurrentSelectedSolutionItem as TranslationProject;
				if (project != null) {
					foreach (POEditorWidget widget in widgets)
						widget.RemoveEntryByString (entry.String);
					project.RemoveEntry (entry.String);
				}
			}
		}
		
		void UpdateProgressBar ()
		{
			int all, untrans, fuzzy, missing, bad;
			catalog.GetStatistics (out all, out fuzzy, out missing, out bad, out untrans);
			double percentage = all > 0 ? ((double)(all - untrans) / all) * 100 : 0.0;
			string barText = String.Format (GettextCatalog.GetString ("{0:#00.00}% Translated"), percentage);
			if (untrans > 0 || fuzzy > 0)
				barText += " (";

			if (untrans > 0) {
				barText += String.Format (GettextCatalog.GetPluralString ("{0} Missing Message", "{0} Missing Messages", untrans), untrans);
			}

			if (fuzzy > 0) {
				if (untrans > 0) {
					barText += ", ";
				}
				barText += String.Format (GettextCatalog.GetPluralString ("{0} Fuzzy Message", "{0} Fuzzy Messages", fuzzy), fuzzy);
			}

			if (untrans > 0 || fuzzy > 0)
				barText += ")";
			
			this.progressbar1.Text = barText;
			percentage = percentage / 100;
			this.progressbar1.Fraction = percentage;
		}		
		
		#region EntryEditor handling
		CatalogEntry currentEntry;
//		Dictionary<Mono.TextEditor.TextEditor, bool> gtkSpellSet = new Dictionary<Mono.TextEditor.TextEditor, bool> (); 
		void RemoveTextViewsFrom (int index)
		{
			for (int i = this.notebookTranslated.NPages - 1; i >= index; i--) {
				Mono.TextEditor.TextEditor view = GetTextView (i);
				if (view == null)
					continue;
//				if (gtkSpellSet.ContainsKey (view)) {
//					GtkSpell.Detach (view);
//					gtkSpellSet.Remove (view);
//				}
				this.notebookTranslated.RemovePage (i);
			}
		}
		
		void EditEntry (CatalogEntry entry)
		{
			this.isUpdating = true;
			try {
				currentEntry = entry;
				this.texteditorOriginal.Caret.Offset = 0;
				this.texteditorOriginal.Document.Text = entry != null ? StringEscaping.ToGettextFormat (entry.String) : "";
				this.texteditorOriginal.VAdjustment.Value = this.texteditorOriginal.HAdjustment.Value = 0;
				
//				if (GtkSpell.IsSupported && !gtkSpellSet.ContainsKey (this.textviewOriginal)) {
//					GtkSpell.Attach (this.textviewOriginal, "en");
//					this.gtkSpellSet[this.textviewOriginal] = true;
//				}
//				
				this.vbox8.Visible = entry != null && entry.HasPlural;
				this.notebookTranslated.ShowTabs = entry != null && entry.HasPlural;
				
				if (entry != null && entry.HasPlural) {
					this.texteditorPlural.Caret.Offset = 0;
					this.texteditorPlural.Document.Text = StringEscaping.ToGettextFormat (entry.PluralString);
					this.texteditorPlural.VAdjustment.Value = this.texteditorPlural.HAdjustment.Value = 0;
//					if (GtkSpell.IsSupported && !gtkSpellSet.ContainsKey (this.textviewOriginalPlural)) {
//						GtkSpell.Attach (this.textviewOriginalPlural, "en");
//						this.gtkSpellSet[this.textviewOriginalPlural] = true;
//					}
				}
				
				this.foundInStore.Clear ();
				
				if (entry != null) { 
					RemoveTextViewsFrom (entry.NumberOfTranslations);
					
					for (int i = this.notebookTranslated.NPages; i < entry.NumberOfTranslations; i++) {
						AddTextview (i);
					}
					
					for (int i = 0; i < entry.NumberOfTranslations; i++) {
						Mono.TextEditor.TextEditor textView = GetTextView (i);
						if (textView == null)
							continue;
						textView.ClearSelection ();
						textView.Document.Text = entry != null ? StringEscaping.ToGettextFormat (entry.GetTranslation (i)) : "";
						textView.Caret.Offset = textView.Document.Text.Length;
						textView.VAdjustment.Value = textView.HAdjustment.Value = 0;
						textView.Document.CommitUpdateAll ();
/*						if (GtkSpell.IsSupported && !gtkSpellSet.ContainsKey (textView)) {
							GtkSpell.Attach (textView, "en");
							this.gtkSpellSet[textView] = true;
						}*/
					}
					
					foreach (string reference in entry.References) {
						string file;
						string line;
						int i = reference.IndexOf (':');
						if (i >= 0) {
							file = reference.Substring (0, i);
							line = reference.Substring (i + 1);
						} else {
							file = reference;
							line = "?";
						}
						string fullName = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (this.poFileName), file);
						this.foundInStore.AppendValues (file, line, fullName, DesktopService.GetPixbufForFile (fullName, IconSize.Menu));
					}
				}
				
				this.textviewComments.Buffer.Text = entry != null ? StringEscaping.ToGettextFormat (entry.Comment) : null;
				
/*				if (GtkSpell.IsSupported) {
					foreach (TextView view in this.gtkSpellSet.Keys)
						GtkSpell.Recheck (view);
				}*/
			} finally {
				this.isUpdating = false;
			}
		}
		#endregion
		
#region TreeView handling
		enum Columns : int
		{
			Stock,
			Fuzzy,
			String,
			Translation,
			CatalogEntry,
			RowColor,
			TypeSortIndicator,
			ForeColor
		}
		
		enum FoundInColumns : int
		{
			File,
			Line,
			FullFileName,
			Pixbuf
		}
		
		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			base.OnStyleSet (previous_style);
			UpdateColors ();
		}
		
		void UpdateColors ()
		{
			TreeIter iter;
			if (store == null || !store.GetIterFirst (out iter))
				return;
			do {
				CatalogEntry entry = (CatalogEntry)store.GetValue (iter, (int)Columns.CatalogEntry);
				store.SetValue (iter, (int)Columns.RowColor, GetRowColorForEntry (entry));
				store.SetValue (iter, (int)Columns.ForeColor, GetForeColorForEntry (entry));
				
			} while (store.IterNext (ref iter));
		}
		
		void FuzzyToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				bool val = (bool)store.GetValue (iter, (int)Columns.Fuzzy);
				CatalogEntry entry = (CatalogEntry)store.GetValue (iter, (int)Columns.CatalogEntry);
				entry.IsFuzzy = !val;
				store.SetValue (iter, (int)Columns.Fuzzy, !val);
				store.SetValue (iter, (int)Columns.Stock, GetStockForEntry (entry));
				store.SetValue (iter, (int)Columns.RowColor, GetRowColorForEntry (entry));
				store.SetValue (iter, (int)Columns.ForeColor, GetForeColorForEntry (entry));
				UpdateProgressBar ();
			}
		}
		
		static string GetStockForEntry (CatalogEntry entry)
		{
			if (entry.References.Length == 0)
				return Gtk.Stock.DialogError;
			return entry.IsFuzzy ? iconFuzzy : entry.IsTranslated ? iconValid : iconMissing;
		}
		
		static string iconFuzzy   = "silk_error";// "md-translation-fuzzy";
		static string iconValid   = "silk_accept";//"md-translation-valid";
		static string iconMissing = "silk_exclamation";//"md-translation-missing";
		
//		static Color translated   = new Color (255, 255, 255);
		static Color untranslated = new Color (234, 232, 227);
		static Color fuzzy        = new Color (237, 226, 187);
		static Color missing      = new Color (237, 167, 167);
		
		Color GetRowColorForEntry (CatalogEntry entry)
		{
			if (entry.References.Length == 0)
				return missing;
			return entry.IsFuzzy ? fuzzy : entry.IsTranslated ? Style.Base (StateType.Normal) : untranslated;
		}
		
		Color GetForeColorForEntry (CatalogEntry entry)
		{
			if (entry.References.Length == 0)
				return missing;
			return entry.IsFuzzy ? Style.Black : entry.IsTranslated ? Style.Text (StateType.Normal) : Style.Black;
		}
		
		static int GetTypeSortIndicator (CatalogEntry entry)
		{
			return entry.IsFuzzy ? 1 : entry.IsTranslated ? 0 : 2;
		}
			
		TreeIter SelectedIter {
			get {
				TreeIter iter;
				if (treeviewEntries.Selection.GetSelected (out iter)) 
					return iter;
				return Gtk.TreeIter.Zero;
			}
		}
			
		CatalogEntry SelectedEntry {
			get {
				TreeIter iter = SelectedIter;
				if (iter.Equals (Gtk.TreeIter.Zero))
					return null;
				if (treeviewEntries.Selection.IterIsSelected (iter))
					return store.GetValue (iter, (int)Columns.CatalogEntry) as CatalogEntry;
				return null;
			}
		}
		
		void OnEntrySelected (object sender, EventArgs args)
		{			
			CatalogEntry entry = SelectedEntry;
			if (entry != null)
				EditEntry (entry);
		}
		
		public void UpdateEntry (CatalogEntry entry)
		{	
			TreeIter iter, foundIter = TreeIter.Zero;
			
			// Look if selected is the same - only wanted usecase
			if (treeviewEntries.Selection.GetSelected (out iter)) {
				CatalogEntry storeEntry = store.GetValue (iter, (int)Columns.CatalogEntry) as CatalogEntry;
				if (entry.Equals (storeEntry))
					foundIter = iter;
			}
						
			// Update data
			if (foundIter.Stamp != TreeIter.Zero.Stamp) {
				store.SetValue (foundIter, (int)Columns.Fuzzy, entry.IsFuzzy);
				store.SetValue (foundIter, (int)Columns.Stock, GetStockForEntry (entry));
				store.SetValue (foundIter, (int)Columns.RowColor, GetRowColorForEntry (entry));
				store.SetValue (foundIter, (int)Columns.ForeColor, GetForeColorForEntry (entry));
			}
		}
 
		bool IsMatch (string text, string filter)
		{
			if (RegexSearch)
				return regex.IsMatch (text);
		
			if (!IsCaseSensitive)
				text = text.ToUpper ();
			int idx = text.IndexOf (filter);
			if (idx >= 0) {
				if (IsWholeWordOnly) {
					return (idx == 0 || char.IsWhiteSpace (text[idx - 1])) &&
						   (idx + filter.Length == text.Length || char.IsWhiteSpace (text[idx + 1]));
				}
				return true;
			}
			return false;
		}
		
		bool ShouldFilter (CatalogEntry entry, string filter)
		{
			if (entry.IsFuzzy) {
				if (!this.togglebuttonFuzzy.Active) {
					return true;
				}
			} else {
				if (!entry.IsTranslated && !this.togglebuttonMissing.Active)
					return true;
				if (entry.IsTranslated && !this.togglebuttonOk.Active)
					return true;
			}
			
			if (String.IsNullOrEmpty (filter)) 
				return false;
			if (DoSearchIn != SearchIn.Translated) {
				if (IsMatch (entry.String, filter))
					return false;
				if (entry.HasPlural) {
					if (IsMatch (entry.PluralString, filter))
						return false;
				}
			}
			
			if (DoSearchIn != SearchIn.Original) {
				for (int i = 0; i < entry.NumberOfTranslations; i++) {
					if (IsMatch (entry.GetTranslation (i), filter))
						return false;
				}
			}
			return true;
		}
		
		FilterWorkerThread updateThread = null;
		string filter = "";
		Regex  regex = new Regex ("");
		
		void UpdateFromCatalog ()
		{
			filter = this.entryFilter.Text;
			if (!IsCaseSensitive && filter != null)
				filter = filter.ToUpper ();
			if (RegexSearch) {
				try {
					RegexOptions options = RegexOptions.Compiled;
					if (!IsCaseSensitive)
						options |= RegexOptions.IgnoreCase;
					regex = new Regex (filter, options);
				} catch (Exception e) {
					IdeApp.Workbench.StatusBar.ShowError (e.Message);
					this.entryFilter.ModifyBase (StateType.Normal, errorColor);
					return;
				}
			}
			this.entryFilter.ModifyBase (StateType.Normal, Style.Base (StateType.Normal));
			StopFilterWorkerThread ();
			updateThread = new FilterWorkerThread (this);
			updateThread.Start ();
		}
		
		void StopFilterWorkerThread ()
		{
			if (updateThread != null)  {
				updateThread.Stop ();
				//updateThread.WaitForFinish ();
				updateThread = null;
			}
		}
		bool IsVisible (TreePath path)
		{
			TreePath start, end, cur;
			this.treeviewEntries.GetVisibleRange (out start, out end);
			TreeIter iter;
			if (!store.GetIter (out iter, start))
				return false;
			do {
				cur = store.GetPath (iter);
				if (cur.Equals (path))
					return true;
			} while (!cur.Equals (end) && store.IterNext (ref iter));
			return false;
		}
		public void SelectEntry (CatalogEntry entry)
		{
			if (updateThread != null && !updateThread.IsStopped)
				return;
			
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					CatalogEntry curEntry = store.GetValue (iter, 4) as CatalogEntry;
					if (entry == curEntry) {
						this.treeviewEntries.Selection.SelectIter (iter);
						TreePath iterPath = store.GetPath (iter);
						if (!IsVisible (iterPath))
							this.treeviewEntries.ScrollToCell (iterPath, treeviewEntries.GetColumn (0), true, 0, 0);
						return;
					}
				} while (store.IterNext (ref iter));
			}
			store.AppendValues (GetStockForEntry (entry), 
			                    entry.IsFuzzy,
			                    StringEscaping.ToGettextFormat (entry.String), 
			                    StringEscaping.ToGettextFormat (entry.GetTranslation (0)), 
			                    entry,
			                    GetRowColorForEntry (entry),
			                    GetTypeSortIndicator (entry),
			                    GetForeColorForEntry (entry)
			);
			SelectEntry (entry);
		}
		
		class FilterWorkerThread : WorkerThread
		{
			POEditorWidget widget;
			
			public FilterWorkerThread (POEditorWidget widget)
			{
				this.widget = widget;
			}
			
			protected override void InnerRun ()
			{
				int number = 1, found = 0;
				double count = widget.catalog.Count;
				ListStore newStore = new ListStore (typeof(string), typeof(bool), typeof(string), typeof(string), typeof(CatalogEntry), typeof(Gdk.Color), typeof(int), typeof(Gdk.Color));
				DispatchService.GuiSyncDispatch (delegate {
					IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Update catalog list..."));
				});
				
				try {
					foreach (CatalogEntry entry in widget.catalog) {
						if (IsStopping)
							return;
						number++;
						if (number % 50 == 0) {
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction (Math.Min (1.0, Math.Max (0.0, number / (double)count)));
							});
						}
						if (!widget.ShouldFilter (entry, widget.filter)) {
							newStore.AppendValues (GetStockForEntry (entry), 
								entry.IsFuzzy, StringEscaping.ToGettextFormat (entry.String), 
								StringEscaping.ToGettextFormat (entry.GetTranslation (0)), 
								entry,
								widget.GetRowColorForEntry (entry),
								GetTypeSortIndicator (entry),
								widget.GetForeColorForEntry (entry)
							);
							found++;
						}
					}
				} catch (Exception) {
				
				}
				if (!IsStopping) {
					MonoDevelop.Core.Gui.DispatchService.GuiSyncDispatch (delegate {
						widget.store.Dispose ();
						widget.treeviewEntries.Model = widget.store = newStore;
						MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.EndProgress ();
						MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.ShowMessage (string.Format (GettextCatalog.GetPluralString ("Found {0} catalog entry.", "Found {0} catalog entries.", found), found));
					});
				} /*else {
					MonoDevelop.Core.Gui.DispatchService.GuiSyncDispatch (delegate {
						MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.EndProgress ();
					});
				}*/
				Stop ();
			}

		}
		
#endregion
		
#region Toolbar handling
		ToggleToolButton AddButton (string label)
		{
			ToggleToolButton newButton = new ToggleToolButton ();
			isUpdating = true;
			try {
				newButton.Label = label;
				newButton.IsImportant = true;
				newButton.Clicked += new EventHandler (OnButtonToggled);
				newButton.ShowAll ();
				this.toolbarPages.Insert (newButton, -1);
			} finally {
				isUpdating = false;
			}
			return newButton;
		}
		
		protected virtual void OnButtonToggled (object sender, System.EventArgs e)
		{
			int i = Array.IndexOf (this.toolbarPages.Children, sender);
			if (i != -1)
				ShowPage (i);
		}
		
		bool isUpdating = false;
		void ShowPage (int page)
		{
			if (notebookPages.CurrentPage == page || isUpdating)
				return;
				
			isUpdating = true;
			try {
				notebookPages.CurrentPage = page;
				for (int i = 0; i < toolbarPages.Children.Length; i++) {
					((ToggleToolButton) toolbarPages.Children[i]).Active = (i == page);
				}
			} finally {
				isUpdating = false;
			}
		}
#endregion
		
		protected override void OnDestroyed ()
		{
			StopFilterWorkerThread ();
			StopTaskWorkerThread ();
		
			if (store != null) {
				store.Dispose ();
				store = null;
			}
			
			if (foundInStore != null) {
				foundInStore.Dispose ();
				foundInStore = null;
			}
			
			widgets.Remove (this);
			ClearTasks ();
			
			base.OnDestroyed ();
		}
#region Tasks
		public class TranslationTask : Task
		{
			POEditorWidget widget;
			CatalogEntry entry;
			
			public TranslationTask (POEditorWidget widget, CatalogEntry entry, string description) : base (widget.poFileName,
			                                                                           description, 0, 0,
			                                                                           TaskSeverity.Error, TaskPriority.Normal, null, widget)
			{
				this.widget = widget;
				this.entry  = entry;
			}
			
			public override void JumpToPosition ()
			{
				widget.SelectEntry (entry);
			}
		}
		
		void ClearTasks ()
		{
			TaskService.Errors.ClearByOwner (this);
		}
		
		static bool CompareTasks (List<Task> list1, List<Task> list2)
		{
			if (list1.Count != list2.Count)
				return false;
			for (int i = 0; i < list1.Count; i++) {
				if (list1[i].Description != list2[i].Description)
					return false;
			}
			return true;
		}
		static CatalogEntryRule[] allRules = {
			new PointRuleCatalogEntryRule (),
			new CaseMismatchCatalogEntryRule (),
			new UnderscoreCatalogEntryRule (),
			new StringFormatCatalogEntryRule (),
			new EndsWithWhitespaceCatalogEntryRule ()
		};
		IEnumerable<CatalogEntryRule> rules = new CatalogEntryRule[] {};
		
		public void UpdateRules (string country)
		{
			rules = from n in allRules where n.IsValid (country) select n;
			UpdateTasks ();
		}
		
		abstract class CatalogEntryRule
		{
			public virtual bool IsValid (string country)
			{
				switch (country) {
				case "ca":
				case "cs":
				case "da":
				case "de":
				case "es":
				case "fr":
				case "hu":
				case "it":
				case "nl":
				case "pl":
				case "pt":
					return true;
				}
				return false;
			}
			public abstract bool EntryFails (CatalogEntry entry);
			public abstract string FailReason (CatalogEntry entry);
		}
		
		class EndsWithWhitespaceCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				return entry.String.EndsWith (" ") && !entry.GetTranslation (0).EndsWith (" ");
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				return GettextCatalog.GetString ("Translation for '{0}' doesn't end with whitespace ' '.", entry.String);
			}
		}
			
		class PointRuleCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				return entry.String.EndsWith (".") && !entry.GetTranslation (0).EndsWith (".");
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				return GettextCatalog.GetString ("Translation for '{0}' doesn't end with '.'.", entry.String);
			}
		}
		
		class CaseMismatchCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				return char.IsLetter (entry.String[0]) && char.IsLetter (entry.GetTranslation (0)[0])  &&
						char.IsUpper (entry.String[0]) && !char.IsUpper (entry.GetTranslation (0)[0]);
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				return GettextCatalog.GetString ("Casing mismatch in '{0}'", entry.String);
			}
		}
		
		class UnderscoreCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				return entry.String.Contains ("_") && !entry.GetTranslation (0).Contains ("_") ||
					!entry.String.Contains ("_") && entry.GetTranslation (0).Contains ("_");
				
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				if (entry.String.Contains ("_") && !entry.GetTranslation (0).Contains ("_"))
					return GettextCatalog.GetString ("Original string '{0}' contains '_', translation doesn't.", entry.String);
				return GettextCatalog.GetString ("Original string '{0}' doesn't contain '_', translation does.", entry.String);
			}
		}
		
		class StringFormatCatalogEntryRule : CatalogEntryRule
		{
			public override bool EntryFails (CatalogEntry entry)
			{
				foreach (System.Text.RegularExpressions.Match match in Regex.Matches (entry.String, @"\{.\}", RegexOptions.None))  {
					if (!entry.GetTranslation (0).Contains (match.Value)) 
						return true;
				}
				return false;
			}
			
			public override string FailReason (CatalogEntry entry)
			{
				foreach (System.Text.RegularExpressions.Match match in Regex.Matches (entry.String, @"\{.\}", RegexOptions.None))  {
					if (!entry.GetTranslation (0).Contains (match.Value)) 
						return GettextCatalog.GetString ("Original string '{0}' contains '{1}', translation doesn't.", entry.String, match.Value);
				}
				return "";
			}
		}
		
		
		List<Task> currentTasks = new List<Task> ();
		class UpdateTaskWorkerThread : WorkerThread
		{
			POEditorWidget widget;
			
			public UpdateTaskWorkerThread (POEditorWidget widget)
			{
				this.widget = widget;
			}
			
			List<Task> tasks = new List<Task> ();
			protected override void InnerRun ()
			{
				if (widget.catalog == null) {
					widget.ClearTasks ();
					Stop ();
					return;
				}
				try {
					foreach (CatalogEntryRule rule in widget.rules) {
						foreach (CatalogEntry entry in widget.catalog) {
							if (IsStopping)
								return;
							if (String.IsNullOrEmpty (entry.String) || String.IsNullOrEmpty (entry.GetTranslation (0)))
								continue;
								if (rule.EntryFails (entry)) {
									tasks.Add (new TranslationTask (widget,
									                                entry,
									                                rule.FailReason (entry)));
								}
						}
					}
				} catch (Exception e) {
					System.Console.WriteLine (e);
					Stop ();
					return;
				}
				if (!CompareTasks (tasks, widget.currentTasks)) {
					widget.ClearTasks ();
					widget.currentTasks = tasks;
					TaskService.Errors.AddRange (tasks);
				}
				Stop ();
			}
		}
		
		UpdateTaskWorkerThread updateTaskThread = null;
		
		void StopTaskWorkerThread ()
		{
			if (updateTaskThread != null)  {
				updateTaskThread.Stop ();
				updateTaskThread.WaitForFinish ();
				updateTaskThread = null;
			}
		}
		
		void UpdateTasks ()
		{
			StopTaskWorkerThread ();
			updateTaskThread = new UpdateTaskWorkerThread (this);
			updateTaskThread.Start ();
		}
#endregion

		#region IUndoHandler implementation
		Stack<Change> undoStack = new Stack<Change> ();
		Stack<Change> redoStack = new Stack<Change> ();
		public class Change
		{
			POEditorWidget widget;
			public CatalogEntry Entry {
				get; 
				set;
			}
			public string OldText {
				get;
				set;
			}
			public string Text {
				get;
				set;
			}
			public int Index {
				get;
				set;
			}
			public Change (POEditorWidget widget, CatalogEntry entry, string oldText, string text, int index)
			{
				this.widget = widget;
				this.Entry = entry;
				this.OldText = oldText;
				this.Text  = text;
				this.Index = index;
			}
			
			public void Undo ()
			{
				widget.inUndoOperation = true;
				widget.SelectEntry (Entry);
				Mono.TextEditor.TextEditor textView = widget.GetTextView (Index);
				if (textView != null)
					textView.Document.Text = OldText;
				widget.inUndoOperation = false;
			}
			
			public void Redo ()
			{
				widget.inUndoOperation = true;
				widget.SelectEntry (Entry);
				Mono.TextEditor.TextEditor textView = widget.GetTextView (Index);
				if (textView != null)
					textView.Document.Text = Text;
				widget.inUndoOperation = false;
			}
		}
		
		bool inUndoOperation = false;
		public void AddChange (CatalogEntry entry, string oldText, string text, int index)
		{
			if (inUndoOperation)
				return;
			redoStack.Clear ();
			undoStack.Push (new Change (this, entry, oldText, text, index));
		}
		
		public void Undo ()
		{
			Change change = undoStack.Pop ();
			change.Undo ();
			redoStack.Push (change);
		}
		
		public void Redo ()
		{
			Change change = redoStack.Pop ();
			change.Redo ();
			undoStack.Push (change);
		}
		
		public void BeginAtomicUndo ()
		{
		}
		
		public void EndAtomicUndo ()
		{
		}
		
		public bool EnableUndo {
			get {
				return undoStack.Count > 0;
			}
		}
		
		public bool EnableRedo {
			get {
				return redoStack.Count > 0;
			}
		}
		#endregion
	}
}
