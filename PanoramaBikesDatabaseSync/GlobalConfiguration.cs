using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;

namespace PanoramaBikesDatabaseSync
{
    public class GlobalConfiguration
    {
        public class PrestaProduct
        {
            public int Id_Product = 0;            
            public int Id_Product_Attribute = 0;            
            public string ReferenceCode;
        }

        public List<PrestaProduct> ProductMappings;

        public Dictionary<string, int> ReferenceQuantityMap;

        public string ExecuteUpdateURL;
        public string ConfigLoadURL;
        public string SyncKey1;
        public string SyncKey2;
        public string InvalidCharacters;
        public List<int> DebugProductIds;
        private string ApothikiFile;


        public bool LoadConfiguration()
        {
            ProductMappings = new List<PrestaProduct>();                                               
            
            LogSystem.Log("Loading Configuration...");

            ExecuteUpdateURL = ConfigurationManager.AppSettings["ExecuteUpdateURL"];
            ConfigLoadURL = ConfigurationManager.AppSettings["ConfigLoadURL"];
            InvalidCharacters = ConfigurationManager.AppSettings["InvalidCharacters"];
            SyncKey1 = ConfigurationManager.AppSettings["SyncKey1"];
            SyncKey2 = ConfigurationManager.AppSettings["SyncKey2"];            
            ApothikiFile = ConfigurationManager.AppSettings["ApothikiFile"];
            DebugProductIds = new List<int>();

            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["DebugProductIds"]))
            {                
                ConfigurationManager.AppSettings["DebugProductIds"].Split(';').ToList().ForEach( id => DebugProductIds.Add(Int32.Parse(id)));

                DebugProductIds = DebugProductIds.Distinct().ToList();

                LogSystem.Debug("Debug Mode - Only these product Ids will be parsed: " + string.Join(";", DebugProductIds.ConvertAll(id => id.ToString())));
            }
            
            LogSystem.Log("Execute Update URL: " + ExecuteUpdateURL);
            LogSystem.Log("Config Load From URL: " + ConfigLoadURL);
            LogSystem.Log("Invalid Characters: " + InvalidCharacters);
            LogSystem.Log("SyncKey1: " + SyncKey1);
            LogSystem.Log("SyncKey2: " + SyncKey2);                        
            LogSystem.Log();
          
            if (!File.Exists(ApothikiFile))
            {
                LogSystem.Error("Apothiki file was not found!");
                Environment.Exit(0);
            }

            LogSystem.Log("Parsing Apothiki File...");

            ReferenceQuantityMap = new Dictionary<string, int>();

            StreamReader file = new StreamReader(ApothikiFile);
           
            //First line are headers!
            string line = file.ReadLine();
            while ((line = file.ReadLine()) != null)
            {
                List<string> data = line.Replace(",00","").Split(';').ToList();

                if (data.Count != 2)
                    continue;

                int quantity = 0;
                if (!int.TryParse(data[1], out quantity))
                    continue;

                ReferenceQuantityMap[data[0]] = quantity;
            }

            LogSystem.Log(string.Format("Products found: {0}", ReferenceQuantityMap.Count));

            return true;
        }



    }
}
