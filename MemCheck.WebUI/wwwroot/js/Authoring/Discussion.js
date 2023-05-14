import { MarkdownEditor } from '../MarkdownEditor.js';
import { BigSizeImage } from '../big-size-image.js';
import { TagButton } from '../TagButton.js';
import { CardRating } from '../CardRating.js';
import { tellAxiosError } from '../Common.js';
import { decodeImageDefinition } from '../MarkdownConversion.js';
import { convertMarkdown } from '../MarkdownConversion.js';
import { downloadMissingImages } from '../ImageDownloading.js';
import { toast } from '../Common.js';
import { toastShortDuration } from '../Common.js';
import { sleep } from '../Common.js';
import { dateTimeWithTime } from '../Common.js';

const discussionApp = Vue.createApp({
    components: {
        'van-button': globalThis.vant.Button,
        'van-popover': globalThis.vant.Popover,
        'van-rate': globalThis.vant.Rate,
        'markdown-editor': MarkdownEditor,
        'big-size-image': BigSizeImage,
        'card-rating': CardRating,
        'tag-button': TagButton,
    },
    data() {
        return {
            mountFinished: false,
            cardId: null,
            currentFullScreenImage: null,   // MyImageType
            newEntryText: '',
            newEntryInReview: false, // When this is true, the user is reviewing changes after a click on the send button
            images: [], // each element represents an image object, with all details in fields. At least 'name' must be defined. Additional fields include 'blob', 'imageId', 'description', etc.
            errorDebugInfoLines: [], // strings
            saving: false,
            loading: false,
            entries: [], // Elements are AuthoringController.GetDiscussionEntriesResultEntry
            totalEntryCount: -1, // Count of all entries in DB (not dosnloaded entries)
        };
    },
    beforeCreate() {
        this.dateTimeWithTime = dateTimeWithTime;
    },
    async mounted() {
        try {
            window.addEventListener('beforeunload', this.onBeforeUnload);
            window.addEventListener('popstate', this.onPopState);
            this.getCardIdFromPageParameter();
            await this.getNextEntries();
        }
        finally {
            this.mountFinished = true;
        }
    },
    beforeDestroy() {
        document.removeEventListener('popstate', this.onPopState);
        document.removeEventListener('beforeunload', this.onBeforeUnload);
    },
    methods: {
        initializationFailure() {
            return !this.cardId;
        },
        getCardIdFromPageParameter() {
            this.cardId = document.getElementById('CardIdInput').value;
            this.errorDebugInfoLines.push(`Card id: ${this.cardId}`);
        },
        clearNewEntry() {
            this.newEntryText = '';
        },
        onBeforeUnload(event) {
            if (this.isDirty()) {
                (event || window.event).returnValue = 'Sure you want to lose your edits?';
                return 'Sure you want to lose your edits?';   // Message will not display on modern browers, but a fixed message will be displayed
            }
            return null;
        },
        isDirty() {
            return false;
        },
        onPopState() {
            // If we are in full screen image mode, a state '#' has been pushed by the browser
            if (!document.location.href.endsWith('#'))
                this.currentFullScreenImage = null;
        },
        showDebugInfo() {
            return false;
        },
        editUrl() {
            const result = `/Authoring/Index?CardId=${this.cardId}`;
            return result;
        },
        isInFrench() {
            return true; // Not sure what to do
        },
        onImageClickFunctionText() {
            return 'const div = document.querySelector("#DiscussionMainDiv"); const thisApp=div.__vue_app__; const imageClicked=thisApp._component.methods.showImageFull;';
        },
        showImageFull(encodedImage) {
            const image = decodeImageDefinition(encodedImage);
            discussionAppInstance.currentFullScreenImage = image;
            history.pushState('ShowingImageDetails', 'BackToCard');
        },
        bigSizeImageLabelsLocalizer() {
            return localized;
        },
        closeFullScreenImage() {
            window.history.back();
        },
        submitNewEntryForReview() {
            this.newEntryInReview = true;
        },
        async postNewEntry() {
            this.saving = true;

            const body = {
                CardId: this.cardId,
                Text: this.newEntryText,
            };

            const task = axios.post('/Authoring/PostDiscussionEntry/', body);

            await task
                .then(result => {
                    const toastMessage = `${localized.ToastMessageForPostSuccessBeforeCount}${result.data.entryCount}${localized.ToastMessageForPostSuccessAfterCount}`;
                    toast(toastMessage, localized.ToastTitleForPostSuccess, true);
                    sleep(toastShortDuration).then(() => {
                        this.clearNewEntry();
                        this.newEntryInReview = false;
                        this.saving = false;
                        location.reload();
                    });
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.newEntryInReview = false;
                    this.saving = false;
                });
        },
        renderedText(text) {
            downloadMissingImages(text, this.images);
            return convertMarkdown(text, this.isInFrench(), this.images, this.onImageClickFunctionText());
        },
        renderedNewEntry() {
            return this.renderedText(this.newEntryText);
        },
        continueEditing() {
            this.newEntryInReview = false;
        },
        async getNextEntries() {
            this.loading = true;
            const request = { pageSize: 10, cardId: this.cardId, lastObtainedEntry: this.entries.at(-1)?.entryId };
            await axios.post('/Authoring/GetDiscussionEntries', request)
                .then(result => {
                    this.totalDiscussionEntries = result.data.totalEntryCount;
                    this.entries.push(...result.data.entries);
                    this.loading = false;
                })
                .catch(error => {
                    tellAxiosError(error);
                    this.loading = false;
                });
        },
        canDownloadMore() {
            return this.entries.length < this.totalDiscussionEntries;
        },
    },
});

const discussionAppInstance = discussionApp.mount('#DiscussionMainDiv');
