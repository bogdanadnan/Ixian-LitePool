using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text;
using LP.Meta;
using IXICore;
using IXICore.Meta;
using LP.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace LP.Pool
{
    public class ConfigData
    {
        public string poolUrl { get; set; }
        public double poolFee { get; set; }
        public double blockReward { get; set; }
    }

    public class ConfigController : ApiController
    {
        public HttpResponseMessage Get()
        {
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, "");

            string configData = "";

            if (!MemCache.Instance.TryGetValue("config_data", out configData) || string.IsNullOrEmpty(configData))
            {
                var networkHeight = IxianHandler.getHighestKnownNetworkBlockHeight();
                if (networkHeight <= 0)
                {
                    networkHeight = 2000000;
                }
                var blockReward = ConsensusConfig.calculateMiningRewardForBlock(networkHeight);

                configData = "var Config = " + JsonConvert.SerializeObject(new ConfigData
                {
                    poolUrl = Config.poolUrl,
                    poolFee = Config.poolFee,
                    blockReward = (double)(blockReward.getAmount() / 100000000)
                });

                MemCache.Instance.Set("config_data", configData, new TimeSpan(1, 0, 0));
            }

            response.Content = new StringContent(configData, Encoding.Unicode, "application/javascript");
            return response;
        }
    }
}
