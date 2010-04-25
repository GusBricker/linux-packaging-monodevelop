
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	class DefaultDeployServiceExtension: DeployServiceExtension
	{
		public override DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionItem entry, ConfigurationSelector configuration)
		{
			if (entry is IDeployable)
				return new DeployFileCollection (((IDeployable)entry).GetDeployFiles (configuration));
			
			return base.GetDeployFiles (ctx, entry, configuration);
		}
		
		public override DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project, ConfigurationSelector configuration)
		{
			DeployFileCollection deployFiles = new DeployFileCollection ();
			base.GetProjectDeployFiles (ctx, project, configuration);
			// Add the compiled output file
			
			string outputFile = project.GetOutputFileName (configuration);
			if (!string.IsNullOrEmpty (outputFile))
				deployFiles.Add (new DeployFile (project, outputFile, Path.GetFileName (outputFile), TargetDirectory.ProgramFiles));
			
			// Collect deployable files
			foreach (ProjectFile file in project.Files) {
				// skip CopyToOutputDirectory files when it's just a project build, because 
				// MonoDevelop.Project.Projects already copies these files using more subtle overwriting
				// semantics
				if (file.CopyToOutputDirectory != FileCopyMode.None)
					continue;
				    
				DeployProperties props = new DeployProperties (file);
				if (props.ShouldDeploy) {
					DeployFile dp = new DeployFile (file);
					deployFiles.Add (dp);
					
					if (string.Compare (Path.GetFileName (dp.SourcePath), "app.config", true)==0 && string.Compare (Path.GetFileName (dp.RelativeTargetPath), "app.config", true)==0) {
						string newName = Path.GetFileName (outputFile) + ".config";
						dp.RelativeTargetPath = Path.Combine (Path.GetDirectoryName (dp.RelativeTargetPath), newName);
					}
				}
			}
			
			foreach (FileCopySet.Item item in project.GetSupportFileList (configuration)) {
				 deployFiles.Add (new DeployFile (project, item.Src, item.Target, TargetDirectory.ProgramFiles));
			}
			
			DotNetProject netProject = project as DotNetProject;
			if (netProject != null) {
				DotNetProjectConfiguration conf = (DotNetProjectConfiguration) project.GetConfiguration (configuration);
				if (conf.DebugMode) {
					string mdbFile = netProject.TargetRuntime.GetAssemblyDebugInfoFile (conf.CompiledOutputName);
					deployFiles.Add (new DeployFile (project, mdbFile, Path.GetFileName (mdbFile), TargetDirectory.ProgramFiles));
				}
			}
			
			return deployFiles;
		}
	}
}


