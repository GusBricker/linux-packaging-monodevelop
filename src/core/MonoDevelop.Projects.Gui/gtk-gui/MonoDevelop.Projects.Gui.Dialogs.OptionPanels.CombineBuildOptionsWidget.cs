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
    
    internal partial class CombineBuildOptionsWidget {
        
        private Gtk.VBox vbox68;
        
        private Gtk.Label label73;
        
        private Gtk.HBox hbox46;
        
        private Gtk.Label label74;
        
        private MonoDevelop.Components.FolderEntry folderEntry;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CombineBuildOptionsWidget
            Stetic.BinContainer.Attach(this);
            this.Name = "MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CombineBuildOptionsWidget";
            // Container child MonoDevelop.Projects.Gui.Dialogs.OptionPanels.CombineBuildOptionsWidget.Gtk.Container+ContainerChild
            this.vbox68 = new Gtk.VBox();
            this.vbox68.Name = "vbox68";
            this.vbox68.BorderWidth = ((uint)(12));
            // Container child vbox68.Gtk.Box+BoxChild
            this.label73 = new Gtk.Label();
            this.label73.Name = "label73";
            this.label73.Xalign = 0F;
            this.label73.LabelProp = MonoDevelop.Core.GettextCatalog.GetString("<b>Output Directory</b>");
            this.label73.UseMarkup = true;
            this.vbox68.Add(this.label73);
            Gtk.Box.BoxChild w1 = ((Gtk.Box.BoxChild)(this.vbox68[this.label73]));
            w1.Position = 0;
            w1.Expand = false;
            w1.Fill = false;
            // Container child vbox68.Gtk.Box+BoxChild
            this.hbox46 = new Gtk.HBox();
            this.hbox46.Name = "hbox46";
            this.hbox46.Spacing = 6;
            // Container child hbox46.Gtk.Box+BoxChild
            this.label74 = new Gtk.Label();
            this.label74.Name = "label74";
            this.label74.LabelProp = MonoDevelop.Core.GettextCatalog.GetString("    ");
            this.hbox46.Add(this.label74);
            Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(this.hbox46[this.label74]));
            w2.Position = 0;
            w2.Expand = false;
            w2.Fill = false;
            // Container child hbox46.Gtk.Box+BoxChild
            this.folderEntry = new MonoDevelop.Components.FolderEntry();
            this.folderEntry.Name = "folderEntry";
            this.hbox46.Add(this.folderEntry);
            Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(this.hbox46[this.folderEntry]));
            w3.Position = 1;
            this.vbox68.Add(this.hbox46);
            Gtk.Box.BoxChild w4 = ((Gtk.Box.BoxChild)(this.vbox68[this.hbox46]));
            w4.Position = 1;
            w4.Expand = false;
            w4.Fill = false;
            w4.Padding = ((uint)(6));
            this.Add(this.vbox68);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.Show();
        }
    }
}
