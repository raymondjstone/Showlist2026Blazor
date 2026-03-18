// Minimal service worker for PWA installability.
// Caches static assets only — Blazor Server requires a live SignalR connection for all data.

const CACHE_NAME = 'showlist-v1';
const STATIC_ASSETS = [
    '/app.css',
    '/icon.svg',
    '/manifest.webmanifest'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(STATIC_ASSETS))
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
        )
    );
    self.clients.claim();
});

self.addEventListener('fetch', event => {
    // Only cache-first for static assets; everything else goes to network
    if (STATIC_ASSETS.some(asset => event.request.url.endsWith(asset))) {
        event.respondWith(
            caches.match(event.request).then(cached => cached || fetch(event.request))
        );
    }
});
