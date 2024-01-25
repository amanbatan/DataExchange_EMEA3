using System;
using System.Xml;
using System.Net;
using System.IO;
using System.Data.SqlClient;
using System.Text;
using System.Collections.Generic;
using System.Threading;

namespace DataExchangeAPP.Class
{

     public class CreateSAPTransaction
    {


        Logs LogSystem = new Logs();

        ConfOption fSapServer = new ConfOption();
        ConfOption.SAPParameters fSapCfgData;

        DataBaseConnection fdb = new DataBaseConnection();

        List<Int32> TransactionsList = new List<Int32>();
        string fThreadID;

        Boolean fFirstStep = false;


        DateTime fStartTime = DateTime.Now;
        
        public void StartThreadService(object SiteName)
        {
            //Starting Threading

            string site;
            site = SiteName.ToString();
            fThreadID = site;

            LogSystem.LogMessage(this.GetType().Name,"Starting SAP Connector Country : " + site);


           while (true)

            {
                Thread.Sleep(0);
                DataTrasmission(site);
                Thread.Sleep(DataExchangeSrv.Properties.Settings.Default.RefreshTime);
           
            }



        }



        public void DataTrasmission(string Country)

        {

            
            try

            {

                if (fFirstStep == false)
                {
                    LogSystem.LogMessage(this.GetType().Name,"Starting New Process to SAP : " + Country);

                    fSapCfgData = fSapServer.SAPConnectionData(Country);
                    LogSystem.LogMessage(this.GetType().Name,"---------------------------------------");
                    LogSystem.LogMessage(this.GetType().Name,"Data Used : ");
                    LogSystem.LogMessage(this.GetType().Name,"URL: " + fSapCfgData.SAPurl);
                    LogSystem.LogMessage(this.GetType().Name,"USER: " + fSapCfgData.Username);
                    LogSystem.LogMessage(this.GetType().Name,"THREAD ID: " + fThreadID);
                    LogSystem.LogMessage(this.GetType().Name,"HexDec: " + fSapCfgData.Hex);
                    LogSystem.LogMessage(this.GetType().Name,"Prefix: " + fSapCfgData.Prefix );
                    LogSystem.LogMessage(this.GetType().Name,"Suffix: " + fSapCfgData.Suffix);
                    LogSystem.LogMessage(this.GetType().Name,"TimeZone: " + fSapCfgData.TimeZone);
                    LogSystem.LogMessage(this.GetType().Name,"StartBadgeID: " + fSapCfgData.StartBadgeID );
                    LogSystem.LogMessage(this.GetType().Name,"TotalIDs: " + fSapCfgData.TotIDs );
                    LogSystem.LogMessage(this.GetType().Name,"---------------------------------------");
                    fFirstStep = true;
                }
                // Check new data


                string NewData = ReturnDataToSend(Country);

                if (NewData != "")

                {
                    fStartTime = DateTime.Now;
                    string ResultSapTransaction;
                    ResultSapTransaction = SendTransactions(NewData);


                }

            }

            catch (Exception ex)
            {

                LogSystem.LogMessage(this.GetType().Name,"DataTrasmissionInitialization : " + Country + " - ", ex);

            }


        }




        string SendTransactions(string BodySapTransaction)

        {
            var _url = fSapCfgData.SAPurl;
            var _action = "\"http://sap.com/xi/WebService/soap1.1\"";

            XmlDocument soapEnvelopeXml = CreateSoapEnvelope(BodySapTransaction);
            HttpWebRequest webRequest = CreateWebRequest(_url, _action);
            
            webRequest.ContentType = "application/xml";
            webRequest.Accept = "application/xml";

            string username = fSapCfgData.Username;
            string password = fSapCfgData.Password;


            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));

            webRequest.Headers.Add("Authorization", "Basic " + svcCredentials);

            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);


            // begin async call to web request.
            IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);

            // suspend this thread until call is complete. You might want to

            asyncResult.AsyncWaitHandle.WaitOne();

            // get the response from the completed web request.
            string soapResult;
            using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
            {
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    soapResult = rd.ReadToEnd();
                }
                Console.Write(soapResult + "\r\n");

                if (soapResult == "<SOAP:Envelope xmlns:SOAP='http://schemas.xmlsoap.org/soap/envelope/'><SOAP:Header/><SOAP:Body/></SOAP:Envelope>")
                {

                    LogSystem.LogMessage(this.GetType().Name,"Data Trasmission completed, closing transaction on DB" + "(" + fThreadID + ")");

                    CloseTransaction();

                    TimeSpan TotalTime;
                    TotalTime= DateTime.Now - fStartTime;


                    LogSystem.LogMessage(this.GetType().Name,"Total Time To complete operation " + (TotalTime.TotalMilliseconds.ToString())   + " ms") ;

                }
               
                else
                {

                    LogSystem.LogMessage(this.GetType().Name,"Data Trasmission not completed properly " + soapResult + "(" + fThreadID + ")");
                }

                return soapResult;

            }
        }

        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Accept = "gzip,deflate";
            webRequest.ContentType = "text/xml;charset=UTF-8";
            webRequest.Headers.Add("SOAPAction", action);


            webRequest.Method = "POST";
            return webRequest;
        }

        private XmlDocument CreateSoapEnvelope(string Body)
        {
            XmlDocument soapEnvelopeDocument = new XmlDocument();

            string Header;
            String Footer;


            Header = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:p1=""" + fSapCfgData.XmlnsSAP + @""">" + "\r\n";
            Header += "<soapenv:Header/>" + "\r\n";
            Header += "<soapenv:Body>" + "\r\n";
            Header += "<p1:" + fSapCfgData.SAPTag + ">" + "\r\n";


            Footer = @" </p1:" + fSapCfgData.SAPTag + ">" + "\r\n";
            Footer += "</soapenv:Body>" + "\r\n";
            Footer += " </soapenv:Envelope>" + "\r\n";




            System.Console.WriteLine(Header + "\r\n" + Body + "\r\n" + Footer);
            LogSystem.LogMessage(this.GetType().Name,Header + "\r\n" + Body + "\r\n" + Footer);


            soapEnvelopeDocument.LoadXml(Header + "\r\n" + Body + "\r\n" + Footer);


            return soapEnvelopeDocument;

        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }

        private string ReturnDataToSend(string country)

        {

            string BodyString;
            String QueryToRun;
            SqlConnection cnn;

            string MaxTransaction = (DataExchangeSrv.Properties.Settings.Default.MaxTransactionPerTime).ToString();
            // check if new data is available with a limit transaction per time

            BodyString = "";

            QueryToRun = "Select top " + MaxTransaction + " * from SAP_Transactions where exported is null";

            if (fdb.ExecQuery(QueryToRun.ToUpper()) > 0)

            {

                // create the Transaction string 

                cnn = fdb.OpenConnection();


                using (cnn)


                {
                    int TotalTransaction = 0;
                    SqlCommand command = new SqlCommand(QueryToRun, cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        String TransId = reader["SAP_Transaction"].ToString();

                        string badgeID = "";
                        badgeID = reader["TIMEID_NO"].ToString();

                        int checkTransactionExist = fdb.ExecQuery("SELECT * from SAP_Transactions where cid = '" +
                                                    reader["CID"] + "' and TEVENTYPE = '" +
                                                    reader["TEVENTYPE"] + "' and PHYSDATE_TIME = '" +
                                                    reader["PHYSDATE_TIME"] + "' and TIMEID_NO = '" +
                                                    reader["TIMEID_NO"] + "' and ATT_ABS_REASON = '" + 
                                                    reader["ATT_ABS_REASON"] + "' and EXPORTED is not null;");

                         

                        if (checkTransactionExist > 0)
                        {
                            fdb.ExecQuery("Update SAP_Transactions set Exported = GetDate(), SyncError = 'Already Exported' where SAP_Transaction = " + TransId + ";");

                            LogSystem.LogMessage(this.GetType().Name, "\r\n\r\n Transaction already Exported ID: " + reader["SAP_Transaction"] + "\r\n" +
                                                                                                       " TimeID: " + reader["TIMEID_NO"] + "\r\n" +
                                                                                                       " CID: " + reader["CID"] + "\r\n" +
                                                                                                       " PHYSDATE_TIME: " + reader["PHYSDATE_TIME"] + "\r\n\r\n");

                        }
                        //else if (badgeID.Length > fSapCfgData.TotIDs)
                        //{

                        //}

                        else if (badgeID != "" && reader["CID"].ToString() != "")
                        {
                              

                        //SapCountryDetails.SAPurl = reader["SapURL"].ToString();

                        BodyString += "<E1BPCC1UPTEVEN xmlns=''>" + "\r\n";

                        BodyString += "<TERMINALID>" + (reader["TerminalID"].ToString()).Replace(" ", "") + "</TERMINALID>" + "\r\n";
                        BodyString += "<TIMEID_NO>" + ReturnBadgeID(badgeID, TransId) + "</TIMEID_NO>" + "\r\n";
                        BodyString += "<CID>" + reader["CID"].ToString() + "</CID>" + "\r\n";
                        BodyString += "<TEVENTTYPE>" + reader["TEvenType"].ToString() + "</TEVENTTYPE>" + "\r\n";
                        BodyString += "<ATT_ABS_REASON>" + reader["ATT_ABS_REASON"].ToString() + "</ATT_ABS_REASON>" + "\r\n";
                        BodyString += "<PHYSDATE_TIME>" + ReturnSapDateTime(reader["PHYSDATE_TIME"].ToString()) + "</PHYSDATE_TIME>" + "\r\n";


                        BodyString += "</E1BPCC1UPTEVEN>" + "\r\n";



                        TotalTransaction = +1;
                        TransactionsList.Add(Convert.ToInt32(reader["SAP_Transaction"]));

                        }

                        else
                        {
                            fdb.ExecQuery("Update SAP_Transactions set Exported = GetDate(), SyncError = 'Cid or Badge not Valid' where SAP_Transaction = " + TransId + ";" );
                        }
                        Thread.Sleep(300);
                    }

                    reader.Close();
                    cnn.Close();
                    LogSystem.LogMessage(this.GetType().Name,"Total transaction Collected: " + TotalTransaction + "(" + fThreadID + ")");

                    return BodyString;


                }


            }
            else
            {
                return BodyString;
            }
        }


        private void CloseTransaction()

        {
            int TotalTransaction = TransactionsList.Count;
            int TotalTransactionClosed = 0;
            string QueryString;
            //prepare String 
            try
            {
                string wherestring = "";
               

                TransactionsList.ForEach(x => wherestring += " or SAP_Transaction = " + x);

                QueryString = "Update SAP_Transactions set Exported = GetDate() where SAP_Transaction = 0 " + wherestring;

                LogSystem.LogMessage(this.GetType().Name,"Total Transaction to close : " + TotalTransaction.ToString() + "(" + fThreadID + ")");

                TotalTransactionClosed = fdb.ExecQuery(QueryString);

                LogSystem.LogMessage(this.GetType().Name,"Total Transaction closed : " + TotalTransactionClosed.ToString() + "(" + fThreadID + ")");

                TransactionsList.Clear();



            }
            catch (Exception ex)
            {

                LogSystem.LogMessage(this.GetType().Name,"Error during transaction closing operation : " + "(" + fThreadID + ")", ex);
                TotalTransaction = 0;
            }


        }


      

        string ReturnBadgeID (string badgeId, string TransId)

        {

            string SAPbadgeID ="";

            if (fSapCfgData.Hex.ToString() == "1")
            {

                long number;
                number = long.Parse(badgeId);
                SAPbadgeID = number.ToString("x");
            }
            else
                SAPbadgeID = badgeId;


            try
            {
                SAPbadgeID = SAPbadgeID.Substring(fSapCfgData.StartBadgeID, fSapCfgData.TotIDs);
            }
            catch
            {
                SAPbadgeID = badgeId;

                fdb.ExecQuery("Update SAP_Transactions set Exported = GetDate(), SyncError = 'BadgeID too short' where SAP_Transaction = " + TransId + ";");

            }
                
           

            SAPbadgeID = (fSapCfgData.Prefix + SAPbadgeID + fSapCfgData.Suffix);

            return SAPbadgeID;

        }

        private string ReturnSapDateTime(string Rcv)
        {

           
            DateTime ValueDT = DateTime.ParseExact((Rcv.Substring(0, 4) + "-" + Rcv.Substring(4, 2) + "-" + Rcv.Substring(6, 2) + " " + Rcv.Substring(8, 2) + ":" + Rcv.Substring(10, 2) + ":00"), "yyyy-MM-dd HH:mm:ss",null);

            // Change to timezone

            //ValueDT = ValueDT.AddHours(fSapCfgData.TimeZone);
            TimeZoneInfo cstTime = TimeZoneInfo.FindSystemTimeZoneById(fSapCfgData.TimeZone); 
            ValueDT = TimeZoneInfo.ConvertTimeFromUtc(ValueDT, cstTime);


            return (ValueDT.Year).ToString().PadLeft(2, '0') + (ValueDT.Month).ToString().PadLeft(2, '0') + ValueDT.Day.ToString().PadLeft(2, '0') + ValueDT.Hour.ToString().PadLeft(2, '0') + ValueDT.Minute.ToString().PadLeft(2, '0') + ValueDT.Second.ToString().PadLeft(2, '0');

            //return  (ValueDT.Day).ToString().PadLeft (2,'0')   + (ValueDT.Month).ToString().PadLeft(2, '0') + ValueDT.Year.ToString().Substring(2,2)   + ValueDT.Hour.ToString().PadLeft(2, '0') + ValueDT.Minute.ToString().PadLeft(2, '0');


        }

      




}


   
}
