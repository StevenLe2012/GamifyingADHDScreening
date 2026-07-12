# Umaki / GamifyingADHDScreening — Unity Agent Handoff

**Purpose:** Give a new AI agent enough context to work on the Unity game, WebGL deploy, and Firebase logging without re-discovering architecture, conventions, or known issues.

**Last updated:** 2026-07-12  
**Unity project path:** `C:\Users\mydoa\Downloads\ADHDScreening\GamifyingADHDScreening\`  
**Git branch:** `computerized_version`  
**Related handoff (analysis pipeline):** `../umaki_study1/AGENT_HANDOFF.md`

---

## 1. What this project is

**Umaki** is a gamified ADHD screening study built in Unity. Participants:

1. Open a WebGL link in the browser (research deployment)
2. See intro thumbnail → optional login → intro video → main game
3. Explore **4 islands** (Cards, Bread, Poison, Skull; Dragon is narrative-only in some flows)
4. Complete **dialogue / ASRS narrative questions** and **Moxo CPT** trials per island
5. Data uploads to **Firebase Realtime Database** (Europe)

**Live URL:** https://umaki-f44d9.web.app  
**Firebase project:** `umaki-f44d9`  
**RTDB URL:** `https://umaki-f44d9-default-rtdb.europe-west1.firebasedatabase.app`

This is a **research instrument**, not a consumer game. Stability, data integrity, and reproducible participant IDs matter more than feature polish.

---

## 2. Workspace layout (parent folder)

```
ADHDScreening/
├── GamifyingADHDScreening/     ← THIS Unity project (source)
├── Umaki_WebGL/                 ← WebGL build output + Firebase Hosting deploy root
├── umaki_study1/                ← Python analysis pipeline (separate AGENT_HANDOFF.md)
├── video_originals_hevc/        ← Original HEVC video backups (pre H.264 re-encode)
└── *.docx, *.log                ← Study docs, build logs
```

**Rule:** Edit game code in `GamifyingADHDScreening/`. Build writes to `Umaki_WebGL/`. Deploy from `Umaki_WebGL/`.

---

## 3. Unity version & environment

| Setting | Value |
|---------|-------|
| Unity | **2022.2.4f1** (revision `8216e0211249`) |
| Editor path (typical) | `C:\Program Files\Unity\Hub\Editor\2022.2.4f1\Editor\Unity.exe` |
| WebGL template | `PROJECT:UmakiFullScreen` |
| WebGL compression | **Gzip** (do not switch to Brotli without updating `firebase.json` headers) |
| Input | **Input System (Both)** — `activeInputHandler: 2` in ProjectSettings |
| WebGL memory | Initial 256 MB, max 2048 MB, geometric growth |

**Close Unity Editor before batch builds** — batch mode fails if the project is open.

---

## 4. Build scenes & game flow

### Enabled build scenes (only 2)

| Index | Scene | Role |
|-------|-------|------|
| 0 | `Assets/Scenes/BootIntro.unity` | Login, thumbnail gate, intro video, preload Main |
| 1 | `Assets/Scenes/Main.unity` | Full game (hub, islands, CPT, dialogue, endings) |

All islands live **inside Main.unity** (teleport/enable roots), not as separate loaded scenes.

### High-level participant flow (WebGL)

```
Browser download (~420 MB .data.unityweb)
  → BootIntro: IntroBoot on GameObject "BootVideo"
      → Thumbnail (press space/click) — skip if ?fast=1&skipintro=1 with ?num=
      → Login screen — skip if ?num= or ?pid= in URL
      → Loading page while intro.mp4 prefetches
      → Intro video plays
      → Async preload + activate Main scene
  → Main: GameManager starts in Narrative state
      → Island travel, dialogue (Ink), CPT per island
      → Ending cutscene → session summary upload
```

### URL parameters (`GameManager` / `IntroBoot`)

| Param | Effect |
|-------|--------|
| `?num=12345` | Sets 5-digit participant ID; **skips login** |
| `?pid=FOO` | Legacy free-form ID; skips login |
| `?fast=1` or `?skipintro=1` | With `?num=`, skips thumbnail + intro video → Main |
| (none) | Shows login screen after thumbnail |

**Example links:**
- Researcher: `https://umaki-f44d9.web.app/?num=24756`
- Self-entry: `https://umaki-f44d9.web.app/`

There is **no geo whitelist**, no block for returning participants, and no Firebase auth required to **load** the game (auth only for **writes**).

---

## 5. Key scripts by folder

### `Assets/_Scripts/Intro/` — Boot, cutscenes, WebGL bridges

| File | Role |
|------|------|
| **`IntroBoot.cs`** | **Central boot orchestrator** on `BootVideo`. WebGL: thumbnail → login → intro video → Main. Read this first for start-screen bugs. |
| `VideoLoadingScreen.cs` | Full-screen loading page overlay (`Resources/UI/LoadingPage.png`) |
| `CutsceneWebGLPrepare.cs` | Prefetch MP4 → blob URL → VideoPlayer.Prepare |
| `CutsceneWebGLVideoOutput.cs` | WebGL video renders to fullscreen UI RawImage (CameraNearPlane often black) |
| `CutsceneWorldHide.cs` | Hides 3D world during cutscenes (black letterbox margins) |
| `IslandIntroCutscene.cs` | Per-island intro videos in Main |
| `EndGameCutscene.cs` | Ending video + score screen |
| `WebGLPageVisibility.cs` | Also contains `WebGLIntroBridge` (gesture forwarding to IntroBoot) |
| `WebGLFullscreen.cs` | Browser fullscreen after user gesture |
| `WebGLFullscreenBootstrap.cs` | Fallback fullscreen on first input in Main if intro skipped |
| `WebGLVideoPrefetch.cs` | C# wrapper for `VideoPrefetch.jslib` |
| `MainSceneLoadBridge.cs` | Signals Main ready → hides post-intro loading overlay |
| `ParticipantLoginScreen.cs` | 5-digit code entry (`Assets/_Scripts/UI/` but wired in IntroBoot) |

### `Assets/_Scripts/` root

| File | Role |
|------|------|
| **`GameManager.cs`** | Global state machine: Narrative → Explore → PrepareCPT → CPT → Teleport |
| `ParticipantSession.cs` | Static holder for login-screen data across scene load |
| `SessionPlayTimeTracker.cs` | Play time → `umaki/session_summary` at end |

### `Assets/_Scripts/MoxoCPT/` — CPT core + Firebase logging

| File | Role |
|------|------|
| **`FirebaseService.cs`** | Anonymous auth, write queue, retry, WebGL localStorage persistence |
| **`LoggingReport.cs`** | CPT trial rows → Firebase; **session/participant ID cache**; attempt tracking |
| `LoggingDialogueChoices.cs` | Dialogue/ASRS choices → Firebase (POST) |
| `LoggingDistractors.cs` | Distractor spawn/dismiss events → Firebase (POST) |
| `DistractorSystem.cs` | Spawns/manages distractor characters during CPT |
| `TrainingCPTRunner.cs` / island runners | Per-island CPT trial logic |

### `Assets/_Scripts/Islands/`

| File | Role |
|------|------|
| `IslandTravelManager.cs` | Hub travel, cutscene gates, island activation |
| `IslandSelectionUI.cs` | Island picker UI |

### `Assets/_Scripts/UI/Dialogue/`

Ink-based narrative dialogue, ASRS embedded questions, stick-option selector for WebGL/gamepad.

### `Assets/Editor/`

| File | Role |
|------|------|
| **`WebGLBuildWithReport.cs`** | Build → WebGL menu + CLI `WebGLBuildWithReport.BuildWebGL` |

---

## 6. Firebase data paths (critical for analysis joins)

All paths under RTDB root `umaki/`. Writes require `auth != null` (anonymous sign-in in `FirebaseService`).

| Collection | Path pattern | Method | Writer |
|------------|--------------|--------|--------|
| CPT trials | `umaki/cpt_trials/{pid}/{sessionId}/{islandId}/a{attempt}/t{N}` | PUT | `LoggingReport.cs` |
| Distractor events | `umaki/distractor_events/{pid}/{sessionId}/{autoKey}` | POST | `LoggingDistractors.cs` |
| Dialogue choices | `umaki/dialogue_choices/{pid}/{sessionId}/{autoKey}` | POST | `LoggingDialogueChoices.cs` |
| Session summary | `umaki/session_summary/{pid}/{sessionId}` | PUT/POST | `SessionPlayTimeTracker.cs` |

**Participant ID format:** `{5digitCode}_{yyyy-MM-dd}` (e.g. `24756_2026-07-12`)

**Session ID format:** `S_{yyyyMMdd_HHmmss}` — set once per browser run via `LoggingReport.EnsureSessionId()`

### Important logging fixes already applied

1. **CPT overwrite bug (fixed):** Trials now include `{islandId}` in path so islands don't overwrite each other's trial indices 0–83.
2. **Session ID:** `LoggingDialogueChoices.EnsureSessionId()` added; hub dialogue uses `island_id = "MAIN"`.
3. **Participant ID drift:** All loggers use `LoggingReport.CurrentParticipantId` cached once per run.

**Analysis pipeline** that consumes this data: `../umaki_study1/` (see its `AGENT_HANDOFF.md`).

---

## 7. WebGL plugins (`Assets/Plugins/WebGL/*.jslib`)

| File | Purpose |
|------|---------|
| `FirebaseWebGL.jslib` | localStorage queue mirror, keepalive flush on tab close |
| `IntroBridge.jslib` | Document-level click/key → `BootVideo.OnUserGestureFromPage()` (armed/disarmed) |
| `VideoPrefetch.jslib` | `fetch()` full MP4 → blob URL cache |
| `WebGLFullscreen.jslib` | `requestFullscreen` + Unity `SetFullscreen(1)` |
| `PageVisibility.jslib` | Tab visibility → pause/resume cutscenes |
| `AudioUnlock.jslib` | Unlock AudioContext on first user gesture |

---

## 8. StreamingAssets videos

| File | Used by |
|------|---------|
| `intro.mp4` | `IntroBoot` (boot intro) |
| `cards_intro.mp4`, `bread_intro.mp4`, `poison_intro.mp4`, `skull_intro.mp4` | Island cutscenes |
| `dragon_intro.mp4` | Dragon island |
| `Ending1.mp4`, `Ending2.mp4` | End game |

Videos were re-encoded to **H.264 720p + faststart** (~45 MB total deployed). Originals in `video_originals_hevc/`.

**WebGL playback:** Full-file prefetch via `VideoPrefetch.jslib` before `VideoPlayer.Prepare()` — streaming HTTP alone causes stalls.

---

## 9. Build & deploy workflow

### Build WebGL

**Unity must be closed.**

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.2.4f1\Editor\Unity.exe" -quit -batchmode -nographics `
  -projectPath "C:\Users\mydoa\Downloads\ADHDScreening\GamifyingADHDScreening" `
  -executeMethod WebGLBuildWithReport.BuildWebGL `
  -logFile "C:\Users\mydoa\Downloads\ADHDScreening\webgl_build.log"
```

Or: Unity menu **Build → WebGL (with size report)**

**Output:** `../Umaki_WebGL/` (~500 MB total; `Umaki_WebGL.data.unityweb` ~413 MB)

**Build report:** `Umaki_WebGL/build-size-report.txt`

The build script **deletes stale `*.loader.js`** before building (mismatched loader + wasm causes crashes).

### Deploy to Firebase

```powershell
cd C:\Users\mydoa\Downloads\ADHDScreening\Umaki_WebGL
firebase deploy --only hosting --non-interactive
```

To deploy DB rules too: `firebase deploy --non-interactive`

### WebGL template

Source: `Assets/WebGLTemplates/UmakiFullScreen/`  
Copied to `Umaki_WebGL/index.html` on build.

**If you edit `index.html` behavior**, edit the **template** and rebuild, or manually sync `Umaki_WebGL/index.html`.

### Hosting cache headers (`Umaki_WebGL/firebase.json`)

| Asset | Cache |
|-------|-------|
| `Build/*.data.unityweb`, `*.wasm.unityweb`, `*.framework.js.unityweb` | **1 year, immutable** (same filenames every build!) |
| `Build/Umaki_WebGL.loader.js` | 1 hour |
| `*.mp4` | 1 year, immutable |

**Risk:** Participants who tested during multiple deploys may have **mixed cached loader + old wasm/data** → black screen or crashes. Incognito fixes cache but not all input/render bugs.

---

## 10. BootIntro scene wiring (important for intro bugs)

In `BootIntro.unity`, `IntroBoot` is on GameObject **`BootVideo`** (not "IntroBoot").

| Inspector field | Scene object |
|-----------------|--------------|
| `loginScreen` | `ParticipantLoginCanvas` → `ParticipantLoginScreen` |
| `thumbnailImage` | `Canvas` → `Thumbnail` Image |
| `rawImage` + `tempRenderTexture` | Assigned for intro video output |
| `fadeGroup` | **null** (unused in current scene) |
| `renderToCamera` | **false** (uses RawImage path) |

`EventSystem` uses **StandaloneInputModule** (legacy), while gesture code in `IntroBoot` uses **new Input System** only (`Mouse.current`, etc.) — known fragility on some WebGL desktops.

Thumbnail sprite: `Assets/Resources/Images/Thumbnail.png` (includes "press space to continue" artwork).

---

## 11. Current WebGL intro flow (as of last deploy)

Implemented in `IntroBoot.CoRun()` (#if UNITY_WEBGL):

1. `CoEnsureBootUIReady()` — fix canvas scale 0, force CanvasScaler
2. Show `thumbnailImage` + invisible `ThumbnailContinue` Button (runtime-created)
3. `CoWaitForUserGesture()`:
   - `_awaitingUserGesture = true`
   - `WebGLIntroBridge.Setup("BootVideo")` — document listeners
   - Wait **1.25s** minimum (`ThumbnailMinDisplaySeconds`)
   - **Arm** bridge; wait for click/Space/Enter via Input System OR `OnUserGestureFromPage`
   - `WebGLFullscreen.Request()` after gesture
4. If no `?num=`: show login, wait for submit
5. `PrepareAndPlay()` — loading overlay, prefetch `intro.mp4`, play video
6. `AfterPlayback` — loading overlay, activate preloaded Main

**HTML `#unity-start-hint` is hidden** after load (`index.html` line ~195). No fallback text if Unity thumbnail fails to render.

---

## 12. Known issues & open problems (do not re-investigate from scratch)

### A. Black screen after loading bar (some participants)

**Symptoms:** Bar reaches 100%, bar disappears, black canvas, click/Space/Enter do nothing. Reported in Vietnam; also one returning participant on desktop incognito with `?num=`.

**Ruled out:** Geo block, participant ID, cache (incognito), automatic fullscreen on load.

**Likely causes (ranked):**
1. **Intro input regression** — strict gesture gating (1.25s disarm), Input-System-only clicks, no visible HTML hint; thumbnail may not render on some WebGL setups
2. **Stale build cache** (non-incognito) — immutable 1-year cache on fixed `.unityweb` filenames vs hourly `loader.js` refresh
3. **Unity/WebGL hard failure** after load on specific GPU/browser (check F12 console)
4. **Slow network** — less likely if bar hits 100%

**Not fixed as of this handoff.** Safe fix directions (when asked): restore visible start hint, re-enable legacy `Input.GetMouseButtonDown`, reduce/remove arming delay, versioned build filenames.

### B. Large initial download (~420 MB)

Firebase CDN is Europe (Frankfurt). Vietnam/Asia = slow. Not a code bug but affects completion rates.

### C. Cutscene / island video issues (mostly fixed)

- Island cutscenes skipping: fixed via `CutsceneWebGLPrepare` prefetch-first
- White screen on 2nd+ island: fixed `CutsceneWebGLVideoOutput.Hide()` texture clear
- CPT path overwrite across islands: fixed with `{islandId}` in path

### D. Editor vs WebGL testing

**Editor Play mode does NOT run the WebGL intro path** (`#if UNITY_WEBGL && !UNITY_EDITOR`). Test intro changes via WebGL build + `firebase serve` or deploy.

Local test server:
```powershell
cd Umaki_WebGL
firebase serve --only hosting --port 5050
# → http://localhost:5050
```

---

## 13. Game architecture (Main scene)

### State machine (`GameManager.GameState`)

```
Narrative → Explore → PrepareCPT → CPT → Teleport → (loop)
```

### Islands (MOXO CPT)

4 base islands with CPT + distractors. Trial structure: NDP (no distractor phase) + DP (distractor phase). Training/replay flows exist per island.

### Dialogue

Ink narrative (`Assets/Ink/`) with embedded ASRS items. Choices logged to `dialogue_choices`. `StickOptionSelector.cs` handles WebGL keyboard/gamepad selection.

### Distractors

`DistractorSystem.cs` — 24 named characters, 6 categories, spawned during DP phase. Events logged to `distractor_events`.

---

## 14. Coding conventions for agents

1. **Minimize scope** — research deploys are sensitive; small focused diffs.
2. **Match existing style** — same naming, `#if UNITY_WEBGL` patterns, static helpers in Intro/.
3. **Don't change Firebase paths** without updating `umaki_study1` pipeline and `config/firebase_config.json`.
4. **Don't commit** `firebase_credentials.json`, `.env`, or service account keys.
5. **Don't force-push** or amend deployed commits unless user asks.
6. **WebGL path separators** — use `/` not `Path.Combine` for StreamingAssets URLs on WebGL.
7. **Rebuild + deploy** after any Unity script or jslib change affecting WebGL.
8. **Only create git commits when user explicitly asks.**

---

## 15. Quick reference commands

```powershell
# Build (Unity closed)
& "C:\Program Files\Unity\Hub\Editor\2022.2.4f1\Editor\Unity.exe" -quit -batchmode -nographics -projectPath "C:\Users\mydoa\Downloads\ADHDScreening\GamifyingADHDScreening" -executeMethod WebGLBuildWithReport.BuildWebGL -logFile "C:\Users\mydoa\Downloads\ADHDScreening\webgl_build.log"

# Deploy
cd C:\Users\mydoa\Downloads\ADHDScreening\Umaki_WebGL
firebase deploy --only hosting --non-interactive

# Local WebGL test
cd C:\Users\mydoa\Downloads\ADHDScreening\Umaki_WebGL
firebase serve --only hosting --port 5050

# Fetch Firebase data for analysis
cd C:\Users\mydoa\Downloads\ADHDScreening\umaki_study1
python 01_fetch_firebase.py
```

---

## 16. Files to read first (by task)

| Task | Start here |
|------|------------|
| Intro / black screen | `IntroBoot.cs`, `Umaki_WebGL/index.html`, `IntroBridge.jslib` |
| Cutscene video | `CutsceneWebGLPrepare.cs`, `IslandIntroCutscene.cs`, `VideoPrefetch.jslib` |
| CPT / trials | `TrainingCPTRunner.cs`, `LoggingReport.cs`, `DistractorSystem.cs` |
| Dialogue / ASRS | `Assets/_Scripts/UI/Dialogue/`, `LoggingDialogueChoices.cs` |
| Firebase writes | `FirebaseService.cs`, `Umaki_WebGL/database.rules.json` |
| Build / deploy | `WebGLBuildWithReport.cs`, `firebase.json`, WebGL template |
| Data analysis | `../umaki_study1/AGENT_HANDOFF.md` |

---

## 17. Study context

- Participants recruited in **Germany and Vietnam** (and possibly elsewhere)
- Germany: generally fast CDN, desktop Chrome, reliable
- Vietnam: slower 420 MB download, more variable devices; some successful completions exist in Firebase (e.g. IDs 12199, 14557, 18476, 24505, 27776, 40663 with full 336 CPT trials)
- Failed sessions often have **no Firebase data** (never passed intro / never finished upload)
- Coordinator shares per-participant links: `?num=XXXXX`

---

*End of Unity agent handoff. For Python analysis pipeline context, see `../umaki_study1/AGENT_HANDOFF.md`.*
