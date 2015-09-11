// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using EnvDTE;
using ReallyStopDebugger.Common;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace ReallyStopDebugger
{
    using Constants = EnvDTE.Constants;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.3", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(ReallyStopDebuggerToolWindow))]
    [Guid(GuidList.guidReallyStopDebuggerPkgString)]
    public sealed class ReallyStopDebuggerPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public ReallyStopDebuggerPackage()
        {
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exist it will be created.
            ReallyStopDebuggerToolWindow window = FindToolWindow(typeof(ReallyStopDebuggerToolWindow), 0, true) as ReallyStopDebuggerToolWindow;

            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            else
            {
                window.currentPackage = this;

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

                ((MyControl)window.Content).currentPackage = this;
                ((MyControl)window.Content).settingsManager = new ShellSettingsManager(this);

                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (null != mcs)
            {
                // Create the command for the cfg menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidReallyStopDebuggerCmdSet, (int)PkgCmdIDList.cmdidReallyStopDebugger);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID );
                mcs.AddCommand(menuItem);
                // Create the command for the lite version menu item.
                CommandID menuCommandID2 = new CommandID(GuidList.guidReallyStopDebuggerCmdSet, (int)PkgCmdIDList.cmdidReallyStopDebuggerLite);
                MenuCommand menuItem2 = new MenuCommand(MenuItemCallbackLite, menuCommandID2);
                mcs.AddCommand(menuItem2);
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidReallyStopDebuggerCmdSet, (int)PkgCmdIDList.cmdidReallyStopDebuggerCfg);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
            }
        }
        #endregion

        /// <summary>
        /// Tries to resolve a DTE instance from this package.
        /// </summary>
        /// <returns>The DTE instance, or null if no instance could be resolved</returns>
        public DTE GetDte()
        {
            return (DTE)GetService(typeof(DTE));
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                ShowToolWindow(sender, e);
            }
            catch (Exception ex)
            {
                this.ShowErrorMessage(
                    string.Format(CultureInfo.CurrentCulture, "Exception: {0}{1}Inside {0}.MenuItemCallback()", ex.Message, Environment.NewLine),
                    "Error");
            }
        }

        /// <summary>
        /// Callback for the silent version of ReallyStopDebugger
        /// </summary>
        private void MenuItemCallbackLite(object sender, EventArgs e)
        {
            try
            {
                #region Stop debug mode
                var dte = GetDte();
                int result = -1;

                //Stop local VS debug/build
                dte.TryExecuteCommand("Debug.StopDebugging");
                dte.TryExecuteCommand("Build.Cancel");

                #endregion

                #region Configuration retrieval & process killing

                //Default values. These may be overriden upon configuration store retrieval
                var filter = new string[] { "MSBuild", "WebDev.WebServer40", "Microsoft.VisualStudio.Web.Host" };

                SettingsStore configurationSettingsStore = (new ShellSettingsManager(this)).GetReadOnlySettingsStore(SettingsScope.UserSettings);
                bool collectionExists = configurationSettingsStore.CollectionExists("ReallyStopDebugger");

                if (collectionExists)
                {
                    var filterByLocalUser = false;
                    var filterByChildren = false;

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ProcessList"))
                    {
                        filter = (configurationSettingsStore.GetString("ReallyStopDebugger", "ProcessList") ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "UserProcessMatch") )
                    {
                        filterByLocalUser = Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "UserProcessMatch"));
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ChildProcessMatch"))
                    {
                        filterByChildren = Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "ChildProcessMatch"));
                    }

                    result = ProcessHelper.KillProcesses(this, filter.ToArray(), filterByLocalUser, filterByChildren, true);
                    
                }
                else
                {
                    result = ProcessHelper.KillProcesses(this, filter.ToArray(), false, false, true);
                }

                #endregion

                if (collectionExists)
                {
                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ForceClean"))
                    {
                        var forceClean = Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "ForceClean"));

                        if (forceClean && !string.IsNullOrWhiteSpace(dte.Solution.FullName))
                        {
                            Common.FileUtils.AttemptHardClean(dte);
                        }
                    }
                }

                #region Output window handling

                string returnMessage;

                // Find the output window.
                Window window = GetDte().Windows.Item(Constants.vsWindowKindOutput);
                OutputWindow outputWindow = (OutputWindow)window.Object;

                OutputWindowPane owp;
                owp = outputWindow.OutputWindowPanes.Add("Output");

                switch (result)
                {
                    case Common.Constants.PROCESSESKILLSUCCESS:
                        {
                            returnMessage = "ReallyStopDebugger>------ Processes killed.";
                            break;
                        }
                    case Common.Constants.PROCESSESNOTFOUND:
                        {
                            returnMessage = "ReallyStopDebugger>------ Could not find any matching processes.";
                            break;
                        }
                    default:
                        {
                            returnMessage = "ReallyStopDebugger>------ Could not close orphaned processes due to an error.";
                            break;
                        }
                }

                owp.Activate();
                owp.OutputString(returnMessage);

                #endregion
            }
            catch (Exception ex)
            {
                this.ShowErrorMessage(
                    string.Format(CultureInfo.CurrentCulture, "Exception: {0}{1}Inside {0}.MenuItemCallbackLite()", ex.Message, Environment.NewLine),
                    "Error");
            }
        }
    }
}
