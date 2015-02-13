//require.config
//({
//    paths:
//    {
//        jquery: 'scripts/jquery-1.10.2',
//        underscore: 'scripts/underscore',
//        backbone: 'scripts/backbone'
//    },
//    shim:
//    {
//        underscore:
//        {
//            exports: '_'
//        },
//        backbone:
//        {
//            deps: ['underscore', 'jquery'],
//            exports: 'Backbone'
//        }
//    }
//});

//require(['jquery', 'underscore', 'backbone'],
//function ($, _, Backbone)
//{
    var User = Backbone.Model.extend
    ({
        urlRoot: '/api/user',
        idAttribute: 'UserName',

        initialize: function ()
        {
            this.userCategories = new UserCategoryCollection,
            this.userCategories.url = '/api/user/' + this.id + '/userCategory'
            this.transactions = new TransactionCollection,
            this.transactions.url = '/api/user/' + this.id + '/transaction'
        }
    });

    var UserCategory = Backbone.Model.extend
    ({
        idAttribute: 'CategoryName'
    });

    var UserCategoryCollection = Backbone.Collection.extend
    ({
        model: UserCategory
    });

    var Transaction = Backbone.Model.extend
    ({
        idAttribute: 'TransactionId'
    });

    var TransactionCollection = Backbone.Collection.extend
    ({
        model: Transaction,
        comparator: function (item)
        {
            return item.get('TransactionDate');
        }
    });

    var UserView = Backbone.View.extend
    ({
        model: User,
        el: $('#accountTabContent'),
        template: _.template($('#user-template').html()),
        render: function ()
        {
            return this.$el.html(this.template(this.model.attributes));
        }
    });

    var TransactionView = Backbone.View.extend
    ({
        tagName: "tr",
        template: _.template($('#transaction-template').html()),
        render: function ()
        {
            return this.$el.html(this.template(this.model.attributes));
        }
    });

    var TransactionCollectionView = Backbone.View.extend
    ({
        el: $('#transactionsTableBody'),
        render: function ()
        {
            var self = this;

            _.each(this.model.models, function (transaction)
            {
                var transactionView = new TransactionView({ model: transaction });
                self.$el.append(transactionView.render());
            });
        }
    });

    var AppView = Backbone.View.extend
    ({
        el: $('body'),

        events:
        {
            "click #spendingTab": "viewSpending",
            "click #categoriesTab": "viewCategories",
            "click #transactionsTab": "viewTransactions",
            "click #accountTab": "viewAccount"
        },

        viewSpending: function ()
        {
            $('.nav-tabs li').removeClass('active');
            $('#spendingTab').addClass('active');
            $('.tab-content').addClass('hidden');
            $('#spendingTabContent').removeClass('hidden');
        },

        viewCategories: function ()
        {
            $('.nav-tabs li').removeClass('active');
            $('#categoriesTab').addClass('active');
            $('.tab-content').addClass('hidden');
            $('#categoriesTabContent').removeClass('hidden');
        },

        viewTransactions: function ()
        {
            $('.nav-tabs li').removeClass('active');
            $('#transactionsTab').addClass('active');
            $('.tab-content').addClass('hidden');
            $('#transactionsTabContent').removeClass('hidden');

            var self = this;

            if (!this.model.transactions.length)
            {
                showFetchResults(this.model.transactions.fetch(), function ()
                {
                    var transactionsView = new TransactionCollectionView({ model: self.model.transactions });
                    transactionsView.render();

                    $('#transactionsLoadingIndicator').addClass('hidden');
                    $('#transactionsTable').removeClass('hidden');
                });
            }
        },

        viewAccount: function ()
        {
            $('.nav-tabs li').removeClass('active');
            $('#accountTab').addClass('active');
            $('.tab-content').addClass('hidden');
            $('#accountTabContent').removeClass('hidden');

            var self = this;
            if (!this.model.has('EmailAddress'))
            {
                showFetchResults(this.model.fetch(), function ()
                {
                    var userView = new UserView({ model: self.model });
                    userView.render();

                    $('#accountLoadingIndicator').addClass('hidden');
                });
            }
        }
    });

    var user = new User({ UserName: 'reubenrybnik' });
    var appView = new AppView({ model: user });

    function showFetchResults(xhr, done)
    {
        xhr.done(done);
        xhr.fail(function (xhr, textStatus, errorThrown)
        {
            $('#lastFetchError').html(errorThrown);
        });
        xhr.complete(function (xhr, textStatus)
        {
            $('#lastFetchStatus').html(textStatus);
        });
    }
//});