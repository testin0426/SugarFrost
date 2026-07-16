mergeInto(LibraryManager.library, {

  naninovelSyncFs: function () {
    FS.syncfs(false, function (err) { });
  },

  naninovelOpenWindow: function (url, target) {
    window.open(UTF8ToString(url), UTF8ToString(target));
  }

});
