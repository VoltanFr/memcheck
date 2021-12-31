const uploadMediaApp = Vue.createApp({
    components: {
        'markdown-editor': MarkdownEditor,
    },
    data() {
        return {
            mountFinished: false,
            name: "",  //string
            description: "",  //string
            versionDescription: "", //string
            source: "",  //string
            selectedFile: null, //File
            imagePreview: null, //blob
            uploading: false,
            editingImageId: null,   //Guid
            returnUrl: "", //string
            originalName: "",
            originalDescription: "",  //string
            originalSource: "",  //string
        }
    },
    async mounted() {
        try {
            window.addEventListener('beforeunload', this.onBeforeUnload);
            this.GetReturnUrlFromPageParameter();
            await this.GetImageToEditFromPageParameter();
        }
        finally {
            this.mountFinished = true;
        }
    },
    beforeDestroy() {
        document.removeEventListener("beforeunload", this.onBeforeUnload);
    },
    methods: {
        async upload() {
            try {
                if (this.editingImageId)
                    await this.uploadEdited();
                else
                    await this.uploadNew();
            }
            finally {
                this.uploading = false;
            }
        },
        async uploadNew() {
            this.uploading = true;
            const f = new FormData();
            f.set("Name", this.name);
            f.set("Description", this.description);
            f.set("Source", this.source);
            f.set("File", this.selectedFile);

            await axios.post('/Media/UploadImage/', f, { headers: { 'Content-Type': 'multipart/form-data' } })
                .then((result) => {
                    tellControllerSuccess(result);
                    this.clearAll();
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async uploadEdited() {
            this.uploading = true;

            const data = { imageName: this.name, source: this.source, description: this.description, versionDescription: this.versionDescription };

            await axios.post('/Media/Update/' + this.editingImageId, data)
                .then((result) => {
                    tellControllerSuccess(result);
                    this.clearAll();
                    if (this.returnUrl)
                        window.location = this.returnUrl;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        clearAll() {
            this.name = "";
            this.description = "";
            this.source = "";
            this.versionDescription = "";
            this.selectedFile = null;
            this.imagePreview = null;
            this.originalName = this.name;
            this.originalDescription = this.description;
            this.originalSource = this.source;
        },
        async onFileSelected(event) {
            if (event.target.files[0]) {
                this.selectedFile = event.target.files[0];
                const fileReader = new FileReader();
                fileReader.addEventListener('load', () => {
                    this.imagePreview = fileReader.result;
                });
                fileReader.readAsDataURL(this.selectedFile);
            }
        },
        async GetImageToEditFromPageParameter() {
            const imageId = document.getElementById("ImageIdInput").value;
            if (!imageId)
                return;

            await axios.get('/Media/GetImageMetadata/' + imageId)
                .then(result => {
                    this.editingImageId = imageId;
                    this.name = result.data.imageName;
                    this.source = result.data.source;
                    this.description = result.data.description;
                    this.originalName = this.name;
                    this.originalDescription = this.description;
                    this.originalSource = this.source;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.clearAll();
                    return;
                });

            await axios.get('/Learn/GetImage/' + imageId + '/2', { responseType: 'arraybuffer' })
                .then(result => {
                    this.imagePreview = base64FromBytes(result.data);
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async GetReturnUrlFromPageParameter() {
            this.returnUrl = document.getElementById("ReturnUrlInput").value;
        },
        onBeforeUnload(event) {
            if (this.isDirty()) {
                (event || window.event).returnValue = "Sure you want to lose your edits?";
                return "Sure you want to lose your edits?";   //Message will not display on modern browers, but a fixed message will be displayed
            }
        },
        isDirty() {
            var result = this.name != this.originalName;
            result = result || (this.description != this.originalDescription);
            result = result || (this.source != this.originalSource);
            result = result || (this.versionDescription != "");
            return result;
        },
    },
});

uploadMediaApp.mount('#UploadMainDiv');
