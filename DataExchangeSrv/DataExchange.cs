using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using DataExchangeAPP.Class;
using System.Threading;
using System.Timers;

namespace DataExchangeSrv
{
    public partial class DataExchange : ServiceBase
    {
        private System.Timers.Timer serviceTimer;

        public DataExchange()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            MainApp StartSrv = new MainApp();
            StartSrv.StartService();
        }


        protected override void OnStart(string[] args)
        {

            MainApp StartSrv = new MainApp();
            Thread fInparallel_StartSrv = new Thread(StartSrv.StartService);



            fInparallel_StartSrv.Name = "MainApp";
            fInparallel_StartSrv.IsBackground = true;
            fInparallel_StartSrv.Start();



            //serviceTimer = new System.Timers.Timer();
            //serviceTimer.Interval = 60000; //1 minute
            //serviceTimer.Elapsed += serviceTimer_Elapsed;
            //serviceTimer.Enabled = true;

        }


        void serviceTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            serviceTimer.Enabled = false;



            serviceTimer.Enabled = true;
        }

        protected override void OnStop()
        {
        }
    }
}
