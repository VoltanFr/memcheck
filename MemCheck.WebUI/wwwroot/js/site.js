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
                    console.log(error);
                });
        },
        getActiveLanguage() {
            axios.get('/UILanguages/getActiveLanguage')
                .then(result => {
                    this.activeLanguage = result.data;
                })
                .catch(error => {
                    console.log(error);
                });
        },
        async     activeLanguageChange() {
            await axios.post('/UILanguages/SetCulture/' + this.activeLanguage)
                .then(result => {
                    if (result.data)    //Culture actually changed
                        window.location.reload(false);
                })
                .catch(error => {
                    console.log(error);
                });
        },
    },
    watch: {
        activeLanguage: async function () {
            await this.activeLanguageChange();
        }, immediate: false
    },
});
