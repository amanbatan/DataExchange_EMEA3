using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using DataExchangeAPP.Class;
namespace DataExchangeAPP.Class
{
    public struct EventDetails
    {
        public int SerialNum;
        public string EvDscr;
        public long CardNum;
        public long EmpID;
        public string LastName;
        public string FirstName;
        public string Ssno;
        public string Event_time_utc;
        public string ReaderDesc;
        public string IntegrationDetails;
        public string CID;
        public string Reason;
    }
    class TransactionCollector
    {
        DataBaseConnection fdb = new DataBaseConnection();
        ConfOption confOption = new ConfOption();
        Boolean fBexit = true;

     
        Logs LogSystem = new Logs();

        String site;
        String inString; 
        String outString;
        

        public void StartControl(object country)
        {
            while (fBexit)
            {

                inString = confOption.getINString(country.ToString());
                outString = confOption.getOutString(country.ToString());
                site = country.ToString(); 

                Thread.Sleep(0);
                try
                {
                     StartRotatingControl();
                }
                catch (Exception ex)
                {
                    LogSystem.LogMessage(this.GetType().Name,"Error in starting rotating control ", ex);
                }
                Thread.Sleep(DataExchangeSrv.Properties.Settings.Default.EventCollectorFrequency);
            }
        }






        public void StartRotatingControl()
        {
            List<EventDetails> NewData = new List<EventDetails>();
            try
            {
                string QueryTransaction;
                SqlConnection cnn;

                QueryTransaction = "SELECT * FROM";
                QueryTransaction += "(SELECT   EVENTS.SERIALNUM, Event.EVDESCR, EVENTS.CARDNUM AS CARDNUM, EVENTS.EMPID, EMP.LASTNAME, EMP.FIRSTNAME, EMP.SSNO, REASON = ";
                QueryTransaction += "		 (CASE ";
                QueryTransaction += "		 WHEN (EVENTS.CARDNUM NOT BETWEEN 1 AND 9) AND  ( LAG(EVENTS.CARDNUM, 1) OVER(ORDER BY READER.READERDESC, EVENTS.event_time_utc, EVENTS.SERIALNUM ) BETWEEN 1 AND 9) THEN";
                QueryTransaction += "		 LAG(EVENTS.CARDNUM, 1) OVER(ORDER BY  READER.READERDESC, EVENTS.SERIALNUM) ";
                QueryTransaction += "		 ELSE";
                QueryTransaction += "		 NULL";
                QueryTransaction += "		 END),";
                QueryTransaction += "		EVENTS.EVENT_TIME_UTC, READER.READERDESC, READER.NOTES,UDFEMP.EXT ";
                QueryTransaction += "            From UDFEMP INNER Join";
                QueryTransaction += "            EMP On UDFEMP.ID = EMP.ID RIGHT OUTER Join";
                QueryTransaction += "			EVENT INNER Join";
                QueryTransaction += "            EVENTS On EVENT.EVTYPEID = EVENTS.EVENTTYPE And EVENT.EVID = EVENTS.EVENTID INNER Join ";
                QueryTransaction += "            ACCESSPANE On EVENTS.MACHINE = ACCESSPANE.PANELID LEFT OUTER Join ";
                QueryTransaction += "			READER On ACCESSPANE.PANELID = READER.PANELID And EVENTS.DEVID = READER.READERID ON EMP.ID = EVENTS.EMPID";
                QueryTransaction += "                WHERE(EVENTS.event_time_utc > '" + (ObtainLastTransaction()).ToString("yyyy-MM-dd HH:mm:ss.fff") + "') And (EVENTS.EMPID <> 0)) as tempTable ";
                QueryTransaction += "WHERE (CARDNUM NOT BETWEEN 0 AND 9) AND SERIALNUM <> '" + fdb.ReturnDataStr("select serialNum from GlobalParameters where properties = 'LastEventTransaction'", "serialNum") + "' " ;
                QueryTransaction += "ORDER BY EVENT_TIME_UTC ASC";





                cnn = fdb.OpenConnectionOGDB();
                DateTime LastT = DateTime.UtcNow;
                string LastSerialNum = "";
                using (cnn)
                {
                    SqlCommand Command = new SqlCommand(QueryTransaction, cnn);
                    SqlDataReader Reader = Command.ExecuteReader();
                    while (Reader.Read())
                    {
                        EventDetails NewItem = new EventDetails
                        {
                            SerialNum = fdb.isNullFieldNum(Reader, "serialnum"),
                            EvDscr = fdb.isNullFieldStr(Reader, "evdescr"),
                            CardNum = long.Parse(fdb.isNullFieldStr(Reader, "cardnum")),
                            EmpID = long.Parse(fdb.isNullFieldStr(Reader, "empid")),
                            LastName = fdb.isNullFieldStr(Reader, "lastname"),
                            FirstName = fdb.isNullFieldStr(Reader, "firstname"),
                            Event_time_utc = fdb.isNullFieldStr(Reader, "event_time_utc"),
                            ReaderDesc = fdb.isNullFieldStr(Reader, "readerdesc"),
                            Ssno = fdb.isNullFieldStr(Reader, "ssno"),
                            IntegrationDetails = fdb.isNullFieldStr(Reader, "notes"),
                            CID = fdb.isNullFieldStr(Reader, "ext"),
                            Reason = ReturnReason(fdb.isNullFieldStr(Reader, "reason"))
                        };

                       
                        NewData.Add(NewItem);


                        LastT = DateTime.Parse(Reader["event_time_utc"].ToString());
                        LastSerialNum = NewItem.SerialNum.ToString();
                    }
                    Reader.Close();
                }
                if (NewData.Count > 0)
                {
                    LogSystem.LogMessage(this.GetType().Name,"Total transaction to process " + NewData.Count);
                    if (DataDispatch(NewData) == true)
                    {
                        fdb.ExecQuery(("update GlobalParameters set value='" + LastT.ToString("yyyy-MM-dd HH:mm:ss.fff") + "' , " +
                                        "serialNum = '" + LastSerialNum + "' " +
                                        "where properties ='LastEventTransaction'"));
                    }
                    NewData = null;
                }
            }
            catch (Exception ex)
            {
                LogSystem.LogMessage(this.GetType().Name,"Error in rotating control ", ex);
            }
            Thread.Sleep(DataExchangeSrv.Properties.Settings.Default.EventCollectorFrequency);
        }








        private DateTime ObtainLastTransaction()
        {
            DateTime LastT;
            LastT = DateTime.UtcNow.AddDays(1);
            try
            {
                  
                LastT = DateTime.Parse(fdb.ReturnDataStr("select value from GlobalParameters where properties ='LastEventTransaction'", "value"));

                ////check lastT if not over days indicated in the configuration
                //double DaysCalculated;
                //DaysCalculated = (DateTime.UtcNow - LastT).TotalDays;
                //if (DaysCalculated > DataExchangeSrv.Properties.Settings.Default.MaxDays)
                //{
                    //LastT = DateTime.UtcNow.AddDays(1);
                //}
            }
            catch (Exception ex)
            {
                LogSystem.LogMessage(this.GetType().Name,"Error during obtain last Transaction ", ex);
            }
            return LastT;
        }







        private Boolean DataDispatch(List<EventDetails> NewItem)
        {
            Boolean Response = false;
            try
            {
                foreach (EventDetails Event in NewItem)
                {
                    // Import Access Granted Swipes and only from readers with NOTES

                    //&& Event.IntegrationDetails.Contains("SAPID") && Event.ReaderDesc.Contains(site)

                    if (Event.EvDscr.Contains("Access Granted") && Event.IntegrationDetails.Contains("SAPID") 
                        && Event.ReaderDesc.Contains(site) )
                    {
                        string queryInsert;
                        String Reason = "";

                        DateTime dateTime = DateTime.ParseExact(Event.Event_time_utc, "dd/MM/yyyy HH:mm:ss", null);
                        string checkDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");





                        // Check conditions before sending Reason, If reason is not found if condition is skipped
                        if (Event.Reason != "0000")
                        {
                            LogSystem.LogMessage(this.GetType().Name, "Entered in Reason Loop ");
                            string sqlQuery = " SELECT EVENT_TIME_UTC ";
                            sqlQuery += " FROM EVENTS ";
                            sqlQuery += " INNER JOIN READER ON EVENTS.DEVID = READER.READERID ";
                            sqlQuery += " INNER JOIN EVENT ON EVENT.EVTYPEID = EVENTS.EVENTTYPE AND EVENT.EVID = EVENTS.EVENTID ";
                            sqlQuery += " WHERE EVENT_TIME_UTC = '" + checkDateTime + "' ";
                            sqlQuery += "       AND CARDNUM BETWEEN 1 AND 9 ";
                            sqlQuery += "       AND EVENT.EVDESCR LIKE 'Access%' ";
                            sqlQuery += "       AND READER.NOTES LIKE '" + Event.IntegrationDetails + "' ";
                            sqlQuery += " ORDER BY EVENT_TIME_UTC DESC; ";

                            int ReasonExist = fdb.ExecQueryDBOnguard(sqlQuery);

                            if (Event.IntegrationDetails.Contains("IN") || ReasonExist == 0)
                            {
                                LogSystem.LogMessage(this.GetType().Name, "Reason Removed ");
                                Reason = "";
                            }
                            else
                            {
                                Reason = Event.Reason;
                            }

                        }
                        else
                        {
                            Reason = Event.Reason;
                        }


                        if (Event.Ssno.ToString().Length > 8)
                            LogSystem.LogMessage(this.GetType().Name, "Event.SSNO (" + Event.Ssno + ") is too long (Max.size = 8)" );
                        else 
                        {   
                            queryInsert = "Insert into SAP_Transactions (Terminalid,TimeID_NO,CID,TEVENTYPE,ATT_ABS_REASON,PHYSDATE_TIME,SITE, SerialNum) values (";
                            queryInsert += "'" + (Event.IntegrationDetails).Substring(6, 4) + "',";
                            queryInsert += "'" + Event.CardNum + "',";
                            queryInsert += "'" + ((Event.Ssno).PadLeft(8, '0')) + "',";
                            queryInsert += "'" + ReturnTransactionType((Event.IntegrationDetails).Substring(11, 1)) + "',";
                            queryInsert += "'" + Event.Reason + "',";
                            queryInsert += "'" + (ReturnDateTimeInUTC(DateTime.Parse((Event.Event_time_utc)))).Substring(0, 14) + "',";
                            queryInsert += "'" + (Event.ReaderDesc).Substring(0, 7) + "',";
                            queryInsert += "'" + (Event.SerialNum).ToString() + "'";
                            queryInsert += ")";

                            fdb.ExecQuery(queryInsert);
                        }
                        //Query Originale  --> In funzione ALBA
                        //queryInsert = "Insert into SAP_Transactions (Terminalid,TimeID_NO,CID,TEVENTYPE,ATT_ABS_REASON,PHYSDATE_TIME,SITE, SerialNum) values (";
                        //queryInsert += "'" + (Event.IntegrationDetails).Substring(6, 4) + "',";
                        //queryInsert += "'" + Event.CardNum + "',";
                        //queryInsert += "'" + ((Event.Ssno).PadLeft(8, '0')) + "',";          
                        //queryInsert += "'" + ReturnTransactionType((Event.IntegrationDetails).Substring(11, 1)) + "',";
                        //queryInsert += "'" + Event.Reason + "',";
                        //queryInsert += "'" + (ReturnDateTimeFromUTC(DateTime.Parse((Event.Event_time_utc)))).Substring(0, 14) + "',";
                        //queryInsert += "'" + (Event.ReaderDesc).Substring(0, 7) + "',";
                        //queryInsert += "'" + (Event.SerialNum).ToString() + "'";
                        //queryInsert += ")";
                         
                    }
                    Response = true;
                }
            }
            catch (Exception ex)
            {
                LogSystem.LogMessage(this.GetType().Name,"Check Event Parameters: ", ex);
                Response = false;
            }
            return Response;
        }
        string ReturnDateTimeInUTC(DateTime EventData)
        {
            try
            {
                string day, month, year, hours, minutes, seconds;
                day = (EventData.Day).ToString().PadLeft(2, '0');
                month = (EventData.Month).ToString().PadLeft(2, '0');
                year = (EventData.Year).ToString();
                hours = (EventData.Hour).ToString().PadLeft(2, '0');
                minutes = (EventData.Minute).ToString().PadLeft(2, '0');
                seconds = (EventData.Second).ToString().PadLeft(2, '0');
                return (year + month + day + hours + minutes + seconds);
            }
            catch (Exception ex)
            {
                LogSystem.LogMessage(this.GetType().Name,"Error Date trasformation ", ex);
                return ("000000000000");
            }
        }
       private string ReturnTransactionType(string InOut) {

            if (InOut == "I")
            {
                return inString;
            }
            else
            {
                return outString;
            }


            }
        private string ReturnReason(string reason)
        {
            if(reason.Length>2)
            {
                return  "0";
            }
            else
            {
                reason = reason.PadLeft(4, '0');
                return reason;
            }
            
        }
       



    }
}
