var app = new Vue({
    el: '#NotifierMainDiv',
    data: {
        mountFinished: false,
        running: false,
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
            this.running = true;
            axios.post('/Admin/LaunchNotifier')
                .then(result => {
                    tellAxiosSuccess("Notifier terminated", "ok", this);
                    this.running = false;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                    this.running = false;
                });
        },
    },
});