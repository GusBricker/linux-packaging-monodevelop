//
// ProjectOperations.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.CodeDom.Compiler;
using System.Collections.Specialized;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Components;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public class ProjectOperations
	{
		ProjectService projectService = MonoDevelop.Projects.Services.ProjectService;
		IAsyncOperation currentBuildOperation = NullAsyncOperation.Success;
		IAsyncOperation currentRunOperation = NullAsyncOperation.Success;
		IBuildTarget currentBuildOperationOwner;
		IBuildTarget currentRunOperationOwner;
		
		SelectReferenceDialog selDialog = null;
		
		SolutionItem currentSolutionItem = null;
		WorkspaceItem currentWorkspaceItem = null;
		object currentItem;
		
		BuildResult lastResult = new BuildResult ();
		
		internal ProjectOperations ()
		{
			IdeApp.Workspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
			IdeApp.Workspace.ItemUnloading += IdeAppWorkspaceItemUnloading;
		}
		
		public BuildResult LastCompilerResult {
			get { return lastResult; }
		}
		
		public Project CurrentSelectedProject {
			get {
				return currentSolutionItem as Project;
			}
		}
		
		public Solution CurrentSelectedSolution {
			get {
				return currentWorkspaceItem as Solution;
			}
		}
		
		public IBuildTarget CurrentSelectedBuildTarget {
			get {
				if (currentSolutionItem != null)
					return currentSolutionItem;
				return currentWorkspaceItem;
			}
		}
		
		public WorkspaceItem CurrentSelectedWorkspaceItem {
			get {
				return currentWorkspaceItem;
			}
			internal set {
				if (value != currentWorkspaceItem) {
					WorkspaceItem oldValue = currentWorkspaceItem;
					currentWorkspaceItem = value;
					if (oldValue is Solution || value is Solution)
						OnCurrentSelectedSolutionChanged(new SolutionEventArgs (currentWorkspaceItem as Solution));
				}
			}
		}
		
		public SolutionItem CurrentSelectedSolutionItem {
			get {
				if (currentSolutionItem == null && CurrentSelectedSolution != null)
					return CurrentSelectedSolution.RootFolder;
				return currentSolutionItem;
			}
			internal set {
				if (value != currentSolutionItem) {
					SolutionItem oldValue = currentSolutionItem;
					currentSolutionItem = value;
					if (oldValue is Project || value is Project)
						OnCurrentProjectChanged (new ProjectEventArgs(currentSolutionItem as Project));
				}
			}
		}
		
		public object CurrentSelectedItem {
			get {
				return currentItem;
			}
			internal set {
				currentItem = value;
			}
		}
		
		public string ProjectsDefaultPath {
			get {
				return PropertyService.Get ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Projects"));
			}
			set {
				PropertyService.Set ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", value);
			}
		}
		
		public IAsyncOperation CurrentBuildOperation {
			get { return currentBuildOperation; }
		}
		
		public IAsyncOperation CurrentRunOperation {
			get { return currentRunOperation; }
			set { currentRunOperation = value; }
		}
		
		public bool IsBuilding (IBuildTarget target)
		{
			return !currentBuildOperation.IsCompleted && ContainsTarget (target, currentBuildOperationOwner);
		}
		
		public bool IsRunning (IBuildTarget target)
		{
			return !currentRunOperation.IsCompleted && ContainsTarget (target, currentRunOperationOwner);
		}
		
		internal static bool ContainsTarget (IBuildTarget owner, IBuildTarget target)
		{
			if (owner == target)
				return true;
			else if (owner is WorkspaceItem)
				return ((WorkspaceItem)owner).ContainsItem (target);
			return false;
		}
		/*
		string GetDeclaredFile(IMember item)
		{			
			if (item is IMember) {
				IMember mem = (IMember) item;				
				if (mem.Region == null)
					return null;
				else if (mem.Region.FileName != null)
					return mem.Region.FileName;
				else if (mem.DeclaringType != null) {
					foreach (IType c in mem.DeclaringType.Parts) {
						if ((mem is IField && c.Fields.Contains((IField)mem)) ||
						    (mem is IEvent && c.Events.Contains((IEvent)mem)) || 
						    (mem is IProperty  && c.Properties.Contains((IProperty)mem)) ||
						    (mem is IMethod && c.Methods.Contains((IMethod)mem))) {
							return GetClassFileName(c);							
						}                                   
					}
				}
			} else if (item is IType) {
				IType cls = (IType) item;
				return GetClassFileName (cls);
			} else if (item is MonoDevelop.Projects.Parser.LocalVariable) {
				MonoDevelop.Projects.Parser.LocalVariable cls = (MonoDevelop.Projects.Parser.LocalVariable) item;
				return cls.Region.FileName;
			}
			return null;
		}
		
		public bool CanJumpToDeclaration (IMember item)
		{
			return (GetDeclaredFile(item) != null);
		}*/
		
		public bool CanJumpToDeclaration (MonoDevelop.Projects.Dom.INode visitable)
		{
			if (visitable is MonoDevelop.Projects.Dom.IType) 
				return ((MonoDevelop.Projects.Dom.IType)visitable).CompilationUnit != null;
			if (visitable is LocalVariable)
				return true;
			IMember member = visitable as MonoDevelop.Projects.Dom.IMember;
			if (member == null || member.DeclaringType == null) 
				return false ;
			return member.DeclaringType.CompilationUnit != null;
		}

		public void JumpToDeclaration (MonoDevelop.Projects.Dom.INode visitable)
		{
			if (visitable is LocalVariable) {
				LocalVariable var = (LocalVariable)visitable;
				MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (var.FileName,
				                                                   var.Region.Start.Line,
				                                                   var.Region.Start.Column,
				                                                   true);
				return;
			}
			IMember member = visitable as MonoDevelop.Projects.Dom.IMember;
			if (member == null) 
				return;
			string fileName;
			if (member is MonoDevelop.Projects.Dom.IType) {
				try {
					fileName = ((MonoDevelop.Projects.Dom.IType)member).CompilationUnit.FileName;
				} catch (Exception e) {
					LoggingService.LogError ("Can't get file name for type:" + member + ". Try to restart monodevelop.", e);
					fileName = null;
				}
			} else {
				if (member.DeclaringType == null) 
					return;
				fileName = member.DeclaringType.CompilationUnit.FileName;
				if (member is ExtensionMethod)
					fileName = ((ExtensionMethod)member).OriginalMethod.DeclaringType.CompilationUnit.FileName;
			}
			Document doc = MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (fileName, member.Location.Line, member.Location.Column, true);
			if (doc != null) {
				MonoDevelop.Ide.Gui.Content.IUrlHandler handler = doc.ActiveView as MonoDevelop.Ide.Gui.Content.IUrlHandler;
				if (handler != null)
					handler.Open (member.HelpUrl);
			}
		}
		
		public void RenameItem (IWorkspaceFileObject item, string newName)
		{
			ProjectOptionsDialog.RenameItem (item, newName);
			if (item is SolutionItem) {
				Save (((SolutionItem)item).ParentSolution);
			} else {
				IdeApp.Workspace.Save ();
				IdeApp.Workspace.SavePreferences ();
			}
		}
		
		public void Export (IWorkspaceObject item)
		{
			Export (item, null);
		}
		
		public void Export (IWorkspaceObject entry, FileFormat format)
		{
			ExportProjectDialog dlg = new ExportProjectDialog (entry, format);
			try {
				dlg.TransientFor = IdeApp.Workbench.RootWindow;
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					
					using (IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (GettextCatalog.GetString ("Export Project"), null, true, true)) {
						string folder = dlg.TargetFolder;
						
						string file = entry is WorkspaceItem ? ((WorkspaceItem)entry).FileName : ((SolutionEntityItem)entry).FileName;
						Services.ProjectService.Export (mon, file, folder, dlg.Format);
					}
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		public void Save (IEnumerable<SolutionEntityItem> entries)
		{
			List<IWorkspaceFileObject> items = new List<IWorkspaceFileObject> ();
			foreach (IWorkspaceFileObject it in entries)
				items.Add (it);
			Save (items);
		}
		
		public void Save (SolutionEntityItem entry)
		{
			if (!entry.FileFormat.CanWrite (entry)) {
				IWorkspaceFileObject itemContainer = GetContainer (entry);
				if (SelectValidFileFormat (itemContainer))
					Save (itemContainer);
				return;
			}
			
			if (!AllowSave (entry))
				return;
			
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				entry.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Project saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void Save (Solution item)
		{
			if (!item.FileFormat.CanWrite (item)) {
				if (!SelectValidFileFormat (item))
					return;
			}
			
			if (!AllowSave (item))
				return;
			
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				item.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Solution saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void Save (IEnumerable<IWorkspaceFileObject> items)
		{
			int count = items.Count ();
			if (count == 0)
				return;
			
			// Verify that the file format for each item is still valid
			
			HashSet<IWorkspaceFileObject> fixedItems = new HashSet<IWorkspaceFileObject> ();
			HashSet<IWorkspaceFileObject> failedItems = new HashSet<IWorkspaceFileObject> ();
			
			foreach (IWorkspaceFileObject entry in items) {
				IWorkspaceFileObject itemContainer = GetContainer (entry);
				if (fixedItems.Contains (itemContainer) || failedItems.Contains (itemContainer))
					continue;
				if (!entry.FileFormat.CanWrite (entry)) {
					// Can't save the project using this format. Try to find a valid format for the whole solution
					if (SelectValidFileFormat (itemContainer))
						fixedItems.Add (itemContainer);
					else
						failedItems.Add (itemContainer);
				}
			}
			if (fixedItems.Count > 0)
				Save (fixedItems);
			
			if (failedItems.Count > 0 || fixedItems.Count > 0) {
				// Some file format changes were required, and some items were saved.
				// Get a list of items not yet saved.
				List<IWorkspaceFileObject> notSavedEntries = new List<IWorkspaceFileObject> ();
				foreach (IWorkspaceFileObject entry in items) {
					IWorkspaceFileObject itemContainer = GetContainer (entry);
					if (!fixedItems.Contains (itemContainer) && !failedItems.Contains (itemContainer))
						notSavedEntries.Add (entry);
				}
				items = notSavedEntries;
			}
			
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				monitor.BeginTask (null, count);
				foreach (IWorkspaceFileObject item in items) {
					if (AllowSave (item))
						item.Save (monitor);
					monitor.Step (1);
				}
				monitor.EndTask ();
				monitor.ReportSuccess (GettextCatalog.GetString ("Items saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void Save (IWorkspaceFileObject item)
		{
			if (item is SolutionEntityItem)
				Save ((SolutionEntityItem) item);
			else if (item is Solution)
				Save ((Solution)item);
			
			if (!item.FileFormat.CanWrite (item)) {
				IWorkspaceFileObject ci = GetContainer (item);
				if (SelectValidFileFormat (ci))
					Save (ci);
				return;
			}
			
			if (!AllowSave (item))
				return;
			
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				item.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Item saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		bool AllowSave (IWorkspaceFileObject item)
		{
			if (HasChanged (item))
				return MessageService.Confirm (
				    GettextCatalog.GetString ("Some project files have been changed from outside MonoDevelop. Do you want to overwrite them?"),
				    GettextCatalog.GetString ("Changes done in those files will be overwritten by MonoDevelop."),
				    AlertButton.OverwriteFile);
			else
				return true;
		}
		
		bool HasChanged (IWorkspaceFileObject item)
		{
			if (item.ItemFilesChanged)
				return true;
			if (item is WorkspaceItem) {
				foreach (SolutionEntityItem eitem in ((WorkspaceItem)item).GetAllSolutionItems<SolutionEntityItem> ())
					if (eitem.ItemFilesChanged)
						return true;
			}
			return false;
		}

		IWorkspaceFileObject GetContainer (IWorkspaceFileObject item)
		{
			SolutionEntityItem si = item as SolutionEntityItem;
			if (si != null && si.ParentSolution != null && !si.ParentSolution.FileFormat.SupportsMixedFormats)
				return si.ParentSolution;
			else
				return item;
		}
		
		bool SelectValidFileFormat (IWorkspaceFileObject item)
		{
			SelectFileFormatDialog dlg = new SelectFileFormatDialog (item);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok && dlg.Format != null) {
					item.ConvertToFormat (dlg.Format, true);
					return true;
				}
				return false;
			} finally {
				dlg.Destroy ();
			}
		}
		
		public void MarkFileDirty (string filename)
		{
			Project entry = IdeApp.Workspace.GetProjectContainingFile (filename);
			if (entry != null) {
				entry.SetNeedsBuilding (true);
			}
		}
		
		public void ShowOptions (IWorkspaceObject entry)
		{
			ShowOptions (entry, null);
		}
		
		public void ShowOptions (IWorkspaceObject entry, string panelId)
		{
			if (entry is SolutionEntityItem) {
				SolutionEntityItem selectedProject = (SolutionEntityItem) entry;
				
				ProjectOptionsDialog optionsDialog = new ProjectOptionsDialog (IdeApp.Workbench.RootWindow, selectedProject);
				SolutionItemConfiguration conf = selectedProject.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				optionsDialog.CurrentConfig = conf != null ? conf.Name : null;
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					
					if (optionsDialog.Run() == (int)Gtk.ResponseType.Ok) {
						selectedProject.SetNeedsBuilding (true);
						foreach (object ob in optionsDialog.ModifiedObjects) {
							if (ob is Solution) {
								Save ((Solution)ob);
								return;
							}
						}
						Save (selectedProject);
						IdeApp.Workspace.SavePreferences ();
					}
				} finally {
					optionsDialog.Destroy ();
				}
			} else if (entry is Solution) {
				Solution solution = (Solution) entry;
				
				CombineOptionsDialog optionsDialog = new CombineOptionsDialog (IdeApp.Workbench.RootWindow, solution);
				optionsDialog.CurrentConfig = IdeApp.Workspace.ActiveConfigurationId;
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					if (optionsDialog.Run () == (int) Gtk.ResponseType.Ok) {
						Save (solution);
						IdeApp.Workspace.SavePreferences (solution);
					}
				} finally {
					optionsDialog.Destroy ();
				}
			}
			else {
				ItemOptionsDialog optionsDialog = new ItemOptionsDialog (IdeApp.Workbench.RootWindow, entry);
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					if (optionsDialog.Run () == (int) Gtk.ResponseType.Ok) {
						if (entry is IBuildTarget)
							((IBuildTarget)entry).SetNeedsBuilding (true, IdeApp.Workspace.ActiveConfiguration);
						if (entry is IWorkspaceFileObject)
							Save ((IWorkspaceFileObject) entry);
						else {
							SolutionItem si = entry as SolutionItem;
							if (si.ParentSolution != null)
								Save (si.ParentSolution);
						}
						IdeApp.Workspace.SavePreferences ();
					}
				} finally {
					optionsDialog.Destroy ();
				}
			}
		}
		
		public void NewSolution ()
		{
			NewSolution (null);
		}
		
		public void NewSolution (string defaultTemplate)
		{
			NewProjectDialog pd = new NewProjectDialog (null, true, null);
			if (defaultTemplate != null)
				pd.SelectTemplate (defaultTemplate);
			pd.Run ();
			pd.Destroy ();
		}
		
		public WorkspaceItem AddNewWorkspaceItem (Workspace parentWorkspace)
		{
			return AddNewWorkspaceItem (parentWorkspace, null);
		}
		
		public WorkspaceItem AddNewWorkspaceItem (Workspace parentWorkspace, string defaultItemId)
		{
			NewProjectDialog npdlg = new NewProjectDialog (null, false, parentWorkspace.BaseDirectory);
			npdlg.SelectTemplate (defaultItemId);
			try {
				if (npdlg.Run () == (int) Gtk.ResponseType.Ok && npdlg.NewItem != null) {
					parentWorkspace.Items.Add ((WorkspaceItem) npdlg.NewItem);
					Save (parentWorkspace);
					return (WorkspaceItem) npdlg.NewItem;
				}
			} finally {
				npdlg.Destroy ();
			}
			return null;
		}
		
		public WorkspaceItem AddWorkspaceItem (Workspace parentWorkspace)
		{
			WorkspaceItem res = null;
			
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Add to Workspace"));
			try {
				fdiag.SetCurrentFolder (parentWorkspace.BaseDirectory);
				fdiag.SelectMultiple = false;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					try {
						res = AddWorkspaceItem (parentWorkspace, fdiag.Filename);
					}
					catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("The file '{0}' could not be loaded.", fdiag.Filename));
					}
				}
			} finally {
				fdiag.Destroy ();
			}
			
			return res;
		}
		
		public WorkspaceItem AddWorkspaceItem (Workspace parentWorkspace, string itemFileName)
		{
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true)) {
				WorkspaceItem it = Services.ProjectService.ReadWorkspaceItem (monitor, itemFileName);
				if (it != null) {
					parentWorkspace.Items.Add (it);
					Save (parentWorkspace);
				}
				return it;
			}
		}
		
		public SolutionItem CreateProject (SolutionFolder parentFolder)
		{
			SolutionItem res = null;
			string basePath = parentFolder != null ? parentFolder.BaseDirectory : null;
			NewProjectDialog npdlg = new NewProjectDialog (parentFolder, false, basePath);
			npdlg.Run ();
			npdlg.Destroy ();
			return res;
		}

		public SolutionItem AddSolutionItem (SolutionFolder parentFolder)
		{
			SolutionItem res = null;
			
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Add to Solution"));
			try {
				fdiag.SetCurrentFolder (parentFolder.BaseDirectory);
				fdiag.SelectMultiple = false;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					try {
						res = AddSolutionItem (parentFolder, fdiag.Filename);
					}
					catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("The file '{0}' could not be loaded.", fdiag.Filename));
					}
				}
			} finally {
				fdiag.Destroy ();
			}
			
			if (res != null)
				IdeApp.Workspace.Save ();

			return res;
		}
		
		public SolutionItem AddSolutionItem (SolutionFolder folder, string entryFileName)
		{
			AddEntryEventArgs args = new AddEntryEventArgs (folder, entryFileName);
			if (AddingEntryToCombine != null)
				AddingEntryToCombine (this, args);
			if (args.Cancel)
				return null;
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true)) {
				return folder.AddItem (monitor, args.FileName, true);
			}
		}

		public void CreateProjectFile (Project parentProject, string basePath)
		{
			CreateProjectFile (parentProject, basePath, null);
		}
		
		public void CreateProjectFile (Project parentProject, string basePath, string selectedTemplateId)
		{
			NewFileDialog nfd = null;
			try {
				nfd = new NewFileDialog (parentProject, basePath);
				if (selectedTemplateId != null)
					nfd.SelectTemplate (selectedTemplateId);
				nfd.Run ();
			} finally {
				if (nfd != null) nfd.Destroy ();
			}
		}

		public bool AddReferenceToProject (DotNetProject project)
		{
			try {
				if (selDialog == null)
					selDialog = new SelectReferenceDialog ();
				
				selDialog.SetProject (project);

				if (selDialog.Run() == (int)Gtk.ResponseType.Ok) {
					ProjectReferenceCollection newRefs = selDialog.ReferenceInformations;
					
					ArrayList toDelete = new ArrayList ();
					foreach (ProjectReference refInfo in project.References)
						if (!newRefs.Contains (refInfo))
							toDelete.Add (refInfo);
					
					foreach (ProjectReference refInfo in toDelete)
							project.References.Remove (refInfo);

					foreach (ProjectReference refInfo in selDialog.ReferenceInformations)
						if (!project.References.Contains (refInfo))
							project.References.Add(refInfo);
					
					return true;
				}
				else
					return false;
			} finally {
				selDialog.Hide ();
			}
		}
		
		public bool SelectProjectReferences (ProjectReferenceCollection references, AssemblyContext ctx, TargetFramework targetVersion)
		{
			try {
				if (selDialog == null)
					selDialog = new SelectReferenceDialog ();
				
				selDialog.SetReferenceCollection (references, ctx, targetVersion);

				if (selDialog.Run() == (int)Gtk.ResponseType.Ok) {
					references.Clear ();
					references.AddRange (selDialog.ReferenceInformations);
					return true;
				}
				else
					return false;
			} finally {
				if (selDialog != null)
					selDialog.Hide ();
			}
		}
		
		public void RemoveSolutionItem (SolutionItem item)
		{
			string question = GettextCatalog.GetString ("Do you really want to remove project '{0}' from '{1}'?", item.Name, item.ParentFolder.Name);
			string secondaryText = GettextCatalog.GetString ("The Delete option physically removes the project files from disc.");
			
			SolutionEntityItem prj = item as SolutionEntityItem;
			if (prj == null) {
				if (MessageService.Confirm (question, AlertButton.Remove) && IdeApp.Workspace.RequestItemUnload (item))
					RemoveItemFromSolution (prj);
				return;
			}
			
			AlertButton result = MessageService.AskQuestion (question, secondaryText,
			                                                 AlertButton.Delete, AlertButton.Cancel, AlertButton.Remove);
			if (result == AlertButton.Delete) {
				if (!IdeApp.Workspace.RequestItemUnload (prj))
					return;
				ConfirmProjectDeleteDialog dlg = new ConfirmProjectDeleteDialog (prj);
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					
					// Remove the project before removing the files to avoid unnecessary events
					RemoveItemFromSolution (prj);
					
					List<FilePath> files = dlg.GetFilesToDelete ();
					dlg.Destroy ();
					using (IProgressMonitor monitor = new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (true)) {
						monitor.BeginTask (GettextCatalog.GetString ("Deleting Files..."), files.Count);
						foreach (FilePath file in files) {
							try {
								if (Directory.Exists (file))
									FileService.DeleteDirectory (file);
								else
									FileService.DeleteFile (file);
							} catch (Exception ex) {
								monitor.ReportError (GettextCatalog.GetString ("The file or directory '{0}' could not be deleted.", file), ex);
							}
							monitor.Step (1);
						}
						monitor.EndTask ();
					}
				} else
					dlg.Destroy ();
			}
			else if (result == AlertButton.Remove && IdeApp.Workspace.RequestItemUnload (prj)) {
				RemoveItemFromSolution (prj);
			}
		}
		
		void RemoveItemFromSolution (SolutionItem prj)
		{
			Solution sol = prj.ParentSolution;
			prj.ParentFolder.Items.Remove (prj);
			prj.Dispose ();
			IdeApp.ProjectOperations.Save (sol);
		}

		public bool CanExecute (IBuildTarget entry)
		{
			ExecutionContext context = new ExecutionContext (Runtime.ProcessService.DefaultExecutionHandler, IdeApp.Workbench.ProgressMonitors);
			return CanExecute (entry, context);
		}
		
		public bool CanExecute (IBuildTarget entry, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors);
			return entry.CanExecute (context, IdeApp.Workspace.ActiveConfiguration);
		}
		
		public bool CanExecute (IBuildTarget entry, ExecutionContext context)
		{
			return entry.CanExecute (context, IdeApp.Workspace.ActiveConfiguration);
		}
		
		public IAsyncOperation Execute (IBuildTarget entry)
		{
			return Execute (entry, Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		public IAsyncOperation Execute (IBuildTarget entry, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors);
			return Execute (entry, context);
		}
		
		public IAsyncOperation Execute (IBuildTarget entry, ExecutionContext context)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();

			DispatchService.ThreadDispatch (delegate {
				ExecuteSolutionItemAsync (monitor, entry, context);
			});
			currentRunOperation = monitor.AsyncOperation;
			currentRunOperationOwner = entry;
			currentRunOperation.Completed += delegate { currentRunOperationOwner = null; };
			return currentRunOperation;
		}
		
		void ExecuteSolutionItemAsync (IProgressMonitor monitor, IBuildTarget entry, ExecutionContext context)
		{
			try {
				OnBeforeStartProject ();
				entry.Execute (monitor, context, IdeApp.Workspace.ActiveConfiguration);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Execution failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void Clean (IBuildTarget entry)
		{
			entry.RunTarget (new NullProgressMonitor (), ProjectService.CleanTarget, IdeApp.Workspace.ActiveConfiguration);
		}
		
		public IAsyncOperation BuildFile (string file)
		{
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Build (tempProject);
				aop.Completed += delegate { tempProject.Dispose (); };
				return aop;
			} else {
				MessageService.ShowError (GettextCatalog.GetString ("The file {0} can't be compiled.", file));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation ExecuteFile (string file)
		{
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Execute (tempProject);
				aop.Completed += delegate { tempProject.Dispose (); };
				return aop;
			} else {
				MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
		
		public bool CanExecuteFile (string file)
		{
			return CanExecuteFile (file, Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		public bool CanExecuteFile (string file, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors);
			return CanExecuteFile (file, context);
		}
		
		public bool CanExecuteFile (string file, ExecutionContext context)
		{
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				bool res = CanExecute (tempProject, context);
				tempProject.Dispose ();
				return res;
			}
			else
				return false;
		}
		
		public IAsyncOperation ExecuteFile (string file, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors);
			return ExecuteFile (file, context);
		}
		
		public IAsyncOperation ExecuteFile (string file, ExecutionContext context)
		{
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Execute (tempProject, context);
				aop.Completed += delegate { tempProject.Dispose (); };
				return aop;
			} else {
				MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation Rebuild (IBuildTarget entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;

			Clean (entry);
			return Build (entry);
		}

		public IAsyncOperation Build (IBuildTarget entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;
			
			DoBeforeCompileAction ();
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
			
			BeginBuild (monitor);

			DispatchService.ThreadDispatch (delegate {
				BuildSolutionItemAsync (entry, monitor);
			}, null);
			currentBuildOperation = monitor.AsyncOperation;
			currentBuildOperationOwner = entry;
			currentBuildOperation.Completed += delegate { currentBuildOperationOwner = null; };
			return currentBuildOperation;
		}
		
		void BuildSolutionItemAsync (IBuildTarget entry, IProgressMonitor monitor)
		{
			BuildResult result = null;
			try {
				SolutionItem it = entry as SolutionItem;
				if (it != null)
					result = it.Build (monitor, IdeApp.Workspace.ActiveConfiguration, true);
				else
					result = entry.RunTarget (monitor, ProjectService.BuildTarget, IdeApp.Workspace.ActiveConfiguration);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Build failed."), ex);
			}
			DispatchService.GuiDispatch (
				delegate {
					BuildDone (monitor, result, entry);	// BuildDone disposes the monitor
			});
		}

		void DoBeforeCompileAction ()
		{
			BeforeCompileAction action = IdeApp.Preferences.BeforeBuildSaveAction;
			
			switch (action) {
				case BeforeCompileAction.Nothing:
					break;
				case BeforeCompileAction.PromptForSave:
					foreach (Document doc in IdeApp.Workbench.Documents) {
						if (doc.IsDirty && doc.Project != null) {
							if (MessageService.AskQuestion (
						            GettextCatalog.GetString ("Save changed documents before building?"),
							        GettextCatalog.GetString ("Some of the open documents have unsaved changes."),
							                                AlertButton.BuildWithoutSave, AlertButton.Save) == AlertButton.Save) {
								MarkFileDirty (doc.FileName);
								doc.Save ();
							}
							else
								break;
						}
					}
					break;
				case BeforeCompileAction.SaveAllFiles:
					foreach (Document doc in new List<Document> (IdeApp.Workbench.Documents))
						if (doc.IsDirty && doc.Project != null)
							doc.Save ();
					break;
				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}
		}

		void BeginBuild (IProgressMonitor monitor)
		{
			TaskService.Errors.ClearByOwner (this);
			if (StartBuild != null)
				StartBuild (this, new BuildEventArgs (monitor, true));
		}
		
		void BuildDone (IProgressMonitor monitor, BuildResult result, IBuildTarget entry)
		{
			Task[] tasks = null;
		
			try {
				if (result != null) {
					lastResult = result;
					monitor.Log.WriteLine ();
					monitor.Log.WriteLine (GettextCatalog.GetString ("---------------------- Done ----------------------"));
					
					tasks = new Task [result.Errors.Count];
					for (int n=0; n<tasks.Length; n++) {
						tasks [n] = new Task (result.Errors [n]);
						tasks [n].Owner = this;
					}

					TaskService.Errors.AddRange (tasks);
					
					string errorString = GettextCatalog.GetPluralString("{0} error", "{0} errors", result.ErrorCount, result.ErrorCount);
					string warningString = GettextCatalog.GetPluralString("{0} warning", "{0} warnings", result.WarningCount, result.WarningCount);

					if (result.ErrorCount == 0 && result.WarningCount == 0 && lastResult.FailedBuildCount == 0) {
						monitor.ReportSuccess (GettextCatalog.GetString ("Build successful."));
					} else if (result.ErrorCount == 0 && result.WarningCount > 0) {
						monitor.ReportWarning(GettextCatalog.GetString("Build: ") + errorString + ", " + warningString);
					} else if (result.ErrorCount > 0) {
						monitor.ReportError(GettextCatalog.GetString("Build: ") + errorString + ", " + warningString, null);
					} else {
						monitor.ReportError(GettextCatalog.GetString("Build failed."), null);
					}
					OnEndBuild (monitor, lastResult.FailedBuildCount == 0);
				} else
					OnEndBuild (monitor, false);
			}
			finally {
				monitor.Dispose ();
			}
			
			// If there is at least an error or warning, show the error list pad.
			if (tasks != null && tasks.Length > 0) {
				try {
					Pad errorsPad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ();
					if (IdeApp.Preferences.ShowErrorsPadAfterBuild) {
						errorsPad.Visible = true;
						errorsPad.BringToFront ();
					}
				} catch {}
			}
		}
		
		public string[] AddFilesToProject (Project project, string[] files, string targetDirectory)
		{
			int action = -1;
			IProgressMonitor monitor = null;
			
			if (files.Length > 10) {
				monitor = new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (true);
				monitor.BeginTask (GettextCatalog.GetString("Adding files..."), files.Length);
			}
			
			List<string> newFileList = new List<string> ();
			
			using (monitor) {
				
				foreach (string file in files) {
					if (monitor != null)
						monitor.Log.WriteLine (file);
					if (file.StartsWith (project.BaseDirectory)) {
						newFileList.Add (MoveCopyFile (project, targetDirectory, file, true, true));
					} else {
						Gtk.MessageDialog md = new Gtk.MessageDialog (
							 IdeApp.Workbench.RootWindow,
							 Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent,
							 Gtk.MessageType.Question, Gtk.ButtonsType.None,
							 GettextCatalog.GetString ("{0} is outside the project directory, what should I do?", file));

						try {
							Gtk.CheckButton remember = null;
							if (files.Length > 1) {
								remember = new Gtk.CheckButton (GettextCatalog.GetString ("Use the same action for all selected files."));
								md.VBox.PackStart (remember, false, false, 0);
							}
							
							int LINK_VALUE = 3;
							int COPY_VALUE = 1;
							int MOVE_VALUE = 2;
							
							md.AddButton (GettextCatalog.GetString ("_Link"), LINK_VALUE);
							md.AddButton (Gtk.Stock.Copy, COPY_VALUE);
							md.AddButton (GettextCatalog.GetString ("_Move"), MOVE_VALUE);
							md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
							md.VBox.ShowAll ();
							
							int ret = -1;
							if (action < 0) {
								ret = md.Run ();
								if (ret < 0)
									return newFileList.ToArray ();
								if (remember != null && remember.Active) action = ret;
							} else {
								ret = action;
							}
							
							try {
								string nf = MoveCopyFile (project, targetDirectory, file,
											  (ret == MOVE_VALUE) || (ret == LINK_VALUE), ret == LINK_VALUE);
								newFileList.Add (nf);
							}
							catch (Exception ex) {
								MessageService.ShowException (ex, GettextCatalog.GetString ("An error occurred while attempt to move/copy that file. Please check your permissions."));
								newFileList.Add (null);
							}
						} finally {
							md.Destroy ();
						}
					}
					if (monitor != null)
						monitor.Step (1);
				}
			}
			return newFileList.ToArray ();
		}
		
		string MoveCopyFile (Project project, string baseDirectory, string filename, bool move, bool alreadyInPlace)
		{
			if (FileService.IsDirectory (filename))
			    return null;

			string name = System.IO.Path.GetFileName (filename);
			string newfilename = alreadyInPlace ? filename : Path.Combine (baseDirectory, name);

			if (filename != newfilename) {
				if (File.Exists (newfilename)) {
					if (!MessageService.Confirm (GettextCatalog.GetString ("The file '{0}' already exists. Do you want to replace it?", newfilename), AlertButton.OverwriteFile))
						return null;
				}
				FileService.CopyFile (filename, newfilename);
				if (move)
					FileService.DeleteFile (filename);
			}
			
			project.AddFile (newfilename);
			return newfilename;
		}		

		public void TransferFiles (IProgressMonitor monitor, Project sourceProject, FilePath sourcePath, Project targetProject, FilePath targetPath, bool removeFromSource, bool copyOnlyProjectFiles)
		{
			// When transfering directories, targetPath is the directory where the source
			// directory will be transfered, including the destination directory or file name.
			// For example, if sourcePath is /a1/a2/a3 and targetPath is /b1/b2, the
			// new folder or file will be /b1/b2
			
			if (targetProject == null)
				throw new ArgumentNullException ("targetProject");

			if (!targetPath.IsChildPathOf (targetProject.BaseDirectory))
				throw new ArgumentException ("Invalid project folder: " + targetPath);

			if (sourceProject != null && !sourcePath.IsChildPathOf (sourceProject.BaseDirectory))
				throw new ArgumentException ("Invalid project folder: " + sourcePath);
				
			if (copyOnlyProjectFiles && sourceProject == null)
				throw new ArgumentException ("A source project must be specified if copyOnlyProjectFiles is True");
			
			bool sourceIsFolder = Directory.Exists (sourcePath);

			bool movingFolder = (removeFromSource && sourceIsFolder && (
					!copyOnlyProjectFiles ||
					IsDirectoryHierarchyEmpty (sourcePath)));

			// Get the list of files to copy

			ICollection<ProjectFile> filesToMove;
			try {
				if (copyOnlyProjectFiles) {
					filesToMove = sourceProject.Files.GetFilesInPath (sourcePath);
				} else {
					ProjectFileCollection col = new ProjectFileCollection ();
					GetAllFilesRecursive (sourcePath, col);
					filesToMove = col;
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not get any file from '{0}'.", sourcePath), ex);
				return;
			}
			
			// If copying a single file, bring any grouped children along
			if (filesToMove.Count == 1 && sourceProject != null) {
				//Make sure the list is a type to which we can append files
				IList<ProjectFile> list = filesToMove as IList<ProjectFile>;
				if (list == null)
					filesToMove = list = new List<ProjectFile> (filesToMove);
				
				// if file's nor parented on the project, it won't have its children resolved
				// So get the 'real' ProjectFile from the project
				ProjectFile pf = list[0];
				if (pf.Project == null)
					pf = sourceProject.Files.GetFile (pf.Name);
				
				// If it resolved, get the children
				if (pf != null)
					foreach (ProjectFile child in pf.DependentChildren)
						list.Add (child);
			}
			
			// Ensure that the destination folder is created, even if no files
			// are copied
			
			try {
				if (sourceIsFolder && !Directory.Exists (targetPath) && !movingFolder)
					FileService.CreateDirectory (targetPath);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not create directory '{0}'.", targetPath), ex);
				return;
			}

			// Make a copy of the original project files, since MoveDirectory will
			// automatically remove them from the project
			
			ProjectFileCollection oldProjectFiles = null;
			if (sourceProject != null) {
				oldProjectFiles = new ProjectFileCollection ();
				oldProjectFiles.AddRange (sourceProject.Files);
			}

			// Transfer files
			// If moving a folder, do it all at once
			
			if (movingFolder) {
				try {
					FileService.MoveDirectory (sourcePath, targetPath);
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Directory '{0}' could not be moved.", sourcePath), ex);
					return;
				}
			}

			monitor.BeginTask (GettextCatalog.GetString ("Copying files..."), filesToMove.Count);
			
			foreach (ProjectFile file in filesToMove) {
				FilePath sourceFile = file.FilePath;
				FilePath newFile = sourceIsFolder ? targetPath.Combine (sourceFile.ToRelative (sourcePath)) : targetPath;
				
				ProjectFile oldProjectFile = oldProjectFiles != null ? oldProjectFiles.GetFile (sourceFile) : null;
				
				if (!movingFolder) {
					try {
						FilePath fileDir = newFile.ParentDirectory;
						if (!Directory.Exists (fileDir))
							FileService.CreateDirectory (fileDir);
						if (removeFromSource)
							FileService.MoveFile (sourceFile, newFile);
						else
							FileService.CopyFile (sourceFile, newFile);
					} catch (Exception ex) {
						monitor.ReportError (GettextCatalog.GetString ("File '{0}' could not be created.", newFile), ex);
						monitor.Step (1);
						continue;
					}
				}
				
				if (oldProjectFile != null) {
					if (removeFromSource && sourceProject.Files.Contains (oldProjectFile))
						sourceProject.Files.Remove (oldProjectFile);
					if (targetProject.Files.GetFile (newFile) == null) {
						ProjectFile projectFile = (ProjectFile) oldProjectFile.Clone ();
						projectFile.Name = newFile;
						targetProject.Files.Add (projectFile);
					}
				}
				
				monitor.Step (1);
			}
			
			monitor.EndTask ();
		}
		
		void GetAllFilesRecursive (string path, ProjectFileCollection files)
		{
			if (File.Exists (path)) {
				files.Add (new ProjectFile (path));
				return;
			}
			
			foreach (string file in Directory.GetFiles (path))
				files.Add (new ProjectFile (file));
			
			foreach (string dir in Directory.GetDirectories (path))
				GetAllFilesRecursive (dir, files);
		}
		
		bool IsDirectoryHierarchyEmpty (string path)
		{
			if (Directory.GetFiles(path).Length > 0) return false;
			foreach (string dir in Directory.GetDirectories (path))
				if (!IsDirectoryHierarchyEmpty (dir)) return false;
			return true;
		}

		void OnBeforeStartProject()
		{
			if (BeforeStartProject != null) {
				BeforeStartProject(this, null);
			}
		}

		void OnEndBuild (IProgressMonitor monitor, bool success)
		{
			if (EndBuild != null) {
				EndBuild (this, new BuildEventArgs (monitor, success));
			}
		}

		void IdeAppWorkspaceItemUnloading (object sender, ItemUnloadingEventArgs args)
		{
			if (IsBuilding (args.Item))
				CurrentBuildOperation.Cancel ();
			if (IsRunning (args.Item)) {
				if (MessageService.Confirm (GettextCatalog.GetString ("The project '{0}' is currently running. It will have to be stopped. Do you want to continue?", currentRunOperationOwner.Name), AlertButton.Yes)) {
					CurrentRunOperation.Cancel ();
				} else
					args.Cancel = true;
			}
		}
		
		void OnWorkspaceItemUnloaded (object s, WorkspaceItemEventArgs args)
		{
			if (ContainsTarget (args.Item, currentSolutionItem))
				CurrentSelectedSolutionItem = null;
			if (ContainsTarget (args.Item, currentWorkspaceItem))
				CurrentSelectedWorkspaceItem = null;
			if ((currentItem is IBuildTarget) && ContainsTarget (args.Item, ((IBuildTarget)currentItem)))
				CurrentSelectedItem = null;
		}
		
		protected virtual void OnCurrentSelectedSolutionChanged(SolutionEventArgs e)
		{
			if (CurrentSelectedSolutionChanged != null) {
				CurrentSelectedSolutionChanged (this, e);
			}
		}
		
		protected virtual void OnCurrentProjectChanged(ProjectEventArgs e)
		{
			if (CurrentSelectedProject != null) {
				StringParserService.Properties["PROJECTNAME"] = CurrentSelectedProject.Name;
			}
			if (CurrentProjectChanged != null) {
				CurrentProjectChanged (this, e);
			}
		}
		
		public event BuildEventHandler StartBuild;
		public event BuildEventHandler EndBuild;
		public event EventHandler BeforeStartProject;
		
		public event EventHandler<SolutionEventArgs> CurrentSelectedSolutionChanged;
		public event ProjectEventHandler CurrentProjectChanged;
		
		// Fired just before an entry is added to a combine
		public event AddEntryEventHandler AddingEntryToCombine;
	}
	
	class ParseProgressMonitorFactory: IProgressMonitorFactory
	{
		public IProgressMonitor CreateProgressMonitor ()
		{
			return new BackgroundProgressMonitor (GettextCatalog.GetString ("Code completion database generation"), "md-parser");
		}
	}
	
	class OpenDocumentFileProvider: ITextFileProvider
	{
		public IEditableTextFile GetEditableTextFile (FilePath filePath)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.FileName == filePath) {
					IEditableTextFile ef = doc.GetContent<IEditableTextFile> ();
					if (ef != null) return ef;
				}
			}
			return null;
		}
	}
}
