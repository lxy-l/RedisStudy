using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Tools;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StateController : ControllerBase
    {

        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        public StateController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }
        /// <summary>
        /// 获取进程名称
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get()
        {
            var json = new
            {
                processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName,
                redis = _configuration.GetConnectionString("RedisConnection"),
                sqlserver = _configuration.GetConnectionString("DefaultConnection"),
                evnname = _env.EnvironmentName

            };

            return Ok(json);
        }
    }
}
