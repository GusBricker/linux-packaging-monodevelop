//
// DomReturnType.cs
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
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MonoDevelop.Projects.Dom
{
	public class ReturnTypePart : IReturnTypePart
	{
		public string Name {
			get;
			set;
		}
		
		protected List<IReturnType> genericArguments = null;
		static readonly ReadOnlyCollection<IReturnType> emptyGenericParameters = new ReadOnlyCollection<IReturnType> (new IReturnType [0]);
		public System.Collections.ObjectModel.ReadOnlyCollection<IReturnType> GenericArguments {
			get {
				if (genericArguments == null)
					return emptyGenericParameters;
				return genericArguments.AsReadOnly ();
			}
		}
		
		
		public object Tag {
			get;
			set;
		}
		
		public ReturnTypePart ()
		{
		}
		
		public ReturnTypePart (string name)
		{
			this.Name = name;
		}
		
		public ReturnTypePart (string name, IEnumerable<IReturnType> typeParameters)
		{
			for (int i = 0; i < name.Length; i++) {
				char ch = name[i];
				if (!(Char.IsLetterOrDigit (ch) || ch =='_')) {
					name = name.Substring (0, i);
					break;
				}
			}
			this.Name = name;
			if (typeParameters != null) 
				this.genericArguments = new List<IReturnType> (typeParameters);
		}
		public ReturnTypePart (string baseName, string name, IEnumerable<ITypeParameter> typeParameters)
		{
			this.Name = name;
			if (typeParameters != null) {
				this.genericArguments = new List<IReturnType> ();
				foreach (ITypeParameter para in typeParameters) {
					this.genericArguments.Add (new DomReturnType (baseName + "." + para.Name));
				}
			}
		}
		
		public string ToInvariantString ()
		{
			if (genericArguments != null && genericArguments.Count > 0) {
				StringBuilder result = new StringBuilder ();
				result.Append (Name);
				result.Append ('<');
				for (int i = 0; i < genericArguments.Count; i++) {
					if (i > 0)
						result.Append (',');
					result.Append (genericArguments[i].ToInvariantString ());
				}
				result.Append ('>');
				return result.ToString ();
			}
			return Name;
		}
		
		public void AddTypeParameter (IReturnType type)
		{
			if (genericArguments == null)
				genericArguments = new List<IReturnType> ();
			this.genericArguments.Add (type);
		}
		public override string ToString ()
		{
			return string.Format ("[ReturnTypePart: Name={0}, #GenericArguments={1}]", Name, GenericArguments.Count);
		}
		
	}
	
	public class DomReturnType : IReturnType
	{
		static readonly int[] zeroDimensions = new int[0];
		static readonly int[] oneDimensions = new int[] { 0 };
		
		List<IReturnTypePart> parts = new List<IReturnTypePart> ();
		
		// TODO dom: free unused return types
//		static Dictionary<string, IReturnType> returnTypeCache;
		
		public static readonly IReturnType Void;
		public static readonly IReturnType Object;
		public static readonly IReturnType Exception;
		public static readonly IReturnType Int32;
		public static readonly IReturnType String;
		public static readonly IReturnType Char;
		public static readonly IReturnType Bool;
		
		public object Tag {
			get {
				return parts[parts.Count - 1].Tag;
			}
			set {
				parts[parts.Count - 1].Tag = value;
			}
		}
		
		static DomReturnType ()
		{
			// Initialization is done here instead of using field initializers to
			// ensure that the returnTypeCache dictionary us properly initialized
			// when calling GetSharedReturnType.

//			returnTypeCache = new Dictionary<string, IReturnType> ();

			Void = GetSharedReturnType ("System.Void");
			String = GetSharedReturnType ("System.String");
			Char = GetSharedReturnType ("System.Char");
			Object = GetSharedReturnType ("System.Object");
			Exception = GetSharedReturnType ("System.Exception");
			Int32 = GetSharedReturnType ("System.Int32");
			Bool = GetSharedReturnType ("System.Boolean");
		}

		public List<IReturnTypePart> Parts {
			get {
				return parts;
			}
		}
		
		public string Name {
			get {
				Debug.Assert (parts.Count > 0);
				return parts[parts.Count - 1].Name;
			}
			set {
				Debug.Assert (parts.Count > 0);
				parts[parts.Count - 1].Name = value;
			}
		}
		
		public ReadOnlyCollection<IReturnType> GenericArguments {
			get {
				Debug.Assert (parts.Count > 0);
				return parts[parts.Count - 1].GenericArguments;
			}
		}
		
		public void AddTypeParameter (IReturnType type)
		{
			Debug.Assert (parts.Count > 0);
			parts[parts.Count - 1].AddTypeParameter (type);
		}
		
		protected string nspace;
		protected int pointerNestingLevel, arrayPointerNestingLevel;
		protected int[] dimensions = null;
		ReturnTypeModifiers modifiers;
		
		public string FullName {
			get {
				if (Parts.Count == 1)
					return !string.IsNullOrEmpty (nspace) ? nspace + "." + Name : Name;
				StringBuilder result = new StringBuilder (nspace);
				foreach (IReturnTypePart part in Parts) {
					if (result.Length > 0)
						result.Append (".");
					result.Append (part.Name);
				}
				return result.ToString ();
			}
		}
		
		public string DecoratedFullName {
			get {
				StringBuilder result = new StringBuilder (Namespace);
				foreach (ReturnTypePart rpart in Parts) {
					if (result.Length > 0)
						result.Append (".");
					result.Append (rpart.Name);
					if (rpart.GenericArguments.Count > 0) {
						result.Append ("`");
						result.Append (rpart.GenericArguments.Count);
					}
				}
				return result.ToString ();
			}
		}
		
		public static KeyValuePair<string, string> SplitFullName (string fullName)
		{
			if (string.IsNullOrEmpty (fullName)) 
				return new KeyValuePair<string, string> ("", "");
			int idx = fullName.LastIndexOf ('.');
			if (idx >= 0) 
				return new KeyValuePair<string, string> (fullName.Substring (0, idx), fullName.Substring (idx + 1));
			return new KeyValuePair<string, string> ("", fullName);
		}

		public ReturnTypeModifiers Modifiers {
			get {
				return this.modifiers;
			}
			set {
				this.modifiers = value;
			}
		}
		
		public string Namespace {
			get {
				return nspace;
			}
			set {
				nspace = value;
			}
		}
		
		public int ArrayPointerNestingLevel {
			get {
				return arrayPointerNestingLevel;
			}
			set {
				arrayPointerNestingLevel = value;
			}
		}
		
		public int PointerNestingLevel {
			get {
				return pointerNestingLevel;
			}
			set {
				pointerNestingLevel = value;
			}
		}
		
		public int ArrayDimensions {
			get {
				return dimensions != null ? dimensions.Length : 0;
			}
			set {
				List<int> curDimensions = new List<int> (dimensions ?? zeroDimensions);
				if (curDimensions.Count > value) 
					curDimensions.RemoveRange (value, value - curDimensions.Count);
				while (curDimensions.Count < value)
					curDimensions.Add (0);
				SetDimensions (curDimensions.ToArray ());
			}
		}

		public bool IsNullable {
			get {
				return (Modifiers & ReturnTypeModifiers.Nullable) == ReturnTypeModifiers.Nullable;
			}
			set {
				if (value) {
					Modifiers |= ReturnTypeModifiers.Nullable;
				} else {
					Modifiers &= ~ReturnTypeModifiers.Nullable;
				}
			}
		}

		public bool IsByRef {
			get {
				return (Modifiers & ReturnTypeModifiers.ByRef) == ReturnTypeModifiers.ByRef;
			}
			set {
				if (value) {
					Modifiers |= ReturnTypeModifiers.ByRef;
				} else {
					Modifiers &= ~ReturnTypeModifiers.ByRef;
				}
			}
		}
		
		protected IType type;
		public virtual IType Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public DomReturnType ()
		{
			this.parts.Add (new ReturnTypePart ());
		}
		
		internal DomReturnType (string ns, List<IReturnTypePart> parts)
		{
			this.nspace = ns;
			this.parts = parts;
		}
		
		public DomReturnType (IType type)
		{
			SetType (type);
		}
		
		public void SetType (IType type)
		{
			if (type == null)
				throw new ArgumentNullException ("type was null");
			this.parts.Clear ();
			this.type = type;
			this.nspace = type is InstantiatedType ? ((InstantiatedType)type).UninstantiatedType.Namespace : type.Namespace;
			IType curType = type;
			do {
				if (curType is InstantiatedType) {
					InstantiatedType instType = (InstantiatedType)curType;
					this.parts.Insert (0, new ReturnTypePart (instType.UninstantiatedType.Name, instType.GenericParameters));
				} else
					this.parts.Insert (0, new ReturnTypePart (curType.FullName, curType.Name, curType.TypeParameters));
				curType = curType.DeclaringType;
			} while (curType != null);
		}
		
		public override bool Equals (object obj)
		{
			DomReturnType type = obj as DomReturnType;
			if (type == null)
				return false;
			if (ArrayDimensions != type.ArrayDimensions)
				return false;
			for (int n=0; n<ArrayDimensions; n++) {
				if (GetDimension (n) != type.GetDimension (n))
					return false;
			}
			if (GenericArguments.Count != type.GenericArguments.Count)
				return false;
			for (int i = 0; i < GenericArguments.Count; i++) {
				if (!GenericArguments[i].Equals (type.GenericArguments [i]))
					return false;
			}

			return Name == type.Name &&
				nspace == type.nspace &&
				pointerNestingLevel == type.pointerNestingLevel &&
				Modifiers == type.Modifiers;
		}

		public override int GetHashCode ()
		{
			return ToInvariantString ().GetHashCode ();
		}

		
		public int GetDimension (int arrayDimension)
		{
			if (dimensions == null || arrayDimension < 0 || arrayDimension >= dimensions.Length)
				return -1;
			return this.dimensions [arrayDimension];
		}

		public void SetDimension (int arrayDimension, int dimension)
		{
			if (arrayDimension < 0 || arrayDimension >= ArrayDimensions)
				return;
			
			// Avoid changing the shared dimension
			if (dimensions == oneDimensions)
				dimensions = new int [ArrayDimensions];
			
			dimensions [arrayDimension] = dimension;
			SetDimensions (dimensions);
		}
		
		public void SetDimensions (int[] arrayDimensions)
		{
			// Reuse common dimension constants to save memory
			if (arrayDimensions == null)
				dimensions = null;
			else if (arrayDimensions != null && arrayDimensions.Length == 1 && arrayDimensions[0] == 0)
				dimensions = oneDimensions;
			else
				dimensions = arrayDimensions;
		}
		
		public int[] GetDimensions ()
		{
			return dimensions ?? zeroDimensions;
		}
		
		public DomReturnType (string name) : this (name, false, new List<IReturnType> ())
		{
		}
		
		public DomReturnType (string name, bool isNullable, IEnumerable<IReturnType> typeParameters)
		{
			KeyValuePair<string, string> splitted = SplitFullName (name);
			this.nspace = splitted.Key;
			this.parts.Add (new ReturnTypePart (splitted.Value, typeParameters));
			this.IsNullable     = isNullable;
		}
		
		public static IReturnType FromInvariantString (string invariantString)
		{
			return GetSharedReturnType (invariantString);
		}
		
		public static int num = 0;
		string invariantString = null;
		public string ToInvariantString ()
		{
			if (invariantString != null)
				return invariantString;
			StringBuilder result = new StringBuilder ();
			result.Append (Namespace);
			foreach (ReturnTypePart part in Parts) {
				if (result.Length > 0)
					result.Append ('.');
				result.Append (part.ToInvariantString ());
							}
			if (this.IsNullable)
				result.Append ('?');

			result.Append ('*', this.PointerNestingLevel);

			for (int i = 0; i < ArrayDimensions; i++) {
				result.Append ('[');
				int dimension = this.GetDimension (i);
				if (dimension > 0)
					result.Append (',', dimension);
				result.Append (']');
			}
			
			result.Append ('*', this.ArrayPointerNestingLevel);

			if (this.IsByRef)
				result.Append ('&');

			return invariantString = result.ToString ();
		}
		
		public S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
		
		public override string ToString ()
		{
			StringBuilder genArgs = new StringBuilder ();
			if (GenericArguments == null) {
				genArgs.Append ("<null>");
			} else {
				genArgs.Append ("{");
				foreach (object o in GenericArguments) {
					if (genArgs.Length > 1)
						genArgs.Append (", ");
					genArgs.Append (o != null ? o.ToString () : "null");
				} 
				genArgs.Append ("}");
			}
			
			return string.Format ("[DomReturnType:FullName={0}, PointerNestingLevel={1}, ArrayDimensions={2}, GenericArguments={3}, UnderlyingType={4}]",
			                      FullName,
			                      PointerNestingLevel,
			                      ArrayDimensions,
			                      genArgs.ToString (),
			                      Type == null ? "null" : Type.ToString ());
		}
		
		public static string ConvertToString (IReturnType type)
		{
			StringBuilder sb = new StringBuilder (DomType.GetInstantiatedTypeName (type.FullName, type.GenericArguments));
			
			if (type.PointerNestingLevel > 0)
				sb.Append (new String ('*', type.PointerNestingLevel));
			
			if (type.ArrayDimensions > 0) {
				for (int i = 0; i < type.ArrayDimensions; i++) {
					sb.Append ("[]");
				}
			}
			
			return sb.ToString ();
		}
		
#region shared return types

		public static IReturnType GetSharedReturnType (string invariantString)
		{
			if (string.IsNullOrEmpty (invariantString))
				return null;
			return new DomReturnType (invariantString);
		/*	lock (returnTypeCache) {
				IReturnType type;
				if (!returnTypeCache.TryGetValue (invariantString, out type)) {
					DomReturnType newType = new DomReturnType (invariantString);
					returnTypeCache[invariantString] = newType;
					return newType;
				}
				return type;
			}*/
		}
		
		public static IReturnType GetSharedReturnType (IReturnType returnType)
		{
			return returnType;
/*			if (returnType == null)
				return null;
			string invariantString = returnType.ToInvariantString();
			lock (returnTypeCache) {
				IReturnType type;
				if (!returnTypeCache.TryGetValue (invariantString, out type)) {
					returnTypeCache[invariantString] = returnType;
					return returnType;
				}
				return type;
			}*/
		}
#endregion
	}
}
