using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;using Microsoft.Win32;

namespace cAlgo
{
    public class cBotLink
    {
        public string SubKey { get; set; }

        public bool Read(string KeyName)
        {
            // Opening the registry key
            RegistryKey rk = Registry.CurrentUser;

            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(SubKey);
            // If the RegistrySubKey doesn't exist -> (null)
            if (sk1 == null)
            {
                return false;
            }
            else
            {
                var result = sk1.GetValue(KeyName);
                return Convert.ToBoolean(result);
            }
        }

        public int ReadNumber(string KeyName)
        {
            // Opening the registry key
            RegistryKey rk = Registry.CurrentUser;

            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(SubKey);
            if (sk1 == null)
            {
                return 0;
            }
            else
            {
                var result = sk1.GetValue(KeyName);
                return Convert.ToInt16(result);
            }
        }

        public bool Write(string KeyName, object Value)
        {
            // Setting
            RegistryKey rk = Registry.CurrentUser;

            // I have to use CreateSubKey 
            // (create or open it if already exits), 
            // 'cause OpenSubKey open a subKey as read-only
            RegistryKey sk1 = rk.CreateSubKey(SubKey);
            // Save the value
            sk1.SetValue(KeyName, Value);

            return true;

        }
    }
}
