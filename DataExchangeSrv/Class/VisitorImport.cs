using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data.SqlClient;


namespace DataExchangeAPP.Class
{
    class VisitorImport
    {
        Logs LogSystem = new Logs();
        DataBaseConnection fdb = new DataBaseConnection();
        ConfOption fOTHserver = new ConfOption();
        ConfOption.OTHParamters fOTHParameters;
        Boolean  fBexit = true;


        public void StartThreadService(object siteobj)

        {
            fOTHParameters = fOTHserver.OTHConnectionData(siteobj.ToString(), "V");

            

                LogSystem.LogMessage(this.GetType().Name, "----VISITOR IMPORT -----------------------------------");
                LogSystem.LogMessage(this.GetType().Name, "OTH SERVER  : " + siteobj.ToString());
                LogSystem.LogMessage(this.GetType().Name, "CONNECTION STRING : " + fOTHParameters.ConnectionString);
                LogSystem.LogMessage(this.GetType().Name, "TABLE NAME : " + fOTHParameters.TableName);
                LogSystem.LogMessage(this.GetType().Name, "REFRESH SYNC (ms) : " + fOTHParameters.Refresh_Sync);
                LogSystem.LogMessage(this.GetType().Name, "REFRESH EVENT (s) : " + fOTHParameters.Refresh_Event);
                LogSystem.LogMessage(this.GetType().Name, "---------------------------------------------------------");
           
            int counter = 0;
            while (fBexit)
            {

                try

                {

      
                    StartRotatingControl(siteobj.ToString());


                }
                catch (Exception ex)


                {

                    LogSystem.LogMessage(this.GetType().Name, "Error in starting rotating control Contractors Alignments ", ex);


                }

                Thread.Sleep(fOTHParameters.Refresh_Event);
            }

        }


        public void StartRotatingControl(string sitecode)
        {
            

            // copy new records

           try
            {
                SqlConnection cnn;
                cnn = fdb.OpenConnectionDB3rdP(fOTHParameters.ConnectionString);

                using (cnn)
                    {

                SqlCommand Command = new SqlCommand("select * from " + fOTHParameters.TableName  + " where CollectedTime is null;", cnn);
                SqlDataReader Reader = Command.ExecuteReader();
                string QueryInsert = "";
                string Visitor_Data = "";
                int Visitor_ID = 0;

                
                while (Reader.Read() && Reader.HasRows)
                {

                    QueryInsert = "INSERT into OTH_Visitor values ( ";
                    QueryInsert += "'" + fdb.TruncateAndFix(Reader["LastName"].ToString(), 40) + "',";
                    QueryInsert += "'" + fdb.TruncateAndFix(Reader["FirstName"].ToString(), 40) + "',";
                    QueryInsert += "'" + fdb.TruncateAndFix(Reader["CompanyName"].ToString(), 50) + "',";
                    QueryInsert += "'" + Reader["BadgeCode"].ToString() + "',";
                    QueryInsert += Reader["VisitStatus"].ToString() + ",";
                    QueryInsert += "'" + Reader["VisitProfile"].ToString() + "',";
                    QueryInsert += "'" + sitecode + "');";
                  

                    Visitor_Data = Reader["LastName"].ToString() + ' ' + Reader["FirstName"].ToString() + ' ' + Reader["BadgeCode"].ToString();
                    Visitor_ID = int.Parse (Reader["VisitorID"].ToString()) ;

                        if (fdb.ExecQuery(QueryInsert) >= 1)
                        {
                            // Close lineimported

                            // New Data imported
                            LogSystem.LogMessage(this.GetType().Name, " New Visitor Add: " + Visitor_Data + " (" + sitecode + ") ");
                            fdb.ExecQueryExt("update " + fOTHParameters.TableName + " set CollectedTime = getdate() where VisitorID = " + Visitor_ID, fOTHParameters.ConnectionString);

                        }

                        else

                        {
                            LogSystem.LogMessage(this.GetType().Name, " ERROR NEW  Visitor NOT Add: " + Visitor_Data + " (" + sitecode + ") " + QueryInsert);

                        }
                    }

                
                }
            }




            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name, "Visitor Import " + sitecode , ex);

            }
        }
                 


        }

    }
