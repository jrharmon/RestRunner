using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Threading;
using RestRunner.Properties;
using RestRunner.ViewModels;

namespace RestRunner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //check to see if this is a new version, and the old settings should be brought over (CallUpgrade is a dummy setting that is initialized as true)
            if (Settings.Default.CallUpgrade)
            {
                Settings.Default.Upgrade();
                Settings.Default.CallUpgrade = false;
                Settings.Default.Save();
            }

            //TEMPORARY: switch to the new save folder
            Settings.Default.SaveFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\AgileData Software\REST Runner";
            Settings.Default.Save();

            //make sure that the save folder is correct, and that it exists
            if (String.IsNullOrEmpty(Settings.Default.SaveFolder))
            {
                Settings.Default.SaveFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\AgileData Software\REST Runner";
                Settings.Default.Save();
            }
            if (!Directory.Exists(Settings.Default.SaveFolder))
                Directory.CreateDirectory(Settings.Default.SaveFolder);

            //increase the max number of outsbound connections the app can make
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;

            //support TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;

            //allow connection to servers with an invalid security certificate.  do not use this unless you are sure the servers are safe
            if (Settings.Default.IgnoreCertificateErrors)
                System.Net.ServicePointManager.ServerCertificateValidationCallback = ((s, certificate, chain, sslPolicyErrors) => true);
        }
    }
}
