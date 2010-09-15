//  Project.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2009 RemObjects Software
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MonoDevelop;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom.Output;


namespace MonoDevelop.Projects
{
	public enum NewFileSearch
	{
		None,
		OnLoad,
		OnLoadAutoInsert
	}

	[DataInclude(typeof(ProjectFile))]
	[DataItem(FallbackType = typeof(UnknownProject))]
	public abstract class Project : SolutionEntityItem
	{
		string[] buildActions;
		bool isDirty;

		public Project ()
		{
			FileService.FileChanged += OnFileChanged;
			files = new ProjectFileCollection ();
			Items.Bind (files);
			DependencyResolutionEnabled = true;
		}

		[ItemProperty("Description", DefaultValue = "")]
		private string description = "";
		public string Description {
			get { return description; }
			set {
				description = value;
				NotifyModified ("Description");
			}
		}

		public virtual bool IsCompileable (string fileName)
		{
			return false;
		}

		private ProjectFileCollection files;
		public ProjectFileCollection Files {
			get { return files; }
		}

		[ItemProperty("newfilesearch", DefaultValue = NewFileSearch.None)]
		protected NewFileSearch newFileSearch = NewFileSearch.None;
		public NewFileSearch NewFileSearch {
			get { return newFileSearch; }

			set {
				newFileSearch = value;
				NotifyModified ("NewFileSearch");
			}
		}

		public abstract string ProjectType {
			get;
		}

		IconId stockIcon = "md-project";
		public virtual IconId StockIcon {
			get { return stockIcon; }
			set { this.stockIcon = value; }
		}

		public virtual Ambience Ambience {
			get { return new NetAmbience (); }
		}

		public virtual string[] SupportedLanguages {
			get { return new String[] { "" }; }
		}

		public virtual string GetDefaultBuildAction (string fileName)
		{
			return IsCompileable (fileName) ? BuildAction.Compile : BuildAction.None;
		}

		public ProjectFile GetProjectFile (string fileName)
		{
			return files.GetFile (fileName);
		}

		public bool IsFileInProject (string fileName)
		{
			return files.GetFile (fileName) != null;
		}

		//NOTE: groups the common actions at the top, separated by a "--" entry *IF* there are 
		// more "uncommon" actions than "common" actions
		public string[] GetBuildActions ()
		{
			if (buildActions != null)
				return buildActions;

			// find all the actions in use and add them to the list of standard actions
			Hashtable actions = new Hashtable ();
			object marker = new object (); //avoid using bools as they need to be boxed. re-use single object instead
			//ad the standard actions
			foreach (string action in GetStandardBuildActions ())
				actions[action] = marker;

			//add any more actions that are in the project file
			foreach (ProjectFile pf in files)
				if (!actions.ContainsKey (pf.BuildAction))
					actions[pf.BuildAction] = marker;

			//remove the "common" actions, since they're handled separately
			IList<string> commonActions = GetCommonBuildActions ();
			foreach (string action in commonActions)
				if (actions.Contains (action))
					actions.Remove (action);

			//calculate dimensions for our new array and create it
			int dashPos = commonActions.Count;
			bool hasDash = commonActions.Count > 0 && actions.Count > 0;
			int arrayLen = commonActions.Count + actions.Count;
			int uncommonStart = hasDash ? dashPos + 1 : dashPos;
			if (hasDash)
				arrayLen++;
			buildActions = new string[arrayLen];

			//populate it
			if (commonActions.Count > 0)
				commonActions.CopyTo (buildActions, 0);
			if (hasDash)
				buildActions[dashPos] = "--";
			if (actions.Count > 0)
				actions.Keys.CopyTo (buildActions, uncommonStart);

			//sort the actions
			if (hasDash) {
				//it may be better to leave common actions in the order that the project specified
				//Array.Sort (buildActions, 0, commonActions.Count, StringComparer.Ordinal);
				Array.Sort (buildActions, uncommonStart, arrayLen - uncommonStart, StringComparer.Ordinal);
			} else {
				Array.Sort (buildActions, StringComparer.Ordinal);
			}
			return buildActions;
		}

		protected virtual IEnumerable<string> GetStandardBuildActions ()
		{
			return BuildAction.StandardActions;
		}

		protected virtual IList<string> GetCommonBuildActions ()
		{
			return BuildAction.StandardActions;
		}

		public static Project LoadProject (string filename, IProgressMonitor monitor)
		{
			Project prj = Services.ProjectService.ReadSolutionItem (monitor, filename) as Project;
			if (prj == null)
				throw new InvalidOperationException ("Invalid project file: " + filename);

			return prj;
		}


		public override void Dispose ()
		{
			FileService.FileChanged -= OnFileChanged;
			foreach (ProjectFile file in Files) {
				file.Dispose ();
			}
			base.Dispose ();
		}

		public ProjectFile AddFile (string filename)
		{
			return AddFile (filename, null);
		}

		public ProjectFile AddFile (string filename, string buildAction)
		{
			foreach (ProjectFile fInfo in Files) {
				if (fInfo.Name == filename) {
					return fInfo;
				}
			}

			if (String.IsNullOrEmpty (buildAction)) {
				buildAction = GetDefaultBuildAction (filename);
			}

			ProjectFile newFileInformation = new ProjectFile (filename, buildAction);
			Files.Add (newFileInformation);
			return newFileInformation;
		}

		public void AddFile (ProjectFile projectFile)
		{
			Files.Add (projectFile);
		}

		public ProjectFile AddDirectory (string relativePath)
		{
			string newPath = Path.Combine (BaseDirectory, relativePath);

			foreach (ProjectFile fInfo in Files)
				if (fInfo.Name == newPath && fInfo.Subtype == Subtype.Directory)
					return fInfo;

			if (!Directory.Exists (newPath)) {
				if (File.Exists (newPath)) {
					string message = GettextCatalog.GetString ("Cannot create directory {0}, as a file with that name exists.", newPath);
					throw new InvalidOperationException (message);
				}
				FileService.CreateDirectory (newPath);
			}

			ProjectFile newDir = new ProjectFile (newPath);
			newDir.Subtype = Subtype.Directory;
			AddFile (newDir);
			return newDir;
		}

		protected internal override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			// create output directory, if not exists
			ProjectConfiguration conf = GetConfiguration (configuration) as ProjectConfiguration;
			if (conf == null) {
				BuildResult cres = new BuildResult ();
				cres.AddError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration.ToString (), Name));
				return cres;
			}
			string outputDir = conf.OutputDirectory;
			try {
				DirectoryInfo directoryInfo = new DirectoryInfo (outputDir);
				if (!directoryInfo.Exists) {
					directoryInfo.Create ();
				}
			} catch (Exception e) {
				throw new ApplicationException ("Can't create project output directory " + outputDir + " original exception:\n" + e.ToString ());
			}

			//copy references and files marked to "CopyToOutputDirectory"
			CopySupportFiles (monitor, configuration);

			StringParserService.Properties["Project"] = Name;

			monitor.BeginTask (GettextCatalog.GetString ("Performing main compilation..."), 0);
			BuildResult res = DoBuild (monitor, configuration);

			isDirty = false;

			if (res != null) {
				string errorString = GettextCatalog.GetPluralString ("{0} error", "{0} errors", res.ErrorCount, res.ErrorCount);
				string warningString = GettextCatalog.GetPluralString ("{0} warning", "{0} warnings", res.WarningCount, res.WarningCount);

				monitor.Log.WriteLine (GettextCatalog.GetString ("Build complete -- ") + errorString + ", " + warningString);
			}
			monitor.EndTask ();

			return res;
		}

		public void CopySupportFiles (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = (ProjectConfiguration) GetConfiguration (configuration);

			foreach (FileCopySet.Item item in GetSupportFileList (configuration)) {
				string dest = Path.GetFullPath (Path.Combine (config.OutputDirectory, item.Target));
				string src = Path.GetFullPath (item.Src);

				try {
					if (dest == src)
						continue;

					if (item.CopyOnlyIfNewer && File.Exists (dest) && (File.GetLastWriteTimeUtc (dest) >= File.GetLastWriteTimeUtc (src)))
						continue;

					if (!Directory.Exists (Path.GetDirectoryName (dest)))
						FileService.CreateDirectory (Path.GetDirectoryName (dest));

					if (File.Exists (src))
						FileService.CopyFile (src, dest);
					else
						monitor.ReportError (GettextCatalog.GetString ("Could not find support file '{0}'.", src), null);

				} catch (IOException ex) {
					monitor.ReportError (GettextCatalog.GetString ("Error copying support file '{0}'.", dest), ex);
				}
			}
		}

		public void DeleteSupportFiles (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = (ProjectConfiguration) GetConfiguration (configuration);

			foreach (FileCopySet.Item item in GetSupportFileList (configuration)) {
				string dest = Path.Combine (config.OutputDirectory, item.Target);

				// Ignore files which were not copied
				if (Path.GetFullPath (dest) == Path.GetFullPath (item.Src))
					continue;

				try {
					if (File.Exists (dest)) {
						FileService.DeleteFile (dest);
					}
				} catch (IOException ex) {
					monitor.ReportError (GettextCatalog.GetString ("Error deleting support file '{0}'.", dest), ex);
				}
			}
		}

		public FileCopySet GetSupportFileList (ConfigurationSelector configuration)
		{
			FileCopySet list = new FileCopySet ();
			PopulateSupportFileList (list, configuration);
			return list;
		}

		protected virtual void PopulateSupportFileList (FileCopySet list, ConfigurationSelector configuration)
		{
			foreach (ProjectFile pf in Files) {
				if (pf.CopyToOutputDirectory == FileCopyMode.None)
					continue;
				list.Add (pf.FilePath, pf.CopyToOutputDirectory == FileCopyMode.PreserveNewest, pf.ProjectVirtualPath);
			}
		}

		protected virtual BuildResult DoBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			BuildResult res = ItemHandler.RunTarget (monitor, "Build", configuration);
			return res ?? new BuildResult ();
		}

		protected internal override void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			SetDirty ();
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null) {
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", config.Id, Name), null);
				return;
			}

			// Delete the generated assembly
			string file = GetOutputFileName (configuration);
			if (file != null) {
				if (File.Exists (file))
					FileService.DeleteFile (file);
			}

			DeleteSupportFiles (monitor, configuration);

			DoClean (monitor, config.Selector);
		}

		protected virtual void DoClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			ItemHandler.RunTarget (monitor, "Clean", configuration);
		}

		void GetBuildableReferencedItems (List<SolutionItem> referenced, SolutionItem item, ConfigurationSelector configuration)
		{
			if (referenced.Contains (item))
				return;

			if (item.NeedsBuilding (configuration))
				referenced.Add (item);

			foreach (SolutionItem ritem in item.GetReferencedItems (configuration))
				GetBuildableReferencedItems (referenced, ritem, configuration);
		}

		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			ProjectConfiguration config = GetConfiguration (configuration) as ProjectConfiguration;
			if (config == null) {
				monitor.ReportError (GettextCatalog.GetString ("Configuration '{0}' not found in project '{1}'", configuration, Name), null);
				return;
			}
			DoExecute (monitor, context, configuration);
		}

		protected virtual void DoExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
		}

		public virtual FilePath GetOutputFileName (ConfigurationSelector configuration)
		{
			return FilePath.Null;
		}

		protected internal override bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			if (!isDirty) {
				if (CheckNeedsBuild (configuration))
					SetDirty ();
			}
			return isDirty;
		}

		protected internal override void OnSetNeedsBuilding (bool value, ConfigurationSelector configuration)
		{
			isDirty = value;
		}

		void SetDirty ()
		{
			if (!Loading)
				isDirty = true;
		}

		protected virtual bool CheckNeedsBuild (ConfigurationSelector configuration)
		{
			DateTime tim = GetLastBuildTime (configuration);
			if (tim == DateTime.MinValue)
				return true;

			foreach (ProjectFile file in Files) {
				if (file.BuildAction == BuildAction.Content || file.BuildAction == BuildAction.None)
					continue;
				try {
					if (File.GetLastWriteTime (file.FilePath) > tim)
						return true;
				} catch (IOException) {
					// Ignore.
				}
			}

			foreach (SolutionItem pref in GetReferencedItems (configuration)) {
				if (pref.GetLastBuildTime (configuration) > tim || pref.NeedsBuilding (configuration))
					return true;
			}

			try {
				if (File.GetLastWriteTime (FileName) > tim)
					return true;
			} catch {
				// Ignore
			}

			return false;
		}

		protected internal override DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			string file = GetOutputFileName (configuration);
			if (file == null)
				return DateTime.MinValue;

			FileInfo finfo = new FileInfo (file);
			if (!finfo.Exists)
				return DateTime.MinValue;
			else
				return finfo.LastWriteTime;
		}

		internal virtual void OnFileChanged (object source, FileEventArgs e)
		{
			ProjectFile file = GetProjectFile (e.FileName);
			if (file != null) {
				SetDirty ();
				try {
					NotifyFileChangedInProject (file);
				} catch {
					// Workaround Mono bug. The watcher seems to
					// stop watching if an exception is thrown in
					// the event handler
				}
			}

		}

		protected internal override List<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> col = base.OnGetItemFiles (includeReferencedFiles);
			if (includeReferencedFiles) {
				foreach (ProjectFile pf in Files) {
					if (pf.Subtype != Subtype.Directory)
						col.Add (pf.FilePath);
				}
			}
			return col;
		}

		protected internal override void OnItemAdded (object obj)
		{
			base.OnItemAdded (obj);
			if (obj is ProjectFile)
				NotifyFileAddedToProject ((ProjectFile)obj);
		}

		protected internal override void OnItemRemoved (object obj)
		{
			base.OnItemRemoved (obj);
			if (obj is ProjectFile)
				NotifyFileRemovedFromProject ((ProjectFile)obj);
		}

		internal void NotifyFileChangedInProject (ProjectFile file)
		{
			OnFileChangedInProject (new ProjectFileEventArgs (this, file));
		}

		internal void NotifyFilePropertyChangedInProject (ProjectFile file)
		{
			NotifyModified ("Files");
			OnFilePropertyChangedInProject (new ProjectFileEventArgs (this, file));
		}

		List<ProjectFile> unresolvedDeps;

		void NotifyFileRemovedFromProject (ProjectFile file)
		{
			file.SetProject (null);

			if (DependencyResolutionEnabled) {
				if (unresolvedDeps.Contains (file))
					unresolvedDeps.Remove (file);
				foreach (ProjectFile f in file.DependentChildren) {
					f.DependsOnFile = null;
					if (!string.IsNullOrEmpty (f.DependsOn))
						unresolvedDeps.Add (f);
				}
				file.DependsOnFile = null;
			}

			SetDirty ();
			NotifyModified ("Files");
			OnFileRemovedFromProject (new ProjectFileEventArgs (this, file));
		}

		void NotifyFileAddedToProject (ProjectFile file)
		{
			if (file.Project != null)
				throw new InvalidOperationException ("ProjectFile already belongs to a project");
			file.SetProject (this);

			ResolveDependencies (file);

			SetDirty ();
			NotifyModified ("Files");
			OnFileAddedToProject (new ProjectFileEventArgs (this, file));
		}

		internal void ResolveDependencies (ProjectFile file)
		{
			if (!DependencyResolutionEnabled)
				return;

			if (!file.ResolveParent ())
				unresolvedDeps.Add (file);

			List<ProjectFile> resolved = null;
			foreach (ProjectFile unres in unresolvedDeps) {
				if (string.IsNullOrEmpty (unres.DependsOn)) {
					resolved.Add (unres);
				}
				if (unres.ResolveParent ()) {
					if (resolved == null)
						resolved = new List<ProjectFile> ();
					resolved.Add (unres);
				}
			}
			if (resolved != null)
				foreach (ProjectFile pf in resolved)
					unresolvedDeps.Remove (pf);
		}

		bool DependencyResolutionEnabled {

			get { return unresolvedDeps != null; }
			set {
				if (value) {
					if (unresolvedDeps != null)
						return;
					unresolvedDeps = new List<ProjectFile> ();
					foreach (ProjectFile file in files)
						ResolveDependencies (file);
				} else {
					unresolvedDeps = null;
				}
			}
		}

		internal void NotifyFileRenamedInProject (ProjectFileRenamedEventArgs args)
		{
			SetDirty ();
			NotifyModified ("Files");
			OnFileRenamedInProject (args);
		}

		protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject (this, e);
			}
		}

		protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}

		protected virtual void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}

		protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			buildActions = null;
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
			}
		}

		protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}

		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;


	}

	public class UnknownProject : Project
	{
		public override string ProjectType {
			get { return ""; }
		}

		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			return null;
		}
		
		internal protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return false;
		}
		
		internal protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			BuildResult res = new BuildResult ();
			res.AddError ("Unknown project type");
			return res;
		}
	}

	public delegate void ProjectEventHandler (Object sender, ProjectEventArgs e);
	public class ProjectEventArgs : EventArgs
	{
		public ProjectEventArgs (Project project)
		{
			this.project = project;
		}

		private Project project;
		public Project Project {
			get { return project; }
		}
	}
}
