using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Win32;

namespace SUNTDLite
{
    public static class Utilities
    {
        private static readonly string UNINSTALL_REGKEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        public static bool WindowsHasInstalledApplication(string applicationName)
        {
            using(RegistryKey key = Registry.LocalMachine.OpenSubKey(UNINSTALL_REGKEY))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        var obj = subkey.GetValue("DisplayName");
                        if (obj != null && obj.ToString().Contains(applicationName))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
