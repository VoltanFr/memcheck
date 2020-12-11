var app = new Vue({
    el: '#SubscriptionsMainDiv',
    data: {
        totalSearchSubscriptionCount: -1, //int
        searchSubscriptions: [],    //AccountController.SearchSubscriptionViewModel
        mountFinished: false,
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
            this.searchSubscriptions = [];
            await axios.post("/Account/GetSearchSubscriptions", this.request)
                .then(result => {
                    this.searchSubscriptions = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        edit(searchSubscriptionId) {
            window.location.href = "/Identity/Account/Manage/EditSearchSubscription?Id=" + searchSubscriptionId + "&ReturnUrl=" + window.location;
        },
    },
});
