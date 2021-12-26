const MarkdownEditor = Vue.defineComponent({
    components: {
        // mention sub components, if we used in the template
    },
    props: {
        modelValue: { required: true, type: String },
        rows: { required: false, type: Number, default: 2 },
        title: { required: true, type: String },
        isinfrench: { required: true, type: Boolean },
    },
    template: `
        <div class="markdown-edit-div">
            <table class="table-responsive markdown-edit-table">
                <tbody class="markdown-edit-table-body">
                    <tr class="markdown-edit-caption-row">
                        <td class="markdown-edit-td">
                            {{title}}&nbsp;&nbsp;
                            <button class="markdown-edit-button" v-on:click="bold()" title="Gras"><i class="fas fa-bold"></i></button>
                            <button class="markdown-edit-button" v-on:click="italic()" title="Italique"><i class="fas fa-italic"></i></button>
                            <button class="markdown-edit-button" v-on:click="insertNbsp()" title="Espace insécable">_</button>
                            <button class="markdown-edit-button" v-on:click="togglePreview()" title="Apperçu du rendu Markdown"><i class="fab fa-markdown"></i></button>
                        </td>
                    </tr>
                    <tr class="markdown-edit-row">
                        <td class="markdown-edit-td">
                            <textarea class="markdown-edit-textarea" v-model="content" v-bind:rows="rows" v-on:keydown="onKeyDown" v-on:input="onInput" ref="text_area_control"></textarea>
                        </td>
                    </tr>
                    <tr class="markdown-edit-row" v-if="previewVisible">
                        <td class="markdown-edit-td">
                            <span class="markdown-render" v-html="renderedHtml()" ></span>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    `,
    data() {
        return {
            content: this.modelValue,
            previewVisible: false,
        };
    },
    methods: {
        onInput(event) {
            this.$emit('update:modelValue', this.content);
            const textarea = this.$refs.text_area_control;
            textarea.style.height = (textarea.scrollHeight) + "px";
        },
        onKeyDown(event) {
            if (event.ctrlKey) {
                if (event.key == ' ') {
                    this.insertNbsp();
                    return;
                }
                if (event.key == 'i' || event.key == 'I') {
                    this.italic();
                    return;
                }
                if (event.key == 'b' || event.key == 'B') {
                    this.bold();
                    return;
                }
            }
        },
        insertNbsp() {
            const textarea = this.$refs.text_area_control;
            let cursorStartPosition = textarea.selectionStart;
            let cursorEndPosition = textarea.selectionEnd;
            let tmpStr = textarea.value;
            const nbsp = '&nbsp;';
            this.content = tmpStr.substring(0, cursorStartPosition) + nbsp + tmpStr.substring(cursorEndPosition, tmpStr.length);
            textarea.value = this.content;
            this.onInput();
            textarea.focus();
            textarea.selectionStart = cursorStartPosition + nbsp.length;
            textarea.selectionEnd = cursorStartPosition + nbsp.length;
        },
        addMarkup(markup) {
            const textarea = this.$refs.text_area_control;
            let cursorStartPosition = textarea.selectionStart;
            let cursorEndPosition = textarea.selectionEnd;
            const initialValue = textarea.value;
            const selectionIsEmpty = cursorStartPosition == cursorEndPosition;
            if (!selectionIsEmpty) {
                // Don't include spaces in the modified area
                while (initialValue.charAt(cursorStartPosition) == ' ')
                    cursorStartPosition++;
                while (initialValue.charAt(cursorEndPosition - 1) == ' ')
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
        },
        renderedHtml() {
            return convertMarkdown(this.content, this.isinfrench);
        },
    },
});
