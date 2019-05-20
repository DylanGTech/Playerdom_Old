using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Path = System.IO.Path;
using System.IO.Compression;

namespace Playerdom.Win32.Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WebClient client = new WebClient();

        public string currentVersion = null;
        public string latestVersion = null;

        string currentDirectory = null;
        string installationPath = null;
        string versionFile = null;


        public MainWindow()
        {
            InitializeComponent();

            RefreshData();
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            string zipPath = Path.Combine(currentDirectory, "Playerdom.zip");
            client.DownloadFile("https://dylangtech.com/Playerdom/Playerdom.zip", zipPath);

            if (Directory.Exists(installationPath))
            {
                Directory.Delete(installationPath);
            }
            Directory.CreateDirectory(installationPath);

            ZipFile.ExtractToDirectory(zipPath, installationPath);

            File.Delete(zipPath);

            RefreshData();

        }



        private void RefreshData()
        {
            currentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            installationPath = Path.Combine(currentDirectory, "Playerdom");
            versionFile = Path.Combine(installationPath, "Version.txt");


            UpdateButton.Content = "Update";
            UpdateButton.IsEnabled = false;


            try
            {
                latestVersion = client.DownloadString("https://dylangtech.com/Playerdom/version.txt");
                LatestVersionLabel.Content = "Latest Version: " + latestVersion;

            }
            catch (Exception e)
            {
                LatestVersionLabel.Content = "Error retrieving latest version";
            }

            try
            {
                if (Directory.Exists(installationPath))
                {
                    if (File.Exists(versionFile))
                    {
                        currentVersion = File.ReadAllText(versionFile);
                        CurrentVersionLabel.Content = "Current Version: " + currentVersion;

                        if (currentVersion != latestVersion)
                        {
                            UpdateButton.Content = "Update";
                            UpdateButton.IsEnabled = true;
                        }


                    }
                    else
                    {
                        CurrentVersionLabel.Content = "Error retreiving current version";
                        UpdateButton.Content = "Install";
                        UpdateButton.IsEnabled = true;
                    }
                }
                else
                {
                    CurrentVersionLabel.Content = "Playerdom not installed";
                    UpdateButton.Content = "Update";
                    UpdateButton.IsEnabled = true;
                }
            }
            catch (Exception e)
            {
                CurrentVersionLabel.Content = "Error retreiving current version";
            }
        }
    }
}
