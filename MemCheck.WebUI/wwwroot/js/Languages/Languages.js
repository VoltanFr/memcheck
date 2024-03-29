﻿import { tellAxiosError } from '../Common.js';

const languagesApp = Vue.createApp({
    data() {
        return {
            newLanguage: {
                name
            },
            allLanguages: [],
            mountFinished: false,
        };
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
                    tellAxiosError(error);
                });
        },
        createNewLanguage() {
            axios.post('/Languages/Create/', this.newLanguage)
                .then(result => {
                    this.allLanguages.push(result.data);
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        showCardsWithLanguage(languageName) {
            window.location.href = `/Search/?tagFilterInput=${languageName}`;
        }
    },
});

languagesApp.mount('#LanguagesMainDiv');
