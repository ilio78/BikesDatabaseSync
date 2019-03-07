using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PanoramaBikesDatabaseSync
{
    public static class LogSystem
    {
        public static void Log(string msg, bool isError = false)
        {
            msg = msg ?? string.Empty;
            
            Console.WriteLine(msg);

            if (!isError)
                return;

            using (StreamWriter file = new StreamWriter(@"Logging.txt", true))
            {
                file.WriteLine(msg);
            }
        }

        public static void Log()
        {
            Log(string.Empty);
        }

        public static void Error(string msg)
        {
            Log(DateTime.Now.ToShortTimeString() + " : Error : " + msg, true);
        }

        public static void Debug(string msg)
        {
            Log(DateTime.Now.ToShortTimeString() + " : Debug : " + msg, true);
        }

    }
}
