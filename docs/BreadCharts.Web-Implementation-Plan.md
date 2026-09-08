# BreadCharts Web Implementation Plan

## Purpose

Build a browser-first BreadCharts application in a clean Blazor Web App. The
application uses Spotify as both the user's identity provider and the source
of searchable music data. Users browse Spotify content, select a track, and
vote for it in the active competition.

The existing web application is preserved as `BreadCharts.Web.old` during the
migration, but it is not part of the solution or build. The new application
should not inherit its UI, authentication flow, or Fluent UI v4 assumptions.

## Product scope

### Initial user experience

1. An unauthenticated user sees a public welcome page.
2. The user signs in with Spotify.
3. The authenticated home page shows the active competition and current voted
   songs, including an explicit empty state.
4. The user opens an add-song dialog.
5. The user searches Spotify by track, artist, album, or playlist.
6. The user browses search results and navigates into details using a
   breadcrumb trail.
7. A track is the terminal browse item and can be added as a vote.
8. The user's vote allocation is enforced, with a default limit of 10.
9. Removing the final vote from a track removes that track from the voted
   options.

### Deferred scope

- Desktop and WASM clients.
- Playlist creation or modification in Spotify.
- Playback and streaming.
- Realtime chart updates.
- Multiple simultaneous competitions in the first UI milestone.
- Admin functionality beyond the minimum needed to configure an active
  competition.

The data model should still represent competitions explicitly so these
features do not require a later schema redesign.

## Repository structure

```text
BreadCharts.AppHost/
    Aspire orchestration for local development

BreadCharts.Web/
    Blazor Web App
    Spotify OAuth and token lifecycle
    Application services
    Fluent UI v5 interface
    Database access

BreadCharts.Web.old/
    Temporary reference copy of the previous web application
    Excluded from the solution and build

BreadCharts.Core/
    Reusable domain models, service contracts, and Spotify abstractions

BreadCharts.WebApi/
    Retained for a future external or desktop client
    Not required by the first browser-only implementation
```

The web application should own the first version's server-side operations.
Keeping a separate API in the first milestone would duplicate authentication
and session boundaries without providing a current product benefit.

## Prerequisites and tooling

- .NET 10 SDK.
- Aspire AppHost and CLI versions aligned with the installed SDK.
- Fluent UI Blazor v5 package.
- Fluent UI Blazor MCP server configured in the developer's AI client.
- Spotify developer application with valid redirect URIs.
- Local user secrets for Spotify credentials.

The MCP server is an AI-client tool, not an application dependency. It should
be configured in Zed or another MCP-compatible client and should not be added
as a project package.

Use the MCP server to check v5 component names, parameters, enums, and
migration guidance before writing Fluent UI markup.

## Project reset

1. Rename the existing project directory to `BreadCharts.Web.old`.
2. Remove the old project from `BreadCharts.sln`.
3. Remove old web project references from `BreadCharts.AppHost`.
4. Create a new .NET 10 Blazor Web App at `BreadCharts.Web`.
5. Add the new project to the solution.
6. Register the new project in `BreadCharts.AppHost`.
7. Start with a plain page and minimal layout.
8. Confirm direct execution and Aspire execution before adding product code.

The old project should remain available for reference until the new
application reaches functional parity for the required flows. It must not
remain a build input.

## Aspire development topology

The initial AppHost should run the web application directly:

```text
BreadCharts.AppHost
    └── BreadCharts.Web
```

The AppHost supplies development environment configuration and service
discovery where useful. It must not contain Spotify client secrets. Secrets
should come from user secrets or an appropriate local environment mechanism.

The web application should be testable both ways:

```text
dotnet run --project BreadCharts.Web/BreadCharts.Web.csproj
dotnet run --project BreadCharts.AppHost/BreadCharts.AppHost.csproj
```

## Authentication design

Use Spotify Authorization Code OAuth on the server:

```text
Browser
    -> BreadCharts.Web /auth/spotify
    -> Spotify authorization
    -> BreadCharts.Web /signin-spotify
    -> local user upsert
    -> secure application cookie
    -> authenticated Blazor UI
```

Requirements:

- The Spotify client secret remains server-side.
- The browser never receives the client secret.
- Spotify access and refresh tokens are stored server-side.
- Access tokens are refreshed when expired.
- OAuth correlation and application cookies use secure settings.
- Redirect URIs are environment-specific and explicitly registered in Spotify.
- Logout clears the application session.
- Authentication failures produce a visible, recoverable UI state.

Only request Spotify scopes required by implemented features. The initial
scope set should be limited to identity/profile access unless a later
feature needs playlist or playback permissions.

The previously exposed Spotify secret must be rotated before credentials are
configured in the new application.

## Spotify client decision

Start with `SpotifyAPI.Web` as the typed client adapter. It provides useful
request and response models and avoids handwritten mappings for the first
implementation.

Before depending on an endpoint, verify it against the current Spotify Web
API and the installed package version:

- Search for track, album, artist, and playlist.
- Get track details.
- Get album details and tracks.
- Get artist details, popular tracks, and albums.
- Get playlist details and items.
- Handle paging and empty results.

If a required wrapper method is stale or unavailable, add a small typed
`HttpClient` implementation for that endpoint rather than replacing the whole
client library. Keep Spotify SDK models behind the application service
boundary.

## Domain model

### Application user

```text
ApplicationUser
- Id
- SpotifyUserId
- DisplayName
- Email
- Protected access token data
- Protected refresh token data
```

Token storage must not expose raw values to the UI. Prefer protected storage
and isolate token access in an authentication/token service.

### Competition

```text
Competition
- Id
- Name
- Status
- StartsAt
- EndsAt
- VoteLimitPerUser
```

The first UI can select one active competition, but the model should include
the competition relationship from the beginning.

### Voted song

```text
VotedSong
- Id
- CompetitionId
- SpotifyTrackId
- TrackName
- AlbumName
- ArtistName
- ImageUrl
- VoteCount
- CreatedAt
```

Cache the display metadata when a track is first added so chart rendering does
not require a Spotify call for every row.

### User vote

```text
SongVote
- Id
- CompetitionId
- VotedSongId
- UserId
- CreatedAt
```

Database constraints and transaction rules:

- Unique `(CompetitionId, UserId, VotedSongId)`.
- A user cannot exceed the competition vote limit.
- Adding a vote increments the voted-song count.
- Removing a vote decrements the count.
- A voted song is deleted when its count reaches zero.
- Add/remove operations are transactional.
- Concurrent vote submissions are handled safely.

## Application service boundaries

```text
ISpotifyAuthService
ISpotifyCatalogService
ICompetitionService
IVotingService
ICurrentUserService
```

Blazor components should call application services and should not directly
depend on `UserManager`, EF Core, or Spotify SDK request types.

### Spotify auth service

- Read the authenticated Spotify identity.
- Obtain and refresh Spotify tokens.
- Expose an authenticated Spotify client to application services.

### Spotify catalog service

- Search Spotify.
- Load track, album, artist, and playlist details.
- Convert SDK responses to application DTOs.
- Handle paging, rate limits, empty results, and recoverable API failures.

### Voting service

- Get the active competition chart.
- Get the current user's vote usage.
- Add a track vote.
- Remove a track vote.
- Enforce vote limits and duplicate protection.

## UI design

### Root and layout

Start with the smallest possible Fluent UI v5 layout:

- Fluent providers.
- Header.
- Main content region.
- Authentication-aware sign-in/sign-out controls.
- No copied v4 navigation markup.

Every component should be checked against the Fluent UI v5 MCP documentation
before use. Avoid introducing a custom error boundary until the standard
Blazor error boundary behavior is understood.

### Welcome page

The public welcome page contains:

- Product name and purpose.
- Sign-in-with-Spotify action.
- Authentication error state.
- No protected data dependencies.

### Authenticated home

The home page contains:

- Active competition status.
- Current voted songs.
- Empty state.
- User vote usage, such as `3 of 10 votes used`.
- Add-song action.

### Spotify search dialog

Use the Fluent UI v5 dialog component selected through the MCP server.

The dialog provides:

- Search input.
- Search submission.
- Loading indicator.
- Empty result state.
- Error state.
- Result type indicator.
- Result image, title, and supporting text.

Initial empty search behavior may show an empty state. A popular/default
result set can be added later if a suitable Spotify endpoint is confirmed.

### Browse and details

Use application DTOs with a common result shape:

```text
SpotifyBrowseItem
- Id
- Kind: Track | Album | Artist | Playlist
- Name
- ImageUrl
- Subtitle
```

Maintain a breadcrumb trail:

```text
Search -> Artist -> Album -> Track
```

Details behavior:

- Track: title, album, artist, artwork, and add-vote action. Terminal item.
- Album: metadata and track list.
- Artist: metadata, popular tracks, and albums.
- Playlist: metadata and playlist tracks.

Support paging at the service layer even if the first UI only loads one page.

## Implementation phases

### Phase 0: reset and baseline

- Configure the Fluent UI v5 MCP server in the development AI client.
- Rotate any exposed Spotify secret.
- Preserve the old web project as `BreadCharts.Web.old`.
- Create and register a clean `BreadCharts.Web`.
- Run a plain page directly and through Aspire.
- Commit the baseline.

### Phase 1: Fluent UI v5 shell

- Add the v5 Fluent UI package.
- Configure Fluent providers.
- Create a minimal layout.
- Confirm interactive server rendering.
- Add a simple v5 button and dialog.
- Confirm the app works through Aspire.

### Phase 2: Spotify authentication

- Configure Spotify OAuth.
- Implement callback and local-user upsert.
- Store and refresh tokens securely.
- Add authenticated/unauthenticated routing.
- Implement logout and failure states.
- Test successful, cancelled, and expired authentication.

### Phase 3: Spotify catalog

- Add the Spotify client adapter.
- Add search DTOs.
- Implement search.
- Implement track, album, artist, and playlist details.
- Implement paging and error handling.
- Add fake catalog data for UI tests where useful.

### Phase 4: voting domain

- Add competition, voted-song, and song-vote entities.
- Add database indexes and constraints.
- Implement transactional add/remove operations.
- Enforce the default ten-vote limit.
- Test duplicate votes, limits, concurrency, and zero-vote deletion.

### Phase 5: authenticated home

- Render the current voted-song section.
- Add empty state and vote usage.
- Add the Spotify search dialog.
- Add track selection and vote submission.
- Refresh the chart after a successful vote.

### Phase 6: browse experience

- Add result selection.
- Add details views.
- Add breadcrumb navigation.
- Add artist, album, and playlist browsing.
- Add track terminal behavior.
- Add paging.

### Phase 7: competition administration

- Add admin authorization.
- Add competition creation and configuration.
- Add start/end status.
- Add configurable vote limits.
- Add competition closure behavior.

### Phase 8: hardening

- Add OAuth integration tests.
- Add Spotify adapter tests with fake responses.
- Add voting transaction and concurrency tests.
- Add structured logging.
- Address package vulnerability warnings.
- Remove the old project after parity is reached.

## First milestone acceptance criteria

The first useful version is complete when:

- Aspire starts the new web application.
- An unauthenticated user sees the welcome page.
- Spotify OAuth completes without exposing credentials to the browser.
- An authenticated user sees an empty or populated voted-song section.
- The user can search for tracks, albums, artists, and playlists.
- The user can browse into album, artist, and playlist details.
- The user can add a track as a vote.
- The user cannot exceed ten votes.
- The user cannot vote for the same track twice.
- Removing the final vote removes the track from the voted options.
- Empty Spotify results and Spotify API failures have visible UI states.
