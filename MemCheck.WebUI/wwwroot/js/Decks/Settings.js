'use strict';

const deckSettingsApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            userDecks: [],  // DecksController.GetUserDecksViewModel
            activeDeck: '',  // DecksController.GetUserDecksViewModel
            singleDeckDisplay: false,
            heapingAlgorithms: [],  // IEnumerable<DecksController.HeapingAlgorithmViewModel>
            mountFinished: false,
        };
    },
    async mounted() {
        try {
            const task1 = this.getUserDecks();
            const task2 = this.getHeapingAlgorithms();
            await Promise.all([task1, task2]);
            this.getActiveDeckFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        getActiveDeckFromPageParameter() {
            if (!this.singleDeckDisplay) {
                // There has to be a better way, but here's how I get a parameter passed to a page
                const wantedDeck = document.getElementById('DeckIdInput').value;
                if (!wantedDeck)
                    return;
                for (let i = 0; i < this.userDecks.length; i++) {
                    if (this.userDecks[i].deckId === wantedDeck)
                        this.activeDeck = this.userDecks[i];
                }
            }
        },
        async getHeapingAlgorithms() {
            await axios.get('/Decks/GetHeapingAlgorithms/')
                .then(result => {
                    this.heapingAlgorithms = result.data;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async getUserDecks() {
            await axios.get('/Decks/GetUserDecks/')
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
        currentHeapingAlgorithmDescription() {
            if (!this.activeDeck)
                return '';
            for (let i = 0; i < this.heapingAlgorithms.length; i++) {
                if (this.heapingAlgorithms[i].id === this.activeDeck.heapingAlgorithmId)
                    return this.heapingAlgorithms[i].descriptionInCurrentLanguage;
            }
            return '';
        },
        async save() {
            const newDeck = { deckId: this.activeDeck.deckId, description: this.activeDeck.description, heapingAlgorithmId: this.activeDeck.heapingAlgorithmId };
            await axios.post('/Decks/Update/', newDeck)
                .then(() => {
                    window.location.href = '/Decks/';
                    return;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
    },
});

deckSettingsApp.mount('#DeckSettingsMainDiv');
