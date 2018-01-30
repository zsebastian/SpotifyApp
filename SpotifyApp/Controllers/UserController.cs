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
using Newtonsoft.Json;

namespace SpotifyApp.Controllers
{
    public class UserController : Controller
    {
    	string redirectUri = "http://localhost:5000/User/Authorized/";
    	SpotifyApi spotify;
    	ISessions sessions;

    	public UserController(SpotifyApi spotify, ISessions sessions)
		{
			this.spotify = spotify;
			this.sessions = sessions;
		}

        public IActionResult Index()
        {
			var uri = spotify.AuthorizeUri(redirectUri);
            return Redirect(uri);
        }

        public IActionResult Authorized(string state, string code, string error)
        {
        	if (error != null) return Error();

			var token = spotify.GetTokenAsync(redirectUri, code).Result;
			Response.Cookies.Append("session_token", 
					sessions.CreateSession(token.Content.access_token),
					new CookieOptions()
					{
						Path = "/",
						HttpOnly = false,
						Secure = false
					});
			return new RedirectResult("/Recommendations");
        }

		IEnumerable<SpotifyApi.AudioFeatures> GetAllAudioFeatures(IEnumerable<SpotifyApi.BrowsedPlaylist> playlist, string token)
		{
			return playlist.Select(p => 
				{
					var id = p.id;
					var owner = p.owner.uri.Split(':')[2];
					return spotify.GetPlaylistTracks(owner, id, token);
				}).SelectMany(t => t.Result.Content.tracks.items)
				.Select(t => t.track.id)
				.Batch(100)
				.Select(ids => spotify.GetAudioFeatures(ids.ToArray(), token))
				.SelectMany(af => af.Result.Content.audio_features)
				// Some audio features are null.
				.Where(af => af != null);
		}

		float Square(float v)
		{
			return v * v;
		}

		public IActionResult ShowRecommendations(string category,
				float acousticness,
				float danceability,
				float energy,
				float loudness,
				float liveness,
				float instrumentalness,
				float valence)
		{
			if (!Request.Cookies.ContainsKey("session_token"))
				return Error();
			var token = sessions.GetAccessToken(Request.Cookies["session_token"]);
			var playlists = spotify.BrowseAllCategoryPlaylistsAsync(category, token).Result;
			ViewData["Message"] = string.Join(", ", playlists.Select(c => c.name));
			var audioFeatures = GetAllAudioFeatures(playlists, token);
			var bestFit = audioFeatures.OrderBy(f => 
				Math.Sqrt(
					Square(f.acousticness - acousticness) +
					Square(f.danceability - danceability) +
					Square(f.acousticness - acousticness) +
					Square(f.energy - energy) +
					Square(f.loudness - loudness) +
					Square(f.liveness - liveness) +
					Square(f.instrumentalness - instrumentalness) +
					Square(f.valence - valence)))
				.Take(10)
				.Select(t => t.id)
				.ToArray();
			ViewData["Tracks"] = bestFit;

			return View();
		}

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
