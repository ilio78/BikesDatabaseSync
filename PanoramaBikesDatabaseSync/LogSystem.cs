using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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


        private static readonly string PriceFileName;

        static LogSystem()
        {
            string dateTimeFormat = "yyyy-MM-dd-HH-mm-ss";
            PriceFileName = $"PriceCompare-{DateTime.Now.ToString(dateTimeFormat)}.html";
        }

        public static void PriceStart()
        {
            string msg = "<html><body><table style='width:100%'><th><tr>" +
                "<td>Reference</td>" +
                "<td>Id</td>" +
                "<td>Store Price</td>" +
                "<td>Website Price</td>" +
                "<td>Original Price - Discount</td></tr></th>";

            WriteToPriceFile(msg);
        }

        public static void PriceEnd()
        {
            string msg = "</table></body></html>";
            WriteToPriceFile(msg);
        }

        public static void Price(string productReference, int productId, float storePrice, float websitePrice, float websiteDiscount, string discountType)
        {
            float websiteActualPrice = websitePrice;
            string websiteDiscountText = "";
            if (discountType == "amount")
            {
                websiteActualPrice = websitePrice - websiteDiscount;
                websiteDiscountText = " - €"+ websiteDiscount;
            }
            else if (discountType == "percentage")
            {
                websiteActualPrice = websitePrice * (1 - websiteDiscount);
                websiteDiscountText = " - " + websiteDiscount + "%";
            }

            string rowColor = "green";
            if (storePrice != websiteActualPrice)
                rowColor = "red";

            string productURL = $"https://panoramabikes.gr/el/search?controller=search&orderby=position&orderway=desc&search_query={productReference}&submit_search=";

            string msg = $"<tr style='color:{rowColor}'><td><a target='_blanc' href='{productURL}'>{productReference}</a></td><td>{productId}</td><td>€{storePrice}</td><td>€{websiteActualPrice}</td><td>€{websitePrice}{websiteDiscountText}</td></tr>";

            WriteToPriceFile(msg);
        }

        private static void WriteToPriceFile(string msg)
        {
            using (StreamWriter file = new StreamWriter(PriceFileName, true))
            {
                file.WriteLine(msg);
            }
        }
    }
}
