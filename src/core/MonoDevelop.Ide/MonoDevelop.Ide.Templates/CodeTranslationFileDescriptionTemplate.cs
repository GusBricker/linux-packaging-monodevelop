//
// CodeTranslationFileDescriptionTemplate.cs: Template that translates .NET 
// 		code using CodeDom.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Xml;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	
	public class CodeTranslationFileDescriptionTemplate : SingleFileDescriptionTemplate
	{
		string content;
		CodeDomProvider parserProvider;
		string tempSubstitutedContent;
		bool showAutogenerationNotice = false;
		string sourceLang;
		
		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			base.Load (filenode, baseDirectory);
			content = filenode.InnerText;
			
			sourceLang = filenode.GetAttribute ("SourceLanguage");
			if ((sourceLang == null) || (sourceLang.Length == 0))
				sourceLang = "C#";
			
			string showAutogen = filenode.GetAttribute ("ShowAutogenerationNotice");
			if ((showAutogen != null) && (showAutogen.Length > 0)) {
				try {
					showAutogenerationNotice = bool.Parse (showAutogen.ToLower());
				} catch (FormatException) {
					throw new InvalidOperationException ("Invalid value for ShowAutogenerationNotice in template.");
				}
			}
			
			//this is a code template, so unless told otherwise, default to adding the standard header
			if (string.IsNullOrEmpty (filenode.GetAttribute ("AddStandardHeader")))
				AddStandardHeader = true;
			
			parserProvider = GetCodeDomProvider (sourceLang);
		}
		
		//Adapted from CodeDomFileDescriptionTemplate.cs
		//TODO: Remove need to have a namespace and type (i.e. translate fragments)
		public override string CreateContent (Project project, Dictionary<string,string> tags, string language)
		{
			//get target language's ICodeGenerator
			if (language == null || language == "")
				throw new InvalidOperationException ("Language not defined in CodeDom based template.");
			
			CodeDomProvider provider = GetCodeDomProvider (language);
			
			//parse the source code
			if (tempSubstitutedContent == null)
				throw new Exception ("Expected ModifyTags to be called before CreateContent");
			
			CodeCompileUnit ccu;
			using (StringReader sr = new StringReader (tempSubstitutedContent)) {
				try {
					ccu = parserProvider.Parse (sr);
				} catch (NotImplementedException) {
					throw new InvalidOperationException ("Invalid Code Translation template: the CodeDomProvider of the source language '"
					                                     + language + "' has not implemented the Parse method.");
				} catch (Exception ex) {
					LoggingService.LogError ("Unparseable template: '" + tempSubstitutedContent + "'.", ex);
					throw;
				}
			}
			
			foreach (CodeNamespace cns in ccu.Namespaces)
				cns.Name = CodeDomFileDescriptionTemplate.StripImplicitNamespace (project, tags, cns.Name);
			
			tempSubstitutedContent = null;
			
			//and generate the code
			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.IndentString = "\t";
			options.BracingStyle = "C";
			
			string txt = string.Empty;
			using (StringWriter sw = new StringWriter ()) {
				provider.GenerateCodeFromCompileUnit (ccu, sw, options);
				txt = sw.ToString ();
			}
			
			if (showAutogenerationNotice) return txt;
			
			//remove auto-generation notice
			int i = txt.IndexOf ("</autogenerated>");
			if (i == -1) return txt;
			i = txt.IndexOf ('\n', i);
			if (i == -1) return txt;
			i = txt.IndexOf ('\n', i + 1);
			if (i == -1) return txt;
			
			return txt.Substring (i+1);
		}
		
		public override void ModifyTags (SolutionFolderItem policyParent, Project project, string language, string identifier, string fileName, ref Dictionary<string,string> tags)
		{
			//prevent parser breakage from missing tags, which SingleFile only provides for DotNetProject
			//if ((project as DotNetProject) == null)
			//	throw new InvalidOperationException ("CodeTranslationFileDescriptionTemplate can only be used with a DotNetProject");
			
			base.ModifyTags (policyParent, project, language, identifier, fileName, ref tags);
			
			//swap out the escaped keyword identifiers for the target language with the source language
			//CodeDOM should take care of handling it for the target language
			System.CodeDom.Compiler.CodeDomProvider provider = GetCodeDomProvider (sourceLang);
			tags ["EscapedIdentifier"] = provider.CreateEscapedIdentifier ((string) tags ["Name"]);
			
			//This is a bit hacky doing it here instead of in CreateContent, but need to
			//substitute all tags in code before language is translated, because language
			//translation gets confused by unsubstituted  substitution tokens.
			tempSubstitutedContent = StringParserService.Parse (content, tags);
		}
		
		private System.CodeDom.Compiler.CodeDomProvider GetCodeDomProvider (string language)
		{
			System.CodeDom.Compiler.CodeDomProvider provider = null;
			var binding = GetLanguageBinding (language);
			if (binding == null)
				throw new InvalidOperationException ("No LanguageBinding was found for the language '" + language + "'.");
			
			provider = binding.GetCodeDomProvider ();
			if (provider == null)
				throw new InvalidOperationException ("No CodeDomProvider was found for the language '" + language + "'.");
			return provider;
		}
		
		//CodeDOM escapes keywords for us, so escaped keywords are valid too. Need to override to allow this,
		//and also to check for validity in source language
		public override bool IsValidName (string name, string language)
		{
			return base.IsValidName (name, language) && base.IsValidName (name, sourceLang);
		}
	}
}
