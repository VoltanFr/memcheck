import { imageSizeMedium } from './Common.js';
import { base64FromBytes } from './Common.js';
import { getMnesiosImageNamesFromSourceText } from './MarkdownConversion.js';
import { convertMarkdown } from './MarkdownConversion.js';

export const MarkdownEditor = Vue.defineComponent({
    components: {
        // mention sub components, if we used in the template
    },
    props: {
        modelValue: { required: true, type: String },
        rows: { required: false, type: Number, default: 2 },
        title: { required: true, type: String },
        isinfrench: { required: true, type: Boolean },
        onimageclickfunctiontext: { required: false, type: String },
    },
    mounted() {
        this.adaptTextAreaSize();

        const ro = new ResizeObserver(() => {
            this.adaptTextAreaSize();
        });

        ro.observe(this.$refs.text_area_control);
    },
    template: `
        <div class="markdown-edit-main-div">
            <div class="markdown-edit-top-bar-div">
                {{title}}&nbsp;&nbsp;
                <button class="markdown-edit-button" v-on:click="bold()" title="Gras"><i class="fas fa-bold"></i></button>
                <button class="markdown-edit-button" v-on:click="italic()" title="Italique"><i class="fas fa-italic"></i></button>
                <button class="markdown-edit-button" v-on:click="insertNbsp()" title="Espace insécable">_</button>
                <button class="markdown-edit-button" v-on:click="insertTable()" title="Modèle de table"><i class="fas fa-table"></i></button>
                <button class="markdown-edit-button" v-on:click="togglePreview()" title="Apperçu du rendu Markdown"><i class="fab fa-markdown"></i></button>
            </div>
            <div class="markdown-edit-text-input-div">
                <textarea class="markdown-edit-textarea" v-model="content" v-bind:rows="rows" v-on:keydown="onKeyDown" v-on:input="onInput" ref="text_area_control"></textarea>
            </div>
            <div class="markdown-edit-preview-div" v-if="previewVisible">
                <span class="markdown-render markdown-body markdown-edit-preview" v-html="preview" ></span>
            </div>
        </div>
    `,
    data() {
        return {
            content: this.modelValue,
            previewVisible: false,
            images: new Map(), // Keys: image names, Values: all image detail fields
            preview: '',
        };
    },
    methods: {
        onInput() {
            this.$emit('update:modelValue', this.content);
            this.adaptTextAreaSize();
            if (this.previewVisible)
                this.renderedHtmlAsync();
        },
        adaptTextAreaSize() {
            const textarea = this.$refs.text_area_control;
            if (!textarea) // Happens when the page does not display the control, eg because a big size image is shown
                return;
            let scrollHeightBeforeUpdate = 0;
            do {
                scrollHeightBeforeUpdate = textarea.scrollHeight;
                const newHeight = Math.max(50, textarea.scrollHeight);
                textarea.style.height = `${newHeight}px`;
            } while (scrollHeightBeforeUpdate !== textarea.scrollHeight);
        },
        onKeyDown(event) {
            if (event.ctrlKey) {
                if (event.key === ' ') {
                    this.insertNbsp();
                    return;
                }
                if (event.key === 'i' || event.key === 'I') {
                    this.italic();
                    return;
                }
                if (event.key === 'b' || event.key === 'B') {
                    this.bold();
                    return;
                }
            }
        },
        insertText(text) {
            const textarea = this.$refs.text_area_control;
            let cursorStartPosition = textarea.selectionStart;
            let cursorEndPosition = textarea.selectionEnd;
            let tmpStr = textarea.value;
            this.content = tmpStr.substring(0, cursorStartPosition) + text + tmpStr.substring(cursorEndPosition, tmpStr.length);
            textarea.value = this.content;
            this.onInput();
            textarea.focus();
            textarea.selectionStart = cursorStartPosition + text.length;
            textarea.selectionEnd = cursorStartPosition + text.length;
        },
        insertNbsp() {
            this.insertText('&nbsp;');
        },
        insertTable() {
            const text = `| Aligné à gauche | Centré | Aligné à droite |
|:---|:----:|---:|
| Contenu | Contenu | Contenu |
| Contenu | Contenu | Contenu |`;
            this.insertText(text);
        },
        addMarkup(markup) {
            const textarea = this.$refs.text_area_control;
            let cursorStartPosition = textarea.selectionStart;
            let cursorEndPosition = textarea.selectionEnd;
            const initialValue = textarea.value;
            const selectionIsEmpty = cursorStartPosition === cursorEndPosition;
            if (!selectionIsEmpty) {
                // Don't include spaces in the modified area
                while (initialValue.charAt(cursorStartPosition) === ' ')
                    cursorStartPosition++;
                while (initialValue.charAt(cursorEndPosition - 1) === ' ')
                    cursorEndPosition--;
            }
            this.content = initialValue.substring(0, cursorStartPosition) + markup + initialValue.substring(cursorStartPosition, cursorEndPosition) + markup + initialValue.substring(cursorEndPosition);
            textarea.value = this.content;
            this.onInput();
            textarea.focus();
            const newCursorPosition = selectionIsEmpty ? cursorStartPosition + markup.length : cursorEndPosition + markup.length * 2;
            textarea.selectionStart = newCursorPosition;
            textarea.selectionEnd = newCursorPosition;
        },
        bold() {
            this.addMarkup('**');
        },
        italic() {
            this.addMarkup('_');
        },
        togglePreview() {
            this.previewVisible = !this.previewVisible;
            if (this.previewVisible)
                this.renderedHtmlAsync();
        },
        async loadImage(imageName) {
            const imageDetails = { name: imageName };

            const getImageByNameRequest = { imageName: imageName, size: imageSizeMedium };
            const getImageByNamePromise = axios.post('/Learn/GetImageByName/', getImageByNameRequest, { responseType: 'arraybuffer' })
                .then(result => {
                    imageDetails.blob = base64FromBytes(result.data);
                })
                .catch(() => {
                });

            const getImageMetadataFromNameRequest = { imageName: imageName };
            const getImageMetadataFromNamePromise = axios.post('/Media/GetImageMetadataFromName/', getImageMetadataFromNameRequest)
                .then(result => {
                    imageDetails.imageId = result.data.imageId;
                    imageDetails.description = result.data.description;
                    imageDetails.source = result.data.source;
                    imageDetails.initialUploadUtcDate = result.data.initialUploadUtcDate;
                    imageDetails.initialVersionCreator = result.data.initialVersionCreator;
                    imageDetails.currentVersionUtcDate = result.data.currentVersionUtcDate;
                    imageDetails.currentVersionDescription = result.data.currentVersionDescription;
                    imageDetails.cardCount = result.data.cardCount;
                    imageDetails.originalImageContentType = result.data.originalImageContentType;
                    imageDetails.originalImageSize = result.data.originalImageSize;
                    imageDetails.smallSize = result.data.smallSize;
                    imageDetails.mediumSize = result.data.mediumSize;
                    imageDetails.bigSize = result.data.bigSize;
                })
                .catch(() => {
                    // Just ignore, and the details won't be available
                });

            await Promise.all([getImageByNamePromise, getImageMetadataFromNamePromise]);

            this.images.set(imageName, imageDetails);
        },
        async renderedHtmlAsync() {
            const neededImageNames = getMnesiosImageNamesFromSourceText(this.content);
            const newImageNames = [...neededImageNames].filter(imageName => !this.images.has(imageName));
            newImageNames.forEach(newImageName => this.images.set(newImageName, null));

            const loadsToWait = [];
            this.images.forEach((value, key) => { if (!value || !value.blob) { const loadPromise = this.loadImage(key); loadsToWait.push(loadPromise); } });
            await Promise.all(loadsToWait);

            const imageArray = Array.from(this.images, ([_key, value]) => {
                return value;
            });

            this.preview = convertMarkdown(this.content, this.isinfrench, imageArray, this.onimageclickfunctiontext);
        },
    },
});
