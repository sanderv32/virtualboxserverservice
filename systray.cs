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
		private const int pipetimeout = 5*100;
		private NotifyIcon notifyIcon;
		private VirtualBox.VirtualBox vbox;
		private System.Windows.Forms.ContextMenu menuitem;
		private StringBuilder extradatakey;
		private StringBuilder pipeName;
		private virtualboxcallback vboxcallback;
		private System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VBoxService));
		private VBoxSSIPC ipcs;
				
		public SysTrayIcon()
		{
			Byte[] bytes= new Byte[64];
			this.extradatakey = new StringBuilder(resources.GetString("VBoxService.ExtraDataKey"));
			this.pipeName = new StringBuilder(resources.GetString("Pipe.Name"));
			this.ipcs = new VBoxSSIPC(resources.GetString("Pipe.Name").ToString());
			this.ipcs.pipetimeout = pipetimeout;
			
			vbox = new VirtualBox.VirtualBox();
			vboxcallback = new virtualboxcallback(this);
			vbox.RegisterCallback(vboxcallback);
			
			this.menuitem = new System.Windows.Forms.ContextMenu();
			for(int i=0; i<((Array)vbox.Machines).Length;i++) {
				MenuItem menu = new MenuItem(vbox.Machines[i].Name);
				menu.Name = vbox.Machines[i].Name;
				menu.Tag = vbox.Machines[i].Id;
				
				ipcs.SendAndReceive("vrdp "+vbox.Machines[i].Id,bytes);
				int port = BitConverter.ToInt16(bytes,0);
				this.addSubItems(menu, vbox.Machines[i].GetExtraData(this.extradatakey.ToString()).ToLower()=="yes"?true:false,port>0?true:false);
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
		private void addSubItems(MenuItem m, bool asService, bool hasConsole)
		{			
			MenuItem subItem = new MenuItem();
			subItem.Text = subItem.Name = "Console";
			subItem.Enabled = asService;
			subItem.Click += contextConsole;
			m.MenuItems.Add(subItem);
			subItem = new MenuItem();
			subItem.Text = subItem.Name = "Start";
			subItem.Enabled = asService;
			subItem.Click += contextStart;
			m.MenuItems.Add(subItem);
			subItem = new MenuItem();
			subItem.Text = subItem.Name = "Stop";
			subItem.Enabled = asService;
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
				
				this.addSubItems(menu,false,false);
				
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
			MenuItem[] t = selected.Parent.MenuItems.Find("Start",false); t[0].Enabled = selected.Checked;		// Enable/disable Start
			t = selected.Parent.MenuItems.Find("Stop",false); t[0].Enabled = selected.Checked;					// Enable/disable Stop
			t = selected.Parent.MenuItems.Find("Console",false); t[0].Enabled = selected.Checked;					// Enable/disable Console
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
			MenuItem selected = (MenuItem)Sender;
			VirtualBox.IMachine machine = vbox.GetMachine((string)selected.Parent.Tag);
			Byte[] bytes = new Byte[64];

			ipcs.SendAndReceive("vrdp "+selected.Parent.Tag,bytes);
			int port = BitConverter.ToInt16(bytes,0);
			bool alreadyConnected = BitConverter.ToBoolean(bytes,2);
#if DEBUG
			Console.WriteLine("Server send port {0} to connect too",port);
#endif
			if (port > 0 && !alreadyConnected) {
				ProcessStartInfo mstsc = new ProcessStartInfo();
				mstsc.FileName = "mstsc.exe";
				mstsc.Arguments = "/v 127.0.0.1:"+port.ToString();
				mstsc.ErrorDialog = true;
				
				Process process = Process.Start(mstsc);
			}
				
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
/*			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes("start"+selected.Parent.Tag);
			this.Send(bytes);*/
			ipcs.SendMessage("start"+selected.Parent.Tag);
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
/*			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes("stop "+selected.Parent.Tag);
			this.Send(bytes);*/
			ipcs.SendMessage("stop "+selected.Parent.Tag);
		}
		
		private void Send(Byte[] buffer)
		{
			using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(pipeName.ToString()))
			{
				try {
					pipeStream.Connect(pipetimeout);
					pipeStream.ReadMode = PipeTransmissionMode.Message;
					//if (pipeStream.IsConnected)
						pipeStream.Write(buffer, 0, buffer.Length);
						pipeStream.Read(buffer, 0, buffer.Length);
				} catch(Exception err) {
#if DEBUG
					Console.WriteLine(err.ToString());
#endif
				}
			}
		}
	}
	
	/// <summary>
	/// IPC to Service
	/// </summary>
	public class VBoxSSIPC {
		private NamedPipeClientStream pipeStream;
		public int pipetimeout=5000;
		public string pipename;
		
		public VBoxSSIPC(string name)
		{
			this.pipename = name;
		}
		
		~VBoxSSIPC()
		{
			this.pipeStream.Close();
		}
		
		
		private void Connect()
		{
			this.pipeStream = new NamedPipeClientStream(this.pipename);
			this.pipeStream.Connect(this.pipetimeout);
		}
		
		public void Send(Byte[] buffer)
		{
			try {
				this.Connect();
				this.pipeStream.ReadMode = PipeTransmissionMode.Message;
				this.pipeStream.Write(buffer, 0, buffer.Length);
			} catch(Exception err) {
#if DEBUG
				Console.WriteLine(err.ToString());
#endif
			}
		}
		
		public void SendMessage(string message)
		{
			Byte[] buffer = new Byte[64];
			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes(message);

			using (this.pipeStream) {
				try {
					this.Connect();
					this.pipeStream.ReadMode = PipeTransmissionMode.Message;
					this.pipeStream.Write(bytes, 0, bytes.Length);
				} catch(Exception err) {
#if DEBUG
					Console.WriteLine(err.ToString());
#endif
				}
			}
		}
			
		public void SendAndReceive(string message, Byte[] result)
		{
			Byte[] buffer = new Byte[64];
			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes(message);

			using (this.pipeStream) {
				try {
					this.Connect();
					this.pipeStream.ReadMode = PipeTransmissionMode.Message;
					this.pipeStream.Write(bytes, 0, bytes.Length);
					this.pipeStream.Read(result, 0, result.Length);
				} catch(Exception err) {
#if DEBUG
					Console.WriteLine(err.ToString());
#endif
				}
			}
		}

	}
}