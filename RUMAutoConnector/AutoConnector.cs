using Microsoft.ClearScript.V8;
using SimpleWifi;
using SimpleWifi.Win32;
using SimpleWifi.Win32.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RUMAutoConnector
{
    public class AutoConnector
    {
        private static readonly HttpClient client = new HttpClient();

        public static void Connect() => Connect(null, null);

        public static void Connect(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Disabled)
                return;

            Application.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    bool result = await Request();
                    if (result)
                    {
                        MainWindow.Instance.Risultato.Content = "Connesso con successo alle " + DateTime.Now + "!";
                    }
                    else
                    {
                        MainWindow.Instance.Risultato.Content = "Non riesco a connettermi a nessuna rete.";
                    }
                }
                catch (AppException ex)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MainWindow.Instance.Risultato.Content = ex.Message;
                    }));
                }
            });
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                Ping myPing = new Ping();
                String host = "google.com";
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

            WlanClient c = new WlanClient();
            if (c.Interfaces.First() != null)
            {
                if (c.Interfaces.First().InterfaceState != WlanInterfaceState.Disconnected)
                {
                    var interfaceFound = c.Interfaces.FirstOrDefault(x => x.CurrentConnection.profileName == "ergo-rum");
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
                        Wifi wifi = new Wifi();
                        var ap = wifi.GetAccessPoints();
                        var ergo = ap.FirstOrDefault(x => x.Name == "ergo-rum");
                        if (ergo != null)
                        {
                            throw new AppException("Sei connesso ad un'altra rete ma la rete ergo è nelle vicinanze! Connettiti no?");
                        }
                        throw new AppException("La rete ergo-rum non è nelle vicinanze.");
                    }
                }
                else
                {
                    Wifi wifi = new Wifi();
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
            using (var engine = new V8ScriptEngine())
            {
                engine.Execute(Properties.Resources.md5 +
                    $"var result = hexMD5('\\344' + '{Properties.Settings.Default.Password}' + '\\243\\042\\032\\371\\306\\204\\103\\331\\142\\162\\372\\327\\010\\027\\141\\247');");
                string md5 = engine.Script.result;

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
}
