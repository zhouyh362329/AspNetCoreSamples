using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HelloWorldWeb
{
    public class Startup 
    {   
        public Startup(ILoggerFactory loggerFactory) 
        {
            loggerFactory.MinimumLevel = LogLevel.Error;
            loggerFactory.AddConsole();
        }
        
        public void ConfigureServices(IServiceCollection services) 
        {
            services.AddMvc();
        }
        
        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<RequestIdMiddleware>()
               .UseMiddleware<MachineNameMiddleware>()
               .UseMvc()
               .Run(async ctx => 
               {
                   ctx.Response.StatusCode = 200;
                   ctx.Response.Headers["Content-Type"] = "text/html";
                   StringBuilder builder = new StringBuilder();
                   builder.AppendLine($"<div>Hello from {Helper.GetMachineName()}! Here is my context:</div>");
                   builder.AppendLine("<hr/>");
                   foreach(DictionaryEntry envVar in System.Environment.GetEnvironmentVariables())
                   {
                       builder.AppendLine(envVar.Key  + ":" + envVar.Value);
                   }
                   await ctx.Response.WriteAsync($"<pre>{builder.ToString()}</pre>");
               });
        }
        
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
    
    public static class Helper 
    {
        public static string GetMachineName() 
        {
#if DNX451
            var machineName = Environment.MachineName;
#else  
            var machineName = Environment.GetEnvironmentVariable("HOSTNAME");
            if(machineName == null) 
            {
                machineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            }
#endif
            
            return machineName;
        }
    }
    
    public class RequestIdMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers["RequestId"] = context.TraceIdentifier;
            await _next(context);
        }        
    }
    
    public class MachineNameMiddleware
    {
        private readonly RequestDelegate _next;

        public MachineNameMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers["MachineName"] = Helper.GetMachineName();
            await _next(context);
        }
    }
    
    public class Car
    {
        public int Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Colour { get; set; }
    }
    
    [Route("cars")]
    public class CarsController : Controller 
    {
        private static IEnumerable<Car> _cars = new[]
        {
            new Car { Id = 1, Make = "Renault", Model = "Clio", Colour = "Red" },
            new Car { Id = 2, Make = "Mercedes", Model = "GLA", Colour = "Black" },
            new Car { Id = 4, Make = "Renault", Model = "Captur", Colour = "White" },
            new Car { Id = 5, Make = "Volkswagen", Model = "Polo", Colour = "Black" },
            new Car { Id = 3, Make = "Mercedes", Model = "CLS Coupe", Colour = "Yellow" },
            new Car { Id = 6, Make = "Toyota", Model = "Yaris", Colour = "Orange" }
        };
           
        [Route("")]
        public IEnumerable<Car> GetCars()
        {
            return _cars;
        }
        
        [Route("{carId}")]
        public IActionResult GetCars(int carId)
        {
            IActionResult result;
            var car = _cars.FirstOrDefault(c => c.Id == carId);
            if(car != null)
            {
                result = Ok(car);      
            }
            else 
            {
                result = HttpNotFound();
            }
            
            return result;
        }
    }
}