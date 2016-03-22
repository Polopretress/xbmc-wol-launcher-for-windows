Listen for Wake-up On Lan call from the Official Android Remote and start XBMC on an already running Windows OS.

Original Work from "Pieh": http://forum.xbmc.org/showthread.php?t=78167

Change Log:

---

**v0.1.0**

  * Added a Notification Area Icon to control the Listener
  * Several change on the Thread Side of the code.
  * Fixed Crash when XBMC was already running
  * Other bugs fix.
  * Filter request using Mac address on multi XBMC environment in order to avoid all XBMCs to fire up at once (now Proper MAC need to be setup on the remote).
  * Feature: Allow to Launch XBMC as "TOP MOST" windows. This is a work around to avoid Windows task bar to stay on top after XBMC's startup on certain configuration. Although I haven't figured out why it seems to be more related to XBMC itself. As suggested by Pieh check <settings -> system -> "Use a fullscreen window rather than true fullscreen">.
  * Feature: Start XBMC directly from the notification Icon by double clicking on it or within the menu.
  * Feature: Enable/Disable the listener.
  * Feature: Change XBMC Path from within the menu.
  * Feature: Reload Configuration (Restart Listener)

**v0.1.1**

  * "Top Most" Property default set to False

