# ReallyStopDebugger
General purpose process and debug killer for Visual Studio

## What is it (really)?
ReallyStopDebugger is a simple Visual Studio extension which allows to stop debugging and kill processes quickly.

##What can I use it for?
During development one can stumble upon build errors caused by MSBuild processes or previous debug instances which weren't killed in time, and lock our solutions assemblies/resources, thus stopping the build.
This is a quick solution to that problem, altough it can be also considered as a general purpose process killer extension.

##Compatibility
It is supported by VS 2012, VS 2013 and VS 2015.
NOTE: If you are updating from version 0.9, you might need to reinstall the extension to load properly (sorry for that!)

##Usage
ReallyStopDebugger has 2 modes (Tools menu):
* Normal mode: In this windowed mode you can set up which processes you want to target, along with additional options:
* Silent mode: This mode runs the extension silently, as set up in the configuration window, and redirects status messages to the Output window (if available)

##Configuration (normal mode)
* User criteria - Lets you choose beteween targeting all proceses or only those running as your current user. Useful for shared development environments, such as VMs
* Process criteria - Lets you add another filtering beteween all processes or only those spawned by the visual studio instance
* Attempt to force clean solution - Will try to manually erase /bin and /obj directories in the solution. Requires VS to be run as Administrator to work properly

All changes are saved after clowing the configuration window.

##Limitations
* Processes will be handled with the same privilege as the VS's owner
* /bin and /obj directories may not be cleaned completely (see above) 

##Dependencies
To compile and run the solution Visual Studio 2015 Community (or higher) Edition is required, plus the VS 2015 SDK and a strong name key file (Key.snk) to compile and sign the assembly (you can disable this option in the project properties, Signing tab).

Since version 1.1.2, Visual Studio 2013 (Update 5 or higher, with its SDK) can be used to work on the extension. Open the ReallyStopDebugger.v12.sln solution to work with VS 2013, or ReallyStopDebugger.v14.sln to work with VS 2015.
WARNING: This solution may be outdated compared to its vs 2015 counterpart

##TODOs
- Improve directory cleaning support
- Add keyboard shortcut commands
