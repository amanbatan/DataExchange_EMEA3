using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace DataExchangeAPP.Class
{
    class ConfOption
    {
        Logs LogSystem = new Logs();
        DataBaseConnection fdb = new DataBaseConnection();
       

        public struct SAPParameters
        {
            public string SAPurl;
            public string Username;
            public string Password;
            public string XmlnsSAP;
            public string SAPTag;
            public string Hex;
            public string Prefix;
            public string Suffix;
            public string TimeZone;
            public int StartBadgeID;
            public int TotIDs;
        }

        public struct OTHParamters
        {
            public string Site;
            public string ConnectionString;
            public string TableName;
            public int Refresh_Sync;
            public int Refresh_Event;
        }

        public SAPParameters SAPConnectionData (string country)
        {

            SAPParameters SapCountryDetails = new SAPParameters();
            SqlConnection cnn;

            try
            {




                cnn = fdb.OpenConnection();


                using (cnn)


                {

                    SqlCommand command = new SqlCommand("Select * from SAP_WebServer where country ='" + country + "';",cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {

                        SapCountryDetails.SAPurl = reader["SapURL"].ToString();
                        SapCountryDetails.Username = reader["Username"].ToString();
                        SapCountryDetails.Password = reader["Password"].ToString();
                        SapCountryDetails.XmlnsSAP = reader["XmlnsSAP"].ToString();
                        SapCountryDetails.SAPTag = reader["SAPTag"].ToString();
                        SapCountryDetails.Hex = reader["HexDec"].ToString();
                        SapCountryDetails.Prefix  = reader["Prefix"].ToString();
                        SapCountryDetails.Suffix  = reader["Suffix"].ToString();
                        SapCountryDetails.TimeZone= reader["TimeZone"].ToString();
                        SapCountryDetails.StartBadgeID  = int.Parse((reader["StartBadgeId"].ToString()));
                        SapCountryDetails.TotIDs  = int.Parse((reader["TotIDs"].ToString()));

                        LogSystem.LogMessage(this.GetType().Name,"Country Web Server Data Collected: " + country);

                    }

                    reader.Close();
                    cnn.Close();

                }
            }





            catch (Exception ex)
            {



                LogSystem.LogMessage(this.GetType().Name,"Error during configuraton reading ", ex);

            }


           
            return SapCountryDetails;

        }

        public OTHParamters  OTHConnectionData(string site, string SourceType)
        {

            OTHParamters OTHCountryDetails = new OTHParamters();
            SqlConnection cnn;

            try
            {




                cnn = fdb.OpenConnection();


                using (cnn)


                {

                    SqlCommand command = new SqlCommand("Select * from OTH_Server where site ='" + site + "' and SourceType ='" + SourceType +"';", cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {

                       

                        OTHCountryDetails.Site= reader["Site"].ToString();
                        OTHCountryDetails.ConnectionString  = reader["ConnectionString"].ToString();
                        OTHCountryDetails.TableName = reader["TableName"].ToString();
                        OTHCountryDetails.Refresh_Event = int.Parse(reader["Refresh_Event"].ToString());
                        OTHCountryDetails.Refresh_Sync = int.Parse(reader["Refresh_Sync"].ToString());

                        LogSystem.LogMessage(this.GetType().Name,"Site Server Data Collected: " + site);

                    }

                    reader.Close();
                    cnn.Close();

                }
            }





            catch (Exception ex)
            {



                LogSystem.LogMessage(this.GetType().Name, "Error during configuraton reading data OTH Config ", ex);

            }



            return OTHCountryDetails;

        }



        public String getINString(String country)
        {
            return fdb.ReturnDataStr("SELECT [ENTRY] from [SAP_WebServer] where COUNTRY like '" + country + "%' ;", "Entry");
        }


        public String getOutString(String country)
        {
            return fdb.ReturnDataStr("SELECT [EXIT] from [SAP_WebServer] where COUNTRY like '" + country + "%' ;", "Exit");
        }





        public Boolean CIDExist(string Cid)
        {
            Boolean result = false;

            try
            {

                string SqlWhere = Cid;
                string sql;
                sql = ("SELECT * FROM EMP WHERE SSNO = '" + Cid + "'; ");


                if (fdb.ExecQueryDBOnguard(sql) > 0)
                    result = true;

                else if (fdb.ExecQueryDBOnguard(sql) == 0)
                    result = false;

            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name, "CIDExist : ", ex);
                result = false;
            }

            return result;
        }




    }
}
