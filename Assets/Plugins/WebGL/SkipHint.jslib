mergeInto(LibraryManager.library, {

  SkipHint_Show: function (messagePtr) {
    var message = UTF8ToString(messagePtr);

    var el = document.getElementById('umaki-skip-hint');
    if (!el) {
      el = document.createElement('div');
      el.id = 'umaki-skip-hint';
      el.style.position = 'fixed';
      el.style.left = '50%';
      el.style.bottom = '28px';
      el.style.transform = 'translateX(-50%)';
      el.style.background = 'rgba(0, 0, 0, 0.65)';
      el.style.color = '#ffffff';
      el.style.padding = '10px 22px';
      el.style.borderRadius = '8px';
      el.style.fontFamily = 'sans-serif';
      el.style.fontSize = '16px';
      el.style.lineHeight = '1.4';
      el.style.textAlign = 'center';
      el.style.zIndex = '2147483647';
      el.style.pointerEvents = 'none';

      // Append inside #unity-container (not document.body): that's the element the game
      // fullscreens (see WebGLFullscreen.jslib / index.html), and the Fullscreen API only
      // paints that element's subtree — anything outside it is hidden while fullscreen.
      var host = document.getElementById('unity-container') || document.body;
      host.appendChild(el);
    }

    el.textContent = message;
    el.style.display = 'block';
  },

  SkipHint_Hide: function () {
    var el = document.getElementById('umaki-skip-hint');
    if (el) el.style.display = 'none';
  }

});
