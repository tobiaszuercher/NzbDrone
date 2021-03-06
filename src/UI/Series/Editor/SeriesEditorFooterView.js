'use strict';
define(
    [
        'underscore',
        'marionette',
        'backgrid',
        'vent',
        'Quality/QualityProfileCollection',
        'AddSeries/RootFolders/RootFolderCollection',
        'Shared/Toolbar/ToolbarLayout',
        'AddSeries/RootFolders/RootFolderLayout',
        'Series/Editor/Organize/OrganizeFilesView',
        'Config'
    ], function (_,
                 Marionette,
                 Backgrid,
                 vent,
                 QualityProfiles,
                 RootFolders,
                 ToolbarLayout,
                 RootFolderLayout,
                 UpdateFilesSeriesView,
                 Config) {
        return Marionette.ItemView.extend({
            template: 'Series/Editor/SeriesEditorFooterViewTemplate',

            ui: {
                monitored           : '.x-monitored',
                qualityProfile      : '.x-quality-profiles',
                seasonFolder        : '.x-season-folder',
                rootFolder          : '.x-root-folder',
                selectedCount       : '.x-selected-count',
                saveButton          : '.x-save',
                organizeFilesButton : '.x-organize-files',
                container           : '.series-editor-footer'
            },

            events: {
                'click .x-save'           : '_updateAndSave',
                'change .x-root-folder'   : '_rootFolderChanged',
                'click .x-organize-files' : '_organizeFiles'
            },

            templateHelpers: function () {
                return {
                    qualityProfiles: QualityProfiles,
                    rootFolders    : RootFolders.toJSON()
                };
            },

            initialize: function (options) {
                this.seriesCollection = options.collection;

                RootFolders.fetch().done(function () {
                    RootFolders.synced = true;
                });

                this.editorGrid = options.editorGrid;
                this.listenTo(this.seriesCollection, 'backgrid:selected', this._updateInfo);
                this.listenTo(RootFolders, 'all', this.render);
            },

            onRender: function () {
                this._updateInfo();
            },

            _updateAndSave: function () {
                var selected = this.editorGrid.getSelectedModels();

                var monitored = this.ui.monitored.val();
                var profile = this.ui.qualityProfile.val();
                var seasonFolder = this.ui.seasonFolder.val();
                var rootFolder = this.ui.rootFolder.val();

                _.each(selected, function (model) {
                    if (monitored === 'true') {
                        model.set('monitored', true);
                    }

                    else if (monitored === 'false') {
                        model.set('monitored', false);
                    }

                    if (profile !== 'noChange') {
                        model.set('qualityProfileId', parseInt(profile, 10));
                    }

                    if (seasonFolder === 'true') {
                        model.set('seasonFolder', true);
                    }

                    else if (seasonFolder === 'false') {
                        model.set('seasonFolder', false);
                    }

                    if (rootFolder !== 'noChange') {
                        var rootFolderPath = RootFolders.get(parseInt(rootFolder, 10));

                        model.set('rootFolderPath', rootFolderPath.get('path'));
                    }

                    model.edited = true;
                });

                this.seriesCollection.save();
            },

            _updateInfo: function () {
                var selected = this.editorGrid.getSelectedModels();
                var selectedCount = selected.length;

                this.ui.selectedCount.html('{0} series selected'.format(selectedCount));

                if (selectedCount === 0) {
                    this.ui.monitored.attr('disabled', '');
                    this.ui.qualityProfile.attr('disabled', '');
                    this.ui.seasonFolder.attr('disabled', '');
                    this.ui.rootFolder.attr('disabled', '');
                    this.ui.saveButton.attr('disabled', '');
                    this.ui.organizeFilesButton.attr('disabled', '');
                }

                else {
                    this.ui.monitored.removeAttr('disabled', '');
                    this.ui.qualityProfile.removeAttr('disabled', '');
                    this.ui.seasonFolder.removeAttr('disabled', '');
                    this.ui.rootFolder.removeAttr('disabled', '');
                    this.ui.saveButton.removeAttr('disabled', '');
                    this.ui.organizeFilesButton.removeAttr('disabled', '');
                }
            },

            _rootFolderChanged: function () {
                var rootFolderValue = this.ui.rootFolder.val();
                if (rootFolderValue === 'addNew') {
                    var rootFolderLayout = new RootFolderLayout();
                    this.listenToOnce(rootFolderLayout, 'folderSelected', this._setRootFolder);
                    vent.trigger(vent.Commands.OpenModalCommand, rootFolderLayout);
                }
                else {
                    Config.setValue(Config.Keys.DefaultRootFolderId, rootFolderValue);
                }
            },

            _setRootFolder: function (options) {
                vent.trigger(vent.Commands.CloseModalCommand);
                this.ui.rootFolder.val(options.model.id);
                this._rootFolderChanged();
            },

            _organizeFiles: function () {
                var selected = this.editorGrid.getSelectedModels();
                var updateFilesSeriesView = new UpdateFilesSeriesView({ series: selected });
                this.listenToOnce(updateFilesSeriesView, 'updatingFiles', this._afterSave);

                vent.trigger(vent.Commands.OpenModalCommand, updateFilesSeriesView);
            }
        });
    });
