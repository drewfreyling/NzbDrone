﻿/// <reference path="../app.js" />
/// <reference path="ErrorModel.js" />

NzbDrone.Shared.ErrorItemView = Backbone.Marionette.ItemView.extend({
    template: "Shared/ErrorTemplate",
});

NzbDrone.Shared.ErrorView = Backbone.Marionette.CollectionView.extend({

    itemView: NzbDrone.Shared.ErrorItemView,

    initialize: function () {

        this.collection = new NzbDrone.Shared.ErrorCollection();
        this.listenTo(this.collection, 'reset', this.render);
    },
});



NzbDrone.addInitializer(function (options) {

    console.log("initializing error handler");

    NzbDrone.Shared.ErrorView.instance = new NzbDrone.Shared.ErrorView();

    NzbDrone.errorRegion.show(NzbDrone.Shared.ErrorView.instance);

});


window.onerror = function (msg, url, line) {

    var errorView = NzbDrone.Shared.ErrorView.instance;

    if (errorView) {
        var model = new NzbDrone.Shared.ErrorModel();

        var a = document.createElement('a');
        a.href = url;

        model.set('title', a.pathname.split('/').pop() + " : " + line);
        model.set('message', msg);
        errorView.collection.add(model);
    } else {
        alert("Error: " + msg + "\nurl: " + url + "\nline #: " + line);
    }

    var suppressErrorAlert = false;
    // If you return true, then error alerts (like in older versions of 
    // Internet Explorer) will be suppressed.
    return suppressErrorAlert;
};

$(document).ajaxError(function (event, XMLHttpRequest, ajaxOptionsa) {

    var errorView = NzbDrone.Shared.ErrorView.instance;

    var model = new NzbDrone.Shared.ErrorModel();
    model.set('title', ajaxOptionsa.url + " : " + XMLHttpRequest.statusText);
    model.set('message', XMLHttpRequest.responseText);
    errorView.collection.add(model);

    var suppressErrorAlert = false;
    // If you return true, then error alerts (like in older versions of 
    // Internet Explorer) will be suppressed.
    return suppressErrorAlert;
});