// 
// WorkspaceItem.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects
{
	public abstract class WorkspaceItem : IBuildTarget, IWorkspaceFileObject, ILoadController
	{
		Workspace parentWorkspace;
		FileFormat format;
		Hashtable extendedProperties;
		FilePath fileName;
		int loading;
		PropertyBag userProperties;
		FileStatusTracker<WorkspaceItemEventArgs> fileStatusTracker;
		
		[ProjectPathItemProperty ("BaseDirectory", DefaultValue=null)]
		FilePath baseDirectory;
		
		public Workspace ParentWorkspace {
			get { return parentWorkspace; }
			internal set { parentWorkspace = value; }
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
		}
		
		// User properties are only loaded when the project is loaded in the IDE.
		public virtual PropertyBag UserProperties {
			get {
				if (userProperties == null)
					userProperties = new PropertyBag ();
				return userProperties; 
			}
		}
		
		// Initializes the user properties of the item
		public virtual void LoadUserProperties (PropertyBag properties)
		{
			if (userProperties != null)
				throw new InvalidOperationException ("User properties already loaded.");
			userProperties = properties;
		}
		
		public virtual string Name {
			get {
				if (fileName.IsNullOrEmpty)
					return string.Empty;
				else
					return fileName.FileNameWithoutExtension;
			}
			set {
				if (fileName.IsNullOrEmpty)
					SetLocation (FilePath.Empty, value);
				else {
					FilePath dir = fileName.ParentDirectory;
					string ext = fileName.Extension;
					FileName = dir.Combine (value) + ext;
				}
			}
		}
		
		public virtual FilePath FileName {
			get {
				return fileName;
			}
			set {
				string oldName = Name;
				fileName = value;
				if (FileFormat != null)
					fileName = FileFormat.GetValidFileName (this, fileName);
				if (oldName != Name)
					OnNameChanged (new WorkspaceItemRenamedEventArgs (this, oldName, Name));
				NotifyModified ();
			}
		}

		public void SetLocation (FilePath baseDirectory, string name)
		{
			// Add a dummy extension to the file name.
			// It will be replaced by the correct one, depending on the format
			FileName = baseDirectory.Combine (name) + ".x";
		}
		
		public FilePath BaseDirectory {
			get {
				if (baseDirectory.IsNull)
					return FileName.ParentDirectory.FullPath;
				else
					return baseDirectory;
			}
			set {
				if (!value.IsNull && !FileName.IsNull && FileName.ParentDirectory.FullPath == value.FullPath)
					baseDirectory = null;
				else if (value.IsNullOrEmpty)
					baseDirectory = null;
				else
					baseDirectory = value.FullPath;
				NotifyModified ();
			}
		}
		
		public FilePath ItemDirectory {
			get { return FileName.ParentDirectory.FullPath; }
		}
		
		protected bool Loading {
			get { return loading > 0; }
		}
		
		public WorkspaceItem ()
		{
			MonoDevelop.Projects.Extensions.ProjectExtensionUtil.LoadControl (this);
			fileStatusTracker = new FileStatusTracker<WorkspaceItemEventArgs> (this, OnReloadRequired, new WorkspaceItemEventArgs (this));
		}

		public virtual List<FilePath> GetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> col = FileFormat.Format.GetItemFiles (this);
			if (!string.IsNullOrEmpty (FileName) && !col.Contains (FileName))
				col.Add (FileName);
			return col;
		}
		
		public virtual SolutionEntityItem FindSolutionItem (string fileName)
		{
			return null;
		}
		
		public virtual bool ContainsItem (IWorkspaceObject obj)
		{
			return this == obj;
		}
		
		public ReadOnlyCollection<SolutionItem> GetAllSolutionItems ()
		{
			return GetAllSolutionItems<SolutionItem> ();
		}
		
		public virtual ReadOnlyCollection<T> GetAllSolutionItems<T> () where T: SolutionItem
		{
			return new List<T> ().AsReadOnly ();
		}
		
		public ReadOnlyCollection<Project> GetAllProjects ()
		{
			return GetAllSolutionItems<Project> ();
		}
		
		public virtual ReadOnlyCollection<Solution> GetAllSolutions ()
		{
			return GetAllItems<Solution> ();
		}
		
		public ReadOnlyCollection<WorkspaceItem> GetAllItems ()
		{
			return GetAllItems<WorkspaceItem> ();
		}
		
		public virtual ReadOnlyCollection<T> GetAllItems<T> () where T: WorkspaceItem
		{
			List<T> list = new List<T> ();
			if (this is T)
				list.Add ((T)this);
			return list.AsReadOnly ();
		}

		public virtual Project GetProjectContainingFile (FilePath fileName)
		{
			return null;
		}
		
		public virtual ReadOnlyCollection<string> GetConfigurations ()
		{
			return new ReadOnlyCollection<string> (new string [0]);
		}
		
		protected internal virtual void OnSave (IProgressMonitor monitor)
		{
			Services.ProjectService.InternalWriteWorkspaceItem (monitor, FileName, this);
		}
		
		internal void SetParentWorkspace (Workspace workspace)
		{
			parentWorkspace = workspace;
		}
		
		public BuildResult RunTarget (IProgressMonitor monitor, string target, string configuration)
		{
			return RunTarget (monitor, target, (SolutionConfigurationSelector) configuration);
		}
		
		public BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			return Services.ProjectService.ExtensionChain.RunTarget (monitor, this, target, configuration);
		}
		
		public void Clean (IProgressMonitor monitor, string configuration)
		{
			Clean (monitor, (SolutionConfigurationSelector) configuration);
		}
		
		public void Clean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			Services.ProjectService.ExtensionChain.RunTarget (monitor, this, ProjectService.CleanTarget, configuration);
		}
		
		public BuildResult Build (IProgressMonitor monitor, string configuration)
		{
			return InternalBuild (monitor, (SolutionConfigurationSelector) configuration);
		}
		
		public BuildResult Build (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return InternalBuild (monitor, configuration);
		}
		
		public void Execute (IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
			Execute (monitor, context, (SolutionConfigurationSelector) configuration);
		}
		
		public void Execute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			Services.ProjectService.ExtensionChain.Execute (monitor, this, context, configuration);
		}
		
		public bool CanExecute (ExecutionContext context, string configuration)
		{
			return CanExecute (context, (SolutionConfigurationSelector) configuration);
		}
		
		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return Services.ProjectService.ExtensionChain.CanExecute (this, context, configuration);
		}
		
		public bool NeedsBuilding (string configuration)
		{
			return NeedsBuilding ((SolutionConfigurationSelector) configuration);
		}
		
		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return Services.ProjectService.ExtensionChain.GetNeedsBuilding (this, configuration);
		}
		
		public void SetNeedsBuilding (bool value)
		{
			foreach (string conf in GetConfigurations ())
				SetNeedsBuilding (value, new SolutionConfigurationSelector (conf));
		}
		
		public void SetNeedsBuilding (bool needsBuilding, string configuration)
		{
			SetNeedsBuilding (needsBuilding, (SolutionConfigurationSelector) configuration);
		}
		
		public void SetNeedsBuilding (bool needsBuilding, ConfigurationSelector configuration)
		{
			Services.ProjectService.ExtensionChain.SetNeedsBuilding (this, needsBuilding, configuration);
		}
		
		public virtual FileFormat FileFormat {
			get {
				if (format == null) {
					format = Services.ProjectService.GetDefaultFormat (this);
				}
				return format;
			}
		}
		
		public virtual void ConvertToFormat (FileFormat format, bool convertChildren)
		{
			this.format = format;
			if (!string.IsNullOrEmpty (FileName))
				FileName = format.GetValidFileName (this, FileName);
		}
		
		internal virtual BuildResult InternalBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return Services.ProjectService.ExtensionChain.RunTarget (monitor, this, ProjectService.BuildTarget, configuration);
		}
		
		protected virtual void OnConfigurationsChanged ()
		{
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, EventArgs.Empty);
			if (ParentWorkspace != null)
				ParentWorkspace.OnConfigurationsChanged ();
		}

		public void Save (FilePath fileName, IProgressMonitor monitor)
		{
			FileName = fileName;
			Save (monitor);
		}
		
		public void Save (IProgressMonitor monitor)
		{
			try {
				fileStatusTracker.BeginSave ();
				Services.ProjectService.ExtensionChain.Save (monitor, this);
				OnSaved (new WorkspaceItemEventArgs (this));
				
				// Update save times
			} finally {
				fileStatusTracker.EndSave ();
			}
			FileService.NotifyFileChanged (FileName);
		}
		
		public virtual bool NeedsReload {
			get {
				return fileStatusTracker.NeedsReload;
			}
			set {
				fileStatusTracker.NeedsReload = value;
			}
		}
		
		public virtual bool ItemFilesChanged {
			get {
				return fileStatusTracker.ItemFilesChanged;
			}
		}

		internal protected virtual BuildResult OnRunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (target == ProjectService.BuildTarget)
				return OnBuild (monitor, configuration);
			else if (target == ProjectService.CleanTarget) {
				OnClean (monitor, configuration);
				return null;
			}
			return null;
		}
		
		protected virtual void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
		}
		
		protected virtual BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return null;
		}
		
		internal protected virtual void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
		}
		
		internal protected virtual bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return true;
		}
		
		internal protected virtual bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			return false;
		}
		
		internal protected virtual void OnSetNeedsBuilding (bool val, ConfigurationSelector configuration)
		{
		}
		
		void ILoadController.BeginLoad ()
		{
			loading++;
			OnBeginLoad ();
		}
		
		void ILoadController.EndLoad ()
		{
			loading--;
			fileStatusTracker.ResetLoadTimes ();
			OnEndLoad ();
		}
		
		protected virtual void OnBeginLoad ()
		{
		}
		
		protected virtual void OnEndLoad ()
		{
		}

		public FilePath GetAbsoluteChildPath (FilePath relPath)
		{
			return relPath.ToAbsolute (BaseDirectory);
		}

		public FilePath GetRelativeChildPath (FilePath absPath)
		{
			return absPath.ToRelative (BaseDirectory);
		}
		
		public virtual void Dispose()
		{
			if (extendedProperties != null) {
				foreach (object ob in extendedProperties.Values) {
					IDisposable disp = ob as IDisposable;
					if (disp != null)
						disp.Dispose ();
				}
			}
			if (userProperties != null)
				((IDisposable)userProperties).Dispose ();
		}
		
		protected virtual void OnNameChanged (WorkspaceItemRenamedEventArgs e)
		{
			NotifyModified ();
			if (NameChanged != null)
				NameChanged (this, e);
		}
		
		protected void NotifyModified ()
		{
			OnModified (new WorkspaceItemEventArgs (this));
		}
		
		protected virtual void OnModified (WorkspaceItemEventArgs args)
		{
			if (Modified != null)
				Modified (this, args);
		}
		
		protected virtual void OnSaved (WorkspaceItemEventArgs args)
		{
			if (Saved != null)
				Saved (this, args);
		}
		
		protected virtual void OnReloadRequired (WorkspaceItemEventArgs args)
		{
			fileStatusTracker.FireReloadRequired (args);
		}
		
		public event EventHandler ConfigurationsChanged;
		public event EventHandler<WorkspaceItemRenamedEventArgs> NameChanged;
		public event EventHandler<WorkspaceItemEventArgs> Modified;
		public event EventHandler<WorkspaceItemEventArgs> Saved;
		
/*		public event EventHandler<WorkspaceItemEventArgs> ReloadRequired {
			add {
				fileStatusTracker.ReloadRequired += value;
			}
			remove {
				fileStatusTracker.ReloadRequired -= value;
			}
		}*/
	}
	
	class FileStatusTracker<TEventArgs> where TEventArgs:EventArgs
	{
		Dictionary<string,DateTime> lastSaveTime;
		Dictionary<string,DateTime> reloadCheckTime;
		bool savingFlag;
//		bool needsReload;
//		List<FileSystemWatcher> watchers;
//		Action<TEventArgs> onReloadRequired;
		TEventArgs eventArgs;
		EventHandler<TEventArgs> reloadRequired;
		
		IWorkspaceFileObject item;
		
		public FileStatusTracker (IWorkspaceFileObject item, Action<TEventArgs> onReloadRequired, TEventArgs eventArgs)
		{
			this.item = item;
			this.eventArgs = eventArgs;
//			this.onReloadRequired = onReloadRequired;
			lastSaveTime = new Dictionary<string,DateTime> ();
			reloadCheckTime = new Dictionary<string,DateTime> ();
			savingFlag = false;
			reloadRequired = null;
		}
		
		public void BeginSave ()
		{
			savingFlag = true;
//			needsReload = false;
			DisposeWatchers ();
		}
		
		public void EndSave ()
		{
			ResetLoadTimes ();
			savingFlag = false;
		}
		
		public void ResetLoadTimes ()
		{
			lastSaveTime.Clear ();
			reloadCheckTime.Clear ();
			foreach (FilePath file in item.GetItemFiles (false))
				lastSaveTime [file] = reloadCheckTime [file] = GetLastWriteTime (file);
//			needsReload = false;
			if (reloadRequired != null)
				InternalNeedsReload ();
		}
		
		public bool NeedsReload {
			get {
				if (savingFlag)
					return false;
				return InternalNeedsReload ();
			}
			set {
//				needsReload = value;
				reloadCheckTime.Clear ();
				foreach (FilePath file in item.GetItemFiles (false)) {
					if (value)
						reloadCheckTime [file] = DateTime.MinValue;
					else
						reloadCheckTime [file] = GetLastWriteTime (file);
				}
			}
		}
		
		public bool ItemFilesChanged {
			get {
				if (savingFlag)
					return false;
				foreach (FilePath file in item.GetItemFiles (false))
					if (GetLastSaveTime (file) != GetLastWriteTime (file))
						return true;
				return false;
			}
		}
		
		bool InternalNeedsReload ()
		{
			foreach (FilePath file in item.GetItemFiles (false)) {
				if (GetLastReloadCheckTime (file) != GetLastWriteTime (file))
					return true;
			}
			return false;
/*			
			if (needsReload)
				return true;
			
			// Watchers already set? if so, then since needsReload==false, no change has
			// happened so far
			if (watchers != null)
				return false;
		
			// Handlers are not set up. Do the check now, and set the handlers.
			watchers = new List<FileSystemWatcher> ();
			foreach (FilePath file in item.GetItemFiles (false)) {
				FileSystemWatcher w = new FileSystemWatcher (file.ParentDirectory, file.FileName);
				w.IncludeSubdirectories = false;
				w.Changed += HandleFileChanged;
				w.EnableRaisingEvents = true;
				watchers.Add (w);
				if (GetLastReloadCheckTime (file) != GetLastWriteTime (file))
					needsReload = true;
			}
			return needsReload;
			*/
		}
		
		void DisposeWatchers ()
		{
/*			if (watchers == null)
				return;
			foreach (FileSystemWatcher w in watchers)
				w.Dispose ();
			watchers = null;
*/		}

/*		void HandleFileChanged (object sender, FileSystemEventArgs e)
		{
			if (!savingFlag && !needsReload) {
				needsReload = true;
				onReloadRequired (eventArgs);
			}
		}
*/
		DateTime GetLastWriteTime (FilePath file)
		{
			try {
				if (!file.IsNullOrEmpty && File.Exists (file))
					return File.GetLastWriteTime (file);
			} catch {
			}
			return GetLastSaveTime (file);
		}

		DateTime GetLastSaveTime (FilePath file)
		{
			DateTime dt;
			if (lastSaveTime.TryGetValue (file, out dt))
				return dt;
			else
				return DateTime.MinValue;
		}

		DateTime GetLastReloadCheckTime (FilePath file)
		{
			DateTime dt;
			if (reloadCheckTime.TryGetValue (file, out dt))
				return dt;
			else
				return DateTime.MinValue;
		}
		
		public event EventHandler<TEventArgs> ReloadRequired {
			add {
				reloadRequired += value;
				if (InternalNeedsReload ())
					value (this, eventArgs);
			}
			remove {
				reloadRequired -= value;
			}
		}
		
		public void FireReloadRequired (TEventArgs args)
		{
			if (reloadRequired != null)
				reloadRequired (this, args);
		}
	}
}
