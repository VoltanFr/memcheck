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
            imageList: [], // Each entry has fields imageId, imageName, imageCardCount, imageThumbnail (this one is set image par image, as we load the bitmaps)
            mountFinished: false,
            loading: false,
            currentFullScreenImage: null, // Fields: see method showImageFull
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
            this.imageList[index].imageThumbnail = base64FromBytes(data);
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
                            imageId: image.imageId,
                            imageName: image.imageName,
                            imageCardCount: image.cardCount,
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
        async showImageFull(imageId) {
            this.loading = true;
            try {
                let imageWithDetails = { imageId: imageId };

                const blobPromise = axios.get(`/Learn/GetImage/${imageId}/${imageSizeMedium}`, { responseType: 'arraybuffer' })
                    .then(result => {
                        imageWithDetails.blob = base64FromBytes(result.data);
                    })
                    .catch(error => {
                        tellAxiosError(error);
                    });

                const detailsPromise = axios.get(`/Media/GetImageMetadataFromId/${imageId}`)
                    .then(result => {
                        imageWithDetails.name = result.data.imageName;
                        imageWithDetails.description = result.data.description;
                        imageWithDetails.source = result.data.source;
                        imageWithDetails.initialUploadUtcDate = result.data.initialUploadUtcDate;
                        imageWithDetails.currentVersionUtcDate = result.data.currentVersionUtcDate;
                        imageWithDetails.currentVersionCreator = result.data.currentVersionCreatorName;
                        imageWithDetails.currentVersionDescription = result.data.currentVersionDescription;
                        imageWithDetails.cardCount = result.data.cardCount;
                        imageWithDetails.originalImageContentType = result.data.originalImageContentType;
                        imageWithDetails.originalImageSize = result.data.originalImageSize;
                        imageWithDetails.smallSize = result.data.smallSize;
                        imageWithDetails.mediumSize = result.data.mediumSize;
                        imageWithDetails.bigSize = result.data.bigSize;
                    })
                    .catch(error => {
                        tellAxiosError(error);
                    });

                await Promise.all([blobPromise, detailsPromise]);

                this.currentFullScreenImage = imageWithDetails;
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
