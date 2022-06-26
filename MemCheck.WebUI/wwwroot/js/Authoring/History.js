import { tellAxiosError } from '../Common.js';
import { dateTime } from '../Common.js';

const cardHistoryApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            mountFinished: false,
            loading: false,
            cardId: null,
            error: '',
            versions: [],   // AuthoringController.CardVersion
        };
    },
    beforeCreate() {
        this.dateTime = dateTime;
    },
    async mounted() {
        try {
            await this.getEntriesFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getEntriesFromPageParameter() {
            this.loading = true;
            this.cardId = document.getElementById('CardIdInput').value;
            if (!this.cardId) {
                this.error = 'Card not found (this page expects a card id parameter)';
                return;
            }

            await axios.get(`/Authoring/CardVersions/${this.cardId}`)
                .then(result => {
                    this.versions = result.data;
                    this.loading = false;
                    this.error = null;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.cardId = null;
                    this.error = `"Card not found: ${error}`;
                    this.loading = false;
                    return;
                });
        },
        showDebugInfo() {
            return true;
        },
    },
});

cardHistoryApp.mount('#HistoryMainDiv');
