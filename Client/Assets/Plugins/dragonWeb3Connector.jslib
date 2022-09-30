
mergeInto(LibraryManager.library, {
  // initialize web3 object 
  drgweb3_init: function () {
    window.dragon.initialize()
  },

  browserAlert: function(msg) {
	window.alert(UTF8ToString(msg));
  }

});
