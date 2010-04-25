//
// DomParameter.cs
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
using System.CodeDom;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom
{
	public class DomParameter : IParameter
	{
		protected List<IAttribute> attributes = null;

		public MemberType MemberType {
			get {
				return MemberType.Parameter;
			}
		}

		public CodeExpression DefaultValue {
			get;
			set;
		}

		public IMember DeclaringMember {
			get;
			set;
		}

		public string Name {
			get;
			set;
		}

		public ParameterModifiers ParameterModifiers {
			get;
			set;
		}

		public IReturnType ReturnType {
			get;
			set;
		}
		
		public bool IsOut {
			get {
				return (this.ParameterModifiers & ParameterModifiers.Out) == ParameterModifiers.Out;
			}
		}
		
		public bool IsRef {
			get {
				return (this.ParameterModifiers & ParameterModifiers.Ref) == ParameterModifiers.Ref;
			}
		}
		
		public bool IsParams {
			get {
				return (this.ParameterModifiers & ParameterModifiers.Params) == ParameterModifiers.Params;
			}
		}
		
		public System.Collections.Generic.IEnumerable<IAttribute> Attributes {
			get {
				return (IEnumerable<IAttribute>)attributes ?? new IAttribute[0];
			}
		}

		public DomLocation Location {
			get;
			set;
		}
		
		public virtual string StockIcon {
			get {
				return Stock.Field;
			}
		}
		
		public DomParameter ()
		{
		}
		
		public DomParameter (IMember declaringMember, string name, IReturnType returnType)
		{
			this.Name            = name;
			this.DeclaringMember = declaringMember;
			this.ReturnType      = returnType;
		}
		
		public void Add (IAttribute attribute)
		{
			if (attributes == null)
				attributes = new List<IAttribute> ();
			attributes.Add (attribute);
		}
		
		public override string ToString ()
		{
			return string.Format ("[DomParameter:Name={0}, ParameterModifiers={1}, ReturnType={2}, Location={3}]",
			                      Name,
			                      ParameterModifiers,
			                      ReturnType,
			                      Location);
		}

		public S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
	}
}
