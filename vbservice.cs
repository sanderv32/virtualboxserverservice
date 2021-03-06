#region GPL v2 License
// one line to give the program's name and an idea of what it does.
// Copyright (C) yyyy  name of author
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
#endregion

/**
 * VirtualBox Server Service v2.0
 *
 * Version 2 uses the COM+ service of windows to start machines.
 */

using System;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using VirtualBox;

namespace VBoxService
{
	sealed class VBoxService : ServiceBase
	{
		private Thread t;
		private Array machines;
		private bool isStopped = false;
		private VirtualBox.VirtualBox vbox;
		private System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VBoxService));
		private StringBuilder extradatakey;

		/// <summary>
		/// Public Constructor for WindowsService.
		/// - Put all of your Initialization code here.
		/// </summary>
		public VBoxService()
		{
			this.ServiceName = resources.GetString("Application.Name");
			this.EventLog.Log = "Application";
			this.extradatakey = new StringBuilder(resources.GetString("VBoxService.ExtraDataKey"));
            
			// These Flags set whether or not to handle that specific
			//  type of event. Set to true if you need it, false otherwise.
			this.CanHandlePowerEvent = true;
			this.CanHandleSessionChangeEvent = true;
			this.CanPauseAndContinue = true;
			this.CanShutdown = true;
			this.CanStop = true;
			
			vbox = new VirtualBox.VirtualBox();
			machines = (Array)vbox.Machines;
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~VBoxService()
		{
		}

		#region Console Window property stuff
		[DllImport("kernel32.dll", ExactSpelling=true)]
		private static extern IntPtr GetConsoleWindow();

		private static IntPtr ThisConsole=GetConsoleWindow();
        
		[DllImport("user32.dll",CharSet=CharSet.Auto, SetLastError=true)]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		private const int HIDE=0;
		private const int MAXIMIZE=3;
		private const int MINIMIZE=6;
		private const int RESTORE=9;
		#endregion

		/// <summary>
		/// Start the VM's where the extradata key is set
		/// </summary>
		private void startvms() 
		{
			if (machines.Length == 0) return;
			foreach(VirtualBox.IMachine m in machines) {
				string xtrakeys = m.GetExtraData(this.extradatakey.ToString());
				if (xtrakeys.ToLower() == "yes") {
					if (m.State==VirtualBox.MachineState.MachineState_PoweredOff || m.State==VirtualBox.MachineState.MachineState_Saved) {
						this.EventLog.WriteEntry(String.Format("Starting VM {0} ({1})",m.Name,m.Id));
						VirtualBox.Session session = new VirtualBox.Session();
						try {
							VirtualBox.IProgress progress = m.Parent.OpenRemoteSession(session, m.Id, "vrdp", "");
							progress.WaitForCompletion(-1);
						} catch (Exception e) {
							this.EventLog.WriteEntry(String.Format("Error starting VM {0} ({1})\r\n\r\n{2}",m.Name,m.Id,e.ToString()),EventLogEntryType.Error);
						}
					}
				}
			}
		}

		/// <summary>
		/// Stop the VM's where the extradata key is set
		/// </summary>
		private void stopvms() 
		{
			if (machines.Length  == 0) return;
			foreach(VirtualBox.IMachine m in machines) {
				string xtrakeys = m.GetExtraData(this.extradatakey.ToString());
				if (xtrakeys.ToLower() == "yes") {
					if (m.State==VirtualBox.MachineState.MachineState_Running) {
						this.EventLog.WriteEntry(String.Format("Stopping VM {0} ({1})",m.Name,m.Id));
						VirtualBox.Session session = new VirtualBox.Session();
						try {
							m.Parent.OpenExistingSession(session, m.Id);
							session.Console.PowerDown().WaitForCompletion(-1);
							session.Close();
						} catch (Exception e) {
							this.EventLog.WriteEntry(String.Format("Error stopping VM {0} ({1})\r\n\r\n{2}\r\n\r\n{3}",m.Name,m.Id,e.ToString(),m.State),EventLogEntryType.Error);
						}
					}
				}
			}
		}

		/// <summary>
		/// The Main Thread: This is where the Service is Run.
		/// </summary>
		static void Main(string[] args)
		{
			try {
				VirtualBox.VirtualBox vbx = new VirtualBox.VirtualBox();
			} catch {
				MessageBox.Show("Unable to open Virtualbox DCOM object.\r\nBe sure that Virtualbox is installed!",
				           "DCOM error!",
				           MessageBoxButtons.OK);
				return;
			}
			if (args.Length>0) {
				if (args[0] == "-install") {
					try {
						System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/LogToConsole=false", Assembly.GetExecutingAssembly().Location }); 
					} catch {
						Console.WriteLine("Unable to install server.");
					}
				} else if (args[0] == "-uninstall") {
					try {
						System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/u", "/LogToConsole=false", Assembly.GetExecutingAssembly().Location }); 
					} catch {
						Console.WriteLine("Unable to uninstall server.");
					}
				} else if (args[0] == "-console") {
					//vbx = new VirtualBox.VirtualBox();
					VBoxService vb = new VBoxService();
					while(true) {
						Thread.Sleep(10000);
					}
				} else 	if (args[0] == "-tray") {
						Console.Title = "VirtualBox Server Service TrayIcon";
#if !DEBUG
						ShowWindow(ThisConsole, HIDE);
#endif
						SysTrayIcon systrayicon = new SysTrayIcon();
						systrayicon.Run();
#if DEBUG
						ShowWindow(ThisConsole, RESTORE);
#endif
					}
			} else	{
				ServiceBase.Run(new VBoxService());
			}
		}

		/// <summary>
		/// Dispose of objects that need it here.
		/// </summary>
		/// <param name="disposing">Whether
		///    or not disposing is going on.</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		/// <summary>
		/// OnStart(): Startup new thread for this service
		/// </summary>
		/// <param name="args"></param>
		protected override void OnStart(string[] args)
		{
			this.isStopped = false;
			t = new Thread(new ThreadStart(this.Start));
			t.Start();

			base.OnStart(args);
		}

		/// <summary>
		/// OnStop(): Stop the service and created threads
		/// </summary>
		protected override void OnStop()
		{
			this.isStopped=true;
			this.stopvms();
			t.Join(new TimeSpan(0,0,30));
			base.OnStop();
		}

		/// <summary>
		/// OnPause: Put your pause code here
		/// - Pause working threads, etc.
		/// </summary>
		protected override void OnPause()
		{
			base.OnPause();
		}

		/// <summary>
		/// OnContinue(): Put your continue code here
		/// - Un-pause working threads, etc.
		/// </summary>
		protected override void OnContinue()
		{
			base.OnContinue();
		}

		/// <summary>
		/// OnShutdown(): If system is been shutdown, stop VM's and created threads
		/// </summary>
		protected override void OnShutdown()
		{
			this.isStopped=true;
			this.stopvms();
			t.Join(new TimeSpan(0,0,30));
			base.OnShutdown();
		}

		/// <summary>
		/// OnCustomCommand(): 
		/// </summary>
		/// <param name="command">Arbitrary Integer between 128 & 256</param>
		protected override void OnCustomCommand(int command)
		{
			//  A custom command can be sent to a service by using this method:
			//#  int command = 128; //Some Arbitrary number between 128 & 256
			//#  ServiceController sc = new ServiceController("NameOfService");
			//#  sc.ExecuteCommand(command);

			base.OnCustomCommand(command);
		}

		/// <summary>
		/// OnPowerEvent(): Useful for detecting power status changes,
		///   such as going into Suspend mode or Low Battery for laptops.
		/// </summary>
		/// <param name="powerStatus">The Power Broadcast Status
		/// (BatteryLow, Suspend, etc.)</param>
		protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
		{
			return base.OnPowerEvent(powerStatus);
		}

		/// <summary>
		/// OnSessionChange(): To handle a change event
		///   from a Terminal Server session.
		///   Useful if you need to determine
		///   when a user logs in remotely or logs off,
		///   or when someone logs into the console.
		/// </summary>
		/// <param name="changeDescription">The Session Change
		/// Event that occured.</param>
		protected override void OnSessionChange(
			SessionChangeDescription changeDescription)
		{
			base.OnSessionChange(changeDescription);
		}

		/// <summary>
		/// </summary>
		public void Start()
		{
			this.startvms();
			while(!this.isStopped)
			{
				Thread.Sleep(1000);
			}
		}
	}
}
