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

namespace SpotifyApp.Controllers
{
    public class UserController : Controller
    {
    	string redirectUri = "http://localhost:5000/User/Authorized/";
        public IActionResult Index()
        {
			var uri = Program.spotify.AuthorizeUri(redirectUri);
            return Redirect(uri);
        }

        string CreateSession(string accessToken)
		{
			var sessionToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			Program.cache.Set(string.Format("accessToken[{0}]", sessionToken), accessToken, TimeSpan.FromHours(3));
			return sessionToken;
		}

		string GetAccessToken(string sessionToken)
		{
			string ret;
			if (!Program.cache.TryGetValue(string.Format("accessToken[{0}]", sessionToken), out ret))
				ret = null;
			return ret;
		}

        public IActionResult Authorized(string state, string code, string error)
        {
        	if (error != null) return Error();

			var token = Program.spotify.GetTokenAsync(redirectUri, code).Result;
			Response.Cookies.Append("session_token", 
					CreateSession(token.Content.access_token),
					new CookieOptions()
					{
						Path = "/",
						HttpOnly = false,
						Secure = false
					});
			return new RedirectResult("/User/ConfigureRecommendations");
        }

        public IActionResult ConfigureRecommendations()
		{
			if (!Request.Cookies.ContainsKey("session_token"))
				return Error();

			var token = GetAccessToken(Request.Cookies["session_token"]);
			var categories = Program.spotify.BrowseAllCategoriesAsync(token).Result;
			ViewData["Message"] = string.Join(", ", categories.Select(c => c.name));
			ViewData["Categories"] = categories.Select(c => new KeyValuePair<string, string>(c.id, c.name));
			return View();
		}

		public IActionResult ShowRecommendations(string category)
		{
			if (!Request.Cookies.ContainsKey("session_token"))
				return Error();
			var token = GetAccessToken(Request.Cookies["session_token"]);
			var playlists = Program.spotify.BrowseAllCategoryPlaylistsAsync(category, token).Result;
			ViewData["Message"] = string.Join(", ", playlists.Select(c => c.name));
			foreach(var p in playlists)
			{
				var id = p.id;
				var owner = p.owner.uri.Split(':')[2];
				var tracks = Program.spotify.GetPlaylistTracks(owner, id, token).Result;
				Console.WriteLine("[{0}] {1}/{2}", p.name, owner, id);
				foreach(var t in tracks.Content.tracks.items)
				{
					Console.WriteLine("  {0}", t.track.name);
				}
			}

			return View();
		}

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
