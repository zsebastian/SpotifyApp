using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace SpotifyApp
{
	public class SpotifyApi: IDisposable
	{
		public interface IAuthenticator
		{
			void Authenticate(HttpClient client, HttpRequestMessage message);
		}

		public class AccessTokenAuthenticator: IAuthenticator
		{
			string token;

			public AccessTokenAuthenticator(string token)
			{
				this.token = token;
			}

			public void Authenticate(HttpClient client, HttpRequestMessage message)
			{
				message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}
		}

		public struct Response<T>
		{
			public HttpStatusCode StatusCode;
			public string Reason;
			public T Content;
			public string RawContent;
			public HttpHeaders Headers;
		}
		readonly Config config;
		readonly string authorizationHeader;
		readonly string baseUri = "https://api.spotify.com/v1";

		public SpotifyApi(Config config)
		{
			this.config = config;
			authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", 
					config.ClientId, config.ClientSecret)));
		}

		public string AuthorizeUri(string redirectUri)
		{
			return Uri.EscapeUriString(string.Format("{0}/?client_id={1}&response_type=code&redirect_uri={2}",
				"https://accounts.spotify.com/authorize",
				config.ClientId,
				redirectUri));
		}

        public struct Token
		{
			public string error;
			public string error_description;
			public string access_token;
			public string token_type;
			public int expires_in; 
			public string refresh_token;
		}

		public async Task<Response<Token>> GetTokenAsync(string redirectUri, string code)
		{
			using (WebClient wc = new WebClient())
            {
                var parameters = new NameValueCollection
                {
                    {"grant_type", "authorization_code"},
                    {"redirect_uri", redirectUri},
                    {"code", code},
                    {"client_id", config.ClientId},
                    {"client_secret", config.ClientSecret}
                };

                Response<Token> ret = new Response<Token>();
                try
                {
                	var dataTask = new TaskCompletionSource<byte[]>();
                	wc.UploadValuesCompleted += (_, e) => 
					{
						if (e.Cancelled)
							dataTask.TrySetCanceled();
						else if (e.Error != null)
							dataTask.TrySetException(e.Error);
						else 
							dataTask.TrySetResult(e.Result);
					};

                    wc.UploadValuesAsync(new Uri("https://accounts.spotify.com/api/token"), "POST", parameters);
                    var data = await dataTask.Task;
                    var str = Encoding.UTF8.GetString(data);
					ret.Content = JsonConvert.DeserializeObject<Token>(str);
					ret.RawContent = str;
					ret.StatusCode = HttpStatusCode.OK;
					ret.Reason = "OK";
                }
                catch (WebException e)
                {
                	var res = (HttpWebResponse)e.Response;
                	ret.StatusCode = res.StatusCode;
                    using (StreamReader reader = new StreamReader(res.GetResponseStream()))
                    {
                    	ret.RawContent = reader.ReadToEnd();
                    	try
						{
							ret.Content = JsonConvert.DeserializeObject<Token>(ret.RawContent);
						}
						catch(JsonSerializationException se)
						{
							// Just means the string was not valid Json. But we don't expect it to be
							// since we are handling an error anyway.
							Console.WriteLine(se);
						}
                    }
                }
                return ret;
			}
		}
		
		public struct Icon
		{
			public int height, width;
			public string url;
		}

		public struct Category
		{
			public string href;
			public Icon[] icons; 
			public string id;
			public string name;
		}

		public struct Categories
		{
			public struct Inner
			{
				public string href;
				public Category[] items; 
				public int limit;
				public int offset;
				public int total;
				public string next;
				public string prev;
			}
			public Inner categories;
		}

      	public struct Tracks
		{
			public string href;
			public int total;
		}

      	public struct User
		{
			public Dictionary<string, string> external_urls;
			public string href;
			public string type;
			public string uri;
		}
      	public struct Image
		{
			public int height;
			public int width;
			public string url;
		}

		public struct BrowsedPlaylist
		{
			public bool collaborative;
			public Dictionary<string, string> external_urls;
			public string href;
			public string id;
			public Image[] images;
			public string name;
			public User owner;
			// uh oh.
			// public bool public;
			public string snapshot_id;
			public Tracks tracks;
			public string type;
			public string uri;
		}

		public struct BrowsedPlaylists
		{
			public struct Inner
			{
				public string href;
				public BrowsedPlaylist[] items; 
				public int limit;
				public int offset;
				public int total;
				public string next;
				public string prev;
			}

			public Inner playlists;
		}

		public struct Track
		{
			public string name;
		}

		public struct PlaylistTrack
		{
			public DateTime added_at;
			public User added_by;
			public bool is_local;
			public Track track;
		}

		public struct Playlist
		{
			public struct Inner
			{
				public string href;
				public PlaylistTrack[] items; 
				public int limit;
				public int offset;
				public int total;
				public string next;
				public string prev;
			}

			public Inner tracks;
		}

		public async Task<Category[]> BrowseAllCategoriesAsync(string token)
		{
			List<Category> categories = new List<Category>();
			int offset = 0;
			int limit = 10;
			var next = await BrowseCategoriesAsync(offset, limit, token);

			while(next.Content.categories.next != null)
			{
				// if categories.next != null we know we can continue.
				// It is set to the url they want us to call, but this
				// will work as well.
				offset += limit;
				categories.AddRange(next.Content.categories.items);
				next = await BrowseCategoriesAsync(offset, limit, token);
			}
			if (next.Content.categories.items != null)
				categories.AddRange(next.Content.categories.items);

			return categories.ToArray();
		}

		public async Task<Response<Categories>> BrowseCategoriesAsync(int offset, int limit, string token)
		{
			return await GetAsync<Categories>(string.Format("/browse/categories?offset={0}&limit={1}", offset, limit),
					new AccessTokenAuthenticator(token));
		}

		public async Task<BrowsedPlaylist[]> BrowseAllCategoryPlaylistsAsync(string categoryId, string token)
		{
			List<BrowsedPlaylist> playlists = new List<BrowsedPlaylist>();
			int offset = 0;
			int limit = 10;
			var next = await BrowseCategoryPlaylistsAsync(categoryId, offset, limit, token);

			while(next.Content.playlists.next != null)
			{
				// if playlists.next != null we know we can continue.
				// It is set to the url they want us to call, but this
				// will work as well.
				offset += limit;
				playlists.AddRange(next.Content.playlists.items);
				next = await BrowseCategoryPlaylistsAsync(categoryId, offset, limit, token);
			}
			if (next.Content.playlists.items != null)
				playlists.AddRange(next.Content.playlists.items);

			return playlists.ToArray();
		}

		public async Task<Response<BrowsedPlaylists>> BrowseCategoryPlaylistsAsync(string categoryId, int offset, int limit, string token)
		{
			return await GetAsync<BrowsedPlaylists>(string.Format("/browse/categories/{0}/playlists/?offset={1}&limit={2}", categoryId, offset, limit),
					new AccessTokenAuthenticator(token));
		}

		public async Task<Response<Playlist>> GetPlaylistTracks(string user, string playlist, string token)
		{
			// Note: it seems we need to use the next object to iterate over the list.
			// default limit seems to be 100? And there is no way to change that. I think.
			return await GetAsync<Playlist>(string.Format("/users/{0}/playlists/{1}/?{2}",
					user, playlist,
					"fields=tracks.items(track(name,href,album(name,href)))"),
					new AccessTokenAuthenticator(token));
		}

		public async Task<Response<T>> GetAsync<T>(string uri, IAuthenticator authenticator)
		{
			return await RequestAsync<T>(HttpMethod.Get, uri, null, authenticator);
		}

		public async Task<Response<T>> GetAsync<T>(string uri)
		{
			return await RequestAsync<T>(HttpMethod.Get, uri, null, null);
		}

		public async Task<Response<T>> PostJsonAsync<T>(string uri, object body)
		{
			var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
			return await PostAsync<T>(uri, content);
		}

		public async Task<Response<T>> PostFormUrlEncodedAsync<T>(string uri, params KeyValuePair<string, string>[] parameters)
		{
			return await PostAsync<T>(uri, new FormUrlEncodedContent(parameters));
		}

		public async Task<Response<T>> PostAsync<T>(string uri, HttpContent content)
		{
			return await RequestAsync<T>(HttpMethod.Post, uri, content, null);
		}

		/// <summary>
		/// Same as RequestAsync<T>(HttpMethod, string, HttpContent, IAuthenticator) 
		/// with HttpContent as null.
		/// </summary>
		/// <param name="method">Http Method used</param>
		/// <param name="uri">Everything after the host-name and version part of the request, starting with '/'</param>
		/// <param name="authenticator">Strategy used for authenticating</param>
		public async Task<Response<T>> RequestAsync<T>(HttpMethod method, string uri, IAuthenticator authenticator)
		{
			return await RequestAsync<T>(method, uri, null, authenticator);
		}

		/// <summary>
		/// Returns an awaitable Response<T> from a request to the spotify api where the content is deserialized from json
		/// </summary>
		/// <param name="method">Http Method used</param>
		/// <param name="uri">Everything after the host-name and version part of the request, starting with '/'</param>
		/// <param name="content">If applicable this is the content that will be sent in the request</param>
		/// <param name="authenticator">Strategy used for authenticating</param>
		public async Task<Response<T>> RequestAsync<T>(HttpMethod method, string uri, HttpContent content, IAuthenticator authenticator)
		{
			using(var httpClient = new HttpClient())
			{
				var message = new HttpRequestMessage()
				{
					Method = method,
					RequestUri = new Uri(string.Format("{0}{1}", baseUri, uri))
				};
				if (authenticator != null) 
					authenticator.Authenticate(httpClient, message);

				if (content != null)
					message.Content = content;

				var response = await httpClient.SendAsync(message);
				T responseContent = default(T);
				string responseString = null;
				try
				{
					responseString = await response.Content.ReadAsStringAsync();
					responseContent = JsonConvert.DeserializeObject<T>(responseString, new JsonSerializerSettings()
					{
						NullValueHandling = NullValueHandling.Ignore
					});
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception reading content [{0}:{1}]: {2}", response.StatusCode, response.ReasonPhrase, e);
				}
				return new Response<T>()
				{
					StatusCode = response.StatusCode,
					Reason = response.ReasonPhrase,
					Content = responseContent,
					RawContent = responseString,
					Headers = response.Headers
				};
			}
		}


		public void Dispose()
		{

		}
	}
}
