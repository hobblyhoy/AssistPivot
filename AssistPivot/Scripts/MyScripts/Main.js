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

    self.updateRequest = ko.observable(false);
    self.collegeYearStatuses = ko.observableArray();
    self.selectedCollegeYearStatus = ko.computed(function() {
        //if (self.collegeYearStatuses.length === 0 || !uw(self.selectedCollege) || !uw(self.selectedYear)) return;
        if (!uw(self.selectedCollege) || !uw(self.selectedYear)) return;

        //var debugCollegeYearStatuses = [{UpToDateAsOf: "0-1", College_CollegeId: 1, Year_YearId: 1}, {UpToDateAsOf: "1-2", College_CollegeId: 1, Year_YearId: 2} ];
        var ret = _(uw(self.collegeYearStatuses)).find(function(status) {
            return status.College_CollegeId === uw(self.selectedCollege).CollegeId
                && status.Year_YearId === uw(self.selectedYear).YearId;
        });
        if (!ret) {
            return {UpToDateAsOf: "Never"};
        }
        return ret;
    });

    //initial load request for our list of Colleges
    assistHelper.request('College')
    .done(function (ret) {
        self.colleges(ret.Data);
    });

    //initial load request for our list of Years
    assistHelper.request('Year')
    .done(function (ret) {
        self.years(ret.Data);
    });

    //Initial load request for our College-Year status sheet
    assistHelper.request('CollegeYearStatus')
    .done(function (ret) {
        self.collegeYearStatuses(ret.Data);
    });




    //handle new college selection
    self.selectedCollege.subscribe(function() {
        self.courses.removeAll();
        self.selectedCourse(null);
        //self.courseRequest();
    });

    //The real meat- request assist data
    self.process = function() {
        var queryObj = {
            collegeId: uw(self.selectedCollege).CollegeId
            ,yearId: uw(self.selectedYear).YearId
            ,updateRequest: uw(self.updateRequest)
        }
        assistHelper.request('Assist', queryObj)
        .done(function (ret) {
            //console.log(ret);
            //debugger;
            //todo update CollegeYearStatus
        });
    }

};

var vmEval = new ViewModel();
window.assistDebug = vmEval;
ko.applyBindings(vmEval);