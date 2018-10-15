using Microsoft.ClearScript.V8;
using SimpleWifi;
using SimpleWifi.Win32;
using SimpleWifi.Win32.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;

namespace RUMAutoConnector
{
    /// <summary>
    /// Classe che gestisce la connessione al wifi ergo
    /// </summary>
    public class AutoConnector
    {
        public const int CHECK_AFTER_SECONDS = 10;
        public static bool IsConnected = false;
        public static bool NotifiedConnectionSuccessOneTime = false;

        public static Wifi wifi = new Wifi();
        public static WlanClient wlan = new WlanClient();

        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Metodo che richiama un tentativo di connessione al wifi ergo
        /// </summary>
        public static void Connect() => Connect(null, null);

        /// <summary>
        /// Metodo che richiama un tentativo di connessione al wifi ergo usato per sottoscriversi agli eventi
        /// </summary>
        public static void Connect(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                if(!MainWindow.Instance.IsDataSaved())
                {
                    MainWindow.Instance.Risultato.Content = $"Sono in attesa di Username e Password. {DateTime.Now}";
                    IsConnected = false;
                    return;
                }
                if (Properties.Settings.Default.Disabled)
                {
                    MainWindow.Instance.Risultato.Content = $"Il servizio risulta disabilitato. {DateTime.Now}";
                    return;
                }

                MainWindow.Instance.Risultato.Content = $"Tentativo di connessione in corso... {DateTime.Now}";

                try
                {
                    bool result = await Request();
                    if (result)
                    {
                        MainWindow.Instance.Risultato.Content = $"Connesso con successo alle {DateTime.Now}";
                        if(!IsConnected && !NotifiedConnectionSuccessOneTime)
                        {
                            MainWindow.Instance.notifyIcon.ShowNotify(MainWindow.Instance.Title, MainWindow.Instance.Risultato.Content.ToString());
                            IsConnected = true;
                            NotifiedConnectionSuccessOneTime = true;
                        }
                    }
                    else
                    {
                        MainWindow.Instance.Risultato.Content = $"Non riesco a connettermi a nessuna rete. {DateTime.Now}";
                        IsConnected = false;
                    }
                }
                catch (AppException ex)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MainWindow.Instance.Risultato.Content = ex.Message + $" {DateTime.Now}";
                        IsConnected = false;
                    }));
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MainWindow.Instance.Risultato.Content = "!:" + ex.Message + $" {DateTime.Now}";
                        IsConnected = false;
                    }));
                }
            });
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                Ping myPing = new Ping();
                string host = "google.com";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task<bool> Request()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            
            if (wlan.Interfaces.First() != null)
            {
                if (wlan.Interfaces.First().InterfaceState != WlanInterfaceState.Disconnected)
                {
                    var interfaceFound = wlan.Interfaces.FirstOrDefault(x => x.CurrentConnection.profileName == "ergo-rum");
                    if (interfaceFound != null)
                    {
                        if (interfaceFound.CurrentConnection.isState == WlanInterfaceState.Connected)
                        {
                            if (!CheckForInternetConnection())
                            {
                                return await Authenticate();
                            }
                            else throw new AppException("Sei connesso alla rete ergo-rum.");
                        }
                        else throw new AppException("Prova a riconnetterti alla rete ergo-rum");
                    }
                    else
                    {
                        var ap = wifi.GetAccessPoints();
                        var ergo = ap.FirstOrDefault(x => x.Name == "ergo-rum");
                        if (ergo != null)
                        {
                            throw new AppException("Sei connesso ad un'altra rete ma la rete ergo è nelle vicinanze!");
                        }
                        throw new AppException("La rete ergo-rum non è nelle vicinanze.");
                    }
                }
                else
                {
                    var ap = wifi.GetAccessPoints();
                    var ergo = ap.FirstOrDefault(x => x.Name == "ergo-rum");
                    if(ergo != null)
                    {
                        if (!ergo.IsConnected)
                        {
                            //connect if not connected
                            Console.WriteLine("\r\n{0}\r\n", ergo.ToString());
                            Console.WriteLine("Trying to connect..\r\n");
                            AuthRequest authRequest = new AuthRequest(ergo);
                            var connected = ergo.Connect(authRequest);
                            if(connected)
                            {
                                return await Authenticate();
                            }
                            else
                            {
                                throw new AppException("Non sono riuscito a connettermi automaticamente alla rete. Prova manualmente.");
                            }
                        }
                    }
                    throw new AppException("La rete ergo-rum non è nelle vicinanze.");
                }
            }
            else throw new AppException("Non riesco ad usare il modulo wifi. Hai una scheda di rete nel tuo pc?");
        }

        private static async Task<bool> Authenticate()
        {
            var values = new Dictionary<string, string>
                                    {
                                        { "dst", "" },
                                        { "popup", "true" },
                                        { "username", Properties.Settings.Default.Username },
                                        { "password", Properties.Settings.Default.Password }
                                    };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://10.250.62.254/login", content);

            var responseString = await response.Content.ReadAsStringAsync();

            if (responseString.Contains("logged"))
            {
                return true;
            }
            else
            {
                throw new AppException("Autenticazione fallita. Ricontrolla username e password.");
            }
        }
    }
}
