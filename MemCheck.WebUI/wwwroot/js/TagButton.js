export const TagButton = Vue.defineComponent({
    components: {
    },
    props: {
        name: { required: true, type: String },
        id: { required: false, type: String, default: null }, // When no idea is given, the button is readonly (can not be clicked)
        additionalbuttonclass: { required: false, type: String, default: null },
    },
    template: `
        <div id="TagButtonComponent" class="tag-button-div">
            <button v-bind:class="getButtonClass()" v-on:click="onClick" v-bind:disabled="!canBeClicked()">{{name}}<span v-if="canBeClicked()"> &times;</span></button>
        </div>
    `,
    methods: {
        getButtonClass() {
            let result = 'tag-button';
            if (this.additionalbuttonclass !== null)
                result = `${result} ${this.additionalbuttonclass}`;
            return result;
        },
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
