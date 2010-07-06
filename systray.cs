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
using System.Diagnostics;
using System.IO.Pipes;

namespace VBoxService
{
	/// <summary>
	/// Description of notifyicon.
	/// </summary>
	public class SysTrayIcon : ApplicationContext
	{
		private const int pipetimeout = 5*1000;
		private NotifyIcon notifyIcon;
		private VirtualBox.VirtualBox vbox;
		private System.Windows.Forms.ContextMenu menuitem;
		private StringBuilder extradatakey;
		private StringBuilder pipeName;
		private virtualboxcallback vboxcallback;
		private System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VBoxService));
				
		public SysTrayIcon()
		{
			this.extradatakey = new StringBuilder(resources.GetString("VBoxService.ExtraDataKey"));
			this.pipeName = new StringBuilder(resources.GetString("Pipe.Name"));
			vbox = new VirtualBox.VirtualBox();
			vboxcallback = new virtualboxcallback(this);
			vbox.RegisterCallback(vboxcallback);
			
			this.menuitem = new System.Windows.Forms.ContextMenu();
			for(int i=0; i<((Array)vbox.Machines).Length;i++) {
				MenuItem menu = new MenuItem(vbox.Machines[i].Name);
				menu.Name = vbox.Machines[i].Name;
				menu.Tag = vbox.Machines[i].Id;
				
				this.addSubItems(menu, vbox.Machines[i].GetExtraData(this.extradatakey.ToString()).ToLower()=="yes"?true:false);
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
		
		/// <summary>
		/// Add subitems to Context menu
		/// </summary>
		/// <param name="m">MenuItem to add submenus to.</param>
		/// <param name="asService">Extradata is set to run as Service</param>
		private void addSubItems(MenuItem m, bool asService)
		{
			MenuItem subItem = new MenuItem();
			subItem.Text = subItem.Name = "Console";
			subItem.Visible = asService;
			subItem.Click += contextConsole;
			m.MenuItems.Add(subItem);
			subItem = new MenuItem();
			subItem.Text = subItem.Name = "Start";
			subItem.Visible = asService;
			subItem.Click += contextStart;
			m.MenuItems.Add(subItem);
			subItem = new MenuItem();
			subItem.Text = subItem.Name = "Stop";
			subItem.Visible = asService;
			subItem.Click += contextStop;
			m.MenuItems.Add(subItem);
			subItem = new MenuItem();
			subItem.Text = subItem.Name = "Service";
			subItem.Click += contextClick;
			subItem.Checked = asService;
			m.MenuItems.Add(subItem);
		}
		
		/// <summary>
		/// Callback if machine is created or deleted from the GUI.
		/// This will update the context menu.
		/// </summary>
		/// <param name="uuid">UUID of the machine</param>
		/// <param name="reg">1=new machine, 0=deleted machine</param>
		public void ChkMenuItem(string uuid, int reg)
		{
			int index;
			
			// New machine is registered in the GUI
			if (reg==1) {
				/// <summary>
				///  Find the break-line before the exit
				/// </summary>
				for(index=0; index<this.menuitem.MenuItems.Count;index++) {
					if (this.menuitem.MenuItems[index].Text=="-") break;
				}
				
				MenuItem menu = new MenuItem(vbox.GetMachine(uuid).Name);
				//menu.Click += contextClick;
				menu.Tag = uuid;
				this.addSubItems(menu,false);
				
				this.menuitem.MenuItems.Add(index,menu);
			} else {
				// Machine is delete in the GUI
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
		
		/// <summary>
		/// Show about box
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
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
		
		/// <summary>
		/// Exit trayicon application handler
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void ExitSystray(object Sender, EventArgs e)
		{
			this.notifyIcon.Visible = false;
			Application.Exit();
		}
		
		/// <summary>
		/// Context click handler
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void contextClick(object Sender, EventArgs e)
		{
			VirtualBox.IMachine machine;
			MenuItem selected=(MenuItem)Sender;
#if DEBUG
			Console.WriteLine(selected.Parent.Tag);
#endif
			selected.Checked = selected.Checked ? false: true;
			MenuItem[] t = selected.Parent.MenuItems.Find("Start",false); t[0].Visible = selected.Checked;		// Enable/disable Start
			t = selected.Parent.MenuItems.Find("Stop",false); t[0].Visible = selected.Checked;					// Enable/disable Stop
			t = selected.Parent.MenuItems.Find("Console",false); t[0].Visible = selected.Checked;					// Enable/disable Console
			machine = vbox.GetMachine((string)selected.Parent.Tag);
			if (selected.Checked)
				machine.SetExtraData(this.extradatakey.ToString(), "yes");
			else 
				machine.SetExtraData(this.extradatakey.ToString(), "");
		}
		
		/// <summary>
		/// Context Console VM handler
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void contextConsole(object Sender, EventArgs e)
		{
#if DEBUG
			Console.WriteLine("Console clicked");
#endif
		}

		/// <summary>
		/// Context Start VM handler
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void contextStart(object Sender, EventArgs e)
		{
			MenuItem selected = (MenuItem)Sender;
			VirtualBox.IMachine machine = vbox.GetMachine((string)selected.Parent.Tag);
			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes("start"+selected.Parent.Tag);
#if DEBUG
			Console.WriteLine("Start clicked");
#endif	
			using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(pipeName.ToString()))
			{
				try {
					pipeStream.Connect(pipetimeout);
					pipeStream.ReadMode = PipeTransmissionMode.Message;
					//if (pipeStream.IsConnected)
						pipeStream.Write(bytes, 0, bytes.Length);
				} catch(Exception err) {
#if DEBUG
					Console.WriteLine(err.ToString());
#endif
				}
			}
		}
		
		/// <summary>
		/// Context Stop VM handler
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void contextStop(object Sender, EventArgs e)
		{
			MenuItem selected = (MenuItem)Sender;
			VirtualBox.IMachine machine = vbox.GetMachine((string)selected.Parent.Tag);
			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes("stop "+selected.Parent.Tag);

#if DEBUG
			Console.WriteLine("Stop clicked {0}",machine.State);
#endif				
			using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(pipeName.ToString()))
			{
				try {
					pipeStream.Connect(pipetimeout);
					pipeStream.ReadMode = PipeTransmissionMode.Message;
					//if (pipeStream.IsConnected)
						pipeStream.Write(bytes, 0, bytes.Length);
				} catch(Exception err) {
#if DEBUG
					Console.WriteLine(err.ToString());
#endif
				}

			}
		}
	}
}