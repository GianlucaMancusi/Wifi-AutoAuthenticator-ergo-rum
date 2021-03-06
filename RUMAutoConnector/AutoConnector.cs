﻿using Microsoft.ClearScript.V8;
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
        public const string SSID = "ergo-rum";

        public const int CHECK_AFTER_SECONDS = 10;
        public const int CHECK_CONNECTED_DEVICES_AFTER_REFRESHES = 30;
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
        /// Contatore di quanti dispositivi sono connessi, si aggiorna ogni CHECK_CONNECTED_DEVICES_AFTER_REFRESHES aggiornamenti del wifi.
        /// Per aggiornarsi pinga tutta la rete (genera 253 datagram sulla rete, è rischioso usarlo troppo)
        /// </summary>
        public static volatile uint ConnectedDevices = 0;
        public static uint CountdownToCountConnectedDevices = 2; //if 0 => count, it's a countdown that starts from CHECK_CONNECTED_DEVICES_AFTER_REFRESHES

        /// <summary>
        /// Metodo che richiama un tentativo di connessione al wifi ergo usato per sottoscriversi agli eventi
        /// </summary>
        public static void Connect(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                if(!MainWindow.Instance.IsDataSaved())
                {
                    MainWindow.Instance.Risultato.Text = "Sono in attesa di Username e Password.".Dateify();
                    IsConnected = false;
                    return;
                }
                if (Properties.Settings.Default.Disabled)
                {
                    MainWindow.Instance.Risultato.Text = "Il servizio risulta disabilitato.".Dateify();
                    return;
                }

                MainWindow.Instance.Risultato.Text = "Tentativo di connessione in corso...".Dateify();

                try
                {
                    bool result = await Request();
                    if (result)
                    {
                        MainWindow.Instance.Risultato.Text = $"Connessione effettuata alla rete {SSID}.".Dateify();
                        if(!IsConnected && !NotifiedConnectionSuccessOneTime)
                        {
                            MainWindow.Instance.notifyIcon.ShowNotify(MainWindow.Instance.Title, MainWindow.Instance.Risultato.Text.ToString());
                            IsConnected = true;
                            NotifiedConnectionSuccessOneTime = true;
                        }
                    }
                    else
                    {
                        MainWindow.Instance.Risultato.Text = "Non riesco a connettermi a nessuna rete.".Dateify();
                        IsConnected = false;
                    }
                }
                catch (AppException ex)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MainWindow.Instance.Risultato.Text = ex.Message.Dateify();
                        IsConnected = false;
                    }));
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MainWindow.Instance.Risultato.Text = "!"+ex.Message.Dateify();
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

        private static void CheckHowManyDevicesAreConnected()
        {
            CountdownToCountConnectedDevices--;
            if (CountdownToCountConnectedDevices != 0)
            {
                return;
            }
            Console.WriteLine("Pinging the whole network to count how many devices are connected...");
            CountdownToCountConnectedDevices = CHECK_CONNECTED_DEVICES_AFTER_REFRESHES;

            ConnectedDevices = 0;
            for (int i = 1; i < 254; i++)
            {
                PingIt(i);
            }
        }

        private static async void PingIt(int i)
        {
            Ping ping = new Ping();
            PingReply reply = await ping.SendPingAsync("10.250.62." + i);
            if (reply.Status == IPStatus.Success)
            {
                ConnectedDevices++;
                Console.WriteLine("10.250.62." + i + " found on this network!");
            }
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                MainWindow.Instance.DispConnessi.Content = ConnectedDevices;
            }));
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
                    var interfaceFound = wlan.Interfaces.FirstOrDefault(x => x.CurrentConnection.profileName == SSID);
                    if (interfaceFound != null)
                    {
                        if (interfaceFound.CurrentConnection.isState == WlanInterfaceState.Connected)
                        {
                            if (!CheckForInternetConnection())
                            {
                                return await Authenticate();
                            }
                            else
                            {
                                CheckHowManyDevicesAreConnected();
                                throw new AppException($"Sei connesso alla rete {SSID}.");
                            }
                        }
                        else throw new AppException($"Prova a riconnetterti alla rete {SSID}");
                    }
                    else
                    {
                        var ap = wifi.GetAccessPoints();
                        var ergo = ap.FirstOrDefault(x => x.Name == SSID);
                        if (ergo != null)
                        {
                            throw new AppException("Sei connesso ad un'altra rete ma la rete ergo è nelle vicinanze!");
                        }
                        throw new AppException($"La rete {SSID} non è nelle vicinanze.");
                    }
                }
                else
                {
                    var ap = wifi.GetAccessPoints();
                    var ergo = ap.FirstOrDefault(x => x.Name == SSID);
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
                                throw new AppException("Non sono riuscito a connettermi automaticamente alla rete. Prova manualmente :(");
                            }
                        }
                    }
                    throw new AppException($"La rete {SSID} non è nelle vicinanze.");
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
            else if (responseString.Contains("RADIUS server is not responding"))
            {
                throw new AppException("La rete RUM sembra avere dei problemi. Il server RADIUS non risponde, quindi non è un problema del tuo pc. Contattare l'assistenza.");
            }
            else
            {
                throw new AppException("Autenticazione fallita. Ricontrolla username e password.");
            }
        }
    }
}
