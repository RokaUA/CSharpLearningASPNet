﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebSampleApp.Controllers;
using WebSampleApp.Middleware;
using WebSampleApp.Services;

namespace WebSampleApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISampleService, DefaultSampleService>();
            services.AddTransient<HomeController>();
            services.AddDistributedMemoryCache();
            services.AddSession(options => options.IdleTimeout = TimeSpan.FromMinutes(10));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseHeaderMiddleware();
            app.UseSession();

            app.Map("/Home", homeApp =>
            {
                homeApp.Run(async context =>
                {
                    HomeController controller = homeApp.ApplicationServices.GetRequiredService<HomeController>();
                    await controller.Index(context);
                });
            });

            app.MapWhen(contex => contex.Request.Path.Value.Contains("hello"), homeApp =>
            {
                homeApp.Run(async context => 
                {
                    await context.Response.WriteAsync("Hello in the path".Div());
                });
            });

            PathString remainingPath; // out var is not possible when used in lambda expression that follows with the next parameter
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/Configuration", out remainingPath), configurationApp =>
            {
                configurationApp.Run(async context =>
                {
                    var configSample = app.ApplicationServices.GetService<ConfigurationSample>();
                    if (remainingPath.StartsWithSegments("/appsettings"))
                    {
                        await configSample.ShowApplicationSettingsAsync(context);
                    }
                    else if (remainingPath.StartsWithSegments("/colons"))
                    {
                        await configSample.ShowApplicationSettingsUsingColonsAsync(context);
                    }
                    else if (remainingPath.StartsWithSegments("/database"))
                    {
                        await configSample.ShowConnectionStringSettingAsync(context);
                    }
                    else if (remainingPath.StartsWithSegments("/stronglytyped"))
                    {
                        await configSample.ShowApplicationSettingsStronglyTyped(context);
                    }
                });
            });

            app.Map("/RequestAndResponse", app1 =>
            {
                app1.Run(async context =>
                {
                    context.Response.ContentType = "text/html";
                    string result = string.Empty;

                    switch (context.Request.Path.Value.ToLower())
                    {
                        case "/header":
                            result = RequestAndResponse.GetHeaderInformation(context.Request);
                            break;
                        case "/add":
                            result = RequestAndResponse.QueryString(context.Request);
                            break;
                        case "/content":
                            result = RequestAndResponse.Content(context.Request);
                            break;
                        case "/encoded":
                            result = RequestAndResponse.ContentEncoded(context.Request);
                            break;
                        case "/form":
                            result = RequestAndResponse.GetForm(context.Request);
                            break;
                        case "/writecookie":
                            result = RequestAndResponse.WriteCookie(context.Response);
                            break;
                        case "/readcookie":
                            result = RequestAndResponse.ReadCookie(context.Request);
                            break;
                        case "/json":
                            result = RequestAndResponse.GetJson(context.Response);
                            break;
                        default:
                            result = RequestAndResponse.GetRequestInformation(context.Request);
                            break;
                    }

                    await context.Response.WriteAsync(result);
                });
            });

            app.Run(async (context) =>
            {
                // await context.Response.WriteAsync("Hello World!");

                string[] lines = new[]
                {
                    @"<ul>",
                      @"<li><a href=""/hello.html"">Static Files</a> - requires UseStaticFiles</li>",
                      @"<li>Request and Response",
                        @"<ul>",
                          @"<li><a href=""/RequestAndResponse"">Request and Response</a></li>",
                          @"<li><a href=""/RequestAndResponse/header"">Header</a></li>",
                          @"<li><a href=""/RequestAndResponse/add?x=38&y=4"">Add</a></li>",
                          @"<li><a href=""/RequestAndResponse/content?data=sample"">Content</a></li>",
                          @"<li><a href=""/RequestAndResponse/content?data=<h1>Heading 1</h1>"">HTML Content</a></li>",
                          @"<li><a href=""/RequestAndResponse/content?data=<script>alert('hacker');</script>"">Bad Content</a></li>",
                          @"<li><a href=""/RequestAndResponse/encoded?data=<h1>sample</h1>"">Encoded content</a></li>",
                          @"<li><a href=""/RequestAndResponse/encoded?data=<script>alert('hacker');</script>"">Encoded bad Content</a></li>",
                          @"<li><a href=""/RequestAndResponse/form"">Form</a></li>",
                          @"<li><a href=""/RequestAndResponse/writecookie"">Write cookie</a></li>",
                          @"<li><a href=""/RequestAndResponse/readcookie"">Read cookie</a></li>",
                          @"<li><a href=""/RequestAndResponse/json"">JSON</a></li>",
                        @"</ul>",
                      @"</li>",
                      @"<li><a href=""/Home"">Home Controller with dependency injection</a></li>",
                      @"<li><a href=""/abc/xyz/42hello42/foobar"">MapWhen with hello in the URL</a></li>",
                      @"<li><a href=""/Session"">Session</a></li>",
                      @"<li>Configuration",
                        @"<ul>",
                          @"<li><a href=""/Configuration/appsettings"">Appsettings</a></li>",
                          @"<li><a href=""/Configuration/colons"">Using Colons</a></li>",
                          @"<li><a href=""/Configuration/database"">Database</a></li>",
                          @"<li><a href=""/Configuration/stronglytyped"">Strongly Typed</a></li>",
                        @"</ul>",
                      @"</li>",
                    @"</ul>"
                };

                var sb = new StringBuilder();
                foreach (var line in lines)
                {
                    sb.Append(line);
                }
                string html = sb.ToString().HtmlDocument("Web Sample App");

                await context.Response.WriteAsync(html);
            });
        }
    }
}
