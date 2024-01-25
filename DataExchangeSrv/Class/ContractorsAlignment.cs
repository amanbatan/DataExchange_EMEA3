using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data.SqlClient;

namespace DataExchangeAPP.Class
{

    public struct EventContractorDetails
    {
        public int id;
        public string UserType;
        public string CompanyCode;
        public string CardholderID;
        public string ReplacementBadgeCode;
        public string Date_Event_Time;
        public int EventType;

    }
    class ContractorsAlignment
    {
       

        DataBaseConnection fdb = new DataBaseConnection();
        Boolean fBexit = true;
        ConfOption fOTHserver = new ConfOption();
        ConfOption.OTHParamters fOTHParameters;

        Logs LogSystem = new Logs();

        Boolean fFirst = true;


        


        public void StartThreadService(object siteobj)

        {


            fOTHParameters = fOTHserver.OTHConnectionData(siteobj.ToString(), "E");
            LogSystem.LogMessage(this.GetType().Name,"----CONTRACTOR ALIGNMENT---------------------------------");
            LogSystem.LogMessage(this.GetType().Name,"OTH SERVER : " + siteobj);
            LogSystem.LogMessage(this.GetType().Name,"CONNECTION STRING : " + fOTHParameters.ConnectionString);
            LogSystem.LogMessage(this.GetType().Name,"TABLE NAME : " + fOTHParameters.TableName);
            LogSystem.LogMessage(this.GetType().Name,"REFRESH SYNC (ms) : " + fOTHParameters.Refresh_Sync);
            LogSystem.LogMessage(this.GetType().Name,"REFRESH EVENT (ms) : " + fOTHParameters.Refresh_Event);
            LogSystem.LogMessage(this.GetType().Name,"---------------------------------------------------------");

            int counter = 0;
            while (fBexit)
            {

                try

                {

                    if (counter== fOTHParameters.Refresh_Sync)
                    {
                        LogSystem.LogMessage(this.GetType().Name,"Contractors Data Alignments");
                        LogSystem.LogMessage(this.GetType().Name,"---------------------------");
                        ContractorsImports CtrImp = new ContractorsImports();
                        CtrImp.StartControl(siteobj.ToString(), fFirst);
                        counter = 0;
                        fFirst = false;
                    }

                    counter = counter + 1;
                    StartRotatingControlContractor();

                  



                }
                catch (Exception ex)


                {

                    LogSystem.LogMessage(this.GetType().Name,"Error in starting rotating control Contractors Alignments ", ex);


                }

                Thread.Sleep(fOTHParameters.Refresh_Event);
            }

        }

        public void StartRotatingControlContractor()
        {

            List<EventContractorDetails> NewData = new List<EventContractorDetails>();

            try

            {

                string QueryTransaction;

                SqlConnection cnn;

                QueryTransaction = " SELECT * ";
                QueryTransaction += " FROM  " + fOTHParameters.TableName + " ";
                QueryTransaction += " where  CollectedTime is null;";


                cnn = fdb.OpenConnectionDB3rdP(fOTHParameters.ConnectionString);

           

                using (cnn)
                {

                    SqlCommand Command = new SqlCommand(QueryTransaction, cnn);
                    SqlDataReader Reader = Command.ExecuteReader();


                    while (Reader.Read())
                    {

                        EventContractorDetails NewItem = new EventContractorDetails
                        {
                        
                            id = fdb.isNullFieldNum(Reader, "id"),
                            UserType = fdb.isNullFieldStr (Reader, "usertype"),
                            CompanyCode = fdb.isNullFieldStr(Reader, "companycode"),
                            CardholderID = fdb.isNullFieldStr(Reader, "CardholderID"),
                            ReplacementBadgeCode = fdb.isNullFieldStr(Reader, "ReplacementBadgeCode"),
                            Date_Event_Time = fdb.isNullFieldStr(Reader, "DateTimeEvent"),
                            EventType = fdb.isNullFieldNum(Reader, "EventType"),

                        };


                        NewData.Add(NewItem);
                    }


                    Reader.Close();
                }

                if (NewData.Count > 0)
                {

                    LogSystem.LogMessage(this.GetType().Name,"Total transaction external to process " + NewData.Count);

                    foreach (EventContractorDetails Item in NewData)
                    {

                        LogSystem.LogMessage(this.GetType().Name,"ID" + Convert.ToChar(9) + ": " + Item.id);
                        LogSystem.LogMessage(this.GetType().Name,"CompanyCode" + Convert.ToChar(9) + ": " + Item.CompanyCode);
                        LogSystem.LogMessage(this.GetType().Name,"CardHolderID" + Convert.ToChar(9) + ": " + Item.CardholderID);
                        LogSystem.LogMessage(this.GetType().Name,"Badge ID" + Convert.ToChar(9) + ": " + Item.ReplacementBadgeCode);

                        //Convert Item.ReplacementBadgeCode to long
                        long longValue = Convert.ToInt64(Item.ReplacementBadgeCode, 16);
                        // Convert long to Hex
                        string BadgeIdDec = longValue.ToString();
                        //Used to get Cid from OTH_Contractors using CardholderID from LNL_CH_INTERACTION
                        String Cid = fdb.ReturnDataStr("Select CID from OTH_Contractors where CardHolderID like '" + Item.CardholderID + "'", "CID");


                        if (Item.EventType == 0) //Assign_Temporary_Badge
                        {
                            fdb.ExecQuery("update OTH_Contractors set badgecodeR =  '" + Item.ReplacementBadgeCode + "', recordstatus = 2 where cardholderid = " + Item.CardholderID);
                            LogSystem.LogMessage(this.GetType().Name,"Temporary Badge Assigned");



                            // if usertype = 0 copy data to temprary badge(table) EMPLOYEE
                            if(int.Parse(Item.UserType) == 0)
                            {
                                fdb.ExecQuery( " INSERT INTO OTH_Temporary(CID, BadgeID, EventType) VALUES('" + Item.CardholderID + "','" + BadgeIdDec + "'," + Item.EventType + ")" );
                            }
                            // if usertype = 1 copy data to temprary badge(table) CONTRACTOR
                            else if (int.Parse(Item.UserType) == 1)
                            {
                                if(Cid != "" )
                                {
                                    fdb.ExecQuery(" INSERT INTO OTH_Temporary(CID, BadgeID, EventType) VALUES('" + Cid + "','" + BadgeIdDec + "'," + Item.EventType + ")");
                                    LogSystem.LogMessage(this.GetType().Name, "CONTRACTOR: Temporary Badge Assigned CID: " + Cid);
                                }
                                else
                                {
                                    LogSystem.LogMessage(this.GetType().Name, "CONTRACTOR: No Contactor found with CardHolderID: " + Item.CardholderID);
                                }
                            }



                        }
                        else if (Item.EventType == 1) //De-Assign_Temporary_Badge
                        {
                            fdb.ExecQuery("update OTH_Contractors set badgecodeR = 'NULL', recordstatus = 2 where cardholderid = " + Item.CardholderID);
                            LogSystem.LogMessage(this.GetType().Name,"Temporary Badge De-Assigned");


                            // if usertype = 0  copy data to temprary badge(table) EMPLOYEE
                            if (int.Parse(Item.UserType) == 0)
                            {
                                fdb.ExecQuery(" INSERT INTO OTH_Temporary(CID, BadgeID, EventType) VALUES('" + Item.CardholderID + "','" + BadgeIdDec + "'," + Item.EventType + ")");
                            }
                            // if usertype = 1  copy data to temprary badge(table) CONTRACTOR
                            else if (int.Parse(Item.UserType) == 1)
                            {
                                if (Cid != "")
                                {
                                    fdb.ExecQuery(" INSERT INTO OTH_Temporary(CID, BadgeID, EventType) VALUES('" + Cid + "','" + BadgeIdDec + "'," + Item.EventType + ")");
                                    LogSystem.LogMessage(this.GetType().Name, "CONTRACTOR: Temporary Badge De-Assigned CID: " + Cid);
                                }
                                else
                                {
                                    LogSystem.LogMessage(this.GetType().Name, "CONTRACTOR: No Contactor found with CardHolderID: " + Item.CardholderID);
                                }
                            }
                        }

                        fdb.ExecQueryExt("Update " + fOTHParameters.TableName + " set CollectedTime = GetDate() where id =  " + Item.id.ToString() + ";", fOTHParameters.ConnectionString);

                    }


                    NewData = null;

                }


            }
            catch (Exception ex)


            {

                LogSystem.LogMessage(this.GetType().Name,"Error in rotating control ", ex);


            }

            
        }
    }

}




