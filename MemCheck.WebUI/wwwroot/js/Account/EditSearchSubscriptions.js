var app = new Vue({
    el: '#EditSearchSubscriptionDiv',
    data: {
        subscription: "",  //AccountController.SearchSubscriptionViewModel. Null if page param not valid
        newName: "",    //string
        mountFinished: false,
        returnUrl: "", //string
    },
    async mounted() {
        try {
            this.GetReturnUrlFromPageParameter();
            await this.GetSubscriptionFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async save() {
            await axios.put('/Account/SetSearchSubscriptionName/' + this.subscription.id, { NewName: this.newName })
                .then(result => {
                    if (this.returnUrl)
                        window.location = this.returnUrl;
                    else
                        window.location = "/";
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        GetReturnUrlFromPageParameter() {
            this.returnUrl = document.getElementById("ReturnUrlInput").value;
        },
        async GetSubscriptionFromPageParameter() {
            subscriptionId = document.getElementById("SubscriptionIdInput").value;
            if (!subscriptionId) {
                this.subscription = "";
                return;
            }
            await axios.get('/Account/GetSearchSubscription/' + subscriptionId)
                .then(result => {
                    this.subscription = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                    this.subscription = "";
                });
        },
    },
});