var app = new Vue({
    el: '#HistoryMainDiv',
    data: {
        mountFinished: false,
        loading: false,
        imageId: null,
        error: "",
        versions: [],   //MediaController.ImageVersion
    },
    async mounted() {
        try {
            await this.GetEntriesFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async GetEntriesFromPageParameter() {
            this.loading = true;
            this.imageId = document.getElementById("ImageIdInput").value;
            if (!this.imageId) {
                this.error = "Image not found (this page expects an image id parameter)";
                return;
            }

            await axios.get('/Media/ImageVersions/' + this.imageId)
                .then(result => {
                    this.versions = result.data;
                    this.loading = false;
                    this.error = null;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.imageId = null;
                    this.error = "Image not found: " + error;
                    this.loading = false;
                    return;
                });
        },
        showDebugInfo() {
            return true;
        },
        dt(utcFromDotNet) {
            return dateTime(utcFromDotNet);
        },
    },
});
