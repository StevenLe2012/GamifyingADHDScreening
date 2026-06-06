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
      notifyAll(0);
    });

    window.addEventListener("resize", function () {
      if (!document.hidden) notifyAll(0);
    });
  }

});
