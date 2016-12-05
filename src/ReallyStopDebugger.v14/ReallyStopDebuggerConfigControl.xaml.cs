// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using ReallyStopDebugger.Common;

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;

namespace ReallyStopDebugger
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        internal Package currentPackage { get; set; }

        internal SettingsManager settingsManager { get; set; }

        public List<ProcessInfo> Processes { get; set; } = new List<ProcessInfo>();

        public bool AllProcessesSelected { get; set; } = false;

        public MyControl()
        {
            this.InitializeComponent();

            // Default control states
            this.processCriteriaRadioButton_allProcesses.IsChecked = true;
            this.userCriteriaRadioButton_allUsers.IsChecked = true;
            this.StatusLabel.Visibility = Visibility.Hidden;

            this.Loaded += this.ReallyStopDebuggerConfig_Loaded;
            this.Unloaded += this.ReallyStopDebuggerConfig_Unloaded;
        }

        #region Control events

        private void killProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            #region Initialize

            if (this.currentPackage == null)
            {
                // The control was loaded in a faulted way or before the package initialization
                this.StatusLabel.Content =
                    string.Format(
                        "Visual Studio instance not found.{0}Please reopen this window and try again",
                        Environment.NewLine);
                this.StatusLabel.Foreground = Brushes.Red;
                this.StatusLabel.Visibility = Visibility.Visible;
                return;
            }

            this.StatusLabel.Content = string.Empty;

            #endregion

            #region Stop debug mode

            var dte = ((ReallyStopDebuggerPackage)this.currentPackage).GetDte();

            try
            {
                // Stop local VS debug
                if (dte != null)
                {
                    dte.ExecuteCommand("Debug.StopDebugging");
                }
            }
            catch (COMException)
            {
                // The command Debug.StopDebugging is not available
            }

            #endregion

            var result = ProcessHelper.KillProcesses(
                this.currentPackage,
                this.processDisplayDataGrid.ItemsSource.Cast<ProcessInfo>().Where(p => p.IsSelected).Select(_ => _.ProcessName).ToList(),
                this.userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault(),
                this.processCriteriaRadioButton_children.IsChecked.GetValueOrDefault());

            if (this.forceCleanCheckBox.IsChecked.GetValueOrDefault()
                && !string.IsNullOrWhiteSpace(dte.Solution.FullName))
            {
                FileUtils.AttemptHardClean(dte);
            }

            #region UI update

            this.StatusLabel.Visibility = Visibility.Visible;

            string returnMessage;

            switch (result)
            {
                case Constants.PROCESSESKILLSUCCESS:
                    {
                        returnMessage = "Processes killed.";
                        this.StatusLabel.Foreground = Brushes.Green;
                        break;
                    }
                case Constants.PROCESSESNOTFOUND:
                    {
                        returnMessage = "Could not find any matching processes.";
                        this.StatusLabel.Foreground = Brushes.Orange;
                        break;
                    }
                default:
                    {
                        returnMessage = "Could not close orphaned processes due to an error.";
                        this.StatusLabel.Foreground = Brushes.Red;
                        break;
                    }
            }

            this.StatusLabel.Content = returnMessage;

            #endregion
        }

        private void loadChildProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            var childProcesses = WindowsInterop.GetChildProcesses(WindowsInterop.GetCurrentProcess().Id);

            // TODO: Remove this on release
#if DEBUG
            var processes = WindowsInterop.GetCurrentUserProcesses();
            childProcesses.AddRange(processes);
#endif

            if (childProcesses.Any())
            {
                var childProcessesInfo =
                    childProcesses.GroupBy(p => p.ProcessName, StringComparer.InvariantCultureIgnoreCase)
                        .Select(g => new ProcessInfo(g))
                        .ToList();

                this.AllProcessesSelected = false;
                this.Processes = childProcessesInfo;
                this.RefreshProcessDisplayDataGrid();
            }
        }

        private void ProcessSelectionHeader_OnChecked(object sender, RoutedEventArgs e)
        {
            this.Processes.ForEach(p => { p.IsSelected = this.AllProcessesSelected; });
            this.RefreshProcessDisplayDataGrid();
        }

        #endregion

        #region Load/Unload events

        private void ReallyStopDebuggerConfig_Loaded(object sender, RoutedEventArgs e)
        {
            this.StatusLabel.Content = string.Empty;

            #region Load settings

            if (this.settingsManager != null)
            {
                var configurationSettingsStore =
                    this.settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                var collectionExists = configurationSettingsStore.CollectionExists("ReallyStopDebugger");

                if (collectionExists)
                {
                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "CustomProcessList"))
                    {
                        this.processCustomDisplayDataGrid.ItemsSource =
                            (configurationSettingsStore.GetString("ReallyStopDebugger", "CustomProcessList")
                             ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ForceClean"))
                    {
                        this.forceCleanCheckBox.IsChecked =
                            Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "ForceClean"));
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "UserProcessMatch"))
                    {
                        this.userCriteriaRadioButton_userOnly.IsChecked =
                            Convert.ToBoolean(
                                configurationSettingsStore.GetString("ReallyStopDebugger", "UserProcessMatch"));
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ChildProcessMatch"))
                    {
                        this.userCriteriaRadioButton_userOnly.IsChecked =
                            Convert.ToBoolean(
                                configurationSettingsStore.GetString("ReallyStopDebugger", "ChildProcessMatch"));
                    }
                }
            }

            #endregion
        }

        private void ReallyStopDebuggerConfig_Unloaded(object sender, RoutedEventArgs e)
        {
            this.processDisplayDataGrid.ItemsSource = null;

            #region Save settings

            if (this.settingsManager != null)
            {
                var userSettingsStore = this.settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                var collectionExists = userSettingsStore.CollectionExists("ReallyStopDebugger");

                if (!collectionExists)
                {
                    userSettingsStore.CreateCollection("ReallyStopDebugger");
                }

                userSettingsStore.SetString(
                    "ReallyStopDebugger",
                    "CustomProcessList",
                    string.Join("\r\n", this.processCustomDisplayDataGrid.ItemsSource ?? new List<string>()));
                userSettingsStore.SetString(
                    "ReallyStopDebugger",
                    "ForceClean",
                    this.forceCleanCheckBox.IsChecked.GetValueOrDefault().ToString());
                userSettingsStore.SetString(
                    "ReallyStopDebugger",
                    "UserProcessMatch",
                    this.userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault().ToString());
                userSettingsStore.SetString(
                    "ReallyStopDebugger",
                    "ChildProcessMatch",
                    this.userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault().ToString());
            }

            #endregion
        }

        #endregion

        #region Helper Methods

        private void RefreshProcessDisplayDataGrid()
        {
            this.processDisplayDataGrid.ItemsSource = null;
            this.processDisplayDataGrid.ItemsSource = this.Processes;
        }

        #endregion
    }
}