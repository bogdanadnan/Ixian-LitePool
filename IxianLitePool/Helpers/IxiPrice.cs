using System;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace LP.Helpers
{
    public class IxiPrice
    {
        public class VitexResponseData
        {
            public string symbol;
            public string tradingCurrency;
            public string quoteCurrency;
            public string tradingCurrencyId;
            public string quoteCurrencyId;
            public string tradingCurrencyName;
            public string quoteCurrencyName;
            [JsonProperty("operator")]
            public string operator_;
            public string operatorName;
            public string operatorLogo;
            public int pricePrecision;
            public int amountPrecision;
            public string minOrderSize;
            public decimal operatorMakerFee;
            public decimal operatorTakerFee;
            public string highPrice;
            public string lowPrice;
            public string lastPrice;
            public string volume;
            public string baseVolume;
            public string bidPrice;
            public string askPrice;
            public uint openBuyOrders;
            public uint openSellOrders;
        }

        public class VitexResponse
        {
            public int code;
            public string msg;
            public VitexResponseData data;
        }

        private static IxiPrice instance = null;
        public static IxiPrice Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new IxiPrice();
                }

                return instance;
            }
        }

        private Thread ixiPriceThread = null;
        private bool ixiPriceRunning = false;

        private decimal ixiPrice = 0;

        public IxiPrice()
        {
            ixiPriceRunning = true;
            ixiPriceThread = new Thread(retrieveIxiPrice);
            ixiPriceThread.Start();
        }

        public void stop()
        {
            if (ixiPriceRunning && ixiPriceThread != null)
            {
                ixiPriceRunning = false;
                ixiPriceThread.Join();
            }
        }

        public decimal getIxiPrice()
        {
            return ixiPrice;
        }

        private void retrieveIxiPrice()
        {
            while(ixiPriceRunning)
            {
                try
                {
                    var client = new WebClient();
                    var response = client.DownloadString("https://api.vitex.net//api/v2/market?symbol=IXI-000_USDT-000");

                    if (string.IsNullOrEmpty(response) || !response.Contains("lastPrice"))
                    {
                        Thread.Sleep(60000);
                        continue;
                    }

                    var vitexData = JsonConvert.DeserializeObject<VitexResponse>(response);

                    if(vitexData == null || vitexData.data == null || !decimal.TryParse(vitexData.data.lastPrice, out ixiPrice))
                    {
                        Thread.Sleep(60000);
                        continue;
                    }

                    Thread.Sleep(60000);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Exception encountered while retrieving IXI price: {0}", ex.Message);
                    Thread.Sleep(60000);
                }
            }
        }
    }
}
