using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetworkHw2Server
{
    public static class Helper
    {
        public static bool IsJson(string result)
        {

            try
            {
                JsonDocument.Parse(result);
                return true;
            }
            catch
            {
                return false;
            }
        }



    }
}

