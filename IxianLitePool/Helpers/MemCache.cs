using System;
using Microsoft.Extensions.Caching.Memory;

namespace LP.Helpers
{
    public class MemCache : MemoryCache
    {
        private static MemCache instance = null;
        public static MemCache Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new MemCache();
                }
                return instance;
            }
        }

        public MemCache() : base(new MemoryCacheOptions())
        {
        }
    }
}
