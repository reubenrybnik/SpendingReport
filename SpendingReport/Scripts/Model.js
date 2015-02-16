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
//        jquery:
//        {
//            exports: '$'
//        },
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
            this.userCategories = new UserCategoryCollection;
            this.userCategories.url = '/api/user/' + this.id + '/userCategory';
            this.transactions = new TransactionCollection;
            this.transactions.url = '/api/user/' + this.id + '/transaction';
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
        idAttribute: 'TransactionId',
        
        defaults:
        {
            PayeeName: '',
            Amount: '',
            TransactionDate: Date.now(),
            CategoryName: '',
            checked: false
        },

        toggle: function(checked)
        {
            if (checked)
            {
                this.set('checked', true);
            }
            else
            {
                this.set('checked', false);
            }
        }
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

        initialize: function()
        {
            this.listenTo(this.model, 'change', this.render);
            this.listenTo(this.model, 'destroy', this.remove);
        },

        events:
        {
            'click #triggerSelect': 'selectTransaction',
            'click #triggerEdit': 'editTransaction',
            'click #triggerSave': 'saveTransaction',
        },

        selectTransaction: function(event)
        {
            this.stopListening(this.model, 'change');
            this.model.set('checked', event.target.checked);
            this.listenTo(this.model, 'change', this.render);
        },

        editTransaction: function()
        {
            this.$el.addClass('editing');
        },

        saveTransaction: function()
        {
            var payeeNameInput = $('#payeeNameInput', this.$el);
            var amountInput = $('#amountInput', this.$el);
            var transactionDateInput = $('#transactionDateInput', this.$el);
            var categoryNameInput = $('#categoryNameInput', this.$el);
            this.model.save
            ({
                PayeeName: payeeNameInput.val(),
                Amount: amountInput.val(),
                TransactionDate: transactionDateInput.val(),
                CategoryName: categoryNameInput.val()
            });

            this.$el.removeClass('editing');
        },

        render: function ()
        {
            this.$el.html(this.template(this.model.attributes));
            return this;
        }
    });

    var TransactionCollectionView = Backbone.View.extend
    ({
        el: $('#transactionsTable'),

        initialize: function()
        {
            this.tableBody = $('#transactionsTableBody', this.$el);
        },

        events:
        {
            'click #selectAllCheckbox': 'selectAll',
            'click #addButton': 'addSingle',
            'click #deleteButton': 'deleteSelected'
        },
        
        selectAll: function(checkbox)
        {
            _.each(this.model, function (item)
            {
                item.toggle(checkbox.checked);
            });
        },

        addSingle: function()
        {
            var newTransaction = new Transaction;
            this.model.add(newTransaction);
            var transactionView = this.renderSingle(newTransaction);
            transactionView.$el.addClass('editing');
        },

        deleteSelected: function()
        {
            _.invoke(this.model.where({ checked: true }), 'destroy');
        },

        render: function ()
        {
            var self = this;
            _.each(this.model.models, this.renderSingle, self);
        },

        renderSingle: function(transaction)
        {
            var transactionView = new TransactionView({ model: transaction });
            this.tableBody.append(transactionView.render().el);
            return transactionView;
        }
    });

    var AppView = Backbone.View.extend
    ({
        el: $('body'),

        events:
        {
            'click #spendingTab': 'viewSpending',
            'click #categoriesTab': 'viewCategories',
            'click #transactionsTab': 'viewTransactions',
            'click #accountTab': 'viewAccount'
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
//},
//function (err)
//{
//    $('#lastFetchError').html(err);
//});