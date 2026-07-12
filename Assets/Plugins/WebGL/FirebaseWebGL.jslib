mergeInto(LibraryManager.library, {

  // ── localStorage passthrough (synchronous, survives reload) ───────────────
  FB_StorageSet: function (keyPtr, valPtr) {
    try { localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valPtr)); } catch (e) {}
  },

  FB_StorageGet: function (keyPtr) {
    var v = "";
    try { v = localStorage.getItem(UTF8ToString(keyPtr)) || ""; } catch (e) { v = ""; }
    var size = lengthBytesUTF8(v) + 1;
    var buf = _malloc(size);
    stringToUTF8(v, buf, size);
    return buf;
  },

  FB_StorageRemove: function (keyPtr) {
    try { localStorage.removeItem(UTF8ToString(keyPtr)); } catch (e) {}
  },

  // ── Flush context: the RTDB base URL + current auth token, refreshed by C# ──
  FB_SetFlushContext: function (basePtr, tokenPtr) {
    window.__umakiFlush = window.__umakiFlush || {};
    window.__umakiFlush.base = UTF8ToString(basePtr);
    window.__umakiFlush.token = UTF8ToString(tokenPtr);
  },

  // ── Register a last-chance flush on page hide / tab close ──────────────────
  // Reads the persisted queue from localStorage and re-sends every pending write
  // with fetch(keepalive:true) so the request survives the page unloading.
  FB_RegisterUnloadFlush: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    if (window.__umakiFlushRegistered) return;
    window.__umakiFlushRegistered = true;

    var flush = function () {
      try {
        var raw = localStorage.getItem(key);
        if (!raw) return;

        var obj = JSON.parse(raw);
        var items = (obj && obj.items) ? obj.items : [];
        if (!items.length) return;

        var ctx = window.__umakiFlush || {};
        var base = ctx.base || "";
        var token = ctx.token || "";
        if (!base) return;

        for (var i = 0; i < items.length; i++) {
          var it = items[i];
          if (!it || !it.path) continue;
          var url = base + "/" + it.path + ".json" + (token ? ("?auth=" + encodeURIComponent(token)) : "");
          try {
            fetch(url, {
              method: it.method || "PUT",
              body: it.body || "{}",
              keepalive: true,
              headers: { "Content-Type": "application/json" }
            });
          } catch (e) {}
        }
      } catch (e) {}
    };

    window.addEventListener("pagehide", flush);
    document.addEventListener("visibilitychange", function () {
      if (document.visibilityState === "hidden") flush();
    });
  }

});
