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

    self.courseRelationships = ko.observableArray();

    self.collegeYearStatuses = ko.observableArray();
    self.selectedCollegeYearStatus = ko.computed(function() {
        var statuses = uw(self.collegeYearStatuses);
        var selectedCollege = uw(self.selectedCollege);
        var selectedYear = uw(self.selectedYear);
        if (statuses.length === 0 || !selectedCollege || !selectedYear) return;

        var ret = _(statuses).find(function(status) {
            return status.CollegeId === selectedCollege.CollegeId
                && status.YearId === selectedYear.YearId;
        });
        if (!ret) {
            return {UpToDateAsOf: "Never", UpdateAllowed: "Force"};
        }
        return ret;
    });
    self.updateCheckboxValue = ko.observable(false);
    self.updateCheckboxDisabled = ko.computed(function() {
        var status = uw(self.selectedCollegeYearStatus)
        if (!status) return;
        switch (status.UpdateAllowed) {
            case "Force":
            case "No":
                return true;
            default:
                return false;
        }
    });
    self.selectedCollegeYearStatus.subscribe(function(status) {
        if (!status) return;
        if (status.UpdateAllowed === "Force") self.updateCheckboxValue(true);
        if (status.UpdateAllowed === "No") self.updateCheckboxValue(false);
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


    // Initial load request for our College-Year status sheet
    self.collegeYearStatusesLoading = ko.observable(true);
    assistHelper.request('CollegeYearStatus', {}, self.collegeYearStatusesLoading)
    .done(function (ret) {
        var data = ret.Data;
        var forceNoUpdates = _(data).some(function(status) {
            return status.UpdateAllowed === "AbsolutelyNot";
        });
        if (forceNoUpdates) {
            _(data).forEach(function(status) {
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
        if (!uw(self.initialLoadIsLoading) && uw(self.notificationType) == "Load") {
            self.notificationType(null);
            self.notificationText(null);
        }
    });


    //handle new college/year selection
    self.resetCourses = function() {
        self.courses.removeAll();
        self.selectedCourse(null);
    }
    self.selectedCollege.subscribe(function() {
        self.resetCourses();
    });
    self.selectedYear.subscribe(function() {
        self.resetCourses();
    });


    self.relaSeperatorEnumToString = function(enumInt) {
        switch (enumInt) {
            case 0:
            case 1:
                return "";
            case 2:
                return "AND";
            case 3:
                return "OR";
        }
    };

    self.noticeTypeEnumToString = function (enumInt) {
        switch (enumInt) {
            case 0:
                return "";
            case 1:
                return "Notice";
            case 2:
                return "Error";
        }
    }

    //Request assist data
    self.processLoading = ko.observable(false);
    self.process = function() {
        self.processLoading(true);
        var queryObj = {
            collegeId: uw(self.selectedCollege).CollegeId
            ,yearId: uw(self.selectedYear).YearId
            ,updateRequest: uw(self.updateCheckboxValue)
        }
        assistHelper.request('Assist', queryObj, self.processLoading)
        .done(function (ret) {
            //A ltitle helper mapping
            _(ret.Data.CourseRelationships).forEach(function(rela) {
                rela.FromRelationshipSeperatorText = self.relaSeperatorEnumToString(rela.FromRelationshipType);
                rela.ToRelationshipSeperatorText = self.relaSeperatorEnumToString(rela.ToRelationshipType);
                rela.ToCollegeName = rela.ToCourses[0].College.Name;
            });

            // Write the returned data to our obs
            self.courses(ret.Data.Courses);
            self.courseRelationships(ret.Data.CourseRelationships);

            // Display any notification text that came down
            if (ret.Data.NotificationText && ret.Data.NotificationType) {
                self.notificationText(ret.Data.NotificationText);
                self.notificationType(self.noticeTypeEnumToString(ret.Data.NotificationType))
            }
        });
    }

    //course Relationships filtered down to the particular course selected
    self.courseRelationshipsFromSelected = ko.computed(function() {
        var relas = uw(self.courseRelationships);
        var selected = uw(self.selectedCourse);
        if (!relas || relas.length === 0 || !selected) return [];

        var ret = _(relas).filter(function(rela) {
            var fromCourseIds = _(rela.FromCourses).map(function(fromCourse) {
                return fromCourse.CourseId;
            });
            return _(fromCourseIds).indexOf(selected.CourseId) > -1;
        }).value();
        ret.unshift(self.exampleCourseRela);
        return ret;
    });

    self.exampleCourseRela = {
        CourseRelationshipId: null
        , FromCourses: [
            {
                College: {Name: "Demo College 1", Shorthand: "DC1"}
                , Credits: 3
                , Description: "Course Description 1 Goes Here"
                , Name: "Course 100"
            },{
                College: {Name: "Demo College 1", Shorthand: "DC1"}
                , Credits: 3
                , Description: "Course Description 2 Goes Here"
                , Name: "Course 101"
            }
        ]
        , FromRelationshipType: 1
        , FromRelationshipSeperatorText: "AND"
        , ToCourses: [
            {
                College: {Name: "Demo College 2", Shorthand: "DC2"}
                , Credits: 2
                , Description: "Course Description 3 Goes Here"
                , Name: "Course 200"
            },{
                College: {Name: "Demo College 2", Shorthand: "DC2"}
                , Credits: 2
                , Description: "Course Description 4 Goes Here"
                , Name: "Course 201"
            },{
                College: {Name: "Demo College 2", Shorthand: "DC2"}
                , Credits: 2
                , Description: "Course Description 5 Goes Here"
                , Name: "Course 202"
            }
        ]
        , ToRelationshipType: 2
        , ToRelationshipSeperatorText: "OR"
        , ToCollegeName: "Demo College 2"
        , UpToDateAsOf: null
    }

};

var vmEval = new ViewModel();
window.assistDebug = vmEval;
ko.applyBindings(vmEval);