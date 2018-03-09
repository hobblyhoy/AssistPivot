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

    self.statusMap = function(statusObj) {
        if (statusObj.UpToDateAsOf) {
            statusObj.UpToDateAsOf = new Date(statusObj.UpToDateAsOf);
            statusObj.UpToDateAsOfPretty = function() { return this.UpToDateAsOf.toLocaleString() };
        } else {
            statusObj.UpToDateAsOfPretty = function() { return 'Never' };
        }

        statusObj.NoUpdateReason = '';
        if (statusObj.UpdateAllowed === 'No') {
            statusObj.NoUpdateReason = "This course has been recently updated."
        } else if (statusObj.UpdateAllowed === 'AbsolutelyNot') {
            statusObj.NoUpdateReason = "No colleges may be updated while an update is already in progress. Try again later."
        }
    }
    self.collegeYearStatuses = ko.observableArray();
    self.selectedCollegeYearStatus = ko.computed(function() {
        var statuses = uw(self.collegeYearStatuses);
        var selectedCollege = uw(self.selectedCollege);
        var selectedYear = uw(self.selectedYear);
        if (statuses.length === 0 || !selectedCollege || !selectedYear) return;

        var ret = _(statuses).find(function(statusObj) {
            return statusObj.CollegeId === selectedCollege.CollegeId
                && statusObj.YearId === selectedYear.YearId;
        });
        if (!ret) {
            ret = {};
            ret.UpdateAllowed 
                    = (statuses[0].UpdateAllowed === 'AbsolutelyNot')
                    ? 'AbsolutelyNot'
                    : 'Force'
            //ret = {UpToDateAsOfPretty: function() {return 'Never'}, UpdateAllowed: updateAllowed};
        }
        self.statusMap(ret);
        return ret;
    });
    self.updateCheckboxValue = ko.observable(false);
    self.updateCheckboxDisabled = ko.computed(function() {
        var statusObj = uw(self.selectedCollegeYearStatus)
        if (!statusObj) return;
        switch (statusObj.UpdateAllowed) {
            case "Force":
                self.updateCheckboxValue(true);
                return true;
            case "No":
            case "AbsolutelyNot":
                self.updateCheckboxValue(false);
                return true;
            default:
                self.updateCheckboxValue(false);
                return false;
        }
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
        var forceNoUpdates = _(data).some(function(statusObj) {
            return statusObj.UpdateAllowed === "AbsolutelyNot";
        });
        if (forceNoUpdates) {
            _(data).forEach(function(statusObj) {
                statusObj.UpdateAllowed = "AbsolutelyNot";
            });
            self.notificationType("Notice");
            self.notificationText("A college update request is in progress. You will only be able to retrieve cached data until this completes")
        }

        _(data).forEach(function(statusObj) {
            self.statusMap(statusObj);
        });

        self.collegeYearStatuses(data);
    });


    self.initialLoadIsLoading = ko.computed(function() {
        return uw(self.collegesLoading) || uw(self.yearsLoading) || uw(self.collegeYearStatusesLoading);
    });

    self.initialLoadTextUpdater = ko.computed(function() {
        if (!uw(self.initialLoadIsLoading) && self.notificationType.peek() == "Load") {
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
        //self.updateCheckboxValue(false);
    });
    self.selectedYear.subscribe(function() {
        self.resetCourses();
        //self.updateCheckboxValue(false);
    });


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

    self.tidyUpCourseName = function(courseName) {
        var ret = courseName.replace("University of California", "UC")
                            .replace("California State University", "CSU")
                            .replace("California Polytechnic University", "CPU")
                            .replace("State University", "SU")
                            .replace(" Community College", "")
                            .replace("College of ", "");
        if (ret.substring(ret.length-8) === " College") {
            ret = ret.substring(0, ret.length-8);
        }
        return ret;
    }

    //Request assist data
    self.processLoading = ko.observable(false);
    self.process = function() {
        self.processLoading(true);
        if (uw(self.updateCheckboxValue)) {
            self.notificationText('Okay, we\'re fetching this data from Assist now. This can take up to an hour to complete. You do not need to stay on this page. Once we have the data stored you can view it at any time.');
            self.notificationType('Load');
        }
        var queryObj = {
            collegeId: uw(self.selectedCollege).CollegeId
            ,yearId: uw(self.selectedYear).YearId
            ,updateRequest: uw(self.updateCheckboxValue)
        }
        assistHelper.request('Assist', queryObj, self.processLoading)
        .done(function (ret) {
            var data = ret.Data;
            //A ltitle helper mapping
            _(data.CourseRelationships).forEach(function(rela) {
                rela.FromCollegeName = self.tidyUpCourseName(rela.FromCourseSet.College.Name);
                rela.ToCollegeName = self.tidyUpCourseName(rela.ToCourseSet.College.Name);
            });

            // Write the returned data to our obs
            if (data.Courses && data.Courses.length) {
                self.courses(data.Courses.sort());
            }
            if (data.CourseRelationships && data.CourseRelationships.length) {
                self.courseRelationships(data.CourseRelationships);
            }

            // Display any notification text that came down
            if (data.NotificationText && data.NotificationType) {
                self.notificationText(data.NotificationText);
                self.notificationType(self.noticeTypeEnumToString(data.NotificationType));
            }

            // If this is a full update we'll get back an updated collegeYearStatus
            if (data.Status) {
                var currentStatus = _(uw(self.collegeYearStatuses)).find(function(statusObj) {
                    return statusObj.CollegeYearStatusId === data.Status.CollegeYearStatusId;
                });
                self.collegeYearStatuses.replace(currentStatus, data.Status);
            }
        });
    }

    //course Relationships filtered down to the particular course selected
    self.courseRelationshipsFromSelected = ko.computed(function() {
        var relas = uw(self.courseRelationships);
        var selected = uw(self.selectedCourse);
        if (!relas || relas.length === 0 || !selected) return [];

        var ret = _(relas).filter(function(rela) {
            var relevantCourseSet 
                    = (rela.FromCourseSet.College.CollegeId === uw(self.selectedCollege).CollegeId)
                    ? rela.FromCourseSet 
                    : rela.ToCourseSet;
            return relevantCourseSet.CommaDelimitedCourseNames.indexOf(selected) > -1;
        }).value();
        //ret.unshift(self.exampleCourseRela);
        return ret;
    });

};

var vmEval = new ViewModel();
window.assistDebug = vmEval;
ko.applyBindings(vmEval);