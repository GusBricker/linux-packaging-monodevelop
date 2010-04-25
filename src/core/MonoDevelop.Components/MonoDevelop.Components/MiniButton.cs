// 
// MiniButton.cs
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
using Gtk;

namespace MonoDevelop.Components
{
	public class MiniButton: Gtk.EventBox
	{
		public event EventHandler Clicked;
		Gdk.Color normalColor;
		bool pressed;
		bool highligted;
		
		public MiniButton ()
		{
			Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
		}
		
		public MiniButton (Gtk.Widget label): this ()
		{
			Add (label);
			label.Show ();
		}
		
		public bool ToggleMode { get; set; }
		
		public bool Pressed {
			get {
				return pressed;
			}
			set {
				if (ToggleMode && pressed != value) {
					pressed = value;
					SetBg (value || highligted);
				}
			}
		}
		
		protected virtual void OnClicked ()
		{
			if (ToggleMode)
				Pressed = !pressed;
			if (Clicked != null)
				Clicked (this, EventArgs.Empty);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1)
				OnClicked ();
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			normalColor = Style.Background (Gtk.StateType.Normal);
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			highligted = true;
			if (!ToggleMode || !pressed)
				SetBg (true);
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			highligted = false;
			if (!ToggleMode || !pressed)
				SetBg (false);
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		void SetBg (bool hilight)
		{
			if (hilight)
				ModifyBg (StateType.Normal, Style.Base (Gtk.StateType.Normal));
			else
				ModifyBg (StateType.Normal, normalColor);
		}
	}
}
