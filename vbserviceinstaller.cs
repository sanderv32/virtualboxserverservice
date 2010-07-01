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

			this.Installers.Add(serviceProcessInstaller);
			this.Installers.Add(serviceInstaller);
		}
	}
}
