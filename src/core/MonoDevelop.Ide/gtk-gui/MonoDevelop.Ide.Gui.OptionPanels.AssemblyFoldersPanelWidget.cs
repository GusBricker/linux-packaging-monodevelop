// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace MonoDevelop.Ide.Gui.OptionPanels {
    
    internal partial class AssemblyFoldersPanelWidget {
        
        private Gtk.VBox vbox1;
        
        private Gtk.Label label1;
        
        private MonoDevelop.Components.FolderListSelector selector;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget MonoDevelop.Ide.Gui.OptionPanels.AssemblyFoldersPanelWidget
            Stetic.BinContainer.Attach(this);
            this.Name = "MonoDevelop.Ide.Gui.OptionPanels.AssemblyFoldersPanelWidget";
            // Container child MonoDevelop.Ide.Gui.OptionPanels.AssemblyFoldersPanelWidget.Gtk.Container+ContainerChild
            this.vbox1 = new Gtk.VBox();
            this.vbox1.Name = "vbox1";
            this.vbox1.Spacing = 6;
            // Container child vbox1.Gtk.Box+BoxChild
            this.label1 = new Gtk.Label();
            this.label1.Name = "label1";
            this.label1.Xalign = 0F;
            this.label1.LabelProp = Mono.Unix.Catalog.GetString("Custom folders where MonoDevelop should look for assemblies and packages:");
            this.vbox1.Add(this.label1);
            Gtk.Box.BoxChild w1 = ((Gtk.Box.BoxChild)(this.vbox1[this.label1]));
            w1.Position = 0;
            w1.Expand = false;
            w1.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.selector = new MonoDevelop.Components.FolderListSelector();
            this.selector.Events = ((Gdk.EventMask)(256));
            this.selector.Name = "selector";
            this.vbox1.Add(this.selector);
            Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(this.vbox1[this.selector]));
            w2.Position = 1;
            this.Add(this.vbox1);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.Hide();
        }
    }
}
