var app = new Vue({
    el: '#AdminPageMainDiv',
    data: {
        loading: false,
        cards: [],
        mountFinished: false,
    },
    mounted() {
        try {
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        getCards() {
            this.loading = true;
            axios.get('/Admin/cards')
                .then(result => {
                    this.cards = result.data;
                })
                .catch(error => {
                    console.log(error);
                })
                .then(() => {
                    this.loading = false;
                });
        },
        deleteCard(id) {
            this.loading = true;
            axios.delete('/Admin/cards/' + id)
                .then(result => {
                    this.cards = result.data;
                })
                .catch(error => {
                    console.log(error);
                })
                .then(() => {
                    this.loading = false;
                });
        }
    },
});