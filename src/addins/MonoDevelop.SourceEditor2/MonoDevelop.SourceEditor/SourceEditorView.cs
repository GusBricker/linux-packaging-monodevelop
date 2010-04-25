// SourceEditorView.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Debugger;
using Mono.Debugging.Client;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.CodeTemplates;
using Services = MonoDevelop.Projects.Services;

namespace MonoDevelop.SourceEditor
{	
	public class SourceEditorView : AbstractViewContent, IExtensibleTextEditor, IBookmarkBuffer, IClipboardHandler, 
		ICompletionWidget,  ISplittable, IFoldable, IToolboxDynamicProvider, IEncodedTextContent,
		ICustomFilteringToolboxConsumer, IZoomable, ITextEditorResolver, Mono.TextEditor.ITextEditorDataProvider,
		ICodeTemplateWidget, ITemplateWidget, ISupportsProjectReload, IPrintable
	{
		SourceEditorWidget widget;
		bool isDisposed = false;
		FileSystemWatcher fileSystemWatcher;
		static bool isInWrite = false;
		DateTime lastSaveTime;
		string loadedMimeType;
		internal object MemoryProbe = Counters.SourceViewsInMemory.CreateMemoryProbe ();
		
		TextMarker currentDebugLineMarker;
		TextMarker debugStackLineMarker;
		TextMarker breakpointMarker;
		TextMarker breakpointDisabledMarker;
		TextMarker tracepointMarker;
		TextMarker tracepointDisabledMarker;
		TextMarker breakpointInvalidMarker;
		
		int lastDebugLine = -1;
		EventHandler currentFrameChanged;
		EventHandler<BreakpointEventArgs> breakpointAdded;
		EventHandler<BreakpointEventArgs> breakpointRemoved;
		EventHandler<BreakpointEventArgs> breakpointStatusChanged;
		
		List<LineSegment> breakpointSegments = new List<LineSegment> ();
		LineSegment debugStackSegment;
		LineSegment currentLineSegment;
		
		bool writeAllowed;
		bool writeAccessChecked;
		
		public Mono.TextEditor.Document Document {
			get {
				return widget.TextEditor.Document;
			}
		}
		
		public ExtensibleTextEditor TextEditor {
			get {
				return widget.TextEditor;
			}
		}
		
		internal SourceEditorWidget SourceEditorWidget {
			get {
				return widget;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return widget;
			}
		}
		
		public int LineCount {
			get {
				return Document.LineCount;
			}
		}
		
		public override Project Project {
			get {
				return base.Project;
			}
			set {
				if (value != base.Project)
					((StyledSourceEditorOptions)SourceEditorWidget.TextEditor.Options).UpdateStyleParent (value, loadedMimeType);
				base.Project = value;
			}
		}
			
		public override string TabPageLabel {
			get { return GettextCatalog.GetString ("Source"); }
		}
		bool wasEdited = false;
		public SourceEditorView ()
		{
			Counters.LoadedEditors++;
			currentFrameChanged = (EventHandler)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler (OnCurrentFrameChanged));
			breakpointAdded = (EventHandler<BreakpointEventArgs>)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointAdded));
			breakpointRemoved = (EventHandler<BreakpointEventArgs>)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointRemoved));
			breakpointStatusChanged = (EventHandler<BreakpointEventArgs>)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler<BreakpointEventArgs> (OnBreakpointStatusChanged));
			
			widget = new SourceEditorWidget (this);
			widget.TextEditor.Document.TextReplaced += delegate(object sender, ReplaceEventArgs args) {
				wasEdited = true;
				int startIndex = args.Offset;
				int endIndex = startIndex + Math.Max (args.Count, args.Value != null ? args.Value.Length : 0);
				if (TextChanged != null)
					TextChanged (this, new TextChangedEventArgs (startIndex, endIndex));
			};
			
			widget.TextEditor.Document.EndUndo += delegate {
				if (!inLoad && wasEdited) {
					autoSave.InformAutoSaveThread (Document);
				}
				wasEdited = false;
			};
			
			widget.TextEditor.Document.TextReplacing += OnTextReplacing;
			widget.TextEditor.Document.TextReplaced += OnTextReplaced;
			widget.TextEditor.Document.ReadOnlyCheckDelegate = CheckReadOnly;
			
			//			widget.TextEditor.Document.DocumentUpdated += delegate {
			//				this.IsDirty = Document.IsDirty;
			//			};
			
			widget.TextEditor.Caret.PositionChanged += delegate {
				FireCompletionContextChanged ();
			};
			widget.TextEditor.IconMargin.ButtonPressed += OnIconButtonPress;
			widget.ShowAll ();
			
			debugStackLineMarker = new DebugStackLineTextMarker (widget.TextEditor);
			currentDebugLineMarker = new CurrentDebugLineTextMarker (widget.TextEditor);
			breakpointMarker = new BreakpointTextMarker (widget.TextEditor, false);
			breakpointDisabledMarker = new DisabledBreakpointTextMarker (widget.TextEditor, false);
			tracepointMarker = new BreakpointTextMarker (widget.TextEditor, true);
			tracepointDisabledMarker = new DisabledBreakpointTextMarker (widget.TextEditor, true);
			breakpointInvalidMarker = new InvalidBreakpointTextMarker (widget.TextEditor);
			
			
			fileSystemWatcher = new FileSystemWatcher ();
			fileSystemWatcher.Created += (FileSystemEventHandler)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));
			fileSystemWatcher.Changed += (FileSystemEventHandler)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new FileSystemEventHandler (OnFileChanged));
			this.WorkbenchWindowChanged += delegate {
				if (WorkbenchWindow != null) {
					WorkbenchWindow.ActiveViewContentChanged += delegate {
						widget.UpdateLineCol ();
					};
				}
			};
			this.ContentNameChanged += delegate {
				this.Document.FileName = this.ContentName;
				isInWrite = true;
				if (String.IsNullOrEmpty (ContentName) || !File.Exists (ContentName))
					return;
				
				fileSystemWatcher.EnableRaisingEvents = false;
				lastSaveTime = File.GetLastWriteTime (ContentName);
				fileSystemWatcher.Path = Path.GetDirectoryName (ContentName);
				fileSystemWatcher.Filter = Path.GetFileName (ContentName);
				isInWrite = false;
				fileSystemWatcher.EnableRaisingEvents = true;
			};
			ClipbardRingUpdated += UpdateClipboardRing;
			
			DebuggingService.CurrentFrameChanged += currentFrameChanged;
			DebuggingService.StoppedEvent += currentFrameChanged;
			DebuggingService.ResumedEvent += currentFrameChanged;
			DebuggingService.Breakpoints.BreakpointAdded += breakpointAdded;
			DebuggingService.Breakpoints.BreakpointRemoved += breakpointRemoved;
			DebuggingService.Breakpoints.BreakpointStatusChanged += breakpointStatusChanged;
			DebuggingService.Breakpoints.BreakpointModified += breakpointStatusChanged;
		}
		AutoSave autoSave = new AutoSave ();
		
		internal AutoSave AutoSave {
			get {
				return autoSave;
			}
		}
		
		public override void Save (string fileName)
		{
			Save (fileName, this.encoding);
		}
		
		public void Save (string fileName, string encoding)
		{
			autoSave.FileName = fileName;
			autoSave.RemoveAutoSaveFile ();

			if (ContentName != fileName) {
				if (!FileService.RequestFileEdit (fileName))
					return;
				writeAllowed = true;
				writeAccessChecked = true;
			}

			if (warnOverwrite) {
				if (fileName == ContentName) {
					if (MonoDevelop.Core.Gui.MessageService.AskQuestion (GettextCatalog.GetString ("This file {0} has been changed outside of MonoDevelop. Are you sure you want to overwrite the file?", fileName), MonoDevelop.Core.Gui.AlertButton.Cancel, MonoDevelop.Core.Gui.AlertButton.OverwriteFile) != MonoDevelop.Core.Gui.AlertButton.OverwriteFile)
						return;
				}
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}

			isInWrite = true;
			try {
				object attributes = null;
				if (File.Exists (fileName)) {
					try {
						attributes = DesktopService.GetFileAttributes (fileName);
					} catch (Exception e) {
						LoggingService.LogWarning ("Can't get file attributes", e);
					}
				}

				TextFile.WriteFile (fileName, Document.Text, encoding);
				lastSaveTime = File.GetLastWriteTime (fileName);
				try {
					if (attributes != null)
						DesktopService.SetFileAttributes (fileName, attributes);
				} catch (Exception e) {
					LoggingService.LogError ("Can't set file attributes", e);
				}
			} finally {
				isInWrite = false;
			}
				
//			if (encoding != null)
//				se.Buffer.SourceEncoding = encoding;
//			TextFileService.FireCommitCountChanges (this);
			
			ContentName = fileName; 
			UpdateMimeType (fileName);
			Document.SetNotDirtyState ();
			this.IsDirty = false;
		}
		
		public override void Load (string fileName)
		{
			Load (fileName, null);
		}
		
		public void Load (string fileName, string encoding)
		{
			// Handle the "reload" case.
			if (autoSave.FileName == fileName) {
				autoSave.RemoveAutoSaveFile ();
			}
			autoSave.FileName = fileName;
			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			
			// Look for a mime type for which there is a syntax mode
			UpdateMimeType (fileName);
			
			if (AutoSave.AutoSaveExists (fileName)) {
				widget.ShowAutoSaveWarning (fileName);
				this.encoding = encoding;
			} else {
				TextFile file = TextFile.ReadFile (fileName, encoding);
				inLoad = true;
				Document.Text = file.Text;
				inLoad = false;
				this.encoding = file.SourceEncoding;
			}
			ContentName = fileName;
			widget.SetParsedDocument (ProjectDomService.GetParsedDocument (ProjectDomService.GetProjectDom (Project), fileName), false);
			widget.TextEditor.Caret.Offset = 0;
			UpdateExecutionLocation ();
			UpdateBreakpoints ();
			this.IsDirty = false;
		}

		bool warnOverwrite = false;
		bool inLoad = false;
		string encoding = null;
		public void Load (string fileName, string content, string encoding)
		{
			autoSave.FileName = fileName;
			if (warnOverwrite) {
				warnOverwrite = false;
				widget.RemoveMessageBar ();
				WorkbenchWindow.ShowNotification = false;
			}
			UpdateMimeType (fileName);
			
			inLoad = true;
			Document.Text = content;
			inLoad = false;
			this.encoding = encoding;
			ContentName = fileName;

			UpdateExecutionLocation ();
			UpdateBreakpoints ();
			this.IsDirty = false;
		}
		
		void UpdateMimeType (string fileName)
		{
			// Look for a mime type for which there is a syntax mode
			string mimeType = DesktopService.GetMimeTypeForUri (fileName);
			if (loadedMimeType != mimeType) {
				loadedMimeType = mimeType;
				if (mimeType != null) {
					foreach (string mt in DesktopService.GetMimeTypeInheritanceChain (loadedMimeType)) {
						if (Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode (mt) != null) {
							Document.MimeType = mt;
							break;
						}
					}
				}
				((StyledSourceEditorOptions)SourceEditorWidget.TextEditor.Options).UpdateStyleParent (Project, loadedMimeType);
			}
		}
		
		public string SourceEncoding {
			get { return encoding; }
		}
		
		public override void Dispose()
		{
			this.isDisposed= true;
			Counters.LoadedEditors--;
			
			if (autoSave != null) {
				autoSave.RemoveAutoSaveFile ();
				autoSave.Dispose ();
				autoSave = null;
			}
			
			ClipbardRingUpdated -= UpdateClipboardRing;
			if (fileSystemWatcher != null) {
				fileSystemWatcher.EnableRaisingEvents = false;
				fileSystemWatcher.Dispose ();
				fileSystemWatcher = null;
			}
			
			if (widget != null) {
				widget.TextEditor.Document.TextReplacing -= OnTextReplacing;
				widget.TextEditor.Document.TextReplacing -= OnTextReplaced;
				widget.TextEditor.Document.ReadOnlyCheckDelegate = null;
				
				widget.Destroy ();
				widget = null;
			}
			
			DebuggingService.CurrentFrameChanged -= currentFrameChanged;
			DebuggingService.StoppedEvent -= currentFrameChanged;
			DebuggingService.ResumedEvent -= currentFrameChanged;
			DebuggingService.Breakpoints.BreakpointAdded -= breakpointAdded;
			DebuggingService.Breakpoints.BreakpointRemoved -= breakpointRemoved;
			DebuggingService.Breakpoints.BreakpointStatusChanged -= breakpointStatusChanged;
			DebuggingService.Breakpoints.BreakpointModified -= breakpointStatusChanged;
			
			// This is not necessary but helps when tracking down memory leaks
			
			debugStackLineMarker = null;
			currentDebugLineMarker = null;
			breakpointMarker = null;
			breakpointDisabledMarker = null;
			breakpointInvalidMarker = null;
			
			currentFrameChanged = null;
			breakpointAdded = null;
			breakpointRemoved = null;
			breakpointStatusChanged = null;
		}
		
		public ProjectDom GetParserContext ()
		{
			//Project project = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (Project != null)
				return ProjectDomService.GetProjectDom (Project);
			return ProjectDom.Empty;
		}
		
		public Ambience GetAmbience ()
		{
			Project project = Project;
			if (project != null)
				return project.Ambience;
			string file = this.IsUntitled ? this.UntitledName : this.ContentName;
			return AmbienceService.GetAmbienceForFile (file);
		}
		
		void OnFileChanged (object sender, FileSystemEventArgs args)
		{
			if (!isInWrite && args.FullPath != ContentName)
				return;
			if (lastSaveTime == File.GetLastWriteTime (ContentName))
				return;
			
			if (args.ChangeType == WatcherChangeTypes.Changed || args.ChangeType == WatcherChangeTypes.Created) 
				widget.ShowFileChangedWarning ();
		}
		
		bool CheckReadOnly (int line)
		{
			if (!writeAccessChecked && !IsUntitled) {
				writeAccessChecked = true;
				writeAllowed = FileService.RequestFileEdit (ContentName);
			}
			return IsUntitled || writeAllowed;
		}
		
		string oldReplaceText;
		
		void OnTextReplacing (object s, ReplaceEventArgs a)
		{
			if (a.Count > 0)  {
				oldReplaceText = widget.TextEditor.Document.GetTextAt (a.Offset, a.Count);
			} else {
				oldReplaceText = "";
			}
		}
		
		void OnTextReplaced (object s, ReplaceEventArgs a)
		{
			this.IsDirty = Document.IsDirty;
			
			DocumentLocation location = Document.OffsetToLocation (a.Offset);
			
			int i=0, lines=0;
			while (i != -1 && i < oldReplaceText.Length) {
				i = oldReplaceText.IndexOf ('\n', i);
				if (i != -1) {
					lines--;
					i++;
				}
			}

			if (a.Value != null) {
				i=0;
				string sb = a.Value;
				while (i < sb.Length) {
					if (sb [i] == '\n')
						lines++;
					i++;
				}
			}
			if (lines != 0)
				TextFileService.FireLineCountChanged (this, location.Line + 1, lines, location.Column + 1);
		}

		void OnCurrentFrameChanged (object s, EventArgs args)
		{
			UpdateExecutionLocation ();
		}
		
		void UpdateExecutionLocation ()
		{
			if (DebuggingService.IsDebugging && !DebuggingService.IsRunning) {
				var frame = CheckFrameIsInFile (DebuggingService.CurrentFrame)
					?? CheckFrameIsInFile (DebuggingService.GetCurrentVisibleFrame ());
				if (frame != null) {
					if (lastDebugLine == frame.SourceLocation.Line)
						return;
					RemoveDebugMarkers ();
					lastDebugLine = frame.SourceLocation.Line;
					var segment = widget.TextEditor.Document.GetLine (lastDebugLine-1);
					if (segment != null) {
						if (DebuggingService.CurrentFrameIndex == 0) {
							currentLineSegment = segment;
							widget.TextEditor.Document.AddMarker (segment, currentDebugLineMarker);
						} else {
							debugStackSegment = segment;
							widget.TextEditor.Document.AddMarker (segment, debugStackLineMarker);
						}
						widget.TextEditor.QueueDraw ();
					}
					return;
				}
			}
			
			if (currentLineSegment != null || debugStackSegment != null) {
				RemoveDebugMarkers ();
				lastDebugLine = -1;
				widget.TextEditor.QueueDraw ();
			}
		}
		
		StackFrame CheckFrameIsInFile (StackFrame frame)
		{
			if (frame != null && frame.SourceLocation.Filename != FilePath.Null
				&& ((FilePath)frame.SourceLocation.Filename).FullPath == ((FilePath)ContentName).FullPath)
				return frame;
			return null;
		}
		
		void RemoveDebugMarkers ()
		{
			if (currentLineSegment != null) {
				widget.TextEditor.Document.RemoveMarker (currentLineSegment, currentDebugLineMarker);
				currentLineSegment = null;
			}
			if (debugStackSegment != null) {
				widget.TextEditor.Document.RemoveMarker (debugStackSegment, debugStackLineMarker);
				debugStackSegment = null;
			}
		}
		
		void UpdateBreakpoints ()
		{
			foreach (LineSegment line in breakpointSegments) {
				widget.TextEditor.Document.RemoveMarker (line, breakpointMarker);
				widget.TextEditor.Document.RemoveMarker (line, breakpointDisabledMarker);
				widget.TextEditor.Document.RemoveMarker (line, breakpointInvalidMarker);
				widget.TextEditor.Document.RemoveMarker (line, tracepointMarker);
				widget.TextEditor.Document.RemoveMarker (line, tracepointDisabledMarker);
			}
			breakpointSegments.Clear ();
			foreach (Breakpoint bp in DebuggingService.Breakpoints.GetBreakpoints ())
				AddBreakpoint (bp);
			widget.TextEditor.QueueDraw ();
			
			// Ensure the current line marker is drawn at the top
			lastDebugLine = -1;
			UpdateExecutionLocation ();
		}
		
		void AddBreakpoint (Breakpoint bp)
		{
			FilePath fp = ContentName;
			if (fp.FullPath == bp.FileName) {
				LineSegment line = widget.TextEditor.Document.GetLine (bp.Line-1);
				if (line == null)
					return;
				if (!bp.Enabled) {
					if (bp.HitAction == HitAction.Break)
						widget.TextEditor.Document.AddMarker (line, breakpointDisabledMarker);
					else
						widget.TextEditor.Document.AddMarker (line, tracepointDisabledMarker);
				}
				else if (bp.IsValid (DebuggingService.DebuggerSession)) {
					if (bp.HitAction == HitAction.Break)
						widget.TextEditor.Document.AddMarker (line, breakpointMarker);
					else
						widget.TextEditor.Document.AddMarker (line, tracepointMarker);
				}
				else
					widget.TextEditor.Document.AddMarker (line, breakpointInvalidMarker);
				widget.TextEditor.QueueDraw ();
				breakpointSegments.Add (line);
			}
		}
		
		void OnBreakpointAdded (object s, BreakpointEventArgs args)
		{
			if (ContentName == null || args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				UpdateBreakpoints ();
				return false;
			});
		}
		
		void OnBreakpointRemoved (object s, BreakpointEventArgs args)
		{
			if (ContentName == null || args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				UpdateBreakpoints ();
				return false;
			});
		}
		
		void OnBreakpointStatusChanged (object s, BreakpointEventArgs args)
		{
			if (ContentName == null || args.Breakpoint.FileName != Path.GetFullPath (ContentName))
				return;
			// Updated with a delay, to make sure it works when called as a
			// result of inserting/removing lines before a breakpoint position
			GLib.Timeout.Add (10, delegate {
				UpdateBreakpoints ();
				return false;
			});
		}
		
		void OnIconButtonPress (object s, MarginMouseEventArgs args)
		{
			if (args.Button == 3) {
				TextEditor.Caret.Line = args.LineNumber;
				TextEditor.Caret.Column = 1;
				IdeApp.CommandService.ShowContextMenu ("/MonoDevelop/SourceEditor2/IconContextMenu/Editor");
			}
			else if (args.Button == 1) {
				if (!string.IsNullOrEmpty (this.Document.FileName))
					DebuggingService.Breakpoints.Toggle (this.Document.FileName, args.LineNumber + 1);
			}
		}
		
		#region IExtensibleTextEditor
		ITextEditorExtension IExtensibleTextEditor.AttachExtension (ITextEditorExtension extension)
		{
			this.widget.TextEditor.Extension = extension;
			return this.widget;
		}
		
//		protected override void OnMoveCursor (MovementStep step, int count, bool extend_selection)
//		{
//			base.OnMoveCursor (step, count, extend_selection);
//			if (extension != null)
//				extension.CursorPositionChanged ();
//		}
		
//		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
//		{
//			if (extension != null)
//				return extension.KeyPress (evnt.Key, evnt.State);
//			return this.KeyPress (evnt.Key, evnt.State); 
//		}		
		#endregion
		
		#region IEditableTextBuffer
		public bool EnableUndo {
			get {
				return this.Document.CanUndo && widget.EditorHasFocus;
			}
		}
		
		public void Undo()
		{
			this.Document.Undo ();
		}
		
		public bool EnableRedo {
			get {
				return this.Document.CanRedo && widget.EditorHasFocus;
			}
		}
		
		class SetCaret 
		{
			SourceEditorView view;
			int line, column;
			bool highlightCaretLine;
			
			public SetCaret (SourceEditorView view, int line, int column, bool highlightCaretLine)
			{
				this.view = view;
				this.line = line;
				this.column = column;
				this.highlightCaretLine = highlightCaretLine;
 			}
			
			public void Run (object sender, ExposeEventArgs e)
			{
				if (view.isDisposed)
					return;
				line = Math.Min (line, view.Document.LineCount);
				view.widget.TextEditor.Caret.AutoScrollToCaret = false;
				try {
					view.widget.TextEditor.Caret.Location = new DocumentLocation (line - 1, column - 1);
					view.widget.TextEditor.GrabFocus ();
					view.widget.TextEditor.CenterToCaret ();
					view.OnCaretPositionSet (EventArgs.Empty);
					view.widget.ExposeEvent -= Run;
				} finally {
					view.widget.TextEditor.Caret.AutoScrollToCaret = true;
					if (highlightCaretLine) {
						view.widget.TextEditor.TextViewMargin.HighlightCaretLine = true;
						view.widget.TextEditor.StartCaretPulseAnimation ();
					}
				}
			}
			
		}
		
		public void SetCaretTo (int line, int column)
		{
			SetCaretTo (line, column, true);
		}
		
		public void SetCaretTo (int line, int column, bool highlight)
		{
			if (widget.Allocation.Width <= 1) {
				SetCaret setCaret = new SetCaret (this, line, column, highlight);
				widget.ExposeEvent += setCaret.Run;
			} else {
				GLib.Timeout.Add (20, delegate {
					new SetCaret (this, line, column, highlight).Run (null, null);
					return false;
				});
			}
		}
		
		public void Redo()
		{
			this.Document.Redo ();
		}
		
		public void BeginAtomicUndo ()
		{
			this.Document.BeginAtomicUndo ();
		}
		public void EndAtomicUndo ()
		{
			this.Document.EndAtomicUndo ();
		}
			
		public string SelectedText { 
			get {
				return TextEditor.IsSomethingSelected ? Document.GetTextAt (TextEditor.SelectionRange) : "";
			}
			set {
				TextEditor.DeleteSelectedText ();
				int length = TextEditor.Insert (TextEditor.Caret.Offset, value);
				TextEditor.SelectionRange = new Segment (TextEditor.Caret.Offset, length);
				TextEditor.Caret.Offset += length; 
			}
		}
		protected virtual void OnCaretPositionSet (EventArgs args)
		{
			if (CaretPositionSet != null) 
				CaretPositionSet (this, args);
		}
		public event EventHandler CaretPositionSet;
		public event EventHandler<TextChangedEventArgs> TextChanged;
		
		public bool HasInputFocus {
			get { return TextEditor.HasFocus; }
		}
		
		#endregion
		
		#region ITextBuffer
		public int CursorPosition { 
			get {
				return TextEditor.Caret.Offset;
			}
			set {
				TextEditor.Caret.Offset = value;
			}
		}

		public int SelectionStartPosition { 
			get {
				if (!TextEditor.IsSomethingSelected)
					return TextEditor.Caret.Offset;
				return TextEditor.SelectionRange.Offset;
			}
		}
		public int SelectionEndPosition { 
			get {
				if (!TextEditor.IsSomethingSelected)
					return TextEditor.Caret.Offset;
				return TextEditor.SelectionRange.EndOffset;
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
			TextEditor.SelectionRange = new Segment (startPosition, endPosition - startPosition);
			TextEditor.ScrollToCaret ();
		}
		
		public void ShowPosition (int position)
		{
			// TODO
		}
		#endregion
		
		#region ITextFile
		public FilePath Name {
			get { 
				return this.ContentName ?? this.UntitledName; 
			} 
		}

		public string Text {
			get {
				return this.widget.TextEditor.Document.Text;
			}
			set {
				this.IsDirty = true;
				this.widget.TextEditor.Document.Text = value;
				if (TextChanged != null)
					TextChanged (this, new TextChangedEventArgs (0, Length));
			}
		}
		
		public int Length { 
			get {
				return this.widget.TextEditor.Document.Length;
			}
		}

		public bool WarnOverwrite {
			get {
				return warnOverwrite;
			}
			set {
				warnOverwrite = value;
			}
		}

		public string GetText (int startPosition, int endPosition)
		{
			if (startPosition < 0 || endPosition < 0 || startPosition > endPosition)
				return "";
			return this.widget.TextEditor.Document.GetTextAt (startPosition, endPosition - startPosition);
		}
		
		public char GetCharAt (int position)
		{
			return this.widget.TextEditor.Document.GetCharAt (position);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return this.widget.TextEditor.Document.LocationToOffset (new DocumentLocation (line - 1, column - 1));
		}
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			DocumentLocation location = this.widget.TextEditor.Document.OffsetToLocation (position);
			line   = location.Line + 1;
			column = location.Column + 1;
		}
		#endregion
		
		#region IEditableTextFile
		public int InsertText (int position, string text)
		{
			int length = this.widget.TextEditor.Insert (position, text);
			this.widget.TextEditor.Caret.Offset = position + length;
			return length;
		}
		public void DeleteText (int position, int length)
		{
			this.widget.TextEditor.Remove (position, length);
			this.widget.TextEditor.Caret.Offset = position;
		}
		#endregion 
		
		#region IBookmarkBuffer
		LineSegment GetLine (int position)
		{
			DocumentLocation location = Document.OffsetToLocation (position);
			return Document.GetLine (location.Line);
		}
				
		public void SetBookmarked (int position, bool mark)
		{
			LineSegment line = GetLine (position);
			if (line != null && line.IsBookmarked != mark) {
				int lineNumber = widget.TextEditor.Document.OffsetToLineNumber (line.Offset);
				line.IsBookmarked = mark;
				widget.TextEditor.Document.RequestUpdate (new LineUpdate (lineNumber));
				widget.TextEditor.Document.CommitDocumentUpdate ();
			}
		}
		
		public bool IsBookmarked (int position)
		{
			LineSegment line = GetLine (position);
			return line != null ? line.IsBookmarked : false;
		}
		
		public void PrevBookmark ()
		{
			TextEditor.RunAction (BookmarkActions.GotoPrevious);
		}
		
		public void NextBookmark ()
		{
			TextEditor.RunAction (BookmarkActions.GotoNext);
		}
		public void ClearBookmarks ()
		{
			TextEditor.RunAction (BookmarkActions.ClearAll);
		}
		#endregion
		
		#region IClipboardHandler
		public bool EnableCut {
			get {
				return widget.EditorHasFocus;
			}
		}
		public bool EnableCopy {
			get {
				return EnableCut;
			}
		}
		public bool EnablePaste {
			get {
				return widget.EditorHasFocus;
			}
		}
		public bool EnableDelete {
			get {
				return widget.EditorHasFocus;
			}
		}
		public bool EnableSelectAll {
			get {
				return widget.EditorHasFocus;
			}
		}
		
		public void Cut ()
		{
			TextEditor.RunAction (ClipboardActions.Cut);
		}
		
		public void Copy ()
		{
			TextEditor.RunAction (ClipboardActions.Copy);
		}
		
		public void Paste ()
		{
			TextEditor.RunAction (ClipboardActions.Paste);
		}
		
		public void Delete ()
		{
			if (TextEditor.IsSomethingSelected) {
				TextEditor.DeleteSelectedText ();
			} else {
				TextEditor.RunAction (DeleteActions.Delete);
			}
		}
		
		public void SelectAll ()
		{
			TextEditor.RunAction (SelectionActions.SelectAll);
		}
		#endregion
		
		#region ICompletionWidget
		public int TextLength {
			get {
				return Document.Length;
			}
		}
		public int SelectedLength { 
			get {
				return TextEditor.IsSomethingSelected ? TextEditor.SelectionRange.Length : 0;
			}
		}
//		public string GetText (int startOffset, int endOffset)
//		{
//			return this.widget.TextEditor.Document.Buffer.GetTextAt (startOffset, endOffset - startOffset);
//		}
		public char GetChar (int offset)
		{
			return Document.GetCharAt (offset);
		}
		
		public Gtk.Style GtkStyle { 
			get {
				return widget.Style.Copy ();
			}
		}

		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset) 
		{
			CodeCompletionContext result = new CodeCompletionContext ();
			result.TriggerOffset = triggerOffset;
			DocumentLocation loc = Document.OffsetToLocation (triggerOffset);
			result.TriggerLine   = loc.Line + 1;
			result.TriggerLineOffset = loc.Column + 1;
			Gdk.Point p = this.widget.TextEditor.DocumentToVisualLocation (loc);
			int tx, ty;
			
			widget.ParentWindow.GetOrigin (out tx, out ty);
			tx += TextEditor.Allocation.X;
			ty += TextEditor.Allocation.Y;
			result.TriggerXCoord = tx + p.X + TextEditor.TextViewMargin.XOffset - (int)TextEditor.HAdjustment.Value;
			result.TriggerYCoord = ty + p.Y - (int)TextEditor.VAdjustment.Value + TextEditor.LineHeight;
			result.TriggerTextHeight = TextEditor.LineHeight;
			return result;
		}
		
		public CodeTemplateContext GetCodeTemplateContext ()
		{
			return TextEditor.GetTemplateContext ();
		}
		
		public string GetCompletionText (CodeCompletionContext ctx)
		{
			if (ctx == null)
				return null;
			int min = Math.Min (ctx.TriggerOffset, TextEditor.Caret.Offset);
			int max = Math.Max (ctx.TriggerOffset, TextEditor.Caret.Offset);
			return Document.GetTextBetween (min, max);
		}
		
		public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			int triggerOffset = ctx.TriggerOffset;
			if (TextEditor.IsSomethingSelected) {
				if (TextEditor.SelectionRange.Offset < ctx.TriggerOffset)
					triggerOffset = ctx.TriggerOffset - TextEditor.SelectionRange.Length;
				TextEditor.DeleteSelectedText ();
			}

			// | in the completion text now marks the caret position
			int idx = complete_word.IndexOf ('|');
			if (idx >= 0) {
				complete_word = complete_word.Remove (idx, 1);
			} else {
				idx = complete_word.Length;
			}
			int length = String.IsNullOrEmpty (partial_word) ? 0 : partial_word.Length;

			triggerOffset += TextEditor.GetTextEditorData ().EnsureCaretIsNotVirtual ();
			this.widget.TextEditor.Document.EndAtomicUndo ();
			this.widget.TextEditor.Replace (triggerOffset, length, complete_word);
			this.widget.TextEditor.Caret.Offset += idx - length;
			this.widget.TextEditor.Document.BeginAtomicUndo ();
			this.widget.TextEditor.Document.CommitLineUpdate (this.widget.TextEditor.Caret.Line);
		}
		
		void FireCompletionContextChanged ()
		{
			if (CompletionContextChanged != null)
				CompletionContextChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler CompletionContextChanged;
		#endregion
		
		#region commenting and indentation

		[CommandHandler (MonoDevelop.Debugger.DebugCommands.ExpressionEvaluator)]
		protected void ShowExpressionEvaluator ()
		{
			string expression;
			if (TextEditor.IsSomethingSelected)
				expression = TextEditor.SelectedText;
			else
				expression = TextEditor.GetExpression (TextEditor.Caret.Offset);
			
			DebuggingService.ShowExpressionEvaluator (expression);
		}

		[CommandUpdateHandler (MonoDevelop.Debugger.DebugCommands.ExpressionEvaluator)]
		protected void UpdateShowExpressionEvaluator (CommandInfo cinfo)
		{
			if (DebuggingService.IsDebugging)
				cinfo.Enabled = DebuggingService.CurrentFrame != null;
			else
				cinfo.Visible = false;
		}
		
		#endregion
		
		#region ISplittable
		public bool EnableSplitHorizontally {
			get {
				return !EnableUnsplit;
			}
		}
		public bool EnableSplitVertically {
			get {
				return !EnableUnsplit;
			}
		}
		public bool EnableUnsplit {
			get {
				return widget.IsSplitted;
			}
		}
		
		public void SplitHorizontally ()
		{
			widget.Split (false);
		}
		
		public void SplitVertically ()
		{
			widget.Split (true);
		}
		
		public void Unsplit ()
		{
			widget.Unsplit ();
		}
		
		public void SwitchWindow ()
		{
			widget.SwitchWindow ();
		}
		
		#endregion
		
		#region IFoldable
		public void ToggleAllFoldings ()
		{
			FoldActions.ToggleAllFolds (TextEditor.GetTextEditorData ());
			widget.TextEditor.ScrollToCaret ();
		}
		
		public void FoldDefinitions ()
		{
			foreach (FoldSegment segment in Document.FoldSegments) {
				if (segment.FoldingType == FoldingType.TypeDefinition)
					segment.IsFolded = false;
				if (segment.FoldingType == FoldingType.TypeMember)
					segment.IsFolded = true;
			}
			widget.TextEditor.Caret.MoveCaretBeforeFoldings ();
			Document.RequestUpdate (new UpdateAll ());
			Document.CommitDocumentUpdate ();
			widget.TextEditor.GetTextEditorData ().RaiseUpdateAdjustmentsRequested ();
			widget.TextEditor.ScrollToCaret ();
		}
		
		public void ToggleFolding ()
		{
			FoldActions.ToggleFold (TextEditor.GetTextEditorData ());
			widget.TextEditor.ScrollToCaret ();
		}
		#endregion
		
		#region IPrintable
		
		public bool CanPrint {
			get { return true; }
		}
		
		public void PrintDocument (PrintingSettings settings)
		{
			RunPrintOperation (PrintOperationAction.PrintDialog, settings);
		}
		
		public void PrintPreviewDocument (PrintingSettings settings)
		{
			RunPrintOperation (PrintOperationAction.Preview, settings);
		}
		
		void RunPrintOperation (PrintOperationAction action, PrintingSettings settings)
		{
			var op = new SourceEditorPrintOperation (TextEditor.Document, Name);
			
			if (settings.PrintSettings != null)
				op.PrintSettings = settings.PrintSettings;
			if (settings.PageSetup != null)
				op.DefaultPageSetup = settings.PageSetup;
			
			//FIXME: implement in-place preview
			//op.Preview += HandleOpPreview;
			
			//FIXME: implement async on platforms that support it
			var result = op.Run (action, IdeApp.Workbench.RootWindow);
			
			if (result == PrintOperationResult.Apply)
				settings.PrintSettings = op.PrintSettings;
			else if (result == PrintOperationResult.Error)
				//FIXME: can't show more details, GTK# GetError binding is bad
				MessageService.ShowError (GettextCatalog.GetString ("Print operation failed."));
		}
		
		#endregion
	
		#region Toolbox
		static List<TextToolboxNode> clipboardRing = new List<TextToolboxNode> ();
		static event EventHandler ClipbardRingUpdated;
		
		static SourceEditorView ()
		{
			ClipboardActions.CopyOperation.Copy += delegate (string text) {
				if (String.IsNullOrEmpty (text))
					return;
				foreach (TextToolboxNode node in clipboardRing) {
					if (node.Text == text) {
						clipboardRing.Remove (node);
						break;
					}
				}
				TextToolboxNode item = new TextToolboxNode (text);
				string[] lines = text.Split ('\n');
				for (int i = 0; i < 3 && i < lines.Length; i++) {
					if (i > 0)
						item.Description += Environment.NewLine;
					string line = lines[i];
					if (line.Length > 16)
						line = line.Substring (0, 16) + "...";
					item.Description += line;
				}
				item.Category = GettextCatalog.GetString ("Clipboard ring");
				item.Icon = DesktopService.GetPixbufForFile ("test.txt", Gtk.IconSize.Menu);
				item.Name = text.Length > 16 ? text.Substring (0, 16) + "..." : text;
				item.Name = item.Name.Replace ("\t", "\\t");
				item.Name = item.Name.Replace ("\n", "\\n");
				clipboardRing.Add (item);
				while (clipboardRing.Count > 12) {
					clipboardRing.RemoveAt (0);
				}
				if (ClipbardRingUpdated != null)
					ClipbardRingUpdated (null, EventArgs.Empty);
			};
		}
		
		public void UpdateClipboardRing (object sender, EventArgs e)
		{
			if (ItemsChanged != null)
				ItemsChanged (this, EventArgs.Empty);
		}
		
		public IEnumerable<ItemToolboxNode> GetDynamicItems (IToolboxConsumer consumer)
		{
			foreach (TextToolboxNode item in clipboardRing)
				yield return item;
			//FIXME: make this work again
//			CategoryToolboxNode category = new CategoryToolboxNode (GettextCatalog.GetString ("Clipboard ring"));
//			category.IsDropTarget    = false;
//			category.CanIconizeItems = false;
//			category.IsSorted        = false;
//			foreach (TextToolboxNode item in clipboardRing) {
//				category.Add (item);
//			}
//			
//			if (clipboardRing.Count == 0) {
//				TextToolboxNode item = new TextToolboxNode (null);
//				item.Category = GettextCatalog.GetString ("Clipboard ring");
//				item.Name = null;
//				//category.Add (item);
//			}
//			return new BaseToolboxNode [] { category };
		}
		
		public event EventHandler ItemsChanged;
		
		void IToolboxConsumer.ConsumeItem (ItemToolboxNode item)
		{
			if (item is TemplateToolboxNode) {
				InsertTemplate (((TemplateToolboxNode)item).Template, new MonoDevelop.Ide.Gui.Document (base.WorkbenchWindow));
				TextEditor.GrabFocus ();
				return;
			}
			string text = GetText (item);
			if (string.IsNullOrEmpty (text))
				return;
			TextEditor.InsertAtCaret (text);
			TextEditor.GrabFocus ();
		}
		
		#region dnd
		Gtk.Widget customSource;
		ItemToolboxNode dragItem;
		void IToolboxConsumer.DragItem (ItemToolboxNode item, Gtk.Widget source, Gdk.DragContext ctx)
		{
			string text = GetText (item);
			if (string.IsNullOrEmpty (text))
				return;
			dragItem = item;
			customSource = source;
			customSource.DragDataGet += HandleDragDataGet;
			customSource.DragEnd += HandleDragEnd;
		}
		
		void HandleDragEnd(object o, DragEndArgs args)
		{
			if (customSource != null) {
				customSource.DragDataGet -= HandleDragDataGet;
				customSource.DragEnd -= HandleDragEnd;
				customSource = null;
			}
		}
		
		void HandleDragDataGet(object o, DragDataGetArgs args)
		{
			if (dragItem != null) {
				TextEditor.CaretToDragCaretPosition ();
				((IToolboxConsumer)this).ConsumeItem (dragItem);
				dragItem = null;
			}
		}
		#endregion
		
		string GetText (ItemToolboxNode item)
		{
			TemplateToolboxNode templateToolboxNode = item as TemplateToolboxNode;
			if (templateToolboxNode != null)
				return templateToolboxNode.Template.Shortcut;
			
			ITextToolboxNode tn = item as ITextToolboxNode;
			if (tn == null) {
				LoggingService.LogWarning ("Cannot use non-ITextToolboxNode toolbox items in the text editor.");
				return null;
			}
			string filename = this.IsUntitled ? UntitledName : ContentName;
			return tn.GetTextForFile (filename, this.Project);
		}
		
		System.ComponentModel.ToolboxItemFilterAttribute[] IToolboxConsumer.ToolboxFilterAttributes {
			get {
				return new System.ComponentModel.ToolboxItemFilterAttribute[] {};
			}
		}
			
		bool ICustomFilteringToolboxConsumer.SupportsItem (ItemToolboxNode item)
		{
			ITextToolboxNode textNode = item as ITextToolboxNode;
			if (textNode == null)
				return false;
			
			string filename = this.IsUntitled ? UntitledName : ContentName;
			//int i = filename.LastIndexOf ('.');
			//string ext = i < 0? null : filename.Substring (i + 1);
			
			return textNode.IsCompatibleWith (filename, this.Project);
		}

		
		public Gtk.TargetEntry[] DragTargets { 
			get {
				return (Gtk.TargetEntry[])ClipboardActions.CopyOperation.targetList;
			}
		}
				
		bool IToolboxConsumer.CustomFilterSupports (ItemToolboxNode item)
		{
			return false;
		}
		
		string IToolboxConsumer.DefaultItemDomain { 
			get {
				return "Text";
			}
		}
		#endregion
		
		#region IZoomable
		bool IZoomable.EnableZoomIn {
			get {
				return this.TextEditor.Options.CanZoomIn;
			}
		}
		
		bool IZoomable.EnableZoomOut {
			get {
				return this.TextEditor.Options.CanZoomOut;
			}
		}
		
		bool IZoomable.EnableZoomReset {
			get {
				return this.TextEditor.Options.CanResetZoom;
			}
		}
		
		void IZoomable.ZoomIn ()
		{
			this.TextEditor.Options.ZoomIn ();
		}
		
		void IZoomable.ZoomOut ()
		{
			this.TextEditor.Options.ZoomOut ();
		}
		
		void IZoomable.ZoomReset ()
		{
			this.TextEditor.Options.ZoomReset ();
		}

		#region ITextEditorResolver implementation 
		
		public ResolveResult GetLanguageItem (int offset)
		{
			return this.SourceEditorWidget.TextEditor.GetLanguageItem (offset);
		}
		
		public ResolveResult GetLanguageItem (int offset, string expression)
		{
			return this.SourceEditorWidget.TextEditor.GetLanguageItem (offset, expression);
		}
		#endregion 
		
		#region ISupportsProjectReload implementaion
		
		ProjectReloadCapability ISupportsProjectReload.ProjectReloadCapability {
			get {
				return ProjectReloadCapability.Full;
			}
		}
		
		void ISupportsProjectReload.Update (Project project)
		{
			// The project will be assigned to the view. Nothing else to do. 
		}
		
		#endregion
		
		#endregion
		public Mono.TextEditor.TextEditorData GetTextEditorData ()
		{
			return TextEditor.GetTextEditorData ();
		}
		
		public void InsertTemplate (CodeTemplate template, MonoDevelop.Ide.Gui.Document doc)
		{
			TextEditor.InsertTemplate (template, doc);
		}
		
		
		[CommandHandler (TextEditorCommands.GotoMatchingBrace)]
		protected void OnGotoMatchingBrace ()
		{
			TextEditor.RunAction (MiscActions.GotoMatchingBracket);
		}
		
	}
} 
