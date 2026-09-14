mergeInto(LibraryManager.library, {

  WebGLRedirect_Go: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    if (!url) return;
    window.location.href = url;
  }

});
