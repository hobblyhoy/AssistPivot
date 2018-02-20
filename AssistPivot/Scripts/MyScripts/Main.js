var ViewModel = function () {
    var self = this;
    window.uw = ko.unwrap;

    //Init
    self.colleges = ko.observableArray();    
    self.selectedCollege = ko.observable();

    self.years = ko.observableArray();
    self.selectedYear = ko.observable();

    self.courses = ko.observableArray();    
    self.selectedCourse = ko.observable();

    assistHelper.test();
    //initial load request for our list of Colleges
    $.ajax({
        url: "/api/College"
        , method: "GET"
        , dataType: "json"
    }).done(function (ret) {
        console.log(ret.Data);
        self.colleges(ret.Data);
    }).fail(function () {
        alert("College Get() fail");
    });

    //initial load request for our list of Years
    $.ajax({
        url: "/api/Year"
        , method: "GET"
        , dataType: "json"
    }).done(function (ret) {
        console.log(ret.Data);
        self.years(ret.Data);
    }).fail(function () {
        alert("Year Get() fail");
    });


    //request for college courses
    self.courseRequest = function() {
        if (!uw(self.selectedCollege)) return;

        $.ajax({
            url: "/api/College?id=" + uw(self.selectedCollege)
            , method: "GET"
            , dataType: "json"
        }).done(function (ret) {
            console.log(ret.Data);
            self.courses(ret.Data);
        }).fail(function () {
            alert("Course Get() fail");
        });
    };

    //handle new college selection
    self.selectedCollege.subscribe(function() {
        self.courses.removeAll();
        self.selectedCourse(null);
        self.courseRequest();
    });

    //The real meat- request assist data
    self.processCourse = function() {
        if (!uw(self.selectedCourse)) return;

        $.ajax({
            url: "/api/Assist?" 
                + "CollegeId=" + uw(self.selectedCourse).CollegeId
                + "&CourseId=" + uw(self.selectedCourse).CourseId
            , method: "GET"
            , dataType: "json"
        }).done(function (ret) {
            console.log(ret);
        }).fail(function () {
            alert("Assist Request fail");
        });
    }

};

var vmEval = new ViewModel();
window.assistDebug = vmEval;
ko.applyBindings(vmEval);