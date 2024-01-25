using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using DataExchangeAPP.Class;

namespace DataExchangeSrv
{
    static class Program
    {
        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        static void Main()
        {


#if DEBUG

            //SAP TRANSACTION
            DataExchange service = new DataExchange();
            service.OnDebug();

            CreateSAPTransaction saptest = new CreateSAPTransaction();
            saptest.StartThreadService("ITA-ALB");




            //MICRONTEL CONTRACTOR
            // ContractorsAlignment CtrAlgn = new ContractorsAlignment();
            //CtrAlgn.StartThreadService("ITA-ALB");


            //SAP EMPLOYEE
            //FileDataImport ImportData = new FileDataImport();
            //ImportData.ProcessDirectory("\\\\sita1801af\\c$\\inetpub\\wwwroot\\DataExchangeFTP","ITA-ALB");

            //MICRONTEL VISITOR

            // VisitorImport VisImp = new VisitorImport();
            //VisImp.StartThreadService("ITA-ALB");




#else
                        ServiceBase[] ServicesToRun;
                        ServicesToRun = new ServiceBase[]
                        {
                            new DataExchange()
                        };
                        ServiceBase.Run(ServicesToRun);



#endif


        }

    }
}
