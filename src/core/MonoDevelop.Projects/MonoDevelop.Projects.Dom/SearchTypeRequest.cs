//
// IParserContext.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom
{
	public class SearchTypeRequest
	{
		string name;
		IList<IReturnType> genericParameters;
		ICompilationUnit currentCompilationUnit;
		int    caretLine, caretColumn;
		bool   caseSensitive = true;
		IType  callingType;
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public ICompilationUnit CurrentCompilationUnit {
			get {
				return currentCompilationUnit;
			}
		}
		
		public int CaretLine {
			get {
				return caretLine;
			}
		}
		
		public int CaretColumn {
			get {
				return caretColumn;
			}
		}

		public bool CaseSensitive {
			get {
				return caseSensitive;
			}
			set {
				caseSensitive = value;
			}
		}

		public IType CallingType {
			get {
				return callingType;
			}
			set {
				callingType = value;
			}
		}

		public IList<IReturnType> GenericParameters {
			get {
				return genericParameters;
			}
			set {
				genericParameters = value;
			}
		}
		
		public SearchTypeRequest (ICompilationUnit currentCompilationUnit)
		{
			this.currentCompilationUnit = currentCompilationUnit;
			this.caretLine   = -1;
			this.caretColumn = -1;
		//	this.genericParameterCount = -1;
		}
		
		public SearchTypeRequest (ICompilationUnit currentCompilationUnit, string name)
		{
			this.currentCompilationUnit = currentCompilationUnit;
			this.caretLine   = -1;
			this.caretColumn = -1;
			this.name        = name;
		//	this.genericParameterCount = -1;
		}
		
		public SearchTypeRequest (ICompilationUnit currentCompilationUnit, IReturnType rtype, IType callingType)
		{
			this.currentCompilationUnit = currentCompilationUnit;
			this.callingType = callingType;
			this.caretLine   = -1;
			this.caretColumn = -1;
			if (rtype != null) {
				name = rtype.Namespace ?? ""; 
				foreach (ReturnTypePart rpart in rtype.Parts) {
					if (name.Length > 0)
						name += ".";
					name += rpart.Name;
					if (rpart.GenericArguments.Count > 0)
						name += "`" + rpart.GenericArguments.Count;
				}
				this.genericParameters = new List<IReturnType> (rtype.GenericArguments);
			}
		}
		
		public SearchTypeRequest (ICompilationUnit currentCompilationUnit, int caretLine, int caretColumn, string name)
		{
			this.currentCompilationUnit = currentCompilationUnit;
			this.caretLine   = caretLine;
			this.caretColumn = caretColumn;
			this.name        = name;
		//	this.genericParameterCount = -1;
		}
		
		public SearchTypeRequest (ICompilationUnit currentCompilationUnit, IType callingType, string name)
		{
			this.currentCompilationUnit = currentCompilationUnit;
			this.callingType = callingType;
			this.name        = name;
		//	this.genericParameterCount = -1;
		}
		
		public SearchTypeRequest (ICompilationUnit currentCompilationUnit, int caretLine, int caretColumn, string name, IList<IReturnType> genericParameters)
		{
			this.currentCompilationUnit = currentCompilationUnit;
			this.caretLine   = caretLine;
			this.caretColumn = caretColumn;
			this.name        = name;
			this.genericParameters = genericParameters;
		}
		
	}
	
	public class SearchTypeResult 
	{
		IReturnType result;
		
		public IReturnType Result {
			get {
				return result;
			}
		}
		
		public SearchTypeResult (IType type)
		{
			this.result = new DomReturnType (type.FullName);
		}
		
		public SearchTypeResult (IReturnType result)
		{
			this.result = result;
		}
		
		public override string ToString ()
		{
			return string.Format ("[SearchTypeResult: Result={0}]", Result);
		}
	}
}
