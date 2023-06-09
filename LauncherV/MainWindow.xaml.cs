﻿using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Net;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace LauncherV
{
    public partial class MainWindow : Window
    {
        string launcherVersion = "1.0.1";
        string onlineVerLink = "https://raw.githubusercontent.com/KilLo445/LauncherV/master/version.txt";
        string updateDL = "https://github.com/KilLo445/LauncherV/releases/latest";

        // Paths
        private string rootPath;
        private string tempPath;
        private string vTemp;
        private string vModsBackup;

        private string gamePath;
        private string steamPath;

        // Files
        private string gtavExe;
        private string gtavExeName = "PlayGTAV.exe";

        // Bools
        bool firstRun;
        bool firstRunMessages = true;
        bool steamInstalled;
        bool gtavSteam;
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Supported Mods

        // Mod Files (.dll, .asi, etc)
        string[] modFiles = {
            "dinput8.dll",
            "ScriptHookV.dll",
            "ScriptHookVDotNet.asi",
            "ScriptHookVDotNet.ini",
            "ScriptHookVDotNet2.dll",
            "ScriptHookVDotNet2.xml",
            "ScriptHookVDotNet3.dll",
            "ScriptHookVDotNet3.xml",
            "OpenIV.asi",
            "NativeTrainer.asi",
            "TrainerV.asi",
            "trainerv.ini",
            "Menyoo.asi",
        };

        // Mod Folders (OpenIV Mods, etc)
        string[] modFolders = {
            "mods",
            "menyooStuff"
        };

        public MainWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            tempPath = Path.GetTempPath();
            vTemp = Path.Combine(tempPath, "LauncherV");

            VersionText.Text = $"v{launcherVersion}";

            CreateReg();
            FirstRun();
            GetCFG();

            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\KilLo\LauncherV", true))
            {
                regKey.SetValue("InstallPath", $"{rootPath}");
                regKey.SetValue("Version", $"{launcherVersion}");
                regKey.Close();
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void CheckForUpdates()
        {
            Version localVersion = new Version(launcherVersion);

            try
            {
                WebClient webClient = new WebClient();
                Version onlineVersion = new Version(webClient.DownloadString(onlineVerLink));

                if (onlineVersion.IsDifferentThan(localVersion))
                {
                    MessageBoxResult updateGUI = MessageBox.Show("An update for LauncherV has been found.\n\nWould you like to download it?", "Update found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (updateGUI == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(updateDL);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"{ex}");
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        private void CreateTemp()
        {
            Directory.CreateDirectory(vTemp);
        }

        private void DelTemp()
        {
            if (Directory.Exists(vTemp))
            {
                try
                {
                    Directory.Delete(vTemp, true);
                }
                catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }


        private void CreateReg()
        {
            RegistryKey key1 = Registry.CurrentUser.OpenSubKey(@"Software", true);
            key1.CreateSubKey("KilLo");
            RegistryKey key2 = Registry.CurrentUser.OpenSubKey(@"Software\\KilLo", true);
            key2.CreateSubKey("LauncherV");
            RegistryKey key3 = Registry.CurrentUser.OpenSubKey(@"Software\\KilLo\\LauncherV", true);
            key3.SetValue("DontEdit", "Please do not edit any of these values unless you know what you are doing, it may break things!");
            key1.Close();
            key2.Close();
            key3.Close();
        }

        private void GetCFG()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\KilLo\LauncherV"))
            {
                if (key != null)
                {
                    // GTA 5 Steam
                    Object obGTAVSteam = key.GetValue("GTAVSteam");
                    if (obGTAVSteam != null)
                    {
                        string strGTAVSteam = (obGTAVSteam as String);
                        if (strGTAVSteam != null)
                        {
                            if (strGTAVSteam == "1")
                            {
                                gtavSteam = true;
                            }
                            else
                            {
                                gtavSteam = false;
                            }
                        }
                        else
                        {
                            gtavSteam = false;
                        }
                    }

                    // Game Path
                    Object obGTAVPath = key.GetValue("GTAVPath");
                    if (obGTAVPath != null)
                    {
                        gamePath = (obGTAVPath as String);
                        gtavExe = Path.Combine(gamePath, $"{gtavExeName}");
                        vModsBackup = Path.Combine(gamePath, ".launcherv", "Grand Theft Auto V");
                        try
                        {
                            Directory.CreateDirectory(vModsBackup);
                        }
                        catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                    }

                    key.Close();
                }
            }
        }

        private void GetSteamInstall()
        {
            using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Valve\\Steam"))
            {
                if (regKey != null)
                {
                    Object obSteamPath = regKey.GetValue("InstallPath");
                    if (obSteamPath != null)
                    {
                        steamPath = (obSteamPath as String);
                        steamInstalled = true;
                    }
                    else
                    {
                        steamInstalled = false;
                    }
                }
                else
                {
                    steamInstalled = false;
                }
            }
        }

        private void LaunchGTAV()
        {
            GetSteamInstall();
            if (steamInstalled == true && gtavSteam == true)
            {
                LaunchGTA_Steam();
            }
            else
            {
                LaunchGTA_EXE();
            }
        }

        private void LaunchGTA_Steam()
        {
            string libraryFolders = Path.Combine(steamPath, "config", "libraryfolders.vdf");
            string libraryFoldersTxt = Path.Combine(vTemp, "libraryfolders.txt");

            CreateTemp();
            File.Delete(libraryFoldersTxt);
            File.Copy(libraryFolders, libraryFoldersTxt);

            string libraryFoldersContent = File.ReadAllText(libraryFoldersTxt);

            if (libraryFoldersContent.Contains("271590"))
            {
                try
                {
                    Process.Start("steam://rungameid/271590");
                    File.Delete(libraryFoldersTxt);
                    Application.Current.Shutdown();
                }
                catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            else
            {
                return;
            }
        }

        private void LaunchGTA_EXE()
        {
            try
            {
                Process.Start(gtavExe);
                Application.Current.Shutdown();
            }
            catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void FirstRun()
        {
            RegistryKey keyV = Registry.CurrentUser.OpenSubKey(@"Software\KilLo\LauncherV", true);
            Object obFirstRun = keyV.GetValue("FirstRun");

            if (obFirstRun == null)
            {
                firstRun = true;
            }
            if (obFirstRun != null)
            {
                string strFirstRun = (obFirstRun as String);
                if (strFirstRun == "0")
                {
                    firstRun = true;
                }
            }

            if (firstRun == true)
            {
                if (firstRunMessages == true)
                {
                    MessageBox.Show("Welcome to LauncherV", "LauncherV", MessageBoxButton.OK, MessageBoxImage.Information);
                    MessageBoxResult mbGTAVSteam = System.Windows.MessageBox.Show("Do you have GTA V install via Steam?", "LauncherV", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                    if (mbGTAVSteam == MessageBoxResult.Yes)
                    {
                        gtavSteam = true;
                        using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\KilLo\LauncherV", true))
                        {
                            regKey.SetValue("GTAVSteam", "1");
                            regKey.Close();
                        }
                    }
                    if (mbGTAVSteam == MessageBoxResult.No)
                    {
                        gtavSteam = false;
                        using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\KilLo\LauncherV", true))
                        {
                            regKey.SetValue("GTAVSteam", "0");
                            regKey.Close();
                        }
                        MessageBox.Show("This launcher was built for the Steam version of the game, other versions may work, however you may have to sign into Rockstar Games on launch.\n\nSupport for other versions may be added in the future.", "LauncherV", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                WinForms.FolderBrowserDialog gtavInstallPathDialog = new WinForms.FolderBrowserDialog();
                gtavInstallPathDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
                gtavInstallPathDialog.Description = "Please select your Grand Theft Auto V install folder.\n(Location with GTA5.exe)";
                gtavInstallPathDialog.ShowNewFolderButton = false;
                WinForms.DialogResult gtavResult = gtavInstallPathDialog.ShowDialog();

                if (gtavResult == WinForms.DialogResult.OK)
                {
                    gamePath = Path.Combine(gtavInstallPathDialog.SelectedPath);
                    gtavExe = Path.Combine(gamePath, $"{gtavExeName}");

                    if (!File.Exists(gtavExe))
                    {
                        SystemSounds.Exclamation.Play();
                        MessageBox.Show("Please select the location with GTA5.exe");
                        firstRunMessages = false;
                        FirstRun();
                    }
                    if (File.Exists(gtavExe))
                    {
                        keyV.SetValue("GTAVPath", gamePath);
                        keyV.SetValue("FirstRun", "1");
                        keyV.Close();
                    }
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            keyV.Close();
        }

        private void ResetLauncher()
        {
            try
            {
                RegistryKey keyV = Registry.CurrentUser.OpenSubKey(@"Software\KilLo\LauncherV", true);
                Object obFirstRun = keyV.GetValue("FirstRun");
                if (obFirstRun != null)
                {
                    string strFirstRun = (obFirstRun as String);
                    if (strFirstRun == "1")
                    {
                        keyV.SetValue("FirstRun", "0");
                    }
                    MessageBox.Show("Please restart LauncherV for the reset to take affect.", "LauncherV", MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}", "Error Resetting LauncherV", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlayGTA5_Click(object sender, RoutedEventArgs e)
        {
            LaunchGTAV();
        }

        private void MoveFile(string source, string dest)
        {
            if (File.Exists(source))
            {
                try
                {
                    File.Move(source, dest);
                }
                catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void MoveFolder(string source, string dest)
        {
            if (Directory.Exists(source))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dest);
                if (dirInfo.Exists == false) { Directory.CreateDirectory(dest); }
                DirectoryInfo dir = new DirectoryInfo(source);
                DirectoryInfo[] dirs = dir.GetDirectories();
                string[] files = Directory.GetFiles(source);
                foreach (string file in files)
                {
                    try
                    {
                        string name = Path.GetFileName(file);
                        string destFile = Path.Combine(dest, name);
                        if (name != "file") File.Move(file, destFile);
                    }
                    catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(dest, subdir.Name);
                    if (!Directory.Exists(temppath))
                        try
                        {
                            Directory.Move(subdir.FullName, temppath);
                        }
                        catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
                Directory.Delete(source);
            }
        }

        private void BackupMods_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool backupMods1 = false;

                try
                {
                    foreach (string i in modFiles)
                    {
                        MoveFile($"{gamePath}\\{i}", $"{vModsBackup}\\{i}");
                    }
                    foreach (string i in modFolders)
                    {
                        MoveFolder($"{gamePath}\\{i}", $"{vModsBackup}\\{i}");
                    }

                    backupMods1 = true;
                }
                catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }

                if (backupMods1 == true)
                {
                    MessageBox.Show("Mods successfully backed up!", "LauncherV", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show($"An error occured while backing up your mods, you most likely don't have any supported mods installed. But here is the error anyway.\n\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void RestoreMods_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (string i in modFiles)
                {
                    MoveFile($"{vModsBackup}\\{i}", $"{gamePath}\\{i}");
                }
                foreach (string i in modFolders)
                {
                    MoveFolder($"{vModsBackup}\\{i}", $"{gamePath}\\{i}");
                }
                MessageBox.Show("Mods successfully restored!", "LauncherV", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"An error occured while restoring your mods, you most likely don't have any mods backed up. But here is the error anyway.\n\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } 
        }

        private void PLAYSTEAM_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("steam://rungameid/271590");
            }
            catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void PLAYEXE_Click(object sender, RoutedEventArgs e)
        {
            LaunchGTA_EXE();
        }

        private void Settings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Image image = sender as Image;
                ContextMenu contextMenu = image.ContextMenu;
                contextMenu.PlacementTarget = image;
                contextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void ChangePath_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey keyV = Registry.CurrentUser.OpenSubKey(@"Software\KilLo\LauncherV", true);
            WinForms.FolderBrowserDialog gtavInstallPathDialog = new WinForms.FolderBrowserDialog();
            gtavInstallPathDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
            gtavInstallPathDialog.Description = "Please select your Grand Theft Auto V install folder.\n(Location with GTA5.exe)";
            gtavInstallPathDialog.ShowNewFolderButton = false;
            WinForms.DialogResult gtavResult = gtavInstallPathDialog.ShowDialog();

            if (gtavResult == WinForms.DialogResult.OK)
            {
                gamePath = Path.Combine(gtavInstallPathDialog.SelectedPath);
                gtavExe = Path.Combine(gamePath, $"{gtavExeName}");

                if (!File.Exists(gtavExe))
                {
                    SystemSounds.Exclamation.Play();
                    MessageBox.Show("Please select the location with GTA5.exe");
                }
                if (File.Exists(gtavExe))
                {
                    keyV.SetValue("GTAVPath", gamePath);
                    keyV.SetValue("FirstRun", "1");
                    keyV.Close();
                }
            }
        }

        private void OpenPathGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(gamePath);
            }
            catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void OpenPathBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(vModsBackup);
            }
            catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void DeleteMods_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult delMods = MessageBox.Show("Are you sure you want to delete all your backed up mods?\n\nThis cannot be undone.", "LauncherV", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (delMods == MessageBoxResult.Yes)
            {
                try
                {
                    if (Directory.Exists(vModsBackup))
                    {
                        Directory.Delete(vModsBackup, true);
                        Directory.CreateDirectory(vModsBackup);
                        MessageBox.Show("Mods deleted.", "LauncherV", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void ResetLauncherV_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resetSoftware = MessageBox.Show("Are you sure you want to reset LauncherV?", "Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (resetSoftware == MessageBoxResult.Yes)
            {
                try
                {
                    ResetLauncher();
                }
                catch (Exception ex) { MessageBox.Show($"{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        struct Version
        {
            internal static Version zero = new Version(0, 0, 0);

            private short major;
            private short minor;
            private short subMinor;

            internal Version(short _major, short _minor, short _subMinor)
            {
                major = _major;
                minor = _minor;
                subMinor = _subMinor;
            }
            internal Version(string _version)
            {
                string[] versionStrings = _version.Split('.');
                if (versionStrings.Length != 3)
                {
                    major = 0;
                    minor = 0;
                    subMinor = 0;
                    return;
                }

                major = short.Parse(versionStrings[0]);
                minor = short.Parse(versionStrings[1]);
                subMinor = short.Parse(versionStrings[2]);
            }

            internal bool IsDifferentThan(Version _otherVersion)
            {
                if (major != _otherVersion.major)
                {
                    return true;
                }
                else
                {
                    if (minor != _otherVersion.minor)
                    {
                        return true;
                    }
                    else
                    {
                        if (subMinor != _otherVersion.subMinor)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public override string ToString()
            {
                return $"{major}.{minor}.{subMinor}";
            }
        }
    }
}