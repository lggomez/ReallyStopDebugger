// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
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

        public MyControl()
        {
            InitializeComponent();

            //Default control states
            processCriteriaRadioButton_allProcesses.IsChecked = true;
            userCriteriaRadioButton_allUsers.IsChecked = true;
            StatusLabel.Visibility = Visibility.Hidden;

            Loaded += ReallyStopDebuggerConfig_Loaded;
            Unloaded += ReallyStopDebuggerConfig_Unloaded;
        }

        #region Click events

        private void killProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            #region Initialize

            if (currentPackage == null)
            {
                //The control was loaded in a faulted way or before the package initialization
                StatusLabel.Content = string.Format("Visual Studio instance not found.{0}Please reopen this window and try again", Environment.NewLine);
                StatusLabel.Foreground = Brushes.Red;
                StatusLabel.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                StatusLabel.Content = string.Empty;
            }

            #endregion

            #region Stop debug mode

            var dte = ((ReallyStopDebuggerPackage)currentPackage).GetDte();

            try
            {
                //Stop local VS debug
                if (dte != null)
                {
                    dte.ExecuteCommand("Debug.StopDebugging");
                }
            }
            catch (COMException)
            { //The command Debug.StopDebugging is not available
            }

            #endregion

            var processNames = processesTextBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = ProcessHelper.KillProcesses(currentPackage, processNames, userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault());

            if (forceCleanCheckBox.IsChecked.GetValueOrDefault() && !string.IsNullOrWhiteSpace(dte.Solution.FullName))
            {
                FileUtils.AttemptHardClean(dte);
            }

            #region UI update

            StatusLabel.Visibility = Visibility.Visible;

            string returnMessage;

            switch (result)
            {
                case Constants.PROCESSESKILLSUCCESS:
                    {
                        returnMessage = "Processes killed.";
                        StatusLabel.Foreground = Brushes.Green;
                        break;
                    }
                case Constants.PROCESSESNOTFOUND:
                    {
                        returnMessage = "Could not find any matching processes.";
                        StatusLabel.Foreground = Brushes.Orange;
                        break;
                    }
                default:
                    {
                        returnMessage = "Could not close orphaned processes due to an error.";
                        StatusLabel.Foreground = Brushes.Red;
                        break;
                    }
            }

            StatusLabel.Content = returnMessage;

            #endregion
        }

        private void loadChildProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            var childProcesses = WindowsInterop.GetChildProcesses(WindowsInterop.GetCurrentProcess().Id);

            if (childProcesses.Any())
            {
                var childProcessesNames = childProcesses
                    .GroupBy(p => p.ProcessName, StringComparer.InvariantCultureIgnoreCase)
                    .Select(g => g.First().ProcessName)
                    .ToList();

                var processesNames = processesTextBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
                processesNames.AddRange(childProcessesNames);

                processesNames = processesNames
                    .GroupBy(_ => _, StringComparer.InvariantCultureIgnoreCase)
                    .Select(_ => _.First())
                    .ToList();

                processesTextBox.Text = string.Empty;

                processesNames.ForEach(
                    _ =>
                        {
                            if (!string.IsNullOrWhiteSpace(processesTextBox.Text))
                            {
                                processesTextBox.Text += Environment.NewLine + _.Trim();
                            }
                            else
                            {
                                processesTextBox.Text += _.Trim();
                            }
                        });
            }
        }

        #endregion

        #region Load/Unload events

        private void ReallyStopDebuggerConfig_Loaded(object sender, RoutedEventArgs e)
        {
            StatusLabel.Content = string.Empty;

            #region Load settings

            if (settingsManager != null)
            {
                var configurationSettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                var collectionExists = configurationSettingsStore.CollectionExists("ReallyStopDebugger");

                if (collectionExists)
                {
                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ProcessList"))
                    {
                        processesTextBox.Text = configurationSettingsStore.GetString("ReallyStopDebugger", "ProcessList") ?? string.Empty;
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ForceClean"))
                    {
                        forceCleanCheckBox.IsChecked = Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "ForceClean"));
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "UserProcessMatch"))
                    {
                        userCriteriaRadioButton_userOnly.IsChecked = Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "UserProcessMatch"));
                    }

                    if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "ChildProcessMatch"))
                    {
                        userCriteriaRadioButton_userOnly.IsChecked = Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "ChildProcessMatch"));
                    }
                }
            }

            #endregion
        }

        private void ReallyStopDebuggerConfig_Unloaded(object sender, RoutedEventArgs e)
        {
            #region Save settings

            if (settingsManager != null)
            {
                var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                var collectionExists = userSettingsStore.CollectionExists("ReallyStopDebugger");

                if (!collectionExists)
                {
                    userSettingsStore.CreateCollection("ReallyStopDebugger");
                }

                userSettingsStore.SetString("ReallyStopDebugger", "ProcessList", processesTextBox.Text);
                userSettingsStore.SetString("ReallyStopDebugger", "ForceClean", forceCleanCheckBox.IsChecked.GetValueOrDefault().ToString());
                userSettingsStore.SetString("ReallyStopDebugger", "UserProcessMatch", userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault().ToString());
                userSettingsStore.SetString("ReallyStopDebugger", "ChildProcessMatch", userCriteriaRadioButton_userOnly.IsChecked.GetValueOrDefault().ToString());
            }

            #endregion
        }

        #endregion
    }
}