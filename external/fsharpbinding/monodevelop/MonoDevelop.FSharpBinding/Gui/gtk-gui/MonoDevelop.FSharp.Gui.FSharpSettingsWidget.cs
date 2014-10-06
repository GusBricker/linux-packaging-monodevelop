
// This file has been generated by the GUI designer. Do not modify.
namespace MonoDevelop.FSharp.Gui
{
	public partial class FSharpSettingsWidget
	{
		private global::Gtk.VBox vbox1;
		
		private global::Gtk.HBox hbox2;
		
		private global::Gtk.Label label4;
		
		private global::Gtk.CheckButton checkInteractiveUseDefault;
		
		private global::Gtk.Table table5;
		
		private global::Gtk.Button buttonBrowse;
		
		private global::Gtk.Entry entryArguments;
		
		private global::Gtk.Entry entryPath;
		
		private global::Gtk.Label GtkLabel4;
		
		private global::Gtk.Label GtkLabel6;
		
		private global::Gtk.CheckButton advanceToNextLineCheckbox;
		
		private global::Gtk.HSeparator hseparator4;
		
		private global::Gtk.VBox vbox4;
		
		private global::Gtk.Label label1;
		
		private global::Gtk.CheckButton matchThemeCheckbox;
		
		private global::Gtk.HBox hbox7;
		
		private global::Gtk.Label label5;
		
		private global::Gtk.ColorButton baseColorButton;
		
		private global::Gtk.Label label6;
		
		private global::Gtk.ColorButton textColorButton;
		
		private global::Gtk.HBox hbox9;
		
		private global::Gtk.Label GtkLabel13;
		
		private global::Gtk.FontButton fontbutton1;
		
		private global::Gtk.HSeparator hseparator3;
		
		private global::Gtk.HBox hbox1;
		
		private global::Gtk.Label label2;
		
		private global::Gtk.CheckButton checkCompilerUseDefault;
		
		private global::Gtk.Frame frame1;
		
		private global::Gtk.Table table2;
		
		private global::Gtk.Button buttonCompilerBrowse;
		
		private global::Gtk.Entry entryCompilerPath;
		
		private global::Gtk.Label label3;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget MonoDevelop.FSharp.Gui.FSharpSettingsWidget
			global::Stetic.BinContainer.Attach (this);
			this.Name = "MonoDevelop.FSharp.Gui.FSharpSettingsWidget";
			// Container child MonoDevelop.FSharp.Gui.FSharpSettingsWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox ();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox ();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label ();
			this.label4.Name = "label4";
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString ("<b>F# Interactive</b>");
			this.label4.UseMarkup = true;
			this.hbox2.Add (this.label4);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox2 [this.label4]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.checkInteractiveUseDefault = new global::Gtk.CheckButton ();
			this.checkInteractiveUseDefault.CanFocus = true;
			this.checkInteractiveUseDefault.Name = "checkInteractiveUseDefault";
			this.checkInteractiveUseDefault.Label = global::Mono.Unix.Catalog.GetString ("Use Default");
			this.checkInteractiveUseDefault.Active = true;
			this.checkInteractiveUseDefault.DrawIndicator = true;
			this.checkInteractiveUseDefault.UseUnderline = true;
			this.hbox2.Add (this.checkInteractiveUseDefault);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox2 [this.checkInteractiveUseDefault]));
			w2.Position = 1;
			this.vbox1.Add (this.hbox2);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hbox2]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.table5 = new global::Gtk.Table (((uint)(2)), ((uint)(3)), false);
			this.table5.Name = "table5";
			this.table5.RowSpacing = ((uint)(6));
			this.table5.ColumnSpacing = ((uint)(6));
			// Container child table5.Gtk.Table+TableChild
			this.buttonBrowse = new global::Gtk.Button ();
			this.buttonBrowse.CanFocus = true;
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.UseUnderline = true;
			this.buttonBrowse.Label = global::Mono.Unix.Catalog.GetString ("_Browse...");
			this.table5.Add (this.buttonBrowse);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table5 [this.buttonBrowse]));
			w4.LeftAttach = ((uint)(2));
			w4.RightAttach = ((uint)(3));
			w4.XPadding = ((uint)(8));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table5.Gtk.Table+TableChild
			this.entryArguments = new global::Gtk.Entry ();
			this.entryArguments.CanFocus = true;
			this.entryArguments.Name = "entryArguments";
			this.entryArguments.IsEditable = true;
			this.entryArguments.InvisibleChar = '●';
			this.table5.Add (this.entryArguments);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table5 [this.entryArguments]));
			w5.TopAttach = ((uint)(1));
			w5.BottomAttach = ((uint)(2));
			w5.LeftAttach = ((uint)(1));
			w5.RightAttach = ((uint)(2));
			w5.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table5.Gtk.Table+TableChild
			this.entryPath = new global::Gtk.Entry ();
			this.entryPath.CanFocus = true;
			this.entryPath.Name = "entryPath";
			this.entryPath.IsEditable = true;
			this.entryPath.InvisibleChar = '●';
			this.table5.Add (this.entryPath);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table5 [this.entryPath]));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table5.Gtk.Table+TableChild
			this.GtkLabel4 = new global::Gtk.Label ();
			this.GtkLabel4.Name = "GtkLabel4";
			this.GtkLabel4.Xalign = 0F;
			this.GtkLabel4.LabelProp = global::Mono.Unix.Catalog.GetString ("Path");
			this.GtkLabel4.UseMarkup = true;
			this.table5.Add (this.GtkLabel4);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table5 [this.GtkLabel4]));
			w7.XPadding = ((uint)(8));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table5.Gtk.Table+TableChild
			this.GtkLabel6 = new global::Gtk.Label ();
			this.GtkLabel6.Name = "GtkLabel6";
			this.GtkLabel6.Xalign = 0F;
			this.GtkLabel6.LabelProp = global::Mono.Unix.Catalog.GetString ("Options");
			this.GtkLabel6.UseMarkup = true;
			this.table5.Add (this.GtkLabel6);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table5 [this.GtkLabel6]));
			w8.TopAttach = ((uint)(1));
			w8.BottomAttach = ((uint)(2));
			w8.XPadding = ((uint)(8));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(0));
			this.vbox1.Add (this.table5);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.table5]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.advanceToNextLineCheckbox = new global::Gtk.CheckButton ();
			this.advanceToNextLineCheckbox.TooltipMarkup = "When sending a line or an empty selection to F# interactive this property automatically advances to the next line.";
			this.advanceToNextLineCheckbox.CanFocus = true;
			this.advanceToNextLineCheckbox.Name = "advanceToNextLineCheckbox";
			this.advanceToNextLineCheckbox.Label = global::Mono.Unix.Catalog.GetString ("Advance to next line");
			this.advanceToNextLineCheckbox.Active = true;
			this.advanceToNextLineCheckbox.DrawIndicator = true;
			this.advanceToNextLineCheckbox.UseUnderline = true;
			this.vbox1.Add (this.advanceToNextLineCheckbox);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.advanceToNextLineCheckbox]));
			w10.Position = 2;
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hseparator4 = new global::Gtk.HSeparator ();
			this.hseparator4.Name = "hseparator4";
			this.vbox1.Add (this.hseparator4);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hseparator4]));
			w11.Position = 3;
			w11.Expand = false;
			w11.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox ();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label ();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString ("<b>FSI Appearance</b>");
			this.label1.UseMarkup = true;
			this.vbox4.Add (this.label1);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox4 [this.label1]));
			w12.Position = 0;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.matchThemeCheckbox = new global::Gtk.CheckButton ();
			this.matchThemeCheckbox.CanFocus = true;
			this.matchThemeCheckbox.Name = "matchThemeCheckbox";
			this.matchThemeCheckbox.Label = global::Mono.Unix.Catalog.GetString ("Match with Theme");
			this.matchThemeCheckbox.Active = true;
			this.matchThemeCheckbox.DrawIndicator = true;
			this.matchThemeCheckbox.UseUnderline = true;
			this.vbox4.Add (this.matchThemeCheckbox);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox4 [this.matchThemeCheckbox]));
			w13.Position = 1;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox7 = new global::Gtk.HBox ();
			this.hbox7.Name = "hbox7";
			// Container child hbox7.Gtk.Box+BoxChild
			this.label5 = new global::Gtk.Label ();
			this.label5.Name = "label5";
			this.label5.Xalign = 0F;
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString ("Base Color");
			this.hbox7.Add (this.label5);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.hbox7 [this.label5]));
			w14.Position = 0;
			w14.Expand = false;
			w14.Fill = false;
			w14.Padding = ((uint)(8));
			// Container child hbox7.Gtk.Box+BoxChild
			this.baseColorButton = new global::Gtk.ColorButton ();
			this.baseColorButton.CanFocus = true;
			this.baseColorButton.Events = ((global::Gdk.EventMask)(784));
			this.baseColorButton.Name = "baseColorButton";
			this.hbox7.Add (this.baseColorButton);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.hbox7 [this.baseColorButton]));
			w15.Position = 1;
			w15.Expand = false;
			w15.Fill = false;
			w15.Padding = ((uint)(8));
			// Container child hbox7.Gtk.Box+BoxChild
			this.label6 = new global::Gtk.Label ();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString ("Text Color");
			this.hbox7.Add (this.label6);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox7 [this.label6]));
			w16.Position = 2;
			w16.Expand = false;
			w16.Fill = false;
			w16.Padding = ((uint)(8));
			// Container child hbox7.Gtk.Box+BoxChild
			this.textColorButton = new global::Gtk.ColorButton ();
			this.textColorButton.CanFocus = true;
			this.textColorButton.Events = ((global::Gdk.EventMask)(784));
			this.textColorButton.Name = "textColorButton";
			this.hbox7.Add (this.textColorButton);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.hbox7 [this.textColorButton]));
			w17.Position = 3;
			w17.Expand = false;
			w17.Fill = false;
			w17.Padding = ((uint)(8));
			this.vbox4.Add (this.hbox7);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.vbox4 [this.hbox7]));
			w18.Position = 2;
			w18.Expand = false;
			w18.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox9 = new global::Gtk.HBox ();
			this.hbox9.Name = "hbox9";
			this.hbox9.Spacing = 6;
			// Container child hbox9.Gtk.Box+BoxChild
			this.GtkLabel13 = new global::Gtk.Label ();
			this.GtkLabel13.Name = "GtkLabel13";
			this.GtkLabel13.LabelProp = global::Mono.Unix.Catalog.GetString ("Interactive Pad Font");
			this.GtkLabel13.UseMarkup = true;
			this.hbox9.Add (this.GtkLabel13);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.hbox9 [this.GtkLabel13]));
			w19.Position = 0;
			w19.Expand = false;
			w19.Fill = false;
			w19.Padding = ((uint)(8));
			// Container child hbox9.Gtk.Box+BoxChild
			this.fontbutton1 = new global::Gtk.FontButton ();
			this.fontbutton1.CanFocus = true;
			this.fontbutton1.Name = "fontbutton1";
			this.hbox9.Add (this.fontbutton1);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.hbox9 [this.fontbutton1]));
			w20.Position = 1;
			w20.Padding = ((uint)(8));
			this.vbox4.Add (this.hbox9);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.vbox4 [this.hbox9]));
			w21.Position = 3;
			w21.Expand = false;
			w21.Fill = false;
			this.vbox1.Add (this.vbox4);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.vbox4]));
			w22.Position = 4;
			w22.Expand = false;
			w22.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hseparator3 = new global::Gtk.HSeparator ();
			this.hseparator3.Name = "hseparator3";
			this.vbox1.Add (this.hseparator3);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hseparator3]));
			w23.Position = 5;
			w23.Expand = false;
			w23.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label ();
			this.label2.TooltipMarkup = "This is only used when xbuild is not being used.";
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString ("<b>F# Default Compiler</b>");
			this.label2.UseMarkup = true;
			this.hbox1.Add (this.label2);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.label2]));
			w24.Position = 0;
			w24.Expand = false;
			w24.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.checkCompilerUseDefault = new global::Gtk.CheckButton ();
			this.checkCompilerUseDefault.CanFocus = true;
			this.checkCompilerUseDefault.Name = "checkCompilerUseDefault";
			this.checkCompilerUseDefault.Label = global::Mono.Unix.Catalog.GetString ("Use Default");
			this.checkCompilerUseDefault.Active = true;
			this.checkCompilerUseDefault.DrawIndicator = true;
			this.checkCompilerUseDefault.UseUnderline = true;
			this.hbox1.Add (this.checkCompilerUseDefault);
			global::Gtk.Box.BoxChild w25 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.checkCompilerUseDefault]));
			w25.Position = 1;
			this.vbox1.Add (this.hbox1);
			global::Gtk.Box.BoxChild w26 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hbox1]));
			w26.Position = 6;
			w26.Expand = false;
			w26.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.frame1 = new global::Gtk.Frame ();
			this.frame1.Name = "frame1";
			this.frame1.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child frame1.Gtk.Container+ContainerChild
			this.table2 = new global::Gtk.Table (((uint)(1)), ((uint)(3)), false);
			this.table2.Name = "table2";
			this.table2.RowSpacing = ((uint)(6));
			this.table2.ColumnSpacing = ((uint)(6));
			// Container child table2.Gtk.Table+TableChild
			this.buttonCompilerBrowse = new global::Gtk.Button ();
			this.buttonCompilerBrowse.CanFocus = true;
			this.buttonCompilerBrowse.Name = "buttonCompilerBrowse";
			this.buttonCompilerBrowse.UseUnderline = true;
			this.buttonCompilerBrowse.Label = global::Mono.Unix.Catalog.GetString ("_Browse...");
			this.table2.Add (this.buttonCompilerBrowse);
			global::Gtk.Table.TableChild w27 = ((global::Gtk.Table.TableChild)(this.table2 [this.buttonCompilerBrowse]));
			w27.LeftAttach = ((uint)(2));
			w27.RightAttach = ((uint)(3));
			w27.XPadding = ((uint)(8));
			w27.XOptions = ((global::Gtk.AttachOptions)(4));
			w27.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.entryCompilerPath = new global::Gtk.Entry ();
			this.entryCompilerPath.CanFocus = true;
			this.entryCompilerPath.Name = "entryCompilerPath";
			this.entryCompilerPath.IsEditable = true;
			this.entryCompilerPath.InvisibleChar = '●';
			this.table2.Add (this.entryCompilerPath);
			global::Gtk.Table.TableChild w28 = ((global::Gtk.Table.TableChild)(this.table2 [this.entryCompilerPath]));
			w28.LeftAttach = ((uint)(1));
			w28.RightAttach = ((uint)(2));
			w28.XPadding = ((uint)(8));
			w28.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table2.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label ();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString ("Path");
			this.table2.Add (this.label3);
			global::Gtk.Table.TableChild w29 = ((global::Gtk.Table.TableChild)(this.table2 [this.label3]));
			w29.XPadding = ((uint)(8));
			w29.XOptions = ((global::Gtk.AttachOptions)(4));
			w29.YOptions = ((global::Gtk.AttachOptions)(4));
			this.frame1.Add (this.table2);
			this.vbox1.Add (this.frame1);
			global::Gtk.Box.BoxChild w31 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.frame1]));
			w31.Position = 7;
			w31.Expand = false;
			w31.Fill = false;
			this.Add (this.vbox1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
		}
	}
}
