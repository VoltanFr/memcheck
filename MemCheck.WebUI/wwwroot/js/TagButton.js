const TagButton = Vue.defineComponent({
    components: {
    },
    props: {
        name: { required: true, type: String },
        id: { required: true, type: String },
    },
    template: `
        <div id="TagButtonComponent" class="tag-button-div">
            <button class="tag-button" v-on:click="onClick">{{name}} &times;</button>
        </div>
    `,
    methods: {
        onClick(event) {
            event.stopPropagation();
            event.preventDefault();
            this.$emit('click', this.id);
        },
    },
});
