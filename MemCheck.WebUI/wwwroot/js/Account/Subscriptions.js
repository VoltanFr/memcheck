var app = new Vue({
    el: '#SubscriptionsMainDiv',
    data: {
        totalSearchSubscriptionCount: -1, //int
        searchSubscriptions: [],    //AccountController.SearchSubscriptionViewModel
        mountFinished: false,
        loading: false,
    },
    async mounted() {
        try {
            await this.getSearchSubscriptions();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getSearchSubscriptions() {
            this.loading = true;
            this.searchSubscriptions = [];
            await axios.post("/Account/GetSearchSubscriptions", this.request)
                .then(result => {
                    this.searchSubscriptions = result.data;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
            this.loading = false;
        },
        edit(searchSubscriptionId) {
            window.location.href = "/Identity/Account/Manage/EditSearchSubscription?Id=" + searchSubscriptionId + "&ReturnUrl=" + window.location;
        },
        async deleteSubscription(subscription) {
            if (confirm(subscription.deleteConfirmMessage)) {
                this.loading = true;

                await axios.delete('/Account/DeleteSearchSubscription/' + subscription.id)
                    .then(result => {
                        tellControllerSuccess(result);
                    })
                    .catch(error => {
                        tellAxiosError(error);
                    });

                this.loading = false;
                await this.getSearchSubscriptions();
            }
        },
    },
});
