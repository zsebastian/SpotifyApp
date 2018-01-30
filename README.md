SpotifyApp
==========

This is just a prototype app that uses the spotify web api. It recommends tracks based on user criteria.

Running
-------

You need a clientid and a clientsecret, for that you need to register an app with spotify. You need to also add a
redirect URI to the settings for your app. Hosting the app as-is the default is
`http://localhost:5000/User/Authorized/`, if you need to change the host you can do so, but if you need to change the
path of the uri you will need to fix routing etc so the correct handler is called in the app
(SpotifyApp.User.Authorized).

Set ClientId and ClientSecret in SpotifyApp/config.json. You can also change the redirect URI there if you need to,
otherwise leave as-is.

Once set up, you can just start the app as you want. `dotnet run --project SpotifyApp` will work, or you can just start
it from Visual Studio.

Once started just open the webpage in your browser, the default url is `http://localhost:5000`.
