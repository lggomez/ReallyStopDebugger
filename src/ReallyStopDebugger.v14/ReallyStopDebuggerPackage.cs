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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using ReallyStopDebugger.Controls;
using ReallyStopDebugger.Native;

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
    [InstalledProductRegistration("#110", "#112", "2.0.1", IconResourceID = 400)]

    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]

    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(ReallyStopDebuggerToolWindow))]
    [Guid(GuidList.guidReallyStopDebuggerPkgString)]
    public sealed class ReallyStopDebuggerPackage : Package
    {
        private SettingsStore configurationSettingsStore;

        private bool collectionExists;

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
            IVsWindowFrame toolWindowFrame = null;

            try
            {
                // Get the instance number 0 of this tool window. This window is single instance so this instance
                // is actually the only one.
                // The last flag is set to true so that if the tool window does not exist it will be created.
                var toolWindow = this.FindToolWindow(typeof(ReallyStopDebuggerToolWindow), 0, true) as ReallyStopDebuggerToolWindow;

                if (toolWindow?.Frame == null)
                {
                    throw new NotSupportedException(Resources.CanNotCreateWindow);
                }

                var myControl = toolWindow.Content as MyControl;
                toolWindowFrame = toolWindow.Frame as IVsWindowFrame;

                myControl.CurrentPackage = this;
                myControl.DTE = this.GetDte();
                myControl.SettingsManager = new ShellSettingsManager(this);
            }
            catch (Exception ex)
            {
                this.ShowErrorMessage($"{Resources.ToolWindowInitFail}({sender.GetType().Name}){ex}{Environment.NewLine}{ex.StackTrace}", Resources.ErrorTitle);
            }

            ErrorHandler.ThrowOnFailure(toolWindowFrame.Show());
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

            this.RegisterCommandHandlers();
        }

        private void RegisterCommandHandlers()
        {
            // Add our command handlers for menu (commands must exist in the .vsct file)
            var commandService = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (null != commandService)
            {
                this.RegisterMainMenuCommand(commandService);
                this.RegisterSilentModeCommand(commandService);
            }
            else
            {
                this.ShowErrorMessage(Resources.CommandServiceFail, Resources.ErrorTitle);
            }
        }

        private void RegisterCommand(
            OleMenuCommandService commandService,
            Guid menuCommandGroupGuid,
            int commandIdNumber,
            EventHandler handler)
        {
            var commandId = new CommandID(menuCommandGroupGuid, commandIdNumber);
            var menuCommand = new MenuCommand(handler, commandId);

            if (commandService.FindCommand(commandId) != null)
            {
                commandService.RemoveCommand(menuCommand); //Is this step needed?
            }

            commandService.AddCommand(menuCommand);

            Debug.WriteLine($"Registered command {nameof(commandService)}: {menuCommandGroupGuid} - {commandIdNumber}");
        }

        private void RegisterMainMenuCommand(OleMenuCommandService commandService)
        {
            // Create the command for the cfg menu item.
            this.RegisterCommand(commandService, GuidList.guidReallyStopDebuggerCmdSet, (int)PkgCmdIDList.cmdidReallyStopDebugger, this.MenuItemCallback);
        }

        private void RegisterSilentModeCommand(OleMenuCommandService commandService)
        {
            // Create the command for the lite version menu item.
            this.RegisterCommand(commandService, GuidList.guidReallyStopDebuggerCmdSet, (int)PkgCmdIDList.cmdidReallyStopDebuggerLite, this.MenuItemCallbackLite);
        }

        #endregion

        /// <summary>
        /// Tries to resolve a DTE instance from this package.
        /// </summary>
        /// <returns>The DTE instance, or null if no instance could be resolved</returns>
        public DTE GetDte()
        {
            return (DTE)this.GetService(typeof(DTE));
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
                this.ShowToolWindow(sender, e);
            }
            catch (Exception ex)
            {
                this.ShowErrorMessage(
                    string.Format(CultureInfo.CurrentCulture, Resources.Exception_General, ex.Message, Environment.NewLine, this.GetType().Name),
                    Resources.ErrorTitle);
            }
        }

        /// <summary>
        /// Callback for the silent version of ReallyStopDebugger
        /// </summary>
        private void MenuItemCallbackLite(object sender, EventArgs e)
        {
            try
            {
                this.LoadSettingsStore();

                // Stop debug mode
                var dte = this.GetDte();

                // Stop local VS debug/build
                dte.TryExecuteCommand("Debug.StopDebugging");
                dte.TryExecuteCommand("Build.Cancel");

                // Kill processes and attempt force clean (if needed)
                var result = this.KillProcesses();
                this.AttemptForceClean(dte);

                this.SendResultToOutputWindow(result);
            }
            catch (Exception ex)
            {
                this.ShowErrorMessage(
                    string.Format(CultureInfo.CurrentCulture, Resources.Exception_General_Lite, ex.Message, Environment.NewLine, this.GetType().Name),
                    Resources.ErrorTitle);
            }
        }

        private void LoadSettingsStore()
        {
            this.configurationSettingsStore =
                new ShellSettingsManager(this).GetReadOnlySettingsStore(SettingsScope.UserSettings);

            this.collectionExists = this.configurationSettingsStore.CollectionExists(Common.Constants.CollectionPath);
        }

        private void AttemptForceClean(DTE dte)
        {
            if (this.collectionExists)
            {
                if (this.configurationSettingsStore.PropertyExists(Common.Constants.CollectionPath, Common.Constants.ForceCleanProperty))
                {
                    var forceClean = Convert.ToBoolean(this.configurationSettingsStore.GetString(Common.Constants.CollectionPath, Common.Constants.ForceCleanProperty));

                    if (forceClean && !string.IsNullOrWhiteSpace(dte.Solution.FullName))
                    {
                        FileUtils.AttemptHardClean(dte);
                    }
                }
            }
        }

        private ProcessOperationException KillProcesses()
        {
            ProcessOperationException result;

            // Default values. These may be overriden upon configuration store retrieval
            var filter = Common.Constants.DeafultFilter;

            if (this.collectionExists)
            {
                var filterByLocalUser = false;
                var filterByChildren = false;

                if (this.configurationSettingsStore.PropertyExists(Common.Constants.CollectionPath, Common.Constants.CustomProcessesProperty))
                {
                    filter =
                        (this.configurationSettingsStore.GetString(Common.Constants.CollectionPath, Common.Constants.CustomProcessesProperty) ?? string.Empty).Split(
                            new[] { "\r\n", "\n" },
                            StringSplitOptions.None);
                }

                if (this.configurationSettingsStore.PropertyExists(Common.Constants.CollectionPath, Common.Constants.UserProcessMatchProperty))
                {
                    filterByLocalUser =
                        Convert.ToBoolean(this.configurationSettingsStore.GetString(Common.Constants.CollectionPath, Common.Constants.UserProcessMatchProperty));
                }

                if (this.configurationSettingsStore.PropertyExists(Common.Constants.CollectionPath, Common.Constants.ChildProcessMatchProperty))
                {
                    filterByChildren =
                        Convert.ToBoolean(this.configurationSettingsStore.GetString(Common.Constants.CollectionPath, Common.Constants.ChildProcessMatchProperty));
                }

                result = ProcessHelper.KillProcesses(this, filter.ToArray(), filterByLocalUser, filterByChildren, true);
            }
            else
            {
                result = ProcessHelper.KillProcesses(this, filter.ToArray(), false, false, true);
            }

            return result;
        }

        private void SendResultToOutputWindow(ProcessOperationException result)
        {
            string returnMessage;

            // Find the output window.
            var window = this.GetDte().Windows.Item(Constants.vsWindowKindOutput);
            var outputWindow = (OutputWindow)window.Object;

            var owp = outputWindow.OutputWindowPanes.Add("Output");

            switch (result.ResultCode)
            {
                case ProcessOperationResults.Success:
                    {
                        returnMessage = Resources.ProcesseskillsuccessMessageLite;
                        break;
                    }
                case ProcessOperationResults.NotFound:
                    {
                        returnMessage = Resources.ProcessesnotfoundMessageLite;
                        break;
                    }
                default:
                    {
                        returnMessage = result.IsFaulted
                                            ? $"{Resources.ProcessesDefaultMessageLite}{Environment.NewLine}{result.InnerProcessException.Message}"
                                            : Resources.ProcessesDefaultMessageLite;
                        break;
                    }
            }

            owp.Activate();
            owp.OutputString($"[{DateTime.Now:HH:mm:ss}] {returnMessage}");
        }
    }
}
