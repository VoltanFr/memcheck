var app = new Vue({
    el: '#FullScreenMainDiv',
    data: {
        mountFinished: false,
        invalidRequest: false,
        imageBlob: null,   //Medium sized
    },
    async mounted() {
        try {
            await this.getImage();
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getImage() {
            imageId = document.getElementById("ImageIdInput").value;
            if (!imageId) {
                this.invalidRequest = true;
                return;
            }

            await axios.get('/Learn/GetImage/' + imageId + '/3', { responseType: 'arraybuffer' })
                .then(result => {
                    this.imageBlob = base64FromBytes(result.data);
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
    },
});