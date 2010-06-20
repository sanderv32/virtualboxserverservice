/*
 * Created by SharpDevelop.
 * User: Alexander
 * Date: 6/18/2010
 * Time: 21:21:27
 * 
 */
using System;
using System.Text;
using System.Windows.Forms;

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
		
		public SysTrayIcon()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VBoxService));
			this.extradatakey = new StringBuilder(resources.GetString("VBoxService.ExtraDataKey"));
			
			vbox = new VirtualBox.VirtualBox();
			
			this.menuitem = new System.Windows.Forms.ContextMenu();
			for(int i=0; i<((Array)vbox.Machines).Length;i++) {
				MenuItem menu = new MenuItem( vbox.Machines[i].Name);
				menu.Click += contextClick;
				menu.Tag = vbox.Machines[i].Id;
				
				if (vbox.Machines[i].GetExtraData(this.extradatakey.ToString()).ToLower() == "yes")
					menu.Checked = true;
				else
					menu.Checked = false;
				
				this.menuitem.MenuItems.Add(menu);
			}
			this.menuitem.MenuItems.Add("-");
			this.menuitem.MenuItems.Add("E&xit",ExitSystray);
			
			this.notifyIcon = new NotifyIcon();
			this.notifyIcon.Icon = (System.Drawing.Icon)resources.GetObject("earth");
			this.notifyIcon.Visible = true;
			this.notifyIcon.Text = resources.GetString("Application.Name");
			this.notifyIcon.ContextMenu = this.menuitem;
		}
		
		public void Run()
		{
			Application.Run();
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