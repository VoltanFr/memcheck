var app = new Vue({
    el: '#SelectLanguageDiv',
    data: {
        activeLanguage: "",
        availableLanguages: []
    },
    async mounted() {
        this.getAvailableLanguages();
        this.getActiveLanguage();
    },
    methods: {
        getAvailableLanguages() {
            axios.get('/UILanguages/getAvailableLanguages')
                .then(result => {
                    this.availableLanguages = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        getActiveLanguage() {
            axios.get('/UILanguages/getActiveLanguage')
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
            await this.activeLanguageChange();
        }, immediate: false
    },
});
