﻿using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace OxGFrame.CenterFrame.APICenter
{
    public delegate void ResponseHandle(string response);

    public static class Http
    {
        /// <summary>
        /// Callback C# and Xml = Acax
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public static void Acax(string url, string method, string[,] headers, object[,] body, ResponseHandle success = null, ResponseHandle error = null)
        {
            method = method.ToUpper();
            RequestAPI(url, method, headers, body, success, error).Forget();
        }

        public static void Acax(string url, string method, string[,] headers, object body, ResponseHandle success = null, ResponseHandle error = null)
        {
            method = method.ToUpper();
            RequestAPI(url, method, headers, body, success, error).Forget();
        }

        /// <summary>
        /// Asynchronous with Callback C# and Xml = Acax
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public async static UniTask<string> AcaxAsync(string url, string method, string[,] headers, object[,] body, ResponseHandle success = null, ResponseHandle error = null)
        {
            method = method.ToUpper();
            return await RequestAPI(url, method, headers, body, success, error);
        }

        public async static UniTask<string> AcaxAsync(string url, string method, string[,] headers, object body, ResponseHandle success = null, ResponseHandle error = null)
        {
            method = method.ToUpper();
            return await RequestAPI(url, method, headers, body, success, error);
        }

        #region Internal Methods
        internal static async UniTask<string> RequestAPI(string url, string method, string[,] headers, object[,] body, ResponseHandle success, ResponseHandle error)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                // Header args
                if (headers != null && headers.Length > 0)
                {
                    for (int row = 0; row < headers.GetLength(0); row++)
                    {
                        if (headers.GetLength(1) != 2) continue;
                        request.SetRequestHeader(headers[row, 0], headers[row, 1]);
                    }
                }

                // Body args
                if (body != null && body.Length > 0)
                {
                    Dictionary<string, object> jsonArgs = new Dictionary<string, object>();
                    for (int row = 0; row < body.GetLength(0); row++)
                    {
                        if (body.GetLength(1) != 2) continue;
                        jsonArgs.Add((string)body[row, 0], body[row, 1]);
                    }
                    string json = JsonConvert.SerializeObject(jsonArgs);
                    byte[] jsonBinary = System.Text.Encoding.UTF8.GetBytes(json);
                    request.uploadHandler = new UploadHandlerRaw(jsonBinary);
                }

                // Response download buffer
                request.downloadHandler = new DownloadHandlerBuffer();

                // Start send request
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.DataProcessingError ||
                    request.result == UnityWebRequest.Result.ProtocolError ||
                    request.result == UnityWebRequest.Result.ConnectionError)
                {
                    if (error != null)
                        error(request.error);
                    return null;
                }
                else
                {
                    if (success != null)
                        success(request.downloadHandler.text);
                    return request.downloadHandler.text;
                }
            }
        }

        internal static async UniTask<string> RequestAPI(string url, string method, string[,] headers, object body, ResponseHandle success, ResponseHandle error)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                // Header args
                if (headers != null && headers.Length > 0)
                {
                    for (int row = 0; row < headers.GetLength(0); row++)
                    {
                        if (headers.GetLength(1) != 2) continue;
                        request.SetRequestHeader(headers[row, 0], headers[row, 1]);
                    }
                }

                // Body args
                if (body != null)
                {
                    string json = JsonConvert.SerializeObject(body);
                    byte[] jsonBinary = System.Text.Encoding.UTF8.GetBytes(json);
                    request.uploadHandler = new UploadHandlerRaw(jsonBinary);
                }

                // Response download buffer
                request.downloadHandler = new DownloadHandlerBuffer();

                // Start send request
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.DataProcessingError ||
                    request.result == UnityWebRequest.Result.ProtocolError ||
                    request.result == UnityWebRequest.Result.ConnectionError)
                {
                    if (error != null)
                        error(request.error);
                    return null;
                }
                else
                {
                    if (success != null)
                        success(request.downloadHandler.text);
                    return request.downloadHandler.text;
                }
            }
        }
        #endregion
    }
}