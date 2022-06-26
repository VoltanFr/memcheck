import { tellAxiosError } from '../Common.js';

const editSearchSubscriptionApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            subscription: '',  // AccountController.SearchSubscriptionViewModel. Null if page param not valid
            newName: '',    // string
            mountFinished: false,
            returnAddress: '', // string
        };
    },
    beforeCreate() {
        this.dateTime = dateTime;
    },
    async mounted() {
        try {
            this.getReturnAddressFromPageParameter();
            await this.getSubscriptionFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async save() {
            await axios.put(`/Account/SetSearchSubscriptionName/${this.subscription.id}`, { NewName: this.newName })
                .then(() => {
                    if (this.returnAddress)
                        window.location = this.returnAddress;
                    else
                        window.location = '/';
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        getReturnAddressFromPageParameter() {
            this.returnAddress = document.getElementById('ReturnAddressInput').value;
        },
        async getSubscriptionFromPageParameter() {
            const subscriptionId = document.getElementById('SubscriptionIdInput').value;
            if (!subscriptionId) {
                this.subscription = '';
                return;
            }
            await axios.get(`/Account/GetSearchSubscription/${subscriptionId}`)
                .then(result => {
                    this.subscription = result.data;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.subscription = '';
                });
        },
    },
});

editSearchSubscriptionApp.mount('#EditSearchSubscriptionDiv');
