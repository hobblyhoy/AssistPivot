var ViewModel = function () {
    var self = this;
    window.uw = ko.unwrap;

    //Init
    self.notificationText = ko.observable("Initial load in progress, please wait");
    self.notificationType = ko.observable("Load"); // None / Notice / Error / Load

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

    // Initial load request for our list of Colleges
    self.collegesLoading = ko.observable(true);
    assistHelper.request('College', {}, self.collegesLoading)
    .done(function (ret) {
        self.colleges(ret.Data);
    });

    // Initial load request for our list of Years
    self.yearsLoading = ko.observable(true);
    assistHelper.request('Year', {}, self.yearsLoading)
    .done(function (ret) {
        var sorted = _.chain(ret.Data)
            .sortBy(function(year) {
                return year.FirstYearExpanded;
            })
            .reverse()
            .value();
        self.years(sorted);
    });

    //Initial load request for our College-Year status sheet
    self.collegeYearStatusesLoading = ko.observable(true);
    assistHelper.request('CollegeYearStatus', {}, self.collegeYearStatusesLoading)
    .done(function (ret) {
        var data = ret.Data;
        var forceNoUpdates = _(data).some(function(status) {
            return status.UpdateAllowed === "AbsolutelyNot";
        });
        if (forceNoUpdates) {
            _(data).foreach(function(status) {
                status.UpdateAllowed = "No";
            });
            self.notificationType("Notice");
            self.notificationText("A college update request is in progress. You will only be able to retrieve cached data until this completes")
        }
        self.collegeYearStatuses(data);
    });

    self.initialLoadIsLoading = ko.computed(function() {
        return uw(self.collegesLoading) || uw(self.yearsLoading) || uw(self.collegeYearStatusesLoading);
    });

    self.initialLoadTextUpdater = ko.computed(function() {
        if (!uw(self.initialLoadIsLoading)) {
            self.notificationType(null);
            self.notificationText(null);
        }
    });



    //handle new college selection
    self.selectedCollege.subscribe(function() {
        self.courses.removeAll();
        self.selectedCourse(null);
        //self.courseRequest();
    });

    //The real meat- request assist data
    self.processLoading = ko.observable(false);
    self.process = function() {
        self.processLoading(true);
        var queryObj = {
            collegeId: uw(self.selectedCollege).CollegeId
            ,yearId: uw(self.selectedYear).YearId
            ,updateRequest: uw(self.updateRequest)
        }
        assistHelper.request('Assist', queryObj, self.processLoading)
        .done(function (ret) {
            //self.collegeYearStatuses(ret.Data);
            //todo update CollegeYearStatus
        });
    }

};

var vmEval = new ViewModel();
window.assistDebug = vmEval;
ko.applyBindings(vmEval);