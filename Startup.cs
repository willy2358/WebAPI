﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspAPIs
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(
                options =>
                {
                    options.SslPort = 44321;
                    options.Filters.Add(new RequireHttpsAttribute());
                }
            ).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            // services.AddCors( options => {
            //     options.AddPolicy("AllowSpecificOrigin", builder => 
            //     { 
            //         // "https://192.168.1.13:8080", "http://192.168.1.13:8080",
            //         builder.WithOrigins( 
            //         "http://192.168.1.6:8080", "https://192.168.1.6:8080", 
            //         "http://localhost:8080", "https://localhost:8080"); 
            //     }); 
            // });
            services.AddAntiforgery(
                options =>
                {
                    options.Cookie.Name = "_af";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.HeaderName = "X-XSRF-TOKEN";
                }
            );

            // TODO 需设置合适的CORS策略
            var corsBuilder = new CorsPolicyBuilder();
            corsBuilder.AllowAnyHeader();
            corsBuilder.AllowAnyMethod();
            corsBuilder.AllowAnyOrigin();
            //The CORS protocol does not allow specifying a wildcard (any) origin 
            //and credentials at the same time. Configure the policy by listing individ
            // corsBuilder.AllowCredentials();

            services.AddCors( options => {
                options.AddPolicy("AllowAnyOrigin", corsBuilder.Build());   
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
            app.UseCors("AllowAnyOrigin"); 
            app.UseStaticFiles();  //support js, css, images
        }
    }
}
