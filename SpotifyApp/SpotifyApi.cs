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
                    }
                }
                return ret;
			}
		}

		public void Dispose()
		{

		}
	}
}
