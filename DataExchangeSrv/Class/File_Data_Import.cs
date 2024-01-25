using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Xml;

namespace DataExchangeAPP.Class
{
    class FileDataImport
    {
        Logs fLogSystem = new Logs();
        DataBaseConnection fDBConn = new DataBaseConnection();
        string fBadgeHex;
        private ConfOption confOption = new ConfOption();

        public struct EmployeeData
        {
           
            public string FirstName;
            public string LastName;
            public string BadgeID;
            public string VirtualBadgeID;
            public string Plate1; 
            public string Plate2;
            public string CID;
            public string BadgeActivation;
            public string BadgeDeactivation;
            public string CIDActive;
            public string CostCenter;
            public string Company;
            public string SiteID;
            public int status; //  1  - New      2 - Update     3 -  Delete
        }

    
           public void StartThreadService (object site)
        {

            fLogSystem.LogMessage(this.GetType().Name,"Retriving URL for FTP site " + site);
            string UrlToUse;
            int RefreshTime;
            

            UrlToUse = fDBConn.ReturnDataStr("select url from  SAP_FTP_URL where site='" + site + "';","url");
            fBadgeHex = fDBConn.ReturnDataStr("select BadgeHex from  SAP_FTP_URL where site='" + site + "';", "BadgeHex");
            RefreshTime =  Convert.ToInt32(fDBConn.ReturnDataStr("select RefreshTime from  SAP_FTP_URL where site='" + site + "';", "RefreshTime"));
            if (UrlToUse != "")
            {
                fLogSystem.LogMessage(this.GetType().Name,"FTP URL Site " + UrlToUse);
               
                while (true)

                {
                    Thread.Sleep(0);
                    ProcessDirectory(UrlToUse, site.ToString());
                    Thread.Sleep(RefreshTime);
                }



            }

        }




        public void ProcessDirectory(string targetDirectory, string SiteID)
        {

            try
            {


                string[] fileEntries = Directory.GetFiles(targetDirectory, "*00000259e0ab.xml");

                if (fileEntries.Count() > 0)
                {


                    fLogSystem.LogMessage(this.GetType().Name,"Total New file found : " + fileEntries.Count().ToString());
               
                    DateTime fStartTime = DateTime.Now;

                    try
                    {
                        Boolean ResultImport = true;





                        foreach (string fileName in fileEntries)
                        {
                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.Load(fileName);

                            fLogSystem.LogMessage(this.GetType().Name, fileName);


                            // Select all E1BPCC1DNPERSO nodes
                            XmlNodeList personNodes = xmlDoc.SelectNodes("/HRCC1DNPERSO01/IDOC/E1BPCC1DNPERSO");


                            // Iterate through each personnel record
                            foreach (XmlNode personNode in personNodes)
                            {


                                if (InsertData(DataToImport(personNode, SiteID)) == false) // Run query to insert
                                {
                                    ResultImport = false;
                                    fLogSystem.LogMessage(this.GetType().Name, "Error on XmlNode TIMEID_NO :  " + personNode.InnerText );
                                }

                            }

                            // If no error is found during import, move the file to the "Processed" folder
                            if (ResultImport)
                            {
                                string processedFolderPath = Path.Combine(targetDirectory, "Processed");

                                // Create the "Processed" folder if it doesn't exist
                                if (!Directory.Exists(processedFolderPath))
                                {
                                    Directory.CreateDirectory(processedFolderPath);
                                }

                                // Generate a new file name with the current datetime
                                string newFileName = Path.Combine(processedFolderPath, $"ProcessedFile_{DateTime.Now:yyyyMMddHHmmssfff}.xml");

                                try
                                {
                                    // Move the file to the "Processed" folder with the new name
                                    File.Move(fileName, newFileName);
                                    fLogSystem.LogMessage(this.GetType().Name, $"File moved to: {newFileName}");
                                }
                                catch (Exception moveException)
                                {
                                    fLogSystem.LogMessage(this.GetType().Name, $"Error moving file: {moveException.Message}");
                                }
                            }




                        }
                     

                    }

                    catch (Exception ex)
                    {
                        fLogSystem.LogMessage(this.GetType().Name,"Process Directory " + ex.Message); 
                    }

        
            

                    TimeSpan TotalTime;
                    TotalTime = DateTime.Now - fStartTime;


                    fLogSystem.LogMessage(this.GetType().Name,"Total Time To complete operation " + (TotalTime.TotalMilliseconds.ToString()) + " ms");

                }


            } 

            catch (Exception ex)
            {
                fLogSystem.LogMessage(this.GetType().Name, "Process Directory " + ex.Message);

            }

        }



        public EmployeeData DataToImport(XmlNode nodeImported, string siteid)
        {
            EmployeeData Employee = new EmployeeData();


            Employee.CID = nodeImported.SelectSingleNode("PERNO").InnerText;
            Employee.BadgeID = nodeImported.SelectSingleNode("TIMEID_NO").InnerText;
            Employee.FirstName = nodeImported.SelectSingleNode("SORT_NAME").InnerText;
            Employee.LastName = nodeImported.SelectSingleNode("EDIT_NAME").InnerText;
            Employee.BadgeActivation = nodeImported.SelectSingleNode("FROM_DATE").InnerText;
            Employee.BadgeDeactivation = nodeImported.SelectSingleNode("TO_DATE").InnerText;
            Employee.CIDActive = nodeImported.SelectSingleNode("TIMEID_VERSION").InnerText;
            //Employee.CostCenter= nodeImported.SelectSingleNode("TIMEID_NO").InnerText;
            //Employee.Company  = nodeImported.SelectSingleNode("TIMEID_NO").InnerText;
            Employee.SiteID = siteid;




            //Check if CID exists, if yes status = 2(Modify) else 1(New)

            if (confOption.CIDExist(Employee.CID))
                Employee.status = 2;
            else
                Employee.status = 1;



                return Employee;
        }

        public Boolean InsertData(EmployeeData EmployeeData)
        {


            // check Badge Mode

            if  (fBadgeHex == "1")

            {
                //conversione HEX a DEC
                EmployeeData.BadgeID = (Convert.ToInt64(EmployeeData.BadgeID, 16)).ToString() ;

            }



            string QueryString;
            Boolean Result = false;

            QueryString = "INSERT into OTH_Employee (FirstName, LastName, BadgeID, CID, BadgeActivation, BadgeDeactivation, site, CIDActive, costcenter, company,  status) ";
            QueryString += " VALUES ";
   
            QueryString += " ('" + EmployeeData.FirstName.Replace("'","''") + "'," ;
            QueryString += " '" + EmployeeData.LastName.Replace("'", "''") +  "',";
            QueryString += " '" + EmployeeData.BadgeID  + "',";
            QueryString += " '" + EmployeeData.CID + "',";
            QueryString += " '" + EmployeeData.BadgeActivation  + "',";
            QueryString += " '" + EmployeeData.BadgeDeactivation + "',";
            QueryString += " '" + EmployeeData.SiteID + "',";
            QueryString += " '" + EmployeeData.CIDActive  + "',";
            QueryString += " '" + EmployeeData.CostCenter + "',";
            QueryString += " '" + EmployeeData.Company + "',";
            QueryString += " " + EmployeeData.status + ");";


            try
            {
               
                if((fDBConn.ExecQuery(QueryString)>=1))

                {
                    string MsgLog;

                    MsgLog = "\r\n"+"USER IMPORTED:" + "\r\n";
                    MsgLog += "---------------------"+ "\r\n";


                    MsgLog += "LastName : " + EmployeeData.LastName+ "\r\n";
                    MsgLog += "FirstName : " + EmployeeData.FirstName + "\r\n";
                    MsgLog += "Badge ID : " + EmployeeData.BadgeID + "\r\n";
                    MsgLog += "CID : " +  EmployeeData.CID + "\r\n";
                    MsgLog += "BadgeActivation : " + EmployeeData.BadgeActivation + "\r\n";
                    MsgLog += "BadgeDeActivation : " + EmployeeData.BadgeDeactivation + "\r\n";
                    MsgLog += "SITE ID: " + EmployeeData.SiteID  + "\r\n";
                    MsgLog += "CID ACTIVE: " + EmployeeData.CIDActive + "\r\n";
                    MsgLog += "Company: " + EmployeeData.Company + "\r\n";
                    MsgLog += "CostCenter: " + EmployeeData.CostCenter + "\r\n";
                    MsgLog += "Status : " + EmployeeData.status + "\r\n";

                    MsgLog += "---------------------" + "\r\n";
                    fLogSystem.LogMessage(this.GetType().Name,MsgLog);
                    Result = true;

                }
            }

                catch (Exception ex)

                {
                fLogSystem.LogMessage(this.GetType().Name,"Failed to insert Employee data : " + ex.Message.ToString());
                Result = false;
            }

            return Result;

        }


    }
}
