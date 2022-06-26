import { dateTime } from '../Common.js';
import { dateTimeWithTime } from '../Common.js';
import { tellAxiosError } from '../Common.js';

const heapViewApp = Vue.createApp({
    components: {
        'van-button': globalThis.vant.Button,
    },
    data() {
        return {
            userDecks: [],  // DecksController.GetUserDecksWithHeapsViewModel
            activeDeck: '',  // DecksController.GetUserDecksWithHeapsViewModel
            singleDeckDisplay: false,
            mountFinished: false,
        };
    },
    beforeCreate() {
        this.dateTime = dateTime;
        this.dateTimeWithTime = dateTimeWithTime;
    },
    async mounted() {
        try {
            await this.getUserDecks();
            this.getActiveDeckFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        getActiveDeckFromPageParameter() {
            // There has to be a better way, but here's how I get a parameter passed to a page
            if (!this.singleDeckDisplay) {
                const wantedDeck = document.getElementById('DeckIdInput').value;
                if (!wantedDeck)
                    return;
                for (let i = 0; i < this.userDecks.length; i++) {
                    if (this.userDecks[i].deckId === wantedDeck)
                        this.activeDeck = this.userDecks[i];
                }
            }
        },
        async getUserDecks() {
            await axios.get('/Decks/GetUserDecksWithHeaps/')
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
        dt(utcFromDotNet) {
            return dateTime(utcFromDotNet);
        },
        openSettingsPage() {
            if (this.activeDeck)
                window.location.href = `/Decks/Settings?DeckId=${this.activeDeck.deckId}`;
        },
    },
});

heapViewApp.mount('#HeapViewMainDiv');
