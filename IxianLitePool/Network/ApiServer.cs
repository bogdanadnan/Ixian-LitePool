// Copyright (C) 2017-2020 Ixian OU
// This file is part of Ixian DLT - www.github.com/ProjectIxian/Ixian-DLT
//
// Ixian DLT is free software: you can redistribute it and/or modify
// it under the terms of the MIT License as published
// by the Open Source Initiative.
//
// Ixian DLT is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// MIT License for more details.

using LP.Meta;
using IXICore;
using IXICore.Meta;
using IXICore.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using IXICore.Utils;
using LP.Pool;
using Microsoft.Extensions.Caching.Memory;

namespace LP.Network
{
    class APIServer : GenericAPIServer
    {
        private Node node = null;
        private MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        private int shareCount = 0;
        private DateTime shareCountTimeStamp = DateTime.Now;
        private bool sharesStarted = false;

        private bool noClients = false;

        private Dictionary<string, List<DateTime>> clientCallErrors = new Dictionary<string, List<DateTime>>(); 

        private static APIServer instance = null;
        public static APIServer Instance
        {
            get
            {
                return instance;
            }
        }

        public APIServer(Node node, List<string> listen_URLs, Dictionary<string, string> authorized_users = null, List<string> allowed_IPs = null)
        {
            this.node = node;
            instance = this;
            if(Config.noStart)
            {
                noClients = true;
            }
            
            // Start the API server
            start(listen_URLs, authorized_users, allowed_IPs);
        }

        public void resetCache()
        {
            cache.Compact(1.0);
        }

        public void lockServer()
        {
            noClients = true;
        }

        public void unlockServer()
        {
            noClients = false;
        }

        protected override void onUpdate(HttpListenerContext context)
        {
            try
            {
                if(noClients)
                {
                    context.Response.ContentType = "application/json";
                    JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_IN_WARMUP, message = "API server in lockdown mode." };
                    sendResponse(context.Response, new JsonResponse { error = error });
                    return;
                }

                string post_data = "";
                string method_name = "";
                Dictionary<string, object> method_params = null;

                HttpListenerRequest request = context.Request;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    post_data = reader.ReadToEnd();
                }
                
                if (post_data.Length > 0)
                {
                    JsonRpcRequest post_data_json = JsonConvert.DeserializeObject<JsonRpcRequest>(post_data);
                    method_name = post_data_json.method;
                    method_params = post_data_json.@params;
                }
                else
                {
                    var segments = context.Request.Url.Segments.Where(s => s != "/").ToArray();
                    if (segments.Length < 1)
                    {
                        context.Response.ContentType = "application/json";
                        JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_REQUEST, message = "Unknown action." };
                        sendResponse(context.Response, new JsonResponse { error = error });
                        return;
                    }

                    method_name = segments[0].Replace("/", "");
                    method_params = new Dictionary<string, object>();
                    foreach(string key in context.Request.QueryString.Keys)
                    {
                        if (key != null && key != "")
                        {
                            string value = context.Request.QueryString[key];
                            if(value == null)
                            {
                                value = "";
                            }
                            method_params.Add(key, value);
                        }
                    }
                }

                if (method_name == null)
                {
                    context.Response.ContentType = "application/json";
                    JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_REQUEST, message = "Unknown action." };
                    sendResponse(context.Response, new JsonResponse { error = error });
                    return;
                }

                try
                {
                    Logging.trace("Processing request " + context.Request.Url);
                    processRequest(context, method_name, method_params);
                }
                catch (Exception e)
                {
                    context.Response.ContentType = "application/json";
                    JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INTERNAL_ERROR, message = "Unknown error occured, see log for details." };
                    sendResponse(context.Response, new JsonResponse { error = error });
                    Logging.error("Exception occured in API server while processing '{0}'. {1}", context.Request.Url, e);
                }
            }
            catch (Exception e)
            {
                context.Response.ContentType = "application/json";
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INTERNAL_ERROR, message = "Unknown error occured, see log for details." };
                sendResponse(context.Response, new JsonResponse { error = error });
                Logging.error("Exception occured in API server. {0}", e);
            }
        }
                
        protected override bool processRequest(HttpListenerContext context, string methodName, Dictionary<string, object> parameters)
        {
            JsonResponse response = null;

            if (methodName.Equals("submitminingsolution", StringComparison.OrdinalIgnoreCase))
            {
                response = onSubmitMiningSolution(parameters);
            }

            if (methodName.Equals("getminingblock", StringComparison.OrdinalIgnoreCase))
            {
                response = onGetMiningBlock(parameters);
            }
            
            if (response == null)
            {
                return false;
            }

            // Set the content type to plain to prevent xml parsing errors in various browsers
            context.Response.ContentType = "application/json";

            sendResponse(context.Response, response);

            context.Response.Close();

            return true;
        }

        // Returns an empty PoW block based on the search algorithm provided as a parameter
        private JsonResponse onGetMiningBlock(Dictionary<string, object> parameters)
        {
            // Check that all the required query parameters are sent
            if (!parameters.ContainsKey("id"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'id' is missing." };
                return new JsonResponse { result = null, error = error };
            }
            if (!parameters.ContainsKey("worker"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'worker' is missing." };
                return new JsonResponse { result = null, error = error };
            }
            if (!parameters.ContainsKey("wallet"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'wallet' is missing." };
                return new JsonResponse { result = null, error = error };
            }
            if (!parameters.ContainsKey("hr"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'hr' is missing." };
                return new JsonResponse { result = null, error = error };
            }
            if (!parameters.ContainsKey("miner"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'miner' is missing." };
                return new JsonResponse { result = null, error = error };
            }

            string id = (string)parameters["id"];
            string worker = (string)parameters["worker"];
            string wallet = (string)parameters["wallet"];

            Dictionary<string, Object> resultArray = null;

            if (cache.TryGetValue(id + "_" + worker + "_" + wallet, out resultArray))
            {
                return new JsonResponse { result = resultArray, error = null };
            }

            try
            {
                byte[] addressBytes = Base58Check.Base58CheckEncoding.DecodePlain(wallet);
                if (!Address.validateChecksum(addressBytes))
                {
                    JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'wallet' is not a valid Ixian address." };
                    return new JsonResponse { result = null, error = error };
                }
            }
            catch (Exception e)
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'wallet' is not a valid Ixian address." };
                return new JsonResponse { result = null, error = error };
            }

            double hr = 0;
            if(!Double.TryParse((string)parameters["hr"], out hr))
            {
                if (!parameters.ContainsKey("hr"))
                {
                    JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'hr' is invalid." };
                    return new JsonResponse { result = null, error = error };
                }
            }

            string version = (string)parameters["miner"];

            Miner miner = new Miner((string)parameters["wallet"]);

            if(!miner.isValid())
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INTERNAL_ERROR, message = "Internal error while querying miner data." };
                return new JsonResponse { result = null, error = error };
            }

            miner.selectWorker(id, worker);
            miner.updateWorker(hr, version);

            RepositoryBlock block = node.getMiningBlock();
            if (block == null)
            {
                return new JsonResponse
                {
                    result = null,
                    error = new JsonError()
                        {code = (int) RPCErrorCode.RPC_INTERNAL_ERROR, message = "Cannot retrieve mining block"}
                };
            }

            byte[] solver_address = IxianHandler.getWalletStorage().getPrimaryAddress();

            resultArray = new Dictionary<string, Object>
            {
                {"num", block.blockNum}, // Block number
                {"ver", block.version}, // Block version
                {"dif", Pool.Pool.Instance.getDifficulty()}, // Block difficulty
                {"chk", block.blockChecksum}, // Block checksum
                {"adr", solver_address} // Solver address
            };

            cache.Set(id + "_" + worker + "_" + wallet, resultArray, new TimeSpan(0, 0, 10));

            miner.commit();

            return new JsonResponse {result = resultArray, error = null};
        }

        // Verifies and submits a mining solution to the network
        private JsonResponse onSubmitMiningSolution(Dictionary<string, object> parameters)
        {
            // Check that all the required query parameters are sent
            if (!parameters.ContainsKey("id"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'id' is missing." };
                return new JsonResponse { result = null, error = error };
            }
            if (!parameters.ContainsKey("worker"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'worker' is missing." };
                return new JsonResponse { result = null, error = error };
            }
            if (!parameters.ContainsKey("wallet"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'wallet' is missing." };
                return new JsonResponse { result = null, error = error };
            }
            if (!parameters.ContainsKey("nonce"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'nonce' is missing" };
                return new JsonResponse { result = null, error = error };
            }
            if (!parameters.ContainsKey("blocknum"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'blocknum' is missing" };
                return new JsonResponse { result = null, error = error };
            }

            string id = (string)parameters["id"];
            string worker = (string)parameters["worker"];
            string wallet = (string)parameters["wallet"];

            if(string.IsNullOrEmpty(wallet))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'wallet' is empty." };
                return new JsonResponse { result = null, error = error };
            }

            lock (clientCallErrors)
            {
                if (clientCallErrors.ContainsKey(wallet))
                {
                    clientCallErrors[wallet].RemoveAll(c => c < (DateTime.Now - (new TimeSpan(0, 1, 0))));

                    if (clientCallErrors[wallet].Count > Config.maxClientFailuresPerMinute)
                    {
                        JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_MISC_ERROR, message = "Too many failures per minute, request denied." };
                        return new JsonResponse { result = null, error = error };
                    }
                }
                else
                {
                    clientCallErrors.Add(wallet, new List<DateTime>());
                }
            }

            try
            {
                byte[] addressBytes = Base58Check.Base58CheckEncoding.DecodePlain(wallet);
                if (!Address.validateChecksum(addressBytes))
                {
                    lock (clientCallErrors)
                    {
                        clientCallErrors[wallet].Add(DateTime.Now);
                    }

                    JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'wallet' is not a valid Ixian address." };
                    return new JsonResponse { result = null, error = error };
                }
            }
            catch (Exception e)
            {
                lock (clientCallErrors)
                {
                    clientCallErrors[wallet].Add(DateTime.Now);
                }

                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'wallet' is not a valid Ixian address." };
                return new JsonResponse { result = null, error = error };
            }

            string nonce = (string)parameters["nonce"];
            if (nonce.Length < 1 || nonce.Length > 128)
            {
                lock (clientCallErrors)
                {
                    clientCallErrors[wallet].Add(DateTime.Now);
                }

                Logging.info("Received incorrect nonce from miner.");
                return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INVALID_PARAMS, message = "Invalid nonce was specified" } };
            }

            if(Pool.Pool.Instance.checkDuplicateShare(nonce))
            {
                lock (clientCallErrors)
                {
                    clientCallErrors[wallet].Add(DateTime.Now);
                }

                Logging.info("Received duplicate nonce from miner.");
                return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INVALID_PARAMS, message = "Duplicate nonce was specified" } };
            }

            ulong blocknum;
            if (!ulong.TryParse((string)parameters["blocknum"], out blocknum))
            {
                lock (clientCallErrors)
                {
                    clientCallErrors[wallet].Add(DateTime.Now);
                }

                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'blocknum' is not a number" };
                return new JsonResponse { result = null, error = error };
            }

            if(!Pool.Pool.Instance.isMinedBlock(blocknum))
            {
                lock (clientCallErrors)
                {
                    clientCallErrors[wallet].Add(DateTime.Now);
                }

                Logging.info("Received incorrect block number from miner.");
                return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INVALID_PARAMS, message = "Invalid block number specified" } };
            }

            RepositoryBlock block = node.getBlock(blocknum);
            if (block == null)
            {
                lock (clientCallErrors)
                {
                    clientCallErrors[wallet].Add(DateTime.Now);
                }

                Logging.info("Received incorrect block number from miner.");
                return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INVALID_PARAMS, message = "Invalid block number specified" } };
            }

            Miner miner = new Miner((string)parameters["wallet"]);

            if (!miner.isValid())
            {
                lock (clientCallErrors)
                {
                    clientCallErrors[wallet].Add(DateTime.Now);
                }

                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INTERNAL_ERROR, message = "Internal error while querying miner data." };
                return new JsonResponse { result = null, error = error };
            }

            Logging.info("Received miner share: {0} #{1}", nonce, blocknum);

            byte[] solver_address = IxianHandler.getWalletStorage().getPrimaryAddress();

            miner.selectWorker(id, worker);

            bool valid_share, verify_result = false;
            ulong difficulty = Pool.Pool.Instance.getDifficulty();
            valid_share = node.verifyNonce_v3(nonce, blocknum, solver_address, difficulty);

            if (valid_share)
            {
                if (!sharesStarted)
                {
                    sharesStarted = true;
                    shareCountTimeStamp = DateTime.Now;
                    shareCount = 0;
                }
                else
                {
                    double interval = (DateTime.Now - shareCountTimeStamp).TotalSeconds;
                    double shares = (++shareCount);
                    if (interval > 10)
                    {
                        Pool.Pool.Instance.updateSharesPerSecond(shares / interval);
                        shareCountTimeStamp = DateTime.Now;
                        shareCount = 0;
                    }
                }

                verify_result = node.verifyNonce_v3(nonce, blocknum, solver_address, block.difficulty);
                miner.addShare(blocknum, nonce, difficulty, verify_result);
                miner.commit();
            }
            else
            {
                lock (clientCallErrors)
                {
                    clientCallErrors[wallet].Add(DateTime.Now);
                }
            }

            // Solution is valid, try to submit it to network
            if (verify_result)
            {
                if (node.sendSolution(Crypto.stringToHash(nonce), blocknum))
                {
                    Logging.info("Miner share {0} ACCEPTED.", nonce);
                }
            }
            else
            {
                Logging.warn("Miner share {0} REJECTED.", nonce);
            }

            return new JsonResponse { result = valid_share, error = valid_share ? null : new JsonError() { code = (int)RPCErrorCode.RPC_VERIFY_REJECTED, message = "Invalid share" } };
        }
    }
}