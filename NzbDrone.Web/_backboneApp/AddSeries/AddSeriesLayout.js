﻿/// <reference path="../app.js" />
/// <reference path="AddNewSeries/AddNewSeriesView.js" />
/// <reference path="RootDir/RootDirView.js" />
/// <reference path="../Quality/qualityProfileCollection.js" />
/// <reference path="../Shared/SpinnerView.js" />
/// <reference path="ImportExistingSeries/ImportSeriesView.js" />

NzbDrone.AddSeries.AddSeriesLayout = Backbone.Marionette.Layout.extend({
    template: "AddSeries/addSeriesLayoutTemplate",

    regions: {
        addNew: "#add-new",
        importExisting: "#import-existing",
        rootFolders: "#root-folders"
    },

    ui: {
        addNewTab: ".nav-tabs a[href='#add-new']",
        importTab: ".nav-tabs a[href='#import-existing']",
        rootDirTab: ".nav-tabs a[href='#root-folders']",
        rootTabRequiredMessage: "",
    },


    rootFolderCollection: new NzbDrone.AddSeries.RootDirCollection(),
    qualityProfileCollection: new NzbDrone.Quality.QualityProfileCollection(),

    onRender: function () {
        this.$('#myTab a').click(function (e) {
            e.preventDefault();
            $(this).tab('show');
        });

        this.qualityProfileCollection.fetch();

        this.addNew.show(new NzbDrone.AddSeries.AddNewSeriesView({ rootFolders: this.rootFolderCollection, qualityProfiles: this.qualityProfileCollection }));
        this.importExisting.show(new NzbDrone.AddSeries.ExistingFolderListView({ collection: this.rootFolderCollection }));
        this.rootFolders.show(new NzbDrone.AddSeries.RootDirView({ collection: this.rootFolderCollection }));

        NzbDrone.vent.listenTo(this.rootFolderCollection, 'add', this.evaluateActions, this);
        NzbDrone.vent.listenTo(this.rootFolderCollection, 'remove', this.evaluateActions, this);
        NzbDrone.vent.listenTo(this.rootFolderCollection, 'reset', this.evaluateActions, this);
    },

    evaluateActions: function () {
        if (this.rootFolderCollection.length == 0) {
            this.ui.addNewTab.hide();
            this.ui.importTab.hide();
            this.ui.rootDirTab.tab('show');
        } else {
            this.ui.addNewTab.show();
            this.ui.importTab.show();
        }
    },


});