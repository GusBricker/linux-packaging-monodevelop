using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.Gui.Dialogs {
	
	public class SplashScreenForm : Gtk.Window, IProgressMonitor, IDisposable
	{
		static SplashScreenForm splashScreen;
		static ProgressBar progress;
		static VBox vbox;
		ProgressTracker tracker = new ProgressTracker ();
		Gdk.Pixbuf bitmap;
		static Gtk.Label label;
		
		public static SplashScreenForm SplashScreen {
			get {
				if (splashScreen == null)
					splashScreen = new SplashScreenForm();
				return splashScreen;
			}
		}
		
		public SplashScreenForm () : base (Gtk.WindowType.Toplevel)
		{
			AppPaintable = true;
			this.Decorated = false;
			this.WindowPosition = WindowPosition.Center;
			this.TypeHint = Gdk.WindowTypeHint.Splashscreen;
			try {
				bitmap = new Gdk.Pixbuf (Assembly.GetCallingAssembly(), "SplashScreen.png");
			} catch (Exception e) {
				LoggingService.LogError ("Can't load splash screen pixbuf 'SplashScreen.png'.", e);
			}
			progress = new ProgressBar();
			progress.Fraction = 0.00;
			progress.HeightRequest = 6;

			vbox = new VBox();
			vbox.BorderWidth = 12;
			label = new Gtk.Label ();
			label.UseMarkup = true;
			label.Xalign = 0;
			vbox.PackEnd (progress, false, true, 0);
			vbox.PackEnd (label, false, true, 3);
			this.Add (vbox);
			if (bitmap != null)
				this.Resize (bitmap.Width, bitmap.Height);
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (bitmap != null) {
				bitmap.Dispose ();
				bitmap = null;
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			if (bitmap != null) {
				Gdk.GC gc = Style.LightGC (StateType.Normal);
				GdkWindow.DrawPixbuf (gc, bitmap, 0, 0, 0, 0, bitmap.Width, bitmap.Height, Gdk.RgbDither.None, 0, 0);

				using (Pango.Layout pl = new Pango.Layout (PangoContext)) {
					Pango.FontDescription des = this.Style.FontDescription.Copy();
					pl.FontDescription = des;
					//pl.SetMarkup("<b><span foreground='#cccccc'>" + BuildVariables.PackageVersionLabel + "</span></b>");
					int w,h;
					pl.GetPixelSize (out w, out h);
					GdkWindow.DrawLayout (gc, bitmap.Width - w - 75, 90, pl);
					des.Dispose ();
				}
			}
			return base.OnExposeEvent (evt);
		}
		
		public static void SetProgress (double Percentage)
		{
			progress.Fraction = Percentage;
			RunMainLoop ();
		}

		public void SetMessage (string Message)
		{
			if (bitmap == null) {
				label.Text = Message;
			} else {
				label.Markup = "<span size='small' foreground='white'>" + Message + "</span>";
			}
			RunMainLoop ();
		}
		
		static void RunMainLoop ()
		{
			DispatchService.RunPendingEvents ();
		}
		
		void IProgressMonitor.BeginTask (string name, int totalWork)
		{
			tracker.BeginTask (name, totalWork);
			SetMessage (tracker.CurrentTask);
		}
		
		void IProgressMonitor.BeginStepTask (string name, int totalWork, int stepSize)
		{
			tracker.BeginStepTask (name, totalWork, stepSize);
			SetMessage (tracker.CurrentTask);
		}
		
		void IProgressMonitor.EndTask ()
		{
			tracker.EndTask ();
			SetProgress (tracker.GlobalWork);
			SetMessage (tracker.CurrentTask);
		}
		
		void IProgressMonitor.Step (int work)
		{
			tracker.Step (work);
			SetProgress (tracker.GlobalWork);
		}
		
		TextWriter IProgressMonitor.Log {
			get { return Console.Out; }
		}
		
		void IProgressMonitor.ReportWarning (string message)
		{
		}
		
		void IProgressMonitor.ReportSuccess (string message)
		{
		}
		
		void IProgressMonitor.ReportError (string message, Exception exception)
		{
		}
		
		bool IProgressMonitor.IsCancelRequested {
			get { return false; }
		}
		
		public event MonitorHandler CancelRequested {
			add { }
			remove { }
		}
		
		// The returned IAsyncOperation object must be thread safe
		IAsyncOperation IProgressMonitor.AsyncOperation {
			get { return null; }
		}
		
		object IProgressMonitor.SyncRoot {
			get { return this; }
		}
		
		void IDisposable.Dispose ()
		{
			Destroy ();
			splashScreen = null;
		}
	}
}
