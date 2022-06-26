import { imageSizeBig } from '../Common.js';
import { tellAxiosError } from '../Common.js';
import { base64FromBytes } from '../Common.js';

const fullScreenImageApp = Vue.createApp({
    components: {
    },
    data() {
        return {
            mountFinished: false,
            invalidRequest: false,
            imageBlob: null,   // Medium sized
        };
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
            const imageId = document.getElementById('ImageIdInput').value;
            if (!imageId) {
                this.invalidRequest = true;
                return;
            }

            await axios.get(`/Learn/GetImage/${imageId}/${imageSizeBig}`, { responseType: 'arraybuffer' })
                .then(result => {
                    this.imageBlob = base64FromBytes(result.data);
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
    },
});

fullScreenImageApp.mount('#FullScreenMainDiv');
