# ReallyStopDebugger
General purpose process and debug killer for Visual Studio

## What is it (really)?
ReallyStopDebugger is a simple Visual Studio extension which allows to stop debugging and kill processes quickly.

##What can I use it for?
During development one can stumble upon build errors caused by stalled MSBuild processes or previous debug instances which weren't killed in time, which lock our solutions assemblies and resources, thus breaking the build.
This is a quick solution to that problem, altough it can be also considered as a general purpose process killer extension, for bigger projects that spawn multiple external utilities or processes dependant on our binaries

##Compatibility
The extension is supported by Visual Studio 2012, 2013 and 2015. Visual Studio '15' also has experimental support (see below)
IMPORTANT: If you are updating from version 0.9, you might need to reinstall the extension to load properly (sorry for that!)

###Visual Studio '15'
Compatibility for the new Visual Studio 15 is enabled and the extension seems to be fully compatible, altough older extensions may be disabled once the final release is launched
If you decide to install the extension for this version, keep in mind the following:
* The VSIX installer will give you a warning about compatibility, which can be ignored
* Visual Studio will disable the extension by default, and once you restart after enabling it, an error message will appear saying that you might have to reinstall Visual Studio. Ignore it and restart Visual Studio again
* After these steps the extension should work properly and appear on the Tools menu

##Usage
ReallyStopDebugger has 2 modes (Tools menu):
* Normal mode: In this windowed mode you can set up which processes you want to target, along with additional options
* Silent mode: This mode runs the extension silently, using the last configuration used in the normal mode, and redirects status messages to the Output window (if available)

Both modes will stop the debug and build operations

##Configuration (normal mode)
* User criteria - Lets you choose beteween targeting all proceses or only those running as your current user. Useful for shared development environments, such as VMs
* Process criteria - Lets you add another filtering beteween all processes or only those spawned by the visual studio instance
* Attempt to force clean solution - Will try to manually erase /bin and /obj directories in the solution. Requires VS to be run as Administrator to work properly

All changes are saved after closing the configuration window.

##Limitations and known issues
* Processes will be handled with the same privilege as the Visual Studio instance owner
* /bin and /obj directories may not be cleaned completely due to permission issues (this is related to the previous issue)

##Dependencies
To compile and run the solution Visual Studio 2015 Community Edition is required, plus the VS 2015 SDK and a strong name key file (Key.snk) to compile and sign the assembly (you can disable this option in the project properties, Signing tab).

##TODOs
- Improve directory cleaning support
- Add keyboard shortcut commands
