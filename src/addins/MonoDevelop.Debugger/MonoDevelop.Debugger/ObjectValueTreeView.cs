// ObjectValueTree.cs
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
using System.Xml;
using System.Text;
using System.Collections.Generic;
using Gtk;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.Debugger
{
	[System.ComponentModel.ToolboxItem (true)]
	public class ObjectValueTreeView: Gtk.TreeView, ICompletionWidget
	{
		List<string> valueNames = new List<string> ();
		Dictionary<string,string> oldValues = new Dictionary<string,string> ();
		List<ObjectValue> values = new List<ObjectValue> ();
		Dictionary<ObjectValue,TreeIter> nodes = new Dictionary<ObjectValue, TreeIter> (); 
		Dictionary<string,ObjectValue> cachedValues = new Dictionary<string,ObjectValue> ();
		TreeStore store;
		TreeViewState state;
		string createMsg;
		bool allowAdding;
		bool allowEditing;
		bool compact;
		StackFrame frame;
		bool disposed;
		
		CellRendererText crtExp;
		CellRendererText crtValue;
		CellRendererText crtType;
		CellRendererPixbuf crpButton;
		Gtk.Entry editEntry;
		Mono.Debugging.Client.CompletionData currentCompletionData;
		
		TreeViewColumn valueCol;
		TreeViewColumn typeCol;
		
		string errorColor = "red";
		string modifiedColor = "blue";
		string disabledColor = "gray";
		
		const int NameCol = 0;
		const int ValueCol = 1;
		const int TypeCol = 2;
		const int ObjectCol = 3;
		const int ExpandedCol = 4;
		const int NameEditableCol = 5;
		const int ValueEditableCol = 6;
		const int IconCol = 7;
		const int NameColorCol = 8;
		const int ValueColorCol = 9;
		const int ValueButtonIconCol = 10;
		const int ValueButtonVisibleCol = 11;
		
		public event EventHandler StartEditing;
		public event EventHandler EndEditing;

		public ObjectValueTreeView ()
		{
			store = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(ObjectValue), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool));
			Model = store;
			RulesHint = true;
			
			Pango.FontDescription newFont = this.Style.FontDescription.Copy ();
			newFont.Size = (newFont.Size * 8) / 10;
			
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Name");
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", IconCol);
			crtExp = new CellRendererText ();
			col.PackStart (crtExp, true);
			col.AddAttribute (crtExp, "text", NameCol);
			col.AddAttribute (crtExp, "editable", NameEditableCol);
			col.AddAttribute (crtExp, "foreground", NameColorCol);
			col.Resizable = true;
			AppendColumn (col);
			
			valueCol = new TreeViewColumn ();
			valueCol.Expand = true;
			valueCol.Title = GettextCatalog.GetString ("Value");
			crtValue = new CellRendererText ();
			valueCol.PackStart (crtValue, true);
			valueCol.AddAttribute (crtValue, "text", ValueCol);
			valueCol.AddAttribute (crtValue, "editable", ValueEditableCol);
			valueCol.AddAttribute (crtValue, "foreground", ValueColorCol);
			crpButton = new CellRendererPixbuf ();
			crpButton.StockSize = (uint) Gtk.IconSize.Menu;
			valueCol.PackStart (crpButton, false);
			valueCol.AddAttribute (crpButton, "stock_id", ValueButtonIconCol);
			valueCol.AddAttribute (crpButton, "visible", ValueButtonVisibleCol);
			valueCol.Resizable = true;
			AppendColumn (valueCol);
			
			typeCol = new TreeViewColumn ();
			typeCol.Expand = true;
			typeCol.Title = GettextCatalog.GetString ("Type");
			crtType = new CellRendererText ();
			typeCol.PackStart (crtType, true);
			typeCol.AddAttribute (crtType, "text", TypeCol);
			typeCol.Resizable = true;
			AppendColumn (typeCol);
			
			state = new TreeViewState (this, NameCol);
			
			crtExp.Edited += OnExpEdited;
			crtExp.EditingStarted += OnExpEditing;
			crtExp.EditingCanceled += OnEditingCancelled;
			crtValue.EditingStarted += OnValueEditing;
			crtValue.Edited += OnValueEdited;
			crtValue.EditingCanceled += OnEditingCancelled;
			
			createMsg = GettextCatalog.GetString ("Click here to add a new watch");
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			disposed = true;
		}

		
		public StackFrame Frame {
			get {
				return frame;
			}
			set {
				frame = value;
				Update ();
			}
		}
				
		public void SaveState ()
		{
			state.Save ();
		}
		
		public void LoadState ()
		{
			state.Load ();
		}
		
		public bool AllowAdding {
			get {
				return allowAdding;
			}
			set {
				allowAdding = value;
				Refresh ();
			}
		}
		
		public bool AllowEditing {
			get {
				return allowEditing;
			}
			set {
				allowEditing = value;
				Refresh ();
			}
		}
		
		public bool CompactView {
			get {
				return compact; 
			}
			set {
				compact = value;
				Pango.FontDescription newFont;
				if (compact) {
					newFont = this.Style.FontDescription.Copy ();
					newFont.Size = (newFont.Size * 8) / 10;
				} else {
					newFont = this.Style.FontDescription;
				}
				crtExp.FontDesc = newFont;
				crtValue.FontDesc = newFont;
				crtType.FontDesc = newFont;
			}
		}
		
		public void AddExpression (string exp)
		{
			valueNames.Add (exp);
			Refresh ();
		}
		
		public void AddExpressions (IEnumerable<string> exps)
		{
			valueNames.AddRange (exps);
			Refresh ();
		}
		
		public void RemoveExpression (string exp)
		{
			valueNames.Remove (exp);
			Refresh ();
		}
		
		public void AddValue (ObjectValue value)
		{
			values.Add (value);
			Refresh ();
		}
		
		public void AddValues (IEnumerable<ObjectValue> newValues)
		{
			foreach (ObjectValue val in newValues)
				values.Add (val);
			Refresh ();
		}
		
		public void RemoveValue (ObjectValue value)
		{
			values.Remove (value);
			Refresh ();
		}
		
		public void ClearValues ()
		{
			values.Clear ();
			Refresh ();
		}
		
		public void ClearExpressions ()
		{
			valueNames.Clear ();
			Update ();
		}
		
		public IEnumerable<string> Expressions {
			get { return valueNames; }
		}
		
		public void Update ()
		{
			cachedValues.Clear ();
			Refresh ();
		}
		
		public void Refresh ()
		{
			foreach (ObjectValue val in new List<ObjectValue> (nodes.Keys))
				UnregisterValue (val);
			nodes.Clear ();
			
			state.Save ();
			
			store.Clear ();

			foreach (ObjectValue val in values)
				AppendValue (TreeIter.Zero, null, val);
			
			if (valueNames.Count > 0) {
				ObjectValue[] expValues = GetValues (valueNames.ToArray ());
				for (int n=0; n<expValues.Length; n++)
					AppendValue (TreeIter.Zero, valueNames [n], expValues [n]);
			}
			
			if (AllowAdding)
				store.AppendValues (createMsg, "", "", null, true, true, null, disabledColor, disabledColor);
			
			state.Load ();
		}
		
		void RefreshRow (TreeIter it)
		{
			ObjectValue val = (ObjectValue) store.GetValue (it, ObjectCol);
			UnregisterValue (val);
			
			RemoveChildren (it);
			TreeIter parent;
			if (!store.IterParent (out parent, it))
				parent = TreeIter.Zero;
			
			EvaluationOptions ops = frame.DebuggerSession.Options.EvaluationOptions;
			ops.AllowMethodEvaluation = true;
			ops.AllowTargetInvoke = true;
			
			string oldName = val.Name;
			val.Refresh (ops);
			
			// Don't update the name for the values entered by the user
			if (store.IterDepth (it) == 0)
				val.Name = oldName;
			
			SetValues (parent, it, val.Name, val);
			RegisterValue (val, it);
		}
		
		void RemoveChildren (TreeIter it)
		{
			TreeIter cit;
			while (store.IterChildren (out cit, it)) {
				ObjectValue val = (ObjectValue) store.GetValue (cit, ObjectCol);
				if (val != null)
					UnregisterValue (val);
				RemoveChildren (cit);
				store.Remove (ref cit);
			}
		}

		void RegisterValue (ObjectValue val, TreeIter it)
		{
			if (val.IsEvaluating) {
				nodes [val] = it;
				val.ValueChanged += OnValueUpdated;
			}
		}

		void UnregisterValue (ObjectValue val)
		{
			val.ValueChanged -= OnValueUpdated;
			nodes.Remove (val);
		}

		void OnValueUpdated (object o, EventArgs a)
		{
			Application.Invoke (delegate {
				if (disposed)
					return;
				ObjectValue val = (ObjectValue) o;
				TreeIter it;
				if (FindValue (val, out it)) {
					// Keep the expression name entered by the user
					if (store.IterDepth (it) == 0)
						val.Name = (string) store.GetValue (it, NameCol);
					RemoveChildren (it);
					TreeIter parent;
					if (!store.IterParent (out parent, it))
						parent = TreeIter.Zero;
					
					// If it was an evaluating group, replace the node with the new nodes
					if (val.IsEvaluatingGroup) {
						if (val.ArrayCount == 0) {
							store.Remove (ref it);
						} else {
							SetValues (parent, it, null, val.GetArrayItem (0));
							RegisterValue (val, it);
							for (int n=1; n<val.ArrayCount; n++) {
								TreeIter cit = store.InsertNodeAfter (it);
								ObjectValue cval = val.GetArrayItem (n);
								SetValues (parent, cit, null, cval);
								RegisterValue (cval, cit);
							}
						}
					} else {
						SetValues (parent, it, val.Name, val);
					}
				}
				UnregisterValue (val);
			});
		}

		bool FindValue (ObjectValue val, out TreeIter it)
		{
			return nodes.TryGetValue (val, out it);
		}
		
		public void ResetChangeTracking ()
		{
			oldValues.Clear ();
		}
		
		public void ChangeCheckpoint ()
		{
			oldValues.Clear ();
			
			TreeIter it;
			if (!store.GetIterFirst (out it))
				return;
			
			ChangeCheckpoint (it, "/");
		}
		
		void ChangeCheckpoint (TreeIter it, string path)
		{
			do {
				string name = (string) store.GetValue (it, NameCol);
				string val = (string) store.GetValue (it, ValueCol);
				oldValues [path + name] = val;
				TreeIter cit;
				if (store.IterChildren (out cit, it))
					ChangeCheckpoint (cit, name + "/");
			} while (store.IterNext (ref it));
		}
		
		void AppendValue (TreeIter parent, string name, ObjectValue val)
		{
			TreeIter it;
			if (parent.Equals (TreeIter.Zero))
				it = store.AppendNode ();
			else
				it = store.AppendNode (parent);
			SetValues (parent, it, name, val);
			RegisterValue (val, it);
		}
		
		void SetValues (TreeIter parent, TreeIter it, string name, ObjectValue val)
		{
			string strval;
			bool canEdit;
			string nameColor = null;
			string valueColor = null;
			string valueButton = null;
			
			if (name == null)
				name = val.Name;

			bool hasParent = !parent.Equals (TreeIter.Zero);
			
			string valPath;
			if (!hasParent)
				valPath = "/" + name;
			else
				valPath = GetIterPath (parent) + "/" + name;
			
			string oldValue;
			oldValues.TryGetValue (valPath, out oldValue);
			
			if (val.IsUnknown) {
				strval = GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", val.Name);
				nameColor = disabledColor;
				canEdit = false;
			}
			else if (val.IsError) {
				strval = val.Value;
				int i = strval.IndexOf ('\n');
				if (i != -1)
					strval = strval.Substring (0, i);
				valueColor = errorColor;
				canEdit = false;
			}
			else if (val.IsNotSupported) {
				strval = val.Value;
				valueColor = disabledColor;
				if (val.CanRefresh)
					valueButton = Gtk.Stock.Refresh;
				canEdit = false;
			}
			else if (val.IsEvaluating) {
				strval = GettextCatalog.GetString ("Evaluating...");
				valueColor = disabledColor;
				if (val.IsEvaluatingGroup) {
					nameColor = disabledColor;
					name = val.Name;
				}
				canEdit = false;
			}
			else {
				canEdit = val.IsPrimitive && !val.IsReadOnly && allowEditing;
				strval = val.DisplayValue ?? "(null)";
				if (oldValue != null && strval != oldValue)
					nameColor = valueColor = modifiedColor;
			}
			
			string icon = GetIcon (val.Flags);

			store.SetValue (it, NameCol, name);
			store.SetValue (it, ValueCol, strval);
			store.SetValue (it, TypeCol, val.TypeName);
			store.SetValue (it, ObjectCol, val);
			store.SetValue (it, ExpandedCol, !val.HasChildren);
			store.SetValue (it, NameEditableCol, !hasParent && allowAdding);
			store.SetValue (it, ValueEditableCol, canEdit);
			store.SetValue (it, IconCol, icon);
			store.SetValue (it, NameColorCol, nameColor);
			store.SetValue (it, ValueColorCol, valueColor);
			store.SetValue (it, ValueButtonIconCol, valueButton);
			store.SetValue (it, ValueButtonVisibleCol, valueButton != null);
			
			if (val.HasChildren) {
				// Add dummy node
				it = store.AppendValues (it, "", "", "", null, true);
			}
			
		}
		
		internal static string GetIcon (ObjectValueFlags flags)
		{
			if ((flags & ObjectValueFlags.Field) != 0 && (flags & ObjectValueFlags.ReadOnly) != 0)
				return "md-literal";
			
			string source;
			string stic = (flags & ObjectValueFlags.Global) != 0 ? "static-" : string.Empty;
			
			switch (flags & ObjectValueFlags.OriginMask) {
				case ObjectValueFlags.Property: source = "property"; break;
				case ObjectValueFlags.Type: source = "class"; stic = string.Empty; break;
				case ObjectValueFlags.Literal: return "md-literal";
				case ObjectValueFlags.Namespace: return "md-name-space";
				case ObjectValueFlags.Group: return "md-open-resource-folder";
				default: source = "field"; break;
			}
			string access;
			switch (flags & ObjectValueFlags.AccessMask) {
				case ObjectValueFlags.Private: access = "private-"; break;
				case ObjectValueFlags.Internal: access = "internal-"; break;
				case ObjectValueFlags.InternalProtected:
				case ObjectValueFlags.Protected: access = "protected-"; break;
				default: access = string.Empty; break;
			}
			
			return "md-" + access + stic + source;
		}
		
		protected override bool OnTestExpandRow (TreeIter iter, TreePath path)
		{
			bool expanded = (bool) store.GetValue (iter, ExpandedCol);
			if (!expanded) {
				store.SetValue (iter, ExpandedCol, true);
				TreeIter it;
				store.IterChildren (out it, iter);
				store.Remove (ref it);
				ObjectValue val = (ObjectValue) store.GetValue (iter, ObjectCol);
				foreach (ObjectValue cval in val.GetAllChildren ())
					AppendValue (iter, null, cval);
				return base.OnTestExpandRow (iter, path);
			}
			else
				return false;
		}
		
		protected override void OnRowCollapsed (TreeIter iter, TreePath path)
		{
			base.OnRowCollapsed (iter, path);
			if (compact)
				ColumnsAutosize ();
		}
		
		protected override void OnRowExpanded (TreeIter iter, TreePath path)
		{
			base.OnRowExpanded (iter, path);
			if (compact)
				ColumnsAutosize ();
		}
		
		string GetIterPath (TreeIter iter)
		{
			StringBuilder sb = new StringBuilder ();
			do {
				string name = (string) store.GetValue (iter, NameCol);
				sb.Insert (0, "/" + name);
			} while (store.IterParent (out iter, iter));
			return sb.ToString ();
		}

		[MonoDevelop.Components.Commands.CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Rename)]
		protected void OnStartEditing ()
		{
			Gtk.TreeIter it;
			if (Selection.GetSelected (out it))
				SetCursor (store.GetPath (it), Columns[0], true);
		}

		void OnExpEditing (object s, Gtk.EditingStartedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			Gtk.Entry e = (Gtk.Entry) args.Editable;
			if (e.Text == createMsg)
				e.Text = string.Empty;
			
			OnStartEditing (args);
		}
		
		void OnExpEdited (object s, Gtk.EditedArgs args)
		{
			OnEndEditing ();
			
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			if (store.GetValue (it, ObjectCol) == null) {
				if (args.NewText.Length > 0) {
					valueNames.Add (args.NewText);
					Refresh ();
				}
			} else {
				string exp = (string) store.GetValue (it, NameCol);
				if (args.NewText == exp)
					return;
				int i = valueNames.IndexOf (exp);
				if (args.NewText.Length != 0)
					valueNames [i] = args.NewText;
				else
					valueNames.RemoveAt (i);
				Refresh ();
			}
		}
		
		void OnValueEditing (object s, Gtk.EditingStartedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			
			Gtk.Entry e = (Gtk.Entry) args.Editable;
			
			ObjectValue val = store.GetValue (it, ObjectCol) as ObjectValue;
			string strVal = val.Value;
			if (!string.IsNullOrEmpty (strVal))
				e.Text = strVal;
			
			e.GrabFocus ();
			OnStartEditing (args);
		}
		
		void OnValueEdited (object s, Gtk.EditedArgs args)
		{
			OnEndEditing ();
			
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			ObjectValue val = store.GetValue (it, ObjectCol) as ObjectValue;
			try {
				string newVal = args.NewText;
/*				if (newVal == null) {
					MessageService.ShowError (GettextCatalog.GetString ("Unregognized escape sequence."));
					return;
				}
*/				val.Value = newVal;
			} catch (Exception ex) {
				LoggingService.LogError ("Could not set value for object '" + val.Name + "'", ex);
			}
			store.SetValue (it, ValueCol, val.DisplayValue);

			// Update the color
			
			string newColor = null;
			
			string valPath = GetIterPath (it);
			string oldValue;
			if (oldValues.TryGetValue (valPath, out oldValue)) {
				if (oldValue != val.Value)
					newColor = modifiedColor;
			}
			
			store.SetValue (it, NameColorCol, newColor);
			store.SetValue (it, ValueColorCol, newColor);
		}
		
		void OnEditingCancelled (object s, EventArgs args)
		{
			OnEndEditing ();
		}
		
		void OnStartEditing (Gtk.EditingStartedArgs args)
		{
			editEntry = (Gtk.Entry) args.Editable;
			editEntry.KeyPressEvent += OnEditKeyPress;
			if (StartEditing != null)
				StartEditing (this, EventArgs.Empty);
		}
		
		void OnEndEditing ()
		{
			editEntry.KeyPressEvent -= OnEditKeyPress;
			CompletionWindowManager.HideWindow ();
			currentCompletionData = null;
			if (EndEditing != null)
				EndEditing (this, EventArgs.Empty);
		}
		
		[GLib.ConnectBeforeAttribute]
		void OnEditKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			Gtk.Entry entry = (Gtk.Entry) s;
			
			if (currentCompletionData != null) {
				KeyActions ka;
				bool ret = CompletionWindowManager.PreProcessKeyEvent (args.Event.Key, (char)args.Event.Key, args.Event.State, out ka);
				CompletionWindowManager.PostProcessKeyEvent (ka);
				args.RetVal = ret;
			}
			
			Gtk.Application.Invoke (delegate {
				char c = (char) Gdk.Keyval.ToUnicode (args.Event.KeyValue);
				if (currentCompletionData == null && IsCompletionChar (c)) {
					string exp = entry.Text.Substring (0, entry.CursorPosition);
					currentCompletionData = GetCompletionData (exp);
					if (currentCompletionData != null) {
						DebugCompletionDataList dataList = new DebugCompletionDataList (currentCompletionData);
						CodeCompletionContext ctx = ((ICompletionWidget)this).CreateCodeCompletionContext (entry.CursorPosition - currentCompletionData.ExpressionLenght);
						CompletionWindowManager.ShowWindow (c, dataList, this, ctx, OnCompletionWindowClosed);
					} else
						currentCompletionData = null;
				}
			});
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			bool res = base.OnButtonPressEvent (evnt);
			TreePath path;
			TreeViewColumn col;
			CellRenderer cr;
			
			if (GetCellAtPos ((int)evnt.X, (int)evnt.Y, out path, out col, out cr)) {
				if (cr == crpButton) {
					TreeIter it;
					store.GetIter (out it, path);
					RefreshRow (it);
				}
			}
			return res;
		}
		
		bool GetCellAtPos (int x, int y, out TreePath path, out TreeViewColumn col, out CellRenderer cellRenderer)
		{
			int cx, cy;
			if (GetPathAtPos (x, y, out path, out col, out cx, out cy)) {
				foreach (CellRenderer cr in col.CellRenderers) {
					int xo, w;
					col.CellGetPosition (cr, out xo, out w);
					if (cx >= xo && cx < xo + w) {
						cellRenderer = cr;
						return true;
					}
				}
			}
			cellRenderer = null;
			return false;
		}

		
		bool IsCompletionChar (char c)
		{
			return (char.IsLetterOrDigit (c) || char.IsPunctuation (c) || char.IsSymbol (c) || char.IsWhiteSpace (c));
		}
		
		void OnCompletionWindowClosed ()
		{
			currentCompletionData = null;
		}
		
		#region ICompletionWidget implementation 
		
		EventHandler completionContextChanged;
		
		event EventHandler ICompletionWidget.CompletionContextChanged {
			add { completionContextChanged += value; }
			remove { completionContextChanged -= value; }
		}
		
		string ICompletionWidget.GetText (int startOffset, int endOffset)
		{
			if (startOffset < 0) startOffset = 0;
			if (endOffset > editEntry.Text.Length) endOffset = editEntry.Text.Length;
			return editEntry.Text.Substring (startOffset, endOffset - startOffset);
		}
		
		char ICompletionWidget.GetChar (int offset)
		{
			string txt = editEntry.Text;
			if (offset >= txt.Length)
				return (char)0;
			else
				return txt [offset];
		}
		
		CodeCompletionContext ICompletionWidget.CreateCodeCompletionContext (int triggerOffset)
		{
			CodeCompletionContext c = new CodeCompletionContext ();
			c.TriggerLine = 0;
			c.TriggerOffset = triggerOffset;
			c.TriggerLineOffset = c.TriggerOffset;
			c.TriggerTextHeight = editEntry.SizeRequest ().Height;
			c.TriggerWordLength = currentCompletionData.ExpressionLenght;
			
			int x, y;
			int tx, ty;
			editEntry.GdkWindow.GetOrigin (out x, out y);
			editEntry.GetLayoutOffsets (out tx, out ty);
			int cp = editEntry.TextIndexToLayoutIndex (editEntry.Position);
			Pango.Rectangle rect = editEntry.Layout.IndexToPos (cp);
			tx += Pango.Units.ToPixels (rect.X) + x;
			y += editEntry.Allocation.Height;
				
			c.TriggerXCoord = tx;
			c.TriggerYCoord = y;
			return c;
		}
		
		string ICompletionWidget.GetCompletionText (CodeCompletionContext ctx)
		{
			return editEntry.Text.Substring (ctx.TriggerOffset, ctx.TriggerWordLength);
		}
		
		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			int sp = editEntry.Position - partial_word.Length;
			editEntry.DeleteText (sp, sp + partial_word.Length);
			editEntry.InsertText (complete_word, ref sp);
			editEntry.Position = sp; // sp is incremented by InsertText
		}
		
		int ICompletionWidget.TextLength {
			get {
				return editEntry.Text.Length;
			}
		}
		
		int ICompletionWidget.SelectedLength {
			get {
				return 0;
			}
		}
		
		Style ICompletionWidget.GtkStyle {
			get {
				return editEntry.Style;
			}
		}
		#endregion 

		ObjectValue[] GetValues (string[] names)
		{
			ObjectValue[] values = new ObjectValue [names.Length];
			List<string> list = new List<string> ();
			
			for (int n=0; n<names.Length; n++) {
				ObjectValue val;
				if (cachedValues.TryGetValue (names [n], out val))
					values [n] = val;
				else
					list.Add (names[n]);
			}

			ObjectValue[] qvalues;
			if (frame != null)
				qvalues = frame.GetExpressionValues (list.ToArray (), true);
			else {
				qvalues = new ObjectValue [list.Count];
				for (int n=0; n<qvalues.Length; n++)
					qvalues [n] = ObjectValue.CreateUnknown (list [n]);
			}

			int kv = 0;
			for (int n=0; n<values.Length; n++) {
				if (values [n] == null) {
					values [n] = qvalues [kv++];
					cachedValues [names[n]] = values [n];
				}
			}
			
			return values;
		}
		
		Mono.Debugging.Client.CompletionData GetCompletionData (string exp)
		{
			if (frame != null)
				return frame.GetExpressionCompletionData (exp);
			else
				return null;
		}
		
		internal void SetCustomFont (Pango.FontDescription font)
		{
			crtExp.FontDesc = crtType.FontDesc = crtValue.FontDesc = font;
		}
	}
	
	class DebugCompletionDataList: List<ICompletionData>, ICompletionDataList
	{
		public bool IsSorted { get; set; }
		public DebugCompletionDataList (Mono.Debugging.Client.CompletionData data)
		{
			IsSorted = false;
			foreach (CompletionItem it in data.Items)
				Add (new DebugCompletionData (it));
		}
		public bool AutoSelect { get; set; }
		public string DefaultCompletionString {
			get {
				return string.Empty;
			}
		}

		public bool AutoCompleteUniqueMatch {
			get { return false; }
		}
		
		public bool AutoCompleteEmptyMatch {
			get { return false; }
		}

		public CompletionSelectionMode CompletionSelectionMode {
			get;
			set;
		}
	}
	
	class DebugCompletionData: ICompletionData
	{
		CompletionItem item;
		
		public DebugCompletionData (CompletionItem item)
		{
			this.item = item;
		}
		
		public string Icon {
			get {
				return ObjectValueTreeView.GetIcon (item.Flags);
			}
		}
		
		public string DisplayText {
			get {
				return item.Name;
			}
		}
		
		public string Description {
			get {
				return string.Empty;
			}
		}
		
		public string CompletionText {
			get {
				return item.Name;
			}
		}
		
		public DisplayFlags DisplayFlags {
			get { return DisplayFlags.None; }
		}
	}
}
