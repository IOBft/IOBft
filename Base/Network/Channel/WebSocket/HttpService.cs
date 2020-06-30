﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace ETModel
{
    public class HttpMessage : IMessage
    {
        public Dictionary<string, string>  map;
        public string               result;  // 返回的消息
        public HttpListenerRequest  request;
        public HttpListenerResponse response;
    }

    public class HttpService : AService
    {
        private readonly HttpListener httpListener;
        public  override  IPEndPoint GetEndPoint() { return NetworkHelper.ToIPEndPoint(prefix.ToLower().Replace("http://", "").Replace("/", "")); }
        public  readonly string prefix;

        public HttpService(string _prefix, Action<AChannel> acceptCallback)
        {
            this.AcceptCallback += acceptCallback;
            
            this.httpListener = new HttpListener();

            this.prefix = _prefix;
            StartAccept(this.prefix);
        }
        
        public HttpService()
        {
        }
        
        public override AChannel GetChannel(long id)
        {
            return null;
        }

        public override AChannel ConnectChannel(IPEndPoint ipEndPoint)
        {
            throw new NotImplementedException();
        }

        public override AChannel ConnectChannel(string address)
        {
            return null;
        }

        public override void Remove(long id)
        {
        }

        public override void Update()
        {            
        }

        public void StartAccept(string prefix)
        {
            try
            {
                this.httpListener.Prefixes.Add(prefix);
                httpListener.Start();
                httpListener.BeginGetContext(Result, null);

            }
            catch (HttpListenerException e)
            {
                Log.Debug(e.ToString());
            }
        }

        private void Result(IAsyncResult ar)
        {
            try
            {
                //当接收到请求后程序流会走到这里
                //继续异步监听
                httpListener.BeginGetContext(Result, null);
                var guid = Guid.NewGuid().ToString();
                //Console.ForegroundColor = ConsoleColor.White;
                //Console.WriteLine($"接到新的请求:{guid},时间：{DateTime.Now.ToString()}");
                //获得context对象
                var context = httpListener.EndGetContext(ar);
                var request = context.Request;
                var response = context.Response;
                //如果是js的ajax请求，还可以设置跨域的ip地址与参数
                context.Response.AppendHeader("Access-Control-Allow-Origin", "*");//后台跨域请求，通常设置为配置文件
                context.Response.AppendHeader("Access-Control-Allow-Headers", "Content-Type, Content-Length, Authorization, Accept, X-Requested-With , yourHeaderFeild");//后台跨域参数设置，通常设置为配置文件
                context.Response.AppendHeader("Access-Control-Allow-Method", "OPTIONS,POST,GET");//后台跨域请求设置，通常设置为配置文件
                context.Response.ContentType = "text/plain;charset=UTF-8";//告诉客户端返回的ContentType类型为纯文本格式，编码为UTF-8
                context.Response.AddHeader("Content-type", "text/plain");//添加响应头信息
                context.Response.ContentEncoding = Encoding.UTF8;
                string returnObj = null;//定义返回客户端的信息
                if (request.HttpMethod == "POST" && request.InputStream != null)
                {
                    //处理客户端发送的请求并返回处理信息
                    returnObj = OnPost(request, response);
                }
                else
                {
                    returnObj = OnGet(request, response);
                }
                var returnByteArr = Encoding.UTF8.GetBytes(returnObj);//设置客户端返回信息的编码
                using (var stream = response.OutputStream)
                {
                    //把处理信息返回到客户端
                    stream.Write(returnByteArr, 0, returnByteArr.Length);
                }
            }
            catch (Exception )
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine($"网络蹦了：{ex.ToString()}");
            }
        }

        private string OnGet(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                int padIndex1 = request.RawUrl.IndexOf("?");
                if (padIndex1 != -1&& request.RawUrl.IndexOf(".html?")==-1)
                {
                    string cmd = request.RawUrl.Substring(1, padIndex1 - 1);
                    string data = request.RawUrl.Substring(padIndex1 + 1, request.RawUrl.Length - (padIndex1 + 1));

                    string[] array = data.Split('&');
                    Dictionary<string, string> map = new Dictionary<string, string>();
                    map.Add("cmd", cmd);

                    for (int ii = 0; ii < array.Length; ii++)
                    {
                        string[] arrayValue = array[ii].Split('=');
                        if (arrayValue != null && arrayValue.Length >= 1)
                        {
                            map.Remove(arrayValue[0]);
                            map.Add(arrayValue[0], arrayValue.Length >= 2 ? arrayValue[1] : "");
                        }
                    }

                    return OnMessage(map, request, response);
                }
                else // http服务
                {
                    //string httpheader = "HTTP/1.1 200 OK\n" +
                    //                    "Server: Microsoft-IIS/5.1\n" +
                    //                    "X-Powered-By: ASP.NET\n" +
                    //                    "Date: Fri, 03 Mar 2020 00:00:00 GMT\n" +
                    //                    "Content-Type: text/html\n" +
                    //                    "Accept-Ranges: bytes\n" +
                    //                    "Last-Modified: Fri, 03 Mar 2020 00:00:00 GMT\n" +
                    //                    "ETag: \"5ca4f75b8c3ec61:9ee\"\n" +
                    //                    "Content-Length: 37\n\n";

                    if (request.RawUrl.IndexOf(".css") != -1)
                        response.ContentType = "text/css";
                    else
                    if (request.RawUrl.IndexOf(".js") != -1)
                        response.ContentType = "text/javascript";
                    else
                        response.ContentType = "text/html";

                    string RawUrl = request.RawUrl.Replace("\\","/");
                    if (RawUrl == "/")
                        RawUrl = "/index.html";
                    if (RawUrl.IndexOf(".html?") != -1)
                        RawUrl = RawUrl.Split('?')[0];

                    if(File.Exists("./wwwroot" + RawUrl))
                        return File.ReadAllText( "./wwwroot" + RawUrl);
                    else
                    if (File.Exists("./wwwroot" + RawUrl+".html"))
                        return File.ReadAllText("./wwwroot" + RawUrl + ".html");

                    return "";
                }
            }
            catch (Exception ex)
            {
                return $"在接收数据时发生错误:{ex.ToString()}";
           }
        }

        private string OnPost(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var byteList = new List<byte>();
                var byteArr = new byte[2048];
                int readLen = 0;
                int len = 0;
                //接收客户端传过来的数据并转成字符串类型
                do
                {
                    readLen = request.InputStream.Read(byteArr, 0, byteArr.Length);
                    len += readLen;
                    byteList.AddRange(byteArr);
                } while (readLen != 0);
                string data = Encoding.UTF8.GetString(byteList.ToArray(), 0, len);
                Dictionary<string, string> map = JsonHelper.FromJson<Dictionary<string, string>>(data);                
                response.StatusDescription = "200";//获取或设置返回给客户端的 HTTP 状态代码的文本说明。
                response.StatusCode = 200;// 获取或设置返回给客户端的 HTTP 状态代码。
                return OnMessage(map, request,  response);
            }
            catch (Exception ex)
            {
                response.StatusDescription = "404";
                response.StatusCode = 404;
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine($"在接收数据时发生错误:{ex.ToString()}");
                return $"在接收数据时发生错误:{ex.ToString()}";
            }
        }

        private string OnMessage(Dictionary<string, string> map, HttpListenerRequest request, HttpListenerResponse response)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            HttpMessage message = new HttpMessage();
            message.map = map;
            message.result = "{\"ret\":\"failed\"}";
            message.request  = request;
            message.response = response;
            componentNetMsg.HandleMsg(null, NetOpcodeBase.HttpMessage, message);
            return message.result;
        }

    }

}