// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 2.0.50727.1433
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace Rulers {
	
	
	// Should subclass MonoMac.AppKit.NSWindow
	[MonoMac.Foundation.Register("MyWindow")]
	public partial class MyWindow {
	}
	
	// Should subclass MonoMac.AppKit.NSWindowController
	[MonoMac.Foundation.Register("MyWindowController")]
	public partial class MyWindowController {
		
		private RectsView __mt_rectsView;
		
		#pragma warning disable 0169
		[MonoMac.Foundation.Connect("rectsView")]
		private RectsView rectsView {
			get {
				this.__mt_rectsView = ((RectsView)(this.GetNativeField("rectsView")));
				return this.__mt_rectsView;
			}
			set {
				this.__mt_rectsView = value;
				this.SetNativeField("rectsView", value);
			}
		}
	}
	
	// Should subclass MonoMac.AppKit.NSView
	[MonoMac.Foundation.Register("RectsView")]
	public partial class RectsView {
		
		#pragma warning disable 0169
		[MonoMac.Foundation.Export("lockSelectedItem:")]
		partial void lockSelectedItem (MonoMac.Foundation.NSObject sender);

		[MonoMac.Foundation.Export("nestle:")]
		partial void nestle (MonoMac.Foundation.NSObject sender);

		[MonoMac.Foundation.Export("zoomIn:")]
		partial void zoomIn (MonoMac.Foundation.NSObject sender);

		[MonoMac.Foundation.Export("zoomOut:")]
		partial void zoomOut (MonoMac.Foundation.NSObject sender);
}
}
