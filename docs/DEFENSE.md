# TuneFlow defense script

Askar Kairatbek · IT-2501. Planned talk and demonstration: about 16 minutes, followed by questions. Rehearse to adjust to your speaking pace.

## Slide 1 — TuneFlow

[0:00–0:40] Hello, my name is Askar Kairatbek from group IT-2501. My project is TuneFlow, a music streaming application built with C# and Blazor. I will explain the goal, technology choice, architecture and implementation, then demonstrate the main user workflow. This version uses six original demo compositions so that the project can run without commercial music accounts or API keys.

## Slide 2 — Project aim and relevance

[0:40–1:30] The aim is to connect the main steps of personal music listening in one application. A listener can discover a track, start playback, save it and organize it into a playlist. The practical significance is the complete workflow, including persistent personal state and error feedback. The project also demonstrates how a domain layer and reusable components can keep a small application understandable as features grow. Cross-platform support is an architectural intention, while actual verification here covers macOS and browser viewport layouts.

## Slide 3 — Technology comparison

[1:30–2:40] I compared three approaches. Interactive Server executes C# components on the server and exchanges UI events over a live connection. WebAssembly moves C# execution into the browser, which changes download and hosting tradeoffs. MVC or Razor Pages are effective for request-based pages, but a continuous music player needs additional client behavior. I selected Interactive Server because it integrates the C# domain and interactive component model with little infrastructure for this academic demonstration. The main tradeoff is the required server connection. Sources: https://learn.microsoft.com/aspnet/core/blazor/hosting-models and https://learn.microsoft.com/aspnet/core/blazor/fundamentals/dependency-injection.

## Slide 4 — Development methodology

[2:40–3:25] The implementation followed a sequence of small steps. First, the assessment criteria were translated into concrete evidence rather than treated as a checklist of claims. Next, the domain rules were separated from UI code. The listening and library features were implemented before verification. Automated domain and HTTP checks then exercised rules and delivery behavior. Browser interaction checked the user workflow. The final documentation records both what works and what has not been verified. This makes the result easier to reproduce and review.

## Slide 5 — System architecture

[3:25–4:45] The browser displays Razor components and owns the audio element. Home coordinates user actions in a Blazor circuit. The Core project contains catalog queries, track records and library rules. Personal data belongs to a scoped LibraryState, which prevents a singleton library from being shared across users. The browser stores a JSON snapshot through a small JavaScript bridge. Audio files travel directly over HTTP rather than through UI event messages. The arrows show returned data and responses. Requests travel in the opposite direction. This is a component and state-container architecture, not a claim of full MVVM.

## Slide 6 — Reusable components and state

[4:45–5:45] Artwork accepts a Track parameter. TrackRow accepts a track, selected and favorite state, and callbacks for actions. These components are reusable across search, favorites, history and playlists. Home decides what an action means, while LibraryState validates data and applies changes. Defensive copies prevent callers from changing internal playlist collections accidentally. A snapshot contains a schema version, favorite IDs, playlists and history. Storage is local to the current browser origin. It does not provide account security or synchronization. Sources: https://learn.microsoft.com/aspnet/core/blazor/state-management/ and https://learn.microsoft.com/aspnet/core/blazor/fundamentals/dependency-injection.

## Slide 7 — The listening workflow

[5:45–6:30] These are screenshots of the running application. On desktop, the sidebar and full player controls remain available. At a mobile viewport, navigation moves to the bottom and the cards use fewer columns. A persistent player keeps listening controls available while browsing. The content can scroll behind the fixed navigation region, with bottom padding to keep the last items reachable. The screenshots are implementation evidence rather than conceptual mockups. The interface also includes empty states for searches and new playlists.

## Slide 8 — Smart mood mix

[6:30–7:30] The recommendation feature is intentionally explainable. A track receives ten points when its mood matches the selected mood, and two points when it is a favorite. Descending score puts suitable tracks first. Track ID breaks ties deterministically. The result is a new ordered sequence, so ranking does not modify the catalog. This uses C# records and LINQ effectively and makes the function easy to test. It is a rule-based recommendation feature, not a trained machine-learning model. With six tracks it does not need indexing or a distributed recommendation service.

## Slide 9 — Streaming and resource use

[7:30–8:25] Each track is a 32-second original synthesized composition. The browser audio element requests the WAV file from ASP.NET Core static file middleware. Range requests support requesting only a portion of a file, which is useful for seeking. The HTTP checks verified a 206 response and the requested number of bytes for all six files. A cache header allows reuse for one day. Progress updates are throttled to reduce unnecessary Blazor messages. This is HTTP audio streaming for a small demo, not adaptive bitrate streaming such as HLS or DASH.

## Slide 10 — Error handling and accessibility

[8:25–9:20] Validation rejects blank or overly long playlist names and duplicate names. Repeated track additions do not create duplicates. Snapshot restoration removes unknown IDs and duplicate records before use. A failed browser storage operation displays a notification while the current session remains usable. Audio errors provide feedback and allow a retry or another track. Labels and visible focus states support keyboard interaction, and reduced-motion preferences are respected. A complete screen-reader or keyboard-only audit has not been performed, so I do not claim full accessibility compliance.

## Slide 11 — Testing strategy

[9:20–10:20] The domain executable contains nineteen tests and all passed. The HTTP script passed twelve checks, including catalog filtering and audio range delivery. The connected browser verified playback and pause, search, favorites, playlist creation and reload persistence. A repeatable Playwright suite is supplied, but its automated run was blocked by the environment, so it is not reported as passed. Desktop and mobile viewports were inspected. Independent Windows and Linux execution, physical devices, additional browsers, coverage measurement and load testing remain future QA tasks.

## Slide 12 — Assessment evidence

[10:20–11:05] The evidence map in the project documentation lists all twenty criteria. Architecture, reuse and state management have direct code locations. The player, library workflows and mood ranking demonstrate core and advanced functionality. Tests, documentation and the presentation support review and defense. Some criteria, particularly scalability and broad compatibility, require more verification than one development environment can establish. I therefore distinguish implementation decisions from proven capacity or compatibility results. The grading decision belongs to the evaluator.

## Slide 13 — Live demonstration

[11:05–15:30] Live demo. Open Discover and play a Focus mix. Pause, seek and resume. Open Search, type Midnight Drive and save it as a favorite. Create a playlist named Study Session. Search again and add Midnight Drive, then add a second track. Open the playlist, rename it and remove one track. Open History to show recent listening. Reload and show that favorites and the playlist persist. Open the Core code to explain the pure mood ranking function and the playlist validation rule. Run the nineteen domain tests. If audio is blocked by browser policy, press Play again and explain the explicit user gesture requirement.

## Slide 14 — Limitations and next steps

[15:30–16:20, then questions to 20:00] TuneFlow implements the complete academic listening workflow with C# and Blazor. Its current limits are explicit: a small original catalog, local browser state and a required server connection. The next steps would be authenticated accounts, database-backed ownership, licensed audio storage and operational testing. Before a public rollout, the runtime support policy, HTTPS, persistent protected server keys and load behavior also need review. I am ready to explain any component, domain rule or test. Thank you.

## Likely questions

**Why use scoped state?** A singleton would share one mutable library between all circuits. Scoped state belongs to one circuit.

**Where is functional programming used?** Search and mood ranking are pure LINQ functions over immutable Track records. Mutable library transitions are encapsulated and separately tested.

**Why no database?** Browser storage keeps this demo reproducible without accounts or setup. It cannot support cross-device synchronization or server-side ownership.

**Is this real streaming?** Yes, the browser receives audio over HTTP with byte-range responses. It does not use adaptive bitrate streaming or a commercial music catalog.

**How would it scale?** Separate metadata and ownership into a database, move licensed audio to object storage/CDN, and test circuit memory and SignalR scale-out. The current app has not been load tested.

**How are tests organized?** An executable domain runner checks business rules, a Python script checks HTTP behavior, and optional Playwright tests exercise the full UI. Only the recorded checks should be described as passed.
