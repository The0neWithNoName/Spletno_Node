using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Windows;

namespace MUR_Blockchain_2._0
{
    public class DefaultController : ApiController
    {
      

        public string Get()
        {
            return "Welcome To a Node API";
        }

        public string Get(string command, string username)
        {
           

            return "error";
        }

    }
}
