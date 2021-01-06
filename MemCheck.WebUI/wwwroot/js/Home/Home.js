var app = new Vue({
    el: '#HomeDiv',
    data: {
        allData: null, //HomeController.GetAllViewModel
        reload: false,
        dateTimeMinValue: "9999-12-31T23:59:59.9999999",
    },
    async mounted() {
        await this.getAll();
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
                        })
                })
                .catch(error => {
                    tellAxiosError(error, this);

                    if (this.allData)
                        sleepTime = 600000; //10'
                    else
                        sleepTime = 1000;   //Never loaded, let's retry quick: the user has no info!

                    sleep(sleepTime).then(() => {
                        this.reload = true;
                    })
                });
        },
        dt(utcFromDotNet) {
            return dateTime(utcFromDotNet);
        },
        isValidDt(utcFromDotNet) {
            return isValidDateTime(utcFromDotNet);
        },
        showDebugInfo() {
            return false; //(this.allData) && ((this.allData.userName == "Voltan") || (this.allData.userName == "Toto1"));
        },
    },
    watch: {
        reload: {
            handler() {
                if (this.reload)
                    this.getAll();
            },
        },
    },
});
