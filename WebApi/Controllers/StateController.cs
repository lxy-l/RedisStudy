using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            return "进程名字是："+processName;
        }
    }
}
