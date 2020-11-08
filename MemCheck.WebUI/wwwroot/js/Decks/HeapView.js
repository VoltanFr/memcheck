var app = new Vue({
    el: '#HeapViewMainDiv',
    data: {
        userDecks: [],  //DecksController.GetUserDecksWithHeapsViewModel
        activeDeck: "",  //DecksController.GetUserDecksWithHeapsViewModel
        singleDeckDisplay: false,
        mountFinished: false,
    },
    async mounted() {
        try {
            await this.GetUserDecks();
            this.GetActiveDeckFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        GetActiveDeckFromPageParameter() {
            //There has to be a better way, but here's how I get a parameter passed to a page
            if (!this.singleDeckDisplay) {
                wantedDeck = document.getElementById("DeckIdInput").value;
                if (!wantedDeck)
                    return;
                for (let i = 0; i < this.userDecks.length; i++) {
                    if (this.userDecks[i].deckId == wantedDeck)
                        this.activeDeck = this.userDecks[i];
                }
            }
        },
        async GetUserDecks() {
            await axios.get('/Decks/GetUserDecksWithHeaps/')
                .then(result => {
                    this.userDecks = result.data;
                    if (this.userDecks.length === 1) {
                        this.activeDeck = this.userDecks[0];
                        this.singleDeckDisplay = true;
                    }
                    else {
                        this.activeDeck = "";
                        this.singleDeckDisplay = false;
                    }
                })
                .catch(error => {
                    console.log(error);
                });
        },
        dt(utcFromDotNet) {
            return dateTime(utcFromDotNet);
        },
        openSettingsPage() {
            if (this.activeDeck)
                window.location.href = '/Decks/Settings?DeckId=' + this.activeDeck.deckId;
        },
    },
});