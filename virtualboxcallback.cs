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

/*
 * Created by SharpDevelop.
 * User: Alexander
 * Date: 6/24/2010
 * Time: 22:02:16
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace VBoxService
{
	/// <summary>
	/// Description of virtualboxcallback.
	/// </summary>
	public class virtualboxcallback : VirtualBox.IVirtualBoxCallback
	{	
		private SysTrayIcon systraycb;
		
		/// <summary>
		/// Constructor
		/// </summary>
		public virtualboxcallback(SysTrayIcon systray)
		{
			systraycb = systray;
		}
		
		/// <summary>
		/// Destructor
		/// </summary>
		~virtualboxcallback()
		{
		}
		
		public int OnExtraDataCanChange(string uuid, string key, string val, out string error)
		{
			error="";
			return 1;
		}
		
		public void OnExtraDataChange(string uuid, string key, string val)
		{
		}
		
		public void OnGuestPropertyChange(string uuid, string name, string val, string flags)
		{
		}
		
		public void OnMachineDataChange(string uuid)
		{
		}
		
		public void OnMachineRegistered(string uuid, int registered)
		{
			systraycb.ChkMenuItem(uuid, registered);
		}
	
		public void OnMachineStateChange(string uuid, VirtualBox.MachineState state)
		{
		}
		
		public void OnMediumRegistered(string uuid, VirtualBox.DeviceType mediumtype, int registered)
		{
		}
		
		public void OnSessionStateChange(string uuid, VirtualBox.SessionState state)
		{
		}
		
		public void OnSnapshotChange(string uuid, string snapuuid)
		{
		}
		
		public void OnSnapshotDeleted(string uuid, string snapuuid)
		{
		}
		
		public void OnSnapshotTaken(string uuid, string snapuuid)
		{
		}
		
	}
}
