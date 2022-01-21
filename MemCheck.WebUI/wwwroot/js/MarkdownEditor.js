﻿const MarkdownEditor = Vue.defineComponent({
    components: {
        // mention sub components, if we used in the template
    },
    props: {
        modelValue: { required: true, type: String },
        rows: { required: false, type: Number, default: 2 },
        title: { required: true, type: String },
        isinfrench: { required: true, type: Boolean },
    },
    mounted() {
        this.adaptTextAreaSize();
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
                            <button class="markdown-edit-button" v-on:click="insertTable()" title="Modèle de table"><i class="fas fa-table"></i></button>
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
                            <span class="markdown-render markdown-body markdown-edit-preview" v-html="renderedHtml()" ></span>
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
            this.adaptTextAreaSize();
        },
        adaptTextAreaSize() {
            const textarea = this.$refs.text_area_control;
            var scrollHeightBeforeUpdate = 0;
            do {
                scrollHeightBeforeUpdate = textarea.scrollHeight;
                const newHeight = Math.max(50, textarea.scrollHeight);
                textarea.style.height = newHeight + "px";
            } while (scrollHeightBeforeUpdate !== textarea.scrollHeight);
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
            var text = `| Aligné à gauche | Centré | Aligné à droite |
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