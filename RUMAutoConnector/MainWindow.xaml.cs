﻿using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Windows;

namespace RUMAutoConnector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;

        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            //Collega il delegato che si sottoscrive all'evento del sistema operativo di cambio di rete wifi
            NetworkChange.NetworkAvailabilityChanged += AutoConnector.Connect;

            //All'avvio tenta la connessione rapida se il servizio è abilitato
            UpdateDisabilitaServizioButton();

            //Carica i dati e lo stato attuale
            LoadSavedInput();
            UpdateStato();

            //Inizia a eseguire il codice di check di connessione ogni 15 secondi per sicurezza
            StartDispatcherTimer();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Username = Username.Text;
            Properties.Settings.Default.Password = Password.Password;
            Properties.Settings.Default.Save();

            UpdateStato();
        }

        private void UpdateStato()
        {
            string stato;
            if (IsDataSaved())
            {
                stato = "hai salvato delle credeziali d'accesso. Tenterò di connettermi.";
            }
            else
            {
                stato = "non hai memorizzato credenziali di accesso";
            }
            Stato.Content = $"Stato: {stato}";
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
            dispatcherTimer.Interval = new TimeSpan(0, 0, 15);
            dispatcherTimer.Start();
        }

        private bool IsDataSaved()
        {
            return !string.IsNullOrEmpty(Properties.Settings.Default.Username) &&
                !string.IsNullOrEmpty(Properties.Settings.Default.Password);
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (!AutoConnector.CheckForInternetConnection())
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
            }
            else
            {
                Disabilita.Content = "Disabilita il servizio";
                AutoConnector.Connect();
            }
        }
    }
}