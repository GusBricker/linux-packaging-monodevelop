/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Errors;
using NGit.Submodule;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>A class used to execute a submodule init command.</summary>
	/// <remarks>
	/// A class used to execute a submodule init command.
	/// This will copy the 'url' and 'update' fields from the working tree
	/// .gitmodules file to a repository's config file for each submodule not
	/// currently present in the repository's config file.
	/// </remarks>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-submodule.html"
	/// *      >Git documentation about submodules</a></seealso>
	public class SubmoduleInitCommand : GitCommand<ICollection<string>>
	{
		private readonly ICollection<string> paths;

		/// <param name="repo"></param>
		protected internal SubmoduleInitCommand(Repository repo) : base(repo)
		{
			paths = new AList<string>();
		}

		/// <summary>Add repository-relative submodule path to initialize</summary>
		/// <param name="path"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.SubmoduleInitCommand AddPath(string path)
		{
			paths.AddItem(path);
			return this;
		}

		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override ICollection<string> Call()
		{
			CheckCallable();
			try
			{
				SubmoduleWalk generator = SubmoduleWalk.ForIndex(repo);
				if (!paths.IsEmpty())
				{
					generator.SetFilter(PathFilterGroup.CreateFromStrings(paths));
				}
				StoredConfig config = repo.GetConfig();
				IList<string> initialized = new AList<string>();
				while (generator.Next())
				{
					// Ignore entry if URL is already present in config file
					if (generator.GetConfigUrl() != null)
					{
						continue;
					}
					string path = generator.GetPath();
					// Copy 'url' and 'update' fields from .gitmodules to config
					// file
					string url = generator.GetRemoteUrl();
					string update = generator.GetModulesUpdate();
					if (url != null)
					{
						config.SetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants.
							CONFIG_KEY_URL, url);
					}
					if (update != null)
					{
						config.SetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants.
							CONFIG_KEY_UPDATE, update);
					}
					if (url != null || update != null)
					{
						initialized.AddItem(path);
					}
				}
				// Save repository config if any values were updated
				if (!initialized.IsEmpty())
				{
					config.Save();
				}
				return initialized;
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			catch (ConfigInvalidException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
		}
	}
}
