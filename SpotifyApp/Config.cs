using Newtonsoft.Json;
using System.IO;

namespace SpotifyApp
{
	public class Config
	{
		public static Config From(string file)
		{
			return JsonConvert.DeserializeObject<Config>(File.ReadAllText(file));
		}
		
		public string ClientId;
		public string ClientSecret;
		public string RedirectUri;
	}
}
