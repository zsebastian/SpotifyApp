﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace SpotifyApp
{
    public static class Program
    {
    	public static Config config;
    	public static SpotifyApi spotify;

        public static void Main(string[] args)
        {
        	config = Config.From("config.json");
        	using (spotify = new SpotifyApi(config))
			{
				BuildWebHost(args).Run();
			}
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
