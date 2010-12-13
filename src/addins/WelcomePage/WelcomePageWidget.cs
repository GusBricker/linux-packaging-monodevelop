// 
// WelcomePageFallbackWidget.cs
// 
// Author:
//   Scott Ellington
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// Copyright (c) 2005 Scott Ellington
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
using System.IO;
using System.Xml;
using Gdk;
using Gtk;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Desktop;

namespace MonoDevelop.WelcomePage
{
	partial class WelcomePageWidget : Gtk.EventBox
	{
		Gdk.Pixbuf bgPixbuf, logoPixbuf;
		
		WelcomePageView parentView;
		
		const string headerSize = "x-large";
		const string textSize = "medium";
		static readonly string headerFormat = "<span size=\"" + headerSize + "\"  foreground=\"#4e6d9f\">{0}</span>";
		static readonly string tableHeaderFormat = "<span size=\"" + textSize + "\" weight=\"bold\" foreground=\"#4e6d9f\">{0}</span>";
		static readonly string textFormat = "<span size=\"" + textSize + "\">{0}</span>";
		
		const uint spacing = 20;
		const uint logoHeight = 90;
		
		//keep ref to delegates, as we're going to use them a lot
		Gtk.LeaveNotifyEventHandler linkHoverLeaveEventHandler;
		Gtk.EnterNotifyEventHandler linkHoverEnterEventHandler;
		EventHandler linkClickedEventHandler;
		
		public WelcomePageWidget (WelcomePageView parentView) : base ()
		{
			this.Build ();
			this.parentView = parentView;
			
			linkHoverLeaveEventHandler = new Gtk.LeaveNotifyEventHandler (handleHoverLeave);
			linkHoverEnterEventHandler = new Gtk.EnterNotifyEventHandler (handleHoverEnter);
			linkClickedEventHandler = new EventHandler (HandleLink);

			string logoPath = AddinManager.CurrentAddin.GetFilePath ("md-logo.png");
			logoPixbuf = new Gdk.Pixbuf (logoPath);
			string bgPath = AddinManager.CurrentAddin.GetFilePath ("md-bg.png");
			bgPixbuf = new Gdk.Pixbuf (bgPath);
			
			alignment1.SetPadding (logoHeight + spacing, 0, spacing, 0);
			ModifyBg (StateType.Normal, Style.White);
			
			BuildFromXml ();
			LoadRecent ();

			IdeApp.Workbench.GuiLocked += OnLock;
			IdeApp.Workbench.GuiUnlocked += OnUnlock;
		}

		void OnLock (object s, EventArgs a)
		{
			Sensitive = false;
		}
		
		void OnUnlock (object s, EventArgs a)
		{
			Sensitive = true;
		}
		
		public void Rebuild ()
		{
			//this can get called by async dispatch after the widget is destroyed
			if (parentView == null)
				return;
			
			clearContainer (actionBox);
			clearContainer (newsLinkBox);
			clearContainer (supportLinkBox);
			clearContainer (devLinkBox);			
			BuildFromXml ();
			LoadRecent ();
		}
		
		void clearContainer (Container c)
		{
			while (c.Children.Length > 0)
				c.Remove (c.Children [0]);
		}
		
		void BuildFromXml ()
		{		
			XmlDocument xml = parentView.GetUpdatedXmlDocument ();
			
			//Actions
			XmlNode actions = xml.SelectSingleNode ("/WelcomePage/Actions");
			headerActions.Markup = string.Format (headerFormat, GettextCatalog.GetString (actions.Attributes ["_title"].Value));
			foreach (XmlNode link in actions.ChildNodes) {
				XmlAttribute a; 
				LinkButton button = new LinkButton ();
				button.Clicked += linkClickedEventHandler;
				button.EnterNotifyEvent += linkHoverEnterEventHandler;
				button.LeaveNotifyEvent += linkHoverLeaveEventHandler;
				a = link.Attributes ["_title"];
				if (a != null) button.Label = string.Format (textFormat, GettextCatalog.GetString (a.Value));
				a = link.Attributes ["href"];
				if (a != null) button.LinkUrl = a.Value;
				a = link.Attributes ["_desc"];
				if (a != null) button.HoverMessage = GettextCatalog.GetString (a.Value);
				actionBox.PackEnd (button, true, false, 0);
			}
			actionBox.ShowAll ();
			
			//Support Links
			XmlNode supportLinks = xml.SelectSingleNode ("/WelcomePage/Links[@_title=\"Support Links\"]");
			headerSupportLinks.Markup = string.Format (headerFormat, GettextCatalog.GetString (supportLinks.Attributes ["_title"].Value));
			foreach (XmlNode link in supportLinks.ChildNodes) {
				XmlAttribute a; 
				LinkButton button = new LinkButton ();
				button.Clicked += linkClickedEventHandler;
				button.EnterNotifyEvent += linkHoverEnterEventHandler;
				button.LeaveNotifyEvent += linkHoverLeaveEventHandler;
				a = link.Attributes ["_title"];
				if (a != null) button.Label = string.Format (textFormat, GettextCatalog.GetString (a.Value));
				a = link.Attributes ["href"];
				if (a != null) button.LinkUrl = a.Value;
				a = link.Attributes ["_desc"];
				if (a != null) button.Description = GettextCatalog.GetString (a.Value);
				supportLinkBox.PackEnd (button, true, false, 0);
			}
			supportLinkBox.ShowAll ();
			
			//News Links
			XmlNode newsLinks = xml.SelectSingleNode ("/WelcomePage/Links[@_title=\"News Links\"]");
			headerNewsLinks.Markup = string.Format (headerFormat, GettextCatalog.GetString (newsLinks.Attributes ["_title"].Value));
			foreach (XmlNode link in newsLinks.ChildNodes) {
				XmlAttribute a; 
				LinkButton button = new LinkButton ();
				button.Clicked += linkClickedEventHandler;
				button.EnterNotifyEvent += linkHoverEnterEventHandler;
				button.LeaveNotifyEvent += linkHoverLeaveEventHandler;
				a = link.Attributes ["_title"];
				if (a != null) button.Label = string.Format (textFormat, GettextCatalog.GetString (a.Value));
				a = link.Attributes ["href"];
				if (a != null) button.LinkUrl = a.Value;
				a = link.Attributes ["_desc"];
				if (a != null) button.Description = GettextCatalog.GetString (a.Value);
				newsLinkBox.PackEnd (button, true, false, 0);
			}
			if (!newsLinks.HasChildNodes)
			{
				LinkButton button = new LinkButton ();
				button.EnterNotifyEvent += linkHoverEnterEventHandler;
				button.LeaveNotifyEvent += linkHoverLeaveEventHandler;
				button.Label = GettextCatalog.GetString ("No news has been found");
				newsLinkBox.PackEnd (button, true, false, 0);
			}
			newsLinkBox.ShowAll ();
			
			//Development Links
			XmlNode devLinks = xml.SelectSingleNode ("/WelcomePage/Links[@_title=\"Development Links\"]");
			headerDevLinks.Markup = string.Format (headerFormat, GettextCatalog.GetString (devLinks.Attributes ["_title"].Value));
			foreach (XmlNode link in devLinks.ChildNodes) {
				XmlAttribute a; 
				LinkButton button = new LinkButton ();
				button.Clicked += linkClickedEventHandler;
				button.EnterNotifyEvent += linkHoverEnterEventHandler;
				button.LeaveNotifyEvent += linkHoverLeaveEventHandler;
				a = link.Attributes ["_title"];
				if (a != null) button.Label = string.Format (textFormat, GettextCatalog.GetString (a.Value));
				a = link.Attributes ["href"];
				if (a != null) button.LinkUrl = a.Value;
				a = link.Attributes ["_desc"];
				if (a != null) button.Description = GettextCatalog.GetString (a.Value);
				devLinkBox.PackEnd (button, true, false, 0);
			}
			devLinkBox.ShowAll ();
			
			//Recently Changed
			XmlNode recChanged = xml.SelectSingleNode ("/WelcomePage/Projects");
			headerRecentProj.Markup = string.Format (headerFormat, GettextCatalog.GetString (recChanged.Attributes ["_title"].Value));
			projNameLabel.Markup = string.Format (tableHeaderFormat, GettextCatalog.GetString (recChanged.Attributes ["_col1"].Value));
			projTimeLabel.Markup = string.Format (tableHeaderFormat, GettextCatalog.GetString (recChanged.Attributes ["_col2"].Value));
			//_linkTitle="Open Project"
		}
		
		void HandleLink (object sender, EventArgs e)
		{
			LinkButton button = (LinkButton) sender;
			if (parentView != null)
				parentView.HandleLinkAction (button.LinkUrl); 
		}
		
		void handleHoverEnter (object sender, EventArgs e)
		{
			parentView.SetLinkStatus (((LinkButton) sender).LinkUrl);
		}
		
		void handleHoverLeave (object sender, EventArgs e)
		{
			parentView.SetLinkStatus (null);
		}
		
		//draw the background
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (logoPixbuf != null) {
				var gc = Style.BackgroundGC (State);
				var lRect = new Rectangle (Allocation.X, Allocation.Y, logoPixbuf.Width, logoPixbuf.Height);
				if (evnt.Region.RectIn (lRect) != OverlapType.Out)
					GdkWindow.DrawPixbuf (gc, logoPixbuf, 0, 0, lRect.X, lRect.Y, lRect.Width, lRect.Height, RgbDither.None, 0, 0);
				
				var bgRect = new Rectangle (Allocation.X + logoPixbuf.Width, Allocation.Y, Allocation.Width - logoPixbuf.Width, bgPixbuf.Height);
				if (evnt.Region.RectIn (bgRect) != OverlapType.Out)
					for (int x = bgRect.X; x < bgRect.Right; x += bgPixbuf.Width)
						GdkWindow.DrawPixbuf (gc, bgPixbuf, 0, 0, x, bgRect.Y, bgPixbuf.Width, bgRect.Height, RgbDither.None, 0, 0);
			}
			
			foreach (Widget widget in Children)
				PropagateExpose (widget, evnt);
			
			return true;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			parentView = null;
			IdeApp.Workbench.GuiLocked -= OnLock;
			IdeApp.Workbench.GuiUnlocked -= OnUnlock;
		}
		
		public void LoadRecent ()
		{
			//this can get called by async dispatch after the widget is destroyed
			if (parentView == null)
				return;
			
			Widget[] oldChildren = (Widget[]) recentFilesTable.Children.Clone ();
			foreach (Widget w in oldChildren) {
				Gtk.Table.TableChild tc = (Gtk.Table.TableChild) recentFilesTable [w];
				if (tc.TopAttach >= 2)
					recentFilesTable.Remove (w);
			}
			
			var recent = parentView.GetRecentProjects ();
			if (recent.Count == 0)
				return;
			
			uint i = 2;
			foreach (var ri in recent) {
				//getting the icon requires probing the file, so handle IO errors
				string icon;
				try {
					if (!System.IO.File.Exists (ri.FileName))
						continue;
/* delay project service creation. 
					icon = IdeApp.Services.ProjectService.FileFormats.GetFileFormats
							(ri.LocalPath, typeof(Solution)).Length > 0
								? "md-solution"
								: "md-workspace";*/
					
					icon = System.IO.Path.GetExtension (ri.FileName) != ".mdw"
								? "md-solution"
								: "md-workspace";
				}
				catch (IOException ex) {
					LoggingService.LogWarning ("Error building recent solutions list", ex);
					continue;
				}
				
				LinkButton button = new LinkButton ();
				Label label = new Label ();
				recentFilesTable.Attach (button, 0, 1, i, i+1);
				recentFilesTable.Attach (label, 1, 2, i, i+1);
				button.Clicked += linkClickedEventHandler;
				button.EnterNotifyEvent += linkHoverEnterEventHandler;
				button.LeaveNotifyEvent += linkHoverLeaveEventHandler;
				label.Justify = Justification.Right;
				label.Xalign = 1;
				button.Xalign = 0;
				
				string name = ri.DisplayName;
				button.Label = string.Format (textFormat, name);
				button.HoverMessage = ri.FileName;
				button.LinkUrl = "project://" + ri.FileName;
				button.Icon = icon;
				label.Markup = string.Format (textFormat, WelcomePageView.TimeSinceEdited (ri.TimeStamp));
				
				i++;
				
				button.InnerLabel.MaxWidthChars = 22;
				button.InnerLabel.Ellipsize = Pango.EllipsizeMode.End;
			}
			recentFilesTable.RowSpacing = 0;
			recentFilesTable.ShowAll ();
		}
	}
	
	[System.ComponentModel.Category("WelcomePage")]
	[System.ComponentModel.ToolboxItem(true)]
	
	
	public class LinkButton : Gtk.Button
	{
		string hoverMessage = null;
		Label label;
		Gtk.Image image;
		string text;
		string desc;
		string icon;
		
		public LinkButton () : base ()
		{
			label = new Label ();
			label.Xalign = 0;
			label.Xpad = 0;
			label.Ypad = 0;
			image = new Gtk.Image ();
			
			HBox box = new HBox (false, 6);
			box.PackStart (image, false, false, 0);
			box.PackStart (label, true, true, 0);
			Add (box);
			Relief = ReliefStyle.None;
		}
			
		public string HoverMessage {
			get { return hoverMessage; }
			set {
				hoverMessage = value;
				this.TooltipText = hoverMessage;
			}
		}
		
		public new string Label {
			get { return text; }
			set { text = value; UpdateLabel (); }
		}
		
		public string Description {
			get { return desc; }
			set { desc = value; UpdateLabel (); }
		}
		
		public string Icon {
			get { return icon; }
			set { icon = value; UpdateLabel (); }
		}
		
		void UpdateLabel ()
		{
			if (icon != null) {
				image.Pixbuf = ImageService.GetPixbuf (icon, Gtk.IconSize.Menu);
				image.Visible = true;
			} else {
				image.Visible = false;
			}
			string markup = string.Format ("<span underline=\"single\" foreground=\"#5a7ac7\">{0}</span>", text);
			if (!string.IsNullOrEmpty (desc))
				markup += "\n<span size=\"small\">" + desc + "</span>";
			label.Markup = markup;
		}
		
		string linkUrl;
		
		public string LinkUrl {
			get { return linkUrl; }
			set { linkUrl = value; }
		}
		
		public Label InnerLabel {
			get { return label; }
		}
	}
}
