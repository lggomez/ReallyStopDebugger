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
using ReallyStopDebugger.Native;

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;

namespace ReallyStopDebugger
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        internal Package CurrentPackage { get; set; }

        internal SettingsManager SettingsManager { get; set; }

        public List<ProcessInfo> Processes { get; set; } = new List<ProcessInfo>();

        public List<CustomProcessInfo> CustomProcesses { get; set; } = new List<CustomProcessInfo>();

        public bool AllProcessesSelected { get; set; }

        public MyControl()
        {
            this.InitializeComponent();

            // Default control states
            this.processCriteriaRadioButton_allProcesses.IsChecked = true;
            this.userCriteriaRadioButton_allUsers.IsChecked = true;
            this.StatusLabel.Visibility = Visibility.Hidden;

            this.Loaded += this.ReallyStopDebuggerConfigLoaded;
            this.Unloaded += this.ReallyStopDebuggerConfigUnloaded;
        }

        #region Control events

        private void KillProcessesButtonClick(object sender, RoutedEventArgs e)
        {
            #region Initialize

            if (this.CurrentPackage == null)
            {
                // The control is in a faulted state or was instantiated before the package initialization
                this.StatusLabel.Content =
                    $"Visual Studio instance not found.{Environment.NewLine}Please reopen this window and try again";
                this.StatusLabel.Foreground = Brushes.Red;
                this.StatusLabel.Visibility = Visibility.Visible;
                return;
            }

            this.StatusLabel.Content = string.Empty;

            #endregion

            #region Stop debug mode

            var dte = ((ReallyStopDebuggerPackage)this.CurrentPackage).GetDte();

            try
            {
                // Stop local VS debug
                dte?.ExecuteCommand("Debug.StopDebugging");
            }
            catch (COMException)
            {
                // The command Debug.StopDebugging is not available
            }

            #endregion

            var result = ProcessHelper.KillProcesses(
                this.CurrentPackage,
                this.processDisplayDataGrid.ItemsSource.Cast<ProcessInfo>()
                    .Where(p => p.IsSelected)
                    .Select(_ => _.ProcessName)
                    .ToList(),
                this.userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault(),
                this.processCriteriaRadioButton_children.IsChecked.GetValueOrDefault());

            if (this.forceCleanCheckBox.IsChecked.GetValueOrDefault()
                && !string.IsNullOrWhiteSpace(dte?.Solution.FullName))
            {
                FileUtils.AttemptHardClean(dte);
            }

            #region UI update

            this.StatusLabel.Visibility = Visibility.Visible;

            string returnMessage;

            switch (result)
            {
                case Constants.Processeskillsuccess:
                    {
                        returnMessage = "Processes killed.";
                        this.StatusLabel.Foreground = Brushes.Green;
                        break;
                    }
                case Constants.Processesnotfound:
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

        private void LoadChildProcessesButtonClick(object sender, RoutedEventArgs e)
        {
            var childProcesses = WindowsNative.GetChildProcesses(WindowsNative.GetCurrentProcess().Id);

            // TODO: Remove this on release
#if DEBUG
            var processes = WindowsNative.GetCurrentUserProcesses();
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

        private void ProcessCustomDisplayDataGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var value = (e.Column.GetCellContent(e.Row) as TextBox)?.Text;

            if (!string.IsNullOrWhiteSpace(value))
            {
                var element = this.CustomProcesses.ElementAt(e.Row.GetIndex());

                if (value != element.ProcessName)
                {
                    if (e.Row.IsEditing)
                    {
                        element.ProcessName = value;
                    }
                    else
                    {
                        this.CustomProcesses.Add(new CustomProcessInfo(value));
                        this.RefreshCustomProcessDisplayDataGrid();
                    }
                }
            }
            else
            {
                if ((e.EditAction == DataGridEditAction.Commit) && !e.Row.IsNewItem)
                {
                    this.CustomProcesses.RemoveAt(e.Row.GetIndex());
                }

                // NOTE: calling .RemoveAll can cause a ArgumentOutOfRangeException 
                // in the RefreshCustomProcessDisplayDataGrid null assignment. Possible compiler bug?
                this.CustomProcesses = this.CustomProcesses.Where(_ => !string.IsNullOrWhiteSpace(_.ProcessName)).ToList();
                this.RefreshCustomProcessDisplayDataGrid();
            }

            this.ResetCustomProcessDisplayDataGridFocus();
        }

        #endregion

        #region Load/Unload events

        private void ReallyStopDebuggerConfigLoaded(object sender, RoutedEventArgs e)
        {
            this.StatusLabel.Content = string.Empty;
            this.LoadExtensionSettings();
        }

        private void ReallyStopDebuggerConfigUnloaded(object sender, RoutedEventArgs e)
        {
            this.processDisplayDataGrid.ItemsSource = null;
            this.SaveExtensionSettings();
        }

        #endregion

        #region Helper Methods

        private void RefreshProcessDisplayDataGrid()
        {
            this.processDisplayDataGrid.ItemsSource = null;
            this.processDisplayDataGrid.ItemsSource = this.Processes;
        }

        private void RefreshCustomProcessDisplayDataGrid()
        {
            this.processCustomDisplayDataGrid.ItemsSource = null;
            this.processCustomDisplayDataGrid.ItemsSource = this.CustomProcesses;
        }

        private void ResetCustomProcessDisplayDataGridFocus()
        {
            // Reset cell focus to last cell
            this.processCustomDisplayDataGrid.CurrentCell = new DataGridCellInfo(
            this.processCustomDisplayDataGrid.Items[this.processCustomDisplayDataGrid.Items.Count > 0 ? this.processCustomDisplayDataGrid.Items.Count - 1 : 0], this.processCustomDisplayDataGrid.Columns[0]);
            this.processCustomDisplayDataGrid.UnselectAllCells();
            this.processCustomDisplayDataGrid.BeginEdit();
        }

        private void SaveExtensionSettings()
        {
            if (this.SettingsManager != null)
            {
                var userSettingsStore = this.SettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                var collectionExists = userSettingsStore.CollectionExists("ReallyStopDebugger");

                if (!collectionExists)
                {
                    userSettingsStore.CreateCollection("ReallyStopDebugger");
                }

                var customProcesses = string.Join(
                    "\r\n",
                    this.CustomProcesses.Select(_ => _.ProcessName)
                        .Where(_ => !string.IsNullOrWhiteSpace(_))
                        .Distinct()
                        .ToList());

                userSettingsStore.SetString("ReallyStopDebugger", "CustomProcessList", customProcesses);
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
        }

        private void LoadExtensionSettings()
        {
            if (this.SettingsManager != null)
            {
                var configurationSettingsStore = this.SettingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                var collectionExists = configurationSettingsStore.CollectionExists("ReallyStopDebugger");

                if (collectionExists)
                {
                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "CustomProcessList"))
                    {
                        var propertyValue = configurationSettingsStore.GetString("ReallyStopDebugger", "CustomProcessList")
                                            ?? string.Empty;
                        var customProcesses =
                            propertyValue.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                .Distinct()
                                .Select(_ => new CustomProcessInfo(_))
                                .ToList();

                        this.CustomProcesses.Clear();
                        this.CustomProcesses.AddRange(customProcesses);
                        this.RefreshCustomProcessDisplayDataGrid();
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ForceClean"))
                    {
                        this.forceCleanCheckBox.IsChecked =
                            Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "ForceClean"));
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "UserProcessMatch"))
                    {
                        this.userCriteriaRadioButton_userOnly.IsChecked =
                            Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "UserProcessMatch"));
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ChildProcessMatch"))
                    {
                        this.userCriteriaRadioButton_userOnly.IsChecked =
                            Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "ChildProcessMatch"));
                    }
                }
            }
        }

        #endregion
    }
}