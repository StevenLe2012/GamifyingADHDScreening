mergeInto(LibraryManager.library, {

  UnityPageVisibility_Register: function (gameObjectNamePtr, methodNamePtr) {
    var go = UTF8ToString(gameObjectNamePtr);
    var method = UTF8ToString(methodNamePtr);

    if (!window.__unityPageVisibilityHandlers) {
      window.__unityPageVisibilityHandlers = [];
    }

    for (var i = 0; i < window.__unityPageVisibilityHandlers.length; i++) {
      var h = window.__unityPageVisibilityHandlers[i];
      if (h.go === go && h.method === method) return;
    }

    window.__unityPageVisibilityHandlers.push({ go: go, method: method });

    if (window.__unityPageVisibilityListenerAdded) return;
    window.__unityPageVisibilityListenerAdded = true;

    function getUnityInstance() {
      if (window.__unityInstance) return window.__unityInstance;
      if (typeof unityInstance !== "undefined") return unityInstance;
      if (typeof gameInstance !== "undefined") return gameInstance;
      return null;
    }

    function sendToUnity(targetGo, targetMethod, arg) {
      var inst = getUnityInstance();
      if (inst && inst.SendMessage) {
        inst.SendMessage(targetGo, targetMethod, arg);
        return;
      }
      if (typeof Module !== "undefined" && Module.SendMessage) {
        Module.SendMessage(targetGo, targetMethod, arg);
      }
    }

    function notifyAll(hidden) {
      var handlers = window.__unityPageVisibilityHandlers || [];
      for (var j = 0; j < handlers.length; j++) {
        sendToUnity(handlers[j].go, handlers[j].method, hidden);
      }
    }

    document.addEventListener("visibilitychange", function () {
      notifyAll(document.hidden ? 1 : 0);
    });

    // Window restore / maximize often fires resize without visibilitychange.
    window.addEventListener("focus", function () {
      cancelPendingBlurHide();
      notifyAll(0);
    });

    // Losing OS focus (e.g. a second monitor, or another window partially
    // overlapping this one) doesn't always flip document.hidden, so
    // visibilitychange alone can miss it — that's what this is for. But blur
    // also fires for brief, harmless browser-chrome interactions (clicking the
    // address bar, Ctrl+F, an extension popup) where the page stays fully
    // visible the whole time. So debounce it: wait BLUR_HIDE_DELAY_MS and only
    // report "hidden" if focus still hasn't returned by then. A quick
    // blur-then-refocus (address bar click) never fires notifyAll at all; a
    // sustained focus loss (switching to another window/monitor) still does.
    var BLUR_HIDE_DELAY_MS = 400;
    var blurHideTimer = null;

    function cancelPendingBlurHide() {
      if (blurHideTimer !== null) {
        clearTimeout(blurHideTimer);
        blurHideTimer = null;
      }
    }

    window.addEventListener("blur", function () {
      cancelPendingBlurHide();
      blurHideTimer = setTimeout(function () {
        blurHideTimer = null;
        if (!document.hasFocus()) {
          notifyAll(1);
        }
      }, BLUR_HIDE_DELAY_MS);
    });

    window.addEventListener("resize", function () {
      if (!document.hidden) notifyAll(0);
    });
  }

});
