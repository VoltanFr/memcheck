var app = new Vue({
    el: '#CompareMainDiv',
    data: {
        mountFinished: false,
        loading: false,
        error: "",
        cardId: null,
        selectedVersionId: null,
        diffResult: null,   //AuthoringController.CardSelectedVersionDiffWithCurrentResult
    },
    async mounted() {
        try {
            await this.GeValuesFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async GeValuesFromPageParameter() {
            this.loading = true;
            this.cardId = document.getElementById("CardIdInput").value;
            this.versionId = document.getElementById("VersionIdInput").value;
            if (!this.cardId || !this.versionId) {
                this.error = "Expected values not found in page parameters)";
                return;
            }

            await axios.get('/Authoring/CardSelectedVersionDiffWithCurrent/' + this.cardId + '/' + this.versionId)
                .then(result => {
                    this.diffResult = result.data;
                    this.loading = false;
                    this.error = null;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.cardId = null;
                    this.versionId = null;
                    this.error = "Failed to load data";
                    this.loading = false;
                    return;
                });
        },
        dt(utcFromDotNet) {
            return dateTime(utcFromDotNet);
        },
        dtWithTime(utcFromDotNet) {
            return dateTimeWithTime(utcFromDotNet);
        },
    },
});
