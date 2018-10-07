using SimpleWifi.Win32.Interop;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;

namespace RUMAutoConnector
{
    public class StatisticsManager : IDisposable
    {
        private SimpleWifi.Wifi wifi;
        private SimpleWifi.Win32.WlanClient wlan;

        public StatisticsManager()
        {
            wifi = AutoConnector.wifi;
            wlan = AutoConnector.wlan;

            _refreshTimer = new Timer(_refreshTimer_Tick, null, 0, 1000);
        }

        public void GetStatistics()
        {
            try
            {
                var m = MainWindow.Instance;
                m.ConnessoAWifi.Content = wifi.ConnectionStatus;
                m.InterfacciaConnessa.Content = wlan.Interfaces?.First()?.InterfaceState != WlanInterfaceState.Disconnected;
                m.ConnessoAdInternet.Content = AutoConnector.CheckForInternetConnection();
                if (wlan.Interfaces.First().InterfaceState != WlanInterfaceState.Disconnected)
                {
                    m.ConnessoAdErgoRum.Content = wlan.Interfaces.Any(x => x.CurrentConnection.profileName == "ergo-rum");
                    m.Segnale.Content = wlan.Interfaces?.First()?.CurrentConnection.wlanAssociationAttributes.wlanSignalQuality + "/100";
                    m.Sicurezza.Content = wlan.Interfaces?.First()?.CurrentConnection.wlanSecurityAttributes.securityEnabled;
                }
                else
                {
                    m.ConnessoAdErgoRum.Content = false;
                    m.Segnale.Content = "non connesso";
                    m.Sicurezza.Content = false;
                }
                m.ConnessioneSpeed.Content = bytesPerSecond < 0 ? "0B" : bytesPerSecond.ToPrettySize();
            }
            catch { }
        }

        private long bytesPerSecond = 0;
        private System.Threading.Timer _refreshTimer;
        private void _refreshTimer_Tick(object state)
        {
            ThreadPool.QueueUserWorkItem(callback =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    GetStatistics();
                }));

                var a = NetworkInterface.GetAllNetworkInterfaces();
                

                long? beginValue = wlan.Interfaces?.First()?.NetworkInterface?.GetIPv4Statistics().BytesReceived;
                DateTime beginTime = DateTime.Now;

                Thread.Sleep(1000);

                long? endValue = wlan.Interfaces?.First()?.NetworkInterface?.GetIPv4Statistics().BytesReceived;
                DateTime endTime = DateTime.Now;

                long recievedBytes = endValue.GetValueOrDefault() - beginValue.GetValueOrDefault();
                double totalSeconds = (endTime - beginTime).TotalSeconds;
                
                bytesPerSecond = recievedBytes / (long)totalSeconds;
                if(bytesPerSecond != 0)
                {
                    ;
                }
            });

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _refreshTimer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~StatisticsManager() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
