﻿using Microsoft.Win32;
using System;

namespace AutoThemeChanger
{
    class RegEditHandler
    {
        public void SwitchThemeBasedOnTime()
        {
            TaskShedHandler task = new TaskShedHandler();
            var hour = DateTime.Now.Hour;
            var minute = DateTime.Now.Minute;
            var lightStart = task.GetRunTime("light");
            var darkStart = task.GetRunTime("dark");

            if (hour < lightStart[0] || hour >= darkStart[0])
            {
                if (hour == darkStart[0])
                {
                    if (minute < darkStart[1])
                    {
                        ThemeToLight();
                        MainWindow.ThemeSettingDark = false;
                        Console.WriteLine("Theme to Light");
                    }
                    if (minute >= darkStart[1])
                    {
                        ThemeToDark();
                        MainWindow.ThemeSettingDark = true;
                        Console.WriteLine("Theme to Dark");
                    }
                }
                else
                {
                    ThemeToDark();
                    MainWindow.ThemeSettingDark = true;
                    Console.WriteLine("Theme to Dark");
                }
            }
            else if (hour >= lightStart[0] || hour < darkStart[0])
            {
                if(hour == lightStart[0])
                {
                    if(minute < lightStart[1])
                    {
                        ThemeToDark();
                        MainWindow.ThemeSettingDark = true;
                        Console.WriteLine("Theme to Dark");
                    }
                    if(minute >= lightStart[1])
                    {
                        ThemeToLight();
                        MainWindow.ThemeSettingDark = false;
                        Console.WriteLine("Theme to Light");
                    }
                }
                else
                {
                    ThemeToLight();
                    MainWindow.ThemeSettingDark = false;
                    Console.WriteLine("Theme to Light");
                }
            }
        }

        public void ThemeToDark()
        {
            if (Properties.Settings.Default.AppThemeChange.Equals(0)) AppTheme(0);
            if (Properties.Settings.Default.SystemThemeChange.Equals(0)) SystemTheme(0);
            if (Properties.Settings.Default.EdgeThemeChange.Equals(0)) EdgeTheme(1);
            if (Properties.Settings.Default.AccentColor && Properties.Settings.Default.SystemThemeChange.Equals(0)) ColorPrevalence(1);
        }

        public void ThemeToLight()
        {
            if (Properties.Settings.Default.AppThemeChange.Equals(0)) AppTheme(1);
            if (Properties.Settings.Default.SystemThemeChange.Equals(0)) SystemTheme(1);
            if (Properties.Settings.Default.EdgeThemeChange.Equals(0)) EdgeTheme(0);
            if (Properties.Settings.Default.AccentColor && Properties.Settings.Default.SystemThemeChange.Equals(0)) ColorPrevalence(0);
        }

        public void AppTheme(int theme)
        {
            GetKey().SetValue("AppsUseLightTheme", theme, RegistryValueKind.DWord);
        }

        public void SystemTheme(int theme)
        {
            GetKey().SetValue("SystemUsesLightTheme", theme, RegistryValueKind.DWord);
        }

        public void EdgeTheme(int theme)
        {
            GetEdgeKey().SetValue("Theme", theme, RegistryValueKind.DWord);
        }

        public void ColorPrevalence(int theme)
        {
            GetKey().SetValue("ColorPrevalence", theme, RegistryValueKind.DWord);
        }

        public bool AppsUseLightTheme()
        {
            var keyValue = GetKey().GetValue("AppsUseLightTheme");
            if ((int)keyValue == 1) return true;
            else return false;
        }

        public bool SystemUsesLightTheme()
        {
            var keyValue = GetKey().GetValue("SystemUsesLightTheme");
            if ((int)keyValue == 1) return true;
            else return false;
        }

        public string GetOSversion()
        {
            var osVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
            return osVersion;
        }

        private RegistryKey GetKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            return registryKey;
        }

        private RegistryKey GetEdgeKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\Main", true);
            return registryKey;
        }
    }
}
