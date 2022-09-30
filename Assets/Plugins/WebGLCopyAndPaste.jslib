var WebGLCopyAndPaste = {
  $WebGLCopyAndPaste: {},

  initWebGLCopyAndPaste__postset: '_initWebGLCopyAndPaste();',

  initWebGLCopyAndPaste: function () {
    // for some reason only on Safari does Unity call
    // preventDefault so let's prevent preventDefault
    // so the browser will generate copy and paste events
    window.addEventListener = function (origFn) {
      function noop() {
      }

      // I hope c,x,v are universal
      const keys = {'c': true, 'x': true, 'v': true};

      // Emscripten doesn't support the spread operator or at
      // least the one used by Unity 2019.4.1
      return function (name, fn) {
        const args = Array.prototype.slice.call(arguments);
        if (name !== 'keypress') {
          return origFn.apply(this, args);
        }
        args[1] = function (event) {
          const hArgs = Array.prototype.slice.call(arguments);
          if (keys[event.key.toLowerCase()] &&
              ((event.metaKey ? 1 : 0) + (event.ctrlKey ? 1 : 0)) === 1) {
            event.preventDefault = noop;
          }
          return fn.apply(this, hArgs);
        };
        return origFn.apply(this, args);
      };
    }(window.addEventListener);

    _initWebGLCopyAndPaste = function (cutCopyFuncPtr, pasteFuncPtr) {

      function sendStringCallback (callback, str) {
        var bufferSize = lengthBytesUTF8(str) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(str, buffer, bufferSize);
        if (typeof Module !== undefined && Module.dynCall_vi) {
          Module.dynCall_vi(callback, buffer);
        } else {
          Runtime.dynCall('vi', callback, [buffer]);
        }
      }

      WebGLCopyAndPaste.data =
          WebGLCopyAndPaste.data || {
            initialized: false,
            cutCopyFunc: cutCopyFuncPtr,
            pasteFunc: pasteFuncPtr,
          };
      const g = WebGLCopyAndPaste.data;

      if (!g.initialized) {
        window.addEventListener('cut', function (e) {
          e.preventDefault();
          sendStringCallback(g.cutCopyFunc, 'x');
          event.clipboardData.setData('text/plain', g.clipboardStr);
        });
        window.addEventListener('copy', function (e) {
          e.preventDefault();
          sendStringCallback(g.cutCopyFunc, 'c');
          event.clipboardData.setData('text/plain', g.clipboardStr);
        });
        window.addEventListener('paste', function (e) {
          const str = e.clipboardData.getData('text');
          sendStringCallback(g.pasteFunc, str);
        });
      }
    };
  },

  passCopyToBrowser: function (stringPtr) {
    var fn = typeof UTF8ToString === 'function' ? UTF8ToString : Pointer_stringify;
    WebGLCopyAndPaste.data.clipboardStr = fn(stringPtr);
  },
};

autoAddDeps(WebGLCopyAndPaste, '$WebGLCopyAndPaste');
mergeInto(LibraryManager.library, WebGLCopyAndPaste);
