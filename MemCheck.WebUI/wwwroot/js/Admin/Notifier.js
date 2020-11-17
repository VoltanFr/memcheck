var app = new Vue({
    el: '#NotifierMainDiv',
    data: {
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
        launch() {
            axios.post('/Admin/LaunchNotifier')
                .then(result => {
                    tellAxiosSuccess("Launched", "ok", this);
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
    },
});