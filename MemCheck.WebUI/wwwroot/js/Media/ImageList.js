import { BigSizeImage } from '../big-size-image.js';
import { imageSizeSmall } from '../Common.js';
import { imageSizeMedium } from '../Common.js';
import { tellAxiosError } from '../Common.js';
import { base64FromBytes } from '../Common.js';
import { copyToClipboardAndToast } from '../Common.js';

const imageListApp = Vue.createApp({
    components: {
        'big-size-image': BigSizeImage,
    },
    data() {
        return {
            request: {
                filter: '', // string
                pageNo: 1, // int. First page is number 1
                pageSize: 10,   // int
            },
            totalCount: -1, // int
            pageCount: 0,   // int
            offeredPageSizes: [10, 50, 100],
            imageList: [],
            mountFinished: false,
            loading: false,
            currentFullScreenImage: null,   // Medium sized
        };
    },
    async mounted() {
        try {
            window.addEventListener('popstate', this.onPopState);
            await this.getImageList();
        }
        finally {
            this.mountFinished = true;
        }
    },
    beforeDestroy() {
        document.removeEventListener('popstate', this.onPopState);
    },
    methods: {
        setImageThumbnail(index, data) {
            this.imageList[index].thumbnail = base64FromBytes(data);
        },
        onAxiosError(error) {
            tellAxiosError(error);
        },
        async getImageList() {
            this.loading = true;
            this.imageList = [];
            await axios.post('/Media/GetImageList', this.request)
                .then(result => {
                    this.totalCount = result.data.totalCount;
                    this.pageCount = result.data.pageCount;
                    this.imageList = result.data.images.map(image => {
                        return {
                            imageName: image.imageName,
                            uploaderUserName: image.uploaderUserName,
                            description: image.description,
                            currentVersionDescription: image.currentVersionDescription,
                            source: image.source,
                            originalImageContentType: image.originalImageContentType,
                            originalImageSize: image.originalImageSize,
                            smallSize: image.smallSize,
                            mediumSize: image.mediumSize,
                            bigSize: image.bigSize,
                            imageId: image.imageId,
                            cardCount: image.cardCount,
                            initialUploadUtcDate: image.initialUploadUtcDate,
                            lastChangeUtcDate: image.lastChangeUtcDate,
                            currentVersionCreator: image.currentVersionCreator,
                        };
                    });
                })
                .catch(error => {
                    tellAxiosError(error);
                });

            for (let i = 0; i < this.imageList.length; i++)
                await axios.get(`/Learn/GetImage/${this.imageList[i].imageId}/${imageSizeSmall}`, { responseType: 'arraybuffer' })
                    .then(result => this.setImageThumbnail(i, result.data))
                    .catch(error => this.onAxiosError(error));

            this.loading = false;

            if (this.request.pageNo > this.pageCount) {
                this.request.pageNo = 1;
                if (this.totalCount > 0)
                    await this.getImageList();
            }
        },
        canMovePage(shift) {
            return (this.request.pageNo + shift > 0) && (this.request.pageNo + shift <= this.pageCount);
        },
        async moveToFirstPage() {
            this.request.pageNo = 1;
            await this.getImageList();
        },
        async moveToLastPage() {
            this.request.pageNo = this.pageCount;
            await this.getImageList();
        },
        async movePage(shift) {
            this.request.pageNo = this.request.pageNo + shift;
            await this.getImageList();
        },
        edit(imageId) {
            window.location.href = `/Media/Upload?ImageId=${imageId}&ReturnAddress=${window.location}`;
        },
        deleteImage(imageId) {
            window.location.href = `/Media/Delete?ImageId=${imageId}&ReturnAddress=${window.location}`;
        },
        imageHistory(imageId) {
            window.location.href = `/Media/History?ImageId=${imageId}`;
        },
        async showImageFull(image) {  // {imageName: string, imageId: Guid, cardCount: int, thumbnail: base64 string}
            this.loading = true;
            try {
                await axios.get(`/Learn/GetImage/${image.imageId}/${imageSizeMedium}`, { responseType: 'arraybuffer' })
                    .then(result => {
                        this.currentFullScreenImage = {
                            imageId: image.imageId,
                            blob: base64FromBytes(result.data),
                            name: image.imageName,
                            description: image.description,
                            source: image.source,
                            initialUploadUtcDate: image.initialUploadUtcDate,
                            initialVersionCreator: image.uploaderUserName,
                            currentVersionUtcDate: image.lastChangeUtcDate,
                            currentVersionCreator: image.currentVersionCreator,
                            currentVersionDescription: image.currentVersionDescription,
                            cardCount: image.cardCount,
                            originalImageContentType: image.originalImageContentType,
                            originalImageSize: image.originalImageSize,
                            smallSize: image.smallSize,
                            mediumSize: image.mediumSize,
                            bigSize: image.bigSize,
                        };
                    })
                    .catch(error => {
                        tellAxiosError(error);
                    });
            }
            finally {
                this.loading = false;
            }
        },
        onPopState() {
            // If we are in full screen image mode, a state "#" has been pushed by the browser
            this.currentFullScreenImage = null;
        },
        closeFullScreenImage() {
            window.history.back();
        },
        copyToClipboard(text) {
            copyToClipboardAndToast(text, localized.CopyToClipboardToastTitleOnSuccess, localized.CopyToClipboardToastTitleOnFailure);
        },
        bigSizeImageLabelsLocalizer() {
            return localized;
        },
    },
});

imageListApp.mount('#MediaMainDiv');
