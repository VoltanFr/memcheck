'use strict';

/* exported ImageChoice */
const ImageChoice = Vue.defineComponent({
    components: {
    },
    props: {
        imagenamesmallscreenstr: { required: true, type: String },
        imagenamemediumscreenstr: { required: true, type: String },
        imagenamebigscreenstr: { required: true, type: String },
    },
    template: `
        <div id="ImageChoiceComponent" class="d-flex authoring-image-choice">
            <span class="small-screen-text">{{imagenamesmallscreenstr}}</span><span class="medium-screen-text">{{imagenamemediumscreenstr}}</span><span class="big-screen-text">{{imagenamebigscreenstr}}</span>
            <input class="authoring-image-choice-input flex-fill responsive-padding-edit" v-model="imageName" />
            <button class="toolbar-button-circle toolbar-button" v-on:click="pasteAndAdd()"><i class="fas fa-paste"></i></button>
            <button class="toolbar-button-circle toolbar-button" v-on:click="add()"><i class="fas fa-plus"></i></button>
        </div>
    `,
    data() {
        return {
            imageName: '',
        };
    },
    methods: {
        async pasteAndAdd() {
            await navigator.clipboard.readText()
                .then(text => {
                    if (!text)
                        return;

                    this.imageName = text;
                    this.add();
                })
                .catch(err => {
                    tellAxiosError(err);
                });
        },
        add() {
            this.$emit('add', this.imageName);
        },
    },
});
