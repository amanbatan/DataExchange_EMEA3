using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataExchangeAPP.Class
{
    class Logs
    {

        public void LogMessage(string source, string description, Exception ErrorException = null)

        {
            String Message = "";
            if (ErrorException != null)
            {
                Message = DateTime.Now.ToString() + " - [" + source.PadRight(30,' ') + "] "  + " - " + description + '(' + ErrorException.Message + ')';
            }
            else
            {
                Message = DateTime.Now.ToString() + " - [" + source.PadRight(30, ' ') + "] "  + " - " + description;
            }

            Console.WriteLine(Message);

            if (DataExchangeSrv.Properties.Settings.Default.LogEnabled == 1)
            {

                WriteLogFile(Message);

            }

        }


        private void WriteLogFile(string Message)

        {

            string path = DataExchangeSrv.Properties.Settings.Default.LogPath + DateTime.Today.Year + DateTime.Today.Month + DateTime.Today.Day + ".txt";
            try
            {

                if (!File.Exists(path))
                {

                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine(Message);
                    }


                }
                else

                {

                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(Message);
                    }

                }
            }
            catch (Exception ex)

            {
                

            }


        }
    }
}


   


