import { TagButton } from '../TagButton.js';
import { CardRating } from '../CardRating.js';
import { isValidDateTime } from '../Common.js';
import { dateTime } from '../Common.js';
import { tellAxiosError } from '../Common.js';

const allCardsListApp = Vue.createApp({
    components: {
        'van-button': globalThis.vant.Button,
        'van-popover': globalThis.vant.Popover,
        'van-datetimepicker': globalThis.vant.DatetimePicker,
        'card-rating': CardRating,
        'tag-button': TagButton,
    },
    data() {
        return {
            mountFinished: false,
            allData: null,   // SearchController.GetAllStaticDataViewModel - We never mute this field after mounted has finished
            mountDebugInfo: '',
        };
    },
    beforeCreate() {
        this.dateTime = dateTime;
        this.isValidDateTime = isValidDateTime;
    },
    async mounted() {
        const start = performance.now();
        try {
            await this.getAllData();
        }
        finally {
            this.mountFinished = true;
            this.mountDebugInfo = `Vue mount time: ${((performance.now() - start) / 1000).toFixed(1)} seconds`;
        }
    },
    methods: {
        async getAllData() {
            await axios.get('/Search/GetAllCardsListData/')
                .then(result => {
                    this.allData = result.data;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        showDebugInfo() {
            return this.allData !== null && this.allData.showDebugInfo;
        },
    },
});

allCardsListApp.mount('#AllCardsListRootDiv');
