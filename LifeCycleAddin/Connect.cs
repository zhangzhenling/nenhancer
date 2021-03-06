using System;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using EnvDTE90;
using Extensibility;

namespace LifeCycleAddin
{
	/// <summary>The object for implementing an Add-in.</summary>
	public class Connect : IDTExtensibility2
	{
		public Connect()
		{
		}

		/// <summary>
        /// Receives notification that the Add-in is being loaded.
        /// </summary>
		public void OnConnection(object application, 
            ext_ConnectMode connectMode, 
            object addInInst, 
            ref Array custom)
		{
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;

            MessageBox.Show(string.Format("Event: OnConnection, connectMode: {0}", connectMode));
		}

		/// <summary>
        /// Receives notification that the Add-in is being unloaded.
        /// </summary>
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
            MessageBox.Show(string.Format("Event: OnDisconnection, connectMode: {0}", disconnectMode));
		}

		/// <summary>
        /// Receives notification when the collection of Add-ins has changed.
        /// </summary>
		public void OnAddInsUpdate(ref Array custom)
		{
            MessageBox.Show("OnAddInsUpdate");
		}

		/// <summary>
        /// Receives notification that the host application has completed loading.
        /// </summary>
		public void OnStartupComplete(ref Array custom)
		{
            MessageBox.Show("OnStartupComplete");
		}

		/// <summary>
        /// Receives notification that the host application is being unloaded.
        /// </summary>
		public void OnBeginShutdown(ref Array custom)
		{
            MessageBox.Show("OnBeginShutdown");
		}
        private DTE2 _applicationObject;
        private AddIn _addInInstance;
	}
}