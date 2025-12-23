# Himzo Watcher

## How this was made
1. Installed NPCAP: https://npcap.com/#download
2. Installed SharpPcap package for VS
3. Ran VS as admin to make the package work
4. Made the code based on Anguses code and setup files
5. Ran the program with CHANGE_ME as the GUID, found the Ethernet adapter and pasted the GUID to Config
6. Published the app
7. Added a new task for startup for the script:
	1. Win+R: taskschd.msc
	2. Create Task
	3. General:
		1. Name: HimzoWatcher
		2. Press Change User or Group: Enter the obj name: SYSTEM, press Ok
		3. Run with highest privileges
		4. Set config for Win10 (or newer OS if available)
	4. Triggers:
		1. Press new..
		2. Begin the task: At startup
		3. Check enabled if not on by default
		4. Press ok
	5. Actions:
		1. Press new..
		2. Action: Start a program
		3. Program/script: paste the path of HimzoWatcher.exe
		4. Press ok
	6. Conditions:
		1. Untick start the task only if the computere is on AC power
	7. Settings:
		1. Untick Stop the task if it runs longer than: ...
		2. Untick If the running task does not end when requested, force it to stop
	8. Press ok
8. Added a new task for startup for the HappyLan:
	1. Create Task
	2. General:
		1. Name: HappyLanGUI
		2. The user should be Himzo or sth similiar
		3. Run with highest privileges
		4. Set config for Win10 (or newer OS if available)
	3. Triggers:
		1. Press new..
		2. Begin the task: At logon
		3. Check enabled if not on by default
		4. Press ok
	4. Actions:
		1. Press new..
		2. Action: Start a program
		3. Program/script: paste the path of happylan.exe
		4. Press ok
	5. Conditions:
		1. Untick start the task only if the computere is on AC power
	6. Settings:
		1. Untick Stop the task if it runs longer than: ...
		2. Untick If the running task does not end when requested, force it to stop
	7. Press ok 
9. Restarted machine to test the setup. It should be working from now on.