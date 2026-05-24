Yes — **you can have the WebApi handle the auth flow almost entirely**, and have the Avalonia browser app wait for the result by polling/fetching an API endpoint.

This is probably the best strategy if the static callback page is becoming a hosting problem.

The pattern is:

> The app creates an auth session on the WebApi, opens the browser/popup to the WebApi auth URL, the WebApi completes Spotify auth, stores the result against that session, and the Avalonia app polls the WebApi until the session is complete.

This avoids needing the Avalonia browser host to serve `auth_callback.html` at all.

---

## Important clarification

You cannot do the whole Spotify redirect flow using `fetch`.

OAuth login requires **top-level browser navigation** or a popup/window because the user must interact with Spotify’s login/consent page.

So this will not work:

```plain text
Avalonia browser app
    fetch("/api/auth/spotify")
        → Spotify login
        → redirect
        → somehow get result
```


Browsers block or complicate that because:

- Spotify login is interactive HTML, not an API response,
- redirects cross origins,
- CORS applies,
- cookies/auth state are involved,
- the user needs a real browser window.

But you **can** use `fetch` to:

1. create an auth session,
2. poll for auth completion,
3. retrieve the final app token/user state.

The popup only exists to let the user complete Spotify login.

---

# Recommended new auth strategy

## Replace callback-page messaging with server-side auth sessions

Instead of:

```plain text
popup → Spotify → WebApi → auth_callback.html → postMessage → app exchanges code
```


use:

```plain text
app → create auth session
app → open popup to WebApi auth URL
popup → Spotify
Spotify → WebApi callback
WebApi → stores auth result for session
popup → displays "Login complete, return to app"
app → polls WebApi for session result
```


No static callback page.  
No `postMessage`.  
No `BroadcastChannel`.  
No `localStorage`.  
No cross-tab communication.

---

## Flow in detail

### 1. Avalonia app starts auth

The browser/desktop app calls:

```
POST /api/auth/session
```


The WebApi creates a temporary auth session and returns:

```json
{
  "sessionId": "abc123",
  "authUrl": "https://localhost:7206/api/auth/spotify?sessionId=abc123"
}
```


The app stores `sessionId`.

---

### 2. Avalonia app opens auth URL

Browser target:

```plain text
window.open(authUrl)
```


Desktop target:

```plain text
Process.Start(authUrl)
```


The popup/browser navigates to:

```plain text
/api/auth/spotify?sessionId=abc123
```


The WebApi starts the Spotify challenge.

---

### 3. Spotify redirects back to WebApi

Spotify redirects to your server-side OAuth callback, for example:

```plain text
https://localhost:7206/api/auth/callback
```


Then your WebApi finalizes auth.

The WebApi knows the original `sessionId`, because it stored it in the auth properties before challenging Spotify.

---

### 4. WebApi stores auth result against session

After successful Spotify auth, the WebApi stores something like:

```json
{
  "status": "complete",
  "appToken": "...",
  "user": {
    "id": "...",
    "displayName": "..."
  }
}
```


against:

```plain text
sessionId = abc123
```


Then the popup gets a very simple server-generated page:

```html
Login complete. You can close this window and return to the app.
```


or even:

```html
<script>window.close()</script>
```


This page is served by the WebApi, not the Avalonia browser app.

---

### 5. Main Avalonia app polls for completion

While the popup is open, the main app polls:

```
GET /api/auth/session/abc123
```


Possible responses:

### Pending

```json
{
  "status": "pending"
}
```


### Complete

```json
{
  "status": "complete",
  "appToken": "...",
  "user": {
    "id": "...",
    "displayName": "..."
  }
}
```


### Failed

```json
{
  "status": "failed",
  "error": "User denied access"
}
```


### Expired

```json
{
  "status": "expired"
}
```


Once complete, the app stops polling and updates its authenticated state.

---

# Why this fits your scenario better

This avoids your current pain points:

## No static auth callback page

You no longer need:

```plain text
/auth_callback.html
```


So it does not matter whether the Avalonia browser dev host serves it correctly.

---

## No browser-origin coordination

The popup does not need to communicate with the opener.

The main app just talks to the WebApi.

---

## Works for browser and desktop

This is a big benefit.

The same auth model can work for:

```plain text
Avalonia Browser
Avalonia Desktop
Mobile later, probably
```


Browser opens popup.  
Desktop opens system browser.  
Both poll the same WebApi session endpoint.

---

## Debugging is easier

You can debug the full auth state in the WebApi.

You are no longer relying on:

- static page hosting,
- JS interop,
- postMessage origin rules,
- localStorage/BroadcastChannel,
- a second WASM instance accidentally booting in the popup.

---

## It works even if the popup cannot close itself

Some browsers only allow `window.close()` if the window was opened by script.

If it cannot close, the page can say:

```plain text
Login complete. You can close this window.
```


The main app still detects completion by polling.

---

# Suggested API shape

I would create these endpoints:

```plain text
POST /api/auth/session
GET  /api/auth/session/{sessionId}
GET  /api/auth/spotify?sessionId=...
GET  /api/auth/finalize
GET  /api/auth/error
```


Optional cleanup:

```plain text
DELETE /api/auth/session/{sessionId}
```


---

## Auth session endpoint

```
POST /api/auth/session
```


Creates a pending auth session.

Returns:

```json
{
  "sessionId": "64f4e6d6a6d94c5d8f02d13e95a2ef33",
  "authUrl": "https://localhost:7206/api/auth/spotify?sessionId=64f4e6d6a6d94c5d8f02d13e95a2ef33"
}
```


---

## Auth status endpoint

```
GET /api/auth/session/{sessionId}
```


Returns pending/complete/failed.

Example pending:

```json
{
  "status": "pending"
}
```


Example complete:

```json
{
  "status": "complete",
  "appToken": "...",
  "spotifyAccessToken": "...",
  "spotifyRefreshToken": "...",
  "expiresIn": "..."
}
```


Though I would strongly consider **not returning Spotify refresh tokens to browser clients** long term.

---

# What happens inside `/api/auth/spotify`

When the app calls:

```plain text
/api/auth/spotify?sessionId=abc123
```


the WebApi should:

1. validate the session exists,
2. store `sessionId` in `AuthenticationProperties.Items`,
3. challenge Spotify.

Conceptually:

```csharp
var props = new AuthenticationProperties
{
    RedirectUri = "/api/auth/finalize"
};

props.Items["sessionId"] = sessionId;

return Results.Challenge(props, ["Spotify"]);
```


Then when Spotify redirects back and your finalize endpoint runs, you read:

```csharp
var sessionId = result.Properties?.Items["sessionId"];
```


and complete that server-side session.

---

# What `/api/auth/finalize` returns

Instead of redirecting to the Avalonia app/static callback page, it can return plain HTML:

```html
<!doctype html>
<html>
<body>
  <p>Login complete. You can close this window and return to the app.</p>
  <script>
    setTimeout(() => window.close(), 500);
  </script>
</body>
</html>
```


This page is served directly by the WebApi.

That means you do not need frontend static hosting to be correct for auth.

---

# What the Avalonia app does

The app-side auth flow becomes:

```plain text
BeginAuth()
    POST /api/auth/session
    open authUrl
    poll /api/auth/session/{sessionId}
    when complete, return AuthResult
```


No callback URI parsing needed for browser.

---

## Browser behavior

```plain text
User clicks login
    ↓
App calls POST /api/auth/session
    ↓
App opens popup to authUrl
    ↓
App starts polling every 1 second
    ↓
User completes Spotify login
    ↓
WebApi marks session complete
    ↓
Poll returns complete
    ↓
App logs in
```


---

## Desktop behavior

Same pattern:

```plain text
User clicks login
    ↓
Desktop calls POST /api/auth/session
    ↓
Desktop opens system browser to authUrl
    ↓
Desktop polls every 1 second
    ↓
User completes Spotify login
    ↓
WebApi marks session complete
    ↓
Desktop receives result
```


This is cleaner than trying to host a local loopback server in the desktop app.

---

# Polling vs SignalR/SSE

Start with polling.

It is simple and reliable.

Example:

```plain text
GET /api/auth/session/{sessionId}
```


every:

```plain text
1000ms
```


for up to:

```plain text
2–5 minutes
```


Later, if you want real-time behavior, you could switch to:

- SignalR,
- Server-Sent Events,
- WebSocket.

But polling is absolutely fine for auth.

---

# Security recommendations

## 1. Session IDs must be random and unguessable

Use a cryptographically strong random value or GUID-like random token.

Example:

```plain text
64f4e6d6a6d94c5d8f02d13e95a2ef33
```


---

## 2. Sessions should expire quickly

For example:

```plain text
5 minutes
```


After expiry:

```json
{
  "status": "expired"
}
```


---

## 3. Sessions should be single-use

Once the app retrieves the completed auth result, remove or invalidate it.

---

## 4. Store auth failure too

If Spotify fails or the user denies consent, store:

```json
{
  "status": "failed",
  "error": "access_denied"
}
```


Then the app can stop polling and show a message.

---

## 5. Avoid storing long-lived Spotify refresh tokens in the browser

The more secure design is:

```plain text
Browser receives only your app token/session.
WebApi stores Spotify refresh token.
Browser calls WebApi.
WebApi calls Spotify.
```


For now you can keep your current response model if needed, but long-term I would move Spotify refresh token ownership to the server.

---

# Does this replace the static callback page?

Yes.

In this model, the OAuth callback path is fully WebApi-owned.

You no longer need:

```plain text
/auth_callback.html
```


You no longer need the popup to return to the Avalonia app origin.

The popup returns to:

```plain text
/api/auth/finalize
```


and the WebApi displays a completion page.

The main app independently discovers completion by polling.

---

# Does the WebApi “handle auth entirely”?

Mostly yes.

The WebApi handles:

- Spotify challenge,
- Spotify callback,
- user creation/update,
- JWT creation,
- token storage,
- session completion,
- final completion page.

The app handles only:

- starting an auth session,
- opening the auth URL,
- polling for completion,
- storing the returned app auth state.

That is a good separation.

---

# Recommended direction

Given the problems you are hitting, I would switch to this:

```plain text
Server-side auth session + popup/browser + polling
```


instead of:

```plain text
static callback page + postMessage + JS interop
```


This will be easier to reason about and will work for both browser and desktop Avalonia.

---

## Final proposed flow

```plain text
Avalonia app
  POST /api/auth/session
        ↓
WebApi
  creates session, returns authUrl
        ↓
Avalonia app
  opens authUrl in popup/browser
  starts polling /api/auth/session/{id}
        ↓
WebApi
  redirects user to Spotify
        ↓
Spotify
  redirects back to WebApi callback
        ↓
WebApi
  finalizes auth
  stores result on session
  returns "Login complete" HTML to popup
        ↓
Avalonia app
  polling sees complete
  receives app token/user
  navigates to authenticated UI
```


This is likely the most robust auth strategy for your Avalonia browser + desktop setup.