'use strict';

const layoutApp = Vue.createApp({
    data() {
        return {
            activeLanguage: '',
            availableLanguages: [], // Strings, like ['En', 'Fr']
            mountFinished: false,
            userLanguageCookieName: 'usrLang',
            allLanguagesCookieName: 'allLangs',
        };
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
            const fromCookie = getCookie(this.allLanguagesCookieName);

            if (fromCookie) {
                this.availableLanguages = fromCookie.split(',');
                if (this.availableLanguages.length > 0)
                    return;
            }

            await axios.get('/UILanguages/getAvailableLanguages')
                .then(result => {
                    this.availableLanguages = result.data;
                    setCookie(this.allLanguagesCookieName, result.data.toString(), 1);
                })
                .catch(error => {
                    tellAxiosError(error);
                    deleteCookie(this.allLanguagesCookieName);
                    this.availableLanguages = ['En', 'Fr'];
                });
        },
        async getActiveLanguage() {
            const fromCookie = getCookie(this.userLanguageCookieName);

            if (fromCookie) {
                this.activeLanguage = fromCookie;

                if (this.activeLanguage && this.activeLanguage.split(',').length === 1)
                    return;
            }

            await axios.get('/UILanguages/getActiveLanguage')
                .then(result => {
                    this.activeLanguage = result.data;
                    setCookie(this.userLanguageCookieName, this.activeLanguage, 1);
                })
                .catch(error => {
                    tellAxiosError(error);
                    deleteCookie(this.userLanguageCookieName);
                    this.activeLanguage = 'Fr';
                });
        },
        async activeLanguageChange() {
            await axios.post(`/UILanguages/SetCulture/${this.activeLanguage}`)
                .then(() => {
                    window.location.reload(false);
                    setCookie(this.userLanguageCookieName, this.activeLanguage, 1);
                })
                .catch(error => {
                    tellAxiosError(error);
                    deleteCookie(this.userLanguageCookieName);
                });
        },
    },
    watch: {
        activeLanguage: {
            handler: function handleActiveLanguageChange(newValue, oldValue) {
                if (newValue !== oldValue && this.mountFinished)
                    this.activeLanguageChange();
            },
        },
    },
});

layoutApp.mount('#SelectLanguageDiv');
