﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Net;

namespace PanoramaBikesDatabaseSync
{
    class Program
    {
        static void Main(string[] args)
        {
            LogSystem.Debug("Starting Panorama Bikes Database Sync");
            LogSystem.Debug("=====================================");
            LogSystem.Log("");

            GlobalConfiguration gc = new GlobalConfiguration();
                
            gc.LoadConfiguration();

            string configLoadUrl = string.Format("{0}?SyncKey1={1}&SyncKey2={2}", gc.ConfigLoadURL, gc.SyncKey1, gc.SyncKey2); 
            //http://www.panoramabikes.gr/sync-sys/sync1.php?SyncKey1=dbsync2014&SyncKey2=dbsync201445

            LogSystem.Log("GET request to : " + configLoadUrl + " ...");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(configLoadUrl);
            request.Method = "GET";
            String dataReceived = String.Empty;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream dataStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        dataReceived = reader.ReadToEnd();
                    }
                }
            }

            LogSystem.Log("Request completed!");

            List<string> dataRows = dataReceived.Replace("<br>","\n").Split('\n').ToList();

            LogSystem.Log("Products retrieved: " + dataRows.Count);
            LogSystem.Log("Parsing data...");
            LogSystem.Log("Expected columns are:");
            LogSystem.Log("Id_Product, Reference, Id_Product_Attribute, Reference_Product_Attribute");
            LogSystem.Log("------------------------------------------------------------------------");

            foreach (string row in dataRows)
            {
                if (string.IsNullOrWhiteSpace(row))
                    continue;

                List<string> fields = row.Split(';').ToList();

                //512;;629;xxxx is OK

                if (fields.Count != 4)
                {
                    //if this happens something is really wrong!
                    LogSystem.Error("Failed to detect fields for: " + row);
                    continue;
                }

                GlobalConfiguration.PrestaProduct productMap = new GlobalConfiguration.PrestaProduct();
                
                if (!int.TryParse(fields[0], out productMap.Id_Product))
                {
                    //again...if this happens something is really wrong!
                    LogSystem.Error("Failed to detect product Id on: " + row);
                    continue;
                }

                if (gc.DebugProductIds.Count > 0 && !gc.DebugProductIds.Contains(productMap.Id_Product))
                    continue;
                

                //We have a product Id now check to see if there is a product attribute Id!
                if (int.TryParse(fields[2], out productMap.Id_Product_Attribute))
                {
                    //in this case we need to have also a product attribute reference
                    productMap.ReferenceCode = fields[3];
                }
                else
                {
                    //if not product attribute Id is found then we need a reference code
                    productMap.ReferenceCode = fields[1];
                }

                if (string.IsNullOrWhiteSpace(productMap.ReferenceCode))
                {
                    LogSystem.Error("Reference code is null or empty: " + row);
                    continue;
                }

                if (productMap.ReferenceCode.Intersect(gc.InvalidCharacters.ToArray()).Count() > 0)
                {
                    LogSystem.Error("Reference code contains invalid characters: " + row);
                    continue;
                }                
                gc.ProductMappings.Add(productMap);
            }

            LogSystem.Log();
            LogSystem.Log("Computing product quantities...");
            
            Dictionary<int, int> panora_stock_available_ProductQuantities = new Dictionary<int,int>();
            Dictionary<int, int> panora_stock_available_AttributeQuantities = new Dictionary<int,int>();            

            for (int i = 0; i < gc.ProductMappings.Count; i++)
            {

                if (!gc.ReferenceQuantityMap.TryGetValue(gc.ProductMappings[i].ReferenceCode, out Tuple<int, float> productInfo))
                {
                    string itemConcerned = "productId " + gc.ProductMappings[i].Id_Product.ToString();
                    if (gc.ProductMappings[i].Id_Product_Attribute > 0)
                        itemConcerned = "productAttributeId " + gc.ProductMappings[i].Id_Product_Attribute;

                    LogSystem.Error(string.Format("Reference code {0} for {1} was not found in warehouse file.",
                                    gc.ProductMappings[i].ReferenceCode, itemConcerned));
                    continue;
                }

                // Check Quantity...
                if (productInfo.Item1 < 0)
                {
                    LogSystem.Error(string.Format("For reference code {0} in warehouse file quantity is less than 0!",
                                    gc.ProductMappings[i].ReferenceCode));
                    continue;
                }
                
                //If there is an attribute use it to update the other table...
                if (gc.ProductMappings[i].Id_Product_Attribute > 0)
                    panora_stock_available_AttributeQuantities.Add(gc.ProductMappings[i].Id_Product_Attribute, productInfo.Item1);
                    
                // The quantity of the whole family is the sum of the quantities of the children!                    
                panora_stock_available_ProductQuantities.TryGetValue(gc.ProductMappings[i].Id_Product, out int productQuantity);
                productQuantity += productInfo.Item1;
                panora_stock_available_ProductQuantities[gc.ProductMappings[i].Id_Product] = productQuantity;                                        
            }

            LogSystem.Log("Quantities computed!");
            LogSystem.Log();
            LogSystem.Log("Generating SQL Command file...");

            StringBuilder panora_stock_available1 = new StringBuilder(2048);
            StringBuilder panora_stock_available2 = new StringBuilder(2048);
            //StringBuilder panora_product = new StringBuilder(2048);
            using (StreamWriter file = new StreamWriter(@"SQLCommands.txt", false))
            {
                file.WriteLine("");
                file.WriteLine("-- File created on: " + DateTime.Now.ToLongTimeString());
                file.WriteLine("");
                file.WriteLine("-- Product attribute SQL update commands:");
                
                foreach (KeyValuePair<int, int> itemQuantity in panora_stock_available_AttributeQuantities)
                {                    
                    panora_stock_available1.AppendFormat("{0},{1};",itemQuantity.Value, itemQuantity.Key);
                    file.WriteLine(string.Format("update panora_stock_available set quantity = {0} where id_product_attribute = {1}", 
                                                    itemQuantity.Value, itemQuantity.Key));
                }
                
                foreach (KeyValuePair<int, int> itemQuantity in panora_stock_available_ProductQuantities)
                {
                    panora_stock_available2.AppendFormat("{0},{1};", itemQuantity.Value, itemQuantity.Key);
                    file.WriteLine(string.Format("update panora_stock_available set quantity = {0} where id_product_attribute = 0 and id_product = {1}", 
                                                    itemQuantity.Value, itemQuantity.Key));
                }

                file.WriteLine("-- Completed SQL update commands:");
            }

            LogSystem.Log("Finished 'combinations' SQL commands.");
            LogSystem.Log();           

            if (string.IsNullOrWhiteSpace(gc.ExecuteUpdateURL))
            {
                LogSystem.Log("ExecuteUpdateURL is null - Website DB will not be updated!");
                LogSystem.Log();
                EndMessage();
                return;
            }

            request = (HttpWebRequest)HttpWebRequest.Create(gc.ExecuteUpdateURL);
            request.Method = "POST";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
            request.ContentType = "application/x-www-form-urlencoded";

            string postData =
                String.Format("SyncKey1={0}&SyncKey2={1}&valueset_panora_stock_available1={2}&valueset_panora_stock_available2={3}",
                    gc.SyncKey1, gc.SyncKey2, panora_stock_available1.ToString(), panora_stock_available2.ToString());            
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentLength = byteArray.Length;
            Stream datastream = request.GetRequestStream();
            datastream.Write(byteArray, 0, byteArray.Length);
            datastream.Close();

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream dataStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        dataReceived = reader.ReadToEnd();
                        LogSystem.Debug("Post response start:");
                        LogSystem.Debug(dataReceived);
                        LogSystem.Debug("Post response end.");
                    }
                }                
            }

            LogSystem.Log("Finished DB Update");
            LogSystem.Log();
            EndMessage();
        }

        static void EndMessage()
        {

            LogSystem.Log();
            LogSystem.Debug("End of running Panorama Bikes Database Sync");
            LogSystem.Debug("===========================================");
            LogSystem.Log();

            Console.ReadLine();
        }

    }

  
}
