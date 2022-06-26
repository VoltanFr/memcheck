import { tellAxiosError } from '../Common.js';
import { tellControllerSuccess } from '../Common.js';

const notifierMainApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            mountFinished: false,
            running: false,
        };
    },
    mounted() {
        this.mountFinished = true;
    },
    methods: {
        launch() {
            this.running = true;
            axios.post('/Admin/LaunchNotifier')
                .then(result => {
                    tellControllerSuccess(result);
                    this.running = false;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.running = false;
                });
        },
    },
});

notifierMainApp.mount('#NotifierMainDiv');
