var app = new Vue({
    el: '#CreateDeck',
    data: {
        heapingAlgorithms: [],  //IEnumerable<DecksController.HeapingAlgorithmViewModel>
        description: "",
        heapingAlgorithm: "",   //DecksController.HeapingAlgorithmViewModel
        mountFinished: false,
    },
    async mounted() {
        try {
            await this.getHeapingAlgorithms();
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
        async create() {
            newDeck = { description: this.description, heapingAlgorithmId: this.heapingAlgorithm.id };
            await axios.post('/Decks/Create/', newDeck)
                .then(result => {
                    window.location.href = '/';
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
    },
});