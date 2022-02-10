using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace VKJsonToBD
{
    [RunInstaller(true)]
    public partial class Installer1 : System.Configuration.Install.Installer
    {
        ServiceInstaller servInstaller;
        ServiceProcessInstaller procInstaller;
        public Installer1()
        {
            InitializeComponent();

            servInstaller = new ServiceInstaller();
            procInstaller = new ServiceProcessInstaller();

            procInstaller.Account = ServiceAccount.LocalSystem;
            servInstaller.StartType = ServiceStartMode.Manual;
            servInstaller.ServiceName = "WinService";
            Installers.Add(procInstaller);
            Installers.Add(servInstaller);
        }
    }
}
