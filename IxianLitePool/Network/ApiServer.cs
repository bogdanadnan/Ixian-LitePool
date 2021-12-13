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

namespace LP.Network
{
    class APIServer : GenericAPIServer
    {
        private Node node = null;
        public APIServer(Node node, List<string> listen_URLs, Dictionary<string, string> authorized_users = null, List<string> allowed_IPs = null)
        {
            this.node = node;
            
            Console.WriteLine("Listening on {0} urls {1}", listen_URLs.Count, listen_URLs.Count > 0 ? listen_URLs[0] : "");
            // Start the API server
            start(listen_URLs, authorized_users, allowed_IPs);
        }

        protected override void onUpdate(HttpListenerContext context)
        {
            try
            {
                if (ConsoleHelpers.verboseConsoleOutput)
                    Console.Write("*");


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
                    if (context.Request.Url.Segments.Length < 2)
                    {
                        context.Response.ContentType = "application/json";
                        JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_REQUEST, message = "Unknown action." };
                        sendResponse(context.Response, new JsonResponse { error = error });
                        return;
                    }

                    method_name = context.Request.Url.Segments[1].Replace("/", "");
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

            if (methodName.Equals("getbalance", StringComparison.OrdinalIgnoreCase))
            {
                response = onGetBalance(parameters);
            }

            if (methodName.Equals("verifyminingsolution", StringComparison.OrdinalIgnoreCase))
            {
                response = onVerifyMiningSolution(parameters);
            }

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

        public JsonResponse onGetBalance(Dictionary<string, object> parameters)
        {
            if (!parameters.ContainsKey("address"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'address' is missing" };
                return new JsonResponse { result = null, error = error };
            }

            byte[] address = Base58Check.Base58CheckEncoding.DecodePlain((string)parameters["address"]);

            IxiNumber balance = node.getWalletBalance(address);

            return new JsonResponse { result = balance.ToString(), error = null };
        }
        
        // Verifies a mining solution based on the block's difficulty
        // It does not submit it to the network.
        private JsonResponse onVerifyMiningSolution(Dictionary<string, object> parameters)
        {
            // Check that all the required query parameters are sent
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

            if (!parameters.ContainsKey("diff"))
            {
                JsonError error = new JsonError { code = (int)RPCErrorCode.RPC_INVALID_PARAMETER, message = "Parameter 'diff' is missing" };
                return new JsonResponse { result = null, error = error };
            }

            string nonce = (string)parameters["nonce"];
            if (nonce.Length < 1 || nonce.Length > 128)
            {
                Logging.info("Received incorrect verify nonce from miner.");
                return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INVALID_PARAMS, message = "Invalid nonce was specified" } };
            }

            ulong blocknum = ulong.Parse((string)parameters["blocknum"]);
            PoolBlock block = node.getBlock(blocknum);
            if (block == null)
            {
                Logging.info("Received incorrect verify block number from miner.");
                return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INVALID_PARAMS, message = "Invalid block number specified" } };
            }

            ulong blockdiff = ulong.Parse((string)parameters["diff"]);

            byte[] solver_address = IxianHandler.getWalletStorage().getPrimaryAddress();

            bool verify_result = node.verifyNonce_v3(nonce, blocknum, solver_address, blockdiff);

            if (verify_result)
            {
                Logging.info("Received verify share: {0} #{1} - PASSED with diff {2}", nonce, blocknum, blockdiff);
            }
            else
            {
                Logging.info("Received verify share: {0} #{1} - REJECTED with diff {2}", nonce, blocknum, blockdiff);
            }

            return new JsonResponse { result = verify_result, error = null };
        }

        // Verifies and submits a mining solution to the network
        private JsonResponse onSubmitMiningSolution(Dictionary<string, object> parameters)
        {
            // Check that all the required query parameters are sent
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


            string nonce = (string)parameters["nonce"];
            if (nonce.Length < 1 || nonce.Length > 128)
            {
                Logging.info("Received incorrect nonce from miner.");
                return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INVALID_PARAMS, message = "Invalid nonce was specified" } };
            }

            ulong blocknum = ulong.Parse((string)parameters["blocknum"]);
            PoolBlock block = node.getBlock(blocknum);
            if (block == null)
            {
                Logging.info("Received incorrect block number from miner.");
                return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INVALID_PARAMS, message = "Invalid block number specified" } };
            }

            Logging.info("Received miner share: {0} #{1}", nonce, blocknum);

            byte[] solver_address = IxianHandler.getWalletStorage().getPrimaryAddress();
            bool verify_result = node.verifyNonce_v3(nonce, blocknum, solver_address, block.difficulty);

            bool send_result = false;

            // Solution is valid, try to submit it to network
            if (verify_result == true)
            {
                if (node.sendSolution(Crypto.stringToHash(nonce), blocknum))
                {
                    Logging.info("Miner share {0} ACCEPTED.", nonce);
                    send_result = true;
                }
            }
            else
            {
                Logging.warn("Miner share {0} REJECTED.", nonce);
            }

            return new JsonResponse { result = send_result, error = null };
        }

        // Returns an empty PoW block based on the search algorithm provided as a parameter
        private JsonResponse onGetMiningBlock(Dictionary<string, object> parameters)
        {
            PoolBlock block = node.getMiningBlock();
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

            Dictionary<string, Object> resultArray = new Dictionary<string, Object>
            {
                {"num", block.blockNum}, // Block number
                {"ver", block.version}, // Block version
                {"dif", block.difficulty}, // Block difficulty
                {"chk", block.blockChecksum}, // Block checksum
                {"adr", solver_address} // Solver address
            };

            return new JsonResponse {result = resultArray, error = null};
        }
    }
}