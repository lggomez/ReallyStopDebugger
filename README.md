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
* Normal mode: In this mode you can set up which processes you want to kill (windowed)
* Silent mode: This mode just kills the processes configured in normal mode, and redirects status messages to the Output window (if available)

##Limitations
* Processes will be handled with the same privilege as the VS's owner
* Currently it doesn't support user filtering. Since .NET Process object doesn't contemplate owners/users it requires meddling with WMI queries, and I haven't found a performant approach yet

##Dependencies
To compile and run the solution Visual Studio 2013 is required, plus the VS 2013 SDK and a strong name key file (Key.snk) to compile and sign the assembly (you can disable this option in the project properties, Signing tab).

##TODOs
- Improve UI
