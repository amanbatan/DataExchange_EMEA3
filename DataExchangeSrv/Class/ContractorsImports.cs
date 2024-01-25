using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading;



namespace DataExchangeAPP.Class
{

 

    class ContractorsImports
    {
        Logs LogSystem = new Logs();
        DataBaseConnection fdb = new DataBaseConnection();
        ConfOption fOTHserver = new ConfOption();
        ConfOption.OTHParamters fOTHParameters;
       

        public void StartControl(string sitecode, Boolean fFirst)
        {
            fOTHParameters = fOTHserver.OTHConnectionData(sitecode,"E");

            if (fFirst == true)
            {
                
                LogSystem.LogMessage(this.GetType().Name, "----CONTRACTOR IMPORT-----------------------------------");
                LogSystem.LogMessage(this.GetType().Name, "OTH SERVER  : " + sitecode);
                LogSystem.LogMessage(this.GetType().Name, "CONNECTION STRING : " + fOTHParameters.ConnectionString);
                LogSystem.LogMessage(this.GetType().Name, "TABLE NAME : " + fOTHParameters.TableName);
                LogSystem.LogMessage(this.GetType().Name, "REFRESH SYNC (ms) : " + fOTHParameters.Refresh_Sync);
                LogSystem.LogMessage(this.GetType().Name, "REFRESH EVENT (s) : " + fOTHParameters.Refresh_Event);
                LogSystem.LogMessage(this.GetType().Name, "---------------------------------------------------------");
            }



            //START Check with existing table
            try
            {

                List<String> SRC_DB = fdb.ReturnDataListOthDB("select [CardHolderId] from [LNL_CONTRACTORS]", "CardHolderId", fOTHParameters.ConnectionString);
                List<String> DST_DB = fdb.ReturnDataListOthDB("select [CardHolderId] from [OTH_CONTRACTORS]", "CardHolderId", DataExchangeSrv.Properties.Settings.Default.DBIntegrConnection);

                // Find New Record
                if ((SRC_DB.Count > 0) && (DST_DB.Count > 0))
                {
                    var NewRecords = SRC_DB.Where(i => !DST_DB.Contains(i)).ToList();
                    LogSystem.LogMessage(this.GetType().Name, "Total New Records Found:" + NewRecords.Count);

                    // copy new records

                    foreach (string CHID in NewRecords)
                    {

                        SqlConnection cnn;
                        cnn = fdb.OpenConnectionDB3rdP(fOTHParameters.ConnectionString);

                        using (cnn)
                        {

                            // List Scrooling




                            SqlCommand Command = new SqlCommand("select * from LNL_CONTRACTORS where CardHolderId ='" + CHID + "';", cnn);
                            SqlDataReader Reader = Command.ExecuteReader();
                            string QueryInsert = "";
                            string CardHolderID = "";
                            while (Reader.Read())
                            {


                                QueryInsert = "INSERT into OTH_Contractors values ( ";
                                QueryInsert += "'" + Reader["CompanyCode"].ToString() + "',";
                                QueryInsert += "'" + fdb.TruncateAndFix(Reader["CompanyName"].ToString(), 50) + "',";
                                QueryInsert += "'" + Reader["CardHolderId"].ToString() + "',";
                                QueryInsert += "'" + fdb.TruncateAndFix(Reader["LastName"].ToString(), 40) + "',";
                                QueryInsert += "'" + fdb.TruncateAndFix(Reader["FirstName"].ToString(), 40) + "',";
                                QueryInsert += "'" + Reader["BadgeCode"].ToString() + "',";
                                QueryInsert += "'',";
                                QueryInsert += Reader["UserActive"].ToString() + ",";
                                QueryInsert += "'" + ReturnSQLDateTime(Convert.ToDateTime(Reader["ActivationDate"]).ToString("yyyy-MM-dd HH:mm:ss.fff")) + "',";
                                QueryInsert += "'" + ReturnSQLDateTime(Convert.ToDateTime(Reader["DeActivationDate"]).ToString("yyyy-MM-dd HH:mm:ss.fff")) + "',";
                                QueryInsert += "'" + sitecode + "',";
                                QueryInsert += 1 + ",";
                                QueryInsert += "null, null, null );";

                                CardHolderID = Reader["CardHolderId"].ToString();
                            }

                            if (fdb.ExecQuery(QueryInsert) >= 1)
                            {
                                // New Data imported
                                LogSystem.LogMessage(this.GetType().Name, " New Record Add: " + CardHolderID);
                            }

                            else

                            {
                                LogSystem.LogMessage(this.GetType().Name, " ERROR NEW  Record NOT Add: " + CardHolderID + " " + QueryInsert);

                            }
                        }

                    }


                    // Find Record To Delete
                    var OldRecords = DST_DB.Where(i => !SRC_DB.Contains(i)).ToList();
                    LogSystem.LogMessage(this.GetType().Name, "Total Old Record To Delete:" + OldRecords.Count);

                    foreach (string CHID in OldRecords)
                    {

                        //Update table to Master Server

                        if (fdb.ExecQuery("Update OTH_Contractors set RecordStatus = 3 where CardHolderId='" + CHID + "';") == 1) ;
                        LogSystem.LogMessage(this.GetType().Name, "Record To Delete: " + CHID);

                        // ONLY FOR TEST PHASE //

                        fdb.ExecQuery("delete from OTH_Contractors   where CardHolderId='" + CHID + "' and RecordStatus = 3");
                        LogSystem.LogMessage(this.GetType().Name, CHID + " DELETED");
                    }

                    //Clean list

                    SRC_DB.Clear();
                    DST_DB.Clear();


                    //Find Update Operation
                    List<String> UPD_DB = fdb.ReturnDataListOthDB("select [CardHolderId] from [LNL_CONTRACTORS] where modifydate>= '" + (ObtainLastTransaction()).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'", "CardHolderId", fOTHParameters.ConnectionString);



                    LogSystem.LogMessage(this.GetType().Name, "Total Record To Update:" + UPD_DB.Count);
                    foreach (string UPID in UPD_DB)
                    {

                        //Update table to OnGuardIntegration

                        //if (fdb.ExecQuery("Update OTH_Contractors set RecordStatus = 2 where CardHolderId='" + UPID + "';") == 1)
                        //    LogSystem.LogMessage(this.GetType().Name, "Record To Update: " + UPID);


                        SqlConnection cnn;
                        cnn = fdb.OpenConnectionDB3rdP(fOTHParameters.ConnectionString);

                        using (cnn)
                        {

                            SqlCommand Command = new SqlCommand("select * from LNL_CONTRACTORS where CardHolderId ='" + UPID + "';", cnn);
                            SqlDataReader Reader = Command.ExecuteReader();
                            string QueryUpdate = "";
                            string CardHolderID = "";
                            while (Reader.Read())
                            {

                                QueryUpdate = "UPDATE OTH_Contractors SET ";
                                QueryUpdate += "CompanyCode = '" + Reader["CompanyCode"].ToString() + "', ";
                                QueryUpdate += "CompanyName = '" + fdb.TruncateAndFix(Reader["CompanyName"].ToString(), 50) + "', "; 
                                QueryUpdate += "LastName = '" + fdb.TruncateAndFix(Reader["LastName"].ToString(), 40) + "', ";
                                QueryUpdate += "FirstName = '" + fdb.TruncateAndFix(Reader["FirstName"].ToString(), 40) + "', ";
                                QueryUpdate += "BadgeCode = '" + Reader["BadgeCode"].ToString() + "', ";
                                QueryUpdate += "UserActive = " + Reader["UserActive"].ToString() + ", ";
                                QueryUpdate += "ActivationDate = '" + ReturnSQLDateTime(Convert.ToDateTime(Reader["ActivationDate"]).ToString("yyyy-MM-dd HH:mm:ss.fff")) + "', ";
                                QueryUpdate += "DeActivationDate = '" + ReturnSQLDateTime(Convert.ToDateTime(Reader["DeActivationDate"]).ToString("yyyy-MM-dd HH:mm:ss.fff")) + "', ";
                                QueryUpdate += "Site = '" + sitecode + "', ";
                                QueryUpdate += "RecordStatus = 2, "; 
                                QueryUpdate += "CollectedTime = null, ";
                                QueryUpdate += "SyncErr = null ";
                                QueryUpdate += "WHERE CardHolderId = '" + UPID + "'; " ;  


                                CardHolderID = Reader["CardHolderId"].ToString();
                            }

                            if (fdb.ExecQuery(QueryUpdate) >= 1)
                            {
                                // Data Updated
                                LogSystem.LogMessage(this.GetType().Name, " Record Updated: " + CardHolderID);
                            }
                            else

                            {
                                LogSystem.LogMessage(this.GetType().Name, " ERROR  Record NOT Updated: " + CardHolderID + " " + QueryUpdate);

                            }
                        }




                    }

                    //Save Last Update
                    fdb.ExecQuery(("update GlobalParameters set value= getdate() where properties ='LastCardsOtherContractor'"));

                }

            }
            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"Populating List Error", ex);

            }



        }

        private string ReturnSQLDateTime (string DateString)

        {


            //string stringSqlDate = DateString.Substring(6,4) +"-" + DateString.Substring(3, 2) + "-" + DateString.Substring(0, 2)+ "T" + DateString.Substring(11, 8) +".000" ;

            string stringSqlDate = DateString.Substring(0, 4) + "-" + DateString.Substring(5, 2) + "-" + DateString.Substring(8, 2) + "T" + DateString.Substring(11, 8) + ".000";
            return stringSqlDate;
        }

        private DateTime ObtainLastTransaction()

        {
            DateTime LastT;
            LastT = DateTime.UtcNow.AddDays(1);


            try
            {
                LastT = DateTime.Parse(fdb.ReturnDataStr("select value from GlobalParameters where properties ='LastCardsOtherContractor'", "value"));
            }
            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"Error during obtain last Transaction ", ex);
            }

            return LastT;

        }

      
    }
}
