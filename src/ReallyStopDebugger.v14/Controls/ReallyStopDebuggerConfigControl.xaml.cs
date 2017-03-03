// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
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
            this.VerifyElevatedProcess();

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

        private void UpdateStatusFromResult(ProcessOperationException result)
        {
            string returnMessage;

            switch (result.ResultCode)
            {
                case ProcessOperationResults.Success:
                    {
                        returnMessage = ReallyStopDebugger.Resources.ProcesseskillsuccessMessage;
                        this.StatusLabel.Foreground = Brushes.Green;
                        break;
                    }
                case ProcessOperationResults.NotFound:
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

        private ProcessOperationException KillProcesses()
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

            this.UpdateCustomProcesses(null, true);
            this.RefreshCustomProcessDisplayDataGrid();
            this.ResetCustomProcessDisplayDataGridFocus();
        }

        private void ProcessCustomDisplayDataGrid_OnSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(DataGridCell))
            {
                DataGrid grid = (DataGrid)sender;
                grid.BeginEdit(e);
            }
        }

        private void ProcessCriteriaRadioButton_byPort_OnChecked(object sender, RoutedEventArgs e)
        {
            this.PortsTextBox.IsEnabled = this.processCriteriaRadioButton_byPort.IsChecked.GetValueOrDefault();
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

        private void VerifyElevatedProcess()
        {
            try
            {
                var windowsIdentity = WindowsIdentity.GetCurrent();

                if (windowsIdentity.Owner
                    .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)
                    && (windowsIdentity.Owner == windowsIdentity.User))
                {
                    this.AdminStatusLabel.Content = "ENABLED";
                    this.AdminStatusLabel.Foreground = Brushes.Green;
                }
                else
                {
                    this.AdminStatusLabel.Content = "DISABLED";
                    this.AdminStatusLabel.Foreground = Brushes.Red;
                }
            }
            catch (Exception)
            {
                this.AdminStatusLabel.Content = "N/A";
                this.AdminStatusLabel.Foreground = Brushes.Red;
            }
        }


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
                .ToList();
        }

        private void SaveExtensionSettings()
        {
            if (this.SettingsManager != null)
            {
                var userSettingsStore = this.SettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                var collectionExists = userSettingsStore.CollectionExists(Constants.CollectionPath);

                if (!collectionExists)
                {
                    userSettingsStore.CreateCollection(Constants.CollectionPath);
                }

                var customProcesses = string.Join("\r\n", this.GetCustomProcessNames());

                userSettingsStore.SetString(Constants.CollectionPath, Constants.CustomProcessesProperty, customProcesses);
                userSettingsStore.SetString(
                    Constants.CollectionPath,
                    Constants.ForceCleanProperty,
                    this.forceCleanCheckBox.IsChecked.GetValueOrDefault().ToString());
                userSettingsStore.SetString(
                    Constants.CollectionPath,
                    Constants.UserProcessMatchProperty,
                    this.userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault().ToString());
                userSettingsStore.SetString(
                    Constants.CollectionPath,
                    Constants.ChildProcessMatchProperty,
                    this.userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault().ToString());
                userSettingsStore.SetString(
                    Constants.CollectionPath,
                    Constants.PortProcessMatchProperty,
                    this.processCriteriaRadioButton_byPort.IsChecked.GetValueOrDefault().ToString());
            }
            else
            {
                Console.WriteLine(
                    $"{ReallyStopDebugger.Resources.SettingsManagerNotFound} at {new StackFrame().GetMethod().Name}");
            }
        }

        private void LoadExtensionSettings()
        {
            // Default values. These may be overriden upon configuration store retrieval
            var customProcesses = Constants.DeafultFilter.Select(_ => new CustomProcessInfo(_)).ToList();

            if (this.SettingsManager != null)
            {
                var configurationSettingsStore =
                    this.SettingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                var collectionExists = configurationSettingsStore.CollectionExists(Constants.CollectionPath);

                if (collectionExists)
                {
                    if (configurationSettingsStore.PropertyExists(Constants.CollectionPath, Constants.CustomProcessesProperty))
                    {
                        var propertyValue = configurationSettingsStore.GetString(
                                                Constants.CollectionPath,
                                                Constants.CustomProcessesProperty) ?? string.Empty;
                        customProcesses.AddRange(
                            propertyValue.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(_ => new CustomProcessInfo(_))
                                .ToList());
                    }

                    if (configurationSettingsStore.PropertyExists(Constants.CollectionPath, Constants.ForceCleanProperty))
                    {
                        this.forceCleanCheckBox.IsChecked =
                            Convert.ToBoolean(configurationSettingsStore.GetString(Constants.CollectionPath, Constants.ForceCleanProperty));
                    }

                    if (configurationSettingsStore.PropertyExists(Constants.CollectionPath, Constants.UserProcessMatchProperty))
                    {
                        this.userCriteriaRadioButton_userOnly.IsChecked =
                            Convert.ToBoolean(
                                configurationSettingsStore.GetString(Constants.CollectionPath, Constants.UserProcessMatchProperty));
                        this.userCriteriaRadioButton_allUsers.IsChecked =
                            !this.userCriteriaRadioButton_userOnly.IsChecked;
                    }

                    if (configurationSettingsStore.PropertyExists(Constants.CollectionPath, Constants.ChildProcessMatchProperty))
                    {
                        this.processCriteriaRadioButton_children.IsChecked =
                            Convert.ToBoolean(
                                configurationSettingsStore.GetString(Constants.CollectionPath, Constants.ChildProcessMatchProperty));
                        this.processCriteriaRadioButton_allProcesses.IsChecked =
                            !this.processCriteriaRadioButton_children.IsChecked;
                    }

                    if (configurationSettingsStore.PropertyExists(Constants.CollectionPath, Constants.PortProcessMatchProperty))
                    {
                        this.processCriteriaRadioButton_byPort.IsChecked =
                            Convert.ToBoolean(
                                configurationSettingsStore.GetString(Constants.CollectionPath, Constants.PortProcessMatchProperty));
                        this.processCriteriaRadioButton_noPort.IsChecked =
                            !this.processCriteriaRadioButton_byPort.IsChecked;
                        this.PortsTextBox.IsEnabled = this.processCriteriaRadioButton_byPort.IsChecked.GetValueOrDefault();
                    }
                }
            }
            else
            {
                Console.WriteLine(
                    $"{ReallyStopDebugger.Resources.SettingsManagerNotFound} at {new StackFrame().GetMethod().Name}");
            }

            this.UpdateCustomProcesses(customProcesses, true);
            this.RefreshCustomProcessDisplayDataGrid();
        }

        private void UpdateCustomProcesses(List<CustomProcessInfo> processInfoList, bool? replaceAll = false)
        {
            // Calling .RemoveAll, .Clear or related method will cause a ArgumentOutOfRangeException 
            // in the RefreshCustomProcessDisplayDataGrid null assignment due to
            // the datasource binding events in the wpf datagrid control

            CustomProcessInfo[] copy = new CustomProcessInfo[this.CustomProcesses.Count];
            this.CustomProcesses.CopyTo(copy);
            var target = copy.Where(_ => !string.IsNullOrWhiteSpace(_.ProcessName))
                .Distinct()
                .GroupBy(_ => _.ProcessName)
                .Select(g => g.First())
                .ToList();

            if (!replaceAll.GetValueOrDefault() && (processInfoList != null))
            {
                target.AddRange(processInfoList);
            }

            this.CustomProcesses = target;
        }

        #endregion
    }
}