using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace lggomez.ReallyStopDebugger
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
            StatusLabel.Visibility = Visibility.Hidden;
            Loaded += ReallyStopDebuggerConfig_Loaded;
            Unloaded += ReallyStopDebuggerConfig_Unloaded;
        }

        private void killProcessesButton_Click(object sender, RoutedEventArgs e)
        {
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

            string[] processNames = processesTextBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = Common.ProcessHelper.KillProcesses(currentPackage, processNames, userProcessCheckBox.IsChecked.GetValueOrDefault());

            if (forceCleanCheckBox.IsChecked.GetValueOrDefault() && !string.IsNullOrWhiteSpace(dte.Solution.FullName))
            {
                Common.FileUtils.AttemptHardClean(dte);
            }
                
            #region UI update

            StatusLabel.Visibility = Visibility.Visible;

            string returnMessage;

            switch (result)
            {
                case Common.Constants.PROCESSESKILLSUCCESS:
                    {
                        returnMessage = "Processes killed.";
                        StatusLabel.Foreground = System.Windows.Media.Brushes.Green;
                        break;
                    }
                case Common.Constants.PROCESSESNOTFOUND:
                    {
                        returnMessage = "Could not find any matching processes.";
                        StatusLabel.Foreground = System.Windows.Media.Brushes.Orange;
                        break;
                    }
                default:
                    {
                        returnMessage = "Could not close orphaned processes due to an error.";
                        StatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                        break;
                    }
            }

            StatusLabel.Content = returnMessage;

            #endregion
        }

        private void ReallyStopDebuggerConfig_Loaded(object sender, RoutedEventArgs e)
        {
            #region Load settings

            SettingsStore configurationSettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            bool collectionExists = configurationSettingsStore.CollectionExists("ReallyStopDebugger");

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

                if (configurationSettingsStore.PropertyExists("ReallyStopDebugger", "UserProcess"))
                {
                    userProcessCheckBox.IsChecked = Convert.ToBoolean(configurationSettingsStore.GetString("ReallyStopDebugger", "UserProcess"));
                }
            }

            #endregion
        }

        private void ReallyStopDebuggerConfig_Unloaded(object sender, RoutedEventArgs e)
        {
            #region Save settings

            WritableSettingsStore userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            bool collectionExists = userSettingsStore.CollectionExists("ReallyStopDebugger");

            if (!collectionExists)
            {
                userSettingsStore.CreateCollection("ReallyStopDebugger");
            }

            userSettingsStore.SetString("ReallyStopDebugger", "ProcessList", processesTextBox.Text);
            userSettingsStore.SetString("ReallyStopDebugger", "ForceClean", forceCleanCheckBox.IsChecked.GetValueOrDefault().ToString());
            userSettingsStore.SetString("ReallyStopDebugger", "UserProcess", userProcessCheckBox.IsChecked.GetValueOrDefault().ToString());

            #endregion
        }
    }
}