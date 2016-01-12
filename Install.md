_Read this only if you want to install this service manually._<br>
If you download the setup, you don't have to read this!!<br>

<h1>Howto install/configure vbservice manually</h1>

For this service .NET 4 is used. You can download this from the Microsoft website.<br>
<br>
Run<br>
<pre><code>vbservice -install</code></pre>
from this path to install the service on your computer.<br>
<br>
To configure a VM to start as a service use the vboxmanage command with the setextradata options with key Service and value yes.<br>For example if you want to start the VM named OpenBSD by the service type:<br>
<pre><code>vboxmanage setextradata OpenBSD Service yes</code></pre>.<br>

<h1>Uninstall vbservice</h1>
Just go to the directory where you have copied vbservice.exe and type<br>
<pre><code>vbservice -uninstall</code></pre>

<h1>Tray Icon</h1>
New in version 2.1 is the trayicon. The tray icon will set the extradata for you. This can be activated by executing the executable with the -tray option.<br>Note that on Vista/Windows7 you get an UAC warning, to prevent this start it from the Task Scheduler at logon with highest privileges.<br>
<pre><code>vboxservice -tray</code></pre>