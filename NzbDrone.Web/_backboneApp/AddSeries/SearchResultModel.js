﻿/// <reference path="../app.js" />
/// <reference path="RootDir/RootDirCollection.js" />
/// <reference path="../Quality/qualityProfileCollection.js" />
NzbDrone.AddSeries.SearchResultModel = Backbone.Model.extend({
    mutators: {
        seriesYear: function () {
            var date = Date.utc.create(this.get('firstAired')).format('({yyyy})');

            //don't append year, if the series name already has the name appended.
            if (this.get('seriesName').endsWith(date)) {
                return "";
            } else {
                return date;
            }
        }
    },

    defaults: {
        qualityProfiles: new NzbDrone.Quality.QualityProfileCollection(),
        rootFolders: new NzbDrone.AddSeries.RootDirCollection()
    }

});
