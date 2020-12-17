var app = new Vue({
    el: '#MediaMainDiv',
    data: {
        request: {
            filter: "", //string
            pageNo: 1, //int. First page is number 1
            pageSize: 10,   //int
        },
        totalCount: -1, //int
        pageCount: 0,   //int
        offeredPageSizes: [10, 50, 100, 500],
        images: [],
        mountFinished: false,
        loading: false,
        currentFullScreenImage: null,   //Medium sized
        TableFormat: false,
        staticText: {
            copyToClipboardToastTitleOnSuccess: "",
            copyToClipboardToastTitleOnFailure: "",
        },
        bigSizeImageLabels: null,   //MediaController.GetBigSizeImageLabels
    },
    async mounted() {
        try {
            window.addEventListener('popstate', this.onPopState);
            task1 = this.getImages();
            task2 = this.getStaticText();
            task3 = this.GetBigSizeImageLabels();
            await Promise.all([task1, task2, task3]);
        }
        finally {
            this.mountFinished = true;
        }
    },
    beforeDestroy() {
        document.removeEventListener("popstate", this.onPopState);
    },
    methods: {
        async getImages() {
            this.loading = true;
            this.images = [];
            await axios.post("/Media/GetImageList", this.request)
                .then(result => {
                    this.totalCount = result.data.totalCount;
                    this.pageCount = result.data.pageCount;
                    this.images = result.data.images.map(image => {
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
                    tellAxiosError(error, this);
                });

            for (let i = 0; i < this.images.length; i++)
                await axios.get('/Learn/GetImage/' + this.images[i].imageId + '/1', { responseType: 'arraybuffer' })
                    .then(result => {
                        this.images[i].thumbnail = base64FromBytes(result.data);
                    })
                    .catch(error => {
                        tellAxiosError(error, this);
                    });

            this.loading = false;

            if (this.request.pageNo > this.pageCount) {
                this.request.pageNo = 1;
                if (this.totalCount > 0)
                    await this.getImages();
            }
        },
        async getStaticText() {
            await axios.get("/Media/GetStaticText")
                .then(result => {
                    this.staticText = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        canMovePage(shift) {
            return (this.request.pageNo + shift > 0) && (this.request.pageNo + shift <= this.pageCount);
        },
        async moveToFirstPage() {
            this.request.pageNo = 1;
            await this.getImages();
        },
        async moveToLastPage() {
            this.request.pageNo = this.pageCount;
            await this.getImages();
        },
        async movePage(shift) {
            this.request.pageNo = this.request.pageNo + shift;
            await this.getImages();
        },
        edit(imageId) {
            window.location.href = "/Media/Upload?ImageId=" + imageId + "&ReturnUrl=" + window.location;
        },
        deleteImage(imageId) {
            window.location.href = "/Media/Delete?ImageId=" + imageId + "&ReturnUrl=" + window.location;
        },
        imageHistory(imageId) {
            window.location.href = "/Media/History?ImageId=" + imageId;
        },
        async showImageFull(image) {  //{imageName: string, imageId: Guid, cardCount: int, thumbnail: base64 string}
            this.loading = true;
            try {
                await axios.get('/Learn/GetImage/' + image.imageId + '/2', { responseType: 'arraybuffer' })
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
                        tellAxiosError(error, this);
                    });
            }
            finally {
                this.loading = false;
            }
        },
        onPopState() {
            //If we are in full screen image mode, a state "#" has been pushed by the browser
            this.currentFullScreenImage = null;
        },
        closeFullScreenImage() {
            window.history.back();
        },
        copyToClipboard(text) {
            copyToClipboardAndToast(text, this.staticText.copyToClipboardToastTitleOnSuccess, this.staticText.copyToClipboardToastTitleOnFailure, this);
        },
        dt(utcFromDotNet) {
            return dateTime(utcFromDotNet);
        },
        async GetBigSizeImageLabels() {
            await axios.get('/Media/GetBigSizeImageLabels')
                .then(result => {
                    this.bigSizeImageLabels = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
    },
});
