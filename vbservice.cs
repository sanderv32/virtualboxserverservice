using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Reflection;
using System.Threading;
using VirtualBox;

namespace VBoxService
{
	class VBoxService : ServiceBase
	{
		private bool isStopped = false;
		private VirtualBox.Control vbox;

		/// <summary>
		/// Public Constructor for WindowsService.
		/// - Put all of your Initialization code here.
		/// </summary>
		public VBoxService()
		{
			this.ServiceName = "VirtualBox Server Service";
			this.EventLog.Log = "Application";
            
			// These Flags set whether or not to handle that specific
			//  type of event. Set to true if you need it, false otherwise.
			this.CanHandlePowerEvent = true;
			this.CanHandleSessionChangeEvent = true;
			this.CanPauseAndContinue = true;
			this.CanShutdown = true;
			this.CanStop = true;

			vbox = new VirtualBox.Control();
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~VBoxService()
		{
		}

		/// <summary>
		/// The Main Thread: This is where your Service is Run.
		/// </summary>
		static void Main(string[] args)
		{
			VirtualBox.Control vbx;
		
			if (args.Length>0) {
				if (args[0] == "-install") {
					try {
						System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/LogToConsole=false", Assembly.GetExecutingAssembly().Location }); 
					} catch (Exception e) {
						Console.WriteLine("Unable to install server.");
					}
				} else if (args[0] == "-uninstall") {
					try {
						System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/u", "/LogToConsole=false", Assembly.GetExecutingAssembly().Location }); 
					} catch (Exception e) {
						Console.WriteLine("Unable to uninstall server.");
					}
				} else if (args[0] == "-console") {
					vbx = new VirtualBox.Control();
					vbx.startvms();
					while(true) {
						Thread.Sleep(10000);
					}
				}
			} else
				ServiceBase.Run(new VBoxService());
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
		/// OnStart(): Put startup code here
		///  - Start threads, get inital data, etc.
		/// </summary>
		/// <param name="args"></param>
		protected override void OnStart(string[] args)
		{
			this.isStopped = false;
			Thread t = new Thread(new ThreadStart(this.Start));
			t.Start();

			base.OnStart(args);
		}

		/// <summary>
		/// OnStop(): Put your stop code here
		/// - Stop threads, set final data, etc.
		/// </summary>
		protected override void OnStop()
		{
			this.isStopped=true;
			vbox.stopvms();
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
		/// OnShutdown(): Called when the System is shutting down
		/// - Put code here when you need special handling
		///   of code that deals with a system shutdown, such
		///   as saving special data before shutdown.
		/// </summary>
		protected override void OnShutdown()
		{
			this.isStopped=true;
			vbox.stopvms();
			base.OnShutdown();
		}

		/// <summary>
		/// OnCustomCommand(): If you need to send a command to your
		///   service without the need for Remoting or Sockets, use
		///   this method to do custom methods.
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
			vbox.startvms();
			while(!this.isStopped)
			{
				Thread.Sleep(1000);
			}
		}
	}
}
