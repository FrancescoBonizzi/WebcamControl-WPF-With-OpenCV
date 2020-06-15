using System;
using System.Windows;

namespace WebcamWithOpenCV
{
    public partial class App : Application
    {
        public App()
        {
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Because sometimes the application doesn't really closes and stays in background
            Environment.Exit(0);
        }
    }
}
