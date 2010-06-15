using System;
using System.Xml;
using System.Text;
using System.Xml.XPath;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace VirtualBox
{
		class Control
		{
			[DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
			private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
			
			private EventLog eventlog;
			private Dictionary<int, string> listVMS = new Dictionary<int, string>();
			private string vbox_user_home;
			private string vboxheadless;
			private string vboxmanage;
			private string virtualbox_path;
			private StringBuilder buffer;

			public Control()
			{
				this.eventlog = new EventLog();
				this.eventlog.Source = "Virtualbox Server Service";
				this.eventlog.Log = "Application";

				buffer = new StringBuilder(256);

				if (GetPrivateProfileString("files","virtualbox_path","",buffer,256,"vbservice.ini")!=0) {
						virtualbox_path = Environment.ExpandEnvironmentVariables(buffer.ToString());
						/*if (virtualbox_path[virtualbox_path.Length-1] == '\\')
							virtualbox_path=virtualbox_path.Substring(0,virtualbox_path.Length-1);*/
						Console.WriteLine("virtualbox_path={0}",virtualbox_path);
				}
				if (GetPrivateProfileString("files","vboxheadless.exe","",buffer,256,"vbservice.ini")!=0) {
						vboxheadless = Environment.ExpandEnvironmentVariables(buffer.ToString());
				}
				if (GetPrivateProfileString("files","vboxmanage.exe","",buffer,256,"vbservice.ini")!=0) {
						vboxmanage = Environment.ExpandEnvironmentVariables(buffer.ToString());
				}
				if (GetPrivateProfileString("config","vbox_user_home","",buffer,256,"vbservice.ini")!=0) {
						vbox_user_home = Environment.ExpandEnvironmentVariables(buffer.ToString());
						Console.WriteLine("vbox_user_home={0}",vbox_user_home);
				}
			}

			~Control()
			{
				this.eventlog.Close();
			}


			public void stopvms()
			{
				Process[] vboxprocess = Process.GetProcessesByName("vboxheadless");
				foreach(Process vbp in vboxprocess)
				{
					if (this.listVMS.ContainsKey(vbp.Id)) {
						this.eventlog.WriteEntry(String.Format("Stopping VM {0} at pid {1}", this.listVMS[vbp.Id], vbp.Id));
						Process vboxcmd = new Process();
						try {
							vboxcmd.StartInfo.EnvironmentVariables.Add("VBOX_USER_HOME",vbox_user_home);
						} catch {}
						vboxcmd.StartInfo.FileName = virtualbox_path+vboxmanage;
						vboxcmd.StartInfo.WorkingDirectory = virtualbox_path;
						//vboxcmd.StartInfo.Arguments = "controlvm "+this.listVMS[vbp.Id]+" poweroff";
						vboxcmd.StartInfo.Arguments = "controlvm "+this.listVMS[vbp.Id]+" savestate";
						vboxcmd.StartInfo.CreateNoWindow = true;
						vboxcmd.StartInfo.UseShellExecute = false;
						vboxcmd.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
						vboxcmd.Start();

					} else {
						this.eventlog.WriteEntry(String.Format("Found pid {0} but does not belong to me", vbp.Id),EventLogEntryType.Warning);
					}

				}
			}

			public void startvms()
			{
				XmlDocument vbConfig = new XmlDocument();

				vbConfig.Load(this.vbox_user_home+@"virtualbox.xml");

				foreach(XmlNode element in vbConfig.GetElementsByTagName("MachineEntry"))
				{
					string xmlMachine=element.Attributes["src"].InnerText;
					string uuid=element.Attributes["uuid"].InnerText;
					uuid=uuid.Substring(1,uuid.Length-2);

					if (xmlMachine.Substring(1,1)!=":") {
						xmlMachine=this.vbox_user_home+xmlMachine;
					}

					XmlDocument machineConfig = new XmlDocument();
					machineConfig.Load(xmlMachine);
					foreach(XmlNode mcelement in machineConfig.GetElementsByTagName("ExtraDataItem"))
					{
						if ( mcelement.Attributes["name"].InnerText.ToLower() == "service" ) {
							if (mcelement.Attributes["value"].InnerText.ToLower() == "yes") {

								try {
									Process vboxcmd = new Process();
									try {
										vboxcmd.StartInfo.EnvironmentVariables.Add("VBOX_USER_HOME",vbox_user_home);
									} catch {}
									vboxcmd.StartInfo.FileName = virtualbox_path+vboxheadless;
									vboxcmd.StartInfo.WorkingDirectory = virtualbox_path;
									vboxcmd.StartInfo.Arguments = "-s \""+uuid+"\" -v on";
									vboxcmd.StartInfo.CreateNoWindow = true;
									vboxcmd.StartInfo.UseShellExecute = false;
									vboxcmd.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
									vboxcmd.Start();
									this.listVMS.Add(vboxcmd.Id, uuid);
									this.eventlog.WriteEntry(String.Format("Starting VM {0} at pid {1}",uuid,vboxcmd.Id));
								} catch (Exception e) {
									eventlog.WriteEntry(e.ToString(),EventLogEntryType.Error);
									continue;
								}
							}
						}
					}
				}
			}
		}
}
