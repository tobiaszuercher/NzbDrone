'use strict';

define(
    [
        'backbone',
        'Quality/ProfileModel'
    ], function (Backbone, ProfileModel) {

        return Backbone.Collection.extend({
            model: ProfileModel,
            url  : window.NzbDrone.ApiRoot + '/profile/schema'
        });
    });
