// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels {
    
    internal partial class CustomCommandPanelWidget {
        
        private Gtk.VBox vbox;
        
        private Gtk.Label label3;
        
        private Gtk.ScrolledWindow scrolledwindow1;
        
        private Gtk.VBox vboxCommands;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CustomCommandPanelWidget
            Stetic.BinContainer.Attach(this);
            this.Events = ((Gdk.EventMask)(256));
            this.Name = "MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CustomCommandPanelWidget";
            // Container child MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CustomCommandPanelWidget.Gtk.Container+ContainerChild
            this.vbox = new Gtk.VBox();
            this.vbox.Name = "vbox";
            this.vbox.Spacing = 6;
            // Container child vbox.Gtk.Box+BoxChild
            this.label3 = new Gtk.Label();
            this.label3.WidthRequest = 470;
            this.label3.Name = "label3";
            this.label3.Xalign = 0F;
            this.label3.LabelProp = MonoDevelop.Core.GettextCatalog.GetString("MonoDevelop can execute user specified commands or scripts before, after or as a replacement of common project operations. It is also possible to enter custom commands which will be available in the project or solution menu.");
            this.label3.Wrap = true;
            this.vbox.Add(this.label3);
            Gtk.Box.BoxChild w1 = ((Gtk.Box.BoxChild)(this.vbox[this.label3]));
            w1.Position = 0;
            w1.Expand = false;
            w1.Fill = false;
            // Container child vbox.Gtk.Box+BoxChild
            this.scrolledwindow1 = new Gtk.ScrolledWindow();
            this.scrolledwindow1.CanFocus = true;
            this.scrolledwindow1.Name = "scrolledwindow1";
            this.scrolledwindow1.HscrollbarPolicy = ((Gtk.PolicyType)(2));
            // Container child scrolledwindow1.Gtk.Container+ContainerChild
            Gtk.Viewport w2 = new Gtk.Viewport();
            w2.ShadowType = ((Gtk.ShadowType)(0));
            // Container child GtkViewport.Gtk.Container+ContainerChild
            this.vboxCommands = new Gtk.VBox();
            this.vboxCommands.CanFocus = true;
            this.vboxCommands.Name = "vboxCommands";
            w2.Add(this.vboxCommands);
            this.scrolledwindow1.Add(w2);
            this.vbox.Add(this.scrolledwindow1);
            Gtk.Box.BoxChild w5 = ((Gtk.Box.BoxChild)(this.vbox[this.scrolledwindow1]));
            w5.Position = 1;
            this.Add(this.vbox);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.Show();
        }
    }
}
