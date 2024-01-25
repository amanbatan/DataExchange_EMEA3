using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;


namespace DataExchangeAPP.Class
{

 
    class DataBaseConnection
    {

        Logs LogSystem = new Logs();

        public SqlConnection OpenConnection()

        {
            try
            {
                string ConnectionString;
                SqlConnection cnn;
                ConnectionString = DataExchangeSrv.Properties.Settings.Default.DBIntegrConnection;

                cnn = new SqlConnection(ConnectionString);
                cnn.Open();
               // Console.WriteLine("Connection Completed");

                return cnn;
            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"Connection Error", ex);
                return null;
            }

        }

        public SqlConnection OpenConnectionOGDB()

        {
            try
            {
                string ConnectionString;
                SqlConnection cnn;
                ConnectionString = DataExchangeSrv.Properties.Settings.Default.DBOnGuard ;

                cnn = new SqlConnection(ConnectionString);
                cnn.Open();

                return cnn;
            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"Connection Error", ex);
                return null;
            }

        }


        public SqlConnection OpenConnectionDB3rdP(string connectionstring)

        {
            try
            {
                string ConnectionString;
                SqlConnection cnn;
                ConnectionString = connectionstring;

                cnn = new SqlConnection(ConnectionString);
                cnn.Open();

                return cnn;
            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"Connection Error", ex);
                return null;
            }

        }

        public void RetentionTime()
        {

            try

            {
                DateTime  LastDayToKeep;
                int DaysToMaintein =(DataExchangeSrv.Properties.Settings.Default.RetentionDay);
                int RecordAffected = 0;

                LastDayToKeep = DateTime.Now.AddDays(DaysToMaintein * -1);


                RecordAffected = ExecQuery("DELETE from SAP_Transactions  where EXPORTED < '" + SqlData(LastDayToKeep) + "' and Exported is not null");

                LogSystem.LogMessage(this.GetType().Name,"Cleaned old data, total record affected : " + RecordAffected);
            }

            catch (Exception ex)
            {

                LogSystem.LogMessage(this.GetType().Name,"Connection Error", ex);
            }


        }


        public int ExecQueryDBOnguard(string QueryToExec)

        {

            int RecordAffected = 0;

            try

            {

                SqlConnection cnn;
                cnn = OpenConnectionOGDB();

                using (cnn)
                {

                    SqlCommand command = new SqlCommand((QueryToExec), cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    if (QueryToExec.Contains("SELECT"))
                    {
                        if (reader.HasRows)
                        {
                            RecordAffected = 1;
                        }
                        else
                        {
                            RecordAffected = 0;
                        }
                    }
                    else
                    {
                        RecordAffected = reader.RecordsAffected;
                    }

                }

            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name, "Exec Query : ", ex);

            }

            return RecordAffected;

        }


        public int ExecQuery(string QueryToExec)

        {

            int RecordAffected =0;

            try

            {

                SqlConnection cnn;
                cnn = OpenConnection();

                using (cnn)
                {

                    SqlCommand command = new SqlCommand((QueryToExec ), cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    if (QueryToExec.Contains("SELECT"))
                    {
                        if (reader.HasRows)
                            {
                            RecordAffected = 1;
                            }
                        else
                        {
                            RecordAffected = 0;
                        }
                    }
                    else
                    {
                        RecordAffected = reader.RecordsAffected;
                    }

                }

            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"Exec Query : ", ex);
                
            }

            return RecordAffected;



        }


        public int ExecQueryExt(string QueryToExec,string ConnectionString )

        {

            int RecordAffected = 0;

            try

            {

                SqlConnection cnn;
                cnn = OpenConnectionDB3rdP(ConnectionString);

                using (cnn)
                {

                    SqlCommand command = new SqlCommand((QueryToExec), cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    if (QueryToExec.Contains("SELECT"))
                    {
                        if (reader.HasRows)
                        {
                            RecordAffected = 1;
                        }
                        else
                        {
                            RecordAffected = 0;
                        }
                    }
                    else
                    {
                        RecordAffected = reader.RecordsAffected;
                    }

                }

            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"Exec Query : ", ex);

            }

            return RecordAffected;



        }

        public  List<String> ReturnDataList (string QueryToExec, string Field)
        {
            List<String> Values = new List<String>();
            string ConnectionString="";

            try
            {

                
               
                SqlConnection cnn;
                ConnectionString = DataExchangeSrv.Properties.Settings.Default.DBIntegrConnection;

                cnn = new SqlConnection(ConnectionString);
                cnn.Open();
                
                using (cnn)
                {
                    SqlCommand command = new SqlCommand((QueryToExec), cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())

                           {

                        Values.Add(reader[Field].ToString());

                            }
                    return Values;

                }

            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"ReturnDataList : (" + ConnectionString + ") ", ex);
                return Values;
            }
        }

        private string SqlData(DateTime DateRcv)
        {
            
            
            string SqlDate, day, month, year, hour, minute;
            try
            {
                day = (DateRcv.Day).ToString();
                month = (DateRcv.Month).ToString();
                year = (DateRcv.Year).ToString();
                hour = (DateRcv.Hour).ToString();
                minute = (DateRcv.Minute).ToString();

                SqlDate = year.PadLeft(2, '0') + '-' + month.PadLeft(2, '0') + '-' + day.PadLeft(2, '0') + 'T' + hour.PadLeft(2, '0') + ':' + minute.PadLeft(2, '0') + ":00.000";

            } 
            
            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"Creating SqlData : ", ex);
                SqlDate = "1970-01-01T00:00:00.000";
            }


            return SqlDate;

        }


        public String  ReturnDataStr(string QueryToExec, string Field)
        {
            string StringToReturn = "";
            
            try
            {

                string ConnectionString;

                SqlConnection cnn;
                ConnectionString = DataExchangeSrv.Properties.Settings.Default.DBIntegrConnection;

                cnn = new SqlConnection(ConnectionString);
                cnn.Open();

                using (cnn)
                {
                    SqlCommand command = new SqlCommand((QueryToExec), cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())

                    {

                        StringToReturn = (reader[Field].ToString());

                    }
                    return StringToReturn;

                }

            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"ReturnDataList : ", ex);
                return StringToReturn;
            }
        }


        public String ReturnDataStrOG(string QueryToExec, string Field)
        {
            string StringToReturn = "";

            try
            {

                string ConnectionString;

                SqlConnection cnn;
                ConnectionString = DataExchangeSrv.Properties.Settings.Default.DBOnGuard ;

                cnn = new SqlConnection(ConnectionString);
                cnn.Open();

                using (cnn)
                {
                    SqlCommand command = new SqlCommand((QueryToExec), cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())

                    {

                        StringToReturn = (reader[Field].ToString());

                    }
                    return StringToReturn;

                }

            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name, "ReturnDataList : ", ex);
                return StringToReturn;
            }
        }
        public string isNullFieldStr(SqlDataReader reader, string field)
        {
            if (reader[field] != null)

            {
                return reader[field].ToString() ;
            }
            else

            {
                return "";
            }
        }


        public int isNullFieldNum(SqlDataReader reader, string field)
        {
            if (reader[field] != null)

            {
                return int.Parse(reader[field].ToString() );
            }
            else

            {
                return 0;
            }
        }

        public List<String> ReturnDataListOthDB(string QueryToExec, string Field,string ConnStr)
        {
            List<String> Values = new List<String>();
            try
            {

                string ConnectionString;

                SqlConnection cnn;
                ConnectionString = ConnStr;

                cnn = new SqlConnection(ConnectionString);
                cnn.Open();

                using (cnn)
                {
                    SqlCommand command = new SqlCommand((QueryToExec), cnn);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())

                    {

                        Values.Add(reader[Field].ToString());

                    }
                    return Values;

                }

            }

            catch (Exception ex)

            {
                LogSystem.LogMessage(this.GetType().Name,"ReturnDataList : ", ex);
                return Values;
            }
        }

        public string TruncateAndFix( string value, int maxLength)
        {
            value = value.ToString().Replace("'", "''");
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }



    }
}
