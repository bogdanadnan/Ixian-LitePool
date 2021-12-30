using System;
using System.Collections.Generic;
using System.Linq;
using LP.DB;
using static LP.DB.PoolDB;

namespace LP.Pool
{
    public class State
    {
        Dictionary<string, PoolStateDBType> states = new Dictionary<string, PoolStateDBType>();

        private static State instance = null;
        public static State Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new State();
                }
                return instance;
            }
        }

        public State()
        {
            states = PoolDB.Instance.getAllPoolStates().GroupBy(s => s.key).ToDictionary(k => k.Key, v => v.LastOrDefault());
        }

        public string get(string key)
        {
            lock (states)
            {
                return states.ContainsKey(key) ? states[key].value : String.Empty;
            }
        }

        public void set(string key, string value)
        {
            lock (states)
            {
                if (states.ContainsKey(key))
                {
                    states[key].value = value;
                    PoolDB.Instance.setPoolState(key, value);
                }
                else
                {
                    states.Add(key, new PoolStateDBType
                    {
                        id = PoolDB.Instance.setPoolState(key, value),
                        key = key,
                        value = value
                    });
                }
            }
        }
    }
}
