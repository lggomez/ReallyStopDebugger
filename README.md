# ReallyStopDebugger
General purpose process and debug killer for Visual Studio

## What is it (really)?
ReallyStopDebugger is a simple Visual Studio extension which allows to stop debugging and kill processes quickly.

##What can I use it for?
During development one can stumble upon build errors caused by MSBuild processes or previous debug instances which weren't killed in time, and lock our solutions assemblies/resources, thus stopping the build.
This is a quick solution to that problem, altough it can be also considered as a general purpose process killer extension.

##Compatibility
It is supported by VS 2012, VS 2013 and VS 2015.

##Usage
ReallyStopDebugger has 2 modes (Tools menu):
* Normal mode: In this mode you can set up which processes you want to kill (windowed), along with additional options:
 * Restrict to this user - Filters processes to the current user's only
 * Attempt to force clean solution - Will try to manually erase /bin and /obj directories in the solution. Requires VS to be run as Administrator to work properly
* Silent mode: This mode just kills the processes configured in normal mode, and redirects status messages to the Output window (if available)

##Limitations
* Processes will be handled with the same privilege as the VS's owner

##Dependencies
To compile and run the solution Visual Studio 2015 Community (or higher) Edition is required, plus the VS 2015 SDK and a strong name key file (Key.snk) to compile and sign the assembly (you can disable this option in the project properties, Signing tab).

##TODOs
- Improve UI
