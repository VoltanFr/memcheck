const TagButton = Vue.defineComponent({
    components: {
    },
    props: {
        name: { required: true, type: String },
        id: { required: false, type: String, default: null }, //When no idea is given, the button is readonly (can not be clicked)
    },
    template: `
        <div id="TagButtonComponent" class="tag-button-div">
            <button class="tag-button" v-on:click="onClick" v-bind:disabled="!canBeClicked()">{{name}}<span v-if="canBeClicked()"> &times;</span></button>
        </div>
    `,
    methods: {
        canBeClicked() {
            return this.id !== null;
        },
        onClick(event) {
            event.stopPropagation();
            event.preventDefault();
            this.$emit('click', this.id);
        },
    },
});
