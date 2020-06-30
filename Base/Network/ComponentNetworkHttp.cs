using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ETModel
{
    // 外网连接
    public class ComponentNetworkHttp : ComponentNetwork
    {
        public override void Awake(JToken jd = null)
        {
            base.Awake(jd);
            //TestRun();
        }

        public static async Task<HttpMessage> Query(string url, HttpMessage request)
        {
            string jsonModel = JsonHelper.ToJson(request.map);
            var wc = new WebClient();
            //发送到服务端并获得返回值
            var returnInfo = await wc.UploadDataTaskAsync(url, System.Text.Encoding.UTF8.GetBytes(jsonModel));
            //把服务端返回的信息转成字符串
            var str = System.Text.Encoding.UTF8.GetString(returnInfo);
            HttpMessage response = new HttpMessage();
            response.map = JsonHelper.FromJson<Dictionary<string,string>>(str);
            return response;
        }

        //public async void TestRun()
        //{
        //    await Task.Delay(3000);

        //    HttpMessage request = new HttpMessage() ;
        //    request.map = new Dictionary<string, string>();
        //    request.map.Add("cmd123", "cmd123");
        //    request.map.Add("test123", "http请求测试");

        //    HttpMessage httpMessage = await Query("http://127.0.0.1:8089/", request);
        //    Log.Info(JsonHelper.ToJson(httpMessage.map));
        //}

        //[MessageMethod(NetOpcodeBase.HttpMessage)]
        //public static void OnHttpMessage(Session session, int opcode, object msg)
        //{
        //    HttpMessage httpMessage = msg as HttpMessage;

        //    Dictionary<string, string> map = httpMessage.map;
        //    map.Remove("cmd");
        //    map.Remove("test456");
        //    map.Remove("result");
        //    map.Add("cmd", "HttpMessage");
        //    map.Add("test456", "http返回测试");
        //    map.Add("result", "Success");

        //    httpMessage.result = JsonHelper.ToJson(map);
        //}

    }


}