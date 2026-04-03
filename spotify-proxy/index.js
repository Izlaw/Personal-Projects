/**
 * Cloudflare Worker: Spotify Token Proxy
 *
 * This worker exchanges your Spotify Client Credentials for an access token
 * so the secret never lives in the WASM client bundle.
 *
 * Setup:
 *   1. Deploy this worker:  wrangler deploy
 *   2. Set secrets:         wrangler secret put SPOTIFY_CLIENT_ID
 *                           wrangler secret put SPOTIFY_CLIENT_SECRET
 *   3. Update AppConfig.SpotifyTokenProxyUrl in the Blazor project
 *      to point to this worker's URL.
 */

const ALLOWED_ORIGINS = [
    'https://personalprojects.pages.dev',  // replace with your actual Pages URL
    'http://localhost:5000',               // local dev
    'http://localhost:5001',
];

export default {
    async fetch(request, env) {
        const origin = request.headers.get('Origin') ?? '';
        const allowedOrigin = ALLOWED_ORIGINS.includes(origin) ? origin : ALLOWED_ORIGINS[0];

        // Handle CORS preflight
        if (request.method === 'OPTIONS') {
            return new Response(null, {
                status: 204,
                headers: {
                    'Access-Control-Allow-Origin': allowedOrigin,
                    'Access-Control-Allow-Methods': 'POST',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Max-Age': '86400',
                },
            });
        }

        if (request.method !== 'POST') {
            return new Response('Method Not Allowed', { status: 405 });
        }

        // Exchange client credentials for Spotify access token
        const credentials = btoa(`${env.SPOTIFY_CLIENT_ID}:${env.SPOTIFY_CLIENT_SECRET}`);

        const tokenResponse = await fetch('https://accounts.spotify.com/api/token', {
            method: 'POST',
            headers: {
                'Authorization': `Basic ${credentials}`,
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: 'grant_type=client_credentials',
        });

        if (!tokenResponse.ok) {
            return new Response(
                JSON.stringify({ error: 'Failed to fetch Spotify token' }),
                {
                    status: tokenResponse.status,
                    headers: {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': allowedOrigin,
                    },
                }
            );
        }

        const tokenData = await tokenResponse.json();

        return new Response(JSON.stringify(tokenData), {
            status: 200,
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': allowedOrigin,
                'Cache-Control': 'no-store',
            },
        });
    },
};
