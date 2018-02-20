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

    self.collegeYearStatuses = ko.observableArray();     

    //initial load request for our list of Colleges
    // $.ajax({
    //     url: "/api/College"
    //     , method: "GET"
    //     , dataType: "json"
    // })
    assistHelper.conReq('College')
    .done(function (ret) {
        self.colleges(ret.Data);
    });

    //initial load request for our list of Years
    assistHelper.conReq('Year')
    .done(function (ret) {
        self.years(ret.Data);
    });

    //Initial load request for our College-Year status sheet
    assistHelper.conReq('CollegeYearStatus')
    .done(function (ret) {
        self.collegeYearStatuses(ret.Data);
    });


    //request for college courses
    // self.courseRequest = function() {
    //     if (!uw(self.selectedCollege)) return;

    //     assistHelper.conReq('College', {id: uw(self.selectedCollege)})
    //     .done(function (ret) {
    //         console.log(ret.Data);
    //         self.courses(ret.Data);
    //     })
    //     .fail(function () {
    //         alert("Course Get() fail");
    //     });
    // };

    //handle new college selection
    self.selectedCollege.subscribe(function() {
        self.courses.removeAll();
        self.selectedCourse(null);
        //self.courseRequest();
    });

    //The real meat- request assist data
    self.processCourse = function() {
        if (!uw(self.selectedCourse)) return;

        assistHelper.conReq('Assist', 
                {CollegeId: uw(self.selectedCourse).CollegeId}
                ,{CourseId: uw(self.selectedCourse).CourseId})
        .done(function (ret) {
            console.log(ret);
        })
        .fail(function () {
            alert("Assist Request fail");
        });
    }

};

var vmEval = new ViewModel();
window.assistDebug = vmEval;
ko.applyBindings(vmEval);