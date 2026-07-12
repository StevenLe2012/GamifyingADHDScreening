mergeInto(LibraryManager.library, {

  WebGLIntroBridge_Setup: function (gameObjectNamePtr) {
    var goName = UTF8ToString(gameObjectNamePtr);
    if (window.__umakiIntroBridgeSetup) return;
    window.__umakiIntroBridgeSetup = true;
    window.__umakiIntroBridgeArmed = 0;
    window.__umakiUserGesturePending = 0;

    function deliverGesture(e) {
      if (!window.__umakiIntroBridgeArmed) return;
      if (window.__umakiUserGesturePending) return;
      if (e && e.type === "keydown") {
        var k = e.key;
        var code = e.code;
        if (k !== "Enter" && k !== " " && code !== "NumpadEnter") return;
      }
      window.__umakiUserGesturePending = 1;

      var hint = document.getElementById("unity-start-hint");
      if (hint) hint.style.display = "none";

      if (window.__unityInstance && window.__unityInstance.SendMessage) {
        try {
          window.__unityInstance.SendMessage(goName, "OnUserGestureFromPage", "");
        } catch (e) {}
      }
    }

    document.addEventListener("pointerdown", deliverGesture, { passive: true });
    document.addEventListener("touchstart", deliverGesture, { passive: true });
    document.addEventListener("keydown", deliverGesture, { passive: true });
  },

  WebGLIntroBridge_Arm: function () {
    window.__umakiIntroBridgeArmed = 1;
    window.__umakiUserGesturePending = 0;
  },

  WebGLIntroBridge_Disarm: function () {
    window.__umakiIntroBridgeArmed = 0;
    window.__umakiUserGesturePending = 0;
  },

  WebGLIntroBridge_Reset: function () {
    window.__umakiUserGesturePending = 0;
  },

  WebGLIntroBridge_ConsumeGesture: function () {
    if (window.__umakiUserGesturePending) {
      window.__umakiUserGesturePending = 0;
      return 1;
    }
    return 0;
  },

  WebGLIntroBridge_HidePageStartHint: function () {
    var hint = document.getElementById("unity-start-hint");
    if (hint) hint.style.display = "none";
  },

  WebGLIntroBridge_ShowPageStartHint: function () {
    var hint = document.getElementById("unity-start-hint");
    if (hint) hint.style.display = "block";
  }

});
