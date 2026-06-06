mergeInto(LibraryManager.library, {

  WebGLFullscreen_Request: function () {
    var el = document.getElementById("unity-container") || document.documentElement;
    var req = el.requestFullscreen ||
              el.webkitRequestFullscreen ||
              el.msRequestFullscreen ||
              el.mozRequestFullScreen;
    if (req) {
      try {
        var p = req.call(el);
        if (p && p.catch) p.catch(function () {});
      } catch (e) {}
    }

    if (window.__unityInstance && window.__unityInstance.SetFullscreen) {
      try {
        window.__unityInstance.SetFullscreen(1);
      } catch (e2) {}
    }
  },

  WebGLFullscreen_IsActive: function () {
    return (document.fullscreenElement ||
            document.webkitFullscreenElement ||
            document.msFullscreenElement ||
            document.mozFullScreenElement) ? 1 : 0;
  }

});
