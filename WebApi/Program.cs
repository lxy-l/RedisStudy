using Microsoft.EntityFrameworkCore;

using StackExchange.Redis;

using Tools;

using WebApi.Data;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);
//builder.Environment
//var Configuration = new ConfigurationBuilder()
//                .SetBasePath(Directory.GetCurrentDirectory())
//                .AddJsonFile("appsettings.json")
//                .Build();
//var configuration = new ConfigurationBuilder()
//               .SetBasePath(env.ContentRootPath)
//               .AddJsonFile("appsettings.json", true, true)
//               .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);
var basePath = AppDomain.CurrentDomain.BaseDirectory;
var config = new ConfigurationOptions
{
    AbortOnConnectFail = false,
    AllowAdmin = true,
    ConnectTimeout = 15000,
    SyncTimeout = 5000,
    EndPoints = { builder.Configuration.GetConnectionString("RedisConnection") }
};
var str = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<JBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<JBContext>();
builder.Services.AddScoped<IJBService, JBService>();
builder.Services.AddSingleton(new Redis(config));
builder.Services.AddHostedService<Queue>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WebApi", Version = "v1" });
    c.OrderActionsBy(o => o.RelativePath);
    var xmlPath = Path.Combine(basePath, "WebApi.xml");
    c.IncludeXmlComments(xmlPath, true);
});
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.RoutePrefix = ""; c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restful_WebApi v1"); });
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();