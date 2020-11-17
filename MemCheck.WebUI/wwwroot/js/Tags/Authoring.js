var app = new Vue({
    el: '#TagAuthoringDiv',
    data: {
        editedTag: "",  //A TagsController.GetTagsTagViewModel if we are editing a tag, otherwise null
        existingTagNames: "",    //Set of string
        newName: "",    //string
        newNameProblem: "", //string
        mountFinished: false,
        returnUrl: "", //string
        guiMessages: {
            alreadyExistsErr: "",
            nameLengthErr: "",
            saved: "",
            labelName: "",
        },
        toastVisible: false,
    },
    async mounted() {
        try {
            task1 = this.GetTagNames();
            task2 = this.GetEditedTagFromPageParameter();
            task3 = this.GetGuiMessages();
            this.GetReturnUrlFromPageParameter();
            await Promise.all([task1, task2, task3]);
            this.$root.$on('bv::toast:hidden', () => { this.toastVisible = false; this.onNameChanged(); });
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async GetTagNames() {
            await axios.get('/Tags/GetTagNames')
                .then(result => {
                    this.existingTagNames = new Set(result.data);
                })
                .catch(error => {
                    tellAxiosError(error, this);
                    this.existingTagNames = new Set();
                });
        },
        async GetGuiMessages() {
            await axios.get('/Tags/GetGuiMessages')
                .then(result => {
                    this.guiMessages = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        async postNewTag() {
            await axios.post('/Tags/Create/', { NewName: this.newName })
                .then(() => {
                    this.afterSave();
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        async save() {
            this.toastVisible = false;
            this.onNameChanged();

            if (this.newNameProblem)
                return;

            if (this.editedTag)
                await this.updateTagName();
            else
                await this.postNewTag();
        },
        async updateTagName() {
            await axios.put('/Tags/Update/' + this.editedTag.tagId, { NewName: this.newName })
                .then(result => {
                    this.afterSave();
                })
                .catch(error => {
                    tellAxiosError(error, this);
                });
        },
        async afterSave() {
            this.$bvToast.toast(this.guiMessages.labelName + ' ' + this.newName, {
                title: this.guiMessages.saved,
                variant: 'success',
                toaster: 'b-toaster-top-center',
                solid: true,
                autoHideDelay: 10000,
            });
            this.toastVisible = true;
            this.editedTag = "";
            this.newName = "";
            this.newNameProblem = "";
            if (this.returnUrl)
                window.location = this.returnUrl;
            else
                await this.GetTagNames();
        },
        GetReturnUrlFromPageParameter() {
            this.returnUrl = document.getElementById("ReturnUrlInput").value;
        },
        async GetEditedTagFromPageParameter() {
            //There has to be a better way, but here's how I get a parameter passed to a page
            tagId = document.getElementById("TagIdInput").value;
            if (!tagId) {
                this.editedTag = "";
                return;
            }

            await axios.get('/Tags/GetTag/' + tagId)
                .then(result => {
                    this.editedTag = result.data;
                })
                .catch(error => {
                    tellAxiosError(error, this);
                    this.editedTag = "";
                });
        },
        onNameChanged() {
            if (this.toastVisible) {
                this.newNameProblem = "";
                return;
            }
            if (this.existingTagNames.has(this.newName)) {
                this.newNameProblem = this.guiMessages.alreadyExistsErr;
                return;
            }
            if (this.newName.length < 3 || this.newName.length > 50) {
                this.newNameProblem = this.guiMessages.nameLengthErr;
                return;
            }
            this.newNameProblem = "";
        },
    },
    watch: {
        newName: {
            handler() {
                this.onNameChanged();
            },
        },
    },
});