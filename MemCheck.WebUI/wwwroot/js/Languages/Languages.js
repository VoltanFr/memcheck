var app = new Vue({
    el: '#LanguagesMainDiv',
    data: {
        newLanguage: {
            name
        },
        allLanguages: [],
        mountFinished: false,
    },
    mounted() {
        try {
            this.getAllLanguages();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        getAllLanguages() {
            axios.get('/Languages/getAllLanguages')
                .then(result => {
                    this.allLanguages = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        createNewLanguage() {
            axios.post('/Languages/Create/', this.newLanguage)
                .then(result => {
                    this.allLanguages.push(result.data);
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        showCardsWithLanguage(languageName) {
            window.location.href = '/Search/?tagFilterInput=' + languageName;
        }
    },
});