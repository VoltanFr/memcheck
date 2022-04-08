'use strict';

const mediaHistoryApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            mountFinished: false,
            loading: false,
            imageId: null,
            error: '',
            versions: [],   // MediaController.ImageVersion
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
            this.imageId = document.getElementById('ImageIdInput').value;
            if (!this.imageId) {
                this.error = 'Image not found (this page expects an image id parameter)';
                return;
            }

            await axios.get(`/Media/ImageVersions/${this.imageId}`)
                .then(result => {
                    this.versions = result.data;
                    this.loading = false;
                    this.error = null;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.imageId = null;
                    this.error = `Image not found: ${error}`;
                    this.loading = false;
                    return;
                });
        },
        showDebugInfo() {
            return true;
        },
    },
});

mediaHistoryApp.mount('#HistoryMainDiv');
