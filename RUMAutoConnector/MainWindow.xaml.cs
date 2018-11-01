using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace RUMAutoConnector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        public static MainWindow Instance;

        public NotifyIcon notifyIcon;
        public StatisticsManager statistics;

        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            try
            {
                Risultato.Text = "Sto avviando il servizio...";

                //Crea icona di notifica
                notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Visible = true,
                    Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                    Text = Title
                };
                notifyIcon.Click += NotifyIcon_Click;
                notifyIcon.BalloonTipClicked += NotifyIcon_Click;

                //Avviare solo un'app per volta
                if (Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
                {
                    System.Windows.MessageBox.Show("Programma già avviato. Probabilmente è attivo nella notifiche", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Error);
                    Process.GetCurrentProcess().Kill();
                }

                //Aggiunge l'app all'avvio automatico 
                if (/*Properties.Settings.Default.FirstStart &&*/ !Debugger.IsAttached)
                {
                    Properties.Settings.Default.FirstStart = false;
                    Properties.Settings.Default.Save();
                    Helper.AddToStartup();
                }

                //Collega il delegato che si sottoscrive all'evento del sistema operativo di cambio di rete wifi
                NetworkChange.NetworkAvailabilityChanged += AutoConnector.Connect;

                //All'avvio tenta la connessione rapida se il servizio è abilitato
                UpdateDisabilitaServizioButton();

                //Carica i dati e lo stato attuale
                LoadSavedInput();

                //Inizia a eseguire il codice di check di connessione ogni 15 secondi per sicurezza
                StartDispatcherTimer();
                notifyIcon.ShowNotify(Title, Risultato.Text.ToString());

                //Avvio il sistema di statistiche
                statistics = new StatisticsManager();
            }
            catch (Exception ex)
            {
                Risultato.Text = ex.Message.Dateify();
                notifyIcon.ShowNotify(Title, ex.Message);
            }
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                if(Properties.Settings.Default.FirstHide)
                {
                    Properties.Settings.Default.FirstHide = false;
                    Properties.Settings.Default.Save();
                    notifyIcon.ShowBalloonTip(1, Title, "La finestra rimarrà nascosta tra le icone", ToolTipIcon.Info);
                }
                return;
            }

            base.OnStateChanged(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Username = Username.Text;
            Properties.Settings.Default.Password = Password.Password;
            Properties.Settings.Default.Save();

            if (string.IsNullOrEmpty(Username.Text) || string.IsNullOrEmpty(Password.Password))
            {
                SaveButton.Content = "Salva e abilita";
                return;
            }
            
            SaveButton.Content = "Salvato!";
            SaveButton.FontWeight = FontWeights.Normal;
            AutoConnector.Connect();
        }

        private void LoadSavedInput()
        {
            Username.Text = Properties.Settings.Default.Username;
            Password.Password = Properties.Settings.Default.Password;
        }

        private void StartDispatcherTimer()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, AutoConnector.CHECK_AFTER_SECONDS);
            dispatcherTimer.Start();
        }

        public bool IsDataSaved()
        {
            return !string.IsNullOrEmpty(Properties.Settings.Default.Username) &&
                !string.IsNullOrEmpty(Properties.Settings.Default.Password);
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            AutoConnector.Connect();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            NetworkChange.NetworkAvailabilityChanged -= AutoConnector.Connect;
        }

        private void Disabilita_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Disabled = !Properties.Settings.Default.Disabled;

            UpdateDisabilitaServizioButton();

            Properties.Settings.Default.Save();
        }

        private void UpdateDisabilitaServizioButton()
        {
            if (Properties.Settings.Default.Disabled)
            {
                Disabilita.Content = "Abilita il servizio";
                Risultato.Text = "Servizio disabilitato.".Dateify();
            }
            else
            {
                Disabilita.Content = "Disabilita il servizio";
                AutoConnector.Connect();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    statistics.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MainWindow() {
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

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Notifications = !Properties.Settings.Default.Notifications;
            Properties.Settings.Default.Save();
            Notifications.IsChecked = Properties.Settings.Default.Notifications;
        }

        private void Username_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateSaveButton();
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateSaveButton();
        }

        private void UpdateSaveButton()
        {
            if(Properties.Settings.Default.Username != Username.Text ||
               Properties.Settings.Default.Password != Password.Password)
            {
                if (!string.IsNullOrEmpty(Username.Text) && !string.IsNullOrEmpty(Password.Password))
                {
                    SaveButton.FontWeight = FontWeights.Bold;
                }
                return;
            }
            SaveButton.FontWeight = FontWeights.Normal;
        }
    }
}
