using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataExchangeAPP.Class
{
    public class Site
    {

        public string name;
        public string IN;
        public string OUT;
       

        public Site(String name)
        {
            this.name = name;
            this.IN = getINString();
            this.OUT = getOutString();
        }


      
      

    }
}
