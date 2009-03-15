#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace MSNPSharp
{
    #region ServiceOperationFailedEvent

    public class ServiceOperationFailedEventArgs : EventArgs
    {
        private string method;
        private Exception exc;

        public ServiceOperationFailedEventArgs(string methodname, Exception ex)
        {
            method = methodname;
            exc = ex;
        }

        public string Method
        {
            get
            {
                return method;
            }
        }
        public Exception Exception
        {
            get
            {
                return exc;
            }
        }
    }

    #endregion

    /// <summary>
    /// Redirect service URL
    /// </summary>
    public static class RDRServiceHost
    {
        private static string omegaContactRDRServiceHost = @"byrdr.omega.contacts.msn.com";
        private static string storageRDRServiceHost = @"tkrdr.storage.msn.com";

        /// <summary>
        /// Redirection host for service on *.omega.contacts.msn.com
        /// </summary>
        public static string OmegaContactRDRServiceHost
        {
            get { return RDRServiceHost.omegaContactRDRServiceHost; }
        }

        /// <summary>
        /// Redirection host for service on *.storage.msn.com
        /// </summary>
        public static string StorageRDRServiceHost
        {
            get { return RDRServiceHost.storageRDRServiceHost; }
        }
    }

    /// <summary>
    /// Base class of webservice-related classes
    /// </summary>
    public abstract class MSNService
    {
        private NSMessageHandler nsMessageHandler;
        private WebProxy webProxy;

        private MSNService()
        {
        }

        protected MSNService(NSMessageHandler nsHandler)
        {
            nsMessageHandler = nsHandler;
            if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
            {
                webProxy = nsMessageHandler.ConnectivitySettings.WebProxy;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        public WebProxy WebProxy
        {
            get
            {
                return webProxy;
            }
        }

        /// <summary>
        /// Fired when request to an async webservice method failed.
        /// </summary>
        public event EventHandler<ServiceOperationFailedEventArgs> ServiceOperationFailed;

        /// <summary>
        /// Fires ServiceOperationFailed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnServiceOperationFailed(object sender, ServiceOperationFailedEventArgs e)
        {
            if (ServiceOperationFailed != null)
                ServiceOperationFailed(sender, e);
        }
    }
};