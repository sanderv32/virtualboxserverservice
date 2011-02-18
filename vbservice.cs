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
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO.Pipes;

using VirtualBox;

namespace VBoxService
{
	sealed class VBoxService : ServiceBase
	{
		private Thread t;
		private VirtualBox.IMachine[] machines;
		private Dictionary<string, VirtualBox.IVRDEServerInfo> display=new Dictionary<string,VirtualBox.IVRDEServerInfo>();
		private bool isStopped = false;
		private VirtualBox.VirtualBox vbox;
		private System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VBoxService));
		private StringBuilder extradatakey;
		private NamedPipeServerStream pipeStream;
		private StringBuilder pipeName;

		/// <summary>
		/// Public Constructor for WindowsService.
		/// - Put all of your Initialization code here.
		/// </summary>
		public VBoxService()
		{
			this.ServiceName = resources.GetString("Application.Name");
			this.EventLog.Log = "Application";
			this.extradatakey = new StringBuilder(resources.GetString("VBoxService.ExtraDataKey"));
			this.pipeName = new StringBuilder(resources.GetString("Pipe.Name"));
            
			// These Flags set whether or not to handle that specific
			//  type of event. Set to true if you need it, false otherwise.
			this.CanHandlePowerEvent = true;
			this.CanHandleSessionChangeEvent = true;
			this.CanPauseAndContinue = true;
			this.CanShutdown = true;
			this.CanStop = true;
			
			vbox = new VirtualBox.VirtualBox();
			machines = vbox.Machines;			
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

		#region Quick fix to stop WaitForConnection
		private void closePipe()
		{
			NamedPipeClientStream pipe = new NamedPipeClientStream(pipeName.ToString());
			try {
				pipe.Connect();
				pipe.Close();
			} catch {
			}
		}
		#endregion
		
		#region Start and Stop VM procedures
		/// <summary>
		/// Start a specific VM
		/// </summary>
		/// <param name="uuid">UUID of machine to start</param>
		private void startvm(string uuid)
		{
			VirtualBox.IMachine m = vbox.FindMachine(uuid);
			string xtrakeys = m.GetExtraData(this.extradatakey.ToString());
			if (xtrakeys.ToLower() == "yes") {
				if (m.State==VirtualBox.MachineState.MachineState_PoweredOff || m.State==VirtualBox.MachineState.MachineState_Saved) {
					this.EventLog.WriteEntry(String.Format("Starting VM {0} ({1})",m.Name,m.Id));
					VirtualBox.Session session = new VirtualBox.Session();
					try {
						VirtualBox.IProgress progress = m.LaunchVMProcess(session, "vrdp", "");
						progress.WaitForCompletion(-1);
						this.display.Add(uuid, session.Console.VRDEServerInfo);
					} catch (Exception e) {
						this.EventLog.WriteEntry(String.Format("Error starting VM {0} ({1})\r\n\r\n{2}",m.Name,m.Id,e.ToString()),EventLogEntryType.Error);
					}
				}
			}
		}
		
		/// <summary>
		/// Stop a specific VM
		/// </summary>
		/// <param name="uuid">UUID of machine to stop</param>
		private void stopvm(string uuid)
		{
			VirtualBox.IMachine m = vbox.FindMachine(uuid);
			string xtrakeys = m.GetExtraData(this.extradatakey.ToString());
			if (xtrakeys.ToLower() == "yes") {
				if (m.State==VirtualBox.MachineState.MachineState_Running) {
					this.EventLog.WriteEntry(String.Format("Stopping VM {0} ({1})",m.Name,m.Id));
					VirtualBox.Session session = new VirtualBox.Session();
					try {
						m.LockMachine(session, VirtualBox.LockType.LockType_Shared);
						session.Console.PowerDown().WaitForCompletion(-1);
						session.UnlockMachine();
						this.display.Remove(uuid);
					} catch (Exception e) {
						this.EventLog.WriteEntry(String.Format("Error stopping VM {0} ({1})\r\n\r\n{2}\r\n\r\n{3}",m.Name,m.Id,e.ToString(),m.State),EventLogEntryType.Error);
					}
				}
			}
	}
		
		/// <summary>
		/// Start the VM's where the extradata key is set
		/// </summary>
		private void startvms() 
		{
			if (machines.Length == 0) return;
			foreach(VirtualBox.IMachine m in machines) {
				string xtrakeys = m.GetExtraData(this.extradatakey.ToString());
				if (xtrakeys.ToLower() == "yes") {
					this.startvm(m.Id);
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
					this.stopvm(m.Id);
				}
			}
		}
		#endregion

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
						Console.WriteLine("Service installed");
					} catch {
						Console.WriteLine("Unable to install server.");
					}
				} else if (args[0] == "-uninstall") {
					try {
						System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/u", "/LogToConsole=false", Assembly.GetExecutingAssembly().Location }); 
						Console.WriteLine("Service uninstalled");
					} catch {
						Console.WriteLine("Unable to uninstall server.");
					}
				} else if (args[0] == "-console") {
					VBoxService vb = new VBoxService();
					/*while(true) {
						Thread.Sleep(10000);
					}*/
					vb.Start();
				} else 	if (args[0] == "-tray") {
						Console.Title = "VirtualBox Server Service TrayIcon";
						ShowWindow(ThisConsole, HIDE);
						SysTrayIcon systrayicon = new SysTrayIcon();
						systrayicon.Run();
						ShowWindow(ThisConsole, RESTORE);
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
			this.t = new Thread(new ThreadStart(this.Start));
			this.t.Start();

			base.OnStart(args);
		}

		/// <summary>
		/// OnStop(): Stop the service and created threads
		/// </summary>
		protected override void OnStop()
		{
			this.isStopped=true;
			this.stopvms();
			this.closePipe();
			this.t.Join(new TimeSpan(0,0,30));
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
			this.closePipe();
			this.t.Join(new TimeSpan(0,0,30));
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
				using (pipeStream = new NamedPipeServerStream(pipeName.ToString(),PipeDirection.InOut,1,PipeTransmissionMode.Message,PipeOptions.None))
				{
					Byte[] bytes = new Byte[64];
					ASCIIEncoding encoding = new ASCIIEncoding();

					pipeStream.WaitForConnection();
					
					pipeStream.Read(bytes, 0, bytes.Length);
					string strMachine = encoding.GetString(bytes,5,bytes.Length-5).TrimEnd('\0');
					switch (encoding.GetString(bytes).ToLower().Substring(0,5)) {
						// Start VM
						case "start":
#if DEBUG
							Console.WriteLine("Received start for machine {0}",strMachine);
#endif
							this.startvm(strMachine);
							break;
						// Stop VM
						case "stop ":
#if DEBUG
							Console.WriteLine("Received stop for machine {0}",strMachine);
#endif
							this.stopvm(strMachine);
							break;
						case "vrdp ":
#if DEBUG
							Console.WriteLine("Received vrdp for machine {0}", strMachine);
#endif
							try {
								bytes=BitConverter.GetBytes(display[strMachine].Port);
								Buffer.BlockCopy(BitConverter.GetBytes(display[strMachine].Active),0,bytes,2,2);
								pipeStream.Write(bytes, 0, 4);
#if DEBUG
								this.EventLog.WriteEntry(string.Format("Key={0}, Port={1}",strMachine,display[strMachine].Port));
#endif
							} catch {
							}
							break;
						default:
							
							break;
					}
				
					//Thread.Sleep(1000);
				}
				pipeStream.Close();
				pipeStream.Dispose();
			}
		}
	}
}
