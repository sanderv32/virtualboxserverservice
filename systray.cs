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
 * Date: 6/18/2010
 * Time: 21:21:27
 * 
 */
using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace VBoxService
{
	/// <summary>
	/// Description of notifyicon.
	/// </summary>
	public class SysTrayIcon : ApplicationContext
	{
		private NotifyIcon notifyIcon;
		private VirtualBox.VirtualBox vbox;
		private System.Windows.Forms.ContextMenu menuitem;
		private StringBuilder extradatakey;
		private virtualboxcallback vboxcallback;
		private System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VBoxService));
				
		public SysTrayIcon()
		{
			this.extradatakey = new StringBuilder(resources.GetString("VBoxService.ExtraDataKey"));
			
			vbox = new VirtualBox.VirtualBox();
			vboxcallback = new virtualboxcallback(this);
			vbox.RegisterCallback(vboxcallback);
			
			this.menuitem = new System.Windows.Forms.ContextMenu();
			for(int i=0; i<((Array)vbox.Machines).Length;i++) {
				MenuItem menu = new MenuItem( vbox.Machines[i].Name);
				menu.Name = vbox.Machines[i].Name;
				menu.Click += contextClick;
				menu.Tag = vbox.Machines[i].Id;
				
				if (vbox.Machines[i].GetExtraData(this.extradatakey.ToString()).ToLower() == "yes")
					menu.Checked = true;
				else
					menu.Checked = false;
				
				this.menuitem.MenuItems.Add(menu);
			}
			this.menuitem.MenuItems.Add("-");
			this.menuitem.MenuItems.Add("&About",AboutBox);
			this.menuitem.MenuItems.Add("E&xit",ExitSystray);
					
			this.notifyIcon = new NotifyIcon();
			this.notifyIcon.Icon = (System.Drawing.Icon)resources.GetObject("icon");
			this.notifyIcon.Visible = true;
			this.notifyIcon.Text = resources.GetString("Application.Name");
			this.notifyIcon.ContextMenu = this.menuitem;
		}
		
		public void ChkMenuItem(string uuid, int reg)
		{
			int index;
			
			if (reg==1) {
				/// <summary>
				///  Find the break-line before the exit
				/// </summary>
				for(index=0; index<this.menuitem.MenuItems.Count;index++) {
					if (this.menuitem.MenuItems[index].Text=="-") break;
				}
				
				MenuItem menu = new MenuItem(vbox.GetMachine(uuid).Name);
				menu.Click += contextClick;
				menu.Tag = uuid;
				this.menuitem.MenuItems.Add(index,menu);
			} else {
				for(int i=0; i<this.menuitem.MenuItems.Count; i++) {
					if ((string)this.menuitem.MenuItems[i].Tag==(string)uuid) {
						this.menuitem.MenuItems.Remove(this.menuitem.MenuItems[i]);
					}
				}
			}
		}
		
		public void Run()
		{
			Application.Run();
		}
		
		private void AboutBox(object Sender, EventArgs e)
		{
			About aboutbox = new About();
			aboutbox.Icon  = new Icon((Icon)this.resources.GetObject("icon"),((Icon)this.resources.GetObject("icon")).Size);
			aboutbox.pictureBox1.Image = new Bitmap((Bitmap)((Icon)resources.GetObject("icon")).ToBitmap());
			aboutbox.label1.Text = String.Format(this.resources.GetString("About.Text"),
			                                     this.resources.GetString("Application.Name"),
			                                     Assembly.GetExecutingAssembly().GetName().Version.ToString());
			aboutbox.Show();
		}
		
		private void ExitSystray(object Sender, EventArgs e)
		{
			this.notifyIcon.Visible = false;
			Application.Exit();
		}
		
		private void contextClick(object Sender, EventArgs e)
		{
			VirtualBox.IMachine machine;
			
			Console.WriteLine(((MenuItem)Sender).Tag);
			((MenuItem)Sender).Checked = ((MenuItem)Sender).Checked ? false: true;
			machine = vbox.GetMachine((string)((MenuItem)Sender).Tag);
			if (((MenuItem)Sender).Checked)
				machine.SetExtraData(this.extradatakey.ToString(), "yes");
			else
				machine.SetExtraData(this.extradatakey.ToString(), "");

		}
	}
}