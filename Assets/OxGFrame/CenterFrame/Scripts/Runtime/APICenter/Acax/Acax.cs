﻿using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using OxGKit.LoggingSystem;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Networking;

namespace OxGFrame.CenterFrame.APICenter
{
    /// <summary>
    /// Use UnityWebRequest
    /// </summary>
    public static class Http
    {
        /// <summary>
        /// Default request timeout in seconds
        /// </summary>
        private const int _MAX_REQUEST_TIME_SECONDS = 60;

        /// <summary>
        /// Callback C# and Xml = Acax
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        /// <param name="timeoutSeconds"></param>
        public static void Acax(string url, string method, string[,] headers, object[,] body, ResponseHandle success = null, ResponseErrorHandle error = null, int? timeoutSeconds = null)
        {
            method = method.ToUpper();
            RequestAPI(url, method, headers, body, success, error, timeoutSeconds).Forget();
        }

        public static void Acax(string url, string method, string[,] headers, object body, ResponseHandle success = null, ResponseErrorHandle error = null, int? timeoutSeconds = null)
        {
            method = method.ToUpper();
            RequestAPI(url, method, headers, body, success, error, timeoutSeconds).Forget();
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
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public async static UniTask<string> AcaxAsync(string url, string method, string[,] headers, object[,] body, ResponseHandle success = null, ResponseErrorHandle error = null, int? timeoutSeconds = null)
        {
            method = method.ToUpper();
            return await RequestAPI(url, method, headers, body, success, error, timeoutSeconds);
        }

        public async static UniTask<string> AcaxAsync(string url, string method, string[,] headers, object body, ResponseHandle success = null, ResponseErrorHandle error = null, int? timeoutSeconds = null)
        {
            method = method.ToUpper();
            return await RequestAPI(url, method, headers, body, success, error, timeoutSeconds);
        }

        #region Internal Methods
        internal static async UniTask<string> RequestAPI(string url, string method, string[,] headers, object body, ResponseHandle success, ResponseErrorHandle error, int? timeoutSeconds)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                try
                {
                    // Header args
                    if (headers != null && headers.Length > 0)
                    {
                        for (int row = 0; row < headers.GetLength(0); row++)
                        {
                            if (headers.GetLength(1) != 2)
                                continue;

                            string headerName = headers[row, 0];
                            string headerValue = headers[row, 1];
                            request.SetRequestHeader(headerName, headerValue);
                        }
                    }

                    // Body args
                    if (body != null)
                    {
                        string json = null;
                        if (body is object[,] bodyArray)
                        {
                            if (bodyArray.Length > 0)
                            {
                                Dictionary<string, object> jsonArgs = new Dictionary<string, object>();
                                for (int row = 0; row < bodyArray.GetLength(0); row++)
                                {
                                    if (bodyArray.GetLength(1) != 2)
                                        continue;
                                    jsonArgs.Add((string)bodyArray[row, 0], bodyArray[row, 1]);
                                }
                                json = JsonConvert.SerializeObject(jsonArgs);
                            }
                        }
                        else
                        {
                            json = JsonConvert.SerializeObject(body);
                        }

                        if (!string.IsNullOrEmpty(json))
                        {
                            byte[] jsonBinary = System.Text.Encoding.UTF8.GetBytes(json);
                            request.uploadHandler = new UploadHandlerRaw(jsonBinary);
                        }
                    }

                    // Response download buffer
                    request.downloadHandler = new DownloadHandlerBuffer();

                    timeoutSeconds ??= _MAX_REQUEST_TIME_SECONDS;
                    var cts = new CancellationTokenSource();
                    cts.CancelAfterSlim(TimeSpan.FromSeconds((int)timeoutSeconds));

                    // Start send request
                    await request.SendWebRequest().WithCancellation(cts.Token);

                    if (request.result == UnityWebRequest.Result.DataProcessingError ||
                        request.result == UnityWebRequest.Result.ProtocolError ||
                        request.result == UnityWebRequest.Result.ConnectionError)
                    {
                        var errorInfo = new ErrorInfo();
                        errorInfo.url = url;
                        errorInfo.message = request.error;
                        errorInfo.exception = null;
                        error?.Invoke(errorInfo);
                        Logging.PrintError<Logger>($"RequestAPI failed. URL: {errorInfo.url}, ErrorMsg: {errorInfo.message}");
                        return null;
                    }
                    else
                    {
                        success?.Invoke(request.downloadHandler.text);
                        return request.downloadHandler.text;
                    }
                }
                catch (Exception ex)
                {
                    var errorInfo = new ErrorInfo();
                    errorInfo.url = url;
                    errorInfo.message = request?.error;
                    errorInfo.exception = ex;
                    error?.Invoke(errorInfo);
                    Logging.PrintError<Logger>($"RequestAPI failed. URL: {errorInfo.url}, ErrorMsg: {errorInfo.message}, Exception: {ex}");
                    return null;
                }
            }
        }
        #endregion
    }
}