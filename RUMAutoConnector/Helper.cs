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

        public static void AddToStartup()
        {
            try
            {
                const string HKCU = "HKEY_CURRENT_USER";
                const string RUN_KEY = @"SOFTWARE\\Microsoft\Windows\CurrentVersion\Run";
                string exePath = System.Windows.Forms.Application.ExecutablePath;
                Microsoft.Win32.Registry.SetValue(HKCU + "\\" + RUN_KEY, "Ergo-RUM-Wifi", exePath);
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
