namespace PersonalProjects;

public static class AppConfig
{
    // DTR target hours (600-hour internship requirement)
    public const double DtrTargetHours = 600.0;

    // Spotify token proxy endpoint — Cloudflare Worker URL
    // Deploy spotify-proxy/ Worker and replace this URL
    public const string SpotifyTokenProxyUrl = "https://spotify-proxy.YOUR_SUBDOMAIN.workers.dev/token";

    // Supabase config lives in wwwroot/js/dbFunctions.js (not here)
}
