//
// NSUrlConnection.cs:
// Author:
//   Miguel de Icaza
//

using System;
using System.Reflection;
using System.Collections;
using System.Runtime.InteropServices;

using MonoMac.ObjCRuntime;

namespace MonoMac.Foundation {

	public partial class NSUrlConnection {
                static Selector selSendSynchronousRequestReturningResponseError = new Selector ("sendSynchronousRequest:returningResponse:error:");
		
		public unsafe static NSData SendSynchronousRequest (NSUrlRequest request, out NSUrlResponse response, out NSError error)
		{
			IntPtr responseStorage = IntPtr.Zero;
			IntPtr errorStorage = IntPtr.Zero;

			void *resp = &responseStorage;
			void *errp = &errorStorage;
			IntPtr rhandle = (IntPtr) resp;
			IntPtr ehandle = (IntPtr) errp;
			
			var res = Messaging.IntPtr_objc_msgSend_IntPtr_IntPtr_IntPtr (
				class_ptr,
				selSendSynchronousRequestReturningResponseError.Handle,
				request.Handle,
				rhandle,
				ehandle);

			if (responseStorage != IntPtr.Zero)
				response = (NSUrlResponse) Runtime.GetNSObject (responseStorage);
			else
				response = null;

			if (errorStorage != IntPtr.Zero)
				error = (NSError) Runtime.GetNSObject (errorStorage);
			else
				error = null;
			
			return (NSData) Runtime.GetNSObject (res);
		}
	}
}
