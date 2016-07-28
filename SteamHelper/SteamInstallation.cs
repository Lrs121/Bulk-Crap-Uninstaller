﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace SteamHelper
{
    internal class SteamInstallation
    {
        private static SteamInstallation _instance;

        private SteamInstallation()
        {
            InstallationDirectory = FindSteamInstallationLocation();
            MainExecutableFilename = Path.Combine(InstallationDirectory, "Steam.exe");
            SteamAppsLocations = FindSteamAppsLocations(InstallationDirectory);
        }

        public static SteamInstallation Instance => _instance ?? (_instance = new SteamInstallation());

        public string InstallationDirectory { get; }
        public IEnumerable<string> SteamAppsLocations { get; }
        public string MainExecutableFilename { get; }

        private static IEnumerable<string> FindSteamAppsLocations(string installationDirectory)
        {
            var steamApps = new List<string> {Path.Combine(installationDirectory, @"SteamApps")};

            var libFoldersFile = Path.Combine(steamApps[0], @"libraryfolders.vdf");
            if (File.Exists(libFoldersFile))
            {
                int dummy;
                foreach (var str in File.ReadAllLines(libFoldersFile))
                {
                    var pieces = str.Split('\"').Where(p => !string.IsNullOrEmpty(p?.Trim())).ToList();
                    if (pieces.Count != 2 || !int.TryParse(pieces[0], out dummy)) continue;
                    if (Directory.Exists(pieces[1]))
                        steamApps.Add(pieces[1].Replace(@"\\", @"\"));
                }
            }
            return steamApps;
        }

        private static string FindSteamInstallationLocation()
        {
            foreach (var keyPath in new[] {@"SOFTWARE\Valve\Steam", @"SOFTWARE\WOW6432Node\Valve\Steam"})
            {
                using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null) continue;

                    var path = key.GetValue(@"InstallPath") as string;
                    if (path != null && Directory.Exists(path))
                        return path;
                }
            }

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\steam\Shell\Open\Command"))
                {
                    var command = key?.GetValue(null) as string;
                    var path = Path.GetDirectoryName(Misc.SeparateArgsFromCommand(command).Key);

                    if (path != null && Directory.Exists(path))
                        return path;
                }
                throw new IOException();
            }
            catch (Exception ex)
            {
                throw new IOException(
                    "Failed to detect your Steam installation. Launch Steam, exit it gracefully, and try again.", ex);
            }
        }
    }
}