using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace DataExchangeAPP.Class
{
    public class MainApp
    {


        // SAP TRANSACTION THREAD //
        List<Thread> SoapConnector = new List<Thread>();
        

        //Start rotating thread on File_Data_Import //
        List<Thread> File_Import_Connector= new List<Thread>();


        List<Thread> TrCollectorList = new List<Thread>();

        ////ROTATING CONTROL ON ONGUARD DB //
        //TransactionCollector TrCollector = new TransactionCollector();


        public void StartService ()

        {
            bool fbExit = false;
            Logs LogSystem = new Logs();
            DataBaseConnection fdb = new DataBaseConnection();



            List<String> siteNames = fdb.ReturnDataList("Select Country From SAP_WebServer;", "Country");



            // Start sending data to SOAP //



            try
            {

                LogSystem.LogMessage(this.GetType().Name, "Starting Thread SAP Transaction Connector");


                foreach (string Site in siteNames)
                {
                    CreateSAPTransaction SapTransaction = new CreateSAPTransaction();
                    Thread fInparallelSoapConnector = new Thread((SapTransaction.StartThreadService));
                    SoapConnector.Add(fInparallelSoapConnector);
                    object SiteObj;
                    SiteObj = Site;
                    LogSystem.LogMessage(this.GetType().Name, "NewThread Created Country : " + Site);
                    fInparallelSoapConnector.Start(SiteObj);
                    fInparallelSoapConnector.Name = "SoapConnector " + Site;
                }

            }
            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name, "Error while Starting Thread SAP Transaction Connector", ex);
            }




            // File_Data_Import ---->  Start Synchronization//

            List<String> SitesFTP = fdb.ReturnDataList("Select Site From SAP_FTP_URL;", "Site");

            try
            {

                LogSystem.LogMessage(this.GetType().Name, "Starting Thread SAP FTP Employee Sync");


                foreach (string Site in SitesFTP)
                {
                    FileDataImport FileImport = new FileDataImport();
                    Thread fInparallelFileImport = new Thread((FileImport.StartThreadService));
                    File_Import_Connector.Add(fInparallelFileImport);

                    Object SiteObj = Site;
                    LogSystem.LogMessage(this.GetType().Name, "NewThread SAP File Import Created Country : " + Site);
                    fInparallelFileImport.Start(SiteObj);

                    fInparallelFileImport.Name = "SAP File Import  " + Site;
                }


            }
            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name, "Error while Starting Thread SAP Transaction Connector", ex);
            }




            //Transaction Collector

            try
            {

                LogSystem.LogMessage(this.GetType().Name, "Starting collecting Transactions to send to SAAP");

                foreach (String site in siteNames)
                {

                    LogSystem.LogMessage(this.GetType().Name, "Starting Rotating Control TransactionCollector on " + site);

                    TransactionCollector collector = new TransactionCollector();
                    Thread fInparallelRotatingControl = new Thread(collector.StartControl);
                    TrCollectorList.Add(fInparallelRotatingControl);

                    LogSystem.LogMessage(this.GetType().Name, "NewThread TransactionCollector started on : " + site);
                    fInparallelRotatingControl.Start(site);

                    LogSystem.LogMessage(this.GetType().Name, "Rotating Control Started");
                }


            }
            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name, "Error while Starting Thread SAP Transaction Connector", ex);
            }






            while (fbExit == false)

            {


                Thread.Sleep(0);

                foreach (Thread Th in SoapConnector)
                {
                    LogSystem.LogMessage(this.GetType().Name, "Thread " + Th.Name + " Is Alive : " + Th.IsAlive.ToString());
                }
                Thread.Sleep(DataExchangeSrv.Properties.Settings.Default.ThreadAliveCheck * 60000);

            }



        }


    }
}
