import { isValidDateTime } from '../Common.js';
import { dateTime } from '../Common.js';
import { tellAxiosError } from '../Common.js';
import { sleep } from '../Common.js';

const app = Vue.createApp({
    data() {
        return {
            allData: null, // HomeController.GetAllViewModel
            reload: false,
            dateTimeMinValue: '9999-12-31T23:59:59.9999999',
        };
    },
    async mounted() {
        await this.getAll();
    },
    beforeCreate() {
        this.dateTime = dateTime;
        this.isValidDateTime = isValidDateTime;
    },
    methods: {
        async getAll() {
            this.reload = false;
            await axios.get('/Home/GetAll')
                .then(result => {
                    this.allData = result.data;

                    if (this.allData.reloadWaitTime > 30000)
                        sleep(this.allData.reloadWaitTime).then(() => {
                            this.reload = true;
                        });
                })
                .catch(error => {
                    tellAxiosError(error);

                    if (this.allData)
                        sleepTime = 600000; // 10'
                    else
                        sleepTime = 1000;   // Never loaded, let's retry quick: the user has no info!

                    sleep(sleepTime).then(() => {
                        this.reload = true;
                    });
                });
        },
        showDebugInfo() {
            return false;
        },
    },
    watch: {
        reload: {
            handler: function reloadHandler() {
                if (this.reload)
                    this.getAll();
            }
        },
    },
});

app.mount('#HomeDiv');
