var self = this;

self.test = function() {
	console.log('gotcha');
	var asdf =_.partition([1, 2, 3, 4], n => n % 2);
    console.log(asdf);
}


window.assistHelper = self;