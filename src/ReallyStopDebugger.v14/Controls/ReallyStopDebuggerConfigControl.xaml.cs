// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using EnvDTE;

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;

using ReallyStopDebugger.Common;
using ReallyStopDebugger.Native;
// ReSharper disable All

namespace ReallyStopDebugger.Controls
{
    using Constants = Common.Constants;
    using Process = System.Diagnostics.Process;
    using StackFrame = System.Diagnostics.StackFrame;

    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        #region Extension properties

        internal Package CurrentPackage { get; set; }

        internal SettingsManager SettingsManager { get; set; }

        internal DTE DTE { get; set; }

        #endregion

        #region Control properties

        public List<ProcessInfo> Processes { get; set; } = new List<ProcessInfo>();

        public List<CustomProcessInfo> CustomProcesses { get; set; } = new List<CustomProcessInfo>();

        public bool AllProcessesSelected { get; set; } 

        #endregion

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
            if (this.IsPackageStateInvalid()) return;

            this.StatusLabel.Content = string.Empty;
            var dte = ((ReallyStopDebuggerPackage)this.CurrentPackage).GetDte();

            this.StopDebugMode(dte);

            var result = this.KillProcesses();

            this.AttemptHardClean(dte);

            this.UpdateStatusFromResult(result);
        }

        private void UpdateStatusFromResult(int result)
        {
            string returnMessage;

            switch (result)
            {
                case Constants.Processeskillsuccess:
                    {
                        returnMessage = ReallyStopDebugger.Resources.ProcesseskillsuccessMessage;
                        this.StatusLabel.Foreground = Brushes.Green;
                        break;
                    }
                case Constants.Processesnotfound:
                    {
                        returnMessage = ReallyStopDebugger.Resources.ProcessesnotfoundMessage;
                        this.StatusLabel.Foreground = Brushes.Orange;
                        break;
                    }
                default:
                    {
                        returnMessage = ReallyStopDebugger.Resources.ProcessesDefaultMessage;
                        this.StatusLabel.Foreground = Brushes.Red;
                        break;
                    }
            }

            this.StatusLabel.Content = returnMessage;
        }

        private int KillProcesses()
        {
            var processNameList =
                this.processDisplayDataGrid.ItemsSource.Cast<ProcessInfo>()
                    .Where(p => p.IsSelected)
                    .Select(_ => _.ProcessName)
                    .Concat(this.GetCustomProcessNames())
                    .Distinct()
                    .ToList();

            var result = ProcessHelper.KillProcesses(
                this.CurrentPackage,
                processNameList,
                this.userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault(),
                this.processCriteriaRadioButton_children.IsChecked.GetValueOrDefault());

            return result;
        }

        private void AttemptHardClean(DTE dte)
        {
            if (this.forceCleanCheckBox.IsChecked.GetValueOrDefault() && !string.IsNullOrWhiteSpace(dte?.Solution.FullName))
            {
                FileUtils.AttemptHardClean(dte);
            }
        }

        private void StopDebugMode(DTE dte)
        {
            try
            {
                // Stop local VS debug
                dte?.ExecuteCommand("Debug.StopDebugging");
            }
            catch (COMException)
            {
                // The command Debug.StopDebugging is not available
            }
        }

        private bool IsPackageStateInvalid()
        {
            if (this.CurrentPackage == null)
            {
                // The control is in a faulted state or was instantiated before the package initialization
                this.StatusLabel.Content =
                    $"{ReallyStopDebugger.Resources.InvalidInstanceError_1}{Environment.NewLine}{ReallyStopDebugger.Resources.InvalidInstanceError_2}";
                this.StatusLabel.Foreground = Brushes.Red;

                return true;
            }
            return false;
        }

        private void LoadChildProcessesButtonClick(object sender, RoutedEventArgs e)
        {
            this.DoProcessLoadWork(this.LoadProcessDependencies);
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
            }

            // NOTE: calling .RemoveAll will cause a ArgumentOutOfRangeException 
            // in the RefreshCustomProcessDisplayDataGrid null assignment. This is related to
            // the datasource binding events in the wpf datagrid control
            this.CustomProcesses = this.CustomProcesses
                .Where(_ => !string.IsNullOrWhiteSpace(_.ProcessName))
                .GroupBy(_ => _.ProcessName)
                .Select(g => g.First())
                .ToList();
            this.RefreshCustomProcessDisplayDataGrid();
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
            this.MapProcessResults(progress, childProcesses);
        }

        private static List<Process> LoadChildProcesses(IProgress<string> progress)
        {
            progress.Report(ReallyStopDebugger.Resources.ProgressReport_1);
            var childProcesses = WindowsNative.GetChildProcesses(WindowsNative.GetCurrentProcess().SafeGetProcessId());
            return childProcesses;
        }

        private void MapProcessResults(IProgress<string> progress, List<Process> childProcesses)
        {
            progress.Report(ReallyStopDebugger.Resources.ProgressReport_2);
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
            this.AdjustWindowSize();
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

        private void AdjustWindowSize()
        {
            try
            {
                var window = this.DTE.ActiveWindow;

                for (int i = 1; i <= window.Collection.Count; i++)
                {
                    var item = window.Collection.Item(i);

                    if (item.Caption.Equals(
                        ReallyStopDebugger.Resources.ToolWindowTitle,
                        StringComparison.InvariantCultureIgnoreCase))
                    {
                        item.Height = (int)this.Height - 50;
                        item.Width = (int)this.Width;
                    }
                }
            }
            catch
            {
                // We don't want to interrupt window initialization in case of failure
            }
        }

        private List<string> GetCustomProcessNames()
        {
            return this.CustomProcesses.Select(_ => _.ProcessName)
                .Where(_ => !string.IsNullOrWhiteSpace(_))
                .Distinct()
                .ToList();
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

                var customProcesses = string.Join("\r\n", this.GetCustomProcessNames());

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
            else
            {
                Console.WriteLine(
                    $"{ReallyStopDebugger.Resources.SettingsManagerNotFound} at {new StackFrame().GetMethod().Name}");
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
            else
            {
                Console.WriteLine(
                    $"{ReallyStopDebugger.Resources.SettingsManagerNotFound} at {new StackFrame().GetMethod().Name}");
            }
        }

        #endregion
    }
}