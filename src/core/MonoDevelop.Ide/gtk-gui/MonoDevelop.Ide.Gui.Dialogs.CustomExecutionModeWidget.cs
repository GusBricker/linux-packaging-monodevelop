// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace MonoDevelop.Ide.Gui.Dialogs {
    
    public partial class CustomExecutionModeWidget {
        
        private Gtk.VBox vbox2;
        
        private Gtk.Table table1;
        
        private Gtk.Entry entryArgs;
        
        private MonoDevelop.Components.FolderEntry folderEntry;
        
        private Gtk.Label label2;
        
        private Gtk.Label label4;
        
        private Gtk.Label label3;
        
        private MonoDevelop.Projects.Gui.Dialogs.OptionPanels.EnvVarList envVarList;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget MonoDevelop.Ide.Gui.Dialogs.CustomExecutionModeWidget
            Stetic.BinContainer.Attach(this);
            this.Name = "MonoDevelop.Ide.Gui.Dialogs.CustomExecutionModeWidget";
            // Container child MonoDevelop.Ide.Gui.Dialogs.CustomExecutionModeWidget.Gtk.Container+ContainerChild
            this.vbox2 = new Gtk.VBox();
            this.vbox2.Name = "vbox2";
            this.vbox2.Spacing = 9;
            this.vbox2.BorderWidth = ((uint)(6));
            // Container child vbox2.Gtk.Box+BoxChild
            this.table1 = new Gtk.Table(((uint)(2)), ((uint)(2)), false);
            this.table1.Name = "table1";
            this.table1.RowSpacing = ((uint)(6));
            this.table1.ColumnSpacing = ((uint)(6));
            // Container child table1.Gtk.Table+TableChild
            this.entryArgs = new Gtk.Entry();
            this.entryArgs.CanFocus = true;
            this.entryArgs.Name = "entryArgs";
            this.entryArgs.IsEditable = true;
            this.entryArgs.InvisibleChar = '●';
            this.table1.Add(this.entryArgs);
            Gtk.Table.TableChild w1 = ((Gtk.Table.TableChild)(this.table1[this.entryArgs]));
            w1.LeftAttach = ((uint)(1));
            w1.RightAttach = ((uint)(2));
            w1.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table1.Gtk.Table+TableChild
            this.folderEntry = new MonoDevelop.Components.FolderEntry();
            this.folderEntry.Name = "folderEntry";
            this.table1.Add(this.folderEntry);
            Gtk.Table.TableChild w2 = ((Gtk.Table.TableChild)(this.table1[this.folderEntry]));
            w2.TopAttach = ((uint)(1));
            w2.BottomAttach = ((uint)(2));
            w2.LeftAttach = ((uint)(1));
            w2.RightAttach = ((uint)(2));
            w2.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table1.Gtk.Table+TableChild
            this.label2 = new Gtk.Label();
            this.label2.Name = "label2";
            this.label2.Xalign = 0F;
            this.label2.LabelProp = Mono.Unix.Catalog.GetString("Working Directory:");
            this.table1.Add(this.label2);
            Gtk.Table.TableChild w3 = ((Gtk.Table.TableChild)(this.table1[this.label2]));
            w3.TopAttach = ((uint)(1));
            w3.BottomAttach = ((uint)(2));
            w3.XOptions = ((Gtk.AttachOptions)(4));
            w3.YOptions = ((Gtk.AttachOptions)(4));
            // Container child table1.Gtk.Table+TableChild
            this.label4 = new Gtk.Label();
            this.label4.Name = "label4";
            this.label4.Xalign = 0F;
            this.label4.LabelProp = Mono.Unix.Catalog.GetString("Arguments:");
            this.table1.Add(this.label4);
            Gtk.Table.TableChild w4 = ((Gtk.Table.TableChild)(this.table1[this.label4]));
            w4.XOptions = ((Gtk.AttachOptions)(4));
            w4.YOptions = ((Gtk.AttachOptions)(4));
            this.vbox2.Add(this.table1);
            Gtk.Box.BoxChild w5 = ((Gtk.Box.BoxChild)(this.vbox2[this.table1]));
            w5.Position = 0;
            w5.Expand = false;
            w5.Fill = false;
            // Container child vbox2.Gtk.Box+BoxChild
            this.label3 = new Gtk.Label();
            this.label3.Name = "label3";
            this.label3.Xalign = 0F;
            this.label3.LabelProp = Mono.Unix.Catalog.GetString("Environment Variables:");
            this.vbox2.Add(this.label3);
            Gtk.Box.BoxChild w6 = ((Gtk.Box.BoxChild)(this.vbox2[this.label3]));
            w6.Position = 1;
            w6.Expand = false;
            w6.Fill = false;
            // Container child vbox2.Gtk.Box+BoxChild
            this.envVarList = new MonoDevelop.Projects.Gui.Dialogs.OptionPanels.EnvVarList();
            this.envVarList.CanFocus = true;
            this.envVarList.Name = "envVarList";
            this.envVarList.ShadowType = ((Gtk.ShadowType)(1));
            this.vbox2.Add(this.envVarList);
            Gtk.Box.BoxChild w7 = ((Gtk.Box.BoxChild)(this.vbox2[this.envVarList]));
            w7.Position = 2;
            this.Add(this.vbox2);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.Hide();
        }
    }
}
