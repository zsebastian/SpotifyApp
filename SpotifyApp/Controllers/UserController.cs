using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpotifyApp.Models;

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

        public IActionResult Authorized(string state, string code, string error)
        {
        	if (error != null) return Error();

			var token = Program.spotify.GetTokenAsync(redirectUri, code).Result;
            ViewData["Message"] = "Authorized!";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
