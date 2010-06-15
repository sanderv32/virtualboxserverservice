using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace VBoxService
{
	[RunInstaller(true)]
	public class VBoxServiceInstaller : Installer
	{
		/// <summary>
		/// Public Constructor for VBoxServiceInstaller.
		/// </summary>
		public VBoxServiceInstaller()
		{
			ServiceProcessInstaller serviceProcessInstaller = 
				new ServiceProcessInstaller();
			ServiceInstaller serviceInstaller = new ServiceInstaller();

			//# Service Account Information

			serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
			serviceProcessInstaller.Username = null;
			serviceProcessInstaller.Password = null;

			//# Service Information

			serviceInstaller.DisplayName = "Virtualbox Server Service";
			serviceInstaller.StartType = ServiceStartMode.Automatic;
			serviceInstaller.ServiceName = "Virtualbox Server Service";
			serviceInstaller.Description = "Start a Virtualbox machine as a service";
			serviceInstaller.DelayedAutoStart = true;


			this.Installers.Add(serviceProcessInstaller);
			this.Installers.Add(serviceInstaller);
		}
	}
}
