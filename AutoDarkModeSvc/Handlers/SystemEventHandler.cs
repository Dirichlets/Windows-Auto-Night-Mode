﻿#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.System.Power;

namespace AutoDarkModeSvc.Handlers
{
    static class SystemEventHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool darkThemeOnBatteryEnabled;
        private static bool resumeEventEnabled;
        private static GlobalState state = GlobalState.Instance();
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        public static void RegisterThemeEvent()
        {
            if (!darkThemeOnBatteryEnabled)
            {
                Logger.Info("enabling event handler for dark mode on battery state discharging");
                PowerManager.PowerSupplyStatusChanged += PowerManager_BatteryStatusChanged;
                darkThemeOnBatteryEnabled = true;
            }
        }

        private static void PowerManager_BatteryStatusChanged(object sender, object e)
        {
            if (PowerManager.PowerSupplyStatus == PowerSupplyStatus.NotPresent)
            {
                Logger.Info("battery discharging, enabling dark mode");
                ThemeManager.UpdateTheme(Theme.Dark, new(SwitchSource.BatteryStatusChanged));
            }
            else
            {
                ThemeManager.RequestSwitch(new(SwitchSource.BatteryStatusChanged));
            }
        }

        public static void DeregisterThemeEvent()
        {
            try
            {
                if (darkThemeOnBatteryEnabled)
                {
                    Logger.Info("disabling event handler for dark mode on battery state discharging");
                    PowerManager.BatteryStatusChanged -= PowerManager_BatteryStatusChanged;
                    darkThemeOnBatteryEnabled = false;
                    ThemeManager.RequestSwitch(new(SwitchSource.BatteryStatusChanged));
                }
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, "while deregistering SystemEvents_PowerModeChanged ");
            }
        }

        public static void RegisterResumeEvent()
        {
            if (!resumeEventEnabled)
            {
                if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
                {
                    Logger.Info("enabling theme refresh at system unlock (win 11)");
                    SystemEvents.SessionSwitch += SystemEvents_Windows11_SessionSwitch;
                }
                else if (builder.Config.Events.Win10AllowLockscreenSwitch)
                {
                    Logger.Info("enabling theme refresh at system resume (win 10)");
                    SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                    SystemEvents.SessionSwitch += SystemEvents_Windows10_SessionSwitch;
                }
                else
                {
                    Logger.Info("enabling theme refresh at system unlock (win 10)");
                    SystemEvents.SessionSwitch += SystemEvents_Windows11_SessionSwitch;
                }

                resumeEventEnabled = true;
            }
        }

        public static void DeregisterResumeEvent()
        {
            try
            {
                if (resumeEventEnabled)
                {
                    Logger.Info("disabling theme refresh events");
                    state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
                    SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                    SystemEvents.SessionSwitch -= SystemEvents_Windows10_SessionSwitch;
                    SystemEvents.SessionSwitch -= SystemEvents_Windows11_SessionSwitch;
                    resumeEventEnabled = false;
                }
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, "while deregistering SystemEvents_PowerModeChanged ");
            }
        }

        private static void SystemEvents_Windows11_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {                
                if (builder.Config.AutoSwitchNotify.Enabled)
                {
                    NotifyAtResume();
                }
                else
                {
                    state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
                    if (!state.PostponeManager.IsSkipNextSwitch && !state.PostponeManager.IsUserDelayed)
                    {
                        Logger.Info("system unlocked, refreshing theme");
                        ThemeManager.RequestSwitch(new(SwitchSource.SystemUnlock));
                    }
                    else
                    {
                        Logger.Info($"system unlocked, no refresh due to active user postpones: {state.PostponeManager}");
                    }
                }                
            }
            else if (e.Reason == SessionSwitchReason.SessionLock)
            {
                state.PostponeManager.Add(new(Helper.PostponeItemSessionLock));
            }
        }

        private static void SystemEvents_Windows10_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                if (builder.Config.AutoSwitchNotify.Enabled)
                {
                    NotifyAtResume();
                }
            }
            else if (e.Reason == SessionSwitchReason.SessionLock)
            {
                if (builder.Config.AutoSwitchNotify.Enabled) state.PostponeManager.Add(new(Helper.PostponeItemSessionLock));
            }
        }


        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                if (builder.Config.AutoSwitchNotify.Enabled == false)
                {
                    if (!state.PostponeManager.IsSkipNextSwitch && !state.PostponeManager.IsUserDelayed)
                    {
                        Logger.Info("system resuming from suspended state, refreshing theme");
                        ThemeManager.RequestSwitch(new(SwitchSource.SystemUnlock));
                    }
                    else
                    {
                        Logger.Info($"system resuming from suspended state, no refresh due to active user postpones: {state.PostponeManager}");
                    }
                }
            }
        }


        private static void NotifyAtResume()
        {
            bool shouldNotify = false;
            if (builder.Config.Governor == Governor.NightLight)
            {
                if (state.NightLight.Requested != state.RequestedTheme) shouldNotify = true;
            }
            else if (builder.Config.Governor == Governor.Default)
            {
                TimedThemeState ts = new();
                if (ts.TargetTheme != state.RequestedTheme) shouldNotify = true;
            }

            if (shouldNotify)
            {
                Logger.Info("system unlocked, prompting user for theme switch");
                Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(o =>
                {
                    ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                    state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
                });
            }
            else
            {
                Logger.Info("system unlocked, theme state valid, not sending notification");
                state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
            }
        }
    }
}
