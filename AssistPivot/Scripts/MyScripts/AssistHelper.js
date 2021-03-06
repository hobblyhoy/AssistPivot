﻿var self = this;

self.request = function(controller, queryStringObj, loadingFunc) {
	var url = "/api/" + controller;
	if (queryStringObj && !_.isEmpty(queryStringObj)) {
		url += "?";
		_(queryStringObj).forEach(function(value, key) {
			url += key + '=' + value + '&';
		});
		url = url.slice(0, -1);
	};

	console.log('Request to:', url);
	return $.ajax({
        url: url
        , method: "GET"
        , dataType: "json"
    }).fail(function () {
    	// Occasionally balks at the super long request w/ everything still working fine server-side.
    	// until I can put in a websocket or websocket-esque tech for more stable connection, just let it fail silently
        // alert("Failed request to " + url);
        console.log("Failed request to " + url);
    }).done(function(ret) {
    	//debug
    	console.log('Data came back from ' + url);
    	console.log(ret.Data);
    	loadingFunc(false);
    });
};


window.assistHelper = self;