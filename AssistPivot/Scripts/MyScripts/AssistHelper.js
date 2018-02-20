var self = this;

self.conReq = function(controller, queryStringObj) {
	var url = "/api/" + controller;
	if (queryStringObj) {
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
        alert("Failed request to " + url);
    }).done(function(ret) {
    	//debug
    	console.log('Data came back from ' + url);
    	console.log(ret.Data);
    });
};


window.assistHelper = self;