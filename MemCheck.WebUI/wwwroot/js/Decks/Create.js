import { tellAxiosError } from '../Common.js';

const createDeckApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            heapingAlgorithms: [],  // IEnumerable<DecksController.HeapingAlgorithmViewModel>
            description: '',
            heapingAlgorithm: '',   // DecksController.HeapingAlgorithmViewModel
            mountFinished: false,
            userDeckNames: [],  // strings
        };
    },
    async mounted() {
        try {
            const task1 = await this.getHeapingAlgorithms();
            const task2 = await this.getUserDeckNames();
            await Promise.all([task1, task2]);
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getHeapingAlgorithms() {
            await axios.get('/Decks/GetHeapingAlgorithms/')
                .then(result => {
                    this.heapingAlgorithms = result.data;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async getUserDeckNames() {
            await axios.get('/Decks/GetUserDecks/')
                .then(result => {
                    for (let i = 0; i < result.data.length; i++) {
                        this.userDeckNames.push(result.data[i].description);
                    }
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async create() {
            const newDeck = { description: this.description, heapingAlgorithmId: this.heapingAlgorithm.id };
            await axios.post('/Decks/Create/', newDeck)
                .then(() => {
                    window.location.href = '/';
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        userHasDecks() {
            return this.userDeckNames.length > 0;
        },
    },
});

createDeckApp.mount('#CreateDeck');
