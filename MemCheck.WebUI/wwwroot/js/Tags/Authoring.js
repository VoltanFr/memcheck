'use strict';

const tagAuthoringApp = Vue.createApp({
    components: {
        'van-button': globalThis.vant.Button,
        'markdown-editor': MarkdownEditor,
    },
    data() {
        return {
            editedTag: '',  // A TagsController.GetTagViewModel if we are editing a tag, otherwise null (we are creating)
            existingTagNames: '',    // Set of string
            newName: '',    // string
            newNameProblem: '', // string
            newDescription: '',    // string
            mountFinished: false,
            returnAddress: '', // string
            guiMessages: {
                alreadyExistsErr: '',
                nameLengthErr: '',
            },
            toastVisible: false,
        };
    },
    async mounted() {
        try {
            const task1 = this.getTagNames();
            const task2 = this.getEditedTagFromPageParameter();
            const task3 = this.getGuiMessages();
            this.getReturnAddressFromPageParameter();
            await Promise.all([task1, task2, task3]);
            // this.$root.$on('bv::toast:hidden', () => { this.toastVisible = false; this.onNameChanged(); });
        }
        finally {
            this.mountFinished = true;
        }
    },
    methods: {
        async getTagNames() {
            await axios.get('/Tags/GetTagNames')
                .then(result => {
                    this.existingTagNames = new Set(result.data);
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.existingTagNames = new Set();
                });
        },
        async getGuiMessages() {
            await axios.get('/Tags/GetGuiMessages')
                .then(result => {
                    this.guiMessages = result.data;
                })
                .catch(error => {
                    tellAxiosError(error);
                });
        },
        async postNewTag() {
            await axios.post('/Tags/Create/', { NewName: this.newName, NewDescription: this.newDescription })
                .then(result => {
                    this.afterSave(result);
                })
                .catch(error => {
                    tellAxiosError(error);
                    return;
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
            await axios.put(`/Tags/Update/${this.editedTag.tagId}`, { NewName: this.newName, NewDescription: this.newDescription })
                .then(result => {
                    this.afterSave(result);
                })
                .catch(error => {
                    tellAxiosError(error);
                    return;
                });
        },
        async afterSave(axiosResult) {
            tellControllerSuccess(axiosResult);
            this.toastVisible = true;
            this.editedTag = '';
            this.newName = '';
            this.newDescription = '';
            this.newNameProblem = '';
            if (this.returnAddress)
                window.location = this.returnAddress;
            else
                window.location = '/Tags/Index';
        },
        getReturnAddressFromPageParameter() {
            this.returnAddress = document.getElementById('ReturnAddressInput').value;
        },
        async getEditedTagFromPageParameter() {
            // There has to be a better way, but here's how I get a parameter passed to a page
            const tagId = document.getElementById('TagIdInput').value;
            if (!tagId) {
                this.editedTag = '';
                return;
            }

            await axios.get(`/Tags/GetTag/${tagId}`)
                .then(result => {
                    this.editedTag = result.data;
                    this.newName = this.editedTag.tagName;
                    this.newDescription = this.editedTag.description;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.editedTag = '';
                });
        },
        renderedDescription() {
            return convertMarkdown(this.editedTag.description, true); // Questionable hardcoding of French
        },
        onNameChanged() {
            if (this.toastVisible) {
                this.newNameProblem = '';
                return;
            }
            if (this.newName !== this.editedTag.tagName && this.existingTagNames.has(this.newName)) {
                this.newNameProblem = this.guiMessages.alreadyExistsErr;
                return;
            }
            if (this.newName.length < 3 || this.newName.length > 50) {
                this.newNameProblem = this.guiMessages.nameLengthErr;
                return;
            }
            this.newNameProblem = '';
        },
    },
    watch: {
        newName: {
            handler: function newNameChangedHandler() {
                this.onNameChanged();
            },
        },
    },
});

tagAuthoringApp.mount('#TagAuthoringDiv');
