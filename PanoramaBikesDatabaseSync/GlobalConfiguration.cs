using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Globalization;

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

        // Key is Product Code - Value is Tuple : Quantity & Price
        public Dictionary<string, Tuple<int,float>> ReferenceQuantityMap;

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
            
            LogSystem.Log("Execute Update URL: " + ExecuteUpdateURL ?? "N/A");
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

            ReferenceQuantityMap = new Dictionary<string, Tuple<int, float>>();

            StreamReader file = new StreamReader(ApothikiFile);
           
            //First line holds the headers!
            string line = file.ReadLine();
            while ((line = file.ReadLine()) != null)
            {
                List<string> data = line.Split(';').ToList();

                if (data.Count != 3)
                    continue;

                if (!int.TryParse(data[1].Replace(",00", ""), out int quantity))
                    continue;

                data[2] = data[2].Replace(',', '.');
                if (!float.TryParse(data[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float price))
                    continue;

                ReferenceQuantityMap[data[0]] = new Tuple<int, float>(quantity, price);
            }

            LogSystem.Log(string.Format("Products found: {0}", ReferenceQuantityMap.Count));

            return true;
        }
    }
}
