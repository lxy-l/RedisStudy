using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tools;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StateController : ControllerBase
    {
        /// <summary>
        /// 获取进程名称
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string Get()
        {
       
            var key= EncyptHelper.CreateKey();
            string txt = "asdfasdfasdfasdf";
            string content = EncyptHelper.RSAEncrypt(txt,key.publicKey);
            string value3 = EncyptHelper.RSADecrypt(content, key.privateKey);

            var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            return "进程名字是："+processName;
        }
    }
}
