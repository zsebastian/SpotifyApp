using System;
using System.Web;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SpotifyApp.Models;
using Microsoft.Extensions.Caching.Memory;

namespace SpotifyApp
{
	public interface ISessions
	{
		string GetAccessToken(string sessionToken);
		string CreateSession(string accessToken);
	}

	public class InMemorySessions: ISessions
	{
    	IMemoryCache cache;

    	public InMemorySessions(IMemoryCache cache)
		{
			this.cache = cache;
		}

		public string GetAccessToken(string sessionToken)
		{
			string ret;
			if (!cache.TryGetValue(string.Format("accessToken[{0}]", sessionToken), out ret))
				ret = null;
			return ret;
		}

        public string CreateSession(string accessToken)
		{
			var sessionToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			cache.Set(string.Format("accessToken[{0}]", sessionToken), accessToken, TimeSpan.FromHours(3));
			return sessionToken;
		}
	}
}
