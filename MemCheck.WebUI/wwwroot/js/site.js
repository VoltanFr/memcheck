var app = new Vue({
    el: '#SelectLanguageDiv',
    data: {
        activeLanguage: "",
        availableLanguages: [],
        mountFinished: false,
    },
    async mounted() {
        try {
            await this.getAvailableLanguages();
            await this.getActiveLanguage();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getAvailableLanguages() {
            await axios.get('/UILanguages/getAvailableLanguages')
                .then(result => {
                    this.availableLanguages = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        async getActiveLanguage() {
            await axios.get('/UILanguages/getActiveLanguage')
                .then(result => {
                    this.activeLanguage = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        async activeLanguageChange() {
            await axios.post('/UILanguages/SetCulture/' + this.activeLanguage)
                .then(result => {
                    if (result.data)    //Culture actually changed
                        window.location.reload(false);
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
    },
    watch: {
        activeLanguage: async function () {
            if (this.mountFinished)
                await this.activeLanguageChange();
        }, immediate: false
    },
});
