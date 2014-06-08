'use strict';
define(
    [
        'backgrid',
        'marionette',
        'underscore',
        'Settings/Profile/ProfileSchemaCollection'
    ], function (Backgrid, Marionette, _, ProfileSchemaCollection) {
        return Backgrid.CellEditor.extend({

            className: 'quality-cell-editor',
            template : 'Cells/Edit/QualityCellEditorTemplate',
            tagName  : 'select',

            events: {
                'change' : 'save',
                'blur'   : 'close',
                'keydown': 'close'
            },

            render: function () {
                var self = this;

                var profileSchemaCollection = new ProfileSchemaCollection();
                var promise = profileSchemaCollection.fetch();

                promise.done(function () {
                    var templateName = self.template;
                    self.schema = profileSchemaCollection.first();

                    var selected = _.find(self.schema.get('items'), function (model) {
                        return model.quality.id === self.model.get(self.column.get('name')).quality.id;
                    });

                    if (selected) {
                        selected.quality.selected = true;
                    }

                    self.templateFunction = Marionette.TemplateCache.get(templateName);
                    var data = self.schema.toJSON();
                    var html = self.templateFunction(data);
                    self.$el.html(html);
                });

                return this;
            },

            save: function (e) {
                var model = this.model;
                var column = this.column;
                var selected = parseInt(this.$el.val(), 10);

                var profileItem = _.find(this.schema.get('items'), function(model) {
                    return model.quality.id === selected;
                });

                var newQuality = {
                    proper : false,
                    quality: profileItem.quality
                };

                model.set(column.get('name'), newQuality);
                model.save();
                model.trigger('backgrid:edited', model, column, new Backgrid.Command(e));
            },

            close: function (e) {
                var model = this.model;
                var column = this.column;
                var command = new Backgrid.Command(e);

                model.trigger('backgrid:edited', model, column, command);
            }
        });
    });
