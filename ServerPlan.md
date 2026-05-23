# Server Behaviour Plan

## Goal

The server should support both:

1. **Desktop clients**, which can use local network discovery.
2. **Browser/WASM clients**, which cannot use UDP discovery and should instead connect back to the server that served the app.

The key rule is:

> If the server serves the WASM app, the WASM app should treat that same browser origin as the API server.

---

# 1. Server Responsibilities

## 1.1 Serve the Web API

The server exposes the application API under stable routes such as:

```plain text
/api/voting/submissions
/api/voting/submit
/api/voting/vote/{trackId}
```


These routes should be available from the same origin that serves the WASM app.

Example:

```plain text
https://breadcharts.example.com/api/voting/submissions
```


---

## 1.2 Serve the Avalonia WASM App

The server should host the built Avalonia WASM app as static files.

Example user-facing URLs:

```plain text
https://breadcharts.example.com/
https://breadcharts.example.com/app
https://192.168.1.25:5001/
```


When a user opens the shared link, the server returns the WASM application.

---

## 1.3 Provide Same-Origin API Access

The WASM app should be able to call the API using the same origin that served it.

For example, if the app is loaded from:

```plain text
https://breadcharts.example.com/
```


then the API should be reachable at:

```plain text
https://breadcharts.example.com/api/...
```


If the app is loaded from:

```plain text
https://192.168.1.25:5001/
```


then the API should be reachable at:

```plain text
https://192.168.1.25:5001/api/...
```


This avoids the need for browser-side server discovery.

---

# 2. WASM Client Server Detection Behaviour

## 2.1 Default WASM Behaviour

When running as a browser/WASM app, the client should assume:

```plain text
API base URL = current browser origin
```


Examples:

| Browser URL | API Base URL |
|---|---|
| `https://breadcharts.example.com/` | `https://breadcharts.example.com` |
| `https://breadcharts.example.com/app` | `https://breadcharts.example.com` |
| `https://192.168.1.25:5001/index.html` | `https://192.168.1.25:5001` |
| `http://localhost:5000/` | `http://localhost:5000` |

The WASM app then calls API endpoints using relative paths:

```plain text
/api/voting/submissions
/api/voting/submit
/api/voting/vote/{trackId}
```


---

## 2.2 No UDP Discovery in WASM

The WASM client should not attempt to use:

```plain text
UDP broadcast
UDP multicast
mDNS browsing
LAN scanning
raw sockets
```


These are not available in the browser security model.

---

## 2.3 Optional Manual Override

The WASM app may optionally provide a manual server override for advanced cases.

Example use cases:

- API hosted on a different domain.
- Development environment.
- Reverse proxy misconfiguration.
- Debugging.

Manual override should not be the default path.

Recommended behaviour:

1. Try same-origin API.
2. If same-origin API responds successfully, continue.
3. If same-origin API fails, show manual server entry.
4. Save successful manual server URL for future use.

---

# 3. Desktop Client Server Discovery Behaviour

## 3.1 UDP Broadcast Support

The server may continue to broadcast discovery messages over UDP for desktop clients.

Example broadcast data:

```json
{
  "app": "BreadCharts",
  "address": "https://192.168.1.25:5001",
  "machineName": "Kitchen-PC"
}
```


Desktop clients can listen for these broadcasts and display discovered servers.

---

## 3.2 Desktop Fallback Behaviour

If no UDP servers are found, the desktop client should allow manual entry.

Recommended desktop flow:

```plain text
Start app
  ↓
Listen for UDP broadcasts
  ↓
If servers found, show list
  ↓
If none found, show manual address entry
  ↓
Validate selected/manual server
  ↓
Continue
```


---

# 4. Server Startup Behaviour

## 4.1 Determine Public/Usable Server Address

On startup, the server should determine the address it should advertise to desktop clients.

If the server is listening on a wildcard address such as:

```plain text
http://0.0.0.0:5000
https://0.0.0.0:5001
http://[::]:5000
```


then the server should translate that into a reachable local network address.

Example:

```plain text
https://192.168.1.25:5001
```


---

## 4.2 Prefer HTTPS

When both HTTP and HTTPS are available, prefer advertising HTTPS.

Example:

```plain text
https://192.168.1.25:5001
```


instead of:

```plain text
http://192.168.1.25:5000
```


---

## 4.3 Start Discovery Broadcast

If UDP discovery is enabled, the server periodically broadcasts its information.

Recommended interval:

```plain text
Every 5 seconds
```


Recommended UDP broadcast scope:

```plain text
Local subnet only
```


Recommended broadcast contents:

```json
{
  "app": "BreadCharts",
  "address": "https://192.168.1.25:5001",
  "machineName": "Kitchen-PC",
  "version": "1.0.0"
}
```


---

# 5. Health and Discovery Metadata Endpoint

## 5.1 Add a Health Endpoint

The server should expose a lightweight endpoint that clients can use to verify they are talking to a valid server.

Example:

```plain text
GET /api/health
```


Example response:

```json
{
  "status": "ok",
  "app": "BreadCharts"
}
```


---

## 5.2 Add a Server Info Endpoint

The server may expose a discovery/info endpoint.

Example:

```plain text
GET /api/discovery/info
```


Example response:

```json
{
  "app": "BreadCharts",
  "machineName": "Kitchen-PC",
  "version": "1.0.0",
  "serverTimeUtc": "2026-05-23T12:00:00Z"
}
```


This endpoint can be used by:

- Desktop clients after UDP discovery.
- WASM clients after same-origin detection.
- Manual server entry validation.
- Pairing flows.

---

# 6. Recommended Connection Flow

## 6.1 WASM Connection Flow

```plain text
User opens shared server link
  ↓
Server returns Avalonia WASM app
  ↓
WASM app starts
  ↓
WASM app uses current browser origin as API base URL
  ↓
WASM app calls GET /api/discovery/info or GET /api/health
  ↓
If valid BreadCharts server:
      continue to app
  Else:
      show error or manual server entry
```


---

## 6.2 Desktop Connection Flow

```plain text
Desktop app starts
  ↓
Start UDP discovery listener
  ↓
Receive server broadcast messages
  ↓
Show discovered BreadCharts servers
  ↓
User selects server
  ↓
Client validates server with GET /api/discovery/info
  ↓
If valid:
      continue to app
  Else:
      show error
```


---

# 7. Reverse Proxy Behaviour

If the server is behind a reverse proxy, the browser-visible origin should be the source of truth for WASM clients.

Example:

```plain text
Public browser URL: https://breadcharts.example.com
Internal API URL:   http://localhost:5000
```


The WASM app should use:

```plain text
https://breadcharts.example.com
```


not:

```plain text
http://localhost:5000
```


The reverse proxy should forward API requests:

```plain text
https://breadcharts.example.com/api/*
```


to the internal API server.

---

# 8. CORS Behaviour

## 8.1 Same-Origin Default

If the WASM app and API are served from the same origin, no special CORS setup should be required.

Preferred setup:

```plain text
WASM app: https://breadcharts.example.com
API:      https://breadcharts.example.com/api
```


---

## 8.2 Cross-Origin Optional Mode

If the WASM app is hosted separately from the API, the server must explicitly allow the WASM app origin.

Example:

```plain text
WASM app: https://app.breadcharts.example.com
API:      https://api.breadcharts.example.com
```


In this case, configure CORS to allow:

```plain text
https://app.breadcharts.example.com
```


Avoid allowing all origins unless this is intentional.

---

# 9. Security Behaviour

## 9.1 Do Not Trust Discovery Alone

Discovery only tells the client where a server might be. It should not be treated as authentication.

Clients should validate the server by calling:

```plain text
GET /api/discovery/info
```


or:

```plain text
GET /api/health
```


---

## 9.2 Use HTTPS Where Possible

Prefer HTTPS for:

- browser/WASM access,
- authentication tokens,
- voting/submission actions,
- any user-identifying data.

---

## 9.3 Avoid Broadcasting Secrets

UDP broadcast messages should not contain:

```plain text
tokens
passwords
private keys
user data
sensitive configuration
```


Discovery broadcasts should contain only basic connection metadata.

---

## 9.4 Optional Pairing Token

For stronger control, the server can require a pairing token before allowing a client to use the API.

Example pairing link:

```plain text
https://breadcharts.example.com/pair?token=abc123
```


The server can validate this token before issuing an app token or allowing participation.

---

# 10. Recommended Final Behaviour

## Server

The server should:

1. Serve the Web API under `/api`.
2. Serve the Avalonia WASM app from the same origin.
3. Expose `/api/health` or `/api/discovery/info`.
4. Optionally broadcast UDP discovery for desktop clients.
5. Prefer HTTPS addresses when advertising itself.
6. Avoid putting sensitive data in discovery broadcasts.

---

## WASM Client

The WASM client should:

1. Use the current browser origin as the API server.
2. Validate the server using `/api/health` or `/api/discovery/info`.
3. Avoid UDP discovery entirely.
4. Use manual entry only as a fallback or advanced option.

---

## Desktop Client

The desktop client should:

1. Use UDP discovery when available.
2. Show discovered servers.
3. Allow manual server entry as fallback.
4. Validate the selected server before continuing.

---

# Summary

For the WASM scenario where the server serves the app link, server discovery should be replaced with **same-origin detection**:

```plain text
The server that served the WASM app is the API server.
```


UDP discovery remains useful for desktop clients, but the browser app should simply call back to the same origin using relative API routes or the current browser origin.