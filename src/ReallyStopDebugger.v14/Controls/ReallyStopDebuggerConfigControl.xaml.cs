// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;

using ReallyStopDebugger.Common;
using ReallyStopDebugger.Native;

namespace ReallyStopDebugger.Controls
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
            DoProcessLoadWork(this.LoadProcessDependencies);
            this.RefreshProcessDisplayDataGrid();
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
                this.CustomProcesses =
                    this.CustomProcesses.Where(_ => !string.IsNullOrWhiteSpace(_.ProcessName)).ToList();
                this.RefreshCustomProcessDisplayDataGrid();
            }

            this.ResetCustomProcessDisplayDataGridFocus();
        }

        #endregion

        #region Workers and process handling code

        public void DoProcessLoadWork(Action<IProgress<string>> processLoadAction)
        {
            SplashWindow splash = new SplashWindow();

            splash.Loaded += (_, eventArgs) =>
            {
                BackgroundWorker worker = new BackgroundWorker();
                Progress<string> progress = new Progress<string>(data => splash.label.Content = data);

                worker.DoWork += (s, workEventArgs) => processLoadAction(progress);
                worker.RunWorkerCompleted += (s, completedEventArgs) => splash.Close();

                worker.RunWorkerAsync();
            };

            this.CalculateSplashCoordinates(splash);
            splash.ShowDialog();
        }

        private void LoadProcessDependencies(IProgress<string> progress)
        {
            var childProcesses = LoadChildProcesses(progress);
#if DEBUG
            // TODO: Remove this on release
            var processes = WindowsNative.GetCurrentUserProcesses();
            childProcesses.AddRange(processes);
#endif
            this.MapProcessResults(progress, childProcesses);
        }

        private static List<Process> LoadChildProcesses(IProgress<string> progress)
        {
            progress.Report("Loading process dependencies");
            var childProcesses = WindowsNative.GetChildProcesses(WindowsNative.GetCurrentProcess().SafeGetProcessId());
            return childProcesses;
        }

        private void MapProcessResults(IProgress<string> progress, List<Process> childProcesses)
        {
            progress.Report("Mapping results to grid");
            if (childProcesses.Any())
            {
                var childProcessesInfo =
                    childProcesses.GroupBy(p => p.SafeGetProcessName(), StringComparer.InvariantCultureIgnoreCase)
                        .Select(g => new ProcessInfo(g))
                        .Where(c => !string.IsNullOrEmpty(c.ProcessName))
                        .ToList();

                this.AllProcessesSelected = false;
                this.Processes = childProcessesInfo;
            }
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
            this.StatusLabel.Content = string.Empty;
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
            this.processCustomDisplayDataGrid.CurrentCell =
                new DataGridCellInfo(
                    this.processCustomDisplayDataGrid.Items[
                        this.processCustomDisplayDataGrid.Items.Count > 0
                            ? this.processCustomDisplayDataGrid.Items.Count - 1
                            : 0],
                    this.processCustomDisplayDataGrid.Columns[0]);
            this.processCustomDisplayDataGrid.UnselectAllCells();
            this.processCustomDisplayDataGrid.BeginEdit();
        }

        private void CalculateSplashCoordinates(SplashWindow splash)
        {
            Point screenCoordinates = this.MyToolWindow.PointToScreen(new Point(0, 0));
            splash.Left = screenCoordinates.X + (this.MyToolWindow.Width / 2) - (splash.Width / 2);
            splash.Top = screenCoordinates.Y + (this.MyToolWindow.Height / 2) - splash.Height;
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
                var configurationSettingsStore =
                    this.SettingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                var collectionExists = configurationSettingsStore.CollectionExists("ReallyStopDebugger");

                if (collectionExists)
                {
                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "CustomProcessList"))
                    {
                        var propertyValue = configurationSettingsStore.GetString(
                                                "ReallyStopDebugger",
                                                "CustomProcessList") ?? string.Empty;
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
        }

        #endregion
    }
}