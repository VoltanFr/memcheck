﻿import { tellAxiosError } from '../Common.js';

const deleteDeckApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            userDecks: [],  // DecksController.GetUserDecksForDeletionViewModel
            activeDeck: '',  // DecksController.GetUserDecksForDeletionViewModel
            singleDeckDisplay: false,
            mountFinished: false,
        };
    },
    async mounted() {
        try {
            await this.getUserDecks();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getUserDecks() {
            await axios.get('/Decks/GetUserDecksForDeletion/')
                .then(result => {
                    this.userDecks = result.data;
                    if (this.userDecks.length === 1) {
                        this.activeDeck = this.userDecks[0];
                        this.singleDeckDisplay = true;
                    }
                    else {
                        this.activeDeck = '';
                        this.singleDeckDisplay = false;
                    }
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async deleteDeck() {
            if (confirm(this.activeDeck.alertMessage)) {
                await axios.delete(`/Decks/DeleteDeck/${this.activeDeck.deckId}`)
                    .then(() => {
                        window.location.href = '/';
                        return;
                    })
                    .catch(error => {
                        tellAxiosError(error);
                    });
            }
        },
    },
});

deleteDeckApp.mount('#DeckDeleteMainDiv');
