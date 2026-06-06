mergeInto(LibraryManager.library, {

  WebGLVideoPrefetch_Start: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    if (!url) return;

    if (!window.__videoPrefetch) {
      window.__videoPrefetch = { cache: {} };
    }

    var cache = window.__videoPrefetch.cache;
    var entry = cache[url];
    if (entry && (entry.ready || entry.loading)) return;

    cache[url] = { loading: true, ready: false, blobUrl: null, error: false };

    fetch(url).then(function (response) {
      if (!response.ok) throw new Error("HTTP " + response.status);
      return response.blob();
    }).then(function (blob) {
      var e = cache[url];
      if (!e) return;
      e.blobUrl = URL.createObjectURL(blob);
      e.ready = true;
      e.loading = false;
    }).catch(function () {
      var e = cache[url];
      if (!e) return;
      e.error = true;
      e.loading = false;
    });
  },

  WebGLVideoPrefetch_IsReady: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    if (!window.__videoPrefetch) return 0;
    var entry = window.__videoPrefetch.cache[url];
    return (entry && entry.ready && entry.blobUrl) ? 1 : 0;
  },

  WebGLVideoPrefetch_GetPlaybackUrl: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    var playback = url;

    if (window.__videoPrefetch) {
      var entry = window.__videoPrefetch.cache[url];
      if (entry && entry.ready && entry.blobUrl) {
        playback = entry.blobUrl;
      }
    }

    var bufferSize = lengthBytesUTF8(playback) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(playback, buffer, bufferSize);
    return buffer;
  },

  WebGLVideoPrefetch_Free: function (ptr) {
    _free(ptr);
  }

});
