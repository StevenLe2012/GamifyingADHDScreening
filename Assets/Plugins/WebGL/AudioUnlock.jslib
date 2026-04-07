mergeInto(LibraryManager.library, {

  // Call once from C# Start(). Registers direct DOM event listeners so that
  // AudioContext.resume() is called INSIDE a real browser event callback —
  // the ONLY way the browser's autoplay policy will allow it to succeed.
  // This is far more reliable than calling resume() from Unity's Update loop.
  SetupAudioAutoUnlock: function () {
    function tryUnlock() {
      var ctx = (typeof WEBAudio !== "undefined") ? WEBAudio.audioContext : null;
      if (!ctx) return;
      if (ctx.state === "suspended") {
        ctx.resume().then(function () {
          console.log("[UnityAudio] AudioContext unlocked by user gesture.");
        }).catch(function (e) {
          console.warn("[UnityAudio] AudioContext resume failed:", e);
        });
      }
    }

    // Attach directly to the DOM — these have browser user-activation, so resume() works.
    document.addEventListener("click",      tryUnlock, { passive: true });
    document.addEventListener("keydown",    tryUnlock, { passive: true });
    document.addEventListener("touchstart", tryUnlock, { passive: true });

    // Try once immediately (context might already be running on reload).
    tryUnlock();
    console.log("[UnityAudio] Audio auto-unlock listeners registered.");
  },

  IsUnityAudioContextRunning: function () {
    if (typeof WEBAudio !== "undefined" && WEBAudio.audioContext) {
      return WEBAudio.audioContext.state === "running" ? 1 : 0;
    }
    return 0;
  }

});
