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
* Normal mode: In this mode you can set up which processes you want to kill (windowed), along with additional options:
 * Load childen - Loads all the childen processes of the current Visual Studio instance
 * Restrict to this user - Filters processes to the current user's only. Useful for shared development environments, such as VMs
 * Attempt to force clean solution - Will try to manually erase /bin and /obj directories in the solution. Requires VS to be run as Administrator to work properly
* Silent mode: This mode just runs the extension as set up in the configuration window, and redirects status messages to the Output window (if available)

##Limitations
* Processes will be handled with the same privilege as the VS's owner
* /bin and /obj directories may not be cleaned completely (see above) 

##Dependencies
To compile and run the solution Visual Studio 2015 Community (or higher) Edition is required, plus the VS 2015 SDK and a strong name key file (Key.snk) to compile and sign the assembly (you can disable this option in the project properties, Signing tab).
Since version 1.1.2, Visual Studio 2013 (Update 5 or higher, with its SDK) can be used to work on the extension. Open the ReallyStopDebugger.v12.sln solution to work with VS 2013, or ReallyStopDebugger.v14.sln to work with VS 2015.

##TODOs
- Improve UI (in progress)
- Improve directory cleaning support
