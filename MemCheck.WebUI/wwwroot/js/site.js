import * as vant from '../../lib/vant/vant.js';

const layoutApp = Vue.createApp({
    data() {
        return {
            activeLanguage: "",
            availableLanguages: [],
            mountFinished: false,
        }
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
            const fromCookie = getCookie("AvailableLanguages");

            if (fromCookie) {
                this.availableLanguages = fromCookie.split(',');
                return;
            }

            await axios.get('/UILanguages/getAvailableLanguages')
                .then(result => {
                    this.availableLanguages = result.data;
                    setCookie("AvailableLanguages", result.data.toString(), 1);
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async getActiveLanguage() {
            const fromCookie = getCookie("ActiveLanguage");

            if (fromCookie) {
                this.activeLanguage = fromCookie;
                return;
            }

            await axios.get('/UILanguages/getActiveLanguage')
                .then(result => {
                    this.activeLanguage = result.data;
                    setCookie("ActiveLanguage", result.data, 1);
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async activeLanguageChange() {
            deleteCookie("ActiveLanguage");
            await axios.post('/UILanguages/SetCulture/' + this.activeLanguage)
                .then(result => {
                    if (result.data)    //Culture actually changed
                        window.location.reload(false);
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
    },
    watch: {
        activeLanguage: {
            handler: function () {
                if (this.mountFinished)
                    this.activeLanguageChange();
            }
        },
    },
});

layoutApp.mount('#SelectLanguageDiv');
